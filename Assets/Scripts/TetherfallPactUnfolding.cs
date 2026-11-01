using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    [Serializable]
    public sealed class PactProgressSaveData
    {
        public string pactId;
        public int bondXp;
        public int bondRank = 1;
        public string unfoldedFormId;
        public List<string> awardedSourceIds = new List<string>();
    }

    public sealed class PactFormDefinition
    {
        public string id;
        public string displayName;
        public string tacticalDescription;
        public string passiveDescription;
        public string traversalDescription;
        public string tacticalLabel;
        public int requiredBondRank;
        public float visualScale = 1f;
        public float assistPotencyMultiplier = 1f;
        public float tacticalPotencyMultiplier = 1f;
        public float tacticalRangeMultiplier = 1f;
        public float tacticalStaggerMultiplier = 1f;
        public int wardReduction;
        public float climbStaminaCostMultiplier = 1f;
        public float glideStaminaCostMultiplier = 1f;
        public float swimSpeedBonus;
        public float staminaRegenerationBonus;
        public float mountedSpeedMultiplier = 1f;
        public float mountedDamageMultiplier = 1f;
        public float mountedJumpVelocityBonus;
        public int mountedWardReduction;
        public bool grantsBoundedThermalLift;
        public float boundedThermalLiftVelocity;
    }

    public sealed class PactDefinition
    {
        public string id;
        public string creatureName;
        public string bondTheme;
        public bool rideReady;
        public int[] bondRankThresholds = Array.Empty<int>();
        public List<PactFormDefinition> greaterForms = new List<PactFormDefinition>();
    }

    public static class TetherfallPactCatalog
    {
        public const string BrambletId = "pact.bramblet";
        public const string SlatebackId = "pact.slateback";
        public const string MistlingId = "pact.mistling";
        public const string CinderwingId = "pact.cinderwing";

        public const string GrovewardenFormId = "pact.bramblet.form.grovewarden";
        public const string RidgebearerFormId = "pact.slateback.form.ridgebearer";
        public const string RainwardenFormId = "pact.mistling.form.rainwarden";
        public const string SuncrestFormId = "pact.cinderwing.form.suncrest";

        private static readonly List<PactDefinition> Definitions = new List<PactDefinition>
        {
            Pact(
                BrambletId,
                "Bramblet",
                "Patient fieldwork and sheltering roots",
                false,
                Form(
                    GrovewardenFormId,
                    "Grovewarden",
                    "Deep Snare spreads farther and holds hostile footing longer.",
                    "Rootguard reduces incoming harm while Bramblet is the active Pact.",
                    "Living handholds reduce climbing stamina use.",
                    "Deep Snare",
                    visualScale: 1.12f,
                    assistPotency: 1.08f,
                    tacticalPotency: 1.24f,
                    tacticalRange: 1.18f,
                    tacticalStagger: 1.28f,
                    wardReduction: 1,
                    climbCost: 0.72f)),
            Pact(
                SlatebackId,
                "Slateback",
                "Trust carried over difficult ground",
                true,
                Form(
                    RidgebearerFormId,
                    "Ridgebearer",
                    "Ridge Charge lands with greater force from the saddle.",
                    "Stoneward reduces incoming harm while riding together.",
                    "A stronger gait increases ride speed and clears broader gaps.",
                    "Ridge Charge",
                    visualScale: 1.1f,
                    mountedSpeed: 1.16f,
                    mountedDamage: 1.3f,
                    mountedJump: 1.15f,
                    mountedWard: 3)),
            Pact(
                MistlingId,
                "Mistling",
                "Care, recovery, and calm passage",
                false,
                Form(
                    RainwardenFormId,
                    "Rainwarden",
                    "High Veil restores more vitality and stamina.",
                    "Rainbreath improves stamina recovery while Mistling is active.",
                    "A guiding current increases swimming speed.",
                    "High Veil",
                    visualScale: 1.15f,
                    assistPotency: 1.1f,
                    tacticalPotency: 1.28f,
                    wardReduction: 1,
                    swimSpeedBonus: 1.25f,
                    staminaRegeneration: 4f)),
            Pact(
                CinderwingId,
                "Cinderwing",
                "Shared momentum and measured daring",
                false,
                Form(
                    SuncrestFormId,
                    "Suncrest",
                    "Sunflare reaches farther and carries greater force.",
                    "Emberdraft reduces gliding stamina use while Cinderwing is active.",
                    "Press R while gliding for one collision-bounded thermal lift per airtime.",
                    "Sunflare",
                    visualScale: 1.16f,
                    assistPotency: 1.12f,
                    tacticalPotency: 1.3f,
                    tacticalRange: 1.12f,
                    tacticalStagger: 1.12f,
                    glideCost: 0.72f,
                    boundedLift: true,
                    boundedLiftVelocity: 5.8f))
        };

        private static readonly Dictionary<string, PactDefinition> ById = BuildIdLookup();
        private static readonly Dictionary<string, PactDefinition> ByCreatureName = BuildCreatureLookup();
        private static readonly Dictionary<string, PactFormDefinition> FormsById = BuildFormLookup();

        public static IReadOnlyList<PactDefinition> All => Definitions;

        public static PactDefinition Find(string id)
        {
            return !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out var definition)
                ? definition
                : null;
        }

        public static PactDefinition FindByCreatureName(string creatureName)
        {
            return !string.IsNullOrEmpty(creatureName) &&
                   ByCreatureName.TryGetValue(creatureName, out var definition)
                ? definition
                : null;
        }

        public static PactFormDefinition FindForm(string id)
        {
            return !string.IsNullOrEmpty(id) && FormsById.TryGetValue(id, out var definition)
                ? definition
                : null;
        }

        public static PactDefinition FindForForm(string formId)
        {
            foreach (var pact in Definitions)
            {
                if (pact.greaterForms.Exists(form =>
                        form != null && string.Equals(form.id, formId, StringComparison.Ordinal)))
                {
                    return pact;
                }
            }

            return null;
        }

        private static PactDefinition Pact(
            string id,
            string creatureName,
            string bondTheme,
            bool rideReady,
            params PactFormDefinition[] forms)
        {
            return new PactDefinition
            {
                id = id,
                creatureName = creatureName,
                bondTheme = bondTheme,
                rideReady = rideReady,
                bondRankThresholds = new[] { 0, 45, 100, 180 },
                greaterForms = new List<PactFormDefinition>(forms ?? Array.Empty<PactFormDefinition>())
            };
        }

        private static PactFormDefinition Form(
            string id,
            string name,
            string tactical,
            string passive,
            string traversal,
            string tacticalLabel,
            float visualScale,
            float assistPotency = 1f,
            float tacticalPotency = 1f,
            float tacticalRange = 1f,
            float tacticalStagger = 1f,
            int wardReduction = 0,
            float climbCost = 1f,
            float glideCost = 1f,
            float swimSpeedBonus = 0f,
            float staminaRegeneration = 0f,
            float mountedSpeed = 1f,
            float mountedDamage = 1f,
            float mountedJump = 0f,
            int mountedWard = 0,
            bool boundedLift = false,
            float boundedLiftVelocity = 0f)
        {
            return new PactFormDefinition
            {
                id = id,
                displayName = name,
                tacticalDescription = tactical,
                passiveDescription = passive,
                traversalDescription = traversal,
                tacticalLabel = tacticalLabel,
                requiredBondRank = 3,
                visualScale = visualScale,
                assistPotencyMultiplier = assistPotency,
                tacticalPotencyMultiplier = tacticalPotency,
                tacticalRangeMultiplier = tacticalRange,
                tacticalStaggerMultiplier = tacticalStagger,
                wardReduction = wardReduction,
                climbStaminaCostMultiplier = climbCost,
                glideStaminaCostMultiplier = glideCost,
                swimSpeedBonus = swimSpeedBonus,
                staminaRegenerationBonus = staminaRegeneration,
                mountedSpeedMultiplier = mountedSpeed,
                mountedDamageMultiplier = mountedDamage,
                mountedJumpVelocityBonus = mountedJump,
                mountedWardReduction = mountedWard,
                grantsBoundedThermalLift = boundedLift,
                boundedThermalLiftVelocity = boundedLiftVelocity
            };
        }

        private static Dictionary<string, PactDefinition> BuildIdLookup()
        {
            var result = new Dictionary<string, PactDefinition>(StringComparer.Ordinal);
            foreach (var definition in Definitions)
            {
                result.Add(definition.id, definition);
            }

            return result;
        }

        private static Dictionary<string, PactDefinition> BuildCreatureLookup()
        {
            var result = new Dictionary<string, PactDefinition>(StringComparer.Ordinal);
            foreach (var definition in Definitions)
            {
                result.Add(definition.creatureName, definition);
            }

            return result;
        }

        private static Dictionary<string, PactFormDefinition> BuildFormLookup()
        {
            var result = new Dictionary<string, PactFormDefinition>(StringComparer.Ordinal);
            foreach (var pact in Definitions)
            {
                foreach (var form in pact.greaterForms)
                {
                    result.Add(form.id, form);
                }
            }

            return result;
        }
    }

    public static class PactBondSourceIds
    {
        public const string PactFormed = "source.pact.formed";
        public const string FirstTacticalResponse = "source.pact.tactical";
        public const string FirstCrossweaveResponse = "source.pact.crossweave";
        public const string FirstThreadlinkResponse = "source.pact.threadlink";
        public const string AlderwakeRegion = "source.region.alderwake";
        public const string MorrowfenRegion = "source.region.morrowfen";
        public const string CinderwoldRegion = "source.region.cinderwold";
        public const string CampLevelOne = "source.camp.level.1";
        public const string CampLevelTwo = "source.camp.level.2";
        public const string CampLevelThree = "source.camp.level.3";
        public const string SkyvaultRestored = "source.skyvault.restored";
        public const string EncounterPrefix = "source.encounter.";

        public static string ForEncounter(string persistenceId)
        {
            if (string.IsNullOrWhiteSpace(persistenceId))
            {
                return string.Empty;
            }

            var value = persistenceId.Trim();
            return value.Length <= 160 && !ContainsControlCharacter(value)
                ? EncounterPrefix + value
                : string.Empty;
        }

        public static string ForRegion(string regionName)
        {
            return regionName switch
            {
                "Alderwake Basin" => AlderwakeRegion,
                "Morrowfen" => MorrowfenRegion,
                "Cinderwold Verge" => CinderwoldRegion,
                _ => string.Empty
            };
        }

        public static string ForCampLevel(int level)
        {
            return level switch
            {
                1 => CampLevelOne,
                2 => CampLevelTwo,
                3 => CampLevelThree,
                _ => string.Empty
            };
        }

        public static bool TryGetAward(string sourceId, out int amount)
        {
            amount = sourceId switch
            {
                PactFormed => 20,
                FirstTacticalResponse => 12,
                FirstCrossweaveResponse => 12,
                FirstThreadlinkResponse => 12,
                AlderwakeRegion => 15,
                MorrowfenRegion => 15,
                CinderwoldRegion => 15,
                CampLevelOne => 10,
                CampLevelTwo => 10,
                CampLevelThree => 10,
                SkyvaultRestored => 25,
                _ => 0
            };
            if (amount > 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(sourceId) ||
                !sourceId.StartsWith(EncounterPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var encounterId = sourceId.Substring(EncounterPrefix.Length);
            if (encounterId.Length == 0 ||
                encounterId.Length > 160 ||
                ContainsControlCharacter(encounterId))
            {
                return false;
            }

            amount = 20;
            return true;
        }

        public static string Describe(string sourceId)
        {
            return sourceId switch
            {
                PactFormed => "a newly shared Pact",
                FirstTacticalResponse => "a first tactical response",
                FirstCrossweaveResponse => "a first Crossweave response",
                FirstThreadlinkResponse => "a first Threadlink response",
                AlderwakeRegion => "discovering Alderwake together",
                MorrowfenRegion => "discovering Morrowfen together",
                CinderwoldRegion => "discovering Cinderwold together",
                CampLevelOne => "sharing a field refuge",
                CampLevelTwo => "building a camp kitchen",
                CampLevelThree => "completing camp services",
                SkyvaultRestored => "restoring the Skyvault route",
                _ when sourceId != null &&
                       sourceId.StartsWith(EncounterPrefix, StringComparison.Ordinal) =>
                    "a first-clear encounter",
                _ => "shared fieldwork"
            };
        }

        private static bool ContainsControlCharacter(string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                if (char.IsControl(value[index]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class PactProgressionRules
    {
        public static int GetBondRank(PactDefinition definition, int bondXp)
        {
            if (definition == null ||
                definition.bondRankThresholds == null ||
                definition.bondRankThresholds.Length == 0)
            {
                return 0;
            }

            var rank = 1;
            var safeXp = Mathf.Max(0, bondXp);
            for (var index = 1; index < definition.bondRankThresholds.Length; index++)
            {
                if (safeXp < definition.bondRankThresholds[index])
                {
                    break;
                }

                rank = index + 1;
            }

            return rank;
        }

        public static int GetNextBondThreshold(PactDefinition definition, int bondRank)
        {
            if (definition == null ||
                definition.bondRankThresholds == null ||
                definition.bondRankThresholds.Length == 0)
            {
                return 0;
            }

            var nextIndex = Mathf.Clamp(bondRank, 0, definition.bondRankThresholds.Length - 1);
            return definition.bondRankThresholds[nextIndex];
        }

        public static string GetRankName(int rank)
        {
            return rank switch
            {
                1 => "Attuned",
                2 => "Trusted",
                3 => "Unfolding Ready",
                4 => "Concordant",
                _ => "Unrecorded"
            };
        }

        public static PactProgressSaveData CreateRecord(string pactId)
        {
            var record = new PactProgressSaveData
            {
                pactId = pactId ?? string.Empty,
                unfoldedFormId = string.Empty
            };
            NormalizeRecord(record);
            return record;
        }

        public static bool TryAwardSource(
            PactProgressSaveData record,
            string sourceId,
            out int amount,
            out int previousRank)
        {
            amount = 0;
            previousRank = record != null ? Mathf.Max(1, record.bondRank) : 0;
            if (record == null ||
                TetherfallPactCatalog.Find(record.pactId) == null ||
                !PactBondSourceIds.TryGetAward(sourceId, out amount))
            {
                return false;
            }

            record.awardedSourceIds ??= new List<string>();
            if (record.awardedSourceIds.Exists(existing =>
                    string.Equals(existing, sourceId, StringComparison.Ordinal)))
            {
                return false;
            }

            NormalizeRecord(record);
            previousRank = record.bondRank;
            record.awardedSourceIds.Add(sourceId);
            NormalizeRecord(record);
            return true;
        }

        public static void NormalizeInPlace(
            List<PactProgressSaveData> records,
            IReadOnlyList<string> pactedCreatureNames = null)
        {
            if (records == null)
            {
                return;
            }

            HashSet<string> allowedPactIds = null;
            if (pactedCreatureNames != null)
            {
                allowedPactIds = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < pactedCreatureNames.Count; index++)
                {
                    var definition =
                        TetherfallPactCatalog.FindByCreatureName(pactedCreatureNames[index]);
                    if (definition != null)
                    {
                        allowedPactIds.Add(definition.id);
                    }
                }
            }

            var merged = new Dictionary<string, PactProgressSaveData>(StringComparer.Ordinal);
            foreach (var sourceRecord in records)
            {
                var definition = sourceRecord != null
                    ? TetherfallPactCatalog.Find(sourceRecord.pactId)
                    : null;
                if (definition == null ||
                    allowedPactIds != null && !allowedPactIds.Contains(definition.id))
                {
                    continue;
                }

                if (!merged.TryGetValue(definition.id, out var target))
                {
                    target = CreateRecord(definition.id);
                    merged.Add(definition.id, target);
                }

                if (sourceRecord.awardedSourceIds != null)
                {
                    foreach (var sourceId in sourceRecord.awardedSourceIds)
                    {
                        if (PactBondSourceIds.TryGetAward(sourceId, out _) &&
                            !target.awardedSourceIds.Exists(existing =>
                                string.Equals(existing, sourceId, StringComparison.Ordinal)))
                        {
                            target.awardedSourceIds.Add(sourceId);
                        }
                    }
                }

                var proposedForm = TetherfallPactCatalog.FindForm(sourceRecord.unfoldedFormId);
                if (string.IsNullOrEmpty(target.unfoldedFormId) &&
                    proposedForm != null &&
                    definition.greaterForms.Contains(proposedForm))
                {
                    target.unfoldedFormId = proposedForm.id;
                }
            }

            records.Clear();
            foreach (var definition in TetherfallPactCatalog.All)
            {
                if (!merged.TryGetValue(definition.id, out var record))
                {
                    continue;
                }

                NormalizeRecord(record);
                records.Add(record);
            }
        }

        public static void NormalizeRecord(PactProgressSaveData record)
        {
            if (record == null)
            {
                return;
            }

            var definition = TetherfallPactCatalog.Find(record.pactId);
            if (definition == null)
            {
                record.pactId = string.Empty;
                record.bondXp = 0;
                record.bondRank = 0;
                record.unfoldedFormId = string.Empty;
                record.awardedSourceIds ??= new List<string>();
                record.awardedSourceIds.Clear();
                return;
            }

            record.awardedSourceIds ??= new List<string>();
            var uniqueSources = new HashSet<string>(StringComparer.Ordinal);
            var totalXp = 0L;
            for (var index = record.awardedSourceIds.Count - 1; index >= 0; index--)
            {
                var sourceId = record.awardedSourceIds[index];
                if (!PactBondSourceIds.TryGetAward(sourceId, out var amount) ||
                    !uniqueSources.Add(sourceId))
                {
                    record.awardedSourceIds.RemoveAt(index);
                    continue;
                }

                totalXp += amount;
            }

            record.bondXp = (int)Math.Min(int.MaxValue, Math.Max(0L, totalXp));
            record.bondRank = GetBondRank(definition, record.bondXp);
            record.unfoldedFormId ??= string.Empty;
            var form = TetherfallPactCatalog.FindForm(record.unfoldedFormId);
            if (form == null ||
                !definition.greaterForms.Contains(form) ||
                record.bondRank < form.requiredBondRank)
            {
                record.unfoldedFormId = string.Empty;
            }
        }
    }

    public sealed partial class TetherfallGame
    {
        private readonly List<PactProgressSaveData> pactProgressRecords =
            new List<PactProgressSaveData>();
        private bool showingPactUnfoldingCamp;
        private string selectedPactLedgerId;
        private string pendingPactFormId;
        private string pactUnfoldingFeedback =
            "Bond grows through first-clear fieldwork, discovery, camp milestones, and first coordinated responses.";

        public IReadOnlyList<PactProgressSaveData> PactProgressRecords => pactProgressRecords;

        public int PactWardReduction
        {
            get
            {
                var reduction = GetActiveSupportForm()?.wardReduction ?? 0;
                if (Player != null && Player.IsMounted)
                {
                    reduction += GetMountedForm()?.mountedWardReduction ?? 0;
                }

                return Mathf.Max(0, reduction);
            }
        }

        public float PactStaminaRegenerationBonus =>
            Mathf.Max(0f, GetActiveSupportForm()?.staminaRegenerationBonus ?? 0f);
        public float PactClimbStaminaCostMultiplier =>
            Mathf.Clamp(GetActiveSupportForm()?.climbStaminaCostMultiplier ?? 1f, 0.5f, 1f);
        public float PactGlideStaminaCostMultiplier =>
            Mathf.Clamp(GetActiveSupportForm()?.glideStaminaCostMultiplier ?? 1f, 0.5f, 1f);
        public float PactSwimSpeedBonus =>
            Mathf.Max(0f, GetActiveSupportForm()?.swimSpeedBonus ?? 0f);
        public float MountedPactSpeedMultiplier =>
            Mathf.Clamp(GetMountedForm()?.mountedSpeedMultiplier ?? 1f, 1f, 1.5f);
        public float MountedPactDamageMultiplier =>
            Mathf.Clamp(GetMountedForm()?.mountedDamageMultiplier ?? 1f, 1f, 1.75f);
        public float MountedPactJumpVelocityBonus =>
            Mathf.Clamp(GetMountedForm()?.mountedJumpVelocityBonus ?? 0f, 0f, 2.5f);
        public bool HasBoundedPactThermalLift =>
            GetActiveSupportForm()?.grantsBoundedThermalLift == true;
        public float BoundedPactThermalLiftVelocity =>
            Mathf.Clamp(GetActiveSupportForm()?.boundedThermalLiftVelocity ?? 0f, 0f, 6.5f);

        public float GetPactFormAssistPotencyMultiplier(PactCreature pact)
        {
            return Mathf.Clamp(GetAppliedForm(pact)?.assistPotencyMultiplier ?? 1f, 1f, 1.5f);
        }

        public float GetPactFormTacticalPotencyMultiplier(PactCreature pact)
        {
            return Mathf.Clamp(GetAppliedForm(pact)?.tacticalPotencyMultiplier ?? 1f, 1f, 1.6f);
        }

        public float GetPactFormTacticalRangeMultiplier(PactCreature pact)
        {
            return Mathf.Clamp(GetAppliedForm(pact)?.tacticalRangeMultiplier ?? 1f, 1f, 1.35f);
        }

        public float GetPactFormTacticalStaggerMultiplier(PactCreature pact)
        {
            return Mathf.Clamp(GetAppliedForm(pact)?.tacticalStaggerMultiplier ?? 1f, 1f, 1.5f);
        }

        public string GetPactTacticalLabel(PactCreature pact, string baseLabel)
        {
            var form = GetAppliedForm(pact);
            return form != null && !string.IsNullOrWhiteSpace(form.tacticalLabel)
                ? form.tacticalLabel
                : baseLabel;
        }

        public string GetPactHudStatus(PactCreature pact)
        {
            if (pact == null)
            {
                return "No active Pact";
            }

            var record = FindPactProgress(pact.PactId);
            if (record == null)
            {
                return pact.CreatureName;
            }

            var form = TetherfallPactCatalog.FindForm(record.unfoldedFormId);
            var state = form != null
                ? form.displayName
                : record.bondRank >= 3
                    ? "Unfolding ready"
                    : $"Bond R{record.bondRank}";
            return $"{pact.CreatureName} · {state}";
        }

        public string GetMountPactHudStatus()
        {
            if (MountEchosworn == null)
            {
                return HasBoundedPactThermalLift
                    ? "R  Thermal lift while gliding"
                    : "R  Mount  Unavailable";
            }

            var record = FindPactProgress(MountEchosworn.PactId);
            var form = record != null
                ? TetherfallPactCatalog.FindForm(record.unfoldedFormId)
                : null;
            return form != null
                ? $"R  Ride  {MountEchosworn.CreatureName} · {form.displayName}"
                : $"R  Ride  {MountEchosworn.CreatureName}";
        }

        public void OnPactJoinedForUnfolding(PactCreature pact)
        {
            if (pact == null || !pact.IsPacted)
            {
                return;
            }

            var record = EnsurePactProgress(pact);
            TryAwardPactSource(record, PactBondSourceIds.PactFormed, false, out _);
            ReconcilePactWithCurrentJourney(pact, record);
            selectedPactLedgerId = pact.PactId;
            Notify(
                $"{pact.CreatureName}'s shared bond is {PactProgressionRules.GetRankName(record.bondRank)} " +
                $"(rank {record.bondRank}). Unfolding is always a camp choice.");
        }

        public void RecordPactTacticalResponse(PactCreature pact)
        {
            AwardSinglePactSource(pact, PactBondSourceIds.FirstTacticalResponse);
        }

        public void RecordPactCrossweaveResponse(PactCreature pact)
        {
            AwardSinglePactSource(pact, PactBondSourceIds.FirstCrossweaveResponse);
        }

        public void RecordPactThreadlinkResponse(PactCreature pact)
        {
            AwardSinglePactSource(pact, PactBondSourceIds.FirstThreadlinkResponse);
        }

        public void RecordPactFirstClear(EnemyCamp camp)
        {
            if (camp != null)
            {
                AwardExpeditionPactSource(PactBondSourceIds.ForEncounter(camp.PersistenceId));
            }
        }

        public void RecordPactRegionDiscovery(string regionName)
        {
            AwardExpeditionPactSource(PactBondSourceIds.ForRegion(regionName));
        }

        public void RecordPactCampLevel(int campLevel)
        {
            AwardExpeditionPactSource(PactBondSourceIds.ForCampLevel(campLevel));
        }

        public void RecordPactSkyvaultRestored()
        {
            AwardExpeditionPactSource(PactBondSourceIds.SkyvaultRestored);
        }

        private void AwardSinglePactSource(PactCreature pact, string sourceId)
        {
            if (pact == null || !pact.IsPacted)
            {
                return;
            }

            var record = EnsurePactProgress(pact);
            if (!TryAwardPactSource(record, sourceId, true, out var rankAdvanced))
            {
                return;
            }

            if (rankAdvanced && record.bondRank >= 3 && string.IsNullOrEmpty(record.unfoldedFormId))
            {
                Notify($"{pact.CreatureName} is ready to consider a greater form at camp.");
            }
        }

        private void AwardExpeditionPactSource(string sourceId)
        {
            if (!PactBondSourceIds.TryGetAward(sourceId, out var amount))
            {
                return;
            }

            var awarded = 0;
            var readyNames = new List<string>();
            foreach (var pact in pacts)
            {
                if (pact == null || !pact.IsPacted)
                {
                    continue;
                }

                var record = EnsurePactProgress(pact);
                if (!TryAwardPactSource(record, sourceId, false, out var rankAdvanced))
                {
                    continue;
                }

                awarded++;
                if (rankAdvanced &&
                    record.bondRank >= 3 &&
                    string.IsNullOrEmpty(record.unfoldedFormId))
                {
                    readyNames.Add(pact.CreatureName);
                }
            }

            if (awarded <= 0)
            {
                return;
            }

            Notify(
                $"Shared Pact bond +{amount}: {PactBondSourceIds.Describe(sourceId)} " +
                $"({awarded} {(awarded == 1 ? "Pact" : "Pacts")}).");
            if (readyNames.Count > 0)
            {
                Notify($"{string.Join(", ", readyNames)} can now consider unfolding at camp.");
            }

            RequestSave();
        }

        private bool TryAwardPactSource(
            PactProgressSaveData record,
            string sourceId,
            bool notify,
            out bool rankAdvanced)
        {
            rankAdvanced = false;
            if (!PactProgressionRules.TryAwardSource(
                    record,
                    sourceId,
                    out var amount,
                    out var previousRank))
            {
                return false;
            }

            rankAdvanced = record.bondRank > previousRank;
            if (notify)
            {
                var pact = TetherfallPactCatalog.Find(record.pactId);
                Notify(
                    $"{pact?.creatureName ?? "Pact"} bond +{amount}: " +
                    $"{PactBondSourceIds.Describe(sourceId)}.");
            }

            RequestSave();
            return true;
        }

        private PactProgressSaveData EnsurePactProgress(PactCreature pact)
        {
            var record = FindPactProgress(pact != null ? pact.PactId : string.Empty);
            if (record != null)
            {
                return record;
            }

            var definition = pact != null
                ? TetherfallPactCatalog.Find(pact.PactId)
                : null;
            if (definition == null)
            {
                return null;
            }

            record = PactProgressionRules.CreateRecord(definition.id);
            pactProgressRecords.Add(record);
            return record;
        }

        private PactProgressSaveData FindPactProgress(string pactId)
        {
            if (string.IsNullOrEmpty(pactId))
            {
                return null;
            }

            return pactProgressRecords.Find(record =>
                record != null && string.Equals(record.pactId, pactId, StringComparison.Ordinal));
        }

        private PactCreature FindPactByStableId(string pactId)
        {
            foreach (var pact in knownEchosworn)
            {
                if (pact != null && string.Equals(pact.PactId, pactId, StringComparison.Ordinal))
                {
                    return pact;
                }
            }

            return null;
        }

        private PactFormDefinition GetAppliedForm(PactCreature pact)
        {
            if (pact == null || !pact.IsPacted)
            {
                return null;
            }

            var record = FindPactProgress(pact.PactId);
            return record != null
                ? TetherfallPactCatalog.FindForm(record.unfoldedFormId)
                : null;
        }

        private PactFormDefinition GetActiveSupportForm()
        {
            return GetAppliedForm(ActiveEchosworn);
        }

        private PactFormDefinition GetMountedForm()
        {
            return GetAppliedForm(MountEchosworn);
        }

        private void ReconcilePactWithCurrentJourney(
            PactCreature pact,
            PactProgressSaveData record)
        {
            if (pact == null || record == null)
            {
                return;
            }

            TryAwardPactSource(record, PactBondSourceIds.PactFormed, false, out _);
            foreach (var region in discoveredRegions)
            {
                TryAwardPactSource(record, PactBondSourceIds.ForRegion(region), false, out _);
            }

            foreach (var camp in enemyCamps)
            {
                if (camp != null && camp.AwardedFirstClear)
                {
                    TryAwardPactSource(
                        record,
                        PactBondSourceIds.ForEncounter(camp.PersistenceId),
                        false,
                        out _);
                }
            }

            if (ActiveCamp != null)
            {
                for (var level = 1; level <= campTier + 1; level++)
                {
                    TryAwardPactSource(record, PactBondSourceIds.ForCampLevel(level), false, out _);
                }
            }

            if (dungeonBossDefeated)
            {
                TryAwardPactSource(record, PactBondSourceIds.SkyvaultRestored, false, out _);
            }
        }

        private void WritePactUnfoldingSave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.pactProgress ??= new List<PactProgressSaveData>();
            data.pactProgress.Clear();
            foreach (var record in pactProgressRecords)
            {
                if (record == null)
                {
                    continue;
                }

                PactProgressionRules.NormalizeRecord(record);
                data.pactProgress.Add(new PactProgressSaveData
                {
                    pactId = record.pactId,
                    bondXp = record.bondXp,
                    bondRank = record.bondRank,
                    unfoldedFormId = record.unfoldedFormId,
                    awardedSourceIds = new List<string>(
                        record.awardedSourceIds ?? new List<string>())
                });
            }
        }

        private void RestorePactUnfoldingState(TetherfallSaveData data)
        {
            pactProgressRecords.Clear();
            if (data != null && data.pactProgress != null)
            {
                foreach (var record in data.pactProgress)
                {
                    if (record == null)
                    {
                        continue;
                    }

                    pactProgressRecords.Add(new PactProgressSaveData
                    {
                        pactId = record.pactId,
                        bondXp = record.bondXp,
                        bondRank = record.bondRank,
                        unfoldedFormId = record.unfoldedFormId,
                        awardedSourceIds = new List<string>(
                            record.awardedSourceIds ?? new List<string>())
                    });
                }
            }

            PactProgressionRules.NormalizeInPlace(
                pactProgressRecords,
                data != null ? data.pactNames : null);
            foreach (var pact in pacts)
            {
                if (pact == null || !pact.IsPacted)
                {
                    continue;
                }

                var record = EnsurePactProgress(pact);
                ReconcilePactWithCurrentJourney(pact, record);
                if (!string.IsNullOrEmpty(record.unfoldedFormId))
                {
                    pact.ApplyUnfoldedForm(record.unfoldedFormId);
                }
            }

            if (string.IsNullOrEmpty(selectedPactLedgerId))
            {
                foreach (var pact in pacts)
                {
                    if (pact != null && pact.IsPacted)
                    {
                        selectedPactLedgerId = pact.PactId;
                        break;
                    }
                }
            }
        }

        private void DrawPactUnfoldingCamp(Rect rect)
        {
            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 70f, rect.width - 52f, 42f),
                "Pact Unfolding is never automatic. Bond ranks make a form available; " +
                "you and the Echosworn choose it here together.",
                bodyStyle);

            var listX = rect.x + 26f;
            var listY = rect.y + 126f;
            const float listWidth = 250f;
            var visiblePacts = 0;
            foreach (var definition in TetherfallPactCatalog.All)
            {
                var pact = FindPactByStableId(definition.id);
                if (pact == null || !pact.IsPacted)
                {
                    continue;
                }

                var record = EnsurePactProgress(pact);
                var selected = string.Equals(
                    selectedPactLedgerId,
                    definition.id,
                    StringComparison.Ordinal);
                var form = TetherfallPactCatalog.FindForm(record.unfoldedFormId);
                var label = form != null
                    ? $"{definition.creatureName}\n{form.displayName} · R{record.bondRank}"
                    : $"{definition.creatureName}\nBond R{record.bondRank} · {record.bondXp} XP";
                if (GUI.Button(
                        new Rect(listX, listY + visiblePacts * 62f, listWidth, 52f),
                        (selected ? "• " : string.Empty) + label))
                {
                    selectedPactLedgerId = definition.id;
                    pendingPactFormId = string.Empty;
                    pactUnfoldingFeedback = $"{definition.creatureName}: {definition.bondTheme}.";
                }

                visiblePacts++;
            }

            if (visiblePacts == 0)
            {
                GUI.Label(
                    new Rect(listX, listY, listWidth, 80f),
                    "No Pact has joined this expedition yet.",
                    mutedStyle);
                return;
            }

            var selectedPact = FindPactByStableId(selectedPactLedgerId);
            if (selectedPact == null || !selectedPact.IsPacted)
            {
                foreach (var pact in pacts)
                {
                    if (pact != null && pact.IsPacted)
                    {
                        selectedPact = pact;
                        selectedPactLedgerId = pact.PactId;
                        break;
                    }
                }
            }

            var selectedDefinition = selectedPact != null
                ? TetherfallPactCatalog.Find(selectedPact.PactId)
                : null;
            var selectedRecord = selectedPact != null
                ? EnsurePactProgress(selectedPact)
                : null;
            if (selectedDefinition == null || selectedRecord == null)
            {
                return;
            }

            var detailX = rect.x + 308f;
            var detailWidth = rect.xMax - detailX - 28f;
            GUI.Label(
                new Rect(detailX, listY, detailWidth, 26f),
                $"{selectedDefinition.creatureName} · Bond rank {selectedRecord.bondRank}/" +
                $"{selectedDefinition.bondRankThresholds.Length} " +
                $"({PactProgressionRules.GetRankName(selectedRecord.bondRank)})",
                headingStyle);
            var maximumRank =
                selectedRecord.bondRank >= selectedDefinition.bondRankThresholds.Length;
            var targetXp = maximumRank
                ? selectedDefinition.bondRankThresholds[
                    selectedDefinition.bondRankThresholds.Length - 1]
                : PactProgressionRules.GetNextBondThreshold(
                    selectedDefinition,
                    selectedRecord.bondRank);
            DrawBar(
                new Rect(detailX, listY + 34f, detailWidth, 10f),
                selectedRecord.bondXp,
                Mathf.Max(1, targetXp));
            GUI.Label(
                new Rect(detailX, listY + 49f, detailWidth, 20f),
                maximumRank
                    ? $"Bond XP {selectedRecord.bondXp} · maximum rank"
                    : $"Bond XP {selectedRecord.bondXp}/{targetXp} · " +
                      "one-time sources cannot be farmed",
                hudMutedRowStyle);

            var currentForm = TetherfallPactCatalog.FindForm(selectedRecord.unfoldedFormId);
            if (currentForm != null)
            {
                GUI.Label(
                    new Rect(detailX, listY + 82f, detailWidth, 26f),
                    $"GREATER FORM · {currentForm.displayName}",
                    headingStyle);
                DrawPactFormEffects(
                    detailX,
                    listY + 116f,
                    detailWidth,
                    currentForm);
                GUI.Label(
                    new Rect(detailX, listY + 260f, detailWidth, 44f),
                    "This shared form is persistent. Its authored silhouette and procedural " +
                    "motion are active in the field.",
                    mutedStyle);
            }
            else
            {
                GUI.Label(
                    new Rect(detailX, listY + 82f, detailWidth, 26f),
                    selectedRecord.bondRank >= 3
                        ? "A GREATER FORM IS READY TO CONSIDER"
                        : "GREATER FORM · NOT YET READY",
                    headingStyle);
                var form = selectedDefinition.greaterForms.Count > 0
                    ? selectedDefinition.greaterForms[0]
                    : null;
                if (form != null)
                {
                    DrawPactFormEffects(detailX, listY + 116f, detailWidth, form);
                    var ready = selectedRecord.bondRank >= form.requiredBondRank;
                    if (GUI.Button(
                            new Rect(detailX, listY + 270f, 220f, 38f),
                            ready
                                ? $"Consider {form.displayName}"
                                : $"Needs bond rank {form.requiredBondRank}"))
                    {
                        if (ready)
                        {
                            pendingPactFormId = form.id;
                            pactUnfoldingFeedback =
                                $"{selectedDefinition.creatureName} is listening. " +
                                "Confirm only if you want this persistent shared form.";
                        }
                        else
                        {
                            pactUnfoldingFeedback =
                                "Keep traveling together. Readable one-time sources are listed below.";
                        }
                    }

                    if (string.Equals(
                            pendingPactFormId,
                            form.id,
                            StringComparison.Ordinal))
                    {
                        GUI.Label(
                            new Rect(detailX, listY + 316f, detailWidth, 42f),
                            $"Confirm {form.displayName}? This is a voluntary, persistent branch choice.",
                            bodyStyle);
                        if (GUI.Button(
                                new Rect(detailX, listY + 364f, 190f, 36f),
                                "Confirm unfolding"))
                        {
                            TryConfirmPactUnfolding(selectedPact, form);
                        }

                        if (GUI.Button(
                                new Rect(detailX + 204f, listY + 364f, 120f, 36f),
                                "Not now"))
                        {
                            pendingPactFormId = string.Empty;
                            pactUnfoldingFeedback =
                                "No change made. The choice remains available at camp.";
                        }
                    }
                }
            }

            GUI.Label(
                new Rect(listX, rect.yMax - 102f, rect.width - 52f, 38f),
                "Bond sources: Pact formed (20), each first-clear encounter (20), each new " +
                "region (15), camp levels (10), first tactical/Crossweave/Threadlink responses " +
                "(12), and Skyvault restoration (25). Each source pays once per Pact.",
                tinyStyle);
            GUI.Label(
                new Rect(listX, rect.yMax - 58f, rect.width - 52f, 24f),
                pactUnfoldingFeedback,
                mutedStyle);
        }

        private void DrawPactFormEffects(
            float x,
            float y,
            float width,
            PactFormDefinition form)
        {
            GUI.Label(
                new Rect(x, y, width, 40f),
                $"TACTICAL · {form.tacticalDescription}",
                bodyStyle);
            GUI.Label(
                new Rect(x, y + 46f, width, 40f),
                $"PASSIVE · {form.passiveDescription}",
                bodyStyle);
            GUI.Label(
                new Rect(x, y + 92f, width, 48f),
                $"TRAVERSAL · {form.traversalDescription}",
                bodyStyle);
        }

        private void TryConfirmPactUnfolding(
            PactCreature pact,
            PactFormDefinition form)
        {
            if (pact == null || form == null || !pact.IsPacted)
            {
                pactUnfoldingFeedback = "That Pact choice is no longer available.";
                pendingPactFormId = string.Empty;
                return;
            }

            if (ActiveCamp == null ||
                Vector3.Distance(PlayerTransform.position, ActiveCamp.transform.position) > 4.6f)
            {
                pactUnfoldingFeedback = "Return beside the portable camp to make this choice.";
                return;
            }

            var definition = TetherfallPactCatalog.Find(pact.PactId);
            var record = EnsurePactProgress(pact);
            if (definition == null ||
                record == null ||
                !definition.greaterForms.Contains(form))
            {
                pactUnfoldingFeedback = "The selected form does not belong to this Pact.";
                pendingPactFormId = string.Empty;
                return;
            }

            if (!string.IsNullOrEmpty(record.unfoldedFormId))
            {
                pactUnfoldingFeedback = "This Pact has already chosen its greater form.";
                pendingPactFormId = string.Empty;
                return;
            }

            if (record.bondRank < form.requiredBondRank)
            {
                pactUnfoldingFeedback =
                    $"Bond rank {form.requiredBondRank} is required. No change was made.";
                pendingPactFormId = string.Empty;
                return;
            }

            record.unfoldedFormId = form.id;
            PactProgressionRules.NormalizeRecord(record);
            if (!string.Equals(record.unfoldedFormId, form.id, StringComparison.Ordinal) ||
                !pact.ApplyUnfoldedForm(form.id))
            {
                record.unfoldedFormId = string.Empty;
                pactUnfoldingFeedback = "The form could not be applied safely. No choice was saved.";
                pendingPactFormId = string.Empty;
                return;
            }

            pendingPactFormId = string.Empty;
            pactUnfoldingFeedback =
                $"{pact.CreatureName} and the Wayfinder chose {form.displayName}.";
            Notify(
                $"{pact.CreatureName} unfolds as {form.displayName}, by shared choice.");
            RequestCriticalSave();
        }
    }
}
