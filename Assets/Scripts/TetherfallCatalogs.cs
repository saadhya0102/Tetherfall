using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum EnemyAttackStyle
    {
        Melee,
        Ranged,
        Heavy
    }

    public enum EnemySpecialMove
    {
        None,
        QuickLunge,
        TripleShot,
        Shockwave,
        GroundBurst
    }

    public sealed class EnemyDefinition
    {
        public string id;
        public string displayName;
        public EnemyAttackStyle attackStyle;
        public int maxHealth;
        public int attackDamage;
        public int experienceReward;
        public float moveSpeed;
        public float attackRange;
        public float detectionRange;
        public float attackCooldown;
        public float telegraphSeconds;
        public float projectileSpeed;
        public float scale;
        public Color color;
        public EnemySpecialMove specialMove;
        public float groundBurstRadius;
        public bool ashguardDuringShockwave;
        public float ashguardDamageMultiplier;
        public float ashguardBreakExposeSeconds;
        public float ashguardBreakStaggerSeconds;
    }

    public sealed class EncounterDefinition
    {
        public string id;
        public string displayName;
        public float respawnSeconds;
        public Color campColor;
        public string[] enemyIds;
        public Vector3[] spawnOffsets;
        public string[][] waves;

        public int WaveCount => waves != null && waves.Length > 0 ? waves.Length : 1;

        public string[] GetWave(int index)
        {
            if (waves != null && index >= 0 && index < waves.Length && waves[index] != null)
            {
                return waves[index];
            }

            return enemyIds ?? System.Array.Empty<string>();
        }
    }

    public sealed class EncounterPlacement
    {
        public string encounterId;
        public Vector3 position;
        public string persistenceId;

        public EncounterPlacement(string id, Vector3 worldPosition, string stablePersistenceId = null)
        {
            encounterId = id;
            position = worldPosition;
            persistenceId = stablePersistenceId;
        }
    }

    public static class EnemyCatalog
    {
        private static readonly Dictionary<string, EnemyDefinition> Definitions =
            new Dictionary<string, EnemyDefinition>
            {
                ["enemy.hollow_raider"] = Create(
                    "enemy.hollow_raider", "Hollow Raider", EnemyAttackStyle.Melee,
                    58, 8, 18, 2.45f, 2.25f, 27f, 1.35f, 0.44f, 0f, 0.96f,
                    new Color(0.48f, 0.15f, 0.19f)),
                ["enemy.hollow_scout"] = Create(
                    "enemy.hollow_scout", "Hollow Scout", EnemyAttackStyle.Ranged,
                    44, 7, 21, 2.8f, 10.5f, 31f, 2.15f, 0.72f, 15f, 0.86f,
                    new Color(0.58f, 0.23f, 0.28f), EnemySpecialMove.TripleShot),
                ["enemy.mossglass_stalker"] = Create(
                    "enemy.mossglass_stalker", "Mossglass Stalker", EnemyAttackStyle.Melee,
                    42, 6, 17, 3.85f, 1.9f, 26f, 1.05f, 0.34f, 0f, 0.72f,
                    new Color(0.28f, 0.46f, 0.35f), EnemySpecialMove.QuickLunge),
                ["enemy.fen_spitter"] = Create(
                    "enemy.fen_spitter", "Fen Spitter", EnemyAttackStyle.Ranged,
                    52, 10, 24, 2.15f, 12f, 34f, 2.45f, 0.85f, 12f, 0.92f,
                    new Color(0.22f, 0.47f, 0.5f), EnemySpecialMove.TripleShot),
                ["enemy.fen_siltcaller"] = Create(
                    "enemy.fen_siltcaller", "Fen Siltcaller", EnemyAttackStyle.Ranged,
                    64, 11, 29, 2.35f, 10.5f, 33f, 2.65f, 0.9f, 0f, 0.96f,
                    new Color(0.2f, 0.68f, 0.7f), EnemySpecialMove.GroundBurst, 2.8f),
                ["enemy.relay_breaker"] = Create(
                    "enemy.relay_breaker", "Relay Breaker", EnemyAttackStyle.Heavy,
                    112, 17, 36, 1.55f, 2.9f, 28f, 2.75f, 1.08f, 0f, 1.35f,
                    new Color(0.45f, 0.2f, 0.17f), EnemySpecialMove.Shockwave),
                ["enemy.cinder_echo"] = Create(
                    "enemy.cinder_echo", "Cinder Echo", EnemyAttackStyle.Melee,
                    48, 9, 22, 3.35f, 2.05f, 29f, 1.18f, 0.4f, 0f, 0.82f,
                    new Color(0.68f, 0.24f, 0.17f), EnemySpecialMove.QuickLunge),
                ["enemy.ashjaw_brute"] = Create(
                    "enemy.ashjaw_brute", "Ashjaw Brute", EnemyAttackStyle.Heavy,
                    146, 21, 44, 1.35f, 3.2f, 30f, 3.05f, 1.22f, 0f, 1.52f,
                    new Color(0.54f, 0.15f, 0.09f), EnemySpecialMove.Shockwave),
                ["enemy.vault_wisp"] = Create(
                    "enemy.vault_wisp", "Vault Wisp", EnemyAttackStyle.Ranged,
                    38, 8, 18, 2.7f, 11f, 30f, 1.9f, 0.62f, 17f, 0.65f,
                    new Color(0.38f, 0.29f, 0.67f), EnemySpecialMove.TripleShot),
                ["enemy.wild.alderwake.mossbell_strider"] = Create(
                    "enemy.wild.alderwake.mossbell_strider", "Mossbell Strider", EnemyAttackStyle.Heavy,
                    76, 8, 24, 2.55f, 2.45f, 24f, 1.7f, 0.62f, 0f, 0.92f,
                    new Color(0.32f, 0.62f, 0.38f), EnemySpecialMove.Shockwave),
                ["enemy.wild.alderwake.copperquill_nester"] = Create(
                    "enemy.wild.alderwake.copperquill_nester", "Copperquill Nester", EnemyAttackStyle.Ranged,
                    52, 7, 22, 3.35f, 9.5f, 28f, 2.2f, 0.72f, 15f, 0.78f,
                    new Color(0.7f, 0.42f, 0.2f), EnemySpecialMove.TripleShot),
                ["enemy.wild.morrowfen.tideveil_skimmer"] = Create(
                    "enemy.wild.morrowfen.tideveil_skimmer", "Tideveil Skimmer", EnemyAttackStyle.Ranged,
                    64, 7, 25, 2.8f, 8.5f, 27f, 2.55f, 0.86f, 12f, 0.84f,
                    new Color(0.26f, 0.7f, 0.67f), EnemySpecialMove.GroundBurst, 2.25f),
                ["enemy.wild.morrowfen.gloamreed_lanternback"] = Create(
                    "enemy.wild.morrowfen.gloamreed_lanternback", "Gloamreed Lanternback", EnemyAttackStyle.Ranged,
                    82, 9, 29, 2.25f, 10f, 29f, 2.7f, 0.92f, 11f, 0.96f,
                    new Color(0.4f, 0.47f, 0.65f), EnemySpecialMove.GroundBurst, 2.7f),
                ["enemy.wild.cinderwold.emberhorn_burrower"] = Create(
                    "enemy.wild.cinderwold.emberhorn_burrower", "Emberhorn Burrower", EnemyAttackStyle.Heavy,
                    112, 13, 36, 1.9f, 2.8f, 27f, 2.5f, 1.02f, 0f, 1.08f,
                    new Color(0.7f, 0.3f, 0.12f), EnemySpecialMove.Shockwave),
                ["enemy.wild.cinderwold.prismtail_kitelizard"] = Create(
                    "enemy.wild.cinderwold.prismtail_kitelizard", "Prismtail Kitelizard", EnemyAttackStyle.Melee,
                    68, 11, 31, 3.75f, 2.15f, 30f, 1.2f, 0.42f, 0f, 0.82f,
                    new Color(0.76f, 0.43f, 0.82f), EnemySpecialMove.QuickLunge),
                ["enemy.elite.mossfang_matriarch"] = Create(
                    "enemy.elite.mossfang_matriarch", "Mossfang Matriarch", EnemyAttackStyle.Melee,
                    260, 19, 92, 3.9f, 2.8f, 36f, 1.02f, 0.5f, 0f, 1.46f,
                    new Color(0.19f, 0.63f, 0.32f), EnemySpecialMove.QuickLunge),
                ["enemy.elite.siltglass_heron"] = Create(
                    "enemy.elite.siltglass_heron", "Siltglass Heron", EnemyAttackStyle.Ranged,
                    230, 17, 96, 3.15f, 14f, 39f, 1.52f, 0.84f, 21f, 1.28f,
                    new Color(0.22f, 0.72f, 0.78f), EnemySpecialMove.TripleShot),
                ["enemy.elite.cindermaw"] = Create(
                    "enemy.elite.cindermaw", "Cindermaw", EnemyAttackStyle.Heavy,
                    330, 25, 118, 1.75f, 3.6f, 37f, 2.45f, 1.06f, 0f, 1.68f,
                    new Color(0.9f, 0.21f, 0.09f), EnemySpecialMove.Shockwave,
                    ashguardDuringShockwave: true,
                    ashguardDamageMultiplier: 0.68f,
                    ashguardBreakExposeSeconds: 5.2f,
                    ashguardBreakStaggerSeconds: 0.62f),
                ["enemy.elite.mirewarden"] = Create(
                    "enemy.elite.mirewarden", "Mirewarden", EnemyAttackStyle.Heavy,
                    290, 23, 108, 2.1f, 3.4f, 35f, 2.2f, 1.02f, 0f, 1.5f,
                    new Color(0.27f, 0.35f, 0.64f), EnemySpecialMove.Shockwave)
            };

        public static EnemyDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id) || !Definitions.TryGetValue(id, out var definition))
            {
                return Definitions["enemy.hollow_raider"];
            }

            return definition;
        }

        private static EnemyDefinition Create(
            string id,
            string displayName,
            EnemyAttackStyle style,
            int health,
            int damage,
            int experience,
            float speed,
            float range,
            float detection,
            float cooldown,
            float telegraph,
            float projectileSpeed,
            float scale,
            Color color,
            EnemySpecialMove specialMove = EnemySpecialMove.None,
            float groundBurstRadius = 0f,
            bool ashguardDuringShockwave = false,
            float ashguardDamageMultiplier = 1f,
            float ashguardBreakExposeSeconds = 0f,
            float ashguardBreakStaggerSeconds = 0f)
        {
            return new EnemyDefinition
            {
                id = id,
                displayName = displayName,
                attackStyle = style,
                maxHealth = health,
                attackDamage = damage,
                experienceReward = experience,
                moveSpeed = speed,
                attackRange = range,
                detectionRange = detection,
                attackCooldown = cooldown,
                telegraphSeconds = telegraph,
                projectileSpeed = projectileSpeed,
                scale = scale,
                color = color,
                specialMove = specialMove,
                groundBurstRadius = groundBurstRadius,
                ashguardDuringShockwave = ashguardDuringShockwave,
                ashguardDamageMultiplier = ashguardDamageMultiplier,
                ashguardBreakExposeSeconds = ashguardBreakExposeSeconds,
                ashguardBreakStaggerSeconds = ashguardBreakStaggerSeconds
            };
        }
    }

    public static class EncounterCatalog
    {
        private static readonly Dictionary<string, EncounterDefinition> Definitions =
            new Dictionary<string, EncounterDefinition>
            {
                ["encounter.alderwake.hollow_reed"] = Create(
                    "encounter.alderwake.hollow_reed", "Hollow Reed Camp", 48f,
                    new Color(0.5f, 0.15f, 0.19f), 4,
                    "enemy.hollow_raider", "enemy.hollow_raider", "enemy.hollow_scout", "enemy.mossglass_stalker"),
                ["encounter.alderwake.mossglass"] = Create(
                    "encounter.alderwake.mossglass", "Mossglass Ambush", 42f,
                    new Color(0.3f, 0.22f, 0.31f), 5,
                    "enemy.mossglass_stalker", "enemy.mossglass_stalker", "enemy.mossglass_stalker",
                    "enemy.hollow_scout", "enemy.hollow_raider"),
                ["encounter.alderwake.ridge"] = CreatePhased(
                    "encounter.alderwake.ridge", "Highbank Toll", 55f,
                    new Color(0.42f, 0.2f, 0.18f),
                    new[] { "enemy.hollow_raider", "enemy.hollow_raider", "enemy.hollow_scout" },
                    new[] { "enemy.hollow_scout", "enemy.relay_breaker" }),
                ["encounter.morrowfen.sunken_watch"] = Create(
                    "encounter.morrowfen.sunken_watch", "Sunken Watch Camp", 50f,
                    new Color(0.25f, 0.18f, 0.39f), 5,
                    "enemy.fen_spitter", "enemy.fen_spitter", "enemy.hollow_raider",
                    "enemy.mossglass_stalker", "enemy.mossglass_stalker"),
                ["encounter.morrowfen.fen_tithe"] = CreatePhased(
                    "encounter.morrowfen.fen_tithe", "Fen Tithe Camp", 52f,
                    new Color(0.23f, 0.18f, 0.42f),
                    new[] { "enemy.fen_spitter", "enemy.hollow_raider", "enemy.mossglass_stalker" },
                    new[] { "enemy.fen_spitter", "enemy.fen_siltcaller", "enemy.relay_breaker" }),
                ["encounter.morrowfen.drowned_choir"] = Create(
                    "encounter.morrowfen.drowned_choir", "Drowned Choir Patrol", 45f,
                    new Color(0.17f, 0.31f, 0.42f), 6,
                    "enemy.vault_wisp", "enemy.vault_wisp", "enemy.fen_spitter",
                    "enemy.fen_spitter", "enemy.mossglass_stalker", "enemy.mossglass_stalker"),
                ["encounter.cinderwold.ashjaw"] = CreatePhased(
                    "encounter.cinderwold.ashjaw", "Ashjaw Encampment", 58f,
                    new Color(0.55f, 0.18f, 0.1f),
                    new[] { "enemy.cinder_echo", "enemy.cinder_echo", "enemy.relay_breaker" },
                    new[] { "enemy.cinder_echo", "enemy.relay_breaker", "enemy.ashjaw_brute" }),
                ["encounter.cinderwold.relay_breakers"] = Create(
                    "encounter.cinderwold.relay_breakers", "Relay Breaker Camp", 54f,
                    new Color(0.51f, 0.18f, 0.12f), 5,
                    "enemy.cinder_echo", "enemy.cinder_echo", "enemy.hollow_scout",
                    "enemy.relay_breaker", "enemy.relay_breaker"),
                ["encounter.cinderwold.ember_shelf"] = Create(
                    "encounter.cinderwold.ember_shelf", "Ember Shelf Patrol", 46f,
                    new Color(0.48f, 0.17f, 0.11f), 6,
                    "enemy.cinder_echo", "enemy.cinder_echo", "enemy.cinder_echo",
                    "enemy.cinder_echo", "enemy.relay_breaker", "enemy.vault_wisp"),
                ["encounter.wild.alderwake.mossbell_striders"] = Create(
                    "encounter.wild.alderwake.mossbell_striders", "Rainroot Mossbell Pair", 82f,
                    new Color(0.3f, 0.58f, 0.36f), 2,
                    "enemy.wild.alderwake.mossbell_strider",
                    "enemy.wild.alderwake.mossbell_strider"),
                ["encounter.wild.alderwake.copperquill_nesters"] = Create(
                    "encounter.wild.alderwake.copperquill_nesters", "High-Bough Copperquills", 78f,
                    new Color(0.65f, 0.39f, 0.2f), 2,
                    "enemy.wild.alderwake.copperquill_nester",
                    "enemy.wild.alderwake.copperquill_nester"),
                ["encounter.wild.morrowfen.tideveil_skimmers"] = Create(
                    "encounter.wild.morrowfen.tideveil_skimmers", "Quiet-Channel Tideveils", 86f,
                    new Color(0.24f, 0.64f, 0.64f), 2,
                    "enemy.wild.morrowfen.tideveil_skimmer",
                    "enemy.wild.morrowfen.tideveil_skimmer"),
                ["encounter.wild.morrowfen.gloamreed_lanternbacks"] = Create(
                    "encounter.wild.morrowfen.gloamreed_lanternbacks", "Low-Lantern Gloamreeds", 90f,
                    new Color(0.38f, 0.43f, 0.62f), 2,
                    "enemy.wild.morrowfen.gloamreed_lanternback",
                    "enemy.wild.morrowfen.gloamreed_lanternback"),
                ["encounter.wild.cinderwold.emberhorn_burrowers"] = Create(
                    "encounter.wild.cinderwold.emberhorn_burrowers", "Cooling-Shelf Emberhorns", 94f,
                    new Color(0.66f, 0.28f, 0.12f), 2,
                    "enemy.wild.cinderwold.emberhorn_burrower",
                    "enemy.wild.cinderwold.emberhorn_burrower"),
                ["encounter.wild.cinderwold.prismtail_kitelizards"] = Create(
                    "encounter.wild.cinderwold.prismtail_kitelizards", "Glasswind Prismtails", 88f,
                    new Color(0.7f, 0.4f, 0.78f), 2,
                    "enemy.wild.cinderwold.prismtail_kitelizard",
                    "enemy.wild.cinderwold.prismtail_kitelizard")
            };

        public static EncounterDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id) || !Definitions.TryGetValue(id, out var definition))
            {
                return Definitions["encounter.alderwake.hollow_reed"];
            }

            return definition;
        }

        public static IReadOnlyList<EncounterPlacement> ExtendedPlacements { get; } =
            new List<EncounterPlacement>
            {
                new EncounterPlacement("encounter.alderwake.ridge", new Vector3(34f, 0f, 91f)),
                new EncounterPlacement("encounter.alderwake.mossglass", new Vector3(-42f, 0f, -92f)),
                new EncounterPlacement("encounter.alderwake.hollow_reed", new Vector3(66f, 0f, -72f)),
                new EncounterPlacement("encounter.alderwake.ridge", new Vector3(-64f, 0f, 78f)),
                new EncounterPlacement("encounter.morrowfen.drowned_choir", new Vector3(151f, 0f, 88f)),
                new EncounterPlacement("encounter.morrowfen.fen_tithe", new Vector3(192f, 0f, -76f)),
                new EncounterPlacement("encounter.morrowfen.sunken_watch", new Vector3(218f, 0f, -30f)),
                new EncounterPlacement("encounter.morrowfen.drowned_choir", new Vector3(97f, 0f, -84f)),
                new EncounterPlacement("encounter.cinderwold.ember_shelf", new Vector3(-148f, 0f, 89f)),
                new EncounterPlacement("encounter.cinderwold.relay_breakers", new Vector3(-201f, 0f, -79f)),
                new EncounterPlacement(
                    "encounter.cinderwold.ashjaw",
                    new Vector3(-208f, 0f, -43f),
                    "encounter.cinderwold.ashjaw@-208:-43"),
                new EncounterPlacement("encounter.cinderwold.ember_shelf", new Vector3(-92f, 0f, 86f)),
                new EncounterPlacement(
                    "encounter.wild.alderwake.mossbell_striders",
                    new Vector3(-60f, 0f, -50f),
                    "encounter.wild.alderwake.mossbell_striders@-60:-50"),
                new EncounterPlacement(
                    "encounter.wild.alderwake.copperquill_nesters",
                    new Vector3(65f, 0f, -20f),
                    "encounter.wild.alderwake.copperquill_nesters@65:-20"),
                new EncounterPlacement(
                    "encounter.wild.morrowfen.tideveil_skimmers",
                    new Vector3(86f, 0f, 78f),
                    "encounter.wild.morrowfen.tideveil_skimmers@86:78"),
                new EncounterPlacement(
                    "encounter.wild.morrowfen.gloamreed_lanternbacks",
                    new Vector3(235f, 0f, -91f),
                    "encounter.wild.morrowfen.gloamreed_lanternbacks@235:-91"),
                new EncounterPlacement(
                    "encounter.wild.cinderwold.emberhorn_burrowers",
                    new Vector3(-232f, 0f, 32f),
                    "encounter.wild.cinderwold.emberhorn_burrowers@-232:32"),
                new EncounterPlacement(
                    "encounter.wild.cinderwold.prismtail_kitelizards",
                    new Vector3(-112f, 0f, -106f),
                    "encounter.wild.cinderwold.prismtail_kitelizards@-112:-106")
            };

        private static EncounterDefinition Create(
            string id,
            string displayName,
            float respawnSeconds,
            Color color,
            int count,
            params string[] enemies)
        {
            var offsets = new Vector3[count];
            for (var index = 0; index < count; index++)
            {
                var angle = index * Mathf.PI * 2f / count;
                var radius = index % 2 == 0 ? 4.2f : 6.1f;
                offsets[index] = new Vector3(Mathf.Cos(angle) * radius, 0.15f, Mathf.Sin(angle) * radius);
            }

            return new EncounterDefinition
            {
                id = id,
                displayName = displayName,
                respawnSeconds = respawnSeconds,
                campColor = color,
                enemyIds = enemies,
                spawnOffsets = offsets,
                waves = new[] { enemies }
            };
        }

        private static EncounterDefinition CreatePhased(
            string id,
            string displayName,
            float respawnSeconds,
            Color color,
            params string[][] waves)
        {
            var largestWave = 1;
            foreach (var wave in waves)
            {
                if (wave != null)
                {
                    largestWave = Mathf.Max(largestWave, wave.Length);
                }
            }

            var offsets = new Vector3[largestWave];
            for (var index = 0; index < largestWave; index++)
            {
                var angle = index * Mathf.PI * 2f / largestWave;
                var radius = index % 2 == 0 ? 4.2f : 6.1f;
                offsets[index] = new Vector3(Mathf.Cos(angle) * radius, 0.15f, Mathf.Sin(angle) * radius);
            }

            return new EncounterDefinition
            {
                id = id,
                displayName = displayName,
                respawnSeconds = respawnSeconds,
                campColor = color,
                enemyIds = waves != null && waves.Length > 0 ? waves[0] : System.Array.Empty<string>(),
                spawnOffsets = offsets,
                waves = waves
            };
        }
    }

    public sealed class RouteEventDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public Vector3 position;
        public Color signalColor;
        public string[][] waves;
        public int favorReward;
        public ResourceKind resourceReward;
        public int resourceAmount;
        public string requiredRestorationSiteId;
        public int requiredRestorationTier;
        public string completionRestorationSiteId;
        public int completionRestorationTier;
        public bool contributesToSignalbreak;
        public bool requiresSignalbreakCompletion;
        public bool repeatable;
        public float cooldownSeconds;
        public int repeatFavorReward;
        public int repeatResourceAmount;
        public int repeatExperienceReward;
    }

    public static class RouteEventCatalog
    {
        public static IReadOnlyList<RouteEventDefinition> All { get; } =
            new List<RouteEventDefinition>
            {
                new RouteEventDefinition
                {
                    id = "event.alderwake.northwake_signal",
                    displayName = "Northwake Signal Break",
                    description = "A damaged relay is pulling hollow raiders toward the observatory.",
                    position = new Vector3(18f, 0f, 103f),
                    signalColor = new Color(0.36f, 0.87f, 0.7f),
                    waves = new[]
                    {
                        new[] { "enemy.hollow_raider", "enemy.mossglass_stalker", "enemy.hollow_scout" },
                        new[] { "enemy.hollow_raider", "enemy.hollow_scout", "enemy.relay_breaker" }
                    },
                    favorReward = 18,
                    resourceReward = ResourceKind.Resin,
                    resourceAmount = 3,
                    contributesToSignalbreak = true,
                    repeatable = true,
                    cooldownSeconds = 600f,
                    repeatFavorReward = 10,
                    repeatResourceAmount = 2,
                    repeatExperienceReward = 35
                },
                new RouteEventDefinition
                {
                    id = "event.morrowfen.lantern_breach",
                    displayName = "Fenlight Lantern Breach",
                    description = "A drowned lantern route needs a Wayfinder before the fen swallows it.",
                    position = new Vector3(198f, 0f, 76f),
                    signalColor = new Color(0.3f, 0.78f, 0.86f),
                    waves = new[]
                    {
                        new[] { "enemy.fen_spitter", "enemy.mossglass_stalker", "enemy.vault_wisp" },
                        new[] { "enemy.fen_spitter", "enemy.fen_spitter", "enemy.relay_breaker", "enemy.vault_wisp" }
                    },
                    favorReward = 24,
                    resourceReward = ResourceKind.Herbs,
                    resourceAmount = 4,
                    requiredRestorationSiteId = RestorationCatalog.FenlightCausewayId,
                    requiredRestorationTier = 1,
                    completionRestorationSiteId = RestorationCatalog.FenlightCausewayId,
                    completionRestorationTier = 2,
                    contributesToSignalbreak = true,
                    repeatable = true,
                    cooldownSeconds = 720f,
                    repeatFavorReward = 13,
                    repeatResourceAmount = 2,
                    repeatExperienceReward = 40
                },
                new RouteEventDefinition
                {
                    id = "event.cinderwold.emberfall",
                    displayName = "Ember Relay Breach",
                    description = "Cinder echoes are tearing open the relay above the ash shelf.",
                    position = new Vector3(-205f, 0f, 76f),
                    signalColor = new Color(0.94f, 0.43f, 0.25f),
                    waves = new[]
                    {
                        new[] { "enemy.cinder_echo", "enemy.cinder_echo", "enemy.relay_breaker" },
                        new[] { "enemy.cinder_echo", "enemy.ashjaw_brute", "enemy.vault_wisp", "enemy.relay_breaker" }
                    },
                    favorReward = 30,
                    resourceReward = ResourceKind.Ore,
                    resourceAmount = 3,
                    contributesToSignalbreak = true,
                    repeatable = true,
                    cooldownSeconds = 840f,
                    repeatFavorReward = 16,
                    repeatResourceAmount = 2,
                    repeatExperienceReward = 45
                },
                new RouteEventDefinition
                {
                    id = "event.alderwake.southroot_watch",
                    displayName = "Southroot Watchfire",
                    description = "A lowland route chime has started calling raiders toward the old southroot marker.",
                    position = new Vector3(-19f, 0f, -118f),
                    signalColor = new Color(0.54f, 0.82f, 0.54f),
                    waves = new[]
                    {
                        new[] { "enemy.mossglass_stalker", "enemy.hollow_raider", "enemy.hollow_scout" },
                        new[] { "enemy.mossglass_stalker", "enemy.mossglass_stalker", "enemy.relay_breaker" }
                    },
                    favorReward = 20,
                    resourceReward = ResourceKind.Fiber,
                    resourceAmount = 3,
                    requiresSignalbreakCompletion = true,
                    repeatable = true,
                    cooldownSeconds = 660f,
                    repeatFavorReward = 11,
                    repeatResourceAmount = 2,
                    repeatExperienceReward = 36
                },
                new RouteEventDefinition
                {
                    id = "event.morrowfen.siltstep_vigil",
                    displayName = "Siltstep Vigil",
                    description = "Fen-static is gathering around a drowned survey shelf above the siltstep crossing.",
                    position = new Vector3(212f, 0f, -44f),
                    signalColor = new Color(0.28f, 0.74f, 0.8f),
                    waves = new[]
                    {
                        new[] { "enemy.fen_spitter", "enemy.mossglass_stalker", "enemy.fen_siltcaller" },
                        new[] { "enemy.fen_spitter", "enemy.fen_siltcaller", "enemy.relay_breaker", "enemy.vault_wisp" }
                    },
                    favorReward = 25,
                    resourceReward = ResourceKind.Herbs,
                    resourceAmount = 3,
                    requiresSignalbreakCompletion = true,
                    repeatable = true,
                    cooldownSeconds = 780f,
                    repeatFavorReward = 14,
                    repeatResourceAmount = 2,
                    repeatExperienceReward = 42
                },
                new RouteEventDefinition
                {
                    id = "event.cinderwold.ashgate_watch",
                    displayName = "Ashgate Signal Watch",
                    description = "A scorched waymark is venting false relay calls into Blackravine's ash winds.",
                    position = new Vector3(-118f, 0f, 88f),
                    signalColor = new Color(0.94f, 0.36f, 0.2f),
                    waves = new[]
                    {
                        new[] { "enemy.cinder_echo", "enemy.cinder_echo", "enemy.vault_wisp" },
                        new[] { "enemy.cinder_echo", "enemy.ashjaw_brute", "enemy.relay_breaker", "enemy.vault_wisp" }
                    },
                    favorReward = 28,
                    resourceReward = ResourceKind.Ore,
                    resourceAmount = 3,
                    requiresSignalbreakCompletion = true,
                    repeatable = true,
                    cooldownSeconds = 900f,
                    repeatFavorReward = 16,
                    repeatResourceAmount = 2,
                    repeatExperienceReward = 46
                }
            };
    }

    public sealed class FieldCacheDefinition
    {
        public string id;
        public string displayName;
        public string inscription;
        public string regionId;
        public Vector3 position;
        public Color color;
        public ResourceKind resourceReward;
        public int resourceAmount;
        public int favorReward;
    }

    public static class FieldCacheCatalog
    {
        public static IReadOnlyList<FieldCacheDefinition> All { get; } =
            new List<FieldCacheDefinition>
            {
                Create(
                    "cache.alderwake.rainline",
                    "Rainline Field Cache",
                    "A soaked route slate reads: 'Shelter shared becomes a road.'",
                    "Alderwake Basin",
                    new Vector3(-70f, 0f, 66f),
                    new Color(0.42f, 0.82f, 0.65f),
                    ResourceKind.Resin,
                    2,
                    7),
                Create(
                    "cache.alderwake.southroot",
                    "Southroot Field Cache",
                    "A brass disc points toward a hidden stone in the roots.",
                    "Alderwake Basin",
                    new Vector3(4f, 0f, -104f),
                    new Color(0.46f, 0.76f, 0.52f),
                    ResourceKind.Fiber,
                    3,
                    8),
                Create(
                    "cache.alderwake.highbank",
                    "Highbank Field Cache",
                    "A route ledger warns that every crossing has two shores.",
                    "Alderwake Basin",
                    new Vector3(43f, 0f, 83f),
                    new Color(0.53f, 0.82f, 0.62f),
                    ResourceKind.Timber,
                    3,
                    9),
                Create(
                    "cache.morrowfen.glasswater",
                    "Glasswater Field Cache",
                    "The wax-sealed map marks a dry line through the reeds.",
                    "Morrowfen",
                    new Vector3(137f, 0f, -76f),
                    new Color(0.3f, 0.78f, 0.88f),
                    ResourceKind.Herbs,
                    3,
                    10),
                Create(
                    "cache.morrowfen.fenlight",
                    "Fenlight Field Cache",
                    "A lantern keeper records the names of those brought home.",
                    "Morrowfen",
                    new Vector3(191f, 0f, 106f),
                    new Color(0.34f, 0.78f, 0.93f),
                    ResourceKind.Chimesalt,
                    2,
                    12),
                Create(
                    "cache.morrowfen.siltstep",
                    "Siltstep Field Cache",
                    "A river-stained charm hums beside the old causeway.",
                    "Morrowfen",
                    new Vector3(234f, 0f, -24f),
                    new Color(0.34f, 0.67f, 0.82f),
                    ResourceKind.Fiber,
                    3,
                    10),
                Create(
                    "cache.cinderwold.embershelf",
                    "Embershelf Field Cache",
                    "A scorched letter says: 'Pack the ember; leave the wildfire.'",
                    "Cinderwold Verge",
                    new Vector3(-158f, 0f, 80f),
                    new Color(0.95f, 0.43f, 0.24f),
                    ResourceKind.Ore,
                    2,
                    12),
                Create(
                    "cache.cinderwold.blackravine",
                    "Blackravine Field Cache",
                    "A salvage tag promises the road will remember honest hands.",
                    "Cinderwold Verge",
                    new Vector3(-226f, 0f, -78f),
                    new Color(0.88f, 0.34f, 0.2f),
                    ResourceKind.Resin,
                    3,
                    11),
                Create(
                    "cache.cinderwold.ashgate",
                    "Ashgate Field Cache",
                    "A route pin warms in the hand beside the broken relay.",
                    "Cinderwold Verge",
                    new Vector3(-98f, 0f, 101f),
                    new Color(0.9f, 0.51f, 0.26f),
                    ResourceKind.Ore,
                    3,
                    13)
            };

        private static FieldCacheDefinition Create(
            string id,
            string displayName,
            string inscription,
            string regionId,
            Vector3 position,
            Color color,
            ResourceKind resourceReward,
            int resourceAmount,
            int favorReward)
        {
            return new FieldCacheDefinition
            {
                id = id,
                displayName = displayName,
                inscription = inscription,
                regionId = regionId,
                position = position,
                color = color,
                resourceReward = resourceReward,
                resourceAmount = resourceAmount,
                favorReward = favorReward
            };
        }
    }

    public static class RestorationCatalog
    {
        public const string FenlightCausewayId = "restoration.morrowfen.fenlight_causeway";

        public static IReadOnlyList<RestorationSiteDefinition> All { get; } =
            new List<RestorationSiteDefinition>
            {
                new RestorationSiteDefinition
                {
                    id = FenlightCausewayId,
                    displayName = "Fenlight Causeway",
                    description = "A drowned lantern route waits for its chain, its defenders, and a final raised span.",
                    position = new Vector3(183f, 0f, 103f),
                    accentColor = new Color(0.3f, 0.76f, 0.82f),
                    maxTier = 3,
                    tiers = new List<RestorationTierDefinition>
                    {
                        new RestorationTierDefinition
                        {
                            tier = 1,
                            title = "Rehang the lantern chain",
                            description = "Set three low lanterns along the drowned mooring.",
                            cost = Costs(
                                new ResourceCost(ResourceKind.Fiber, 4),
                                new ResourceCost(ResourceKind.Resin, 3),
                                new ResourceCost(ResourceKind.Chimesalt, 2)),
                            experienceReward = 70,
                            favorReward = 10
                        },
                        new RestorationTierDefinition
                        {
                            tier = 2,
                            title = "Hold the lantern breach",
                            description = "Defend the Fenlight signal while the route light is exposed.",
                            isManual = false,
                            requiredRouteEventId = "event.morrowfen.lantern_breach",
                            experienceReward = 85,
                            favorReward = 12
                        },
                        new RestorationTierDefinition
                        {
                            tier = 3,
                            title = "Raise the causeway",
                            description = "Build a copper-railed walkway above the fen water.",
                            cost = Costs(
                                new ResourceCost(ResourceKind.Timber, 4),
                                new ResourceCost(ResourceKind.Fiber, 3),
                                new ResourceCost(ResourceKind.Ore, 2)),
                            experienceReward = 100,
                            favorReward = 16,
                            rewardEquipmentId = "charm.mirelight"
                        }
                    }
                }
            };

        public static RestorationSiteDefinition Get(string id)
        {
            foreach (var definition in All)
            {
                if (definition.id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        private static List<ResourceCost> Costs(params ResourceCost[] costs)
        {
            return new List<ResourceCost>(costs);
        }
    }

    public static class ProvisioningCatalog
    {
        public const string BrightbrothId = "meal.brightbroth";

        public static IReadOnlyList<MealDefinition> Meals { get; } =
            new List<MealDefinition>
            {
                new MealDefinition
                {
                    id = BrightbrothId,
                    displayName = "Brightbroth",
                    description = "Restores vitality and a strong reserve of field stamina.",
                    minimumCampTier = 1,
                    ingredients = Costs(
                        new ResourceCost(ResourceKind.Herbs, 1),
                        new ResourceCost(ResourceKind.Resin, 1)),
                    vitalityRestore = 42,
                    staminaRestore = 45f,
                    effect = MealEffect.Restore
                },
                new MealDefinition
                {
                    id = "meal.reedrunner_tonic",
                    displayName = "Reedrunner Tonic",
                    description = "Restores stamina and grants a short burst of route speed.",
                    minimumCampTier = 1,
                    recipeUnlockFavorPrice = 18,
                    ingredients = Costs(
                        new ResourceCost(ResourceKind.Herbs, 2),
                        new ResourceCost(ResourceKind.Fiber, 1)),
                    vitalityRestore = 12,
                    staminaRestore = 72f,
                    effect = MealEffect.Swift,
                    effectDurationSeconds = 18f
                },
                new MealDefinition
                {
                    id = "meal.cindersalt_stew",
                    displayName = "Cindersalt Stew",
                    description = "Restores vitality and grants a temporary reinforced guard.",
                    minimumCampTier = 2,
                    recipeUnlockFavorPrice = 26,
                    ingredients = Costs(
                        new ResourceCost(ResourceKind.Herbs, 1),
                        new ResourceCost(ResourceKind.Ore, 1),
                        new ResourceCost(ResourceKind.Chimesalt, 1)),
                    vitalityRestore = 64,
                    staminaRestore = 32f,
                    effect = MealEffect.Ward,
                    effectDurationSeconds = 24f
                }
            };

        public static IReadOnlyList<VendorProfileDefinition> Vendors { get; } =
            new List<VendorProfileDefinition>
            {
                new VendorProfileDefinition
                {
                    id = "vendor.alderwake.broker",
                    displayName = "Marn Kest's route stock",
                    serviceDescription = "Route charms and basic materials for new Wayfinders.",
                    equipmentIds = new List<string> { "charm.anchor", "charm.galespring" },
                    supplies = new List<VendorSupplyDefinition>
                    {
                        Supply(ResourceKind.Herbs, 2, 8),
                        Supply(ResourceKind.Fiber, 2, 8),
                        Supply(ResourceKind.Resin, 2, 9)
                    }
                },
                new VendorProfileDefinition
                {
                    id = "vendor.alderwake.hearthmarket",
                    displayName = "Kite Rowe's commons counter",
                    serviceDescription = "Hearthmarket travel goods, route charms, and stove-ready provisions.",
                    equipmentIds = new List<string> { "charm.anchor", "charm.galespring" },
                    recipeIds = new List<string> { "meal.reedrunner_tonic" },
                    supplies = new List<VendorSupplyDefinition>
                    {
                        Supply(ResourceKind.Herbs, 3, 10),
                        Supply(ResourceKind.Fiber, 3, 10),
                        Supply(ResourceKind.Resin, 2, 9)
                    }
                },
                new VendorProfileDefinition
                {
                    id = "vendor.morrowfen.provisioner",
                    displayName = "Nemi Rill's fen pantry",
                    serviceDescription = "Fen ingredients and a fast-moving tonic for reed routes.",
                    recipeIds = new List<string> { "meal.reedrunner_tonic" },
                    supplies = new List<VendorSupplyDefinition>
                    {
                        Supply(ResourceKind.Herbs, 3, 10),
                        Supply(ResourceKind.Fiber, 3, 10),
                        Supply(ResourceKind.Chimesalt, 1, 12)
                    }
                },
                new VendorProfileDefinition
                {
                    id = "vendor.veyrwater.ferrier",
                    displayName = "Luma Sedge's ferry stores",
                    serviceDescription = "Fog lantern supplies and portable meals for wet-channel travel.",
                    recipeIds = new List<string> { "meal.reedrunner_tonic", "meal.cindersalt_stew" },
                    supplies = new List<VendorSupplyDefinition>
                    {
                        Supply(ResourceKind.Herbs, 3, 10),
                        Supply(ResourceKind.Resin, 3, 11),
                        Supply(ResourceKind.Chimesalt, 2, 14)
                    }
                },
                new VendorProfileDefinition
                {
                    id = "vendor.cinderwold.salvager",
                    displayName = "Daro Quell's salvage table",
                    serviceDescription = "Ashland salvage and a stew for holding ground under pressure.",
                    recipeIds = new List<string> { "meal.cindersalt_stew" },
                    supplies = new List<VendorSupplyDefinition>
                    {
                        Supply(ResourceKind.Ore, 2, 14),
                        Supply(ResourceKind.Resin, 2, 10),
                        Supply(ResourceKind.Chimesalt, 1, 13)
                    }
                }
            };

        public static IReadOnlyList<CampContractDefinition> CampContracts { get; } =
            new List<CampContractDefinition>
            {
                new CampContractDefinition
                {
                    id = "contract.fen_provisions",
                    title = "Fen Provision Parcel",
                    description = "Pack a dry delivery for the lantern keepers.",
                    minimumCampTier = 2,
                    delivery = Costs(
                        new ResourceCost(ResourceKind.Herbs, 5),
                        new ResourceCost(ResourceKind.Fiber, 4)),
                    favorReward = 22
                },
                new CampContractDefinition
                {
                    id = "contract.ash_salvage",
                    title = "Ash Salvage Tally",
                    description = "Send usable routework metal back through Alderwake.",
                    minimumCampTier = 2,
                    delivery = Costs(
                        new ResourceCost(ResourceKind.Ore, 3),
                        new ResourceCost(ResourceKind.Resin, 2)),
                    favorReward = 26
                }
            };

        public static MealDefinition GetMeal(string id)
        {
            foreach (var meal in Meals)
            {
                if (meal.id == id)
                {
                    return meal;
                }
            }

            return null;
        }

        public static VendorProfileDefinition GetVendor(string id)
        {
            foreach (var vendor in Vendors)
            {
                if (vendor.id == id)
                {
                    return vendor;
                }
            }

            return null;
        }

        private static List<ResourceCost> Costs(params ResourceCost[] costs)
        {
            return new List<ResourceCost>(costs);
        }

        private static VendorSupplyDefinition Supply(ResourceKind kind, int amount, int favorPrice)
        {
            return new VendorSupplyDefinition
            {
                kind = kind,
                amount = amount,
                favorPrice = favorPrice
            };
        }
    }

    public static class CinderwoldArcCatalog
    {
        public const string QuestId = "cindercompact";
        public const string EmberfallEventId = "event.cinderwold.emberfall";
        public const string AshjawMilestoneId = "encounter.cinderwold.ashjaw@-208:-43";
        public const string RouteboundResolutionId = "routebound";
        public const string AshboundResolutionId = "ashbound";

        public static readonly Vector3 LedgerPosition = new Vector3(-113f, 0f, -17f);
        public static readonly Vector3 RelayPosition = new Vector3(-188f, 0f, 103f);
        public static readonly Vector3 DaisPosition = RelayPosition + new Vector3(3.75f, 0f, -0.6f);
        public static readonly IReadOnlyList<string> RequiredCacheIds =
            new List<string>
            {
                "cache.cinderwold.embershelf",
                "cache.cinderwold.blackravine",
                "cache.cinderwold.ashgate"
            };
        public static readonly List<ResourceCost> RouteboundCost =
            new List<ResourceCost>
            {
                new ResourceCost(ResourceKind.Ore, 4),
                new ResourceCost(ResourceKind.Resin, 3),
                new ResourceCost(ResourceKind.Chimesalt, 2)
            };
    }

    public sealed class MainStoryStageDefinition
    {
        public string id;
        public string title;
        public string objective;
        public int progress;
    }

    public static class MainStoryCatalog
    {
        public const string QuestId = "story.hollow_choir.main";
        public const string HearthlineRouteId = "story.hollow_choir.route.hearthline";
        public const string OpenCurrentRouteId = "story.hollow_choir.route.open_current";
        public const string BossId = "boss.hollow_choir_cantor";
        public const int CompletedProgress = 6;

        public static readonly Vector3 RoutekeeperPosition = new Vector3(-33f, 0.9f, 54f);
        public static readonly Vector3 AlderwakeDefenseCenter = new Vector3(-48f, 0f, 56f);
        public static readonly Vector3 ObservatoryGatePosition = new Vector3(232f, 0.9f, -108f);
        public static readonly Vector3 ObservatoryReturnPosition = new Vector3(228f, 1.2f, -104f);
        public static readonly Vector3 ObservatoryCenter = new Vector3(300f, 0f, 226f);
        public static readonly Vector3 CinderwoldBreachCenter = new Vector3(-126f, 0f, -98f);

        public static readonly IReadOnlyList<MainStoryStageDefinition> Stages =
            new List<MainStoryStageDefinition>
            {
                Stage(
                    "story.hollow_choir.stage.rainline_report",
                    "A Chime Out of Season",
                    "Meet routekeeper Maelin Rook at the Rainline Watch west of Hearthmarket.",
                    0),
                Stage(
                    "story.hollow_choir.stage.alderwake_defense",
                    "Hold What Was Rebuilt",
                    "Sound the Rainline route signal and hold its three approaches beside Maelin.",
                    1),
                Stage(
                    "story.hollow_choir.stage.morrowfen_pursuit",
                    "The Stolen Chord",
                    "Enter the Morrowfen resonance observatory and follow the stolen chord through three chambers.",
                    2),
                Stage(
                    "story.hollow_choir.stage.resonance_witness",
                    "A Voice Is Not a Chain",
                    "Read the recovered resonance lens before leaving the observatory.",
                    3),
                Stage(
                    "story.hollow_choir.stage.cinderwold_choice",
                    "Two Shores of Fire",
                    "At the Cinderwold breach, choose the Hearthline or Open Current to protect.",
                    4),
                Stage(
                    "story.hollow_choir.stage.last_cantor",
                    "The Last False Note",
                    "Sound the protected route and defeat Vesryn, Cantor of the Hollow Choir.",
                    5)
            };

        public static readonly string[][] AlderwakeDefenseWaves =
        {
            new[] { "enemy.hollow_raider", "enemy.hollow_scout", "enemy.mossglass_stalker" },
            new[] { "enemy.hollow_raider", "enemy.hollow_raider", "enemy.hollow_scout", "enemy.hollow_scout" },
            new[] { "enemy.relay_breaker", "enemy.hollow_scout", "enemy.hollow_scout", "enemy.mossglass_stalker" }
        };

        public static readonly string[][] ObservatoryWaves =
        {
            new[] { "enemy.hollow_scout", "enemy.hollow_raider", "enemy.vault_wisp" },
            new[] { "enemy.hollow_scout", "enemy.hollow_scout", "enemy.fen_siltcaller", "enemy.vault_wisp" },
            new[] { "enemy.relay_breaker", "enemy.hollow_scout", "enemy.hollow_scout", "enemy.vault_wisp", "enemy.vault_wisp" }
        };

        public static QuestRecord CreateQuestRecord()
        {
            return new QuestRecord
            {
                id = QuestId,
                title = "The Chord Beyond the Hearth",
                objective = Stages[0].objective,
                required = CompletedProgress,
                favorReward = 48
            };
        }

        public static QuestRecord CreateRouteMarker(string routeId)
        {
            var hearthline = routeId == HearthlineRouteId;
            return new QuestRecord
            {
                id = hearthline ? HearthlineRouteId : OpenCurrentRouteId,
                title = hearthline ? "Route Held: Hearthline" : "Route Held: Open Current",
                objective = hearthline
                    ? "The repaired road carries the breach load toward Alderwake's hearths."
                    : "A voluntary Echosworn passage remains open beside the repaired road.",
                required = 1,
                favorReward = 0
            };
        }

        public static MainStoryStageDefinition GetStage(int progress)
        {
            if (progress < 0 || progress >= CompletedProgress)
            {
                return null;
            }

            return Stages[progress];
        }

        public static bool IsRouteMarker(string questId)
        {
            return questId == HearthlineRouteId || questId == OpenCurrentRouteId;
        }

        private static MainStoryStageDefinition Stage(
            string id,
            string title,
            string objective,
            int progress)
        {
            return new MainStoryStageDefinition
            {
                id = id,
                title = title,
                objective = objective,
                progress = progress
            };
        }
    }

    public static class EquipmentCatalog
    {
        public static List<EquipmentDefinition> CreateAll()
        {
            return new List<EquipmentDefinition>
            {
                new EquipmentDefinition
                {
                    id = "weapon.riftbow",
                    displayName = "Riftbow",
                    slot = EquipmentSlot.Weapon,
                    discipline = WeaponDiscipline.Riftbow,
                    damageBonus = 5,
                    description = "A field bow that sends a Threadglass bolt through a narrow line.",
                    craftCost = new List<ResourceCost>
                    {
                        new ResourceCost(ResourceKind.Timber, 4),
                        new ResourceCost(ResourceKind.Fiber, 3),
                        new ResourceCost(ResourceKind.Ore, 1)
                    }
                },
                new EquipmentDefinition
                {
                    id = "weapon.threadstaff",
                    displayName = "Threadstaff",
                    slot = EquipmentSlot.Weapon,
                    discipline = WeaponDiscipline.Threadstaff,
                    damageBonus = 3,
                    description = "A resonant staff that breaks a small circle of hostile pressure.",
                    craftCost = new List<ResourceCost>
                    {
                        new ResourceCost(ResourceKind.Timber, 3),
                        new ResourceCost(ResourceKind.Resin, 2),
                        new ResourceCost(ResourceKind.Chimesalt, 2)
                    }
                },
                new EquipmentDefinition
                {
                    id = "coat.mosscoat",
                    displayName = "Mosscoat",
                    slot = EquipmentSlot.Coat,
                    maxHealthBonus = 24,
                    description = "Stitched field cloth with a living-fiber lining.",
                    craftCost = new List<ResourceCost>
                    {
                        new ResourceCost(ResourceKind.Fiber, 4),
                        new ResourceCost(ResourceKind.Resin, 2)
                    }
                },
                new EquipmentDefinition
                {
                    id = "charm.anchor",
                    displayName = "Anchor Charm",
                    slot = EquipmentSlot.Charm,
                    guardBonus = 4,
                    description = "A route weight that strengthens a clean guard.",
                    favorPrice = 24
                },
                new EquipmentDefinition
                {
                    id = "charm.galespring",
                    displayName = "Galespring Charm",
                    slot = EquipmentSlot.Charm,
                    staminaRegenerationBonus = 13f,
                    description = "A wind-wound charm that restores field stamina more quickly.",
                    favorPrice = 28
                },
                new EquipmentDefinition
                {
                    id = "charm.mirelight",
                    displayName = "Mirelight Charm",
                    slot = EquipmentSlot.Charm,
                    staminaRegenerationBonus = 8f,
                    restorationRewardOnly = true,
                    description = "A lantern charm recovered from the restored Fenlight Causeway."
                },
                new EquipmentDefinition
                {
                    id = DungeonCatalog.GlasswaterRewardId,
                    displayName = "Sluiceglass Charm",
                    slot = EquipmentSlot.Charm,
                    guardBonus = 5,
                    staminaRegenerationBonus = 6f,
                    restorationRewardOnly = true,
                    description = "A keeper's tideglass that steadies guards and recovers stamina between pressure surges."
                },
                new EquipmentDefinition
                {
                    id = "charm.emberwatch",
                    displayName = "Emberwatch Charm",
                    slot = EquipmentSlot.Charm,
                    guardBonus = 6,
                    restorationRewardOnly = true,
                    description = "A relayward charm that turns a clean guard into a steadier hold."
                },
                new EquipmentDefinition
                {
                    id = "tool.ashkite",
                    displayName = "Ashkite",
                    slot = EquipmentSlot.Tool,
                    staminaBonus = 18f,
                    restorationRewardOnly = true,
                    description = "A migration-sail tool that carries a larger reserve through ash currents."
                },
                new EquipmentDefinition
                {
                    id = "tool.hookline",
                    displayName = "Hookline",
                    slot = EquipmentSlot.Tool,
                    moveSpeedBonus = 0.08f,
                    description = "A braided tool line that gives climbing routes a faster pull.",
                    craftCost = new List<ResourceCost>
                    {
                        new ResourceCost(ResourceKind.Fiber, 5),
                        new ResourceCost(ResourceKind.Ore, 2)
                    }
                },
                new EquipmentDefinition
                {
                    id = "tool.ridgepack",
                    displayName = "Ridgepack",
                    slot = EquipmentSlot.Tool,
                    staminaBonus = 24f,
                    description = "A balanced climbing pack that increases your field stamina reserve.",
                    craftCost = new List<ResourceCost>
                    {
                        new ResourceCost(ResourceKind.Fiber, 4),
                        new ResourceCost(ResourceKind.Herbs, 3),
                        new ResourceCost(ResourceKind.Ore, 2)
                    }
                },
                new EquipmentDefinition
                {
                    id = "tool.thermal_reel",
                    displayName = "Thermal Reel",
                    slot = EquipmentSlot.Tool,
                    windLiftVelocityBonus = 2.6f,
                    restorationRewardOnly = true,
                    description = "A forge-balanced spool that turns Threadwind lifts into longer gliding routes."
                }
            };
        }
    }

    public static class CrossweaveCatalog
    {
        private static readonly List<CrossweaveDefinition> Definitions = new List<CrossweaveDefinition>
        {
            new CrossweaveDefinition
            {
                weapon = WeaponDiscipline.Pathblade,
                affinity = CrossweaveAffinity.Anchor,
                displayName = "Anchor Relay",
                cue = "ANCHOR",
                color = new Color(0.94f, 0.58f, 0.26f)
            },
            new CrossweaveDefinition
            {
                weapon = WeaponDiscipline.Riftbow,
                affinity = CrossweaveAffinity.Sight,
                displayName = "Sight Relay",
                cue = "SIGHT",
                color = new Color(0.38f, 0.9f, 0.82f)
            },
            new CrossweaveDefinition
            {
                weapon = WeaponDiscipline.Threadstaff,
                affinity = CrossweaveAffinity.Veil,
                displayName = "Veil Relay",
                cue = "VEIL",
                color = new Color(0.55f, 0.7f, 1f)
            }
        };

        public static CrossweaveDefinition Get(WeaponDiscipline weapon)
        {
            return Definitions.Find(definition => definition.weapon == weapon);
        }

        public static CrossweaveAffinity GetPactAffinity(PactCreature pact)
        {
            if (pact == null)
            {
                return CrossweaveAffinity.Anchor;
            }

            return pact.CreatureName == "Cinderwing"
                ? CrossweaveAffinity.Sight
                : pact.CreatureName == "Mistling"
                    ? CrossweaveAffinity.Veil
                    : CrossweaveAffinity.Anchor;
        }

        public static CrossweaveAffinity GetPactAffinity(PactResonanceAffinity affinity)
        {
            return affinity switch
            {
                PactResonanceAffinity.Gale => CrossweaveAffinity.Sight,
                PactResonanceAffinity.Ember => CrossweaveAffinity.Sight,
                PactResonanceAffinity.Tide => CrossweaveAffinity.Veil,
                PactResonanceAffinity.Gloam => CrossweaveAffinity.Veil,
                _ => CrossweaveAffinity.Anchor
            };
        }

        public static CrossweaveAffinity GetCompanionAffinity(HumanCompanion companion)
        {
            if (companion == null)
            {
                return CrossweaveAffinity.Anchor;
            }

            return companion.Role.Contains("scout")
                ? CrossweaveAffinity.Sight
                : companion.Role.Contains("medic")
                    ? CrossweaveAffinity.Veil
                    : CrossweaveAffinity.Anchor;
        }
    }

    public static class BossCatalog
    {
        private static readonly List<BossDefinition> Definitions = new List<BossDefinition>
        {
            new BossDefinition
            {
                id = "boss.skyvault_warden",
                displayName = "Skyvault Warden",
                maxHealth = 620,
                baseColor = new Color(0.34f, 0.2f, 0.5f),
                phaseColor = new Color(0.58f, 0.19f, 0.34f),
                labelColor = new Color(0.96f, 0.75f, 0.89f),
                phaseThresholdOne = 0.5f,
                phaseOneMoveSpeed = 1.45f,
                phaseTwoMoveSpeed = 2.15f,
                summonedEnemyId = "enemy.vault_wisp",
                phaseOneActions = new List<BossActionKind>
                {
                    BossActionKind.Sweep,
                    BossActionKind.ShardVolley
                },
                phaseTwoActions = new List<BossActionKind>
                {
                    BossActionKind.Threadquake,
                    BossActionKind.ShardVolley,
                    BossActionKind.Sweep
                }
            },
            new BossDefinition
            {
                id = DungeonCatalog.GlasswaterBossId,
                displayName = "Glasswater Regent",
                maxHealth = 720,
                baseColor = new Color(0.16f, 0.48f, 0.55f),
                phaseColor = new Color(0.35f, 0.72f, 0.78f),
                labelColor = new Color(0.72f, 0.96f, 0.92f),
                phaseThresholdOne = 0.68f,
                phaseThresholdTwo = 0.34f,
                phaseOneMoveSpeed = 1.55f,
                phaseTwoMoveSpeed = 2.2f,
                summonedEnemyId = "enemy.vault_wisp",
                phaseOneActions = new List<BossActionKind>
                {
                    BossActionKind.ShardVolley,
                    BossActionKind.Sweep
                },
                phaseTwoActions = new List<BossActionKind>
                {
                    BossActionKind.Threadquake,
                    BossActionKind.ShardVolley,
                    BossActionKind.RailCharge
                }
            },
            new BossDefinition
            {
                id = "boss.scoria_harrower",
                displayName = "Scoria Harrower",
                maxHealth = 760,
                baseColor = new Color(0.36f, 0.16f, 0.1f),
                phaseColor = new Color(0.75f, 0.22f, 0.09f),
                labelColor = new Color(1f, 0.67f, 0.34f),
                phaseThresholdOne = 0.7f,
                phaseThresholdTwo = 0.35f,
                phaseOneMoveSpeed = 1.7f,
                phaseTwoMoveSpeed = 2.35f,
                phaseOneActions = new List<BossActionKind>
                {
                    BossActionKind.RailCharge,
                    BossActionKind.Cinderfall,
                    BossActionKind.Sweep
                },
                phaseTwoActions = new List<BossActionKind>
                {
                    BossActionKind.Cinderfall,
                    BossActionKind.RailCharge,
                    BossActionKind.Threadquake
                }
            },
            new BossDefinition
            {
                id = MainStoryCatalog.BossId,
                displayName = "Vesryn, Hollow Cantor",
                maxHealth = 690,
                baseColor = new Color(0.19f, 0.12f, 0.27f),
                phaseColor = new Color(0.64f, 0.17f, 0.29f),
                labelColor = new Color(0.82f, 0.94f, 0.88f),
                phaseThresholdOne = 0.7f,
                phaseThresholdTwo = 0.35f,
                phaseOneMoveSpeed = 1.55f,
                phaseTwoMoveSpeed = 2.1f,
                summonedEnemyId = "enemy.hollow_scout",
                phaseOneActions = new List<BossActionKind>
                {
                    BossActionKind.ShardVolley,
                    BossActionKind.Sweep
                },
                phaseTwoActions = new List<BossActionKind>
                {
                    BossActionKind.RailCharge,
                    BossActionKind.Cinderfall,
                    BossActionKind.Threadquake
                }
            }
        };

        public static BossDefinition Get(string id)
        {
            return Definitions.Find(definition => definition.id == id);
        }
    }

    public static class TrialCatalog
    {
        public static readonly TrialDefinition AshwakeCrucible = new TrialDefinition
        {
            id = "trial.ashwake_crucible",
            displayName = "Ashwake Crucible",
            gatePosition = new Vector3(-214f, 0.9f, -104f),
            entrancePosition = new Vector3(-160f, 1.6f, -253f),
            returnPosition = new Vector3(-198f, 1.2f, -96f),
            bossId = "boss.scoria_harrower",
            rewardEquipmentId = "tool.thermal_reel",
            accentColor = new Color(0.96f, 0.42f, 0.19f)
        };
    }

    public static class CityCatalog
    {
        public static readonly CityDefinition AlderwakeHearthmarket = new CityDefinition
        {
            id = "city.alderwake.hearthmarket",
            displayName = "Alderwake Hearthmarket",
            subtitle = "A growing river city of routeworkers, growers, and wayfinders",
            center = new Vector3(0f, 0f, 72f),
            accentColor = new Color(0.46f, 0.84f, 0.62f),
            residents = new List<CityNpcDefinition>
            {
                Resident(
                    "npc.alderwake.iven",
                    "Iven Lark",
                    "notice steward",
                    new Vector3(0f, 0.9f, 64f),
                    new Color(0.69f, 0.47f, 0.24f),
                    CityResidentService.NoticeBoard,
                    "Every route asks something of us. Pick work that strengthens the people who travel it.",
                    1.1f),
                Resident(
                    "npc.alderwake.yara",
                    "Yara Pell",
                    "hearthkeeper",
                    new Vector3(-21f, 0.9f, 62f),
                    new Color(0.55f, 0.34f, 0.23f),
                    CityResidentService.Inn,
                    "A warm bowl is not a detour. It is how a Wayfinder returns with their whole self.",
                    1f),
                Resident(
                    "npc.alderwake.arin",
                    "Arin Vale",
                    "ring marshal",
                    new Vector3(24f, 0.9f, 69f),
                    new Color(0.45f, 0.32f, 0.58f),
                    CityResidentService.NoticeBoard,
                    "The ring does not make enemies. It makes the road less likely to surprise you.",
                    1f),
                Resident(
                    "npc.alderwake.vela",
                    "Vela Oris",
                    "route archivist",
                    new Vector3(-21f, 0.9f, 80.8f),
                    new Color(0.29f, 0.54f, 0.63f),
                    CityResidentService.Archive,
                    "Maps are promises made by people who came back. Leave yours better than you found it.",
                    0.75f),
                Resident(
                    "npc.alderwake.ena",
                    "Ena Moss",
                    "terrace grower",
                    new Vector3(11f, 0.9f, 86f),
                    new Color(0.35f, 0.69f, 0.36f),
                    CityResidentService.Dialogue,
                    "The city's first harvest is always small. The second one means we believed in the first.",
                    1.5f),
                Resident(
                    "npc.alderwake.mira",
                    "Mira Sable",
                    "copperweaver",
                    new Vector3(27f, 0.9f, 82f),
                    new Color(0.78f, 0.43f, 0.2f),
                    CityResidentService.Dialogue,
                    "Threadglass catches light differently after rain. I make sure the city notices.",
                    1.3f),
                Resident(
                    "npc.alderwake.oren",
                    "Oren Fallow",
                    "dock courier",
                    new Vector3(-31f, 0.9f, 75f),
                    new Color(0.44f, 0.48f, 0.62f),
                    CityResidentService.Dialogue,
                    "If a letter reaches the marsh dry, that is the work of six people, not one.",
                    1.8f)
            },
            activities = new List<CityActivityDefinition>
            {
                new CityActivityDefinition
                {
                    id = "city.alderwake.supply_run",
                    title = "Hearthmarket supply drive",
                    description = "Deliver timber and fiber for new roofs, public benches, and river awnings.",
                    kind = CityActivityKind.Delivery,
                    cost = new List<ResourceCost>
                    {
                        new ResourceCost(ResourceKind.Timber, 3),
                        new ResourceCost(ResourceKind.Fiber, 3)
                    },
                    favorReward = 18,
                    experienceReward = 24,
                    cooldownSeconds = 240f
                },
                new CityActivityDefinition
                {
                    id = "city.alderwake.training_ring",
                    title = "Rivergate training ring",
                    description = "Face a controlled three-hostile road drill in the marshal's ring.",
                    kind = CityActivityKind.Training,
                    enemyIds = new[]
                    {
                        "enemy.hollow_raider",
                        "enemy.mossglass_stalker",
                        "enemy.hollow_scout"
                    },
                    arenaPosition = new Vector3(24f, 0f, 76f),
                    favorReward = 26,
                    experienceReward = 34,
                    cooldownSeconds = 360f
                },
                new CityActivityDefinition
                {
                    id = "city.alderwake.archive_exchange",
                    title = "Route archive exchange",
                    description = "Record a field observation with the archive to improve public route knowledge.",
                    kind = CityActivityKind.Archive,
                    favorReward = 12,
                    experienceReward = 18,
                    cooldownSeconds = 480f
                }
            }
        };

        public static readonly CityDefinition VeyrwaterMooring = new CityDefinition
        {
            id = "city.morrowfen.veyrwater",
            displayName = "Veyrwater Mooring",
            subtitle = "Lantern docks, reedwrights, and ferries holding back the fen",
            center = new Vector3(203f, 0f, 72f),
            accentColor = new Color(0.32f, 0.74f, 0.76f),
            residents = new List<CityNpcDefinition>
            {
                Resident(
                    "npc.veyrwater.solin",
                    "Solin Wick",
                    "lantern captain",
                    new Vector3(203f, 0.9f, 64f),
                    new Color(0.34f, 0.58f, 0.68f),
                    CityResidentService.NoticeBoard,
                    "A dark channel does not stay empty. It merely invites something else to use it first.",
                    1.1f),
                Resident(
                    "npc.veyrwater.tessa",
                    "Tessa Mire",
                    "ferry host",
                    new Vector3(187f, 0.9f, 64f),
                    new Color(0.42f, 0.48f, 0.28f),
                    CityResidentService.Inn,
                    "The tide has never met a traveler it could hurry. Sit down before you make the same mistake.",
                    1f),
                Resident(
                    "npc.veyrwater.jalen",
                    "Jalen Reed",
                    "watch trainer",
                    new Vector3(220f, 0.9f, 64f),
                    new Color(0.33f, 0.27f, 0.57f),
                    CityResidentService.NoticeBoard,
                    "Watch the waterline, then the treeline. The third place something moves is the one that matters.",
                    1f),
                Resident(
                    "npc.veyrwater.issa",
                    "Issa Venn",
                    "flood archivist",
                    new Vector3(187f, 0.9f, 81.8f),
                    new Color(0.36f, 0.67f, 0.71f),
                    CityResidentService.Archive,
                    "A drowned road is still a road. We remember where it went, so it can return.",
                    0.7f),
                Resident(
                    "npc.veyrwater.pell",
                    "Pell Yarrow",
                    "reedwright",
                    new Vector3(222f, 0.9f, 84f),
                    new Color(0.46f, 0.65f, 0.35f),
                    CityResidentService.Dialogue,
                    "Reed roofs bend in wind. That is why they outlast the proud ones.",
                    1f),
                Resident(
                    "npc.veyrwater.fenn",
                    "Fenn Arlo",
                    "marsh courier",
                    new Vector3(225f, 0.9f, 84f),
                    new Color(0.73f, 0.5f, 0.22f),
                    CityResidentService.Dialogue,
                    "If a package comes wrapped in oilcloth, do not ask where it slept.",
                    1f)
            },
            activities = new List<CityActivityDefinition>
            {
                new CityActivityDefinition
                {
                    id = "city.veyrwater.lantern_stock",
                    title = "Veyrwater lantern stock",
                    description = "Deliver resin and herbs so the ferry channels stay legible through the fog.",
                    kind = CityActivityKind.Delivery,
                    cost = new List<ResourceCost>
                    {
                        new ResourceCost(ResourceKind.Resin, 2),
                        new ResourceCost(ResourceKind.Herbs, 3)
                    },
                    favorReward = 20,
                    experienceReward = 26,
                    cooldownSeconds = 240f
                },
                new CityActivityDefinition
                {
                    id = "city.veyrwater.floodwatch",
                    title = "Floodwatch drill",
                    description = "Hold the mooring watch against a mixed fen pressure drill.",
                    kind = CityActivityKind.Training,
                    enemyIds = new[]
                    {
                        "enemy.mossglass_stalker",
                        "enemy.fen_spitter",
                        "enemy.vault_wisp"
                    },
                    arenaPosition = new Vector3(226f, 0f, 77f),
                    favorReward = 28,
                    experienceReward = 36,
                    cooldownSeconds = 360f
                },
                new CityActivityDefinition
                {
                    id = "city.veyrwater.archive_exchange",
                    title = "Flood archive exchange",
                    description = "Add a field account to Veyrwater's living map of safe channels.",
                    kind = CityActivityKind.Archive,
                    favorReward = 14,
                    experienceReward = 20,
                    cooldownSeconds = 480f
                }
            }
        };

        private static CityNpcDefinition Resident(
            string id,
            string displayName,
            string role,
            Vector3 position,
            Color color,
            CityResidentService service,
            string dialogue,
            float wanderRadius)
        {
            return new CityNpcDefinition
            {
                id = id,
                displayName = displayName,
                role = role,
                position = position,
                color = color,
                service = service,
                dialogue = dialogue,
                wanderRadius = wanderRadius
            };
        }
    }

    public static class BountyCatalog
    {
        public static readonly IReadOnlyList<BountyDefinition> All = new List<BountyDefinition>
        {
            Create(
                "bounty.mossfang_matriarch",
                "Mossfang Matriarch",
                "A vine-fed hunt leader has begun driving smaller predators toward Alderwake's trade road.",
                CityCatalog.AlderwakeHearthmarket.id,
                "enemy.elite.mossfang_matriarch",
                new Vector3(60f, 0f, -78f),
                BountyDifficulty.Notable,
                32,
                44,
                new ResourceCost(ResourceKind.Herbs, 3),
                new Color(0.33f, 0.83f, 0.4f),
                "Briarpack Call",
                "enemy.mossglass_stalker",
                "enemy.mossglass_stalker"),
            Create(
                "bounty.siltglass_heron",
                "Siltglass Heron",
                "A luminous marsh hunter is pinning couriers in the fog with long-range volleys.",
                CityCatalog.VeyrwaterMooring.id,
                "enemy.elite.siltglass_heron",
                new Vector3(203f, 0f, -76f),
                BountyDifficulty.Dangerous,
                38,
                52,
                new ResourceCost(ResourceKind.Chimesalt, 2),
                new Color(0.3f, 0.83f, 0.9f),
                "Mist Volley",
                "enemy.fen_spitter",
                "enemy.vault_wisp"),
            Create(
                "bounty.cindermaw",
                "Cindermaw",
                "An ash-shelter brute braces its relay-breaking shockwaves in Ashguard; answer with a focused weapon-swap Crossweave.",
                CityCatalog.AlderwakeHearthmarket.id,
                "enemy.elite.cindermaw",
                new Vector3(-216f, 0f, 90f),
                BountyDifficulty.Apex,
                52,
                72,
                new ResourceCost(ResourceKind.Ore, 3),
                new Color(0.98f, 0.31f, 0.12f),
                "Ashguard Break",
                "enemy.cinder_echo",
                "enemy.cinder_echo"),
            Create(
                "bounty.mirewarden",
                "Mirewarden",
                "A drowned route sentinel has turned a northern marsh crossing into a crushing choke point.",
                CityCatalog.VeyrwaterMooring.id,
                "enemy.elite.mirewarden",
                new Vector3(118f, 0f, 107f),
                BountyDifficulty.Dangerous,
                44,
                60,
                new ResourceCost(ResourceKind.Resin, 3),
                new Color(0.45f, 0.42f, 0.92f),
                "Siltward Invocation",
                "enemy.fen_spitter",
                "enemy.fen_siltcaller")
        };

        public static BountyDefinition Get(string id)
        {
            foreach (var bounty in All)
            {
                if (bounty.id == id)
                {
                    return bounty;
                }
            }

            return null;
        }

        private static BountyDefinition Create(
            string id,
            string title,
            string description,
            string sponsorCityId,
            string targetEnemyId,
            Vector3 position,
            BountyDifficulty difficulty,
            int favorReward,
            int experienceReward,
            ResourceCost materialReward,
            Color accentColor,
            string phaseTitle,
            params string[] reinforcementEnemyIds)
        {
            return new BountyDefinition
            {
                id = id,
                title = title,
                description = description,
                sponsorCityId = sponsorCityId,
                targetEnemyId = targetEnemyId,
                position = position,
                difficulty = difficulty,
                favorReward = favorReward,
                experienceReward = experienceReward,
                materialReward = new List<ResourceCost> { materialReward },
                cooldownSeconds = 900f,
                accentColor = accentColor,
                phaseTitle = phaseTitle,
                reinforcementEnemyIds = reinforcementEnemyIds ?? System.Array.Empty<string>()
            };
        }
    }

    public static class ThreadwayCatalog
    {
        public static readonly IReadOnlyList<ThreadwayDefinition> All = new List<ThreadwayDefinition>
        {
            Create(
                "threadway.hearthmarket",
                "Hearthmarket Loom",
                "Alderwake Basin",
                "A coppered anchor maintained by the market's routewrights.",
                new Vector3(0f, 0f, 49f),
                new Vector3(0f, 0.05f, 53f),
                0,
                new Color(0.96f, 0.61f, 0.28f)),
            Create(
                "threadway.northwake",
                "Northwake Lens",
                "Alderwake Basin",
                "An observatory lens that threads the high terraces into the road network.",
                new Vector3(8f, 0f, 101f),
                new Vector3(8f, 0.05f, 97f),
                1,
                new Color(0.38f, 0.88f, 0.64f)),
            Create(
                "threadway.southroot",
                "Southroot Coil",
                "Alderwake Basin",
                "A root-bound relay for travelers crossing the basin's low roads.",
                new Vector3(-4f, 0f, -103f),
                new Vector3(-4f, 0.05f, -99f),
                1,
                new Color(0.39f, 0.75f, 0.48f)),
            Create(
                "threadway.veyrwater",
                "Veyrwater Mooring",
                "Morrowfen",
                "A tide-steady anchor wired to the mooring's lantern ledger.",
                new Vector3(203f, 0f, 53f),
                new Vector3(203f, 0.05f, 57f),
                0,
                new Color(0.34f, 0.86f, 0.76f)),
            Create(
                "threadway.lanternreach",
                "Lanternreach Shelf",
                "Morrowfen",
                "A raised marsh shelf where old route lights can still answer one another.",
                new Vector3(181f, 0f, 46f),
                new Vector3(181f, 0.05f, 42f),
                1,
                new Color(0.31f, 0.68f, 0.92f)),
            Create(
                "threadway.skyvault",
                "Skyvault Threshold",
                "Morrowfen",
                "A quiet staging anchor outside the buried vault entrance.",
                new Vector3(108f, 0f, 24f),
                new Vector3(104f, 0.05f, 24f),
                2,
                new Color(0.62f, 0.74f, 0.98f)),
            Create(
                "threadway.cinderstation",
                "Cinder Station",
                "Cinderwold Verge",
                "A salvager's anchor built from heat-scored route fittings.",
                new Vector3(-99f, 0f, -7f),
                new Vector3(-95f, 0.05f, -7f),
                0,
                new Color(0.95f, 0.42f, 0.23f)),
            Create(
                "threadway.emberrelay",
                "Ember Relay",
                "Cinderwold Verge",
                "A cracked relay stone that answers only after it is rekindled.",
                new Vector3(-177f, 0f, 91f),
                new Vector3(-173f, 0.05f, 91f),
                2,
                new Color(0.98f, 0.28f, 0.16f)),
            Create(
                "threadway.ashshelf",
                "Ashshelf Spur",
                "Cinderwold Verge",
                "A distant shelf anchor overlooking the most dangerous relay routes.",
                new Vector3(-219f, 0f, -104f),
                new Vector3(-215f, 0.05f, -104f),
                2,
                new Color(0.78f, 0.37f, 0.26f))
        };

        public static ThreadwayDefinition Get(string id)
        {
            foreach (var definition in All)
            {
                if (definition.id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        private static ThreadwayDefinition Create(
            string id,
            string title,
            string region,
            string description,
            Vector3 position,
            Vector3 arrivalPosition,
            int activationChimesaltCost,
            Color accentColor)
        {
            return new ThreadwayDefinition
            {
                id = id,
                title = title,
                region = region,
                description = description,
                position = position,
                arrivalPosition = arrivalPosition,
                activationChimesaltCost = activationChimesaltCost,
                accentColor = accentColor
            };
        }
    }

    public static class QuestCatalog
    {
        public static List<QuestRecord> CreateAll()
        {
            return new List<QuestRecord>
            {
                Create("alderwake", "Hands at the Hearth", "Restore Alderwake's wayside station.", 3, 24),
                Create("skyvault", "The Skyvault Below", "Stabilize three resonance pylons and defeat the Warden.", 5, 40),
                Create("fieldcraft", "Tools of the Road", "Craft or purchase field equipment.", 3, 18),
                Create("campward", "No Tithe on the Road", "Clear three hostile camps across the connected regions.", 3, 26),
                Create("hearthgarden", "Green After Stillfall", "Harvest three Hearthgarden plots.", 3, 16),
                Create("accords", "Three Willing Voices", "Form three voluntary support Pacts.", 3, 28),
                Create("signalbreak", "Signals in the Storm", "Stabilize the three broken route signals.", 3, 34),
                Create("fieldarchives", "Lost Bearings", "Complete all nine regional Wayfinder field surveys.", 9, 30),
                Create("fenlight", "A Light Held Low", "Restore the Fenlight Causeway after opening the Skyvault route.", 4, 36),
                Create(
                    DungeonCatalog.GlasswaterQuestId,
                    "Glasswater Under Pressure",
                    "Solve both sluice controls, clear the Brood Cistern, and defeat the Glasswater Regent.",
                    4,
                    48),
                Create("cindercompact", "The Ember Compact", "Choose the fate of the Ember Relay after gathering its witness records.", 4, 44),
                Create("hearthmarket", "A City Takes Root", "Complete three Hearthmarket civic activities.", 3, 40),
                Create("veyrwater", "Lights Against the Flood", "Complete three Veyrwater civic activities.", 3, 42),
                Create("bounties", "Names on the Board", "Resolve four city-issued elite bounties.", 4, 64),
                Create("threadways", "Routes Remembered", "Restore nine regional Threadway anchors.", 9, 72),
                Create("routevigil", "The Road Keeps Watch", "Hold twelve destabilized route signal patrols.", 12, 58)
            };
        }

        private static QuestRecord Create(string id, string title, string objective, int required, int reward)
        {
            return new QuestRecord
            {
                id = id,
                title = title,
                objective = objective,
                required = required,
                favorReward = reward
            };
        }
    }
}
