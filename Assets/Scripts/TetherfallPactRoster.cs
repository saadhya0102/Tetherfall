using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Tetherfall
{
    public enum PactCombatRole
    {
        Vanguard,
        Warden,
        Skirmisher,
        Mender,
        Controller,
        Resonator
    }

    public enum PactResonanceAffinity
    {
        Grove,
        Gale,
        Tide,
        Gloam,
        Ember,
        Prism
    }

    public enum PactMovementKind
    {
        Grounded,
        Hovering,
        Amphibious,
        Gliding
    }

    public enum PactWorkKind
    {
        Foraging,
        Cultivation,
        Survey,
        Courier,
        Crafting,
        Waterkeeping,
        Care,
        Watch,
        Heatkeeping,
        Salvage
    }

    public enum PactFieldSlotState
    {
        Reserve,
        ActiveField,
        SettlementPreference
    }

    public enum PactArtBodyPlan
    {
        LowQuadruped,
        Longstrider,
        Winged,
        Finned,
        Burrower,
        Kitelizard
    }

    public sealed class PactBaseStats
    {
        public int vitality;
        public int power;
        public int guard;
        public float speed;
        public float assistRange;
        public float assistCooldown;
    }

    public sealed class PactMovementProfile
    {
        public PactMovementKind kind;
        public bool usesFlyingMotor;
        public float formationSpacing;
        public string traversalCharacter;
    }

    public sealed class PactWorkAffinity
    {
        public PactWorkKind kind;
        public int rating;
        public string voluntaryPreference;
    }

    public sealed class PactArtProfile
    {
        public string artProfileId;
        public PactArtBodyPlan bodyPlan;
        public Color surfaceColor;
        public Color accentColor;
        public Color resonanceColor;
        public Color eyeColor;
        public Vector3 bodyScale;
        public Vector3 headScale;
        public float legLength;
        public float tailLength;
        public float crestHeight;
        public string silhouetteDirection;
        public string materialDirection;
        public string articulationDirection;
    }

    public sealed class PactSpeciesDefinition
    {
        public string speciesId;
        public string displayName;
        public string defaultEpithet;
        public string regionId;
        public string regionTheme;
        public bool isWild;
        public string eligibleEnemyId;
        public string linkedLegendaryPactId;
        public PactCombatRole combatRole;
        public PactResonanceAffinity resonanceAffinity;
        public PactMovementProfile movement;
        public PactArtProfile art;
        public PactBaseStats baseStats;
        public PactWorkAffinity[] workAffinities = Array.Empty<PactWorkAffinity>();
        public float baseStamina;
        public float baseMorale;
        public string fieldTemperament;
    }

    public static class TetherfallAccordSpeciesCatalog
    {
        public const string MossbellStriderId = "species.wild.alderwake.mossbell_strider";
        public const string CopperquillNesterId = "species.wild.alderwake.copperquill_nester";
        public const string TideveilSkimmerId = "species.wild.morrowfen.tideveil_skimmer";
        public const string GloamreedLanternbackId = "species.wild.morrowfen.gloamreed_lanternback";
        public const string EmberhornBurrowerId = "species.wild.cinderwold.emberhorn_burrower";
        public const string PrismtailKitelizardId = "species.wild.cinderwold.prismtail_kitelizard";

        public const string StoryBrambletSpeciesId = "species.story.bramblet";
        public const string StorySlatebackSpeciesId = "species.story.slateback";
        public const string StoryMistlingSpeciesId = "species.story.mistling";
        public const string StoryCinderwingSpeciesId = "species.story.cinderwing";

        private static readonly List<PactSpeciesDefinition> WildDefinitions =
            new List<PactSpeciesDefinition>
            {
                Species(
                    MossbellStriderId,
                    "Mossbell Strider",
                    "of the Rainroot Verge",
                    "region.alderwake",
                    "Alderwake Basin",
                    "enemy.wild.alderwake.mossbell_strider",
                    PactCombatRole.Warden,
                    PactResonanceAffinity.Grove,
                    Movement(
                        PactMovementKind.Grounded,
                        false,
                        1.15f,
                        "Sure-footed over root shelves and rain-soft ground."),
                    Art(
                        "art.pact.mossbell_strider",
                        PactArtBodyPlan.Longstrider,
                        new Color(0.24f, 0.38f, 0.22f),
                        new Color(0.69f, 0.58f, 0.28f),
                        new Color(0.32f, 0.92f, 0.62f),
                        new Color(0.86f, 0.94f, 0.68f),
                        new Vector3(1.28f, 0.82f, 1.52f),
                        new Vector3(0.68f, 0.62f, 0.7f),
                        0.72f,
                        0.78f,
                        0.46f,
                        "A rain-deer silhouette with a low bell-shaped crown and broad rootstep feet.",
                        "Layered bark plates, wet moss nap, and copper seed-bells kept clear of familiar animal markings.",
                        "Long planted strides, listening crown tilts, and a counter-swinging fern tail."),
                    Stats(132, 14, 20, 5.8f, 8.5f, 3.15f),
                    88f,
                    76f,
                    "Patient, sheltering, and most confident beside varied companions.",
                    Work(PactWorkKind.Cultivation, 5, "Prefers tending living terraces at an unhurried pace."),
                    Work(PactWorkKind.Foraging, 4, "Volunteers for rainroot and seed gathering along familiar paths."),
                    Work(PactWorkKind.Survey, 2, "Enjoys short boundary walks with frequent rest.")),
                Species(
                    CopperquillNesterId,
                    "Copperquill Nester",
                    "of the High Bough",
                    "region.alderwake",
                    "Alderwake Basin",
                    "enemy.wild.alderwake.copperquill_nester",
                    PactCombatRole.Skirmisher,
                    PactResonanceAffinity.Gale,
                    Movement(
                        PactMovementKind.Gliding,
                        true,
                        1.3f,
                        "Short, controlled glides between canopy breaks."),
                    Art(
                        "art.pact.copperquill_nester",
                        PactArtBodyPlan.Winged,
                        new Color(0.37f, 0.31f, 0.24f),
                        new Color(0.78f, 0.4f, 0.16f),
                        new Color(0.42f, 0.9f, 0.74f),
                        new Color(0.98f, 0.82f, 0.45f),
                        new Vector3(1.05f, 0.64f, 1.28f),
                        new Vector3(0.6f, 0.54f, 0.62f),
                        0.38f,
                        0.86f,
                        0.58f,
                        "A compact canopy glider with split copper quills and a seed-pod breast keel.",
                        "Matte charcoal down, hammered-copper quill edges, and teal current-lines.",
                        "Asymmetric quill fans, quick head checks, and two-stage folding wings."),
                    Stats(92, 19, 9, 7.4f, 10.5f, 2.35f),
                    96f,
                    71f,
                    "Alert and playful, settling fastest when given a high lookout.",
                    Work(PactWorkKind.Courier, 4, "Chooses short message flights with visible landing markers."),
                    Work(PactWorkKind.Survey, 3, "Likes mapping canopy currents in clear weather."),
                    Work(PactWorkKind.Crafting, 2, "Offers shed quills for fine routework when comfortable.")),
                Species(
                    TideveilSkimmerId,
                    "Tideveil Skimmer",
                    "of the Quiet Channel",
                    "region.morrowfen",
                    "Morrowfen",
                    "enemy.wild.morrowfen.tideveil_skimmer",
                    PactCombatRole.Mender,
                    PactResonanceAffinity.Tide,
                    Movement(
                        PactMovementKind.Amphibious,
                        false,
                        1.2f,
                        "Skims reedwater, then plants broad fins on firm silt."),
                    Art(
                        "art.pact.tideveil_skimmer",
                        PactArtBodyPlan.Finned,
                        new Color(0.18f, 0.36f, 0.39f),
                        new Color(0.48f, 0.72f, 0.67f),
                        new Color(0.3f, 0.92f, 0.9f),
                        new Color(0.78f, 0.98f, 0.95f),
                        new Vector3(1.34f, 0.54f, 1.56f),
                        new Vector3(0.62f, 0.48f, 0.7f),
                        0.34f,
                        1.02f,
                        0.36f,
                        "A low reedwater skimmer with four leaf-fins and a translucent veil crest.",
                        "Silt-dark pebbled hide, nacre fin planes, and contained turquoise droplets.",
                        "Alternating fin paddles, slow veil breathing, and a buoyant tail wave."),
                    Stats(104, 11, 13, 6.3f, 9.5f, 4.4f),
                    91f,
                    84f,
                    "Gentle and observant, answering distress before threat.",
                    Work(PactWorkKind.Waterkeeping, 5, "Prefers clearing calm channels and checking clean-water chimes."),
                    Work(PactWorkKind.Care, 4, "Volunteers near recovery gardens when quiet space is available."),
                    Work(PactWorkKind.Foraging, 2, "Enjoys gathering floating herbs close to shore.")),
                Species(
                    GloamreedLanternbackId,
                    "Gloamreed Lanternback",
                    "of the Low Lanterns",
                    "region.morrowfen",
                    "Morrowfen",
                    "enemy.wild.morrowfen.gloamreed_lanternback",
                    PactCombatRole.Controller,
                    PactResonanceAffinity.Gloam,
                    Movement(
                        PactMovementKind.Hovering,
                        true,
                        1.25f,
                        "Hovers just above reeds and settled boardwalks."),
                    Art(
                        "art.pact.gloamreed_lanternback",
                        PactArtBodyPlan.LowQuadruped,
                        new Color(0.19f, 0.22f, 0.31f),
                        new Color(0.38f, 0.55f, 0.59f),
                        new Color(0.55f, 0.36f, 0.88f),
                        new Color(0.82f, 0.7f, 1f),
                        new Vector3(1.2f, 0.7f, 1.42f),
                        new Vector3(0.7f, 0.56f, 0.68f),
                        0.4f,
                        0.66f,
                        0.72f,
                        "A broad-backed reed prowler carrying three enclosed dusk lanterns.",
                        "Blue-black reed scales, fogged glass bladders, and violet resonance seams.",
                        "Measured hover pulses, independent lantern sway, and a coiling rudder tail."),
                    Stats(116, 13, 18, 5.6f, 11f, 3.55f),
                    83f,
                    79f,
                    "Reserved but dependable, preferring predictable signals and low voices.",
                    Work(PactWorkKind.Survey, 5, "Chooses dusk route checks where its lanterns remain useful."),
                    Work(PactWorkKind.Watch, 4, "Prefers quiet perimeter watches with a sheltered perch."),
                    Work(PactWorkKind.Waterkeeping, 3, "Enjoys marking safe reedwater depth at twilight.")),
                Species(
                    EmberhornBurrowerId,
                    "Emberhorn Burrower",
                    "of the Cooling Shelf",
                    "region.cinderwold",
                    "Cinderwold Verge",
                    "enemy.wild.cinderwold.emberhorn_burrower",
                    PactCombatRole.Vanguard,
                    PactResonanceAffinity.Ember,
                    Movement(
                        PactMovementKind.Grounded,
                        false,
                        1.4f,
                        "Drives across basalt shelves with a low, stable center."),
                    Art(
                        "art.pact.emberhorn_burrower",
                        PactArtBodyPlan.Burrower,
                        new Color(0.28f, 0.13f, 0.1f),
                        new Color(0.61f, 0.31f, 0.14f),
                        new Color(1f, 0.35f, 0.09f),
                        new Color(1f, 0.78f, 0.32f),
                        new Vector3(1.46f, 0.82f, 1.62f),
                        new Vector3(0.76f, 0.66f, 0.78f),
                        0.54f,
                        0.48f,
                        0.62f,
                        "A shovel-chested basalt burrower with paired heat-radiator horns.",
                        "Cooling lava plates, copper fault staples, and ember light confined below the shell.",
                        "Heavy alternating foresteps, shell compression, and heat-fin fanning after effort."),
                    Stats(158, 22, 24, 5.1f, 8f, 3.8f),
                    82f,
                    73f,
                    "Steady and protective, happiest after choosing a cool resting hollow.",
                    Work(PactWorkKind.Heatkeeping, 5, "Prefers tending controlled kiln warmth with regular cooling breaks."),
                    Work(PactWorkKind.Salvage, 4, "Volunteers to turn loose basalt where footing is secure."),
                    Work(PactWorkKind.Crafting, 3, "Enjoys compressing prepared clay and ashbrick.")),
                Species(
                    PrismtailKitelizardId,
                    "Prismtail Kitelizard",
                    "of the Glasswind",
                    "region.cinderwold",
                    "Cinderwold Verge",
                    "enemy.wild.cinderwold.prismtail_kitelizard",
                    PactCombatRole.Resonator,
                    PactResonanceAffinity.Prism,
                    Movement(
                        PactMovementKind.Gliding,
                        true,
                        1.35f,
                        "Rides hot crosswinds on a faceted tail sail."),
                    Art(
                        "art.pact.prismtail_kitelizard",
                        PactArtBodyPlan.Kitelizard,
                        new Color(0.3f, 0.2f, 0.24f),
                        new Color(0.65f, 0.42f, 0.38f),
                        new Color(0.86f, 0.48f, 0.94f),
                        new Color(0.92f, 0.9f, 1f),
                        new Vector3(1.22f, 0.5f, 1.52f),
                        new Vector3(0.58f, 0.48f, 0.66f),
                        0.3f,
                        1.34f,
                        0.42f,
                        "A lean cliff lizard with side-sails and a long faceted kite tail.",
                        "Smoke-satin scales, opaline tail panes, and magenta-gold refraction lines.",
                        "Quick wallward steps, sail tension changes, and a delayed ribbon-tail follow-through."),
                    Stats(98, 18, 11, 7.1f, 10f, 2.75f),
                    94f,
                    77f,
                    "Curious and social, often matching its resonance to a new companion.",
                    Work(PactWorkKind.Survey, 4, "Chooses glasswind readings from broad, unobstructed ledges."),
                    Work(PactWorkKind.Courier, 3, "Enjoys short ridge deliveries when return currents are marked."),
                    Work(PactWorkKind.Salvage, 2, "Volunteers to locate resonant glass without handling sharp fragments."))
            };

        private static readonly List<PactSpeciesDefinition> StoryDefinitions =
            new List<PactSpeciesDefinition>
            {
                StorySpecies(
                    StoryBrambletSpeciesId,
                    TetherfallPactCatalog.BrambletId,
                    "Bramblet",
                    "First Rootfriend",
                    PactCombatRole.Warden,
                    PactResonanceAffinity.Grove,
                    PactArtBodyPlan.LowQuadruped,
                    new Color(0.24f, 0.5f, 0.28f),
                    new Color(0.36f, 0.9f, 0.57f)),
                StorySpecies(
                    StorySlatebackSpeciesId,
                    TetherfallPactCatalog.SlatebackId,
                    "Slateback",
                    "First Ridgefriend",
                    PactCombatRole.Vanguard,
                    PactResonanceAffinity.Prism,
                    PactArtBodyPlan.Burrower,
                    new Color(0.34f, 0.4f, 0.5f),
                    new Color(0.28f, 0.88f, 0.78f)),
                StorySpecies(
                    StoryMistlingSpeciesId,
                    TetherfallPactCatalog.MistlingId,
                    "Mistling",
                    "First Rainfriend",
                    PactCombatRole.Mender,
                    PactResonanceAffinity.Tide,
                    PactArtBodyPlan.Finned,
                    new Color(0.48f, 0.76f, 0.8f),
                    new Color(0.33f, 0.96f, 0.93f)),
                StorySpecies(
                    StoryCinderwingSpeciesId,
                    TetherfallPactCatalog.CinderwingId,
                    "Cinderwing",
                    "First Sunfriend",
                    PactCombatRole.Skirmisher,
                    PactResonanceAffinity.Ember,
                    PactArtBodyPlan.Winged,
                    new Color(0.68f, 0.22f, 0.08f),
                    new Color(1f, 0.54f, 0.12f))
            };

        private static readonly List<PactSpeciesDefinition> AllDefinitions = BuildAll();
        private static readonly Dictionary<string, PactSpeciesDefinition> BySpeciesId =
            BuildSpeciesLookup();
        private static readonly Dictionary<string, PactSpeciesDefinition> ByEnemyId =
            BuildEnemyLookup();
        private static readonly Dictionary<string, PactSpeciesDefinition> ByLegendaryPactId =
            BuildLegendaryLookup();

        public static IReadOnlyList<PactSpeciesDefinition> All => AllDefinitions;
        public static IReadOnlyList<PactSpeciesDefinition> Wild => WildDefinitions;

        public static PactSpeciesDefinition Find(string speciesId)
        {
            return !string.IsNullOrEmpty(speciesId) &&
                   BySpeciesId.TryGetValue(speciesId, out var definition)
                ? definition
                : null;
        }

        public static PactSpeciesDefinition FindByEligibleEnemyId(string enemyId)
        {
            return !string.IsNullOrEmpty(enemyId) &&
                   ByEnemyId.TryGetValue(enemyId, out var definition)
                ? definition
                : null;
        }

        public static PactSpeciesDefinition FindByLegendaryPactId(string pactId)
        {
            return !string.IsNullOrEmpty(pactId) &&
                   ByLegendaryPactId.TryGetValue(pactId, out var definition)
                ? definition
                : null;
        }

        public static bool IsEligibleWildEnemyId(string enemyId)
        {
            var definition = FindByEligibleEnemyId(enemyId);
            return definition != null &&
                   definition.isWild &&
                   enemyId.StartsWith("enemy.wild.", StringComparison.Ordinal);
        }

        public static PactWorkAffinity FindWorkAffinity(
            PactSpeciesDefinition definition,
            PactWorkKind kind)
        {
            if (definition?.workAffinities == null)
            {
                return null;
            }

            for (var index = 0; index < definition.workAffinities.Length; index++)
            {
                var affinity = definition.workAffinities[index];
                if (affinity != null && affinity.kind == kind)
                {
                    return affinity;
                }
            }

            return null;
        }

        private static PactSpeciesDefinition Species(
            string speciesId,
            string displayName,
            string epithet,
            string regionId,
            string regionTheme,
            string enemyId,
            PactCombatRole role,
            PactResonanceAffinity affinity,
            PactMovementProfile movement,
            PactArtProfile art,
            PactBaseStats stats,
            float stamina,
            float morale,
            string temperament,
            params PactWorkAffinity[] work)
        {
            return new PactSpeciesDefinition
            {
                speciesId = speciesId,
                displayName = displayName,
                defaultEpithet = epithet,
                regionId = regionId,
                regionTheme = regionTheme,
                isWild = true,
                eligibleEnemyId = enemyId,
                linkedLegendaryPactId = string.Empty,
                combatRole = role,
                resonanceAffinity = affinity,
                movement = movement,
                art = art,
                baseStats = stats,
                workAffinities = work ?? Array.Empty<PactWorkAffinity>(),
                baseStamina = stamina,
                baseMorale = morale,
                fieldTemperament = temperament
            };
        }

        private static PactSpeciesDefinition StorySpecies(
            string speciesId,
            string pactId,
            string displayName,
            string epithet,
            PactCombatRole role,
            PactResonanceAffinity affinity,
            PactArtBodyPlan bodyPlan,
            Color surface,
            Color resonance)
        {
            return new PactSpeciesDefinition
            {
                speciesId = speciesId,
                displayName = displayName,
                defaultEpithet = epithet,
                regionId = "region.story_accord",
                regionTheme = "Established story Accord",
                isWild = false,
                eligibleEnemyId = string.Empty,
                linkedLegendaryPactId = pactId,
                combatRole = role,
                resonanceAffinity = affinity,
                movement = Movement(
                    bodyPlan == PactArtBodyPlan.Winged || bodyPlan == PactArtBodyPlan.Finned
                        ? PactMovementKind.Hovering
                        : PactMovementKind.Grounded,
                    bodyPlan == PactArtBodyPlan.Winged || bodyPlan == PactArtBodyPlan.Finned,
                    1.2f,
                    "Retains its established expedition movement."),
                art = Art(
                    $"art.pact.story.{displayName.ToLowerInvariant()}",
                    bodyPlan,
                    surface,
                    Color.Lerp(surface, Color.white, 0.24f),
                    resonance,
                    Color.Lerp(resonance, Color.white, 0.58f),
                    new Vector3(1.18f, 0.76f, 1.36f),
                    new Vector3(0.65f, 0.58f, 0.68f),
                    0.52f,
                    0.74f,
                    0.42f,
                    "A compact fallback silhouette used only until established story art is linked.",
                    "Keeps the established palette while remaining collider-free and clearly provisional.",
                    "Uses the same bounded gait, gaze, and tail articulation as field roster actors."),
                baseStats = Stats(112, 15, 15, 6.2f, 9f, 3.1f),
                workAffinities = new[]
                {
                    Work(PactWorkKind.Survey, 3, "May volunteer for short familiar route checks."),
                    Work(PactWorkKind.Care, 3, "Prefers shared camp tasks near trusted companions.")
                },
                baseStamina = 90f,
                baseMorale = 88f,
                fieldTemperament = "Established companion; its legendary bond remains authoritative elsewhere."
            };
        }

        private static PactMovementProfile Movement(
            PactMovementKind kind,
            bool flying,
            float spacing,
            string character)
        {
            return new PactMovementProfile
            {
                kind = kind,
                usesFlyingMotor = flying,
                formationSpacing = spacing,
                traversalCharacter = character
            };
        }

        private static PactArtProfile Art(
            string id,
            PactArtBodyPlan bodyPlan,
            Color surface,
            Color accent,
            Color resonance,
            Color eye,
            Vector3 bodyScale,
            Vector3 headScale,
            float legLength,
            float tailLength,
            float crestHeight,
            string silhouette,
            string material,
            string articulation)
        {
            return new PactArtProfile
            {
                artProfileId = id,
                bodyPlan = bodyPlan,
                surfaceColor = surface,
                accentColor = accent,
                resonanceColor = resonance,
                eyeColor = eye,
                bodyScale = bodyScale,
                headScale = headScale,
                legLength = legLength,
                tailLength = tailLength,
                crestHeight = crestHeight,
                silhouetteDirection = silhouette,
                materialDirection = material,
                articulationDirection = articulation
            };
        }

        private static PactBaseStats Stats(
            int vitality,
            int power,
            int guard,
            float speed,
            float assistRange,
            float assistCooldown)
        {
            return new PactBaseStats
            {
                vitality = vitality,
                power = power,
                guard = guard,
                speed = speed,
                assistRange = assistRange,
                assistCooldown = assistCooldown
            };
        }

        private static PactWorkAffinity Work(
            PactWorkKind kind,
            int rating,
            string preference)
        {
            return new PactWorkAffinity
            {
                kind = kind,
                rating = rating,
                voluntaryPreference = preference
            };
        }

        private static List<PactSpeciesDefinition> BuildAll()
        {
            var result = new List<PactSpeciesDefinition>(
                WildDefinitions.Count + StoryDefinitions.Count);
            result.AddRange(WildDefinitions);
            result.AddRange(StoryDefinitions);
            return result;
        }

        private static Dictionary<string, PactSpeciesDefinition> BuildSpeciesLookup()
        {
            var result =
                new Dictionary<string, PactSpeciesDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < AllDefinitions.Count; index++)
            {
                var definition = AllDefinitions[index];
                result.Add(definition.speciesId, definition);
            }

            return result;
        }

        private static Dictionary<string, PactSpeciesDefinition> BuildEnemyLookup()
        {
            var result =
                new Dictionary<string, PactSpeciesDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < WildDefinitions.Count; index++)
            {
                var definition = WildDefinitions[index];
                result.Add(definition.eligibleEnemyId, definition);
            }

            return result;
        }

        private static Dictionary<string, PactSpeciesDefinition> BuildLegendaryLookup()
        {
            var result =
                new Dictionary<string, PactSpeciesDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < StoryDefinitions.Count; index++)
            {
                var definition = StoryDefinitions[index];
                result.Add(definition.linkedLegendaryPactId, definition);
            }

            return result;
        }
    }

    [Serializable]
    public sealed class CapturedPactSaveData
    {
        public string instanceId;
        public string speciesId;
        public string nickname;
        public string epithet;
        public int level = 1;
        public int experience;
        public int bond;
        public float stamina;
        public float morale;
        public PactFieldSlotState fieldSlotState;
        public int fieldSlot = -1;
        public string settlementId;
        public string stationId;
        public string bedId;
        public string originId;
        public long accordFormedUtcTicks;
        public string linkedLegendaryPactId;

        public bool IsSettlementAssigned =>
            !string.IsNullOrEmpty(settlementId) ||
            !string.IsNullOrEmpty(stationId) ||
            !string.IsNullOrEmpty(bedId);

        public CapturedPactSaveData Clone()
        {
            return new CapturedPactSaveData
            {
                instanceId = instanceId,
                speciesId = speciesId,
                nickname = nickname,
                epithet = epithet,
                level = level,
                experience = experience,
                bond = bond,
                stamina = stamina,
                morale = morale,
                fieldSlotState = fieldSlotState,
                fieldSlot = fieldSlot,
                settlementId = settlementId,
                stationId = stationId,
                bedId = bedId,
                originId = originId,
                accordFormedUtcTicks = accordFormedUtcTicks,
                linkedLegendaryPactId = linkedLegendaryPactId
            };
        }
    }

    [Serializable]
    public sealed class PactRosterSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<CapturedPactSaveData> pacts = new List<CapturedPactSaveData>();
        public List<string> activeFieldInstanceIds = new List<string>();
        public string commandFocusInstanceId;
        public long lastAbstractUpdateUtcTicks;

        public PactRosterSaveData Clone()
        {
            var clone = new PactRosterSaveData
            {
                schemaVersion = schemaVersion,
                commandFocusInstanceId = commandFocusInstanceId,
                lastAbstractUpdateUtcTicks = lastAbstractUpdateUtcTicks
            };
            if (pacts != null)
            {
                for (var index = 0; index < pacts.Count; index++)
                {
                    if (pacts[index] != null)
                    {
                        clone.pacts.Add(pacts[index].Clone());
                    }
                }
            }

            if (activeFieldInstanceIds != null)
            {
                clone.activeFieldInstanceIds.AddRange(activeFieldInstanceIds);
            }

            return clone;
        }
    }

    public static class PactRosterRules
    {
        public const int AbsoluteMaximumFieldUnits = 5;
        public const float MaximumConcordMultiplier = 1.16f;
        private const float ConcordStepPerAdditionalAffinity = 0.04f;

        public static bool IsFieldEligible(CapturedPactSaveData record)
        {
            if (record == null || record.IsSettlementAssigned)
            {
                return false;
            }

            var definition = TetherfallAccordSpeciesCatalog.Find(record.speciesId);
            if (definition == null)
            {
                return false;
            }

            if (definition.isWild)
            {
                return string.IsNullOrEmpty(record.linkedLegendaryPactId);
            }

            if (string.IsNullOrEmpty(record.linkedLegendaryPactId) ||
                !string.Equals(
                    record.linkedLegendaryPactId,
                    definition.linkedLegendaryPactId,
                    StringComparison.Ordinal) ||
                string.Equals(
                    record.linkedLegendaryPactId,
                    TetherfallPactCatalog.SlatebackId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var legendary = TetherfallPactCatalog.Find(
                record.linkedLegendaryPactId);
            return legendary != null && !legendary.rideReady;
        }

        public static bool IsLinkedStoryRecord(CapturedPactSaveData record)
        {
            return record != null &&
                   !string.IsNullOrEmpty(record.linkedLegendaryPactId);
        }

        public static int GetMaximumActiveSlots(int clearRank)
        {
            if (clearRank >= 6)
            {
                return 5;
            }

            if (clearRank >= 4)
            {
                return 4;
            }

            return clearRank >= 2 ? 3 : 2;
        }

        public static float GetConcordMultiplier(PactRosterSaveData roster)
        {
            if (roster?.pacts == null || roster.activeFieldInstanceIds == null)
            {
                return 1f;
            }

            var affinityMask = 0;
            var uniqueAffinities = 0;
            for (var activeIndex = 0;
                 activeIndex < roster.activeFieldInstanceIds.Count;
                 activeIndex++)
            {
                var record = FindRecord(roster.pacts, roster.activeFieldInstanceIds[activeIndex]);
                var definition = record != null
                    ? TetherfallAccordSpeciesCatalog.Find(record.speciesId)
                    : null;
                if (definition == null)
                {
                    continue;
                }

                var affinityBit = 1 << (int)definition.resonanceAffinity;
                if ((affinityMask & affinityBit) != 0)
                {
                    continue;
                }

                affinityMask |= affinityBit;
                uniqueAffinities++;
            }

            return Mathf.Min(
                MaximumConcordMultiplier,
                1f + Mathf.Max(0, uniqueAffinities - 1) *
                ConcordStepPerAdditionalAffinity);
        }

        public static CapturedPactSaveData FindRecord(
            IReadOnlyList<CapturedPactSaveData> records,
            string instanceId)
        {
            if (records == null || string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                if (record != null &&
                    string.Equals(record.instanceId, instanceId, StringComparison.Ordinal))
                {
                    return record;
                }
            }

            return null;
        }

        public static string GetDisplayName(CapturedPactSaveData record)
        {
            if (record == null)
            {
                return "Unknown Echosworn";
            }

            var species = TetherfallAccordSpeciesCatalog.Find(record.speciesId);
            var name = !string.IsNullOrWhiteSpace(record.nickname)
                ? record.nickname
                : species?.displayName ?? "Unknown Echosworn";
            return !string.IsNullOrWhiteSpace(record.epithet)
                ? $"{name} {record.epithet}"
                : name;
        }

        public static void AdvanceAbstractReserves(
            PactRosterSaveData roster,
            long nowUtcTicks)
        {
            if (roster?.pacts == null || nowUtcTicks <= 0)
            {
                return;
            }

            if (roster.lastAbstractUpdateUtcTicks <= 0 ||
                roster.lastAbstractUpdateUtcTicks > nowUtcTicks)
            {
                roster.lastAbstractUpdateUtcTicks = nowUtcTicks;
                return;
            }

            var elapsedTicks = nowUtcTicks - roster.lastAbstractUpdateUtcTicks;
            var maximumTicks = TimeSpan.FromHours(12d).Ticks;
            elapsedTicks = Math.Min(elapsedTicks, maximumTicks);
            var elapsedMinutes = (float)(elapsedTicks / (double)TimeSpan.TicksPerMinute);
            if (elapsedMinutes < 0.25f)
            {
                return;
            }

            for (var index = 0; index < roster.pacts.Count; index++)
            {
                var record = roster.pacts[index];
                if (record == null || record.fieldSlotState == PactFieldSlotState.ActiveField)
                {
                    continue;
                }

                var bedComfort = !string.IsNullOrEmpty(record.bedId) ? 1.35f : 1f;
                record.stamina = Mathf.Clamp(
                    record.stamina + elapsedMinutes * 0.12f * bedComfort,
                    0f,
                    100f);
                record.morale = Mathf.Clamp(
                    record.morale + elapsedMinutes * 0.045f * bedComfort,
                    0f,
                    100f);
            }

            roster.lastAbstractUpdateUtcTicks = nowUtcTicks;
        }
    }

    public static class PactRosterNormalization
    {
        private const int MaximumTextLength = 80;
        private const int MaximumProgressionLevel = 100;

        public static void NormalizeInPlace(PactRosterSaveData roster, int clearRank)
        {
            if (roster == null)
            {
                return;
            }

            roster.pacts ??= new List<CapturedPactSaveData>();
            roster.activeFieldInstanceIds ??= new List<string>();
            roster.commandFocusInstanceId ??= string.Empty;
            roster.lastAbstractUpdateUtcTicks = Math.Max(0L, roster.lastAbstractUpdateUtcTicks);

            var recordIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < roster.pacts.Count; index++)
            {
                var record = roster.pacts[index];
                if (record == null)
                {
                    roster.pacts.RemoveAt(index--);
                    continue;
                }

                var definition = TetherfallAccordSpeciesCatalog.Find(record.speciesId);
                if (definition == null)
                {
                    roster.pacts.RemoveAt(index--);
                    continue;
                }

                NormalizeRecord(record, definition);
                if (string.IsNullOrEmpty(record.instanceId))
                {
                    record.instanceId = CreateMigratedInstanceId(record, index);
                }

                if (!recordIds.Add(record.instanceId))
                {
                    roster.pacts.RemoveAt(index--);
                }
            }

            for (var index = 0; index < roster.pacts.Count; index++)
            {
                var record = roster.pacts[index];
                record.fieldSlot = -1;
                record.fieldSlotState = record.IsSettlementAssigned
                    ? PactFieldSlotState.SettlementPreference
                    : PactFieldSlotState.Reserve;
            }

            var activeIds = new HashSet<string>(StringComparer.Ordinal);
            var maximumSlots = Mathf.Min(
                PactRosterRules.AbsoluteMaximumFieldUnits,
                PactRosterRules.GetMaximumActiveSlots(clearRank));
            for (var index = 0; index < roster.activeFieldInstanceIds.Count; index++)
            {
                var instanceId = roster.activeFieldInstanceIds[index];
                var record = PactRosterRules.FindRecord(roster.pacts, instanceId);
                if (string.IsNullOrEmpty(instanceId) ||
                    record == null ||
                    !PactRosterRules.IsFieldEligible(record) ||
                    !activeIds.Add(instanceId) ||
                    index >= maximumSlots)
                {
                    roster.activeFieldInstanceIds.RemoveAt(index--);
                    continue;
                }

                record.fieldSlotState = PactFieldSlotState.ActiveField;
                record.fieldSlot = index;
            }

            if (!activeIds.Contains(roster.commandFocusInstanceId))
            {
                roster.commandFocusInstanceId =
                    roster.activeFieldInstanceIds.Count > 0
                        ? roster.activeFieldInstanceIds[0]
                        : string.Empty;
            }

            roster.schemaVersion = PactRosterSaveData.CurrentSchemaVersion;
        }

        public static CapturedPactSaveData CreateWildAccordRecord(
            PactSpeciesDefinition species,
            string originId,
            long accordUtcTicks)
        {
            if (species == null || !species.isWild)
            {
                return null;
            }

            return new CapturedPactSaveData
            {
                instanceId = $"accord.{Guid.NewGuid():N}",
                speciesId = species.speciesId,
                nickname = string.Empty,
                epithet = species.defaultEpithet,
                level = 1,
                experience = 0,
                bond = 10,
                stamina = Mathf.Clamp(species.baseStamina, 0f, 100f),
                morale = Mathf.Clamp(species.baseMorale, 0f, 100f),
                fieldSlotState = PactFieldSlotState.Reserve,
                fieldSlot = -1,
                settlementId = string.Empty,
                stationId = string.Empty,
                bedId = string.Empty,
                originId = string.IsNullOrWhiteSpace(originId)
                    ? "origin.wild.unspecified"
                    : originId.Trim(),
                accordFormedUtcTicks = Math.Max(0L, accordUtcTicks),
                linkedLegendaryPactId = string.Empty
            };
        }

        private static void NormalizeRecord(
            CapturedPactSaveData record,
            PactSpeciesDefinition definition)
        {
            record.instanceId = NormalizeId(record.instanceId);
            record.speciesId = definition.speciesId;
            record.nickname = NormalizeText(record.nickname);
            record.epithet = NormalizeText(record.epithet);
            if (string.IsNullOrEmpty(record.epithet))
            {
                record.epithet = definition.defaultEpithet;
            }

            record.level = Mathf.Clamp(record.level, 1, MaximumProgressionLevel);
            record.experience = Mathf.Max(0, record.experience);
            record.bond = Mathf.Clamp(record.bond, 0, 100);
            record.stamina = Mathf.Clamp(IsFinite(record.stamina) ? record.stamina : 0f, 0f, 100f);
            record.morale = Mathf.Clamp(IsFinite(record.morale) ? record.morale : 0f, 0f, 100f);
            record.settlementId = NormalizeId(record.settlementId);
            record.stationId = NormalizeId(record.stationId);
            record.bedId = NormalizeId(record.bedId);
            record.originId = NormalizeId(record.originId);
            if (string.IsNullOrEmpty(record.originId))
            {
                record.originId = "origin.unknown";
            }

            record.accordFormedUtcTicks = Math.Max(0L, record.accordFormedUtcTicks);
            record.linkedLegendaryPactId = definition.isWild
                ? string.Empty
                : definition.linkedLegendaryPactId;
            if (string.IsNullOrEmpty(record.settlementId))
            {
                record.stationId = string.Empty;
                record.bedId = string.Empty;
            }
        }

        private static string CreateMigratedInstanceId(
            CapturedPactSaveData record,
            int sourceIndex)
        {
            unchecked
            {
                var hash = 1469598103934665603UL;
                AppendHash(ref hash, record.speciesId);
                AppendHash(ref hash, record.originId);
                AppendHash(ref hash, record.accordFormedUtcTicks.ToString());
                AppendHash(ref hash, sourceIndex.ToString());
                return $"accord.migrated.{hash:x16}";
            }
        }

        private static void AppendHash(ref ulong hash, string value)
        {
            if (value == null)
            {
                return;
            }

            for (var index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= 1099511628211UL;
            }
        }

        private static string NormalizeId(string value)
        {
            value = value?.Trim() ?? string.Empty;
            return value.Length <= MaximumTextLength
                ? value
                : value.Substring(0, MaximumTextLength);
        }

        private static string NormalizeText(string value)
        {
            value = value?.Trim() ?? string.Empty;
            return value.Length <= MaximumTextLength
                ? value
                : value.Substring(0, MaximumTextLength);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public static class PactRosterMigration
    {
        public static int SeedCurrentStoryPacts(
            PactRosterSaveData roster,
            IReadOnlyList<PactProgressSaveData> legendaryProgress,
            IReadOnlyList<string> establishedPactNames,
            long migrationUtcTicks)
        {
            if (roster == null)
            {
                return 0;
            }

            roster.pacts ??= new List<CapturedPactSaveData>();
            var added = 0;
            if (legendaryProgress != null)
            {
                for (var index = 0; index < legendaryProgress.Count; index++)
                {
                    var progress = legendaryProgress[index];
                    var definition = progress != null
                        ? TetherfallAccordSpeciesCatalog.FindByLegendaryPactId(progress.pactId)
                        : null;
                    if (definition != null &&
                        TrySeedStoryReference(roster, definition, migrationUtcTicks))
                    {
                        added++;
                    }
                }
            }

            if (establishedPactNames != null)
            {
                for (var index = 0; index < establishedPactNames.Count; index++)
                {
                    var pact = TetherfallPactCatalog.FindByCreatureName(
                        establishedPactNames[index]);
                    var definition = pact != null
                        ? TetherfallAccordSpeciesCatalog.FindByLegendaryPactId(pact.id)
                        : null;
                    if (definition != null &&
                        TrySeedStoryReference(roster, definition, migrationUtcTicks))
                    {
                        added++;
                    }
                }
            }

            return added;
        }

        public static CapturedPactSaveData EnsureStoryPactRecord(
            PactRosterSaveData roster,
            string legendaryPactId,
            long accordUtcTicks,
            out bool added)
        {
            added = false;
            if (roster == null ||
                string.IsNullOrEmpty(legendaryPactId) ||
                string.Equals(
                    legendaryPactId,
                    TetherfallPactCatalog.SlatebackId,
                    StringComparison.Ordinal))
            {
                return null;
            }

            var legendary = TetherfallPactCatalog.Find(legendaryPactId);
            var definition =
                TetherfallAccordSpeciesCatalog.FindByLegendaryPactId(
                    legendaryPactId);
            if (legendary == null || legendary.rideReady || definition == null)
            {
                return null;
            }

            roster.pacts ??= new List<CapturedPactSaveData>();
            for (var index = 0; index < roster.pacts.Count; index++)
            {
                var existing = roster.pacts[index];
                if (existing != null &&
                    (string.Equals(
                         existing.linkedLegendaryPactId,
                         legendaryPactId,
                         StringComparison.Ordinal) ||
                     string.Equals(
                         existing.instanceId,
                         $"accord.story.{legendaryPactId}",
                         StringComparison.Ordinal)))
                {
                    return existing;
                }
            }

            var record = CreateStoryRecord(
                definition,
                accordUtcTicks,
                "origin.story.runtime");
            roster.pacts.Add(record);
            added = true;
            return record;
        }

        private static bool TrySeedStoryReference(
            PactRosterSaveData roster,
            PactSpeciesDefinition definition,
            long migrationUtcTicks)
        {
            if (definition == null ||
                string.Equals(
                    definition.linkedLegendaryPactId,
                    TetherfallPactCatalog.SlatebackId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var legendary = TetherfallPactCatalog.Find(
                definition.linkedLegendaryPactId);
            if (legendary == null || legendary.rideReady)
            {
                return false;
            }

            for (var index = 0; index < roster.pacts.Count; index++)
            {
                var existing = roster.pacts[index];
                if (existing != null &&
                    (string.Equals(
                         existing.linkedLegendaryPactId,
                         definition.linkedLegendaryPactId,
                         StringComparison.Ordinal) ||
                     string.Equals(
                         existing.instanceId,
                         $"accord.story.{definition.linkedLegendaryPactId}",
                         StringComparison.Ordinal)))
                {
                    return false;
                }
            }

            roster.pacts.Add(CreateStoryRecord(
                definition,
                migrationUtcTicks,
                "origin.story.migration"));
            return true;
        }

        private static CapturedPactSaveData CreateStoryRecord(
            PactSpeciesDefinition definition,
            long migrationUtcTicks,
            string originId)
        {
            return new CapturedPactSaveData
            {
                instanceId = $"accord.story.{definition.linkedLegendaryPactId}",
                speciesId = definition.speciesId,
                nickname = string.Empty,
                epithet = definition.defaultEpithet,
                level = 1,
                experience = 0,
                bond = 0,
                stamina = definition.baseStamina,
                morale = definition.baseMorale,
                fieldSlotState = PactFieldSlotState.Reserve,
                fieldSlot = -1,
                settlementId = string.Empty,
                stationId = string.Empty,
                bedId = string.Empty,
                originId = originId,
                accordFormedUtcTicks = Math.Max(0L, migrationUtcTicks),
                linkedLegendaryPactId = definition.linkedLegendaryPactId
            };
        }
    }

    /// <summary>
    /// Keeps this additive pass compilable before TetherfallSaveData gains its serialized
    /// pactRoster member. Once that member exists, the same hooks discover and use it.
    /// The weak sidecar is intentionally only an edit/runtime bridge, not disk persistence.
    /// </summary>
    public static class PactRosterSaveBridge
    {
        private const string SerializedMemberName = "pactRoster";
        private static readonly FieldInfo SerializedField = typeof(TetherfallSaveData).GetField(
            SerializedMemberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly PropertyInfo SerializedProperty = typeof(TetherfallSaveData).GetProperty(
            SerializedMemberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly ConditionalWeakTable<TetherfallSaveData, PactRosterSaveData>
            TransientSidecars =
                new ConditionalWeakTable<TetherfallSaveData, PactRosterSaveData>();

        public static bool HasSerializedMember =>
            (SerializedField != null &&
             typeof(PactRosterSaveData).IsAssignableFrom(SerializedField.FieldType)) ||
            (SerializedProperty != null &&
             SerializedProperty.CanRead &&
             SerializedProperty.CanWrite &&
             typeof(PactRosterSaveData).IsAssignableFrom(SerializedProperty.PropertyType));

        public static void Write(TetherfallSaveData owner, PactRosterSaveData roster)
        {
            if (owner == null || roster == null)
            {
                return;
            }

            var snapshot = roster.Clone();
            if (TryWriteSerialized(owner, snapshot))
            {
                return;
            }

            TransientSidecars.Remove(owner);
            TransientSidecars.Add(owner, snapshot);
        }

        public static bool TryRead(
            TetherfallSaveData owner,
            out PactRosterSaveData roster)
        {
            roster = null;
            if (owner == null)
            {
                return false;
            }

            if (TryReadSerialized(owner, out roster))
            {
                roster = roster.Clone();
                return true;
            }

            if (TransientSidecars.TryGetValue(owner, out var transient) &&
                transient != null)
            {
                roster = transient.Clone();
                return true;
            }

            return false;
        }

        private static bool TryWriteSerialized(
            TetherfallSaveData owner,
            PactRosterSaveData roster)
        {
            if (SerializedField != null &&
                typeof(PactRosterSaveData).IsAssignableFrom(SerializedField.FieldType))
            {
                SerializedField.SetValue(owner, roster);
                return true;
            }

            if (SerializedProperty != null &&
                SerializedProperty.CanWrite &&
                typeof(PactRosterSaveData).IsAssignableFrom(SerializedProperty.PropertyType))
            {
                SerializedProperty.SetValue(owner, roster);
                return true;
            }

            return false;
        }

        private static bool TryReadSerialized(
            TetherfallSaveData owner,
            out PactRosterSaveData roster)
        {
            roster = null;
            if (SerializedField != null &&
                typeof(PactRosterSaveData).IsAssignableFrom(SerializedField.FieldType))
            {
                roster = SerializedField.GetValue(owner) as PactRosterSaveData;
                return roster != null;
            }

            if (SerializedProperty != null &&
                SerializedProperty.CanRead &&
                typeof(PactRosterSaveData).IsAssignableFrom(SerializedProperty.PropertyType))
            {
                roster = SerializedProperty.GetValue(owner) as PactRosterSaveData;
                return roster != null;
            }

            return false;
        }
    }

    public sealed partial class TetherfallGame
    {
        public const int PactActorBudget = PactRosterRules.AbsoluteMaximumFieldUnits;
        public const float PactActorActivationDistance = 46f;
        public const int ThreadbindAccordExperienceReward = 40;
        private const float PactActorActivationDistanceSquared =
            PactActorActivationDistance * PactActorActivationDistance;
        private const float PactRosterUpdateInterval = 0.2f;
        private const float PactAbstractUpdateInterval = 10f;
        private const float SafeSettlementRadiusSquared = 34f * 34f;

        private static readonly Vector3[] PactFormationOffsets =
        {
            new Vector3(-1.55f, 0f, -1.7f),
            new Vector3(1.55f, 0f, -1.7f),
            new Vector3(-2.45f, 0f, -3.05f),
            new Vector3(2.45f, 0f, -3.05f),
            new Vector3(0f, 0f, -4.15f)
        };

        private PactRosterSaveData pactRosterState = new PactRosterSaveData();
        private readonly Dictionary<string, CapturedPactActor> pactFieldActors =
            new Dictionary<string, CapturedPactActor>(StringComparer.Ordinal);
        private readonly List<string> pactActorRemovalBuffer = new List<string>(PactActorBudget);
        private bool pactRosterInitialized;
        private bool showingPactRosterCamp;
        private int pactRosterUiPage;
        private int activeStoryPactActorCount;
        private float nextPactRosterUpdateAt;
        private float nextPactAbstractUpdateAt;

        public IReadOnlyList<CapturedPactSaveData> PactRoster =>
            pactRosterState.pacts;
        public IReadOnlyList<string> ActivePactFieldIds =>
            pactRosterState.activeFieldInstanceIds;
        public string PactCommandFocusInstanceId =>
            pactRosterState.commandFocusInstanceId;
        public int MaximumActivePactSlots =>
            PactRosterRules.GetMaximumActiveSlots(Level);
        public float PactConcordMultiplier =>
            PactRosterRules.GetConcordMultiplier(pactRosterState);
        public int ActiveCombatPactCount =>
            pactRosterState?.activeFieldInstanceIds?.Count ?? 0;
        public int SimulatedPactActorCount =>
            pactFieldActors.Count + activeStoryPactActorCount;

        public void InitializePactRosterSystem()
        {
            if (pactRosterState == null)
            {
                pactRosterState = new PactRosterSaveData();
            }

            PactRosterNormalization.NormalizeInPlace(pactRosterState, Level);
            PactRosterRules.AdvanceAbstractReserves(
                pactRosterState,
                DateTime.UtcNow.Ticks);
            pactRosterInitialized = true;
            nextPactRosterUpdateAt = 0f;
            nextPactAbstractUpdateAt = Time.time + PactAbstractUpdateInterval;
            SynchronizePactFieldActors();
        }

        public void UpdatePactRosterSystem()
        {
            if (!pactRosterInitialized)
            {
                InitializePactRosterSystem();
            }

            if (Time.time < nextPactRosterUpdateAt)
            {
                return;
            }

            nextPactRosterUpdateAt = Time.time + PactRosterUpdateInterval;
            if (Time.time >= nextPactAbstractUpdateAt)
            {
                nextPactAbstractUpdateAt = Time.time + PactAbstractUpdateInterval;
                PactRosterRules.AdvanceAbstractReserves(
                    pactRosterState,
                    DateTime.UtcNow.Ticks);
            }

            SynchronizePactFieldActors();
            if (PlayerTransform == null)
            {
                return;
            }

            var playerPosition = PlayerTransform.position;
            var insideSafeSettlement = IsInsidePactSafeSettlement(playerPosition);
            for (var slot = 0; slot < pactRosterState.activeFieldInstanceIds.Count; slot++)
            {
                var instanceId = pactRosterState.activeFieldInstanceIds[slot];
                var record = PactRosterRules.FindRecord(
                    pactRosterState.pacts,
                    instanceId);
                if (record == null || !PactRosterRules.IsFieldEligible(record))
                {
                    continue;
                }

                var storyActor = PactRosterRules.IsLinkedStoryRecord(record)
                    ? FindPactCreatureByStableId(record.linkedLegendaryPactId)
                    : null;
                if (storyActor != null)
                {
                    UpdateStoryPactFieldSimulation(
                        storyActor,
                        record,
                        slot,
                        playerPosition,
                        insideSafeSettlement);
                    continue;
                }

                if (!pactFieldActors.TryGetValue(instanceId, out var actor) ||
                    actor == null)
                {
                    continue;
                }

                actor.SetFieldSlot(slot);
                var delta = actor.transform.position - playerPosition;
                if (delta.sqrMagnitude > PactActorActivationDistanceSquared)
                {
                    if (!insideSafeSettlement)
                    {
                        actor.TryRecallToFormation(GetPactFormationPosition(slot));
                        delta = actor.transform.position - playerPosition;
                        if (delta.sqrMagnitude > PactActorActivationDistanceSquared)
                        {
                            actor.SetSimulationActive(false);
                            continue;
                        }
                    }
                    else
                    {
                        actor.SetSimulationActive(false);
                        continue;
                    }
                }

                actor.SetSimulationActive(true);
            }
        }

        public void WritePactRosterSave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (!pactRosterInitialized)
            {
                InitializePactRosterSystem();
            }

            PactRosterRules.AdvanceAbstractReserves(
                pactRosterState,
                DateTime.UtcNow.Ticks);
            PactRosterNormalization.NormalizeInPlace(pactRosterState, Level);
            PactRosterSaveBridge.Write(data, pactRosterState);
        }

        public void RestorePactRosterState(TetherfallSaveData data)
        {
            DestroyAllPactFieldActors();
            if (!PactRosterSaveBridge.TryRead(data, out pactRosterState))
            {
                pactRosterState = new PactRosterSaveData();
            }

            pactRosterState ??= new PactRosterSaveData();
            PactRosterNormalization.NormalizeInPlace(
                pactRosterState,
                data != null ? Mathf.Max(1, data.level) : Level);
            var hadPersistedRosterRecords = pactRosterState.pacts.Count > 0;
            EnsureEstablishedStoryPactRecords(true);
            if (!hadPersistedRosterRecords &&
                data != null &&
                !string.IsNullOrEmpty(data.activePactName))
            {
                var legacyFocus = FindPactCreature(data.activePactName);
                var legacyRecord = legacyFocus != null
                    ? FindStoryPactRosterRecord(legacyFocus.PactId)
                    : null;
                if (legacyRecord != null)
                {
                    if (!ContainsActivePactId(legacyRecord.instanceId))
                    {
                        if (pactRosterState.activeFieldInstanceIds.Count >=
                            MaximumActivePactSlots)
                        {
                            pactRosterState.activeFieldInstanceIds.RemoveAt(
                                pactRosterState.activeFieldInstanceIds.Count -
                                1);
                        }

                        pactRosterState.activeFieldInstanceIds.Add(
                            legacyRecord.instanceId);
                    }

                    pactRosterState.commandFocusInstanceId =
                        legacyRecord.instanceId;
                }
            }

            PactRosterNormalization.NormalizeInPlace(
                pactRosterState,
                data != null ? Mathf.Max(1, data.level) : Level);
            PactRosterRules.AdvanceAbstractReserves(
                pactRosterState,
                DateTime.UtcNow.Ticks);
            pactRosterInitialized = true;
            nextPactRosterUpdateAt = 0f;
            nextPactAbstractUpdateAt = Time.time + PactAbstractUpdateInterval;
            SynchronizePactFieldActors();
        }

        public bool TryAddPactToField(string instanceId, out string reason)
        {
            EnsurePactRosterInitialized();
            var record = PactRosterRules.FindRecord(pactRosterState.pacts, instanceId);
            if (record == null)
            {
                reason = "That Accord is not in the roster.";
                return false;
            }

            if (record.IsSettlementAssigned)
            {
                reason = "This Echosworn chose a settlement preference and remains there.";
                return false;
            }

            if (!PactRosterRules.IsFieldEligible(record))
            {
                reason = string.Equals(
                    record.linkedLegendaryPactId,
                    TetherfallPactCatalog.SlatebackId,
                    StringComparison.Ordinal)
                    ? "Slateback remains the separate expedition mount."
                    : "That Accord cannot take a combat field slot.";
                return false;
            }

            if (ContainsActivePactId(instanceId))
            {
                pactRosterState.commandFocusInstanceId = instanceId;
                SynchronizePactCommandFocusAlias();
                RequestSave();
                reason = "Already active; command focus updated.";
                return true;
            }

            if (pactRosterState.activeFieldInstanceIds.Count >= MaximumActivePactSlots)
            {
                reason = $"Clear rank {Level} supports {MaximumActivePactSlots} active Accords.";
                return false;
            }

            pactRosterState.activeFieldInstanceIds.Add(instanceId);
            pactRosterState.commandFocusInstanceId = instanceId;
            PactRosterNormalization.NormalizeInPlace(pactRosterState, Level);
            SynchronizePactFieldActors();
            RequestSave();
            reason = "Joined the field formation.";
            return true;
        }

        public bool AddPactToField(string instanceId)
        {
            return TryAddPactToField(instanceId, out _);
        }

        public bool RemovePactFromField(string instanceId)
        {
            EnsurePactRosterInitialized();
            var removed = false;
            for (var index = pactRosterState.activeFieldInstanceIds.Count - 1;
                 index >= 0;
                 index--)
            {
                if (!string.Equals(
                        pactRosterState.activeFieldInstanceIds[index],
                        instanceId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                pactRosterState.activeFieldInstanceIds.RemoveAt(index);
                removed = true;
            }

            if (!removed)
            {
                return false;
            }

            PactRosterNormalization.NormalizeInPlace(pactRosterState, Level);
            SynchronizePactFieldActors();
            RequestSave();
            return true;
        }

        public bool SetPactCommandFocus(string instanceId)
        {
            EnsurePactRosterInitialized();
            if (!ContainsActivePactId(instanceId))
            {
                return false;
            }

            pactRosterState.commandFocusInstanceId = instanceId;
            SynchronizePactCommandFocusAlias();
            RequestSave();
            return true;
        }

        internal bool EnsureStoryPactInRoster(
            PactCreature pact,
            bool activateIfCapacity,
            bool activateOnlyWhenAdded = false)
        {
            if (pact == null ||
                !pact.IsPacted ||
                pact.IsMount ||
                string.Equals(
                    pact.PactId,
                    TetherfallPactCatalog.SlatebackId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            EnsurePactRosterInitialized();
            var record = PactRosterMigration.EnsureStoryPactRecord(
                pactRosterState,
                pact.PactId,
                DateTime.UtcNow.Ticks,
                out var added);
            if (record == null)
            {
                return false;
            }

            var mayActivate =
                activateIfCapacity &&
                (added || !activateOnlyWhenAdded) &&
                !ContainsActivePactId(record.instanceId) &&
                pactRosterState.activeFieldInstanceIds.Count <
                MaximumActivePactSlots;
            if (mayActivate)
            {
                pactRosterState.activeFieldInstanceIds.Add(record.instanceId);
                pactRosterState.commandFocusInstanceId = record.instanceId;
            }

            PactRosterNormalization.NormalizeInPlace(pactRosterState, Level);
            SynchronizePactFieldActors();
            return ContainsActivePactId(record.instanceId);
        }

        public bool CyclePactCommandFocus()
        {
            EnsurePactRosterInitialized();
            var activeCount = pactRosterState.activeFieldInstanceIds.Count;
            if (activeCount == 0)
            {
                Notify("No combat Pact is assigned to the field roster.");
                return false;
            }

            var currentIndex = -1;
            for (var index = 0; index < activeCount; index++)
            {
                if (string.Equals(
                        pactRosterState.activeFieldInstanceIds[index],
                        pactRosterState.commandFocusInstanceId,
                        StringComparison.Ordinal))
                {
                    currentIndex = index;
                    break;
                }
            }

            var nextIndex = (currentIndex + 1 + activeCount) % activeCount;
            var nextId = pactRosterState.activeFieldInstanceIds[nextIndex];
            pactRosterState.commandFocusInstanceId = nextId;
            SynchronizePactCommandFocusAlias();
            var nextRecord = PactRosterRules.FindRecord(
                pactRosterState.pacts,
                nextId);
            Notify(
                $"{PactRosterRules.GetDisplayName(nextRecord)} is now command focus.");
            RequestSave();
            return true;
        }

        internal bool TryGetActivePactActor(
            int slot,
            out CapturedPactSaveData record,
            out PactCreature storyActor,
            out CapturedPactActor capturedActor)
        {
            record = null;
            storyActor = null;
            capturedActor = null;
            if (pactRosterState?.activeFieldInstanceIds == null ||
                slot < 0 ||
                slot >= pactRosterState.activeFieldInstanceIds.Count)
            {
                return false;
            }

            var instanceId = pactRosterState.activeFieldInstanceIds[slot];
            record = PactRosterRules.FindRecord(
                pactRosterState.pacts,
                instanceId);
            if (record == null || !PactRosterRules.IsFieldEligible(record))
            {
                return false;
            }

            if (PactRosterRules.IsLinkedStoryRecord(record))
            {
                storyActor = FindPactCreatureByStableId(
                    record.linkedLegendaryPactId);
                return storyActor != null &&
                       storyActor.IsPacted &&
                       storyActor.IsFieldActive &&
                       storyActor.isActiveAndEnabled;
            }

            if (!pactFieldActors.TryGetValue(
                    instanceId,
                    out capturedActor) ||
                capturedActor == null ||
                !capturedActor.isActiveAndEnabled)
            {
                capturedActor = null;
                return false;
            }

            return true;
        }

        internal CapturedPactSaveData GetActivePactRecord(int slot)
        {
            if (pactRosterState?.activeFieldInstanceIds == null ||
                slot < 0 ||
                slot >= pactRosterState.activeFieldInstanceIds.Count)
            {
                return null;
            }

            return PactRosterRules.FindRecord(
                pactRosterState.pacts,
                pactRosterState.activeFieldInstanceIds[slot]);
        }

        internal int CountAvailableActivePactActors()
        {
            var count = 0;
            for (var slot = 0; slot < ActiveCombatPactCount; slot++)
            {
                if (TryGetActivePactActor(
                        slot,
                        out _,
                        out _,
                        out _))
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryAssignPactSettlementPreference(
            string instanceId,
            string settlementId,
            string stationId,
            string bedId,
            PactWorkKind preferredWork,
            out string reason)
        {
            EnsurePactRosterInitialized();
            var record = PactRosterRules.FindRecord(pactRosterState.pacts, instanceId);
            var definition = record != null
                ? TetherfallAccordSpeciesCatalog.Find(record.speciesId)
                : null;
            var affinity = TetherfallAccordSpeciesCatalog.FindWorkAffinity(
                definition,
                preferredWork);
            if (record == null || definition == null)
            {
                reason = "That Accord is not in the roster.";
                return false;
            }

            if (affinity == null || affinity.rating <= 0)
            {
                reason = $"{definition.displayName} has not expressed that work preference.";
                return false;
            }

            if (record.morale < 25f)
            {
                reason = $"{definition.displayName} prefers rest before choosing settlement work.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(settlementId) ||
                string.IsNullOrWhiteSpace(stationId))
            {
                reason = "A settlement and preferred station are both required.";
                return false;
            }

            RemovePactFromField(instanceId);
            record.settlementId = settlementId.Trim();
            record.stationId = stationId.Trim();
            record.bedId = bedId?.Trim() ?? string.Empty;
            record.fieldSlotState = PactFieldSlotState.SettlementPreference;
            record.fieldSlot = -1;
            SynchronizePactFieldActors();
            RequestSave();
            reason = affinity.voluntaryPreference;
            return true;
        }

        public bool ClearPactSettlementPreference(string instanceId)
        {
            EnsurePactRosterInitialized();
            var record = PactRosterRules.FindRecord(pactRosterState.pacts, instanceId);
            if (record == null || !record.IsSettlementAssigned)
            {
                return false;
            }

            record.settlementId = string.Empty;
            record.stationId = string.Empty;
            record.bedId = string.Empty;
            record.fieldSlotState = PactFieldSlotState.Reserve;
            RequestSave();
            return true;
        }

        internal bool AcceptThreadbindAccord(
            PactSpeciesDefinition species,
            string originId,
            out CapturedPactSaveData record)
        {
            EnsurePactRosterInitialized();
            record = PactRosterNormalization.CreateWildAccordRecord(
                species,
                originId,
                DateTime.UtcNow.Ticks);
            if (record == null)
            {
                return false;
            }

            if (!TryInsertThreadbindAccordRecord(record))
            {
                record = null;
                return false;
            }

            var joinedField = TryAddPactToField(record.instanceId, out _);
            OnPactFormed();
            GainExperience(ThreadbindAccordExperienceReward);
            RecordTutorialEvent(TutorialEvent.FormPact);
            if (!joinedField)
            {
                RequestSave();
            }

            return true;
        }

        private bool TryInsertThreadbindAccordRecord(
            CapturedPactSaveData record)
        {
            if (record == null ||
                string.IsNullOrEmpty(record.instanceId) ||
                PactRosterRules.FindRecord(
                    pactRosterState.pacts,
                    record.instanceId) != null)
            {
                return false;
            }

            pactRosterState.pacts.Add(record);
            PactRosterNormalization.NormalizeInPlace(pactRosterState, Level);
            return ReferenceEquals(
                PactRosterRules.FindRecord(
                    pactRosterState.pacts,
                    record.instanceId),
                record);
        }

        internal Vector3 GetPactFormationPosition(int slot)
        {
            if (PlayerTransform == null)
            {
                return transform.position;
            }

            slot = Mathf.Clamp(slot, 0, PactFormationOffsets.Length - 1);
            var offset = PactFormationOffsets[slot];
            return PlayerTransform.position +
                   PlayerTransform.right * offset.x +
                   PlayerTransform.forward * offset.z;
        }

        public void RecallPactRosterToFormation(bool immediate = true)
        {
            if (!pactRosterInitialized || PlayerTransform == null)
            {
                return;
            }

            SynchronizePactFieldActors();
            for (var slot = 0; slot < ActiveCombatPactCount; slot++)
            {
                var record = GetActivePactRecord(slot);
                if (record == null)
                {
                    continue;
                }

                var destination = GetPactFormationPosition(slot);
                if (PactRosterRules.IsLinkedStoryRecord(record))
                {
                    var storyActor = FindPactCreatureByStableId(
                        record.linkedLegendaryPactId);
                    if (storyActor != null)
                    {
                        storyActor.SetFieldSimulationActive(true);
                        storyActor.TryRecallToFormation(
                            destination,
                            immediate);
                    }

                    continue;
                }

                if (pactFieldActors.TryGetValue(
                        record.instanceId,
                        out var actor) &&
                    actor != null)
                {
                    actor.SetSimulationActive(true);
                    actor.TryRecallToFormation(destination, immediate);
                }
            }
        }

        internal bool IsInsidePactSafeSettlement(Vector3 position)
        {
            if (PlanarDistanceSquared(
                    position,
                    CityCatalog.AlderwakeHearthmarket.center) <=
                SafeSettlementRadiusSquared ||
                PlanarDistanceSquared(
                    position,
                    CityCatalog.VeyrwaterMooring.center) <=
                SafeSettlementRadiusSquared)
            {
                return true;
            }

            if (!HasClaimedPlayerSettlement)
            {
                return false;
            }

            var safeRadius =
                TetherfallSettlementRules.GetBuildRadius(
                    PlayerSettlementCoreTier) + 8f;
            return PlanarDistanceSquared(
                       position,
                       GetPlayerSettlementCorePosition()) <=
                   safeRadius * safeRadius;
        }

        internal bool IsPactCommandFocus(string instanceId)
        {
            return string.Equals(
                pactRosterState.commandFocusInstanceId,
                instanceId,
                StringComparison.Ordinal);
        }

        public void DrawPactRosterSection(Rect rect)
        {
            EnsurePactRosterInitialized();
            DrawPanel(rect, "THREADBIND ACCORD ROSTER");
            var body = bodyStyle ?? GUI.skin.label;
            var muted = mutedStyle ?? GUI.skin.label;
            var focusBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(
                    TetherfallInputAction.RotateSupportPactOrRallyBeat1);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 35f, rect.width - 28f, 54f),
                $"Field Pacts fight and answer commands; reserves remain actor-free. {focusBinding} cycles command " +
                "focus; traversal/form passives remain focus-only. Diverse affinities raise capped Concord. " +
                $"Field {ActiveCombatPactCount}/{MaximumActivePactSlots} (cap 5) · " +
                $"Concord x{PactConcordMultiplier:0.00}",
                body);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 89f, rect.width - 28f, 20f),
                "Field slots: rank 1 = 2 · rank 2 = 3 · rank 4 = 4 · rank 6 = 5",
                muted);

            var y = rect.y + 114f;
            var rowHeight = 46f;
            var availableHeight = Mathf.Max(
                rowHeight,
                rect.yMax - 8f - y);
            var rowsWithoutFooter = Mathf.Max(
                1,
                Mathf.FloorToInt(availableHeight / rowHeight));
            var paged = pactRosterState.pacts.Count > rowsWithoutFooter;
            var rowsPerPage = paged
                ? Mathf.Max(
                    1,
                    Mathf.FloorToInt(
                        (availableHeight - 38f) / rowHeight))
                : rowsWithoutFooter;
            var pageCount = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    pactRosterState.pacts.Count /
                    (float)rowsPerPage));
            pactRosterUiPage = Mathf.Clamp(
                pactRosterUiPage,
                0,
                pageCount - 1);
            var firstRecord = pactRosterUiPage * rowsPerPage;
            var lastRecord = Mathf.Min(
                pactRosterState.pacts.Count,
                firstRecord + rowsPerPage);
            for (var index = firstRecord; index < lastRecord; index++)
            {
                var record = pactRosterState.pacts[index];
                if (record == null)
                {
                    continue;
                }

                var definition = TetherfallAccordSpeciesCatalog.Find(record.speciesId);
                if (definition == null)
                {
                    continue;
                }

                var active = ContainsActivePactId(record.instanceId);
                var focused = string.Equals(
                    pactRosterState.commandFocusInstanceId,
                    record.instanceId,
                    StringComparison.Ordinal);
                var state = record.IsSettlementAssigned
                    ? $"Preferred station: {record.stationId}"
                    : active
                        ? focused ? "FIELD · COMMAND FOCUS" : $"FIELD · SLOT {record.fieldSlot + 1}"
                        : "RESERVE · ABSTRACT CARE";
                GUI.Label(
                    new Rect(rect.x + 14f, y, rect.width - 190f, 22f),
                    $"{PactRosterRules.GetDisplayName(record)} · {definition.combatRole} · " +
                    $"{definition.resonanceAffinity}",
                    body);
                GUI.Label(
                    new Rect(rect.x + 14f, y + 21f, rect.width - 190f, 20f),
                    $"{state}  STA {record.stamina:0}  MORALE {record.morale:0}",
                    muted);

                var buttonX = rect.xMax - 164f;
                if (!record.IsSettlementAssigned)
                {
                    if (GUI.Button(
                            new Rect(buttonX, y + 4f, 74f, 30f),
                            active ? "Reserve" : "Field"))
                    {
                        if (active)
                        {
                            RemovePactFromField(record.instanceId);
                        }
                        else
                        {
                            TryAddPactToField(record.instanceId, out _);
                        }

                        return;
                    }

                    GUI.enabled = active && !focused;
                    if (GUI.Button(
                            new Rect(buttonX + 80f, y + 4f, 74f, 30f),
                            focused ? "Focused" : "Focus"))
                    {
                        SetPactCommandFocus(record.instanceId);
                        GUI.enabled = true;
                        return;
                    }

                    GUI.enabled = true;
                }

                y += rowHeight;
            }

            if (pageCount <= 1)
            {
                return;
            }

            var footerY = rect.yMax - 36f;
            GUI.enabled = pactRosterUiPage > 0;
            if (GUI.Button(
                    new Rect(rect.x + 14f, footerY, 84f, 28f),
                    "Previous"))
            {
                pactRosterUiPage--;
            }

            GUI.enabled = true;
            GUI.Label(
                new Rect(
                    rect.center.x - 80f,
                    footerY + 4f,
                    160f,
                    22f),
                $"Roster page {pactRosterUiPage + 1}/{pageCount}",
                muted);
            GUI.enabled = pactRosterUiPage + 1 < pageCount;
            if (GUI.Button(
                    new Rect(rect.xMax - 98f, footerY, 84f, 28f),
                    "Next"))
            {
                pactRosterUiPage++;
            }

            GUI.enabled = true;
        }

        private void EnsurePactRosterInitialized()
        {
            if (!pactRosterInitialized)
            {
                InitializePactRosterSystem();
            }
        }

        private bool ContainsActivePactId(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return false;
            }

            for (var index = 0;
                 index < pactRosterState.activeFieldInstanceIds.Count;
                 index++)
            {
                if (string.Equals(
                        pactRosterState.activeFieldInstanceIds[index],
                        instanceId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void SynchronizePactFieldActors()
        {
            if (!pactRosterInitialized || PlayerTransform == null)
            {
                return;
            }

            activeStoryPactActorCount = 0;
            for (var pactIndex = 0;
                 pactIndex < knownEchosworn.Count;
                 pactIndex++)
            {
                var pact = knownEchosworn[pactIndex];
                if (pact == null || !pact.IsPacted || pact.IsMount)
                {
                    continue;
                }

                var storyRecord = FindStoryPactRosterRecord(pact.PactId);
                var activeSlot = storyRecord != null
                    ? FindActivePactSlot(storyRecord.instanceId)
                    : -1;
                pact.ConfigureFieldRosterState(
                    storyRecord?.instanceId,
                    activeSlot,
                    activeSlot >= 0);
                if (activeSlot >= 0)
                {
                    activeStoryPactActorCount++;
                }
            }

            for (var slot = 0; slot < pactRosterState.activeFieldInstanceIds.Count; slot++)
            {
                var instanceId = pactRosterState.activeFieldInstanceIds[slot];
                var record = PactRosterRules.FindRecord(pactRosterState.pacts, instanceId);
                var definition = record != null
                    ? TetherfallAccordSpeciesCatalog.Find(record.speciesId)
                    : null;
                if (record == null ||
                    definition == null ||
                    !PactRosterRules.IsFieldEligible(record) ||
                    PactRosterRules.IsLinkedStoryRecord(record))
                {
                    continue;
                }

                if (!pactFieldActors.TryGetValue(instanceId, out var actor) ||
                    actor == null)
                {
                    actor = CapturedPactActor.Create(
                        this,
                        record,
                        definition,
                        GetPactFormationPosition(slot),
                        slot);
                    if (actor != null)
                    {
                        pactFieldActors[instanceId] = actor;
                    }
                }
                else
                {
                    actor.RefreshRecord(record, definition);
                    actor.SetFieldSlot(slot);
                }
            }

            pactActorRemovalBuffer.Clear();
            foreach (var pair in pactFieldActors)
            {
                var actorRecord = PactRosterRules.FindRecord(
                    pactRosterState.pacts,
                    pair.Key);
                if (!ContainsActivePactId(pair.Key) ||
                    pair.Value == null ||
                    actorRecord == null ||
                    !PactRosterRules.IsFieldEligible(actorRecord) ||
                    PactRosterRules.IsLinkedStoryRecord(actorRecord))
                {
                    pactActorRemovalBuffer.Add(pair.Key);
                }
            }

            for (var index = 0; index < pactActorRemovalBuffer.Count; index++)
            {
                var instanceId = pactActorRemovalBuffer[index];
                if (pactFieldActors.TryGetValue(instanceId, out var actor) &&
                    actor != null)
                {
                    actor.Retire();
                }

                pactFieldActors.Remove(instanceId);
            }

            pactActorRemovalBuffer.Clear();
            SynchronizePactCommandFocusAlias();
        }

        private void DestroyAllPactFieldActors()
        {
            foreach (var pair in pactFieldActors)
            {
                if (pair.Value != null)
                {
                    pair.Value.Retire();
                }
            }

            pactFieldActors.Clear();
            pactActorRemovalBuffer.Clear();
            activeStoryPactActorCount = 0;
            for (var index = 0; index < knownEchosworn.Count; index++)
            {
                var pact = knownEchosworn[index];
                if (pact != null && pact.IsPacted && !pact.IsMount)
                {
                    pact.ConfigureFieldRosterState(null, -1, false);
                }
            }
        }

        private void EnsureEstablishedStoryPactRecords(
            bool activateOnlyWhenAdded)
        {
            for (var index = 0; index < pacts.Count; index++)
            {
                var pact = pacts[index];
                if (pact != null && pact.IsPacted && !pact.IsMount)
                {
                    EnsureStoryPactInRoster(
                        pact,
                        true,
                        activateOnlyWhenAdded);
                }
            }
        }

        private CapturedPactSaveData FindStoryPactRosterRecord(
            string legendaryPactId)
        {
            if (string.IsNullOrEmpty(legendaryPactId) ||
                pactRosterState?.pacts == null)
            {
                return null;
            }

            for (var index = 0; index < pactRosterState.pacts.Count; index++)
            {
                var record = pactRosterState.pacts[index];
                if (record != null &&
                    string.Equals(
                        record.linkedLegendaryPactId,
                        legendaryPactId,
                        StringComparison.Ordinal))
                {
                    return record;
                }
            }

            return null;
        }

        private PactCreature FindPactCreatureByStableId(string pactId)
        {
            if (string.IsNullOrEmpty(pactId))
            {
                return null;
            }

            for (var index = 0; index < knownEchosworn.Count; index++)
            {
                var pact = knownEchosworn[index];
                if (pact != null &&
                    string.Equals(
                        pact.PactId,
                        pactId,
                        StringComparison.Ordinal))
                {
                    return pact;
                }
            }

            return null;
        }

        private int FindActivePactSlot(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) ||
                pactRosterState?.activeFieldInstanceIds == null)
            {
                return -1;
            }

            for (var index = 0;
                 index < pactRosterState.activeFieldInstanceIds.Count;
                 index++)
            {
                if (string.Equals(
                        pactRosterState.activeFieldInstanceIds[index],
                        instanceId,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private void SynchronizePactCommandFocusAlias()
        {
            ActiveEchosworn = null;
            var focus = PactRosterRules.FindRecord(
                pactRosterState?.pacts,
                pactRosterState?.commandFocusInstanceId);
            if (focus != null &&
                PactRosterRules.IsLinkedStoryRecord(focus) &&
                ContainsActivePactId(focus.instanceId))
            {
                var storyActor = FindPactCreatureByStableId(
                    focus.linkedLegendaryPactId);
                if (storyActor != null &&
                    storyActor.IsPacted &&
                    !storyActor.IsMount)
                {
                    ActiveEchosworn = storyActor;
                }
            }

            RefreshActivePactVisuals();
        }

        private void UpdateStoryPactFieldSimulation(
            PactCreature actor,
            CapturedPactSaveData record,
            int slot,
            Vector3 playerPosition,
            bool insideSafeSettlement)
        {
            actor.ConfigureFieldRosterState(record.instanceId, slot, true);
            var delta = actor.transform.position - playerPosition;
            if (delta.sqrMagnitude > PactActorActivationDistanceSquared)
            {
                if (!insideSafeSettlement)
                {
                    actor.TryRecallToFormation(
                        GetPactFormationPosition(slot),
                        false);
                    delta = actor.transform.position - playerPosition;
                    if (delta.sqrMagnitude >
                        PactActorActivationDistanceSquared)
                    {
                        actor.SetFieldSimulationActive(false);
                        return;
                    }
                }
                else
                {
                    actor.SetFieldSimulationActive(false);
                    return;
                }
            }

            actor.SetFieldSimulationActive(true);
        }

        private static float PlanarDistanceSquared(Vector3 first, Vector3 second)
        {
            var x = first.x - second.x;
            var z = first.z - second.z;
            return x * x + z * z;
        }
    }
}
