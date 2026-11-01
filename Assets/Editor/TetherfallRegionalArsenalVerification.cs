#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallRegionalArsenalVerification
    {
        private const string MenuPath = "Tetherfall/Verify Regional Arsenal";
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2209)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Regional-arsenal verification is edit-mode only. Stop Play Mode and run it again.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            VerifyRegionCoverage(failures);
            VerifyIdentifiersAndCatalogShape(failures);
            VerifyBalanceAndResolution(failures);
            VerifyUpgradePaths(failures);
            VerifyArtAndVfxBudgets(failures);
            VerifyEquipmentProjection(failures);
            VerifyIndependentSelection(failures);
            VerifyDeterministicLookup(failures);
            VerifyStaticRuntimeContract(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Tetherfall regional arsenal verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            Debug.Log(
                $"Tetherfall regional arsenal verification passed: {assertionCount} assertions; " +
                $"{TetherfallRegionalArsenal.Regions.Count} regions, " +
                $"{TetherfallRegionalArsenal.Families.Count} families, " +
                $"{TetherfallRegionalArsenal.Variants.Count} stable-ID variants; " +
                "three semantic upgrade tiers, independent discipline selection, " +
                "bounded mesh/VFX descriptors, and EquipmentDefinition projection validated.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyRegionCoverage(List<string> failures)
        {
            Check(
                TetherfallRegionalArsenal.Regions.Count ==
                TetherfallRegionalArsenal.ExpectedRegionCount,
                $"Expected {TetherfallRegionalArsenal.ExpectedRegionCount} arsenal regions, found " +
                $"{TetherfallRegionalArsenal.Regions.Count}.",
                failures);
            Check(
                TetherfallRegionalArsenal.Families.Count ==
                TetherfallRegionalArsenal.ExpectedRegionCount,
                "Every covered region must own exactly one curated weapon family.",
                failures);
            Check(
                TetherfallRegionalArsenal.Variants.Count ==
                TetherfallRegionalArsenal.ExpectedRegionCount *
                TetherfallRegionalArsenal.VariantsPerFamily,
                "The arsenal must expose exactly three discipline variants per region.",
                failures);

            var legacyIds = new[]
            {
                TetherfallRegionalArsenal.AlderwakeRegionId,
                TetherfallRegionalArsenal.MorrowfenRegionId,
                TetherfallRegionalArsenal.CinderwoldRegionId
            };
            foreach (var id in legacyIds)
            {
                var region = TetherfallRegionalArsenal.GetRegion(id);
                Check(region != null, $"Legacy playable region '{id}' has no arsenal family.", failures);
                Check(
                    region != null && region.Status == RegionalWeaponRegionStatus.LegacyPlayable,
                    $"Region '{id}' is not classified as legacy playable.",
                    failures);
            }

            Check(
                legacyIds.Length == TetherfallRegionalArsenal.LegacyPlayableRegionCount,
                "Legacy region authority count drifted.",
                failures);
            Check(
                TetherfallRegionPacks.Playable.Count ==
                TetherfallRegionalArsenal.CurrentPlayableRegionCount,
                "Arsenal current-region authority no longer matches TetherfallRegionPacks.Playable.",
                failures);
            foreach (var pack in TetherfallRegionPacks.Playable)
            {
                Check(pack != null, "TetherfallRegionPacks.Playable contains a null pack.", failures);
                if (pack == null)
                {
                    continue;
                }

                var region = TetherfallRegionalArsenal.GetRegion(pack.id);
                Check(
                    region != null,
                    $"Current playable region pack '{pack.id}' is missing from the arsenal.",
                    failures);
                Check(
                    region != null &&
                    region.Status == RegionalWeaponRegionStatus.CurrentPlayable,
                    $"Current playable region '{pack.id}' has the wrong arsenal status.",
                    failures);
                Check(
                    TetherfallRegionalArsenal.GetFamilyForRegion(pack.id) != null,
                    $"Current playable region '{pack.id}' has no weapon family.",
                    failures);
            }

            var authoredIds = new[]
            {
                TetherfallRegionalArsenal.TidewaysRegionId,
                TetherfallRegionalArsenal.PelagicCourtRegionId,
                TetherfallRegionalArsenal.SkylanceRegionId,
                TetherfallRegionalArsenal.SkyKingdomRegionId,
                TetherfallRegionalArsenal.EmbercrownRegionId
            };
            Check(
                authoredIds.Length == TetherfallRegionalArsenal.AuthoredExpansionRegionCount,
                "Authored expansion region authority count drifted.",
                failures);
            foreach (var id in authoredIds)
            {
                var region = TetherfallRegionalArsenal.GetRegion(id);
                Check(
                    region != null,
                    $"Required authored expansion region '{id}' is missing from the arsenal.",
                    failures);
                Check(
                    region != null &&
                    region.Status == RegionalWeaponRegionStatus.AuthoredExpansion,
                    $"Authored expansion region '{id}' has the wrong status.",
                    failures);
                Check(
                    TetherfallRegionalArsenal.GetFamilyForRegion(id) != null,
                    $"Authored expansion region '{id}' has no curated weapon family.",
                    failures);
            }

            Check(
                TetherfallRegionalArsenal.GetRegion(
                    TetherfallRegionalArsenal.EmbercrownRegionId)?.DisplayName.Contains("Embercrown") ==
                true,
                "The volcanic Embercrown region is not named explicitly.",
                failures);
            Check(
                TetherfallRegionalArsenal.GetRegion(
                    TetherfallRegionalArsenal.PelagicCourtRegionId)?.DisplayName.Contains("Pelagic") ==
                true,
                "The underwater Pelagic Court region is not named explicitly.",
                failures);
            Check(
                TetherfallRegionalArsenal.GetRegion(
                    TetherfallRegionalArsenal.SkylanceRegionId)?.DisplayName.Contains("Skylance") ==
                true,
                "The mountain Skylance region is not named explicitly.",
                failures);
            Check(
                TetherfallRegionalArsenal.GetRegion(
                    TetherfallRegionalArsenal.SkyKingdomRegionId)?.DisplayName.Contains("Sky Kingdom") ==
                true,
                "The high-altitude sky kingdom is not named explicitly.",
                failures);
            var aeralithReward = TetherfallRegionalArsenal.GetVariant(
                TetherfallRegionalArsenal.AeralithRegionalRewardWeaponId);
            Check(
                aeralithReward != null &&
                aeralithReward.RegionId == TetherfallRegionalArsenal.SkyKingdomRegionId,
                "Aeralith's world-wonder reward ID does not resolve to its regional family.",
                failures);
            var embercrownReward = TetherfallRegionalArsenal.GetVariant(
                TetherfallRegionalArsenal.EmbercrownRegionalRewardWeaponId);
            Check(
                embercrownReward != null &&
                embercrownReward.RegionId == TetherfallRegionalArsenal.EmbercrownRegionId,
                "Embercrown's world-wonder reward ID does not resolve to its regional family.",
                failures);
        }

        private static void VerifyIdentifiersAndCatalogShape(List<string> failures)
        {
            var globalDefinitionIds = new HashSet<string>(StringComparer.Ordinal);
            var familyRegions = new HashSet<string>(StringComparer.Ordinal);
            var artIds = new HashSet<string>(StringComparer.Ordinal);
            var vfxIds = new HashSet<string>(StringComparer.Ordinal);
            var acquisitionKinds = new HashSet<RegionalWeaponAcquisitionKind>();
            var identities = new HashSet<RegionalWeaponIdentity>();
            var elements = new HashSet<RegionalWeaponElement>();

            foreach (var region in TetherfallRegionalArsenal.Regions)
            {
                Check(region != null, "Regional arsenal contains a null region.", failures);
                if (region == null)
                {
                    continue;
                }

                VerifySemanticId(region.Id, "region.", "region", failures);
                Check(
                    globalDefinitionIds.Add(region.Id),
                    $"Definition ID '{region.Id}' is duplicated.",
                    failures);
                RequireText(region.DisplayName, $"Region '{region.Id}' display name", failures);
                RequireText(region.Lore, $"Region '{region.Id}' lore", failures);
            }

            foreach (var family in TetherfallRegionalArsenal.Families)
            {
                Check(family != null, "Regional arsenal contains a null family.", failures);
                if (family == null)
                {
                    continue;
                }

                VerifySemanticId(family.Id, "weapon_family.", "family", failures);
                Check(
                    globalDefinitionIds.Add(family.Id),
                    $"Definition ID '{family.Id}' is duplicated.",
                    failures);
                Check(
                    familyRegions.Add(family.RegionId),
                    $"Region '{family.RegionId}' owns more than one weapon family.",
                    failures);
                Check(
                    TetherfallRegionalArsenal.GetRegion(family.RegionId) != null,
                    $"Family '{family.Id}' references unknown region '{family.RegionId}'.",
                    failures);
                RequireText(family.DisplayName, $"Family '{family.Id}' display name", failures);
                RequireText(family.Lore, $"Family '{family.Id}' lore", failures);
                Check(
                    family.Variants.Count == TetherfallRegionalArsenal.VariantsPerFamily,
                    $"Family '{family.Id}' contains {family.Variants.Count} variants; expected three.",
                    failures);

                var disciplines = new HashSet<WeaponDiscipline>();
                var familyIdentities = new HashSet<RegionalWeaponIdentity>();
                foreach (var variant in family.Variants)
                {
                    Check(variant != null, $"Family '{family.Id}' contains a null variant.", failures);
                    if (variant == null)
                    {
                        continue;
                    }

                    VerifySemanticId(variant.Id, "weapon.", "variant", failures);
                    Check(
                        globalDefinitionIds.Add(variant.Id),
                        $"Definition ID '{variant.Id}' is duplicated.",
                        failures);
                    Check(
                        string.Equals(variant.FamilyId, family.Id, StringComparison.Ordinal),
                        $"Variant '{variant.Id}' points to family '{variant.FamilyId}', not '{family.Id}'.",
                        failures);
                    Check(
                        string.Equals(variant.RegionId, family.RegionId, StringComparison.Ordinal),
                        $"Variant '{variant.Id}' points to region '{variant.RegionId}', " +
                        $"not '{family.RegionId}'.",
                        failures);
                    Check(
                        disciplines.Add(variant.Discipline),
                        $"Family '{family.Id}' repeats discipline '{variant.Discipline}'.",
                        failures);
                    Check(
                        familyIdentities.Add(variant.Modifier.Identity),
                        $"Family '{family.Id}' repeats primary identity " +
                        $"'{variant.Modifier.Identity}'; each variant needs a distinct role.",
                        failures);
                    RequireText(variant.DisplayName, $"Variant '{variant.Id}' display name", failures);
                    RequireText(variant.Description, $"Variant '{variant.Id}' description", failures);
                    RequireText(variant.Lore, $"Variant '{variant.Id}' lore", failures);

                    Check(variant.Acquisition != null,
                        $"Variant '{variant.Id}' has no acquisition definition.", failures);
                    Check(variant.Modifier != null,
                        $"Variant '{variant.Id}' has no modifier definition.", failures);
                    Check(variant.Art != null,
                        $"Variant '{variant.Id}' has no art attachment profile.", failures);
                    Check(variant.Vfx != null,
                        $"Variant '{variant.Id}' has no VFX profile.", failures);
                    if (variant.Acquisition != null)
                    {
                        VerifySemanticId(
                            variant.Acquisition.SourceId,
                            string.Empty,
                            $"acquisition source for '{variant.Id}'",
                            failures);
                        RequireText(
                            variant.Acquisition.SourceDescription,
                            $"Acquisition source for '{variant.Id}'",
                            failures);
                        acquisitionKinds.Add(variant.Acquisition.Kind);
                    }

                    if (variant.Modifier != null)
                    {
                        identities.Add(variant.Modifier.Identity);
                        elements.Add(variant.Modifier.Element);
                    }

                    if (variant.Art != null)
                    {
                        VerifySemanticId(variant.Art.Id, "weapon_art.", "weapon art", failures);
                        Check(
                            artIds.Add(variant.Art.Id),
                            $"Weapon art ID '{variant.Art.Id}' is duplicated.",
                            failures);
                    }

                    if (variant.Vfx != null)
                    {
                        VerifySemanticId(variant.Vfx.Id, "weapon_vfx.", "weapon VFX", failures);
                        Check(
                            vfxIds.Add(variant.Vfx.Id),
                            $"Weapon VFX ID '{variant.Vfx.Id}' is duplicated.",
                            failures);
                    }
                }

                foreach (WeaponDiscipline discipline in Enum.GetValues(typeof(WeaponDiscipline)))
                {
                    Check(
                        disciplines.Contains(discipline),
                        $"Family '{family.Id}' is missing discipline '{discipline}'.",
                        failures);
                    Check(
                        TetherfallRegionalArsenal.GetVariant(family.RegionId, discipline) != null,
                        $"Region '{family.RegionId}' cannot resolve discipline '{discipline}'.",
                        failures);
                }
            }

            Check(
                familyRegions.Count == TetherfallRegionalArsenal.Regions.Count,
                "Not every arsenal region has exactly one family.",
                failures);
            Check(
                acquisitionKinds.Count == Enum.GetValues(typeof(RegionalWeaponAcquisitionKind)).Length,
                "The curated set does not exercise every supported acquisition kind.",
                failures);
            Check(
                identities.Count == Enum.GetValues(typeof(RegionalWeaponIdentity)).Length,
                "The curated set does not exercise every meaningful variant identity.",
                failures);
            Check(
                elements.Count >= 9,
                $"Regional element diversity fell to {elements.Count}; expected at least nine profiles.",
                failures);
        }

        private static void VerifyBalanceAndResolution(List<string> failures)
        {
            foreach (var variant in TetherfallRegionalArsenal.Variants)
            {
                if (variant?.Modifier == null)
                {
                    continue;
                }

                var modifier = variant.Modifier;
                Check(
                    modifier.DamageMultiplier >= 0.94f &&
                    modifier.DamageMultiplier <= 1.08f,
                    $"Variant '{variant.Id}' damage multiplier " +
                    $"{modifier.DamageMultiplier:0.###} is outside 0.94..1.08.",
                    failures);
                Check(
                    modifier.ComboStepMultiplier >= 0.02f &&
                    modifier.ComboStepMultiplier <= 0.09f,
                    $"Variant '{variant.Id}' combo step " +
                    $"{modifier.ComboStepMultiplier:0.###} is outside 0.02..0.09.",
                    failures);
                Check(
                    modifier.RangeMultiplier >= 0.95f &&
                    modifier.RangeMultiplier <= 1.25f,
                    $"Variant '{variant.Id}' range multiplier " +
                    $"{modifier.RangeMultiplier:0.###} is outside 0.95..1.25.",
                    failures);
                Check(
                    modifier.GuardBonus >= 0 && modifier.GuardBonus <= 5,
                    $"Variant '{variant.Id}' guard bonus {modifier.GuardBonus} is outside 0..5.",
                    failures);
                Check(
                    modifier.GuardDamageTakenMultiplier >= 0.83f &&
                    modifier.GuardDamageTakenMultiplier <= 1f,
                    $"Variant '{variant.Id}' guarded-damage multiplier " +
                    $"{modifier.GuardDamageTakenMultiplier:0.###} is outside 0.83..1.",
                    failures);
                Check(
                    modifier.GuardCounterDamageMultiplier >= 1f &&
                    modifier.GuardCounterDamageMultiplier <= 1.16f,
                    $"Variant '{variant.Id}' counter multiplier " +
                    $"{modifier.GuardCounterDamageMultiplier:0.###} is outside 1..1.16.",
                    failures);
                Check(
                    modifier.ElementalBuildup >= 0.09f &&
                    modifier.ElementalBuildup <= 0.28f,
                    $"Variant '{variant.Id}' elemental buildup " +
                    $"{modifier.ElementalBuildup:0.###} is outside 0.09..0.28.",
                    failures);
                Check(
                    modifier.PactMatchDamageMultiplier >= 1.05f &&
                    modifier.PactMatchDamageMultiplier <= 1.12f,
                    $"Variant '{variant.Id}' Pact multiplier " +
                    $"{modifier.PactMatchDamageMultiplier:0.###} is outside 1.05..1.12.",
                    failures);
                Check(
                    modifier.PactCommandCooldownMultiplier >= 0.88f &&
                    modifier.PactCommandCooldownMultiplier <= 0.97f,
                    $"Variant '{variant.Id}' Pact command cooldown multiplier " +
                    $"{modifier.PactCommandCooldownMultiplier:0.###} is outside 0.88..0.97.",
                    failures);
                Check(
                    variant.ProjectedDamageBonus >= 3 &&
                    variant.ProjectedDamageBonus <= 6,
                    $"Variant '{variant.Id}' projected flat bonus " +
                    $"{variant.ProjectedDamageBonus} is outside 3..6.",
                    failures);
                RequireText(
                    modifier.GameplayNote,
                    $"Variant '{variant.Id}' modifier note",
                    failures);

                Check(
                    variant.TryGetUpgradeTier(
                        TetherfallRegionalArsenal.BaseUpgradeTierId,
                        out var baseTier),
                    $"Variant '{variant.Id}' cannot resolve the base tier by semantic ID.",
                    failures);
                Check(
                    variant.TryGetUpgradeTier(
                        TetherfallRegionalArsenal.MasterworkUpgradeTierId,
                        out var masterworkTier),
                    $"Variant '{variant.Id}' cannot resolve the masterwork tier by semantic ID.",
                    failures);
                if (baseTier == null || masterworkTier == null)
                {
                    continue;
                }

                var neutralInput = new RegionalWeaponResolutionInput(
                    100f,
                    10f,
                    1f,
                    0f,
                    0,
                    false,
                    false,
                    modifier.PactAffinity);
                var pressureInput = new RegionalWeaponResolutionInput(
                    100f,
                    10f,
                    1f,
                    0f,
                    2,
                    true,
                    true,
                    modifier.PactAffinity);
                var neutral = RegionalWeaponResolution.Resolve(variant, baseTier, neutralInput);
                var pressure = RegionalWeaponResolution.Resolve(
                    variant,
                    masterworkTier,
                    pressureInput);
                var pressureAgain = RegionalWeaponResolution.Resolve(
                    variant,
                    masterworkTier,
                    pressureInput);
                Check(
                    neutral.Damage >= 94f && neutral.Damage <= 108f,
                    $"Variant '{variant.Id}' neutral damage {neutral.Damage:0.###} escaped bounds.",
                    failures);
                Check(
                    pressure.Damage >= neutral.Damage &&
                    pressure.Damage <= 170f,
                    $"Variant '{variant.Id}' full-pressure damage " +
                    $"{pressure.Damage:0.###} escaped bounded progression.",
                    failures);
                Check(
                    pressure.EffectiveRange >= 9.5f &&
                    pressure.EffectiveRange <= 13.6f,
                    $"Variant '{variant.Id}' resolved range " +
                    $"{pressure.EffectiveRange:0.###} escaped bounds.",
                    failures);
                Check(
                    pressure.GuardBonus >= modifier.GuardBonus &&
                    pressure.GuardBonus <= 7,
                    $"Variant '{variant.Id}' resolved guard bonus " +
                    $"{pressure.GuardBonus} escaped bounds.",
                    failures);
                Check(
                    pressure.GuardDamageTakenMultiplier >= 0.79f &&
                    pressure.GuardDamageTakenMultiplier <= 0.97f,
                    $"Variant '{variant.Id}' resolved guarded damage " +
                    $"{pressure.GuardDamageTakenMultiplier:0.###} escaped bounds.",
                    failures);
                Check(
                    pressure.ElementalBuildup >= 0.17f &&
                    pressure.ElementalBuildup <= 0.36f,
                    $"Variant '{variant.Id}' resolved elemental buildup " +
                    $"{pressure.ElementalBuildup:0.###} escaped bounds.",
                    failures);
                Check(
                    pressure.Damage == pressureAgain.Damage &&
                    pressure.EffectiveRange == pressureAgain.EffectiveRange &&
                    pressure.GuardBonus == pressureAgain.GuardBonus &&
                    pressure.PactSynergyActive == pressureAgain.PactSynergyActive,
                    $"Variant '{variant.Id}' resolution is not deterministic.",
                    failures);
                Check(
                    pressure.PactSynergyActive == modifier.HasPactSynergy,
                    $"Variant '{variant.Id}' did not resolve Pact synergy according to its profile.",
                    failures);
                Check(
                    pressure.PactCommandCooldownMultiplier >= 0.88f &&
                    pressure.PactCommandCooldownMultiplier <= 1f,
                    $"Variant '{variant.Id}' resolved Pact cooldown escaped 0.88..1.",
                    failures);

                var unknownTierResolved = RegionalWeaponResolution.TryResolve(
                    variant.Id,
                    "weapon_tier.unknown",
                    neutralInput,
                    out _);
                Check(
                    !unknownTierResolved,
                    $"Variant '{variant.Id}' silently accepted an unknown upgrade tier.",
                    failures);
            }
        }

        private static void VerifyUpgradePaths(List<string> failures)
        {
            foreach (var variant in TetherfallRegionalArsenal.Variants)
            {
                if (variant == null)
                {
                    continue;
                }

                Check(
                    variant.UpgradeTiers.Count == 3,
                    $"Variant '{variant.Id}' has {variant.UpgradeTiers.Count} tiers; expected three.",
                    failures);
                var tierIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var tier in variant.UpgradeTiers)
                {
                    Check(tier != null, $"Variant '{variant.Id}' has a null upgrade tier.", failures);
                    if (tier == null)
                    {
                        continue;
                    }

                    VerifySemanticId(tier.Id, "weapon_tier.", "upgrade tier", failures);
                    Check(
                        tierIds.Add(tier.Id),
                        $"Variant '{variant.Id}' repeats tier ID '{tier.Id}'.",
                        failures);
                    Check(
                        tier.DamageMultiplier >= 1f && tier.DamageMultiplier <= 1.12f,
                        $"Variant '{variant.Id}' tier '{tier.Id}' damage multiplier is unbalanced.",
                        failures);
                    Check(
                        tier.RangeMultiplier >= 1f && tier.RangeMultiplier <= 1.04f,
                        $"Variant '{variant.Id}' tier '{tier.Id}' range multiplier is unbalanced.",
                        failures);
                    Check(
                        tier.GuardBonus >= 0 && tier.GuardBonus <= 2,
                        $"Variant '{variant.Id}' tier '{tier.Id}' guard bonus is unbalanced.",
                        failures);
                    Check(
                        tier.GuardDamageTakenMultiplier >= 0.96f &&
                        tier.GuardDamageTakenMultiplier <= 1f,
                        $"Variant '{variant.Id}' tier '{tier.Id}' guarded-damage modifier is unbalanced.",
                        failures);

                    var resources = new HashSet<ResourceKind>();
                    foreach (var cost in tier.Costs)
                    {
                        Check(
                            cost != null,
                            $"Variant '{variant.Id}' tier '{tier.Id}' has a null ingredient.",
                            failures);
                        if (cost == null)
                        {
                            continue;
                        }

                        Check(
                            cost.Amount > 0,
                            $"Variant '{variant.Id}' tier '{tier.Id}' has a non-positive ingredient.",
                            failures);
                        Check(
                            resources.Add(cost.Kind),
                            $"Variant '{variant.Id}' tier '{tier.Id}' repeats resource '{cost.Kind}'.",
                            failures);
                    }
                }

                Check(
                    tierIds.SetEquals(new[]
                    {
                        TetherfallRegionalArsenal.BaseUpgradeTierId,
                        TetherfallRegionalArsenal.ResonantUpgradeTierId,
                        TetherfallRegionalArsenal.MasterworkUpgradeTierId
                    }),
                    $"Variant '{variant.Id}' tier IDs drifted from the semantic save contract.",
                    failures);
                variant.TryGetUpgradeTier(
                    TetherfallRegionalArsenal.BaseUpgradeTierId,
                    out var baseTier);
                variant.TryGetUpgradeTier(
                    TetherfallRegionalArsenal.ResonantUpgradeTierId,
                    out var resonantTier);
                variant.TryGetUpgradeTier(
                    TetherfallRegionalArsenal.MasterworkUpgradeTierId,
                    out var masterworkTier);
                if (baseTier == null || resonantTier == null || masterworkTier == null)
                {
                    continue;
                }

                Check(
                    string.IsNullOrEmpty(baseTier.PreviousTierId) &&
                    baseTier.Costs.Count == 0,
                    $"Variant '{variant.Id}' base tier must be free and have no predecessor.",
                    failures);
                Check(
                    string.Equals(
                        resonantTier.PreviousTierId,
                        TetherfallRegionalArsenal.BaseUpgradeTierId,
                        StringComparison.Ordinal),
                    $"Variant '{variant.Id}' resonant tier does not reference base by ID.",
                    failures);
                Check(
                    string.Equals(
                        masterworkTier.PreviousTierId,
                        TetherfallRegionalArsenal.ResonantUpgradeTierId,
                        StringComparison.Ordinal),
                    $"Variant '{variant.Id}' masterwork tier does not reference resonant by ID.",
                    failures);
                Check(
                    resonantTier.Costs.Count == 3 && masterworkTier.Costs.Count == 3,
                    $"Variant '{variant.Id}' paid tiers need three regional ingredients.",
                    failures);
                Check(
                    SumCosts(masterworkTier.Costs) > SumCosts(resonantTier.Costs),
                    $"Variant '{variant.Id}' masterwork cost must exceed its resonant cost.",
                    failures);
            }
        }

        private static void VerifyArtAndVfxBudgets(List<string> failures)
        {
            foreach (var variant in TetherfallRegionalArsenal.Variants)
            {
                if (variant?.Art == null || variant.Vfx == null)
                {
                    continue;
                }

                var art = variant.Art;
                Check(
                    string.Equals(
                        art.AttachmentSocket,
                        "Wayfinder weapon loadout",
                        StringComparison.Ordinal),
                    $"Variant '{variant.Id}' targets unsupported attachment socket " +
                    $"'{art.AttachmentSocket}'.",
                    failures);
                RequireText(
                    art.SilhouetteDirection,
                    $"Variant '{variant.Id}' silhouette direction",
                    failures);
                Check(
                    art.Parts.Count >= 5 &&
                    art.Parts.Count <= TetherfallRegionalArsenal.MaximumArtRenderersPerWeapon,
                    $"Variant '{variant.Id}' uses {art.Parts.Count} art parts; budget is 5.." +
                    $"{TetherfallRegionalArsenal.MaximumArtRenderersPerWeapon}.",
                    failures);
                Check(IsFiniteColor(art.PrimaryColor) &&
                      IsFiniteColor(art.AccentColor) &&
                      IsFiniteColor(art.EmissionColor),
                    $"Variant '{variant.Id}' art palette is invalid.", failures);

                var partIds = new HashSet<string>(StringComparer.Ordinal);
                var emissiveCount = 0;
                foreach (var part in art.Parts)
                {
                    Check(part != null, $"Variant '{variant.Id}' has a null art part.", failures);
                    if (part == null)
                    {
                        continue;
                    }

                    VerifySemanticId(part.Id, art.Id + ".part.", "weapon art part", failures);
                    Check(
                        partIds.Add(part.Id),
                        $"Variant '{variant.Id}' repeats art part ID '{part.Id}'.",
                        failures);
                    Check(
                        IsFinite(part.LocalPosition) &&
                        IsFinite(part.LocalScale) &&
                        IsFinite(part.LocalEulerAngles),
                        $"Variant '{variant.Id}' art part '{part.Id}' has nonfinite transforms.",
                        failures);
                    Check(
                        part.LocalScale.x > 0f &&
                        part.LocalScale.y > 0f &&
                        part.LocalScale.z > 0f,
                        $"Variant '{variant.Id}' art part '{part.Id}' has non-positive scale.",
                        failures);
                    Check(
                        part.LocalPosition.sqrMagnitude <= 6f,
                        $"Variant '{variant.Id}' art part '{part.Id}' is implausibly far from its socket.",
                        failures);
                    if (part.Emissive)
                    {
                        emissiveCount++;
                    }
                }

                Check(
                    emissiveCount <= TetherfallRegionalArsenal.MaximumEmissivePartsPerWeapon,
                    $"Variant '{variant.Id}' has {emissiveCount} emissive parts; budget is " +
                    $"{TetherfallRegionalArsenal.MaximumEmissivePartsPerWeapon}.",
                    failures);

                var vfx = variant.Vfx;
                RequireText(vfx.ReleaseCue, $"Variant '{variant.Id}' release cue", failures);
                RequireText(vfx.ImpactCue, $"Variant '{variant.Id}' impact cue", failures);
                Check(
                    vfx.MaximumActivePrimitives > 0 &&
                    vfx.MaximumActivePrimitives <=
                    TetherfallRegionalArsenal.MaximumVfxPrimitivesPerCue,
                    $"Variant '{variant.Id}' VFX primitive budget is " +
                    $"{vfx.MaximumActivePrimitives}.",
                    failures);
                Check(
                    vfx.MaximumImpactShards > 0 &&
                    vfx.MaximumImpactShards <=
                    TetherfallRegionalArsenal.MaximumVfxImpactShards,
                    $"Variant '{variant.Id}' impact shard budget is " +
                    $"{vfx.MaximumImpactShards}.",
                    failures);
                Check(
                    vfx.MaximumLifetimeSeconds > 0f &&
                    vfx.MaximumLifetimeSeconds <=
                    TetherfallRegionalArsenal.MaximumVfxLifetimeSeconds,
                    $"Variant '{variant.Id}' VFX lifetime is " +
                    $"{vfx.MaximumLifetimeSeconds:0.###} seconds.",
                    failures);
                Check(
                    vfx.MaximumDistance > 0f &&
                    vfx.MaximumDistance <= TetherfallRegionalArsenal.MaximumVfxDistance,
                    $"Variant '{variant.Id}' VFX distance is {vfx.MaximumDistance:0.###}.",
                    failures);
                Check(
                    vfx.RequiresPooling && vfx.ColliderFree && !vfx.UsesDynamicLights,
                    $"Variant '{variant.Id}' VFX must be pooled, collider-free, and light-free.",
                    failures);
                Check(
                    IsFiniteColor(vfx.TrailColor) && IsFiniteColor(vfx.ImpactColor),
                    $"Variant '{variant.Id}' VFX palette is invalid.",
                    failures);
            }
        }

        private static void VerifyEquipmentProjection(List<string> failures)
        {
            var projection = TetherfallRegionalArsenal.CreateEquipmentProjection();
            Check(
                projection.Count == TetherfallRegionalArsenal.Variants.Count,
                "Equipment projection is not one-to-one with regional variants.",
                failures);
            var projectedIds = new HashSet<string>(StringComparer.Ordinal);
            var existingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var existing in EquipmentCatalog.CreateAll())
            {
                if (existing != null)
                {
                    existingIds.Add(existing.id);
                }
            }

            for (var index = 0; index < projection.Count; index++)
            {
                var equipment = projection[index];
                var variant = TetherfallRegionalArsenal.Variants[index];
                Check(equipment != null, $"Projection index {index} is null.", failures);
                if (equipment == null || variant == null)
                {
                    continue;
                }

                Check(
                    projectedIds.Add(equipment.id),
                    $"Projected equipment ID '{equipment.id}' is duplicated.",
                    failures);
                Check(
                    !existingIds.Contains(equipment.id),
                    $"Projected equipment ID '{equipment.id}' collides with EquipmentCatalog.",
                    failures);
                Check(
                    string.Equals(equipment.id, variant.Id, StringComparison.Ordinal),
                    $"Projection '{equipment.id}' does not preserve variant ID '{variant.Id}'.",
                    failures);
                Check(
                    string.Equals(equipment.displayName, variant.DisplayName, StringComparison.Ordinal),
                    $"Projection '{equipment.id}' does not preserve its display name.",
                    failures);
                Check(
                    equipment.slot == EquipmentSlot.Weapon,
                    $"Projection '{equipment.id}' is not a weapon.",
                    failures);
                Check(
                    equipment.discipline == variant.Discipline,
                    $"Projection '{equipment.id}' changed discipline.",
                    failures);
                Check(
                    equipment.damageBonus == variant.ProjectedDamageBonus,
                    $"Projection '{equipment.id}' changed its flat damage bonus.",
                    failures);
                RequireText(equipment.description, $"Projection '{equipment.id}' description", failures);

                switch (variant.Acquisition.Kind)
                {
                    case RegionalWeaponAcquisitionKind.Crafting:
                        Check(
                            equipment.favorPrice == 0 &&
                            !equipment.restorationRewardOnly &&
                            equipment.craftCost.Count == variant.Acquisition.Costs.Count,
                            $"Craft projection '{equipment.id}' has incorrect store flags or costs.",
                            failures);
                        VerifyProjectedCosts(variant, equipment, failures);
                        break;
                    case RegionalWeaponAcquisitionKind.Vendor:
                        Check(
                            equipment.favorPrice == variant.Acquisition.FavorPrice &&
                            equipment.favorPrice > 0 &&
                            !equipment.restorationRewardOnly &&
                            equipment.craftCost.Count == 0,
                            $"Vendor projection '{equipment.id}' has incorrect price or flags.",
                            failures);
                        break;
                    default:
                        Check(
                            equipment.favorPrice == 0 &&
                            equipment.restorationRewardOnly &&
                            equipment.craftCost.Count == 0,
                            $"Reward projection '{equipment.id}' could leak into crafting or vendors.",
                            failures);
                        break;
                }
            }

            var secondProjection = TetherfallRegionalArsenal.CreateEquipmentProjection();
            Check(
                secondProjection.Count == projection.Count,
                "Repeated projection changed cardinality.",
                failures);
            if (projection.Count > 0 && secondProjection.Count > 0)
            {
                var originalVariantId = TetherfallRegionalArsenal.Variants[0].Id;
                var originalProjectedId = secondProjection[0].id;
                Check(
                    !ReferenceEquals(projection[0], secondProjection[0]),
                    "Equipment projection reuses mutable EquipmentDefinition instances.",
                    failures);
                projection[0].id = "weapon.mutation_probe";
                Check(
                    string.Equals(secondProjection[0].id, originalProjectedId, StringComparison.Ordinal) &&
                    string.Equals(
                        TetherfallRegionalArsenal.Variants[0].Id,
                        originalVariantId,
                        StringComparison.Ordinal),
                    "Mutating one EquipmentDefinition projection changed catalog authority.",
                    failures);
                if (projection[0].craftCost.Count > 0 &&
                    secondProjection[0].craftCost.Count > 0)
                {
                    var originalAmount = secondProjection[0].craftCost[0].amount;
                    projection[0].craftCost[0].amount = 999;
                    Check(
                        secondProjection[0].craftCost[0].amount == originalAmount,
                        "Projected ResourceCost instances are shared across projections.",
                        failures);
                }
            }

            var appendTarget = EquipmentCatalog.CreateAll();
            var existingCount = appendTarget.Count;
            var appended = TetherfallRegionalArsenal.AppendEquipmentProjection(appendTarget);
            Check(
                appended == TetherfallRegionalArsenal.Variants.Count &&
                appendTarget.Count == existingCount + appended,
                "AppendEquipmentProjection did not append the complete projection.",
                failures);
            var duplicateRejected = false;
            try
            {
                TetherfallRegionalArsenal.AppendEquipmentProjection(appendTarget);
            }
            catch (InvalidOperationException)
            {
                duplicateRejected = true;
            }

            Check(
                duplicateRejected,
                "AppendEquipmentProjection did not reject duplicate semantic IDs.",
                failures);
        }

        private static void VerifyIndependentSelection(List<string> failures)
        {
            var alderwake = TetherfallRegionalArsenal.GetFamilyForRegion(
                TetherfallRegionalArsenal.AlderwakeRegionId);
            var tideglass = TetherfallRegionalArsenal.GetFamilyForRegion(
                TetherfallRegionalArsenal.TideglassRegionId);
            Check(alderwake != null && tideglass != null,
                "Selection verification families are missing.", failures);
            if (alderwake == null || tideglass == null)
            {
                return;
            }

            var pathblade = TetherfallRegionalArsenal.GetVariant(
                alderwake.RegionId,
                WeaponDiscipline.Pathblade);
            var riftbow = TetherfallRegionalArsenal.GetVariant(
                alderwake.RegionId,
                WeaponDiscipline.Riftbow);
            var threadstaff = TetherfallRegionalArsenal.GetVariant(
                alderwake.RegionId,
                WeaponDiscipline.Threadstaff);
            var alternateRiftbow = TetherfallRegionalArsenal.GetVariant(
                tideglass.RegionId,
                WeaponDiscipline.Riftbow);
            Check(
                pathblade != null && riftbow != null &&
                threadstaff != null && alternateRiftbow != null,
                "Selection verification variants are missing.",
                failures);
            if (pathblade == null || riftbow == null ||
                threadstaff == null || alternateRiftbow == null)
            {
                return;
            }

            var selections = new List<RegionalWeaponSelectionRecord>
            {
                Selection(WeaponDiscipline.Threadstaff, threadstaff.Id),
                Selection(WeaponDiscipline.Pathblade, pathblade.Id),
                Selection(WeaponDiscipline.Riftbow, riftbow.Id)
            };
            var input = new RegionalWeaponResolutionInput(
                100f,
                10f,
                1f,
                0f,
                1,
                false,
                false,
                PactResonanceAffinity.Grove);
            Check(
                RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Pathblade,
                    input,
                    out var pathResult) &&
                pathResult.VariantId == pathblade.Id,
                "Pathblade selection depended on list order.",
                failures);
            Check(
                RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Riftbow,
                    input,
                    out var bowResult) &&
                bowResult.VariantId == riftbow.Id,
                "Riftbow selection depended on list order.",
                failures);
            Check(
                RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Threadstaff,
                    input,
                    out var staffResult) &&
                staffResult.VariantId == threadstaff.Id,
                "Threadstaff selection depended on list order.",
                failures);

            selections[2].variantId = alternateRiftbow.Id;
            Check(
                RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Riftbow,
                    input,
                    out var alternateBowResult) &&
                alternateBowResult.VariantId == alternateRiftbow.Id,
                "One discipline cannot select a regional variant independently.",
                failures);
            Check(
                RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Pathblade,
                    input,
                    out var unchangedPathResult) &&
                unchangedPathResult.VariantId == pathblade.Id,
                "Changing the Riftbow selection changed the Pathblade selection.",
                failures);
            Check(
                RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Threadstaff,
                    input,
                    out var unchangedStaffResult) &&
                unchangedStaffResult.VariantId == threadstaff.Id,
                "Changing the Riftbow selection changed the Threadstaff selection.",
                failures);

            selections.Add(Selection(WeaponDiscipline.Riftbow, riftbow.Id));
            Check(
                !RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Riftbow,
                    input,
                    out _),
                "Duplicate discipline selection was resolved by incidental list order.",
                failures);
            selections.RemoveAt(selections.Count - 1);
            selections[2].variantId = pathblade.Id;
            Check(
                !RegionalWeaponResolution.TryResolveSelection(
                    selections,
                    WeaponDiscipline.Riftbow,
                    input,
                    out _),
                "A selection accepted a variant from the wrong discipline.",
                failures);

            foreach (WeaponDiscipline discipline in Enum.GetValues(typeof(WeaponDiscipline)))
            {
                var starterId = TetherfallRegionalArsenal.GetExplicitStarterVariantId(discipline);
                var starter = TetherfallRegionalArsenal.GetVariant(starterId);
                Check(
                    starter != null && starter.Discipline == discipline,
                    $"Explicit starter mapping for '{discipline}' is invalid.",
                    failures);
            }
        }

        private static void VerifyDeterministicLookup(List<string> failures)
        {
            foreach (var region in TetherfallRegionalArsenal.Regions)
            {
                var firstRegion = TetherfallRegionalArsenal.GetRegion(region.Id);
                var secondRegion = TetherfallRegionalArsenal.GetRegion(region.Id);
                Check(
                    ReferenceEquals(firstRegion, secondRegion),
                    $"Region lookup '{region.Id}' is not deterministic.",
                    failures);

                var family = TetherfallRegionalArsenal.GetFamilyForRegion(region.Id);
                Check(
                    family != null &&
                    ReferenceEquals(family, TetherfallRegionalArsenal.GetFamily(family.Id)),
                    $"Family lookup for region '{region.Id}' is not deterministic.",
                    failures);
                if (family == null)
                {
                    continue;
                }

                foreach (WeaponDiscipline discipline in Enum.GetValues(typeof(WeaponDiscipline)))
                {
                    var bySlot = TetherfallRegionalArsenal.GetVariant(region.Id, discipline);
                    Check(
                        bySlot != null &&
                        ReferenceEquals(
                            bySlot,
                            TetherfallRegionalArsenal.GetVariant(bySlot.Id)),
                        $"Variant lookup for '{region.Id}'/'{discipline}' is not deterministic.",
                        failures);
                }
            }

            Check(TetherfallRegionalArsenal.GetRegion(null) == null,
                "Null region lookup should return null.", failures);
            Check(TetherfallRegionalArsenal.GetFamily(null) == null,
                "Null family lookup should return null.", failures);
            Check(TetherfallRegionalArsenal.GetVariant(null) == null,
                "Null variant lookup should return null.", failures);
            Check(TetherfallRegionalArsenal.GetFamilyForRegion("region.unknown") == null,
                "Unknown region unexpectedly resolved a family.", failures);

            var firstProjection = TetherfallRegionalArsenal.CreateEquipmentProjection();
            var secondProjection = TetherfallRegionalArsenal.CreateEquipmentProjection();
            Check(
                firstProjection.Count == secondProjection.Count,
                "Repeated equipment projection changed count.",
                failures);
            var count = Math.Min(firstProjection.Count, secondProjection.Count);
            for (var index = 0; index < count; index++)
            {
                Check(
                    string.Equals(
                        firstProjection[index].id,
                        secondProjection[index].id,
                        StringComparison.Ordinal),
                    $"Equipment projection order changed at index {index}.",
                    failures);
            }
        }

        private static void VerifyStaticRuntimeContract(List<string> failures)
        {
            var runtimePath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "TetherfallRegionalArsenal.cs");
            Check(File.Exists(runtimePath),
                $"Regional arsenal runtime source was not found: {runtimePath}", failures);
            if (!File.Exists(runtimePath))
            {
                return;
            }

            var source = File.ReadAllText(runtimePath).Replace("\r\n", "\n");
            Check(!source.Contains("using UnityEditor"),
                "Regional arsenal runtime source depends on UnityEditor.", failures);
            Check(!source.Contains("RuntimeInitializeOnLoadMethod"),
                "Regional arsenal must not auto-install itself at runtime.", failures);
            Check(!source.Contains("InitializeOnLoad"),
                "Regional arsenal must not auto-install itself in the editor.", failures);
            Check(!source.Contains("new GameObject") &&
                  !source.Contains("AddComponent<") &&
                  !source.Contains("Resources.Load"),
                "Regional arsenal foundation must remain data-only and side-effect free.", failures);
            Check(source.Contains("CreateEquipmentProjection()") &&
                  source.Contains("AppendEquipmentProjection(") &&
                  source.Contains("ProjectToEquipmentDefinition("),
                "Regional arsenal does not expose the appendable EquipmentDefinition projection.",
                failures);
            Check(source.Contains("TryResolveSelection(") &&
                  source.Contains("candidate.discipline != discipline") &&
                  source.Contains("if (selected != null)"),
                "Selection resolution is not keyed explicitly by discipline.",
                failures);
            Check(!source.Contains(".First(") &&
                  !source.Contains(".FirstOrDefault(") &&
                  !source.Contains("selectedVariants[0]") &&
                  !source.Contains("UpgradeTiers["),
                "Runtime source contains order-fragile selection or upgrade-tier indexing.",
                failures);
            Check(source.Contains("BaseUpgradeTierId") &&
                  source.Contains("ResonantUpgradeTierId") &&
                  source.Contains("MasterworkUpgradeTierId"),
                "Runtime source does not retain semantic upgrade-tier IDs.",
                failures);
        }

        private static RegionalWeaponSelectionRecord Selection(
            WeaponDiscipline discipline,
            string variantId)
        {
            return new RegionalWeaponSelectionRecord
            {
                discipline = discipline,
                variantId = variantId,
                upgradeTierId = TetherfallRegionalArsenal.BaseUpgradeTierId
            };
        }

        private static void VerifyProjectedCosts(
            RegionalWeaponVariantDefinition variant,
            EquipmentDefinition equipment,
            List<string> failures)
        {
            if (variant.Acquisition.Costs.Count != equipment.craftCost.Count)
            {
                return;
            }

            for (var index = 0; index < variant.Acquisition.Costs.Count; index++)
            {
                var source = variant.Acquisition.Costs[index];
                var projected = equipment.craftCost[index];
                Check(
                    projected != null &&
                    projected.kind == source.Kind &&
                    projected.amount == source.Amount,
                    $"Projection '{equipment.id}' changed crafting ingredient {index}.",
                    failures);
            }
        }

        private static int SumCosts(IReadOnlyList<RegionalWeaponIngredient> costs)
        {
            var result = 0;
            foreach (var cost in costs)
            {
                if (cost != null)
                {
                    result += cost.Amount;
                }
            }

            return result;
        }

        private static void VerifySemanticId(
            string id,
            string requiredPrefix,
            string label,
            List<string> failures)
        {
            Check(
                IsSemanticId(id, requiredPrefix),
                $"{label} ID '{id}' is not a stable semantic ID with prefix '{requiredPrefix}'.",
                failures);
            Check(
                !IsCoordinateDerivedId(id),
                $"{label} ID '{id}' appears coordinate-derived.",
                failures);
        }

        private static bool IsSemanticId(string id, string requiredPrefix)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                (!string.IsNullOrEmpty(requiredPrefix) &&
                 !id.StartsWith(requiredPrefix, StringComparison.Ordinal)) ||
                id.StartsWith(".", StringComparison.Ordinal) ||
                id.EndsWith(".", StringComparison.Ordinal) ||
                id.Contains(".."))
            {
                return false;
            }

            foreach (var character in id)
            {
                if ((character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '.' ||
                    character == '_')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsCoordinateDerivedId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            return id.Contains("@") ||
                   Regex.IsMatch(
                       id,
                       @"(?:^|[._])[-]?\d+(?:[:;,]|__)[-]?\d+(?:$|[._])",
                       RegexOptions.CultureInvariant);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFiniteColor(Color value)
        {
            return IsFinite(value.r) &&
                   IsFinite(value.g) &&
                   IsFinite(value.b) &&
                   IsFinite(value.a) &&
                   value.r >= 0f && value.r <= 1f &&
                   value.g >= 0f && value.g <= 1f &&
                   value.b >= 0f && value.b <= 1f &&
                   value.a >= 0f && value.a <= 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void RequireText(
            string value,
            string label,
            List<string> failures)
        {
            Check(!string.IsNullOrWhiteSpace(value), $"{label} is missing.", failures);
        }

        private static void Check(
            bool condition,
            string message,
            List<string> failures)
        {
            assertionCount++;
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
#endif
