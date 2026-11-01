using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Interaction-only component for a route merchant. Movement, visibility, and
    /// trade rotation are centrally scheduled by TetherfallRoamingWorldDirector.
    /// </summary>
    public sealed class TetherfallRoamingMerchant : MonoBehaviour, IWorldInteractable
    {
        private TetherfallRoamingWorldDirector director;
        private int merchantIndex;

        public string InteractionPrompt =>
            director != null ? director.GetMerchantInteractionPrompt(merchantIndex) : string.Empty;

        internal void Initialize(TetherfallRoamingWorldDirector owner, int index)
        {
            director = owner;
            merchantIndex = index;
        }

        public bool CanInteract(TetherfallGame game)
        {
            return director != null && director.CanInteractWithMerchant(merchantIndex, game);
        }

        public void Interact(TetherfallGame game)
        {
            director?.TradeWithMerchant(merchantIndex, game);
        }
    }

    /// <summary>
    /// A single bounded scheduler for route merchants, regional packs, and named
    /// landmark minibosses. Distant agents are inactive, and all route movement
    /// happens here rather than in one Update per roaming presentation.
    /// </summary>
    public sealed class TetherfallRoamingWorldDirector : MonoBehaviour
    {
        public const string RuntimeRootName = "Tetherfall Roaming World";
        public const int AuthoredMerchantPopulation = 3;
        public const int AuthoredPackPopulation = 3;
        public const int AuthoredMinibossPopulation = 3;
        public const int MaxVisibleMerchants = 2;
        public const int MaxAwakeHostileGroups = 2;
        public const int MaxActiveEnemies = 6;
        public const int MaxResidentEnemyObjects = 18;
        public const int MaxActiveRoamingRenderers = 256;
        public const int MaxMerchantSharedMaterials = 3;
        public const int MaxMerchantRenderers = 24;
        public const int MaxCoreAgentUpdates = 1 + MaxActiveEnemies;

        private const int MaxEnemiesPerGroup = 3;
        private const float MerchantWakeDistance = 72f;
        private const float MerchantKeepVisibleDistance = 82f;
        private const float HostileWakeDistance = 82f;
        private const float HostileKeepAwakeDistance = 96f;
        private const float MerchantInteractionHoldDistance = 5.2f;
        private const float PackEngagementDistance = 42f;
        private const float MinibossEngagementDistance = 48f;
        private const float MinibossIntroDistance = 54f;
        private const float BudgetRefreshSeconds = 0.25f;
        private const float TradeRepeatGuardSeconds = 0.7f;
        private const float MerchantStopSeconds = 4.5f;
        private const float HostileStopSeconds = 2.1f;
        private const float MaximumGroundStep = 2.35f;
        private const float MinimumGroundNormalY = 0.68f;

        private static readonly HashSet<string> SessionRewardedMinibosses =
            new HashSet<string>(StringComparer.Ordinal);

        private sealed class TradeOffer
        {
            public readonly int timber;
            public readonly int stone;
            public readonly int resin;
            public readonly int chimesalt;
            public readonly ResourceKind rewardKind;
            public readonly int rewardAmount;
            public readonly string prompt;
            public readonly string costLabel;
            public readonly string rewardLabel;

            public TradeOffer(
                int timberCost,
                int stoneCost,
                int resinCost,
                int chimesaltCost,
                ResourceKind kind,
                int amount,
                string interactionPrompt,
                string cost,
                string reward)
            {
                timber = timberCost;
                stone = stoneCost;
                resin = resinCost;
                chimesalt = chimesaltCost;
                rewardKind = kind;
                rewardAmount = amount;
                prompt = interactionPrompt;
                costLabel = cost;
                rewardLabel = reward;
            }

            public int TotalCost => timber + stone + resin + chimesalt;
        }

        private sealed class MerchantBlueprint
        {
            public string id;
            public string displayName;
            public string routeDescription;
            public float speed;
            public Vector3[] route;
            public TradeOffer[] trades;
        }

        private sealed class HostileBlueprint
        {
            public string id;
            public string displayName;
            public string patrolName;
            public bool miniboss;
            public float speed;
            public float respawnSeconds;
            public Vector3[] route;
            public string[] initialEnemyIds;
            public string[] reinforcementEnemyIds;
            public string introCue;
            public string phaseCue;
            public ResourceKind rewardA;
            public int rewardAAmount;
            public ResourceKind rewardB;
            public int rewardBAmount;
            public int experienceReward;
        }

        private sealed class MerchantState
        {
            public MerchantBlueprint blueprint;
            public GameObject root;
            public TetherfallRoamingMerchant interactable;
            public CharacterController controller;
            public Renderer[] renderers;
            public Vector3 routePosition;
            public int nextStop;
            public int tradeIndex;
            public int lastTradeFrame = -1;
            public float waitUntil;
            public float interactionHoldUntil;
            public float nextTradeAt;
            public bool settlingPrompt;
            public bool budgetSelected;
            public bool visible;
            public string prompt;
        }

        private sealed class HostileState
        {
            public readonly TetherfallRoamingWorldDirector owner;
            public readonly HostileBlueprint blueprint;
            public readonly Enemy[] enemies = new Enemy[MaxEnemiesPerGroup];
            public readonly CharacterController[] controllers = new CharacterController[MaxEnemiesPerGroup];
            public readonly Renderer[][] renderers = new Renderer[MaxEnemiesPerGroup][];
            public Vector3 center;
            public int nextStop;
            public int rendererCount;
            public float waitUntil;
            public float respawnAt;
            public bool phaseTriggered;
            public bool introPlayed;
            public bool budgetSelected;
            public bool awake;

            public HostileState(TetherfallRoamingWorldDirector roamingOwner, HostileBlueprint definition)
            {
                owner = roamingOwner;
                blueprint = definition;
                center = definition.route[0];
                nextStop = definition.route.Length > 1 ? 1 : 0;
                waitUntil = Time.time + 1.25f;
            }

            public void OnEnemyDefeated(Enemy enemy)
            {
                owner.HandleEnemyDefeated(this, enemy);
            }
        }

        private static readonly MerchantBlueprint[] MerchantBlueprints =
        {
            new MerchantBlueprint
            {
                id = "roaming.merchant.lark",
                displayName = "Lark Fen",
                routeDescription = "Hearthmarket south gate, Eastbank farm road, Seedspindle approach",
                speed = 2.05f,
                route = new[]
                {
                    new Vector3(-12f, 0.05f, 48f),
                    new Vector3(20f, 0.05f, 48f),
                    new Vector3(48f, 0.05f, 22f),
                    new Vector3(54f, 0.05f, -8f),
                    new Vector3(32f, 0.05f, -28f),
                    new Vector3(10f, 0.05f, -20f),
                    new Vector3(8f, 0.05f, 12f)
                },
                trades = new[]
                {
                    new TradeOffer(
                        2, 0, 0, 0, ResourceKind.Herbs, 2,
                        "Trade 2 Timber for 2 Herbs", "2 Timber", "2 Herbs"),
                    new TradeOffer(
                        0, 2, 0, 0, ResourceKind.Resin, 1,
                        "Trade 2 Stone for 1 Resin", "2 Stone", "1 Resin"),
                    new TradeOffer(
                        0, 0, 2, 0, ResourceKind.Chimesalt, 1,
                        "Trade 2 Resin for 1 Chimesalt", "2 Resin", "1 Chimesalt")
                }
            },
            new MerchantBlueprint
            {
                id = "roaming.merchant.nim",
                displayName = "Nim Vale",
                routeDescription = "Veyrwater east gate, north reedline, ferry ridge",
                speed = 1.9f,
                route = new[]
                {
                    new Vector3(188f, 0.05f, 48f),
                    new Vector3(222f, 0.05f, 48f),
                    new Vector3(232f, 0.05f, 70f),
                    new Vector3(226f, 0.05f, 104f),
                    new Vector3(202f, 0.05f, 116f),
                    new Vector3(178f, 0.05f, 102f),
                    new Vector3(176f, 0.05f, 72f)
                },
                trades = new[]
                {
                    new TradeOffer(
                        0, 0, 1, 0, ResourceKind.Fiber, 2,
                        "Trade 1 Resin for 2 Fiber", "1 Resin", "2 Fiber"),
                    new TradeOffer(
                        0, 0, 0, 1, ResourceKind.Herbs, 3,
                        "Trade 1 Chimesalt for 3 Herbs", "1 Chimesalt", "3 Herbs"),
                    new TradeOffer(
                        0, 2, 0, 0, ResourceKind.Herbs, 2,
                        "Trade 2 Stone for 2 Herbs", "2 Stone", "2 Herbs")
                }
            },
            new MerchantBlueprint
            {
                id = "roaming.merchant.orra",
                displayName = "Orra Flint",
                routeDescription = "relay salvage table, Ash Spire, west signal shelf",
                speed = 1.85f,
                route = new[]
                {
                    new Vector3(-110f, 0.05f, -14f),
                    new Vector3(-132f, 0.05f, 8f),
                    new Vector3(-154f, 0.05f, 28f),
                    new Vector3(-180f, 0.05f, 38f),
                    new Vector3(-206f, 0.05f, 24f),
                    new Vector3(-204f, 0.05f, 0f),
                    new Vector3(-176f, 0.05f, -8f),
                    new Vector3(-146f, 0.05f, -4f)
                },
                trades = new[]
                {
                    new TradeOffer(
                        0, 2, 1, 0, ResourceKind.Ore, 2,
                        "Trade 2 Stone + 1 Resin for 2 Ore", "2 Stone + 1 Resin", "2 Ore"),
                    new TradeOffer(
                        1, 0, 0, 1, ResourceKind.Ore, 3,
                        "Trade 1 Timber + 1 Chimesalt for 3 Ore", "1 Timber + 1 Chimesalt", "3 Ore"),
                    new TradeOffer(
                        2, 0, 0, 0, ResourceKind.Resin, 1,
                        "Trade 2 Timber for 1 Resin", "2 Timber", "1 Resin")
                }
            }
        };

        private static readonly HostileBlueprint[] PackBlueprints =
        {
            new HostileBlueprint
            {
                id = "roaming.pack.briarbound",
                displayName = "Briarbound Foragers",
                patrolName = "Southbank briar circuit",
                speed = 1.55f,
                respawnSeconds = 85f,
                route = new[]
                {
                    new Vector3(-18f, 0.05f, -56f),
                    new Vector3(8f, 0.05f, -76f),
                    new Vector3(34f, 0.05f, -66f),
                    new Vector3(42f, 0.05f, -40f),
                    new Vector3(16f, 0.05f, -30f),
                    new Vector3(-10f, 0.05f, -38f)
                },
                initialEnemyIds = new[]
                {
                    "enemy.mossglass_stalker",
                    "enemy.mossglass_stalker",
                    "enemy.hollow_scout"
                }
            },
            new HostileBlueprint
            {
                id = "roaming.pack.siltwake",
                displayName = "Siltwake Drifters",
                patrolName = "Low-fen lantern circuit",
                speed = 1.35f,
                respawnSeconds = 95f,
                route = new[]
                {
                    new Vector3(88f, 0.05f, -54f),
                    new Vector3(108f, 0.05f, -72f),
                    new Vector3(136f, 0.05f, -66f),
                    new Vector3(144f, 0.05f, -50f),
                    new Vector3(136f, 0.05f, -24f),
                    new Vector3(106f, 0.05f, -28f),
                    new Vector3(90f, 0.05f, -40f)
                },
                initialEnemyIds = new[]
                {
                    "enemy.fen_spitter",
                    "enemy.fen_siltcaller",
                    "enemy.fen_spitter"
                }
            },
            new HostileBlueprint
            {
                id = "roaming.pack.emberjaw",
                displayName = "Emberjaw Scavengers",
                patrolName = "Lower relay shelf circuit",
                speed = 1.45f,
                respawnSeconds = 105f,
                route = new[]
                {
                    new Vector3(-96f, 0.05f, 42f),
                    new Vector3(-118f, 0.05f, 58f),
                    new Vector3(-144f, 0.05f, 52f),
                    new Vector3(-158f, 0.05f, 30f),
                    new Vector3(-142f, 0.05f, 12f),
                    new Vector3(-112f, 0.05f, 18f)
                },
                initialEnemyIds = new[]
                {
                    "enemy.cinder_echo",
                    "enemy.cinder_echo",
                    "enemy.relay_breaker"
                }
            }
        };

        private static readonly HostileBlueprint[] MinibossBlueprints =
        {
            new HostileBlueprint
            {
                id = "roaming.miniboss.thornwake",
                displayName = "Thornwake, the Tollkeeper",
                patrolName = "Southroot Marker circuit",
                miniboss = true,
                speed = 1.2f,
                respawnSeconds = 240f,
                route = new[]
                {
                    new Vector3(-20f, 0.05f, -108f),
                    new Vector3(4f, 0.05f, -116f),
                    new Vector3(30f, 0.05f, -106f),
                    new Vector3(36f, 0.05f, -86f),
                    new Vector3(14f, 0.05f, -72f),
                    new Vector3(-14f, 0.05f, -80f)
                },
                initialEnemyIds = new[] { "enemy.elite.mossfang_matriarch" },
                reinforcementEnemyIds = new[]
                {
                    "enemy.mossglass_stalker",
                    "enemy.hollow_scout"
                },
                introCue = "Rootglass antlers turn toward the road.",
                phaseCue = "The tollkeeper tears open two briar trails",
                rewardA = ResourceKind.Herbs,
                rewardAAmount = 4,
                rewardB = ResourceKind.Resin,
                rewardBAmount = 2,
                experienceReward = 80
            },
            new HostileBlueprint
            {
                id = "roaming.miniboss.ilyra",
                displayName = "Ilyra, the Lantern Heron",
                patrolName = "Veyrwater outer reedline",
                miniboss = true,
                speed = 1.15f,
                respawnSeconds = 270f,
                route = new[]
                {
                    new Vector3(168f, 0.05f, 108f),
                    new Vector3(194f, 0.05f, 118f),
                    new Vector3(222f, 0.05f, 110f),
                    new Vector3(232f, 0.05f, 88f),
                    new Vector3(226f, 0.05f, 62f),
                    new Vector3(232f, 0.05f, 42f),
                    new Vector3(210f, 0.05f, 36f),
                    new Vector3(184f, 0.05f, 44f),
                    new Vector3(170f, 0.05f, 70f)
                },
                initialEnemyIds = new[] { "enemy.elite.siltglass_heron" },
                reinforcementEnemyIds = new[]
                {
                    "enemy.fen_spitter",
                    "enemy.fen_siltcaller"
                },
                introCue = "A drowned bell-note carries across the reeds.",
                phaseCue = "Its lantern plumage calls the fen awake",
                rewardA = ResourceKind.Chimesalt,
                rewardAAmount = 3,
                rewardB = ResourceKind.Fiber,
                rewardBAmount = 3,
                experienceReward = 90
            },
            new HostileBlueprint
            {
                id = "roaming.miniboss.kharos",
                displayName = "Kharos, Relay-Eater",
                patrolName = "Broken west relay circuit",
                miniboss = true,
                speed = 1.05f,
                respawnSeconds = 300f,
                route = new[]
                {
                    new Vector3(-172f, 0.05f, 58f),
                    new Vector3(-198f, 0.05f, 62f),
                    new Vector3(-222f, 0.05f, 48f),
                    new Vector3(-230f, 0.05f, 24f),
                    new Vector3(-218f, 0.05f, 2f),
                    new Vector3(-190f, 0.05f, 0f),
                    new Vector3(-170f, 0.05f, 22f)
                },
                initialEnemyIds = new[] { "enemy.elite.cindermaw" },
                reinforcementEnemyIds = new[]
                {
                    "enemy.cinder_echo",
                    "enemy.cinder_echo"
                },
                introCue = "A relay plate buckles under an answering roar.",
                phaseCue = "Kharos vents a second ash-heart",
                rewardA = ResourceKind.Ore,
                rewardAAmount = 4,
                rewardB = ResourceKind.Chimesalt,
                rewardBAmount = 2,
                experienceReward = 110
            }
        };

        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        private TetherfallGame game;
        private MerchantState[] merchants;
        private HostileState[] packs;
        private HostileState[] minibosses;
        private Material merchantClothMaterial;
        private Material merchantLeatherMaterial;
        private Material merchantCopperMaterial;
        private float nextBudgetAt;
        private bool initialized;

        public int RuntimeMerchantCount => merchants != null ? merchants.Length : 0;
        public int RuntimePackCount => packs != null ? packs.Length : 0;
        public int RuntimeMinibossCount => minibosses != null ? minibosses.Length : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionRewards()
        {
            SessionRewardedMinibosses.Clear();
        }

        public static void EnsureInitialized(TetherfallGame owner)
        {
            if (owner == null)
            {
                return;
            }

            var directors = owner.GetComponentsInChildren<TetherfallRoamingWorldDirector>(true);
            TetherfallRoamingWorldDirector primary = null;
            for (var index = 0; index < directors.Length; index++)
            {
                var candidate = directors[index];
                if (candidate != null && (primary == null || (!primary.initialized && candidate.initialized)))
                {
                    primary = candidate;
                }
            }

            if (primary == null)
            {
                var root = new GameObject(RuntimeRootName);
                root.transform.SetParent(owner.transform, false);
                primary = root.AddComponent<TetherfallRoamingWorldDirector>();
            }

            primary.Configure(owner);
            for (var index = 0; index < directors.Length; index++)
            {
                var duplicate = directors[index];
                if (duplicate != null && duplicate != primary)
                {
                    duplicate.enabled = false;
                    duplicate.gameObject.SetActive(false);
                    DestroyRuntimeObject(duplicate.gameObject);
                }
            }
        }

        public static bool ValidateAuthoredConfiguration(out string result)
        {
            if (MerchantBlueprints.Length != AuthoredMerchantPopulation ||
                PackBlueprints.Length != AuthoredPackPopulation ||
                MinibossBlueprints.Length != AuthoredMinibossPopulation)
            {
                result = "Roaming population constants do not match the authored definitions.";
                return false;
            }

            if (!ActivationBudgetsAreValid(
                    MaxVisibleMerchants,
                    MaxAwakeHostileGroups,
                    MaxActiveEnemies,
                    MaxEnemiesPerGroup))
            {
                result = "Roaming activation budgets are internally inconsistent.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < MerchantBlueprints.Length; index++)
            {
                var merchant = MerchantBlueprints[index];
                if (!ValidateIdentity(ids, merchant.id, merchant.displayName, out result) ||
                    string.IsNullOrWhiteSpace(merchant.routeDescription) ||
                    !ValidateRoute(merchant.displayName, merchant.route, out result))
                {
                    if (string.IsNullOrEmpty(result))
                    {
                        result = $"{merchant.displayName} has no route description.";
                    }

                    return false;
                }

                if (merchant.trades == null || merchant.trades.Length < 3)
                {
                    result = $"{merchant.displayName} needs at least three rotating trades.";
                    return false;
                }

                for (var tradeIndex = 0; tradeIndex < merchant.trades.Length; tradeIndex++)
                {
                    var trade = merchant.trades[tradeIndex];
                    if (trade == null ||
                        trade.TotalCost <= 0 ||
                        trade.rewardAmount <= 0 ||
                        string.IsNullOrWhiteSpace(trade.prompt))
                    {
                        result = $"{merchant.displayName} has an invalid trade at index {tradeIndex}.";
                        return false;
                    }
                }
            }

            for (var index = 0; index < PackBlueprints.Length; index++)
            {
                var pack = PackBlueprints[index];
                if (!ValidateHostileBlueprint(pack, false, ids, out result))
                {
                    return false;
                }
            }

            for (var index = 0; index < MinibossBlueprints.Length; index++)
            {
                var miniboss = MinibossBlueprints[index];
                if (!ValidateHostileBlueprint(miniboss, true, ids, out result))
                {
                    return false;
                }
            }

            result =
                $"{MerchantBlueprints.Length} merchants, {PackBlueprints.Length} packs, " +
                $"{MinibossBlueprints.Length} minibosses; budgets {MaxVisibleMerchants} merchants, " +
                $"{MaxAwakeHostileGroups} hostile groups, {MaxActiveEnemies} enemies.";
            return true;
        }

        private static bool ActivationBudgetsAreValid(
            int visibleMerchants,
            int awakeGroups,
            int activeEnemies,
            int enemiesPerGroup)
        {
            return visibleMerchants > 0 &&
                   awakeGroups > 0 &&
                   activeEnemies >= enemiesPerGroup &&
                   activeEnemies <= awakeGroups * enemiesPerGroup;
        }

        private static bool ValidateIdentity(
            HashSet<string> ids,
            string id,
            string displayName,
            out string result)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
            {
                result = "A roaming definition is missing an ID or display name.";
                return false;
            }

            if (!ids.Add(id))
            {
                result = $"Duplicate roaming ID '{id}'.";
                return false;
            }

            result = string.Empty;
            return true;
        }

        private static bool ValidateHostileBlueprint(
            HostileBlueprint blueprint,
            bool shouldBeMiniboss,
            HashSet<string> ids,
            out string result)
        {
            result = string.Empty;
            if (blueprint == null)
            {
                result = "A hostile roaming definition is missing.";
                return false;
            }

            if (!ValidateIdentity(ids, blueprint.id, blueprint.displayName, out result) ||
                blueprint.miniboss != shouldBeMiniboss ||
                string.IsNullOrWhiteSpace(blueprint.patrolName) ||
                !ValidateRoute(blueprint.displayName, blueprint.route, out result))
            {
                if (string.IsNullOrEmpty(result))
                {
                    result = "A hostile roaming definition has invalid type or patrol metadata.";
                }

                return false;
            }

            var expectedInitialCount = shouldBeMiniboss ? 1 : MaxEnemiesPerGroup;
            if (blueprint.initialEnemyIds == null ||
                blueprint.initialEnemyIds.Length != expectedInitialCount)
            {
                result = $"{blueprint.displayName} has an invalid initial roster.";
                return false;
            }

            for (var index = 0; index < blueprint.initialEnemyIds.Length; index++)
            {
                if (!ValidateEnemyId(blueprint.displayName, blueprint.initialEnemyIds[index], out result))
                {
                    return false;
                }
            }

            if (!shouldBeMiniboss)
            {
                return true;
            }

            if (blueprint.reinforcementEnemyIds == null ||
                blueprint.reinforcementEnemyIds.Length != MaxEnemiesPerGroup - 1 ||
                blueprint.rewardAAmount <= 0 ||
                blueprint.rewardBAmount <= 0 ||
                blueprint.experienceReward <= 0 ||
                string.IsNullOrWhiteSpace(blueprint.introCue) ||
                string.IsNullOrWhiteSpace(blueprint.phaseCue))
            {
                result = $"{blueprint.displayName} has invalid phase or reward metadata.";
                return false;
            }

            for (var index = 0; index < blueprint.reinforcementEnemyIds.Length; index++)
            {
                if (!ValidateEnemyId(
                        blueprint.displayName,
                        blueprint.reinforcementEnemyIds[index],
                        out result))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateEnemyId(string owner, string enemyId, out string result)
        {
            var enemy = EnemyCatalog.Get(enemyId);
            if (enemy == null || !string.Equals(enemy.id, enemyId, StringComparison.Ordinal))
            {
                result = $"{owner} references unknown enemy ID '{enemyId}'.";
                return false;
            }

            result = string.Empty;
            return true;
        }

        private static bool ValidateRoute(string owner, Vector3[] route, out string result)
        {
            if (route == null || route.Length < 4)
            {
                result = $"{owner} needs a route loop with at least four authored stops.";
                return false;
            }

            for (var index = 0; index < route.Length; index++)
            {
                var point = route[index];
                if (!IsFinite(point) ||
                    Mathf.Abs(point.x) > 234f ||
                    Mathf.Abs(point.z) > 124f ||
                    Mathf.Abs(point.y) > 3f)
                {
                    result = $"{owner} route stop {index} leaves the bounded roaming world.";
                    return false;
                }

                if (HorizontalSqr(point, new Vector3(0f, 0f, 72f)) < 22f * 22f ||
                    HorizontalSqr(point, new Vector3(203f, 0f, 72f)) < 22f * 22f)
                {
                    result = $"{owner} route stop {index} enters a critical city interior.";
                    return false;
                }

                var nextPoint = route[(index + 1) % route.Length];
                if (SegmentEntersCircle(
                        point,
                        nextPoint,
                        new Vector3(0f, 0f, 72f),
                        22f) ||
                    SegmentEntersCircle(
                        point,
                        nextPoint,
                        new Vector3(203f, 0f, 72f),
                        22f))
                {
                    result = $"{owner} route segment {index} crosses a critical city interior.";
                    return false;
                }
            }

            result = string.Empty;
            return true;
        }

        private static bool SegmentEntersCircle(
            Vector3 start,
            Vector3 end,
            Vector3 center,
            float radius)
        {
            var segmentX = end.x - start.x;
            var segmentZ = end.z - start.z;
            var lengthSqr = segmentX * segmentX + segmentZ * segmentZ;
            if (lengthSqr <= 0.0001f)
            {
                return HorizontalSqr(start, center) < radius * radius;
            }

            var startX = start.x - center.x;
            var startZ = start.z - center.z;
            var t = Mathf.Clamp01(-(startX * segmentX + startZ * segmentZ) / lengthSqr);
            var closestX = startX + segmentX * t;
            var closestZ = startZ + segmentZ * t;
            return closestX * closestX + closestZ * closestZ < radius * radius;
        }

        private static bool IsFinite(Vector3 point)
        {
            return !float.IsNaN(point.x) &&
                   !float.IsNaN(point.y) &&
                   !float.IsNaN(point.z) &&
                   !float.IsInfinity(point.x) &&
                   !float.IsInfinity(point.y) &&
                   !float.IsInfinity(point.z);
        }

        private void Configure(TetherfallGame owner)
        {
            game = owner;
            if (initialized)
            {
                return;
            }

            if (!ValidateAuthoredConfiguration(out var validation))
            {
                Debug.LogError($"Roaming-world configuration rejected: {validation}");
                enabled = false;
                return;
            }

            gameObject.name = RuntimeRootName;
            merchantClothMaterial = game.CreateMaterial(new Color(0.16f, 0.38f, 0.37f), 0.02f, 0.34f);
            merchantLeatherMaterial = game.CreateMaterial(new Color(0.28f, 0.16f, 0.11f), 0.02f, 0.26f);
            merchantCopperMaterial = game.CreateMaterial(new Color(0.78f, 0.45f, 0.22f), 0.28f, 0.58f);

            merchants = new MerchantState[MerchantBlueprints.Length];
            for (var index = 0; index < MerchantBlueprints.Length; index++)
            {
                merchants[index] = CreateMerchant(index, MerchantBlueprints[index]);
            }

            packs = new HostileState[PackBlueprints.Length];
            for (var index = 0; index < PackBlueprints.Length; index++)
            {
                packs[index] = new HostileState(this, PackBlueprints[index]);
            }

            minibosses = new HostileState[MinibossBlueprints.Length];
            for (var index = 0; index < MinibossBlueprints.Length; index++)
            {
                minibosses[index] = new HostileState(this, MinibossBlueprints[index]);
            }

            nextBudgetAt = Time.time;
            initialized = true;
            Debug.Log($"Tetherfall roaming world ready: {validation}");
        }

        private MerchantState CreateMerchant(int index, MerchantBlueprint blueprint)
        {
            var root = new GameObject($"{blueprint.displayName} · route merchant");
            root.transform.SetParent(transform, false);
            var position = blueprint.route[0];
            if (TryProjectToGround(position, out var grounded))
            {
                position = grounded;
            }

            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.radius = 0.42f;
            controller.height = 1.9f;
            controller.center = Vector3.up * 0.95f;
            controller.stepOffset = 0.34f;
            controller.slopeLimit = 46f;

            AddMerchantPiece(
                root.transform,
                PrimitiveType.Capsule,
                "route coat",
                new Vector3(0f, 0.94f, 0f),
                new Vector3(0.82f, 0.96f, 0.72f),
                Quaternion.identity,
                merchantClothMaterial);
            AddMerchantPiece(
                root.transform,
                PrimitiveType.Sphere,
                "weather hood",
                new Vector3(0f, 1.72f, 0.02f),
                new Vector3(0.72f, 0.5f, 0.72f),
                Quaternion.identity,
                merchantClothMaterial);
            AddMerchantPiece(
                root.transform,
                PrimitiveType.Cube,
                "route pack",
                new Vector3(0f, 1.02f, -0.48f),
                new Vector3(0.72f, 0.84f, 0.34f),
                Quaternion.Euler(5f, 0f, 0f),
                merchantLeatherMaterial);
            AddMerchantPiece(
                root.transform,
                PrimitiveType.Cube,
                "pack clasp",
                new Vector3(0f, 1.05f, -0.67f),
                new Vector3(0.18f, 0.18f, 0.05f),
                Quaternion.identity,
                merchantCopperMaterial);
            AddMerchantPiece(
                root.transform,
                PrimitiveType.Cylinder,
                "walking staff",
                new Vector3(0.62f, 0.9f, 0.08f),
                new Vector3(0.07f, 0.9f, 0.07f),
                Quaternion.Euler(0f, 0f, -7f),
                merchantLeatherMaterial);
            AddMerchantPiece(
                root.transform,
                PrimitiveType.Cube,
                "route pennant",
                new Vector3(0.64f, 1.65f, 0.08f),
                new Vector3(0.36f, 0.22f, 0.04f),
                Quaternion.Euler(0f, 0f, -8f),
                merchantCopperMaterial);

            WorldVisuals.AddLabel(
                root.transform,
                blueprint.displayName,
                new Vector3(0f, 2.35f, 0f),
                new Color(0.95f, 0.84f, 0.67f),
                0.08f);

            var interactable = root.AddComponent<TetherfallRoamingMerchant>();
            interactable.Initialize(this, index);
            game.RegisterInteractable(interactable);

            var state = new MerchantState
            {
                blueprint = blueprint,
                root = root,
                interactable = interactable,
                controller = controller,
                renderers = root.GetComponentsInChildren<Renderer>(true),
                routePosition = position,
                nextStop = blueprint.route.Length > 1 ? 1 : 0,
                tradeIndex = index % blueprint.trades.Length,
                waitUntil = Time.time + 1.5f + index * 0.65f,
                prompt = blueprint.trades[index % blueprint.trades.Length].prompt
            };
            root.SetActive(false);
            return state;
        }

        private static void AddMerchantPiece(
            Transform parent,
            PrimitiveType primitive,
            string pieceName,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = pieceName;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            piece.transform.localRotation = localRotation;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            var collider = piece.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }
        }

        private void Update()
        {
            if (!initialized || game == null || game.Player == null)
            {
                return;
            }

            var deltaTime = Mathf.Min(Time.deltaTime, 0.08f);
            if (game.GameplayPaused)
            {
                PauseDirectorTimers(deltaTime);
                return;
            }

            if (Time.time >= nextBudgetAt)
            {
                RebalanceActivation();
                nextBudgetAt = Time.time + BudgetRefreshSeconds;
            }

            for (var index = 0; index < merchants.Length; index++)
            {
                TickMerchant(merchants[index], deltaTime);
            }

            for (var index = 0; index < packs.Length; index++)
            {
                TickHostile(packs[index], deltaTime);
            }

            for (var index = 0; index < minibosses.Length; index++)
            {
                TickHostile(minibosses[index], deltaTime);
            }
        }

        private void PauseDirectorTimers(float deltaTime)
        {
            nextBudgetAt += deltaTime;
            for (var index = 0; index < merchants.Length; index++)
            {
                var merchant = merchants[index];
                ShiftFutureTimer(ref merchant.waitUntil, deltaTime);
                ShiftFutureTimer(ref merchant.interactionHoldUntil, deltaTime);
                ShiftFutureTimer(ref merchant.nextTradeAt, deltaTime);
            }

            for (var index = 0; index < packs.Length; index++)
            {
                PauseHostileTimers(packs[index], deltaTime);
            }

            for (var index = 0; index < minibosses.Length; index++)
            {
                PauseHostileTimers(minibosses[index], deltaTime);
            }
        }

        private static void PauseHostileTimers(HostileState state, float deltaTime)
        {
            ShiftFutureTimer(ref state.waitUntil, deltaTime);
            ShiftFutureTimer(ref state.respawnAt, deltaTime);
        }

        private static void ShiftFutureTimer(ref float timer, float deltaTime)
        {
            if (timer > Time.time)
            {
                timer += deltaTime;
            }
        }

        private void RebalanceActivation()
        {
            var playerPosition = game.PlayerTransform.position;
            SelectMerchants(playerPosition);
            SelectHostileGroups(playerPosition);
            EnforceRendererBudget(playerPosition);
        }

        private void SelectMerchants(Vector3 playerPosition)
        {
            for (var index = 0; index < merchants.Length; index++)
            {
                merchants[index].budgetSelected = false;
            }

            for (var slot = 0; slot < MaxVisibleMerchants; slot++)
            {
                MerchantState selected = null;
                var bestDistance = float.MaxValue;
                for (var index = 0; index < merchants.Length; index++)
                {
                    var merchant = merchants[index];
                    if (merchant.budgetSelected)
                    {
                        continue;
                    }

                    var distance = HorizontalSqr(playerPosition, merchant.routePosition);
                    var allowedDistance = merchant.visible
                        ? MerchantKeepVisibleDistance * MerchantKeepVisibleDistance
                        : MerchantWakeDistance * MerchantWakeDistance;
                    if (distance <= allowedDistance && distance < bestDistance)
                    {
                        selected = merchant;
                        bestDistance = distance;
                    }
                }

                if (selected == null)
                {
                    break;
                }

                selected.budgetSelected = true;
            }

            for (var index = 0; index < merchants.Length; index++)
            {
                SetMerchantVisible(merchants[index], merchants[index].budgetSelected);
            }
        }

        private void SelectHostileGroups(Vector3 playerPosition)
        {
            for (var index = 0; index < packs.Length; index++)
            {
                packs[index].budgetSelected = false;
            }

            for (var index = 0; index < minibosses.Length; index++)
            {
                minibosses[index].budgetSelected = false;
            }

            for (var slot = 0; slot < MaxAwakeHostileGroups; slot++)
            {
                HostileState selected = null;
                var bestDistance = float.MaxValue;
                var total = packs.Length + minibosses.Length;
                for (var index = 0; index < total; index++)
                {
                    var hostile = GetHostile(index);
                    if (hostile.budgetSelected || !CanWake(hostile))
                    {
                        continue;
                    }

                    var distance = HorizontalSqr(playerPosition, hostile.center);
                    var allowedDistance = hostile.awake
                        ? HostileKeepAwakeDistance * HostileKeepAwakeDistance
                        : HostileWakeDistance * HostileWakeDistance;
                    if (distance <= allowedDistance && distance < bestDistance)
                    {
                        selected = hostile;
                        bestDistance = distance;
                    }
                }

                if (selected == null)
                {
                    break;
                }

                selected.budgetSelected = true;
            }

            for (var index = 0; index < packs.Length; index++)
            {
                SetHostileAwake(packs[index], packs[index].budgetSelected);
            }

            for (var index = 0; index < minibosses.Length; index++)
            {
                SetHostileAwake(minibosses[index], minibosses[index].budgetSelected);
            }
        }

        private HostileState GetHostile(int flatIndex)
        {
            return flatIndex < packs.Length
                ? packs[flatIndex]
                : minibosses[flatIndex - packs.Length];
        }

        private static bool CanWake(HostileState state)
        {
            return HasLiveEnemies(state) || Time.time >= state.respawnAt;
        }

        private static void SetMerchantVisible(MerchantState merchant, bool visible)
        {
            if (merchant.visible == visible)
            {
                return;
            }

            merchant.visible = visible;
            if (merchant.root != null)
            {
                merchant.root.SetActive(visible);
            }
        }

        private void SetHostileAwake(HostileState state, bool shouldWake)
        {
            if (shouldWake && !HasLiveEnemies(state))
            {
                if (Time.time < state.respawnAt)
                {
                    shouldWake = false;
                }
                else
                {
                    SpawnInitialGroup(state);
                    shouldWake = HasLiveEnemies(state);
                }
            }

            state.awake = shouldWake;
            for (var index = 0; index < state.enemies.Length; index++)
            {
                var enemy = state.enemies[index];
                if (enemy != null && enemy.gameObject.activeSelf != shouldWake)
                {
                    enemy.gameObject.SetActive(shouldWake);
                }
            }
        }

        private void EnforceRendererBudget(Vector3 playerPosition)
        {
            var activeRenderers = CountActiveRoamingRenderers();
            while (activeRenderers > MaxActiveRoamingRenderers)
            {
                MerchantState farthestMerchant = null;
                var farthestMerchantDistance = -1f;
                for (var index = 0; index < merchants.Length; index++)
                {
                    var merchant = merchants[index];
                    if (!merchant.visible)
                    {
                        continue;
                    }

                    var distance = HorizontalSqr(playerPosition, merchant.routePosition);
                    if (distance > farthestMerchantDistance &&
                        distance > MerchantInteractionHoldDistance * MerchantInteractionHoldDistance * 4f)
                    {
                        farthestMerchant = merchant;
                        farthestMerchantDistance = distance;
                    }
                }

                if (farthestMerchant != null)
                {
                    activeRenderers -= farthestMerchant.renderers != null
                        ? farthestMerchant.renderers.Length
                        : 0;
                    farthestMerchant.budgetSelected = false;
                    SetMerchantVisible(farthestMerchant, false);
                    continue;
                }

                HostileState farthestHostile = null;
                var farthestHostileDistance = -1f;
                var total = packs.Length + minibosses.Length;
                for (var index = 0; index < total; index++)
                {
                    var hostile = GetHostile(index);
                    if (!hostile.awake)
                    {
                        continue;
                    }

                    var distance = HorizontalSqr(playerPosition, hostile.center);
                    if (distance > farthestHostileDistance)
                    {
                        farthestHostile = hostile;
                        farthestHostileDistance = distance;
                    }
                }

                if (farthestHostile == null)
                {
                    break;
                }

                activeRenderers -= farthestHostile.rendererCount;
                farthestHostile.budgetSelected = false;
                SetHostileAwake(farthestHostile, false);
            }
        }

        private int CountActiveRoamingRenderers()
        {
            var count = 0;
            for (var index = 0; index < merchants.Length; index++)
            {
                if (merchants[index].visible && merchants[index].renderers != null)
                {
                    count += merchants[index].renderers.Length;
                }
            }

            for (var index = 0; index < packs.Length; index++)
            {
                if (packs[index].awake)
                {
                    count += packs[index].rendererCount;
                }
            }

            for (var index = 0; index < minibosses.Length; index++)
            {
                if (minibosses[index].awake)
                {
                    count += minibosses[index].rendererCount;
                }
            }

            return count;
        }

        private void TickMerchant(MerchantState merchant, float deltaTime)
        {
            if (merchant.settlingPrompt && Time.time >= merchant.nextTradeAt)
            {
                merchant.settlingPrompt = false;
                RefreshMerchantPrompt(merchant);
            }

            if (!merchant.visible ||
                merchant.root == null ||
                Time.time < merchant.interactionHoldUntil ||
                HorizontalSqr(game.PlayerTransform.position, merchant.routePosition) <=
                MerchantInteractionHoldDistance * MerchantInteractionHoldDistance)
            {
                return;
            }

            var previousPosition = merchant.routePosition;
            var arrived = AdvanceRoute(
                merchant.blueprint.route,
                ref merchant.routePosition,
                ref merchant.nextStop,
                ref merchant.waitUntil,
                merchant.blueprint.speed,
                MerchantStopSeconds,
                deltaTime,
                out var travelDirection);

            var motion = merchant.routePosition - merchant.root.transform.position;
            if (motion.sqrMagnitude > 0.000001f && merchant.controller != null)
            {
                var flags = merchant.controller.Move(motion);
                if ((flags & CollisionFlags.Sides) != 0 && travelDirection.sqrMagnitude > 0.01f)
                {
                    var sidestep = Vector3.Cross(Vector3.up, travelDirection).normalized;
                    merchant.controller.Move(sidestep * deltaTime * merchant.blueprint.speed * 0.45f);
                }

                merchant.routePosition = merchant.root.transform.position;
            }

            var actualTravel = merchant.routePosition - previousPosition;
            actualTravel.y = 0f;
            if (actualTravel.sqrMagnitude > 0.001f)
            {
                merchant.root.transform.rotation = Quaternion.Slerp(
                    merchant.root.transform.rotation,
                    Quaternion.LookRotation(actualTravel.normalized),
                    1f - Mathf.Exp(-7f * deltaTime));
            }

            if (arrived)
            {
                merchant.tradeIndex =
                    (merchant.tradeIndex + 1) % merchant.blueprint.trades.Length;
                RefreshMerchantPrompt(merchant);
            }
        }

        private void TickHostile(HostileState state, float deltaTime)
        {
            if (!state.awake || !HasLiveEnemies(state))
            {
                return;
            }

            var playerPosition = game.PlayerTransform.position;
            var playerDistance = HorizontalSqr(playerPosition, state.center);
            if (state.blueprint.miniboss)
            {
                if (!state.introPlayed &&
                    playerDistance <= MinibossIntroDistance * MinibossIntroDistance)
                {
                    state.introPlayed = true;
                    game.Notify(
                        $"{state.blueprint.displayName} patrols {state.blueprint.patrolName}. " +
                        state.blueprint.introCue,
                        6.2f);
                }

                var primary = state.enemies[0];
                if (!state.phaseTriggered &&
                    primary != null &&
                    primary.MaxHealth > 0 &&
                    primary.Health <= Mathf.CeilToInt(primary.MaxHealth * 0.52f))
                {
                    TriggerMinibossPhase(state);
                }
            }

            var engagementDistance = state.blueprint.miniboss
                ? MinibossEngagementDistance
                : PackEngagementDistance;
            if (playerDistance <= engagementDistance * engagementDistance)
            {
                return;
            }

            AdvanceRoute(
                state.blueprint.route,
                ref state.center,
                ref state.nextStop,
                ref state.waitUntil,
                state.blueprint.speed,
                HostileStopSeconds,
                deltaTime,
                out var travelDirection);
            MoveHostileFormation(state, travelDirection, deltaTime);
        }

        private bool AdvanceRoute(
            Vector3[] route,
            ref Vector3 position,
            ref int nextStop,
            ref float waitUntil,
            float speed,
            float stopSeconds,
            float deltaTime,
            out Vector3 travelDirection)
        {
            travelDirection = Vector3.zero;
            if (route == null || route.Length == 0 || Time.time < waitUntil)
            {
                return false;
            }

            nextStop = Mathf.Clamp(nextStop, 0, route.Length - 1);
            var target = route[nextStop];
            var toTarget = target - position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            if (distance <= 0.42f)
            {
                var settled = target;
                if (TryProjectToGround(settled, out var groundedTarget))
                {
                    settled = groundedTarget;
                }

                position = settled;
                nextStop = (nextStop + 1) % route.Length;
                waitUntil = Time.time + stopSeconds;
                return true;
            }

            travelDirection = toTarget / distance;
            var candidate = position +
                            travelDirection * Mathf.Min(distance, speed * deltaTime);
            if (TryProjectToGround(candidate, out var grounded))
            {
                position = grounded;
            }

            return false;
        }

        private void MoveHostileFormation(
            HostileState state,
            Vector3 travelDirection,
            float deltaTime)
        {
            if (travelDirection.sqrMagnitude < 0.01f)
            {
                travelDirection =
                    state.blueprint.route[state.nextStop] - state.center;
                travelDirection.y = 0f;
                if (travelDirection.sqrMagnitude < 0.01f)
                {
                    travelDirection = Vector3.forward;
                }
                else
                {
                    travelDirection.Normalize();
                }
            }

            var right = Vector3.Cross(Vector3.up, travelDirection).normalized;
            for (var index = 0; index < state.enemies.Length; index++)
            {
                var enemy = state.enemies[index];
                var controller = state.controllers[index];
                if (enemy == null || controller == null || !enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 offset;
                switch (index)
                {
                    case 1:
                        offset = -right * 1.55f - travelDirection * 1.25f;
                        break;
                    case 2:
                        offset = right * 1.55f - travelDirection * 1.25f;
                        break;
                    default:
                        offset = Vector3.zero;
                        break;
                }

                var target = state.center + offset;
                var desired = target - enemy.transform.position;
                desired.y = 0f;
                var separation = Vector3.zero;
                for (var otherIndex = 0; otherIndex < state.enemies.Length; otherIndex++)
                {
                    var other = state.enemies[otherIndex];
                    if (other == null || other == enemy)
                    {
                        continue;
                    }

                    var away = enemy.transform.position - other.transform.position;
                    away.y = 0f;
                    var separationDistance = away.magnitude;
                    if (separationDistance > 0.01f && separationDistance < 1.45f)
                    {
                        separation += away / separationDistance * (1.45f - separationDistance);
                    }
                }

                desired += separation * 0.72f;
                if (desired.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                var direction = desired.normalized;
                var moveSpeed = Mathf.Max(state.blueprint.speed * 1.6f, 2.15f);
                var flags = controller.Move(direction * moveSpeed * deltaTime);
                if ((flags & CollisionFlags.Sides) != 0)
                {
                    controller.Move(right * ((index & 1) == 0 ? 1f : -1f) *
                                    moveSpeed * 0.42f * deltaTime);
                }

                enemy.transform.rotation = Quaternion.Slerp(
                    enemy.transform.rotation,
                    Quaternion.LookRotation(direction),
                    1f - Mathf.Exp(-6f * deltaTime));
            }
        }

        private bool TryProjectToGround(Vector3 desired, out Vector3 grounded)
        {
            var origin = desired + Vector3.up * 6f;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                16f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var bestDifference = float.MaxValue;
            var found = false;
            var bestPoint = desired;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = groundHits[index];
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.GetComponentInParent<PlayerAvatar>() != null ||
                    hit.normal.y < MinimumGroundNormalY)
                {
                    continue;
                }

                var difference = Mathf.Abs(hit.point.y - desired.y);
                if (difference <= MaximumGroundStep && difference < bestDifference)
                {
                    bestDifference = difference;
                    bestPoint = hit.point + Vector3.up * 0.045f;
                    found = true;
                }
            }

            grounded = found ? bestPoint : desired;
            return found;
        }

        internal string GetMerchantInteractionPrompt(int index)
        {
            return IsMerchantIndexValid(index) ? merchants[index].prompt : string.Empty;
        }

        internal bool CanInteractWithMerchant(int index, TetherfallGame owner)
        {
            return IsMerchantIndexValid(index) &&
                   owner == game &&
                   merchants[index].visible &&
                   merchants[index].root != null &&
                   merchants[index].root.activeInHierarchy;
        }

        internal void TradeWithMerchant(int index, TetherfallGame owner)
        {
            if (!CanInteractWithMerchant(index, owner))
            {
                return;
            }

            var merchant = merchants[index];
            if (merchant.lastTradeFrame == Time.frameCount || Time.time < merchant.nextTradeAt)
            {
                owner.Notify($"{merchant.blueprint.displayName}'s route stock is still settling.");
                return;
            }

            merchant.lastTradeFrame = Time.frameCount;
            var offer = merchant.blueprint.trades[merchant.tradeIndex];
            if (!owner.SpendResources(
                    offer.timber,
                    offer.stone,
                    offer.resin,
                    offer.chimesalt))
            {
                merchant.nextTradeAt = Time.time + 0.35f;
                merchant.interactionHoldUntil = Time.time + 0.55f;
                merchant.settlingPrompt = true;
                merchant.prompt = "Route stock settling";
                owner.Notify(
                    $"{merchant.blueprint.displayName} needs {offer.costLabel} for that trade.");
                return;
            }

            merchant.nextTradeAt = Time.time + TradeRepeatGuardSeconds;
            merchant.interactionHoldUntil = Time.time + 1.35f;
            merchant.settlingPrompt = true;
            merchant.prompt = "Route stock settling";
            owner.AddResource(
                offer.rewardKind,
                offer.rewardAmount,
                ResourceAcquisitionSource.VendorTrade);
            owner.Notify(
                $"{merchant.blueprint.displayName}: {offer.costLabel} traded for {offer.rewardLabel}.");
            merchant.tradeIndex =
                (merchant.tradeIndex + 1) % merchant.blueprint.trades.Length;
        }

        private void RefreshMerchantPrompt(MerchantState merchant)
        {
            if (merchant == null || merchant.blueprint.trades.Length == 0)
            {
                return;
            }

            merchant.prompt = merchant.blueprint.trades[merchant.tradeIndex].prompt;
        }

        private bool IsMerchantIndexValid(int index)
        {
            return initialized &&
                   merchants != null &&
                   index >= 0 &&
                   index < merchants.Length &&
                   merchants[index] != null;
        }

        private void SpawnInitialGroup(HostileState state)
        {
            if (HasLiveEnemies(state))
            {
                return;
            }

            state.center = state.blueprint.route[0];
            if (TryProjectToGround(state.center, out var groundedCenter))
            {
                state.center = groundedCenter;
            }

            state.nextStop = state.blueprint.route.Length > 1 ? 1 : 0;
            state.waitUntil = Time.time + 1.1f;
            state.phaseTriggered = false;
            state.introPlayed = false;
            var initialCount = Mathf.Min(
                state.blueprint.initialEnemyIds.Length,
                MaxEnemiesPerGroup);
            for (var index = 0; index < initialCount; index++)
            {
                SpawnHostileMember(
                    state,
                    index,
                    state.blueprint.initialEnemyIds[index],
                    GetSpawnPosition(state, index));
            }

            RefreshHostileRendererCount(state);
        }

        private Vector3 GetSpawnPosition(HostileState state, int index)
        {
            var forward = state.blueprint.route[state.nextStop] - state.center;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var offset = index == 1
                ? -right * 1.55f - forward * 1.25f
                : index == 2
                    ? right * 1.55f - forward * 1.25f
                    : Vector3.zero;
            var position = state.center + offset;
            if (TryProjectToGround(position, out var grounded))
            {
                position = grounded;
            }

            return position + Vector3.up * 0.08f;
        }

        private bool SpawnHostileMember(
            HostileState state,
            int index,
            string enemyId,
            Vector3 position)
        {
            if (index < 0 || index >= state.enemies.Length || state.enemies[index] != null)
            {
                return false;
            }

            var enemy = Enemy.Create(game, null, enemyId, position, state.OnEnemyDefeated);
            if (enemy == null)
            {
                return false;
            }

            enemy.transform.SetParent(transform, true);
            state.enemies[index] = enemy;
            state.controllers[index] = enemy.GetComponent<CharacterController>();
            if (state.blueprint.miniboss && index == 0)
            {
                enemy.gameObject.name = state.blueprint.displayName;
            }

            state.renderers[index] = enemy.GetComponentsInChildren<Renderer>(true);
            return true;
        }

        private void TriggerMinibossPhase(HostileState state)
        {
            state.phaseTriggered = true;
            var primary = state.enemies[0];
            if (primary == null)
            {
                return;
            }

            var availableSlots = Mathf.Max(0, MaxActiveEnemies - CountAwakeEnemies());
            var spawned = 0;
            for (var index = 0;
                 index < state.blueprint.reinforcementEnemyIds.Length &&
                 availableSlots > 0;
                 index++)
            {
                var angle = index == 0 ? -0.75f : 0.75f;
                var offset = new Vector3(Mathf.Sin(angle) * 4.2f, 0f, Mathf.Cos(angle) * 4.2f);
                var position = primary.transform.position + offset;
                if (TryProjectToGround(position, out var grounded))
                {
                    position = grounded + Vector3.up * 0.08f;
                }

                if (SpawnHostileMember(
                        state,
                        index + 1,
                        state.blueprint.reinforcementEnemyIds[index],
                        position))
                {
                    availableSlots--;
                    spawned++;
                }
            }

            RefreshHostileRendererCount(state);
            primary.ApplyStagger(0.32f);
            game.Notify(
                spawned > 0
                    ? $"{state.blueprint.displayName}: {state.blueprint.phaseCue}. " +
                      $"{spawned} reinforcements answer."
                    : $"{state.blueprint.displayName}: {state.blueprint.phaseCue}.",
                5.8f);
            EnforceRendererBudget(game.PlayerTransform.position);
        }

        private int CountAwakeEnemies()
        {
            var count = 0;
            for (var index = 0; index < packs.Length; index++)
            {
                if (packs[index].awake)
                {
                    count += CountLiveEnemies(packs[index]);
                }
            }

            for (var index = 0; index < minibosses.Length; index++)
            {
                if (minibosses[index].awake)
                {
                    count += CountLiveEnemies(minibosses[index]);
                }
            }

            return count;
        }

        private void HandleEnemyDefeated(HostileState state, Enemy defeatedEnemy)
        {
            var defeatedIndex = -1;
            for (var index = 0; index < state.enemies.Length; index++)
            {
                if (state.enemies[index] == defeatedEnemy)
                {
                    defeatedIndex = index;
                    state.enemies[index] = null;
                    state.controllers[index] = null;
                    state.renderers[index] = null;
                    break;
                }
            }

            RefreshHostileRendererCount(state);
            if (defeatedIndex < 0)
            {
                return;
            }

            if (state.blueprint.miniboss)
            {
                if (defeatedIndex == 0)
                {
                    CompleteMiniboss(state);
                }

                return;
            }

            if (!HasLiveEnemies(state))
            {
                state.awake = false;
                state.budgetSelected = false;
                state.respawnAt = Time.time + state.blueprint.respawnSeconds;
                state.center = state.blueprint.route[0];
                state.nextStop = state.blueprint.route.Length > 1 ? 1 : 0;
                game.Notify(
                    $"{state.blueprint.displayName} scattered. The route will repopulate later.");
            }
        }

        private void CompleteMiniboss(HostileState state)
        {
            for (var index = 1; index < state.enemies.Length; index++)
            {
                var reinforcement = state.enemies[index];
                if (reinforcement != null)
                {
                    UnityEngine.Object.Destroy(reinforcement.gameObject);
                }

                state.enemies[index] = null;
                state.controllers[index] = null;
                state.renderers[index] = null;
            }

            state.rendererCount = 0;
            state.awake = false;
            state.budgetSelected = false;
            state.respawnAt = Time.time + state.blueprint.respawnSeconds;
            state.center = state.blueprint.route[0];
            state.nextStop = state.blueprint.route.Length > 1 ? 1 : 0;
            state.phaseTriggered = false;
            state.introPlayed = false;

            if (SessionRewardedMinibosses.Add(state.blueprint.id))
            {
                game.AddResource(state.blueprint.rewardA, state.blueprint.rewardAAmount);
                game.AddResource(state.blueprint.rewardB, state.blueprint.rewardBAmount);
                game.GainExperience(state.blueprint.experienceReward);
                game.Notify(
                    $"{state.blueprint.displayName} defeated. Named patrol cache claimed for this expedition.");
            }
            else
            {
                game.Notify(
                    $"{state.blueprint.displayName} defeated. Its named patrol cache was already claimed.");
            }
        }

        private static int CountLiveEnemies(HostileState state)
        {
            var count = 0;
            for (var index = 0; index < state.enemies.Length; index++)
            {
                if (state.enemies[index] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasLiveEnemies(HostileState state)
        {
            return CountLiveEnemies(state) > 0;
        }

        private static void RefreshHostileRendererCount(HostileState state)
        {
            var count = 0;
            for (var index = 0; index < state.renderers.Length; index++)
            {
                if (state.renderers[index] != null)
                {
                    count += state.renderers[index].Length;
                }
            }

            state.rendererCount = count;
        }

        private static float HorizontalSqr(Vector3 first, Vector3 second)
        {
            var x = first.x - second.x;
            var z = first.z - second.z;
            return x * x + z * z;
        }

        private void OnDestroy()
        {
            DestroyOwnedMaterial(ref merchantClothMaterial);
            DestroyOwnedMaterial(ref merchantLeatherMaterial);
            DestroyOwnedMaterial(ref merchantCopperMaterial);
        }

        private static void DestroyOwnedMaterial(ref Material material)
        {
            if (material == null)
            {
                return;
            }

            DestroyRuntimeObject(material);
            material = null;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
