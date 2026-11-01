using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum RegionalWeaponRegionStatus
    {
        LegacyPlayable,
        CurrentPlayable,
        AuthoredExpansion
    }

    public enum RegionalWeaponAcquisitionKind
    {
        Crafting,
        Vendor,
        DiscoveryReward,
        EncounterReward,
        QuestReward,
        BossReward,
        PactReward
    }

    public enum RegionalWeaponElement
    {
        None,
        Grove,
        Tide,
        Ember,
        Gale,
        Frost,
        Prism,
        Gloam,
        Stone,
        Storm,
        Radiance
    }

    public enum RegionalWeaponIdentity
    {
        Combo,
        Range,
        Guard,
        Elemental,
        PactSynergy,
        Hybrid
    }

    public enum RegionalWeaponArtMaterialRole
    {
        Grip,
        Frame,
        Edge,
        Focus,
        Thread
    }

    public sealed class RegionalWeaponIngredient
    {
        public RegionalWeaponIngredient(ResourceKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }

        public ResourceKind Kind { get; }
        public int Amount { get; }
    }

    public sealed class RegionalWeaponRegionDefinition
    {
        public RegionalWeaponRegionDefinition(
            string id,
            string displayName,
            RegionalWeaponRegionStatus status,
            string lore)
        {
            Id = id;
            DisplayName = displayName;
            Status = status;
            Lore = lore;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public RegionalWeaponRegionStatus Status { get; }
        public string Lore { get; }
    }

    public sealed class RegionalWeaponAcquisitionDefinition
    {
        public RegionalWeaponAcquisitionDefinition(
            RegionalWeaponAcquisitionKind kind,
            string sourceId,
            string sourceDescription,
            int favorPrice,
            RegionalWeaponIngredient[] costs)
        {
            Kind = kind;
            SourceId = sourceId;
            SourceDescription = sourceDescription;
            FavorPrice = favorPrice;
            Costs = Array.AsReadOnly(costs ?? Array.Empty<RegionalWeaponIngredient>());
        }

        public RegionalWeaponAcquisitionKind Kind { get; }
        public string SourceId { get; }
        public string SourceDescription { get; }
        public int FavorPrice { get; }
        public IReadOnlyList<RegionalWeaponIngredient> Costs { get; }
    }

    public sealed class RegionalWeaponUpgradeTierDefinition
    {
        public RegionalWeaponUpgradeTierDefinition(
            string id,
            string previousTierId,
            string displayName,
            float damageMultiplier,
            float rangeMultiplier,
            int guardBonus,
            float guardDamageTakenMultiplier,
            float elementalBuildupBonus,
            float pactSynergyMultiplier,
            RegionalWeaponIngredient[] costs)
        {
            Id = id;
            PreviousTierId = previousTierId;
            DisplayName = displayName;
            DamageMultiplier = damageMultiplier;
            RangeMultiplier = rangeMultiplier;
            GuardBonus = guardBonus;
            GuardDamageTakenMultiplier = guardDamageTakenMultiplier;
            ElementalBuildupBonus = elementalBuildupBonus;
            PactSynergyMultiplier = pactSynergyMultiplier;
            Costs = Array.AsReadOnly(costs ?? Array.Empty<RegionalWeaponIngredient>());
        }

        public string Id { get; }
        public string PreviousTierId { get; }
        public string DisplayName { get; }
        public float DamageMultiplier { get; }
        public float RangeMultiplier { get; }
        public int GuardBonus { get; }
        public float GuardDamageTakenMultiplier { get; }
        public float ElementalBuildupBonus { get; }
        public float PactSynergyMultiplier { get; }
        public IReadOnlyList<RegionalWeaponIngredient> Costs { get; }
    }

    public sealed class RegionalWeaponModifierDefinition
    {
        public RegionalWeaponModifierDefinition(
            RegionalWeaponIdentity identity,
            float damageMultiplier,
            float comboStepMultiplier,
            float rangeMultiplier,
            int guardBonus,
            float guardDamageTakenMultiplier,
            float guardCounterDamageMultiplier,
            RegionalWeaponElement element,
            float elementalBuildup,
            bool hasPactSynergy,
            PactResonanceAffinity pactAffinity,
            float pactMatchDamageMultiplier,
            float pactCommandCooldownMultiplier,
            OvertonePitch bonusPitch,
            string gameplayNote)
        {
            Identity = identity;
            DamageMultiplier = damageMultiplier;
            ComboStepMultiplier = comboStepMultiplier;
            RangeMultiplier = rangeMultiplier;
            GuardBonus = guardBonus;
            GuardDamageTakenMultiplier = guardDamageTakenMultiplier;
            GuardCounterDamageMultiplier = guardCounterDamageMultiplier;
            Element = element;
            ElementalBuildup = elementalBuildup;
            HasPactSynergy = hasPactSynergy;
            PactAffinity = pactAffinity;
            PactMatchDamageMultiplier = pactMatchDamageMultiplier;
            PactCommandCooldownMultiplier = pactCommandCooldownMultiplier;
            BonusPitch = bonusPitch;
            GameplayNote = gameplayNote;
        }

        public RegionalWeaponIdentity Identity { get; }
        public float DamageMultiplier { get; }
        public float ComboStepMultiplier { get; }
        public float RangeMultiplier { get; }
        public int GuardBonus { get; }
        public float GuardDamageTakenMultiplier { get; }
        public float GuardCounterDamageMultiplier { get; }
        public RegionalWeaponElement Element { get; }
        public float ElementalBuildup { get; }
        public bool HasPactSynergy { get; }
        public PactResonanceAffinity PactAffinity { get; }
        public float PactMatchDamageMultiplier { get; }
        public float PactCommandCooldownMultiplier { get; }
        public OvertonePitch BonusPitch { get; }
        public string GameplayNote { get; }
    }

    public sealed class RegionalWeaponMeshPartDefinition
    {
        public RegionalWeaponMeshPartDefinition(
            string id,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEulerAngles,
            RegionalWeaponArtMaterialRole materialRole,
            bool emissive)
        {
            Id = id;
            Primitive = primitive;
            LocalPosition = localPosition;
            LocalScale = localScale;
            LocalEulerAngles = localEulerAngles;
            MaterialRole = materialRole;
            Emissive = emissive;
        }

        public string Id { get; }
        public PrimitiveType Primitive { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalScale { get; }
        public Vector3 LocalEulerAngles { get; }
        public RegionalWeaponArtMaterialRole MaterialRole { get; }
        public bool Emissive { get; }
    }

    public sealed class RegionalWeaponArtProfile
    {
        public RegionalWeaponArtProfile(
            string id,
            string attachmentSocket,
            string silhouetteDirection,
            Color primaryColor,
            Color accentColor,
            Color emissionColor,
            RegionalWeaponMeshPartDefinition[] parts)
        {
            Id = id;
            AttachmentSocket = attachmentSocket;
            SilhouetteDirection = silhouetteDirection;
            PrimaryColor = primaryColor;
            AccentColor = accentColor;
            EmissionColor = emissionColor;
            Parts = Array.AsReadOnly(parts ?? Array.Empty<RegionalWeaponMeshPartDefinition>());
        }

        public string Id { get; }
        public string AttachmentSocket { get; }
        public string SilhouetteDirection { get; }
        public Color PrimaryColor { get; }
        public Color AccentColor { get; }
        public Color EmissionColor { get; }
        public IReadOnlyList<RegionalWeaponMeshPartDefinition> Parts { get; }
    }

    public sealed class RegionalWeaponVfxProfile
    {
        public RegionalWeaponVfxProfile(
            string id,
            string releaseCue,
            string impactCue,
            Color trailColor,
            Color impactColor,
            int maximumActivePrimitives,
            int maximumImpactShards,
            float maximumLifetimeSeconds,
            float maximumDistance,
            bool requiresPooling,
            bool colliderFree,
            bool usesDynamicLights)
        {
            Id = id;
            ReleaseCue = releaseCue;
            ImpactCue = impactCue;
            TrailColor = trailColor;
            ImpactColor = impactColor;
            MaximumActivePrimitives = maximumActivePrimitives;
            MaximumImpactShards = maximumImpactShards;
            MaximumLifetimeSeconds = maximumLifetimeSeconds;
            MaximumDistance = maximumDistance;
            RequiresPooling = requiresPooling;
            ColliderFree = colliderFree;
            UsesDynamicLights = usesDynamicLights;
        }

        public string Id { get; }
        public string ReleaseCue { get; }
        public string ImpactCue { get; }
        public Color TrailColor { get; }
        public Color ImpactColor { get; }
        public int MaximumActivePrimitives { get; }
        public int MaximumImpactShards { get; }
        public float MaximumLifetimeSeconds { get; }
        public float MaximumDistance { get; }
        public bool RequiresPooling { get; }
        public bool ColliderFree { get; }
        public bool UsesDynamicLights { get; }
    }

    public sealed class RegionalWeaponVariantDefinition
    {
        private readonly Dictionary<string, RegionalWeaponUpgradeTierDefinition> upgradeById;

        public RegionalWeaponVariantDefinition(
            string id,
            string familyId,
            string regionId,
            string displayName,
            string description,
            string lore,
            WeaponDiscipline discipline,
            int projectedDamageBonus,
            RegionalWeaponModifierDefinition modifier,
            RegionalWeaponAcquisitionDefinition acquisition,
            RegionalWeaponUpgradeTierDefinition[] upgradeTiers,
            RegionalWeaponArtProfile art,
            RegionalWeaponVfxProfile vfx)
        {
            Id = id;
            FamilyId = familyId;
            RegionId = regionId;
            DisplayName = displayName;
            Description = description;
            Lore = lore;
            Discipline = discipline;
            ProjectedDamageBonus = projectedDamageBonus;
            Modifier = modifier;
            Acquisition = acquisition;
            UpgradeTiers = Array.AsReadOnly(
                upgradeTiers ?? Array.Empty<RegionalWeaponUpgradeTierDefinition>());
            Art = art;
            Vfx = vfx;

            upgradeById = new Dictionary<string, RegionalWeaponUpgradeTierDefinition>(
                StringComparer.Ordinal);
            foreach (var tier in UpgradeTiers)
            {
                if (tier == null || string.IsNullOrEmpty(tier.Id) ||
                    upgradeById.ContainsKey(tier.Id))
                {
                    throw new InvalidOperationException(
                        $"Weapon variant '{id}' has invalid or duplicate upgrade tier data.");
                }

                upgradeById.Add(tier.Id, tier);
            }
        }

        public string Id { get; }
        public string FamilyId { get; }
        public string RegionId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Lore { get; }
        public WeaponDiscipline Discipline { get; }
        public int ProjectedDamageBonus { get; }
        public RegionalWeaponModifierDefinition Modifier { get; }
        public RegionalWeaponAcquisitionDefinition Acquisition { get; }
        public IReadOnlyList<RegionalWeaponUpgradeTierDefinition> UpgradeTiers { get; }
        public RegionalWeaponArtProfile Art { get; }
        public RegionalWeaponVfxProfile Vfx { get; }

        public bool TryGetUpgradeTier(
            string tierId,
            out RegionalWeaponUpgradeTierDefinition tier)
        {
            return upgradeById.TryGetValue(tierId ?? string.Empty, out tier);
        }
    }

    public sealed class RegionalWeaponFamilyDefinition
    {
        public RegionalWeaponFamilyDefinition(
            string id,
            string regionId,
            string displayName,
            string lore,
            RegionalWeaponVariantDefinition[] variants)
        {
            Id = id;
            RegionId = regionId;
            DisplayName = displayName;
            Lore = lore;
            Variants = Array.AsReadOnly(
                variants ?? Array.Empty<RegionalWeaponVariantDefinition>());
        }

        public string Id { get; }
        public string RegionId { get; }
        public string DisplayName { get; }
        public string Lore { get; }
        public IReadOnlyList<RegionalWeaponVariantDefinition> Variants { get; }
    }

    [Serializable]
    public sealed class RegionalWeaponSelectionRecord
    {
        public WeaponDiscipline discipline;
        public string variantId = string.Empty;
        public string upgradeTierId = TetherfallRegionalArsenal.BaseUpgradeTierId;
    }

    [Serializable]
    public sealed class RegionalWeaponUpgradeUnlockRecord
    {
        public string variantId = string.Empty;
        public string upgradeTierId = TetherfallRegionalArsenal.BaseUpgradeTierId;
    }

    [Serializable]
    public sealed class RegionalWeaponLoadoutData
    {
        // Ownership remains in the existing unlockedEquipment ID set. This record only
        // adds independent discipline selection and semantic upgrade-tier progress.
        public List<RegionalWeaponSelectionRecord> selectedVariants =
            new List<RegionalWeaponSelectionRecord>();
        public List<RegionalWeaponUpgradeUnlockRecord> unlockedUpgradeTiers =
            new List<RegionalWeaponUpgradeUnlockRecord>();
    }

    public readonly struct RegionalWeaponResolutionInput
    {
        public RegionalWeaponResolutionInput(
            float baseDamage,
            float baseRange,
            float baseGuardDamageTakenMultiplier,
            float baseElementalBuildup,
            int comboStage,
            bool isGuardCounter,
            bool hasActivePact,
            PactResonanceAffinity activePactAffinity)
        {
            BaseDamage = baseDamage;
            BaseRange = baseRange;
            BaseGuardDamageTakenMultiplier = baseGuardDamageTakenMultiplier;
            BaseElementalBuildup = baseElementalBuildup;
            ComboStage = comboStage;
            IsGuardCounter = isGuardCounter;
            HasActivePact = hasActivePact;
            ActivePactAffinity = activePactAffinity;
        }

        public float BaseDamage { get; }
        public float BaseRange { get; }
        public float BaseGuardDamageTakenMultiplier { get; }
        public float BaseElementalBuildup { get; }
        public int ComboStage { get; }
        public bool IsGuardCounter { get; }
        public bool HasActivePact { get; }
        public PactResonanceAffinity ActivePactAffinity { get; }
    }

    public readonly struct RegionalWeaponResolvedStats
    {
        public RegionalWeaponResolvedStats(
            string variantId,
            string upgradeTierId,
            WeaponDiscipline discipline,
            RegionalWeaponElement element,
            float damage,
            float effectiveRange,
            int guardBonus,
            float guardDamageTakenMultiplier,
            float elementalBuildup,
            bool pactSynergyActive,
            float pactCommandCooldownMultiplier,
            OvertonePitch bonusPitch)
        {
            VariantId = variantId;
            UpgradeTierId = upgradeTierId;
            Discipline = discipline;
            Element = element;
            Damage = damage;
            EffectiveRange = effectiveRange;
            GuardBonus = guardBonus;
            GuardDamageTakenMultiplier = guardDamageTakenMultiplier;
            ElementalBuildup = elementalBuildup;
            PactSynergyActive = pactSynergyActive;
            PactCommandCooldownMultiplier = pactCommandCooldownMultiplier;
            BonusPitch = bonusPitch;
        }

        public string VariantId { get; }
        public string UpgradeTierId { get; }
        public WeaponDiscipline Discipline { get; }
        public RegionalWeaponElement Element { get; }
        public float Damage { get; }
        public float EffectiveRange { get; }
        public int GuardBonus { get; }
        public float GuardDamageTakenMultiplier { get; }
        public float ElementalBuildup { get; }
        public bool PactSynergyActive { get; }
        public float PactCommandCooldownMultiplier { get; }
        public OvertonePitch BonusPitch { get; }
    }

    public static class RegionalWeaponResolution
    {
        public static bool TryResolve(
            string variantId,
            string upgradeTierId,
            RegionalWeaponResolutionInput input,
            out RegionalWeaponResolvedStats result)
        {
            result = default;
            if (!TetherfallRegionalArsenal.TryGetVariant(variantId, out var variant) ||
                !variant.TryGetUpgradeTier(upgradeTierId, out var tier))
            {
                return false;
            }

            result = Resolve(variant, tier, input);
            return true;
        }

        public static RegionalWeaponResolvedStats Resolve(
            RegionalWeaponVariantDefinition variant,
            RegionalWeaponUpgradeTierDefinition tier,
            RegionalWeaponResolutionInput input)
        {
            if (variant == null)
            {
                throw new ArgumentNullException(nameof(variant));
            }

            if (tier == null || !variant.TryGetUpgradeTier(tier.Id, out var ownedTier) ||
                !ReferenceEquals(tier, ownedTier))
            {
                throw new ArgumentException(
                    "The upgrade tier must belong to the selected weapon variant.",
                    nameof(tier));
            }

            var modifier = variant.Modifier;
            var comboStage = Clamp(input.ComboStage, 0, 2);
            var pactMatch = input.HasActivePact &&
                            modifier.HasPactSynergy &&
                            input.ActivePactAffinity == modifier.PactAffinity;
            var damage = Math.Max(0f, input.BaseDamage) *
                         modifier.DamageMultiplier *
                         tier.DamageMultiplier *
                         (1f + modifier.ComboStepMultiplier * comboStage);
            if (input.IsGuardCounter)
            {
                damage *= modifier.GuardCounterDamageMultiplier;
            }

            if (pactMatch)
            {
                damage *= modifier.PactMatchDamageMultiplier * tier.PactSynergyMultiplier;
            }

            var effectiveRange = Math.Max(0f, input.BaseRange) *
                                 modifier.RangeMultiplier *
                                 tier.RangeMultiplier;
            var guardDamageTakenMultiplier = Clamp(
                Math.Max(0f, input.BaseGuardDamageTakenMultiplier) *
                modifier.GuardDamageTakenMultiplier *
                tier.GuardDamageTakenMultiplier,
                0.1f,
                1.5f);
            var elementalBuildup = Clamp(
                Math.Max(0f, input.BaseElementalBuildup) +
                modifier.ElementalBuildup +
                tier.ElementalBuildupBonus,
                0f,
                1f);

            return new RegionalWeaponResolvedStats(
                variant.Id,
                tier.Id,
                variant.Discipline,
                modifier.Element,
                damage,
                effectiveRange,
                modifier.GuardBonus + tier.GuardBonus,
                guardDamageTakenMultiplier,
                elementalBuildup,
                pactMatch,
                pactMatch ? modifier.PactCommandCooldownMultiplier : 1f,
                modifier.BonusPitch);
        }

        public static bool TryResolveSelection(
            IEnumerable<RegionalWeaponSelectionRecord> selections,
            WeaponDiscipline discipline,
            RegionalWeaponResolutionInput input,
            out RegionalWeaponResolvedStats result)
        {
            result = default;
            if (selections == null)
            {
                return false;
            }

            RegionalWeaponSelectionRecord selected = null;
            foreach (var candidate in selections)
            {
                if (candidate == null || candidate.discipline != discipline)
                {
                    continue;
                }

                if (selected != null)
                {
                    return false;
                }

                selected = candidate;
            }

            if (selected == null ||
                !TetherfallRegionalArsenal.TryGetVariant(selected.variantId, out var variant) ||
                variant.Discipline != discipline)
            {
                return false;
            }

            return TryResolve(
                selected.variantId,
                selected.upgradeTierId,
                input,
                out result);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }

    public static class TetherfallRegionalArsenal
    {
        public const string BaseUpgradeTierId = "weapon_tier.base";
        public const string ResonantUpgradeTierId = "weapon_tier.resonant";
        public const string MasterworkUpgradeTierId = "weapon_tier.masterwork";
        public const int LegacyPlayableRegionCount = 3;
        public const int CurrentPlayableRegionCount = 10;
        public const int AuthoredExpansionRegionCount = 5;
        public const int ExpectedRegionCount =
            LegacyPlayableRegionCount + CurrentPlayableRegionCount + AuthoredExpansionRegionCount;
        public const int VariantsPerFamily = 3;
        public const int MaximumArtRenderersPerWeapon = 6;
        public const int MaximumEmissivePartsPerWeapon = 2;
        public const int MaximumVfxPrimitivesPerCue = 6;
        public const int MaximumVfxImpactShards = 4;
        public const float MaximumVfxLifetimeSeconds = 0.75f;
        public const float MaximumVfxDistance = 58f;

        public const string AlderwakeRegionId = "region.alderwake";
        public const string MorrowfenRegionId = "region.morrowfen";
        public const string CinderwoldRegionId = "region.cinderwold";
        public const string GlassmereRegionId = "region.glassmere";
        public const string RimecoilRegionId = "region.rimecoil";
        public const string SunkenWealdRegionId = "region.sunken_weald";
        public const string PrismscarRegionId = "region.prismscar";
        public const string GalecradleRegionId = "region.galecradle";
        public const string LumenwasteRegionId = "region.lumenwaste";
        public const string BrassrootRegionId = "region.brassroot";
        public const string CloudbarrowRegionId = "region.cloudbarrow";
        public const string TideglassRegionId = "region.tideglass";
        public const string NightloomRegionId = "region.nightloom";
        public const string TidewaysRegionId = "region.tideways";
        public const string PelagicCourtRegionId = "region.pelagic_court";
        public const string SkylanceRegionId = "region.skylance.mount_vael";
        public const string SkyKingdomRegionId = "region.skylance.aerie_kingdom";
        public const string EmbercrownRegionId = "region.embercrown.caldera";

        public const string ExplicitStarterPathbladeId = "weapon.alderwake.canopy_shear";
        public const string ExplicitStarterRiftbowId = "weapon.alderwake.rainglass_longbow";
        public const string ExplicitStarterThreadstaffId = "weapon.alderwake.hearthroot_crook";
        public const string AeralithRegionalRewardWeaponId = "weapon.aeralith.stormlance";
        public const string EmbercrownRegionalRewardWeaponId = "weapon.embercrown.cinderwake";

        private static readonly RegionalWeaponRegionDefinition[] RegionData = CreateRegions();
        private static readonly RegionalWeaponFamilyDefinition[] FamilyData = CreateFamilies();
        private static readonly RegionalWeaponVariantDefinition[] VariantData =
            FlattenVariants(FamilyData);
        private static readonly Dictionary<string, RegionalWeaponRegionDefinition> RegionById =
            BuildLookup(RegionData, item => item.Id, "regional arsenal region");
        private static readonly Dictionary<string, RegionalWeaponFamilyDefinition> FamilyById =
            BuildLookup(FamilyData, item => item.Id, "regional weapon family");
        private static readonly Dictionary<string, RegionalWeaponFamilyDefinition> FamilyByRegionId =
            BuildLookup(FamilyData, item => item.RegionId, "regional weapon family region");
        private static readonly Dictionary<string, RegionalWeaponVariantDefinition> VariantById =
            BuildLookup(VariantData, item => item.Id, "regional weapon variant");
        private static readonly Dictionary<string, RegionalWeaponVariantDefinition> VariantBySlot =
            BuildLookup(
                VariantData,
                item => CreateSlotKey(item.RegionId, item.Discipline),
                "regional weapon discipline slot");
        private static readonly IReadOnlyList<RegionalWeaponRegionDefinition> ReadOnlyRegions =
            Array.AsReadOnly(RegionData);
        private static readonly IReadOnlyList<RegionalWeaponFamilyDefinition> ReadOnlyFamilies =
            Array.AsReadOnly(FamilyData);
        private static readonly IReadOnlyList<RegionalWeaponVariantDefinition> ReadOnlyVariants =
            Array.AsReadOnly(VariantData);

        public static IReadOnlyList<RegionalWeaponRegionDefinition> Regions => ReadOnlyRegions;
        public static IReadOnlyList<RegionalWeaponFamilyDefinition> Families => ReadOnlyFamilies;
        public static IReadOnlyList<RegionalWeaponVariantDefinition> Variants => ReadOnlyVariants;

        public static RegionalWeaponRegionDefinition GetRegion(string id)
        {
            RegionById.TryGetValue(id ?? string.Empty, out var definition);
            return definition;
        }

        public static RegionalWeaponFamilyDefinition GetFamily(string id)
        {
            FamilyById.TryGetValue(id ?? string.Empty, out var definition);
            return definition;
        }

        public static RegionalWeaponFamilyDefinition GetFamilyForRegion(string regionId)
        {
            FamilyByRegionId.TryGetValue(regionId ?? string.Empty, out var definition);
            return definition;
        }

        public static RegionalWeaponVariantDefinition GetVariant(string id)
        {
            VariantById.TryGetValue(id ?? string.Empty, out var definition);
            return definition;
        }

        public static RegionalWeaponVariantDefinition GetVariant(
            string regionId,
            WeaponDiscipline discipline)
        {
            VariantBySlot.TryGetValue(CreateSlotKey(regionId, discipline), out var definition);
            return definition;
        }

        public static bool TryGetVariant(
            string id,
            out RegionalWeaponVariantDefinition definition)
        {
            return VariantById.TryGetValue(id ?? string.Empty, out definition);
        }

        public static string GetExplicitStarterVariantId(WeaponDiscipline discipline)
        {
            return discipline switch
            {
                WeaponDiscipline.Riftbow => ExplicitStarterRiftbowId,
                WeaponDiscipline.Threadstaff => ExplicitStarterThreadstaffId,
                _ => ExplicitStarterPathbladeId
            };
        }

        public static List<EquipmentDefinition> CreateEquipmentProjection()
        {
            var projection = new List<EquipmentDefinition>(VariantData.Length);
            foreach (var variant in VariantData)
            {
                projection.Add(ProjectToEquipmentDefinition(variant));
            }

            return projection;
        }

        public static EquipmentDefinition ProjectToEquipmentDefinition(
            RegionalWeaponVariantDefinition variant)
        {
            if (variant == null)
            {
                throw new ArgumentNullException(nameof(variant));
            }

            var craftCost = new List<ResourceCost>();
            if (variant.Acquisition.Kind == RegionalWeaponAcquisitionKind.Crafting)
            {
                foreach (var ingredient in variant.Acquisition.Costs)
                {
                    craftCost.Add(new ResourceCost(ingredient.Kind, ingredient.Amount));
                }
            }

            return new EquipmentDefinition
            {
                id = variant.Id,
                displayName = variant.DisplayName,
                slot = EquipmentSlot.Weapon,
                description = $"{variant.Description} {variant.Modifier.GameplayNote}",
                discipline = variant.Discipline,
                damageBonus = variant.ProjectedDamageBonus,
                favorPrice =
                    variant.Acquisition.Kind == RegionalWeaponAcquisitionKind.Vendor
                        ? variant.Acquisition.FavorPrice
                        : 0,
                restorationRewardOnly =
                    variant.Acquisition.Kind != RegionalWeaponAcquisitionKind.Crafting &&
                    variant.Acquisition.Kind != RegionalWeaponAcquisitionKind.Vendor,
                craftCost = craftCost
            };
        }

        public static int AppendEquipmentProjection(ICollection<EquipmentDefinition> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var occupiedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var existing in destination)
            {
                if (existing != null && !string.IsNullOrEmpty(existing.id))
                {
                    occupiedIds.Add(existing.id);
                }
            }

            foreach (var variant in VariantData)
            {
                if (!occupiedIds.Add(variant.Id))
                {
                    throw new InvalidOperationException(
                        $"Equipment projection would duplicate stable ID '{variant.Id}'.");
                }
            }

            foreach (var variant in VariantData)
            {
                destination.Add(ProjectToEquipmentDefinition(variant));
            }

            return VariantData.Length;
        }

        private static RegionalWeaponRegionDefinition[] CreateRegions()
        {
            return new[]
            {
                Region(
                    AlderwakeRegionId,
                    "Alderwake Basin",
                    RegionalWeaponRegionStatus.LegacyPlayable,
                    "Rainroot routewrights tune practical field instruments to the restored Hearthgate."),
                Region(
                    MorrowfenRegionId,
                    "Morrowfen",
                    RegionalWeaponRegionStatus.LegacyPlayable,
                    "Fen ferriers balance reed, lantern glass, and stillwater resonance."),
                Region(
                    CinderwoldRegionId,
                    "Cinderwold Verge",
                    RegionalWeaponRegionStatus.LegacyPlayable,
                    "Ash-route salvagers bind cooling copper around stubborn ember cores."),
                Region(
                    GlassmereRegionId,
                    "Glassmere Steppe",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Choirglass smiths cut each profile to preserve the Arch's elusive fifth note."),
                Region(
                    RimecoilRegionId,
                    "Rimecoil Range",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "High-route tenders measure blue ice, crosswind, and warm ore in equal turns."),
                Region(
                    SunkenWealdRegionId,
                    "Sunken Weald",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Crownroot growers shape amber-resin arms that flex instead of fighting the flood."),
                Region(
                    PrismscarRegionId,
                    "Prismscar Downs",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Array keepers trap split sunset in violet flint and release it along measured facets."),
                Region(
                    GalecradleRegionId,
                    "Galecradle Mesa",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Kitewrights leave deliberate gaps so mesa winds become part of every weapon."),
                Region(
                    LumenwasteRegionId,
                    "Lumenwaste Reach",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Lens walkers forge around shadows, treating withheld light as a working edge."),
                Region(
                    BrassrootRegionId,
                    "Brassroot Deeps",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Foundry descendants quench living metal roots without severing their resonance."),
                Region(
                    CloudbarrowRegionId,
                    "Cloudbarrow Expanse",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Grasskeepers build open-framed arms that leave a ceremonial road for weather."),
                Region(
                    TideglassRegionId,
                    "Tideglass Strand",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Drymark readers copy needle reefs into black-glass silhouettes above the tide."),
                Region(
                    NightloomRegionId,
                    "Nightloom Hollow",
                    RegionalWeaponRegionStatus.CurrentPlayable,
                    "Canopy tenders knot violet glow into darkstrand frames that never need flame."),
                Region(
                    TidewaysRegionId,
                    "Tideways",
                    RegionalWeaponRegionStatus.AuthoredExpansion,
                    "Lockkeepers turn canal pressure, shipline rhythm, and river law into civic arms."),
                Region(
                    PelagicCourtRegionId,
                    "Pelagic Court",
                    RegionalWeaponRegionStatus.AuthoredExpansion,
                    "Court artificers grow nacre around abyssal current-knots beneath the outer sea."),
                Region(
                    SkylanceRegionId,
                    "Mount Vael Skylance",
                    RegionalWeaponRegionStatus.AuthoredExpansion,
                    "Cliff villages lash hoarfrost steel to nestwood, light enough for the high ascent."),
                Region(
                    SkyKingdomRegionId,
                    "Aeralith Sky Kingdom",
                    RegionalWeaponRegionStatus.AuthoredExpansion,
                    "First-Wind stewards tune cloud-throne instruments to open air rather than walls."),
                Region(
                    EmbercrownRegionId,
                    "Embercrown Caldera",
                    RegionalWeaponRegionStatus.AuthoredExpansion,
                    "Crownforgers draw heat from branching lava rivers without sealing the volcano's voice.")
            };
        }

        private static RegionalWeaponFamilyDefinition[] CreateFamilies()
        {
            return new[]
            {
                CreateFamily(
                    AlderwakeRegionId,
                    "alderwake",
                    "rainroot_instruments",
                    "Rainroot Instruments",
                    "Three balanced route tools descended from the first Hearthgate survey kit.",
                    "rainroot ribs, copper braces, and one rain-bright focus",
                    Seed(
                        "canopy_shear",
                        "Canopy-Shear",
                        "A hooked pathblade that gathers speed through a clean three-cut route.",
                        RegionalWeaponIdentity.Combo,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.alderwake.routework_blade",
                        "Alderwake routework bench"),
                    Seed(
                        "rainglass_longbow",
                        "Rainglass Longbow",
                        "A long-limbed Riftbow that carries a narrow line through basin rain.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.Vendor,
                        "vendor.alderwake.hearthmarket",
                        "Hearthmarket commons counter"),
                    Seed(
                        "hearthroot_crook",
                        "Hearthroot Crook",
                        "A guarding Threadstaff whose root fork steadies an answering Pact.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "quest.alderwake.hearthgate_restoration",
                        "Hearthgate restoration"),
                    RegionalWeaponElement.Grove,
                    PactResonanceAffinity.Grove,
                    ResourceKind.Timber,
                    ResourceKind.Resin,
                    ResourceKind.Fiber,
                    new Color(0.22f, 0.54f, 0.34f),
                    new Color(0.35f, 0.9f, 0.74f),
                    0),
                CreateFamily(
                    MorrowfenRegionId,
                    "morrowfen",
                    "lantern_reed_armory",
                    "Lantern-Reed Armory",
                    "Fen ferriers keep each piece buoyant, quiet, and legible through fog.",
                    "reedstone curves, siltglass channels, and suspended lantern beads",
                    Seed(
                        "reedwake_falx",
                        "Reedwake Falx",
                        "A crescent blade that turns a guarded impact into a low returning cut.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.morrowfen.reedwork_falx",
                        "Veyrwater reedwork bench"),
                    Seed(
                        "stillwater_needlebow",
                        "Stillwater Needlebow",
                        "A silent bow that keeps full line tension across wet channels.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.Vendor,
                        "vendor.morrowfen.provisioner",
                        "Morrowfen fen pantry"),
                    Seed(
                        "siltbell_staff",
                        "Siltbell Staff",
                        "A tide-tuned bellstaff that deepens Ring overtones beside Tide Pacts.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.BossReward,
                        "boss.skyvault_warden",
                        "Skyvault Warden"),
                    RegionalWeaponElement.Tide,
                    PactResonanceAffinity.Tide,
                    ResourceKind.Resin,
                    ResourceKind.Herbs,
                    ResourceKind.Chimesalt,
                    new Color(0.16f, 0.46f, 0.48f),
                    new Color(0.35f, 0.88f, 0.8f),
                    1),
                CreateFamily(
                    CinderwoldRegionId,
                    "cinderwold",
                    "ash_copper_array",
                    "Ash-Copper Array",
                    "Relay smiths preserve a live ember seam inside work-hardened copper.",
                    "split slag planes, copper relay cages, and banked ember seams",
                    Seed(
                        "coalbright_sever",
                        "Coalbright Sever",
                        "A heavy-edged blade whose final route cut vents a bright ember seam.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.cinderwold.salvage_blade",
                        "Cinderwold salvage bench"),
                    Seed(
                        "skyfire_relay_bow",
                        "Skyfire Relay Bow",
                        "A copper relay bow that calls Ember and Gale responders into its firing line.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "quest.cinderwold.ember_compact",
                        "Ember Compact resolution"),
                    Seed(
                        "ashbell_censer",
                        "Ashbell Censer",
                        "A braced censer-staff that catches pressure before ringing it outward.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.BossReward,
                        "trial.ashwake_crucible",
                        "Ashwake Crucible"),
                    RegionalWeaponElement.Ember,
                    PactResonanceAffinity.Ember,
                    ResourceKind.Ore,
                    ResourceKind.Stone,
                    ResourceKind.Chimesalt,
                    new Color(0.48f, 0.2f, 0.12f),
                    new Color(1f, 0.48f, 0.2f),
                    2),
                CreateFamily(
                    GlassmereRegionId,
                    "glassmere",
                    "choirglass_ensemble",
                    "Choirglass Ensemble",
                    "Every piece carries a different interval of the rain-polished Arch.",
                    "clear offset planes, mirrored edges, and five-note focus clusters",
                    Seed(
                        "fifth_note_edge",
                        "Fifth-Note Edge",
                        "An offset glassblade that rewards completing rather than repeating a phrase.",
                        RegionalWeaponIdentity.Combo,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.glassmere.refraction_niche",
                        "Refraction Niche"),
                    Seed(
                        "refraction_arc",
                        "Refraction Arc",
                        "A faceted Riftbow that bends a long line around its luminous nock.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.glassmere.mirror_palisade",
                        "Mirror Palisade"),
                    Seed(
                        "choirglass_baton",
                        "Choirglass Baton",
                        "A prism baton that supplies a Pulse color to coordinated Pact patterns.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.glassmere.choirglass_baton",
                        "Glassmere tuning fork"),
                    RegionalWeaponElement.Prism,
                    PactResonanceAffinity.Prism,
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    ResourceKind.Fiber,
                    new Color(0.38f, 0.7f, 0.74f),
                    new Color(0.75f, 0.97f, 0.92f),
                    3),
                CreateFamily(
                    RimecoilRegionId,
                    "rimecoil",
                    "hoarfold_arsenal",
                    "Hoarfold Arsenal",
                    "Warm ore cores keep these switchback instruments from freezing closed.",
                    "blue-ice forks, dark ore spines, and wind-readable sextant vanes",
                    Seed(
                        "blue_cleft_fang",
                        "Blue-Cleft Fang",
                        "A short ice fang that turns a perfect brace into a climbing countercut.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.rimecoil.blue_cleft",
                        "Blue Cleft Cache"),
                    Seed(
                        "sextant_bow",
                        "Sextant Bow",
                        "Twin sight vanes measure long shots against the range's crosswind.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.rimecoil.coil_redoubt",
                        "Coil Redoubt"),
                    Seed(
                        "rimecoil_tuning_rod",
                        "Rimecoil Tuning Rod",
                        "A frost rod that aligns its Ring with high-route Gale Pacts.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.rimecoil.tuning_rod",
                        "Rimecoil Turn"),
                    RegionalWeaponElement.Frost,
                    PactResonanceAffinity.Gale,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    ResourceKind.Fiber,
                    new Color(0.24f, 0.38f, 0.58f),
                    new Color(0.73f, 0.9f, 1f),
                    4),
                CreateFamily(
                    SunkenWealdRegionId,
                    "sunken_weald",
                    "hollow_crown_tools",
                    "Hollow-Crown Tools",
                    "Living root laminates hold a dry amber chamber at each instrument's heart.",
                    "broad crownroot hooks, amber joints, and flood-flexed limbs",
                    Seed(
                        "crownroot_bill",
                        "Crownroot Bill",
                        "A root-hook blade that flexes through successive cuts without losing reach.",
                        RegionalWeaponIdentity.Combo,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.sunken_weald.hollow_heart",
                        "Hollow-Heart Cache"),
                    Seed(
                        "amberwake_bow",
                        "Amberwake Bow",
                        "A resin-laminated bow that seeds Grove resonance along its bolt line.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.sunken_weald.root_snare",
                        "Root Snare"),
                    Seed(
                        "hollow_heart_staff",
                        "Hollow-Heart Staff",
                        "A dry-cored staff that reinforces guards while a Grove Pact answers.",
                        RegionalWeaponIdentity.Hybrid,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.sunken_weald.hollow_heart_staff",
                        "Weald Root-Loom"),
                    RegionalWeaponElement.Grove,
                    PactResonanceAffinity.Grove,
                    ResourceKind.Resin,
                    ResourceKind.Timber,
                    ResourceKind.Herbs,
                    new Color(0.3f, 0.34f, 0.16f),
                    new Color(0.88f, 0.66f, 0.22f),
                    5),
                CreateFamily(
                    PrismscarRegionId,
                    "prismscar",
                    "split_sun_array",
                    "Split-Sun Array",
                    "Violet flint stores the sunset as a sequence of deliberate afterimages.",
                    "sawtooth flint edges, split copper vanes, and violet afterimage cores",
                    Seed(
                        "afterimage_splitter",
                        "Afterimage Splitter",
                        "A flint pathblade whose third cut arrives with a stored afterimage.",
                        RegionalWeaponIdentity.Combo,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.prismscar.shadowline",
                        "Shadowline Cache"),
                    Seed(
                        "meridian_prismbow",
                        "Meridian Prismbow",
                        "A split-lens bow that layers prism buildup over a measured long line.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.prismscar.prism_trench",
                        "Prism Trench"),
                    Seed(
                        "facet_harp_staff",
                        "Facet-Harp Staff",
                        "Nine short facets answer Prism Pacts with a quickened command cadence.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.prismscar.facet_harp_staff",
                        "Prismscar Meridian"),
                    RegionalWeaponElement.Prism,
                    PactResonanceAffinity.Prism,
                    ResourceKind.Stone,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    new Color(0.42f, 0.26f, 0.48f),
                    new Color(0.93f, 0.59f, 0.3f),
                    0),
                CreateFamily(
                    GalecradleRegionId,
                    "galecradle",
                    "kitebone_rig",
                    "Kitebone Rig",
                    "Open kitebone frames turn mesa crosswinds into lift, line, and recoil.",
                    "ochre grips, pale kite ribs, and turquoise wind gaps",
                    Seed(
                        "kitebone_cutter",
                        "Kitebone Cutter",
                        "A hollow-backed blade that advances farther as its combo catches wind.",
                        RegionalWeaponIdentity.Combo,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.galecradle.high_perch",
                        "High-Perch Cache"),
                    Seed(
                        "crosswind_yoke",
                        "Crosswind Yoke",
                        "A wide yoke-bow that keeps a stable line beyond ordinary Riftbow reach.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.galecradle.kite_ring",
                        "Kite Ring"),
                    Seed(
                        "updraft_shepherd",
                        "Updraft Shepherd",
                        "A forked staff that lets Gale Pacts answer with less command recovery.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.galecradle.updraft_shepherd",
                        "Galecradle Yoke"),
                    RegionalWeaponElement.Gale,
                    PactResonanceAffinity.Gale,
                    ResourceKind.Fiber,
                    ResourceKind.Ore,
                    ResourceKind.Resin,
                    new Color(0.48f, 0.32f, 0.16f),
                    new Color(0.25f, 0.88f, 0.79f),
                    1),
                CreateFamily(
                    LumenwasteRegionId,
                    "lumenwaste",
                    "walking_lens_arms",
                    "Walking-Lens Arms",
                    "Their dark apertures make phosphor light useful by giving it a boundary.",
                    "muted gold planes, black apertures, and west-facing lens beads",
                    Seed(
                        "noonshadow_blade",
                        "Noonshadow Blade",
                        "A black-aperture edge that flashes only when a combo crosses its shortest shadow.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.lumenwaste.lens_shadow",
                        "Lens-Shadow Cache"),
                    Seed(
                        "walking_lens_bow",
                        "Walking-Lens Bow",
                        "Five small lenses keep a radiant shot coherent across dune haze.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.lumenwaste.lens_walk",
                        "Lens Walk"),
                    Seed(
                        "beacon_cantor",
                        "Beacon Cantor",
                        "A shaded staff that adds Pulse color to a Prism Pact's measured answer.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.lumenwaste.beacon_cantor",
                        "Lumenwaste Walking Line"),
                    RegionalWeaponElement.Radiance,
                    PactResonanceAffinity.Prism,
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    ResourceKind.Fiber,
                    new Color(0.42f, 0.38f, 0.12f),
                    new Color(1f, 0.92f, 0.34f),
                    2),
                CreateFamily(
                    BrassrootRegionId,
                    "brassroot",
                    "quenchroot_castings",
                    "Quenchroot Castings",
                    "Each casting follows a branching root channel instead of a straight mold seam.",
                    "branching brass spines, dark quench grips, and warm casting nodes",
                    Seed(
                        "quenchroot_cleaver",
                        "Quenchroot Cleaver",
                        "A branching cleaver that braces hard, then releases its stored casting force.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.brassroot.quenched_ledger",
                        "Quenched Ledger"),
                    Seed(
                        "casting_line_bow",
                        "Casting-Line Bow",
                        "A root-cast bow whose heated line carries stone-heavy impact at range.",
                        RegionalWeaponIdentity.Hybrid,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.brassroot.foundry_terrace",
                        "Foundry Terrace"),
                    Seed(
                        "pouring_root_staff",
                        "Pouring-Root Staff",
                        "A forked casting staff that steadies Ember Pacts inside its cooling ring.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.brassroot.pouring_root_staff",
                        "Brassroot Intake"),
                    RegionalWeaponElement.Stone,
                    PactResonanceAffinity.Ember,
                    ResourceKind.Ore,
                    ResourceKind.Stone,
                    ResourceKind.Resin,
                    new Color(0.46f, 0.24f, 0.12f),
                    new Color(0.92f, 0.57f, 0.2f),
                    3),
                CreateFamily(
                    CloudbarrowRegionId,
                    "cloudbarrow",
                    "weatherlane_set",
                    "Weatherlane Set",
                    "Barrow keepers leave every frame open on the north side for passing weather.",
                    "grass-bound stone grips, open capstones, and pale wind streamers",
                    Seed(
                        "lee_wake_sabre",
                        "Lee-Wake Sabre",
                        "A light sabre that shelters its wielder before opening into a quick counter.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.cloudbarrow.lee_vault",
                        "Lee-Side Vault"),
                    Seed(
                        "weatherlane_bow",
                        "Weatherlane Bow",
                        "An open-limbed bow that draws a long sightline through moving grass.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.cloudbarrow.barrow_gate",
                        "Barrow Gate"),
                    Seed(
                        "open_cairn_staff",
                        "Open-Cairn Staff",
                        "A three-capstone staff that welcomes Gale responders into its ward.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.cloudbarrow.open_cairn_staff",
                        "Cloudbarrow Processional"),
                    RegionalWeaponElement.Gale,
                    PactResonanceAffinity.Gale,
                    ResourceKind.Herbs,
                    ResourceKind.Fiber,
                    ResourceKind.Stone,
                    new Color(0.3f, 0.42f, 0.24f),
                    new Color(0.78f, 0.92f, 0.67f),
                    4),
                CreateFamily(
                    TideglassRegionId,
                    "tideglass",
                    "drymark_needles",
                    "Drymark Needles",
                    "Black-sand ferriers copy the reef above the waterline, where every edge has a twin.",
                    "black-glass grips, thin sea-stack profiles, and cyan mirror seams",
                    Seed(
                        "drymark_scimitar",
                        "Drymark Scimitar",
                        "A mirrored scimitar that layers tide pressure across a flowing combo.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.tideglass.stranded_survey",
                        "Stranded Survey"),
                    Seed(
                        "ebbmirror_bow",
                        "Ebbmirror Bow",
                        "A needle-limbed bow that keeps its reflection-line over shallow water.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.tideglass.tidebreak_spines",
                        "Tidebreak Spines"),
                    Seed(
                        "needle_reef_staff",
                        "Needle-Reef Staff",
                        "Six narrow foci turn a guarded Ring into a Tide Pact invitation.",
                        RegionalWeaponIdentity.Hybrid,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.tideglass.needle_reef_staff",
                        "Tideglass Drymark"),
                    RegionalWeaponElement.Tide,
                    PactResonanceAffinity.Tide,
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    ResourceKind.Ore,
                    new Color(0.08f, 0.13f, 0.2f),
                    new Color(0.32f, 0.78f, 0.9f),
                    5),
                CreateFamily(
                    NightloomRegionId,
                    "nightloom",
                    "moonlattice_weave",
                    "Moonlattice Weave",
                    "Darkstrand frames carry violet canopy light without an exposed flame.",
                    "woven dark roots, suspended glow knots, and crescent lattice gaps",
                    Seed(
                        "darkstrand_sickle",
                        "Darkstrand Sickle",
                        "A crescent rootblade whose final cut blooms from a deliberately unlit seam.",
                        RegionalWeaponIdentity.Combo,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.nightloom.glow_knot",
                        "Glow-Knot Cache"),
                    Seed(
                        "moonlattice_bow",
                        "Moonlattice Bow",
                        "A woven bow that sends Gloam through a line of thirteen small crossings.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.region.nightloom.loom_ambush",
                        "Loom Ambush"),
                    Seed(
                        "glow_knot_spindle",
                        "Glow-Knot Spindle",
                        "A compact staff that shortens a Gloam Pact's answer after a clean guard.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.nightloom.glow_knot_spindle",
                        "Nightloom high weave"),
                    RegionalWeaponElement.Gloam,
                    PactResonanceAffinity.Gloam,
                    ResourceKind.Herbs,
                    ResourceKind.Resin,
                    ResourceKind.Fiber,
                    new Color(0.13f, 0.1f, 0.2f),
                    new Color(0.68f, 0.38f, 0.9f),
                    0),
                CreateFamily(
                    TidewaysRegionId,
                    "tideways",
                    "seven_sluice_office",
                    "Seven-Sluice Office",
                    "Licensed lockkeepers carry civic weapons keyed to canal gates and ship bells.",
                    "copper lock teeth, blue shipline arcs, and seven pressure windows",
                    Seed(
                        "lockwater_cutlass",
                        "Lockwater Cutlass",
                        "A canal cutlass that stores a guarded surge for the next close route.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.Vendor,
                        "vendor.tideways.arsenal_ropewright",
                        "Arsenal Ropewright"),
                    Seed(
                        "canal_crown_bow",
                        "Canal-Crown Bow",
                        "A shipline bow awarded for breaking the river guardian's crossing hold.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.BossReward,
                        "boss.tideways.orrun_river_guardian",
                        "Orrun, River Guardian"),
                    Seed(
                        "seven_sluice_crozier",
                        "Seven-Sluice Crozier",
                        "A pressure-window staff that coordinates Tide Pacts through canal cadence.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "quest.tideways.bellfoundry_compact",
                        "Bellfoundry Compact"),
                    RegionalWeaponElement.Tide,
                    PactResonanceAffinity.Tide,
                    ResourceKind.Ore,
                    ResourceKind.Fiber,
                    ResourceKind.Chimesalt,
                    new Color(0.18f, 0.42f, 0.5f),
                    new Color(0.34f, 0.9f, 0.86f),
                    1),
                CreateFamily(
                    PelagicCourtRegionId,
                    "pelagic_court",
                    "nacre_audience_regalia",
                    "Nacre Audience Regalia",
                    "The Court grows each arm as a petition: pressure outside, living current within.",
                    "layered nacre plates, abyss-dark cores, and pearl current channels",
                    Seed(
                        "abyssal_petition",
                        "Abyssal Petition",
                        "A petition-blade that gains force as its cuts descend through water pressure.",
                        RegionalWeaponIdentity.Combo,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "quest.pelagic_court.shoal_compact",
                        "Shoal Compact"),
                    Seed(
                        "pearlcurrent_arc",
                        "Pearlcurrent Arc",
                        "A nacre bow reclaimed from the usurper beneath the current throne.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.BossReward,
                        "boss.pelagic_court.nacre_regent",
                        "Nacre Regent"),
                    Seed(
                        "throne_tide_scepter",
                        "Throne-Tide Scepter",
                        "A court scepter that braids Tide Pact answers into a pressure ward.",
                        RegionalWeaponIdentity.Hybrid,
                        RegionalWeaponAcquisitionKind.DiscoveryReward,
                        "secret.pelagic_court.throne_current",
                        "Throne Current archive"),
                    RegionalWeaponElement.Tide,
                    PactResonanceAffinity.Tide,
                    ResourceKind.Chimesalt,
                    ResourceKind.Resin,
                    ResourceKind.Ore,
                    new Color(0.12f, 0.3f, 0.48f),
                    new Color(0.7f, 0.94f, 0.9f),
                    2),
                CreateFamily(
                    SkylanceRegionId,
                    "skylance",
                    "summit_nest_armory",
                    "Summit-Nest Armory",
                    "Cliff smiths remove every gram that does not survive frost, fall, or first flight.",
                    "hoarfrost steel, pale nestwood spars, and red summit pennants",
                    Seed(
                        "avalanche_talon",
                        "Avalanche Talon",
                        "A braced climbing blade that turns absorbed force into a descending counter.",
                        RegionalWeaponIdentity.Guard,
                        RegionalWeaponAcquisitionKind.Crafting,
                        "craft.skylance.cliff_forge",
                        "Skylance cliff forge"),
                    Seed(
                        "nestwind_greatbow",
                        "Nestwind Greatbow",
                        "A high-nest bow whose long line remains stable over ravines.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.EncounterReward,
                        "encounter.skylance.alpine.glass_ledge",
                        "Glass Ledge flight-Pact encounter"),
                    Seed(
                        "summit_vane",
                        "Summit Vane",
                        "A light vane-staff awarded when a flight-capable Pact chooses the ascent.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.PactReward,
                        "milestone.world_wonders.tide_to_sky.bond_flight_pact",
                        "First Flight accord"),
                    RegionalWeaponElement.Frost,
                    PactResonanceAffinity.Gale,
                    ResourceKind.Ore,
                    ResourceKind.Fiber,
                    ResourceKind.Chimesalt,
                    new Color(0.26f, 0.36f, 0.5f),
                    new Color(0.75f, 0.91f, 1f),
                    3),
                CreateFamily(
                    SkyKingdomRegionId,
                    "aeralith",
                    "first_wind_regalia",
                    "First-Wind Regalia",
                    "Aerie stewards tune royal instruments to the oldest current above the cloud deck.",
                    "white cloudmetal, open gold frames, and storm-blue resonance sails",
                    Seed(
                        "stormlance",
                        "Aeralith Stormlance",
                        "An open-frame skyblade granted after Aeralith recognizes a grounded Wayfinder.",
                        RegionalWeaponIdentity.Hybrid,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "milestone.world_wonders.tide_to_sky.claim_aeralith_reward",
                        "Aeralith reward rite"),
                    Seed(
                        "horizon_harpbow",
                        "Horizon Harpbow",
                        "A many-stringed bow taken from the Tempest Castellan's broken standard.",
                        RegionalWeaponIdentity.Range,
                        RegionalWeaponAcquisitionKind.BossReward,
                        "socket.skylance.boss.storm_throne",
                        "Storm Throne"),
                    Seed(
                        "first_wind_scepter",
                        "First-Wind Scepter",
                        "A royal windstaff that gives Gale Pacts room to answer at full cadence.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "socket.skylance.quest.archive_weather_ledger",
                        "Weather Ledger"),
                    RegionalWeaponElement.Storm,
                    PactResonanceAffinity.Gale,
                    ResourceKind.Chimesalt,
                    ResourceKind.Fiber,
                    ResourceKind.Ore,
                    new Color(0.45f, 0.58f, 0.72f),
                    new Color(0.94f, 0.82f, 0.42f),
                    4),
                CreateFamily(
                    EmbercrownRegionId,
                    "embercrown",
                    "caldera_crownforge",
                    "Caldera Crownforge",
                    "Crownforgers leave branching channels open so each instrument can breathe with the lava rivers.",
                    "obsidian crowns, bright magma channels, and heat-shielded copper grips",
                    Seed(
                        "cinderwake",
                        "Cinderwake",
                        "An obsidian pathblade whose completed cut route vents a controlled lava tongue.",
                        RegionalWeaponIdentity.Elemental,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "milestone.world_wonders.embercrown.claim_cinderwake",
                        "Stabilized heartforge"),
                    Seed(
                        "magma_vein_bow",
                        "Magma-Vein Bow",
                        "A heat-channel bow claimed from the guardian above the branching lava rivers.",
                        RegionalWeaponIdentity.Hybrid,
                        RegionalWeaponAcquisitionKind.BossReward,
                        "boss.embercrown.pyreheart_matriarch",
                        "Pyreheart Matriarch"),
                    Seed(
                        "crownforge_thyrsus",
                        "Crownforge Thyrsus",
                        "A vented forge-staff that invites Ember Pacts into its pressure lattice.",
                        RegionalWeaponIdentity.PactSynergy,
                        RegionalWeaponAcquisitionKind.QuestReward,
                        "quest.embercrown.three_forges",
                        "Three Forges"),
                    RegionalWeaponElement.Ember,
                    PactResonanceAffinity.Ember,
                    ResourceKind.Ore,
                    ResourceKind.Stone,
                    ResourceKind.Chimesalt,
                    new Color(0.25f, 0.08f, 0.045f),
                    new Color(1f, 0.36f, 0.08f),
                    5)
            };
        }

        private static RegionalWeaponRegionDefinition Region(
            string id,
            string displayName,
            RegionalWeaponRegionStatus status,
            string lore)
        {
            return new RegionalWeaponRegionDefinition(id, displayName, status, lore);
        }

        private static VariantSeed Seed(
            string slug,
            string displayName,
            string description,
            RegionalWeaponIdentity identity,
            RegionalWeaponAcquisitionKind acquisitionKind,
            string sourceId,
            string sourceDescription)
        {
            return new VariantSeed(
                slug,
                displayName,
                description,
                identity,
                acquisitionKind,
                sourceId,
                sourceDescription);
        }

        private static RegionalWeaponFamilyDefinition CreateFamily(
            string regionId,
            string regionSlug,
            string familySlug,
            string displayName,
            string lore,
            string silhouetteDirection,
            VariantSeed pathblade,
            VariantSeed riftbow,
            VariantSeed threadstaff,
            RegionalWeaponElement element,
            PactResonanceAffinity pactAffinity,
            ResourceKind primaryIngredient,
            ResourceKind secondaryIngredient,
            ResourceKind catalystIngredient,
            Color primaryColor,
            Color accentColor,
            int balancePattern)
        {
            var familyId = $"weapon_family.{regionSlug}.{familySlug}";
            var ingredients = new[]
            {
                primaryIngredient,
                secondaryIngredient,
                catalystIngredient
            };
            return new RegionalWeaponFamilyDefinition(
                familyId,
                regionId,
                displayName,
                lore,
                new[]
                {
                    CreateVariant(
                        familyId,
                        regionId,
                        regionSlug,
                        pathblade,
                        WeaponDiscipline.Pathblade,
                        element,
                        pactAffinity,
                        ingredients,
                        primaryColor,
                        accentColor,
                        silhouetteDirection,
                        lore,
                        balancePattern),
                    CreateVariant(
                        familyId,
                        regionId,
                        regionSlug,
                        riftbow,
                        WeaponDiscipline.Riftbow,
                        element,
                        pactAffinity,
                        ingredients,
                        primaryColor,
                        accentColor,
                        silhouetteDirection,
                        lore,
                        balancePattern),
                    CreateVariant(
                        familyId,
                        regionId,
                        regionSlug,
                        threadstaff,
                        WeaponDiscipline.Threadstaff,
                        element,
                        pactAffinity,
                        ingredients,
                        primaryColor,
                        accentColor,
                        silhouetteDirection,
                        lore,
                        balancePattern)
                });
        }

        private static RegionalWeaponVariantDefinition CreateVariant(
            string familyId,
            string regionId,
            string regionSlug,
            VariantSeed seed,
            WeaponDiscipline discipline,
            RegionalWeaponElement element,
            PactResonanceAffinity pactAffinity,
            ResourceKind[] ingredients,
            Color primaryColor,
            Color accentColor,
            string silhouetteDirection,
            string familyLore,
            int balancePattern)
        {
            var id = $"weapon.{regionSlug}.{seed.Slug}";
            var artId = $"weapon_art.{regionSlug}.{seed.Slug}";
            var vfxId = $"weapon_vfx.{regionSlug}.{seed.Slug}";
            var modifier = CreateModifier(
                seed.Identity,
                discipline,
                element,
                pactAffinity,
                balancePattern);
            var acquisition = CreateAcquisition(
                seed,
                discipline,
                ingredients,
                balancePattern);
            return new RegionalWeaponVariantDefinition(
                id,
                familyId,
                regionId,
                seed.DisplayName,
                seed.Description,
                $"{familyLore} {seed.DisplayName} is its {discipline} reading.",
                discipline,
                GetProjectedDamageBonus(seed.Identity, discipline, balancePattern),
                modifier,
                acquisition,
                CreateUpgradePath(ingredients),
                CreateArtProfile(
                    artId,
                    discipline,
                    primaryColor,
                    accentColor,
                    silhouetteDirection),
                CreateVfxProfile(vfxId, discipline, element, primaryColor, accentColor));
        }

        private static RegionalWeaponAcquisitionDefinition CreateAcquisition(
            VariantSeed seed,
            WeaponDiscipline discipline,
            ResourceKind[] ingredients,
            int balancePattern)
        {
            var costs = seed.AcquisitionKind == RegionalWeaponAcquisitionKind.Crafting
                ? new[]
                {
                    new RegionalWeaponIngredient(ingredients[0], discipline == WeaponDiscipline.Pathblade ? 4 : 3),
                    new RegionalWeaponIngredient(ingredients[1], 3),
                    new RegionalWeaponIngredient(ingredients[2], 2)
                }
                : Array.Empty<RegionalWeaponIngredient>();
            var favorPrice = seed.AcquisitionKind == RegionalWeaponAcquisitionKind.Vendor
                ? 38 + balancePattern * 3
                : 0;
            return new RegionalWeaponAcquisitionDefinition(
                seed.AcquisitionKind,
                seed.SourceId,
                seed.SourceDescription,
                favorPrice,
                costs);
        }

        private static RegionalWeaponUpgradeTierDefinition[] CreateUpgradePath(
            ResourceKind[] ingredients)
        {
            return new[]
            {
                new RegionalWeaponUpgradeTierDefinition(
                    BaseUpgradeTierId,
                    string.Empty,
                    "Field-forged",
                    1f,
                    1f,
                    0,
                    1f,
                    0f,
                    1f,
                    Array.Empty<RegionalWeaponIngredient>()),
                new RegionalWeaponUpgradeTierDefinition(
                    ResonantUpgradeTierId,
                    BaseUpgradeTierId,
                    "Resonant",
                    1.06f,
                    1.02f,
                    1,
                    0.98f,
                    0.04f,
                    1.02f,
                    new[]
                    {
                        new RegionalWeaponIngredient(ingredients[0], 3),
                        new RegionalWeaponIngredient(ingredients[1], 2),
                        new RegionalWeaponIngredient(ingredients[2], 1)
                    }),
                new RegionalWeaponUpgradeTierDefinition(
                    MasterworkUpgradeTierId,
                    ResonantUpgradeTierId,
                    "Regional masterwork",
                    1.12f,
                    1.04f,
                    2,
                    0.96f,
                    0.08f,
                    1.04f,
                    new[]
                    {
                        new RegionalWeaponIngredient(ingredients[0], 6),
                        new RegionalWeaponIngredient(ingredients[1], 4),
                        new RegionalWeaponIngredient(ingredients[2], 3)
                    })
            };
        }

        private static RegionalWeaponModifierDefinition CreateModifier(
            RegionalWeaponIdentity identity,
            WeaponDiscipline discipline,
            RegionalWeaponElement element,
            PactResonanceAffinity pactAffinity,
            int balancePattern)
        {
            var damage = 1f;
            var combo = 0.04f;
            var range = 1f;
            var guardBonus = 1;
            var guardTaken = 0.96f;
            var guardCounter = 1.05f;
            var buildup = 0.14f;
            var pactDamage = 1.06f;
            var pactCooldown = 0.96f;
            var hasPactSynergy = identity == RegionalWeaponIdentity.PactSynergy ||
                                 identity == RegionalWeaponIdentity.Hybrid;
            var note = string.Empty;

            switch (identity)
            {
                case RegionalWeaponIdentity.Combo:
                    damage = 1.02f;
                    combo = 0.085f;
                    range = 0.98f;
                    guardBonus = 1;
                    guardTaken = 0.97f;
                    guardCounter = 1.04f;
                    buildup = 0.12f;
                    note = "Successive combo stages gain extra force.";
                    break;
                case RegionalWeaponIdentity.Range:
                    damage = 0.98f;
                    combo = 0.025f;
                    range = 1.2f;
                    guardBonus = 0;
                    guardTaken = 1f;
                    guardCounter = 1f;
                    buildup = 0.13f;
                    note = "Its authored line reaches farther than the discipline baseline.";
                    break;
                case RegionalWeaponIdentity.Guard:
                    damage = 0.97f;
                    combo = 0.035f;
                    range = 1f;
                    guardBonus = 4;
                    guardTaken = 0.86f;
                    guardCounter = 1.16f;
                    buildup = 0.1f;
                    note = "Guarded pressure is reduced and clean counters hit harder.";
                    break;
                case RegionalWeaponIdentity.Elemental:
                    damage = 1f;
                    combo = 0.045f;
                    range = 1.07f;
                    guardBonus = 1;
                    guardTaken = 0.96f;
                    guardCounter = 1.05f;
                    buildup = 0.26f;
                    note = $"Builds {element} resonance quickly without replacing the base hit.";
                    break;
                case RegionalWeaponIdentity.PactSynergy:
                    damage = 0.97f;
                    combo = 0.04f;
                    range = 1.06f;
                    guardBonus = 2;
                    guardTaken = 0.94f;
                    guardCounter = 1.08f;
                    buildup = 0.16f;
                    pactDamage = 1.12f;
                    pactCooldown = 0.88f;
                    note = $"Matches {pactAffinity} Pacts for stronger hits and quicker commands.";
                    break;
                case RegionalWeaponIdentity.Hybrid:
                    damage = 1f;
                    combo = 0.055f;
                    range = 1.09f;
                    guardBonus = 2;
                    guardTaken = 0.92f;
                    guardCounter = 1.1f;
                    buildup = 0.19f;
                    pactDamage = 1.08f;
                    pactCooldown = 0.93f;
                    note = $"Balances guard, reach, and a measured {pactAffinity} Pact response.";
                    break;
            }

            damage += (balancePattern % 3 - 1) * 0.008f;
            range += ((balancePattern + 1) % 3 - 1) * 0.01f;
            buildup += (balancePattern % 2) * 0.01f;
            switch (discipline)
            {
                case WeaponDiscipline.Pathblade:
                    damage += 0.015f;
                    range -= 0.015f;
                    break;
                case WeaponDiscipline.Riftbow:
                    range += 0.035f;
                    guardBonus = Math.Max(0, guardBonus - 1);
                    break;
                case WeaponDiscipline.Threadstaff:
                    damage -= 0.01f;
                    range += 0.015f;
                    guardBonus += 1;
                    guardTaken -= 0.015f;
                    break;
            }

            return new RegionalWeaponModifierDefinition(
                identity,
                damage,
                combo,
                range,
                guardBonus,
                guardTaken,
                guardCounter,
                element,
                buildup,
                hasPactSynergy,
                pactAffinity,
                pactDamage,
                pactCooldown,
                GetBonusPitch(element, discipline),
                note);
        }

        private static int GetProjectedDamageBonus(
            RegionalWeaponIdentity identity,
            WeaponDiscipline discipline,
            int balancePattern)
        {
            var bonus = discipline switch
            {
                WeaponDiscipline.Pathblade => 4,
                WeaponDiscipline.Riftbow => 4,
                _ => 3
            };
            if (identity == RegionalWeaponIdentity.Combo ||
                identity == RegionalWeaponIdentity.Elemental)
            {
                bonus++;
            }

            if (balancePattern == 5 && discipline != WeaponDiscipline.Threadstaff)
            {
                bonus++;
            }

            return Math.Min(6, bonus);
        }

        private static OvertonePitch GetBonusPitch(
            RegionalWeaponElement element,
            WeaponDiscipline discipline)
        {
            switch (element)
            {
                case RegionalWeaponElement.Prism:
                case RegionalWeaponElement.Radiance:
                case RegionalWeaponElement.Storm:
                    return OvertonePitch.Pulse;
                case RegionalWeaponElement.Tide:
                case RegionalWeaponElement.Frost:
                case RegionalWeaponElement.Gloam:
                    return OvertonePitch.Ring;
                case RegionalWeaponElement.Gale:
                    return OvertonePitch.Line;
                case RegionalWeaponElement.Grove:
                case RegionalWeaponElement.Stone:
                    return OvertonePitch.Bind;
                case RegionalWeaponElement.Ember:
                    return OvertonePitch.Cut;
                default:
                    return discipline switch
                    {
                        WeaponDiscipline.Riftbow => OvertonePitch.Line,
                        WeaponDiscipline.Threadstaff => OvertonePitch.Ring,
                        _ => OvertonePitch.Cut
                    };
            }
        }

        private static RegionalWeaponArtProfile CreateArtProfile(
            string id,
            WeaponDiscipline discipline,
            Color primaryColor,
            Color accentColor,
            string silhouetteDirection)
        {
            RegionalWeaponMeshPartDefinition[] parts;
            switch (discipline)
            {
                case WeaponDiscipline.Riftbow:
                    parts = new[]
                    {
                        Part(id, "grip", PrimitiveType.Cylinder, 0.47f, 0.98f, -0.14f, 0.045f, 0.18f, 0.045f, 0f, 0f, 0f, RegionalWeaponArtMaterialRole.Grip),
                        Part(id, "upper_limb", PrimitiveType.Cube, 0.47f, 1.24f, -0.14f, 0.075f, 0.46f, 0.1f, 0f, 0f, -25f, RegionalWeaponArtMaterialRole.Frame),
                        Part(id, "lower_limb", PrimitiveType.Cube, 0.47f, 0.72f, -0.14f, 0.075f, 0.46f, 0.1f, 0f, 0f, 25f, RegionalWeaponArtMaterialRole.Frame),
                        Part(id, "string", PrimitiveType.Cylinder, 0.47f, 0.98f, -0.14f, 0.015f, 0.55f, 0.015f, 0f, 0f, 0f, RegionalWeaponArtMaterialRole.Thread),
                        Part(id, "nock", PrimitiveType.Sphere, 0.47f, 1.01f, -0.25f, 0.085f, 0.085f, 0.085f, 0f, 0f, 0f, RegionalWeaponArtMaterialRole.Focus, true),
                        Part(id, "sight", PrimitiveType.Cube, 0.47f, 1.08f, -0.18f, 0.16f, 0.025f, 0.035f, 0f, 12f, 0f, RegionalWeaponArtMaterialRole.Edge, true)
                    };
                    break;
                case WeaponDiscipline.Threadstaff:
                    parts = new[]
                    {
                        Part(id, "shaft", PrimitiveType.Cylinder, 0.48f, 1.1f, -0.12f, 0.05f, 0.82f, 0.05f, 18f, 0f, 16f, RegionalWeaponArtMaterialRole.Frame),
                        Part(id, "lower_grip", PrimitiveType.Cylinder, 0.61f, 0.58f, -0.02f, 0.075f, 0.2f, 0.075f, 18f, 0f, 16f, RegionalWeaponArtMaterialRole.Grip),
                        Part(id, "focus", PrimitiveType.Sphere, 0.28f, 1.78f, -0.12f, 0.13f, 0.13f, 0.13f, 0f, 0f, 0f, RegionalWeaponArtMaterialRole.Focus, true),
                        Part(id, "outer_ring", PrimitiveType.Cylinder, 0.28f, 1.78f, -0.12f, 0.2f, 0.018f, 0.2f, 90f, 0f, 0f, RegionalWeaponArtMaterialRole.Edge),
                        Part(id, "thread_fork", PrimitiveType.Cube, 0.28f, 1.62f, -0.12f, 0.32f, 0.035f, 0.05f, 0f, 0f, 18f, RegionalWeaponArtMaterialRole.Thread, true)
                    };
                    break;
                default:
                    parts = new[]
                    {
                        Part(id, "grip", PrimitiveType.Cylinder, 0.38f, 0.68f, -0.12f, 0.065f, 0.3f, 0.065f, 28f, 8f, 22f, RegionalWeaponArtMaterialRole.Grip),
                        Part(id, "guard", PrimitiveType.Cube, 0.35f, 0.8f, -0.14f, 0.29f, 0.05f, 0.09f, 0f, 8f, 24f, RegionalWeaponArtMaterialRole.Frame),
                        Part(id, "spine", PrimitiveType.Cylinder, 0.51f, 1.15f, -0.13f, 0.055f, 0.52f, 0.055f, 32f, 8f, 24f, RegionalWeaponArtMaterialRole.Frame),
                        Part(id, "edge", PrimitiveType.Cube, 0.55f, 1.18f, -0.12f, 0.055f, 0.54f, 0.13f, 32f, 8f, 24f, RegionalWeaponArtMaterialRole.Edge),
                        Part(id, "focus", PrimitiveType.Sphere, 0.7f, 1.55f, -0.08f, 0.09f, 0.09f, 0.09f, 0f, 0f, 0f, RegionalWeaponArtMaterialRole.Focus, true)
                    };
                    break;
            }

            return new RegionalWeaponArtProfile(
                id,
                "Wayfinder weapon loadout",
                silhouetteDirection,
                primaryColor,
                accentColor,
                Color.Lerp(accentColor, Color.white, 0.22f),
                parts);
        }

        private static RegionalWeaponMeshPartDefinition Part(
            string profileId,
            string slug,
            PrimitiveType primitive,
            float positionX,
            float positionY,
            float positionZ,
            float scaleX,
            float scaleY,
            float scaleZ,
            float rotationX,
            float rotationY,
            float rotationZ,
            RegionalWeaponArtMaterialRole materialRole,
            bool emissive = false)
        {
            return new RegionalWeaponMeshPartDefinition(
                $"{profileId}.part.{slug}",
                primitive,
                new Vector3(positionX, positionY, positionZ),
                new Vector3(scaleX, scaleY, scaleZ),
                new Vector3(rotationX, rotationY, rotationZ),
                materialRole,
                emissive);
        }

        private static RegionalWeaponVfxProfile CreateVfxProfile(
            string id,
            WeaponDiscipline discipline,
            RegionalWeaponElement element,
            Color primaryColor,
            Color accentColor)
        {
            var releaseCue = discipline switch
            {
                WeaponDiscipline.Riftbow => $"{element} line release",
                WeaponDiscipline.Threadstaff => $"{element} ward ring",
                _ => $"{element} directional ribbon"
            };
            var impactCue = discipline switch
            {
                WeaponDiscipline.Riftbow => $"{element} piercing facet",
                WeaponDiscipline.Threadstaff => $"{element} resonance knot",
                _ => $"{element} cut shard"
            };
            var primitiveCount = discipline == WeaponDiscipline.Threadstaff
                ? 6
                : discipline == WeaponDiscipline.Riftbow
                    ? 4
                    : 5;
            var lifetime = discipline == WeaponDiscipline.Threadstaff
                ? 0.62f
                : discipline == WeaponDiscipline.Riftbow
                    ? 0.44f
                    : 0.38f;
            return new RegionalWeaponVfxProfile(
                id,
                releaseCue,
                impactCue,
                Color.Lerp(primaryColor, accentColor, 0.65f),
                Color.Lerp(accentColor, Color.white, 0.18f),
                primitiveCount,
                discipline == WeaponDiscipline.Threadstaff ? 4 : 3,
                lifetime,
                MaximumVfxDistance,
                true,
                true,
                false);
        }

        private static RegionalWeaponVariantDefinition[] FlattenVariants(
            RegionalWeaponFamilyDefinition[] families)
        {
            var result = new RegionalWeaponVariantDefinition[
                families.Length * VariantsPerFamily];
            var cursor = 0;
            foreach (var family in families)
            {
                foreach (var variant in family.Variants)
                {
                    result[cursor++] = variant;
                }
            }

            if (cursor != result.Length)
            {
                throw new InvalidOperationException(
                    "Every regional weapon family must contain exactly three discipline variants.");
            }

            return result;
        }

        private static Dictionary<string, T> BuildLookup<T>(
            IEnumerable<T> definitions,
            Func<T, string> idSelector,
            string label)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new InvalidOperationException($"{label} catalog contains a null entry.");
                }

                var id = idSelector(definition);
                if (string.IsNullOrEmpty(id) || result.ContainsKey(id))
                {
                    throw new InvalidOperationException(
                        $"{label} catalog contains an invalid or duplicate key '{id}'.");
                }

                result.Add(id, definition);
            }

            return result;
        }

        private static string CreateSlotKey(
            string regionId,
            WeaponDiscipline discipline)
        {
            return $"{regionId ?? string.Empty}|{discipline}";
        }

        private sealed class VariantSeed
        {
            public VariantSeed(
                string slug,
                string displayName,
                string description,
                RegionalWeaponIdentity identity,
                RegionalWeaponAcquisitionKind acquisitionKind,
                string sourceId,
                string sourceDescription)
            {
                Slug = slug;
                DisplayName = displayName;
                Description = description;
                Identity = identity;
                AcquisitionKind = acquisitionKind;
                SourceId = sourceId;
                SourceDescription = sourceDescription;
            }

            public string Slug { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public RegionalWeaponIdentity Identity { get; }
            public RegionalWeaponAcquisitionKind AcquisitionKind { get; }
            public string SourceId { get; }
            public string SourceDescription { get; }
        }
    }
}
