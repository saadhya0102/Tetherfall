using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tetherfall
{
    public enum InfiniteSpireRoomCategory
    {
        Easy = 0,
        Combat = 1,
        Swarm = 2,
        Traversal = 3,
        Puzzle = 4,
        Miniboss = 5,
        Boss = 6,
        Treasure = 7,
        Merchant = 8,
        Rest = 9
    }

    public enum InfiniteSpireRoomBeat
    {
        Approach = 0,
        Pressure = 1,
        Cadence = 2,
        Seal = 3
    }

    [Flags]
    public enum InfiniteSpireRoomBeatMask
    {
        None = 0,
        Approach = 1 << 0,
        Pressure = 1 << 1,
        Cadence = 1 << 2,
        Seal = 1 << 3
    }

    public enum InfiniteSpirePuzzleKind
    {
        None = 0,
        OrderedSequence = 1,
        StateMatch = 2
    }

    /// <summary>
    /// Conservative authored geometry metadata. Runtime builders must stay at or below these values.
    /// </summary>
    public readonly struct InfiniteSpireGeometryBudget
    {
        public InfiniteSpireGeometryBudget(
            int renderers,
            int colliders,
            int addedLights,
            int particleSystems,
            int mechanisms,
            int maximumSimultaneousEnemies,
            int serializedWaves)
        {
            Renderers = renderers;
            Colliders = colliders;
            AddedLights = addedLights;
            ParticleSystems = particleSystems;
            Mechanisms = mechanisms;
            MaximumSimultaneousEnemies = maximumSimultaneousEnemies;
            SerializedWaves = serializedWaves;
        }

        public int Renderers { get; }
        public int Colliders { get; }
        public int AddedLights { get; }
        public int ParticleSystems { get; }
        public int Mechanisms { get; }
        public int MaximumSimultaneousEnemies { get; }
        public int SerializedWaves { get; }
    }

    /// <summary>
    /// Hard runtime ceilings for the later three-room streaming window.
    /// </summary>
    public static class InfiniteSpireLoadedWindowBudgets
    {
        public const int MaximumLoadedRooms = 3;
        public const int MaximumFullyActiveRooms = 1;
        public const int MaximumLoadedRenderers = 110;
        public const int MaximumLoadedColliders = 72;
        public const int MaximumAddedLights = 0;
        public const int MaximumActiveEnemies = 8;
        public const int MaximumActiveBosses = 1;
        public const int MaximumParticleSystems = 2;

        public const int MaximumStandardRoomRenderers = 36;
        public const int MaximumStandardRoomColliders = 28;
        public const int MaximumBossRoomRenderers = 44;
        public const int MaximumBossRoomColliders = 32;

        public const int MaximumStubRenderers = 12;
        public const int MaximumStubColliders = 8;
    }

    public sealed class InfiniteSpireWaveDefinition
    {
        internal InfiniteSpireWaveDefinition(string[] enemyIds)
        {
            EnemyIds = InfiniteSpireCatalogStorage.Freeze(enemyIds);
        }

        public IReadOnlyList<string> EnemyIds { get; }
    }

    public sealed class InfiniteSpireWaveSetDefinition
    {
        internal InfiniteSpireWaveSetDefinition(InfiniteSpireWaveDefinition[] waves)
        {
            Waves = InfiniteSpireCatalogStorage.Freeze(waves);
        }

        public IReadOnlyList<InfiniteSpireWaveDefinition> Waves { get; }
    }

    public sealed class InfiniteSpireMerchantOfferDefinition
    {
        internal InfiniteSpireMerchantOfferDefinition(
            string id,
            string displayName,
            int baseSpirecoilCost)
        {
            Id = id;
            DisplayName = displayName;
            BaseSpirecoilCost = baseSpirecoilCost;
            Salt = InfiniteSpireCatalogStorage.StableSalt(id);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int BaseSpirecoilCost { get; }
        public uint Salt { get; }
    }

    public sealed class InfiniteSpireMerchantKitDefinition
    {
        internal InfiniteSpireMerchantKitDefinition(
            string id,
            string displayName,
            InfiniteSpireMerchantOfferDefinition[] offers)
        {
            Id = id;
            DisplayName = displayName;
            Offers = InfiniteSpireCatalogStorage.Freeze(offers);
            Salt = InfiniteSpireCatalogStorage.StableSalt(id);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<InfiniteSpireMerchantOfferDefinition> Offers { get; }
        public uint Salt { get; }
    }

    public sealed class InfiniteSpireMinibossProfile
    {
        internal InfiniteSpireMinibossProfile(
            string id,
            string displayName,
            string enemyId)
        {
            Id = id;
            DisplayName = displayName;
            EnemyId = enemyId;
            Salt = InfiniteSpireCatalogStorage.StableSalt(id);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string EnemyId { get; }
        public uint Salt { get; }
    }

    public sealed class InfiniteSpireBossProfile
    {
        internal InfiniteSpireBossProfile(
            string id,
            string displayName,
            string bossId,
            string supportEnemyId)
        {
            Id = id;
            DisplayName = displayName;
            BossId = bossId;
            SupportEnemyId = supportEnemyId ?? string.Empty;
            Salt = InfiniteSpireCatalogStorage.StableSalt(id);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string BossId { get; }
        public string SupportEnemyId { get; }
        public uint Salt { get; }
    }

    public sealed class InfiniteSpireTemplateDefinition
    {
        internal InfiniteSpireTemplateDefinition(
            string id,
            string displayName,
            InfiniteSpireRoomCategory category,
            InfiniteSpireRoomBeatMask allowedBeats,
            int layoutVariantCount,
            InfiniteSpirePuzzleKind puzzleKind,
            int mechanismCount,
            int mechanismStateCount,
            InfiniteSpireGeometryBudget geometryBudget,
            InfiniteSpireWaveSetDefinition[] waveSets,
            InfiniteSpireMerchantKitDefinition merchantKit)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            AllowedBeats = allowedBeats;
            LayoutVariantCount = layoutVariantCount;
            PuzzleKind = puzzleKind;
            MechanismCount = mechanismCount;
            MechanismStateCount = mechanismStateCount;
            GeometryBudget = geometryBudget;
            WaveSets = InfiniteSpireCatalogStorage.Freeze(waveSets);
            MerchantKit = merchantKit;
            Salt = InfiniteSpireCatalogStorage.StableSalt(id);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public InfiniteSpireRoomCategory Category { get; }
        public InfiniteSpireRoomBeatMask AllowedBeats { get; }
        public int LayoutVariantCount { get; }
        public InfiniteSpirePuzzleKind PuzzleKind { get; }
        public int MechanismCount { get; }
        public int MechanismStateCount { get; }
        public InfiniteSpireGeometryBudget GeometryBudget { get; }
        public IReadOnlyList<InfiniteSpireWaveSetDefinition> WaveSets { get; }
        public InfiniteSpireMerchantKitDefinition MerchantKit { get; }
        public uint Salt { get; }

        public bool SupportsBeat(InfiniteSpireRoomBeat beat)
        {
            var mask = beat switch
            {
                InfiniteSpireRoomBeat.Approach => InfiniteSpireRoomBeatMask.Approach,
                InfiniteSpireRoomBeat.Pressure => InfiniteSpireRoomBeatMask.Pressure,
                InfiniteSpireRoomBeat.Cadence => InfiniteSpireRoomBeatMask.Cadence,
                InfiniteSpireRoomBeat.Seal => InfiniteSpireRoomBeatMask.Seal,
                _ => InfiniteSpireRoomBeatMask.None
            };
            return (AllowedBeats & mask) != 0;
        }
    }

    /// <summary>
    /// Constants are persistence contracts. Existing values must never be renamed or reused.
    /// </summary>
    public static class InfiniteSpireIds
    {
        public const string Dungeon = "dungeon.infinite_spire";

        public const string EasyCopperThreshold = "spire.template.easy.copper_threshold";
        public const string EasyThreadglassGallery = "spire.template.easy.threadglass_gallery";
        public const string EasyQuietRibwalk = "spire.template.easy.quiet_ribwalk";
        public const string EasyWayfinderAnteroom = "spire.template.easy.wayfinder_anteroom";

        public const string CombatRibArena = "spire.template.combat.rib_arena";
        public const string CombatSplitGallery = "spire.template.combat.split_gallery";
        public const string CombatRelayCrossing = "spire.template.combat.relay_crossing";
        public const string CombatCinderBalcony = "spire.template.combat.cinder_balcony";
        public const string CombatFenwardSquare = "spire.template.combat.fenward_square";
        public const string CombatVaultlineCourt = "spire.template.combat.vaultline_court";

        public const string SwarmWispWell = "spire.template.swarm.wisp_well";
        public const string SwarmScoutRush = "spire.template.swarm.scout_rush";
        public const string SwarmCinderSurge = "spire.template.swarm.cinder_surge";
        public const string SwarmFenEchoTide = "spire.template.swarm.fen_echo_tide";

        public const string TraversalCopperLift = "spire.template.traversal.copper_lift";
        public const string TraversalBrokenRibAscent = "spire.template.traversal.broken_rib_ascent";
        public const string TraversalWindglassSwitchback = "spire.template.traversal.windglass_switchback";

        public const string PuzzleMeridianDials = "spire.template.puzzle.meridian_dials";
        public const string PuzzleCounterchimeLattice = "spire.template.puzzle.counterchime_lattice";
        public const string PuzzleTideglassRegisters = "spire.template.puzzle.tideglass_registers";
        public const string PuzzleCopperPhaseBridge = "spire.template.puzzle.copper_phase_bridge";

        public const string MinibossCrownRing = "spire.template.miniboss.crown_ring";
        public const string MinibossBrokenLoom = "spire.template.miniboss.broken_loom";
        public const string MinibossPressureDais = "spire.template.miniboss.pressure_dais";
        public const string MinibossQuietCrucible = "spire.template.miniboss.quiet_crucible";

        public const string BossHaloAperture = "spire.template.boss.halo_aperture";
        public const string BossDeepLoom = "spire.template.boss.deep_loom";
        public const string BossCrownObservatory = "spire.template.boss.crown_observatory";
        public const string BossMeridianVault = "spire.template.boss.meridian_vault";

        public const string TreasureCopperCache = "spire.template.treasure.copper_cache";
        public const string TreasureSealedArchive = "spire.template.treasure.sealed_archive";
        public const string TreasureWayfinderReliquary = "spire.template.treasure.wayfinder_reliquary";

        public const string MerchantFieldCounter = "spire.template.merchant.field_counter";
        public const string MerchantProvisionLoom = "spire.template.merchant.provision_loom";
        public const string MerchantRoutewrightDesk = "spire.template.merchant.routewright_desk";
        public const string MerchantBulwarkExchange = "spire.template.merchant.bulwark_exchange";

        public const string RestLoomrestMeridian = "spire.template.rest.loomrest_meridian";

        public const string MinibossProfileMossfang =
            "spire.template.miniboss.profile.mossfang_matriarch";
        public const string MinibossProfileSiltglass =
            "spire.template.miniboss.profile.siltglass_heron";
        public const string MinibossProfileCindermaw =
            "spire.template.miniboss.profile.cindermaw";
        public const string MinibossProfileMirewarden =
            "spire.template.miniboss.profile.mirewarden";

        public const string BossProfileSkyvaultLattice =
            "spire.template.boss.profile.skyvault_lattice";
        public const string BossProfileGlasswaterRegency =
            "spire.template.boss.profile.glasswater_regency";
        public const string BossProfileScoriaRail =
            "spire.template.boss.profile.scoria_rail";
        public const string BossProfileHollowCadence =
            "spire.template.boss.profile.hollow_cadence";
        public const string BossProfileSkyvaultShardClock =
            "spire.template.boss.profile.skyvault_shard_clock";
        public const string BossProfileGlasswaterCounterflow =
            "spire.template.boss.profile.glasswater_counterflow";
        public const string BossProfileScoriaAshMeridian =
            "spire.template.boss.profile.scoria_ash_meridian";
        public const string BossProfileHollowSilentRefrain =
            "spire.template.boss.profile.hollow_silent_refrain";

        public const string MerchantKitFieldMender =
            "spire.template.merchant.kit.field_mender";
        public const string MerchantKitProvisioner =
            "spire.template.merchant.kit.provisioner";
        public const string MerchantKitRoutewright =
            "spire.template.merchant.kit.routewright";
        public const string MerchantKitBulwark =
            "spire.template.merchant.kit.bulwark";

        public const string MerchantOfferVitalityMend =
            "spire.template.merchant.offer.vitality_mend";
        public const string MerchantOfferStaminaRefill =
            "spire.template.merchant.offer.stamina_refill";
        public const string MerchantOfferGuardTemper =
            "spire.template.merchant.offer.guard_temper";
        public const string MerchantOfferDamageBraid =
            "spire.template.merchant.offer.damage_braid";
        public const string MerchantOfferFleetThread =
            "spire.template.merchant.offer.fleet_thread";
        public const string MerchantOfferWaveWard =
            "spire.template.merchant.offer.wave_ward";
        public const string MerchantOfferTreasureBearing =
            "spire.template.merchant.offer.treasure_bearing";
        public const string MerchantOfferSealRepair =
            "spire.template.merchant.offer.seal_repair";
    }

    public static class InfiniteSpireCatalog
    {
        public const string DungeonId = InfiniteSpireIds.Dungeon;
        public const string TemplateIdPrefix = "spire.template.";

        public static IReadOnlyList<InfiniteSpireMerchantOfferDefinition> MerchantOffers { get; } =
            InfiniteSpireCatalogStorage.Freeze(new[]
            {
                Offer(InfiniteSpireIds.MerchantOfferVitalityMend, "Vitality Mend", 24),
                Offer(InfiniteSpireIds.MerchantOfferStaminaRefill, "Stamina Refill", 18),
                Offer(InfiniteSpireIds.MerchantOfferGuardTemper, "Guard Temper", 32),
                Offer(InfiniteSpireIds.MerchantOfferDamageBraid, "Damage Braid", 38),
                Offer(InfiniteSpireIds.MerchantOfferFleetThread, "Fleet Thread", 28),
                Offer(InfiniteSpireIds.MerchantOfferWaveWard, "Wave Ward", 34),
                Offer(InfiniteSpireIds.MerchantOfferTreasureBearing, "Treasure Bearing", 30),
                Offer(InfiniteSpireIds.MerchantOfferSealRepair, "Seal Repair", 26)
            });

        public static IReadOnlyList<InfiniteSpireMerchantKitDefinition> MerchantKits { get; } =
            InfiniteSpireCatalogStorage.Freeze(new[]
            {
                Kit(
                    InfiniteSpireIds.MerchantKitFieldMender,
                    "Field Mender Kit",
                    InfiniteSpireIds.MerchantOfferVitalityMend,
                    InfiniteSpireIds.MerchantOfferStaminaRefill,
                    InfiniteSpireIds.MerchantOfferSealRepair,
                    InfiniteSpireIds.MerchantOfferGuardTemper),
                Kit(
                    InfiniteSpireIds.MerchantKitProvisioner,
                    "Ascent Provision Kit",
                    InfiniteSpireIds.MerchantOfferStaminaRefill,
                    InfiniteSpireIds.MerchantOfferFleetThread,
                    InfiniteSpireIds.MerchantOfferWaveWard,
                    InfiniteSpireIds.MerchantOfferVitalityMend),
                Kit(
                    InfiniteSpireIds.MerchantKitRoutewright,
                    "Routewright Kit",
                    InfiniteSpireIds.MerchantOfferTreasureBearing,
                    InfiniteSpireIds.MerchantOfferFleetThread,
                    InfiniteSpireIds.MerchantOfferSealRepair,
                    InfiniteSpireIds.MerchantOfferDamageBraid),
                Kit(
                    InfiniteSpireIds.MerchantKitBulwark,
                    "Bulwark Kit",
                    InfiniteSpireIds.MerchantOfferGuardTemper,
                    InfiniteSpireIds.MerchantOfferWaveWard,
                    InfiniteSpireIds.MerchantOfferVitalityMend,
                    InfiniteSpireIds.MerchantOfferDamageBraid)
            });

        public static IReadOnlyList<InfiniteSpireMinibossProfile> MinibossProfiles { get; } =
            InfiniteSpireCatalogStorage.Freeze(new[]
            {
                new InfiniteSpireMinibossProfile(
                    InfiniteSpireIds.MinibossProfileMossfang,
                    "Mossfang Crown",
                    "enemy.elite.mossfang_matriarch"),
                new InfiniteSpireMinibossProfile(
                    InfiniteSpireIds.MinibossProfileSiltglass,
                    "Siltglass Crown",
                    "enemy.elite.siltglass_heron"),
                new InfiniteSpireMinibossProfile(
                    InfiniteSpireIds.MinibossProfileCindermaw,
                    "Cindermaw Crown",
                    "enemy.elite.cindermaw"),
                new InfiniteSpireMinibossProfile(
                    InfiniteSpireIds.MinibossProfileMirewarden,
                    "Mirewarden Crown",
                    "enemy.elite.mirewarden")
            });

        public static IReadOnlyList<InfiniteSpireBossProfile> BossProfiles { get; } =
            InfiniteSpireCatalogStorage.Freeze(new[]
            {
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileSkyvaultLattice,
                    "Skyvault Lattice",
                    "boss.skyvault_warden",
                    "enemy.vault_wisp"),
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileGlasswaterRegency,
                    "Glasswater Regency",
                    "boss.glasswater_regent",
                    "enemy.fen_siltcaller"),
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileScoriaRail,
                    "Scoria Rail",
                    "boss.scoria_harrower",
                    "enemy.cinder_echo"),
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileHollowCadence,
                    "Hollow Cadence",
                    "boss.hollow_choir_cantor",
                    "enemy.hollow_scout"),
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileSkyvaultShardClock,
                    "Skyvault Shard Clock",
                    "boss.skyvault_warden",
                    "enemy.hollow_scout"),
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileGlasswaterCounterflow,
                    "Glasswater Counterflow",
                    "boss.glasswater_regent",
                    "enemy.vault_wisp"),
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileScoriaAshMeridian,
                    "Scoria Ash Meridian",
                    "boss.scoria_harrower",
                    "enemy.relay_breaker"),
                new InfiniteSpireBossProfile(
                    InfiniteSpireIds.BossProfileHollowSilentRefrain,
                    "Hollow Silent Refrain",
                    "boss.hollow_choir_cantor",
                    "enemy.fen_spitter")
            });

        public static IReadOnlyList<InfiniteSpireTemplateDefinition> Templates { get; } =
            InfiniteSpireCatalogStorage.Freeze(BuildTemplates());

        public static IReadOnlyList<InfiniteSpireTemplateDefinition> All => Templates;

        public static InfiniteSpireTemplateDefinition Get(string id)
        {
            return GetTemplate(id);
        }

        public static InfiniteSpireTemplateDefinition GetTemplate(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var index = 0; index < Templates.Count; index++)
            {
                var definition = Templates[index];
                if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static InfiniteSpireMerchantOfferDefinition GetMerchantOffer(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var index = 0; index < MerchantOffers.Count; index++)
            {
                var definition = MerchantOffers[index];
                if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static InfiniteSpireMerchantKitDefinition GetMerchantKit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var index = 0; index < MerchantKits.Count; index++)
            {
                var definition = MerchantKits[index];
                if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static int CountTemplates(
            InfiniteSpireRoomCategory category,
            InfiniteSpireRoomBeat beat)
        {
            var count = 0;
            for (var index = 0; index < Templates.Count; index++)
            {
                var template = Templates[index];
                if (template.Category == category && template.SupportsBeat(beat))
                {
                    count++;
                }
            }

            return count;
        }

        private static InfiniteSpireTemplateDefinition[] BuildTemplates()
        {
            var approachAndSeal =
                InfiniteSpireRoomBeatMask.Approach | InfiniteSpireRoomBeatMask.Seal;
            var pressure = InfiniteSpireRoomBeatMask.Pressure;
            var cadence = InfiniteSpireRoomBeatMask.Cadence;

            return new[]
            {
                Template(
                    InfiniteSpireIds.EasyCopperThreshold,
                    "Copper Threshold",
                    InfiniteSpireRoomCategory.Easy,
                    approachAndSeal,
                    3,
                    Budget(22, 16, 1, 0, 2, 1),
                    Sets(Set(Wave("enemy.hollow_raider", "enemy.hollow_scout")))),
                Template(
                    InfiniteSpireIds.EasyThreadglassGallery,
                    "Threadglass Gallery",
                    InfiniteSpireRoomCategory.Easy,
                    approachAndSeal,
                    3,
                    Budget(24, 16, 1, 0, 2, 1),
                    Sets(Set(Wave("enemy.mossglass_stalker", "enemy.hollow_raider")))),
                Template(
                    InfiniteSpireIds.EasyQuietRibwalk,
                    "Quiet Ribwalk",
                    InfiniteSpireRoomCategory.Easy,
                    approachAndSeal,
                    4,
                    Budget(21, 15, 1, 0, 2, 1),
                    Sets(Set(Wave("enemy.vault_wisp", "enemy.vault_wisp")))),
                Template(
                    InfiniteSpireIds.EasyWayfinderAnteroom,
                    "Wayfinder Anteroom",
                    InfiniteSpireRoomCategory.Easy,
                    approachAndSeal,
                    3,
                    Budget(23, 17, 1, 0, 2, 1),
                    Sets(Set(Wave("enemy.cinder_echo", "enemy.hollow_scout")))),

                Template(
                    InfiniteSpireIds.CombatRibArena,
                    "Rib Arena",
                    InfiniteSpireRoomCategory.Combat,
                    pressure | cadence,
                    4,
                    Budget(31, 22, 1, 0, 5, 1),
                    Sets(
                        Set(Wave(
                            "enemy.hollow_raider",
                            "enemy.mossglass_stalker",
                            "enemy.hollow_scout")),
                        Set(Wave(
                            "enemy.fen_spitter",
                            "enemy.mossglass_stalker",
                            "enemy.hollow_raider",
                            "enemy.vault_wisp")))),
                Template(
                    InfiniteSpireIds.CombatSplitGallery,
                    "Split Gallery",
                    InfiniteSpireRoomCategory.Combat,
                    pressure | cadence,
                    3,
                    Budget(30, 22, 1, 0, 5, 1),
                    Sets(
                        Set(Wave(
                            "enemy.hollow_scout",
                            "enemy.hollow_scout",
                            "enemy.relay_breaker")),
                        Set(Wave(
                            "enemy.fen_spitter",
                            "enemy.fen_siltcaller",
                            "enemy.mossglass_stalker")))),
                Template(
                    InfiniteSpireIds.CombatRelayCrossing,
                    "Relay Crossing",
                    InfiniteSpireRoomCategory.Combat,
                    pressure | cadence,
                    4,
                    Budget(32, 24, 1, 0, 5, 1),
                    Sets(
                        Set(Wave(
                            "enemy.relay_breaker",
                            "enemy.hollow_raider",
                            "enemy.hollow_scout")),
                        Set(Wave(
                            "enemy.relay_breaker",
                            "enemy.cinder_echo",
                            "enemy.cinder_echo",
                            "enemy.vault_wisp")))),
                Template(
                    InfiniteSpireIds.CombatCinderBalcony,
                    "Cinder Balcony",
                    InfiniteSpireRoomCategory.Combat,
                    pressure | cadence,
                    3,
                    Budget(29, 21, 1, 0, 5, 1),
                    Sets(
                        Set(Wave(
                            "enemy.cinder_echo",
                            "enemy.cinder_echo",
                            "enemy.ashjaw_brute")),
                        Set(Wave(
                            "enemy.cinder_echo",
                            "enemy.relay_breaker",
                            "enemy.vault_wisp")))),
                Template(
                    InfiniteSpireIds.CombatFenwardSquare,
                    "Fenward Square",
                    InfiniteSpireRoomCategory.Combat,
                    pressure | cadence,
                    3,
                    Budget(33, 23, 1, 0, 5, 1),
                    Sets(
                        Set(Wave(
                            "enemy.fen_spitter",
                            "enemy.fen_siltcaller",
                            "enemy.mossglass_stalker")),
                        Set(Wave(
                            "enemy.fen_spitter",
                            "enemy.fen_spitter",
                            "enemy.relay_breaker",
                            "enemy.vault_wisp")))),
                Template(
                    InfiniteSpireIds.CombatVaultlineCourt,
                    "Vaultline Court",
                    InfiniteSpireRoomCategory.Combat,
                    pressure | cadence,
                    4,
                    Budget(34, 24, 1, 0, 5, 1),
                    Sets(
                        Set(Wave(
                            "enemy.vault_wisp",
                            "enemy.vault_wisp",
                            "enemy.hollow_scout",
                            "enemy.relay_breaker")),
                        Set(Wave(
                            "enemy.vault_wisp",
                            "enemy.fen_siltcaller",
                            "enemy.hollow_raider",
                            "enemy.mossglass_stalker")))),

                Template(
                    InfiniteSpireIds.SwarmWispWell,
                    "Wisp Well",
                    InfiniteSpireRoomCategory.Swarm,
                    pressure,
                    4,
                    Budget(34, 23, 1, 0, 5, 2),
                    Sets(
                        Set(
                            Wave("enemy.vault_wisp", "enemy.vault_wisp", "enemy.vault_wisp"),
                            Wave(
                                "enemy.vault_wisp",
                                "enemy.hollow_scout",
                                "enemy.hollow_scout",
                                "enemy.relay_breaker")),
                        Set(
                            Wave("enemy.vault_wisp", "enemy.vault_wisp", "enemy.fen_spitter"),
                            Wave(
                                "enemy.vault_wisp",
                                "enemy.vault_wisp",
                                "enemy.fen_siltcaller",
                                "enemy.mossglass_stalker")))),
                Template(
                    InfiniteSpireIds.SwarmScoutRush,
                    "Scout Rush",
                    InfiniteSpireRoomCategory.Swarm,
                    pressure,
                    3,
                    Budget(32, 22, 1, 0, 5, 2),
                    Sets(
                        Set(
                            Wave("enemy.hollow_scout", "enemy.hollow_scout", "enemy.hollow_raider"),
                            Wave(
                                "enemy.hollow_scout",
                                "enemy.hollow_scout",
                                "enemy.mossglass_stalker",
                                "enemy.relay_breaker")),
                        Set(
                            Wave("enemy.hollow_raider", "enemy.hollow_raider", "enemy.hollow_scout"),
                            Wave(
                                "enemy.hollow_scout",
                                "enemy.hollow_scout",
                                "enemy.vault_wisp",
                                "enemy.mossglass_stalker")))),
                Template(
                    InfiniteSpireIds.SwarmCinderSurge,
                    "Cinder Surge",
                    InfiniteSpireRoomCategory.Swarm,
                    pressure,
                    4,
                    Budget(36, 24, 1, 0, 5, 2),
                    Sets(
                        Set(
                            Wave("enemy.cinder_echo", "enemy.cinder_echo", "enemy.cinder_echo"),
                            Wave(
                                "enemy.cinder_echo",
                                "enemy.cinder_echo",
                                "enemy.relay_breaker",
                                "enemy.ashjaw_brute")),
                        Set(
                            Wave("enemy.cinder_echo", "enemy.cinder_echo", "enemy.vault_wisp"),
                            Wave(
                                "enemy.cinder_echo",
                                "enemy.relay_breaker",
                                "enemy.relay_breaker",
                                "enemy.vault_wisp")))),
                Template(
                    InfiniteSpireIds.SwarmFenEchoTide,
                    "Fen Echo Tide",
                    InfiniteSpireRoomCategory.Swarm,
                    pressure,
                    3,
                    Budget(35, 24, 1, 0, 5, 2),
                    Sets(
                        Set(
                            Wave("enemy.fen_spitter", "enemy.fen_spitter", "enemy.vault_wisp"),
                            Wave(
                                "enemy.fen_spitter",
                                "enemy.fen_siltcaller",
                                "enemy.mossglass_stalker",
                                "enemy.vault_wisp")),
                        Set(
                            Wave("enemy.mossglass_stalker", "enemy.mossglass_stalker", "enemy.fen_spitter"),
                            Wave(
                                "enemy.fen_spitter",
                                "enemy.fen_spitter",
                                "enemy.fen_siltcaller",
                                "enemy.relay_breaker")))),

                Template(
                    InfiniteSpireIds.TraversalCopperLift,
                    "Copper Lift",
                    InfiniteSpireRoomCategory.Traversal,
                    approachAndSeal | pressure,
                    4,
                    Budget(27, 23, 0, 0, 0, 0)),
                Template(
                    InfiniteSpireIds.TraversalBrokenRibAscent,
                    "Broken Rib Ascent",
                    InfiniteSpireRoomCategory.Traversal,
                    approachAndSeal | pressure,
                    4,
                    Budget(31, 24, 0, 0, 0, 0)),
                Template(
                    InfiniteSpireIds.TraversalWindglassSwitchback,
                    "Windglass Switchback",
                    InfiniteSpireRoomCategory.Traversal,
                    approachAndSeal | pressure,
                    3,
                    Budget(29, 22, 0, 0, 0, 0)),

                PuzzleTemplate(
                    InfiniteSpireIds.PuzzleMeridianDials,
                    "Meridian Dials",
                    InfiniteSpirePuzzleKind.OrderedSequence,
                    3,
                    1,
                    4,
                    Budget(30, 21, 1, 3, 0, 0)),
                PuzzleTemplate(
                    InfiniteSpireIds.PuzzleCounterchimeLattice,
                    "Counterchime Lattice",
                    InfiniteSpirePuzzleKind.OrderedSequence,
                    4,
                    1,
                    3,
                    Budget(34, 22, 1, 4, 0, 0)),
                PuzzleTemplate(
                    InfiniteSpireIds.PuzzleTideglassRegisters,
                    "Tideglass Registers",
                    InfiniteSpirePuzzleKind.StateMatch,
                    3,
                    3,
                    4,
                    Budget(33, 22, 1, 3, 0, 0)),
                PuzzleTemplate(
                    InfiniteSpireIds.PuzzleCopperPhaseBridge,
                    "Copper Phase Bridge",
                    InfiniteSpirePuzzleKind.StateMatch,
                    4,
                    3,
                    3,
                    Budget(35, 24, 1, 4, 0, 0)),

                Template(
                    InfiniteSpireIds.MinibossCrownRing,
                    "Crown Ring",
                    InfiniteSpireRoomCategory.Miniboss,
                    cadence,
                    3,
                    Budget(34, 24, 1, 0, 1, 1)),
                Template(
                    InfiniteSpireIds.MinibossBrokenLoom,
                    "Broken Loom",
                    InfiniteSpireRoomCategory.Miniboss,
                    cadence,
                    3,
                    Budget(35, 24, 1, 0, 1, 1)),
                Template(
                    InfiniteSpireIds.MinibossPressureDais,
                    "Pressure Dais",
                    InfiniteSpireRoomCategory.Miniboss,
                    cadence,
                    4,
                    Budget(36, 24, 1, 0, 1, 1)),
                Template(
                    InfiniteSpireIds.MinibossQuietCrucible,
                    "Quiet Crucible",
                    InfiniteSpireRoomCategory.Miniboss,
                    cadence,
                    3,
                    Budget(34, 23, 1, 0, 1, 1)),

                Template(
                    InfiniteSpireIds.BossHaloAperture,
                    "Halo Aperture",
                    InfiniteSpireRoomCategory.Boss,
                    cadence,
                    4,
                    Budget(42, 30, 2, 0, 4, 1)),
                Template(
                    InfiniteSpireIds.BossDeepLoom,
                    "Deep Loom",
                    InfiniteSpireRoomCategory.Boss,
                    cadence,
                    3,
                    Budget(44, 32, 2, 0, 4, 1)),
                Template(
                    InfiniteSpireIds.BossCrownObservatory,
                    "Crown Observatory",
                    InfiniteSpireRoomCategory.Boss,
                    cadence,
                    4,
                    Budget(43, 31, 2, 0, 4, 1)),
                Template(
                    InfiniteSpireIds.BossMeridianVault,
                    "Meridian Vault",
                    InfiniteSpireRoomCategory.Boss,
                    cadence,
                    3,
                    Budget(41, 30, 2, 0, 4, 1)),

                Template(
                    InfiniteSpireIds.TreasureCopperCache,
                    "Copper Cache",
                    InfiniteSpireRoomCategory.Treasure,
                    cadence,
                    3,
                    Budget(24, 16, 1, 0, 0, 0)),
                Template(
                    InfiniteSpireIds.TreasureSealedArchive,
                    "Sealed Archive",
                    InfiniteSpireRoomCategory.Treasure,
                    cadence,
                    4,
                    Budget(27, 18, 1, 0, 0, 0)),
                Template(
                    InfiniteSpireIds.TreasureWayfinderReliquary,
                    "Wayfinder Reliquary",
                    InfiniteSpireRoomCategory.Treasure,
                    cadence,
                    3,
                    Budget(25, 17, 1, 0, 0, 0)),

                MerchantTemplate(
                    InfiniteSpireIds.MerchantFieldCounter,
                    "Field Counter",
                    InfiniteSpireIds.MerchantKitFieldMender,
                    3,
                    Budget(25, 17, 0, 0, 0, 0)),
                MerchantTemplate(
                    InfiniteSpireIds.MerchantProvisionLoom,
                    "Provision Loom",
                    InfiniteSpireIds.MerchantKitProvisioner,
                    3,
                    Budget(27, 18, 0, 0, 0, 0)),
                MerchantTemplate(
                    InfiniteSpireIds.MerchantRoutewrightDesk,
                    "Routewright Desk",
                    InfiniteSpireIds.MerchantKitRoutewright,
                    4,
                    Budget(29, 18, 0, 0, 0, 0)),
                MerchantTemplate(
                    InfiniteSpireIds.MerchantBulwarkExchange,
                    "Bulwark Exchange",
                    InfiniteSpireIds.MerchantKitBulwark,
                    3,
                    Budget(26, 17, 0, 0, 0, 0)),

                Template(
                    InfiniteSpireIds.RestLoomrestMeridian,
                    "Loomrest Meridian",
                    InfiniteSpireRoomCategory.Rest,
                    cadence,
                    1,
                    Budget(34, 20, 1, 0, 0, 0))
            };
        }

        private static InfiniteSpireTemplateDefinition Template(
            string id,
            string displayName,
            InfiniteSpireRoomCategory category,
            InfiniteSpireRoomBeatMask beats,
            int layouts,
            InfiniteSpireGeometryBudget budget,
            InfiniteSpireWaveSetDefinition[] waveSets = null)
        {
            return new InfiniteSpireTemplateDefinition(
                id,
                displayName,
                category,
                beats,
                layouts,
                InfiniteSpirePuzzleKind.None,
                0,
                0,
                budget,
                waveSets,
                null);
        }

        private static InfiniteSpireTemplateDefinition PuzzleTemplate(
            string id,
            string displayName,
            InfiniteSpirePuzzleKind puzzleKind,
            int mechanismCount,
            int stateCount,
            int layouts,
            InfiniteSpireGeometryBudget budget)
        {
            return new InfiniteSpireTemplateDefinition(
                id,
                displayName,
                InfiniteSpireRoomCategory.Puzzle,
                InfiniteSpireRoomBeatMask.Pressure,
                layouts,
                puzzleKind,
                mechanismCount,
                stateCount,
                budget,
                null,
                null);
        }

        private static InfiniteSpireTemplateDefinition MerchantTemplate(
            string id,
            string displayName,
            string kitId,
            int layouts,
            InfiniteSpireGeometryBudget budget)
        {
            return new InfiniteSpireTemplateDefinition(
                id,
                displayName,
                InfiniteSpireRoomCategory.Merchant,
                InfiniteSpireRoomBeatMask.Cadence,
                layouts,
                InfiniteSpirePuzzleKind.None,
                0,
                0,
                budget,
                null,
                GetMerchantKit(kitId));
        }

        private static InfiniteSpireMerchantOfferDefinition Offer(
            string id,
            string displayName,
            int cost)
        {
            return new InfiniteSpireMerchantOfferDefinition(id, displayName, cost);
        }

        private static InfiniteSpireMerchantKitDefinition Kit(
            string id,
            string displayName,
            params string[] offerIds)
        {
            var offers = new InfiniteSpireMerchantOfferDefinition[offerIds.Length];
            for (var index = 0; index < offerIds.Length; index++)
            {
                offers[index] = GetMerchantOffer(offerIds[index]);
            }

            return new InfiniteSpireMerchantKitDefinition(id, displayName, offers);
        }

        private static InfiniteSpireGeometryBudget Budget(
            int renderers,
            int colliders,
            int particles,
            int mechanisms,
            int enemies,
            int waves)
        {
            return new InfiniteSpireGeometryBudget(
                renderers,
                colliders,
                0,
                particles,
                mechanisms,
                enemies,
                waves);
        }

        private static InfiniteSpireWaveDefinition Wave(params string[] enemyIds)
        {
            return new InfiniteSpireWaveDefinition(enemyIds);
        }

        private static InfiniteSpireWaveSetDefinition Set(
            params InfiniteSpireWaveDefinition[] waves)
        {
            return new InfiniteSpireWaveSetDefinition(waves);
        }

        private static InfiniteSpireWaveSetDefinition[] Sets(
            params InfiniteSpireWaveSetDefinition[] sets)
        {
            return sets;
        }
    }

    internal static class InfiniteSpireCatalogStorage
    {
        public static IReadOnlyList<T> Freeze<T>(T[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.AsReadOnly(Array.Empty<T>());
            }

            var copy = new T[source.Length];
            Array.Copy(source, copy, source.Length);
            return new ReadOnlyCollection<T>(copy);
        }

        public static uint StableSalt(string value)
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            var hash = offset;
            if (value == null)
            {
                return hash;
            }

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                hash ^= (byte)character;
                hash *= prime;
                hash ^= (byte)(character >> 8);
                hash *= prime;
            }

            return hash;
        }
    }
}
