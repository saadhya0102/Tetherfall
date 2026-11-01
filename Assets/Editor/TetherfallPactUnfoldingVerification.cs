using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallPactUnfoldingVerification
    {
        private const string MenuPath = "Tetherfall/Verify Pact Unfolding";

        [MenuItem(MenuPath, false, 2206)]
        public static void Verify()
        {
            VerifyInternal(true);
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            VerifyInternal(false);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            var failures = new List<string>();
            VerifyCatalog(failures);
            VerifyThresholdsAndSources(failures);
            VerifySaveNormalization(failures);
            VerifyIdempotentFormApplication(failures);
            VerifyInvalidData(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Pact Unfolding verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    $"Tetherfall Pact Unfolding verification passed: " +
                    $"{TetherfallPactCatalog.All.Count} stable Pact definitions, thresholds, " +
                    "bounded source normalization, save migration, form idempotency, and " +
                    "invalid-data rejection verified.");
            }
        }

        private static void VerifyCatalog(List<string> failures)
        {
            var expectedCreatures = new HashSet<string>(
                new[] { "Bramblet", "Slateback", "Mistling", "Cinderwing" },
                StringComparer.Ordinal);
            var pactIds = new HashSet<string>(StringComparer.Ordinal);
            var creatureNames = new HashSet<string>(StringComparer.Ordinal);
            var formIds = new HashSet<string>(StringComparer.Ordinal);

            Check(
                TetherfallPactCatalog.All.Count == expectedCreatures.Count,
                $"Catalog has {TetherfallPactCatalog.All.Count} Pacts; expected " +
                $"{expectedCreatures.Count} existing Echosworn.",
                failures);

            foreach (var pact in TetherfallPactCatalog.All)
            {
                Check(pact != null, "Catalog contains a null Pact definition.", failures);
                if (pact == null)
                {
                    continue;
                }

                Check(!string.IsNullOrWhiteSpace(pact.id),
                    "A Pact definition has no stable ID.", failures);
                Check(pactIds.Add(pact.id),
                    $"Duplicate Pact ID '{pact.id}'.", failures);
                Check(!string.IsNullOrWhiteSpace(pact.creatureName),
                    $"Pact '{pact.id}' has no creature name.", failures);
                Check(creatureNames.Add(pact.creatureName),
                    $"Creature '{pact.creatureName}' has duplicate Pact definitions.", failures);
                Check(expectedCreatures.Remove(pact.creatureName),
                    $"Unexpected Pact creature '{pact.creatureName}'.", failures);
                Check(!string.IsNullOrWhiteSpace(pact.bondTheme),
                    $"Pact '{pact.id}' has no bond theme.", failures);
                Check(
                    pact.bondRankThresholds != null &&
                    pact.bondRankThresholds.Length >= 3,
                    $"Pact '{pact.id}' needs at least three bond ranks.",
                    failures);
                VerifyThresholdOrder(pact, failures);
                Check(pact.greaterForms != null && pact.greaterForms.Count >= 1,
                    $"Pact '{pact.id}' has no greater-form branch.", failures);

                if (pact.greaterForms == null)
                {
                    continue;
                }

                foreach (var form in pact.greaterForms)
                {
                    Check(form != null, $"Pact '{pact.id}' has a null form.", failures);
                    if (form == null)
                    {
                        continue;
                    }

                    Check(!string.IsNullOrWhiteSpace(form.id),
                        $"Pact '{pact.id}' has a form without a stable ID.", failures);
                    Check(formIds.Add(form.id),
                        $"Duplicate form ID '{form.id}'.", failures);
                    Check(
                        ReferenceEquals(TetherfallPactCatalog.FindForForm(form.id), pact),
                        $"Form '{form.id}' does not resolve to its owning Pact.",
                        failures);
                    Check(!string.IsNullOrWhiteSpace(form.displayName),
                        $"Form '{form.id}' has no display name.", failures);
                    Check(!string.IsNullOrWhiteSpace(form.tacticalDescription),
                        $"Form '{form.id}' has no tactical description.", failures);
                    Check(!string.IsNullOrWhiteSpace(form.passiveDescription),
                        $"Form '{form.id}' has no passive description.", failures);
                    Check(!string.IsNullOrWhiteSpace(form.traversalDescription),
                        $"Form '{form.id}' has no traversal description.", failures);
                    Check(
                        form.requiredBondRank >= 2 &&
                        pact.bondRankThresholds != null &&
                        form.requiredBondRank <= pact.bondRankThresholds.Length,
                        $"Form '{form.id}' has invalid required rank {form.requiredBondRank}.",
                        failures);
                    Check(IsFinite(form.visualScale) &&
                          form.visualScale >= 1f &&
                          form.visualScale <= 1.3f,
                        $"Form '{form.id}' has unsafe visual scale {form.visualScale}.",
                        failures);
                    VerifyFiniteEffects(form, failures);
                }
            }

            Check(expectedCreatures.Count == 0,
                $"Missing Pact definitions: {string.Join(", ", expectedCreatures)}.",
                failures);

            var slateback = TetherfallPactCatalog.Find(TetherfallPactCatalog.SlatebackId);
            var ridgebearer =
                TetherfallPactCatalog.FindForm(TetherfallPactCatalog.RidgebearerFormId);
            Check(slateback != null && slateback.rideReady,
                "Slateback must remain the ride-ready base Pact.", failures);
            Check(ridgebearer != null &&
                  ridgebearer.mountedSpeedMultiplier > 1f &&
                  ridgebearer.mountedDamageMultiplier > 1f &&
                  ridgebearer.mountedJumpVelocityBonus > 0f,
                "Ridgebearer must provide real ride progression.", failures);

            var suncrest = TetherfallPactCatalog.FindForm(TetherfallPactCatalog.SuncrestFormId);
            Check(suncrest != null &&
                  suncrest.grantsBoundedThermalLift &&
                  suncrest.boundedThermalLiftVelocity > 0f &&
                  suncrest.boundedThermalLiftVelocity <= 6.5f,
                "Suncrest must grant a bounded, capped thermal lift.", failures);
        }

        private static void VerifyThresholdOrder(
            PactDefinition pact,
            List<string> failures)
        {
            if (pact.bondRankThresholds == null ||
                pact.bondRankThresholds.Length == 0)
            {
                return;
            }

            Check(pact.bondRankThresholds[0] == 0,
                $"Pact '{pact.id}' first threshold must be zero.", failures);
            for (var index = 1; index < pact.bondRankThresholds.Length; index++)
            {
                Check(
                    pact.bondRankThresholds[index] >
                    pact.bondRankThresholds[index - 1],
                    $"Pact '{pact.id}' thresholds are not strictly increasing at rank " +
                    $"{index + 1}.",
                    failures);
            }
        }

        private static void VerifyFiniteEffects(
            PactFormDefinition form,
            List<string> failures)
        {
            var multiplierValues = new[]
            {
                form.assistPotencyMultiplier,
                form.tacticalPotencyMultiplier,
                form.tacticalRangeMultiplier,
                form.tacticalStaggerMultiplier,
                form.climbStaminaCostMultiplier,
                form.glideStaminaCostMultiplier,
                form.mountedSpeedMultiplier,
                form.mountedDamageMultiplier
            };
            foreach (var value in multiplierValues)
            {
                Check(IsFinite(value) && value > 0f,
                    $"Form '{form.id}' has a non-finite or non-positive multiplier.",
                    failures);
            }

            Check(IsFinite(form.swimSpeedBonus) && form.swimSpeedBonus >= 0f,
                $"Form '{form.id}' has invalid swim speed.", failures);
            Check(IsFinite(form.staminaRegenerationBonus) &&
                  form.staminaRegenerationBonus >= 0f,
                $"Form '{form.id}' has invalid stamina regeneration.", failures);
            Check(IsFinite(form.mountedJumpVelocityBonus) &&
                  form.mountedJumpVelocityBonus >= 0f,
                $"Form '{form.id}' has invalid mounted jump bonus.", failures);
            Check(IsFinite(form.boundedThermalLiftVelocity) &&
                  form.boundedThermalLiftVelocity >= 0f,
                $"Form '{form.id}' has invalid thermal lift.", failures);
            Check(form.wardReduction >= 0 && form.mountedWardReduction >= 0,
                $"Form '{form.id}' has a negative ward value.", failures);
        }

        private static void VerifyThresholdsAndSources(List<string> failures)
        {
            foreach (var pact in TetherfallPactCatalog.All)
            {
                for (var index = 0; index < pact.bondRankThresholds.Length; index++)
                {
                    var threshold = pact.bondRankThresholds[index];
                    Check(
                        PactProgressionRules.GetBondRank(pact, threshold) == index + 1,
                        $"Pact '{pact.id}' did not enter rank {index + 1} at XP {threshold}.",
                        failures);
                    if (index > 0)
                    {
                        Check(
                            PactProgressionRules.GetBondRank(pact, threshold - 1) == index,
                            $"Pact '{pact.id}' entered rank {index + 1} before its threshold.",
                            failures);
                    }
                }
            }

            var record =
                PactProgressionRules.CreateRecord(TetherfallPactCatalog.BrambletId);
            Check(
                PactProgressionRules.TryAwardSource(
                    record,
                    PactBondSourceIds.PactFormed,
                    out var amount,
                    out _),
                "A valid Pact-formed source was rejected.",
                failures);
            Check(amount == 20 && record.bondXp == 20,
                "Pact-formed source awarded an unexpected amount.", failures);
            Check(
                !PactProgressionRules.TryAwardSource(
                    record,
                    PactBondSourceIds.PactFormed,
                    out _,
                    out _),
                "The same bounded source awarded twice.",
                failures);
            Check(record.bondXp == 20,
                "A duplicate source changed bond XP.", failures);
        }

        private static void VerifySaveNormalization(List<string> failures)
        {
            var normalize = typeof(TetherfallSaveSystem).GetMethod(
                "Normalize",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(TetherfallSaveData) },
                null);
            Check(normalize != null,
                "TetherfallSaveSystem.Normalize was not found.", failures);
            if (normalize == null)
            {
                return;
            }

            var data = new TetherfallSaveData
            {
                schemaVersion = 20
            };
            data.pactNames.Add("Bramblet");
            data.pactNames.Add("Slateback");
            data.pactProgress.Add(new PactProgressSaveData
            {
                pactId = TetherfallPactCatalog.BrambletId,
                bondXp = int.MaxValue,
                bondRank = 99,
                unfoldedFormId = TetherfallPactCatalog.GrovewardenFormId,
                awardedSourceIds = new List<string>
                {
                    PactBondSourceIds.PactFormed,
                    PactBondSourceIds.PactFormed,
                    PactBondSourceIds.AlderwakeRegion,
                    PactBondSourceIds.MorrowfenRegion,
                    PactBondSourceIds.CinderwoldRegion,
                    PactBondSourceIds.CampLevelOne,
                    PactBondSourceIds.FirstTacticalResponse,
                    PactBondSourceIds.FirstCrossweaveResponse,
                    PactBondSourceIds.ForEncounter("encounter.verify.first"),
                    "source.invalid"
                }
            });
            data.pactProgress.Add(new PactProgressSaveData
            {
                pactId = TetherfallPactCatalog.BrambletId,
                awardedSourceIds = new List<string>
                {
                    PactBondSourceIds.FirstThreadlinkResponse
                }
            });
            data.pactProgress.Add(new PactProgressSaveData
            {
                pactId = TetherfallPactCatalog.SlatebackId,
                bondXp = 999,
                bondRank = 4,
                unfoldedFormId = TetherfallPactCatalog.SuncrestFormId,
                awardedSourceIds = new List<string>
                {
                    PactBondSourceIds.PactFormed
                }
            });
            data.pactProgress.Add(new PactProgressSaveData
            {
                pactId = "pact.unknown",
                bondXp = 500,
                bondRank = 4
            });
            data.pactProgress.Add(null);

            var listReference = data.pactProgress;
            InvokeNormalize(normalize, data, failures);
            Check(data.schemaVersion == TetherfallSaveData.CurrentSchemaVersion,
                "Save normalization did not migrate to the current schema.", failures);
            Check(ReferenceEquals(listReference, data.pactProgress),
                "Save normalization replaced the Pact progress list.", failures);
            Check(data.pactProgress.Count == 2,
                $"Save normalization retained {data.pactProgress.Count} records; expected 2.",
                failures);

            var bramblet = FindRecord(data.pactProgress, TetherfallPactCatalog.BrambletId);
            Check(bramblet != null, "Normalized Bramblet record is missing.", failures);
            if (bramblet != null)
            {
                Check(bramblet.bondXp == 131,
                    $"Bramblet normalized XP was {bramblet.bondXp}; expected 131.",
                    failures);
                Check(bramblet.bondRank == 3,
                    $"Bramblet normalized rank was {bramblet.bondRank}; expected 3.",
                    failures);
                Check(bramblet.unfoldedFormId == TetherfallPactCatalog.GrovewardenFormId,
                    "A valid unlocked form was not preserved.", failures);
                Check(bramblet.awardedSourceIds.Count == 9,
                    "Duplicate or invalid Bramblet sources were not normalized.", failures);
            }

            var slateback = FindRecord(data.pactProgress, TetherfallPactCatalog.SlatebackId);
            Check(slateback != null && slateback.bondXp == 20 && slateback.bondRank == 1,
                "Slateback XP/rank was not derived from its source ledger.", failures);
            Check(slateback != null && string.IsNullOrEmpty(slateback.unfoldedFormId),
                "A foreign or rank-locked form survived normalization.", failures);

            var firstJson = JsonUtility.ToJson(data);
            InvokeNormalize(normalize, data, failures);
            var secondJson = JsonUtility.ToJson(data);
            Check(firstJson == secondJson,
                "Pact save normalization is not idempotent.", failures);
            Check(ReferenceEquals(listReference, data.pactProgress),
                "Repeated normalization replaced the Pact progress list.", failures);
        }

        private static void VerifyIdempotentFormApplication(List<string> failures)
        {
            foreach (var pact in TetherfallPactCatalog.All)
            {
                foreach (var form in pact.greaterForms)
                {
                    var root = new GameObject($"Verifier {pact.creatureName}");
                    root.transform.localScale = pact.rideReady
                        ? new Vector3(1.55f, 1.3f, 2.05f)
                        : new Vector3(1.15f, 0.88f, 1.35f);
                    try
                    {
                        var first = TetherfallPactFormArt.Apply(null, root.transform, form);
                        var firstRendererCount =
                            root.GetComponentsInChildren<Renderer>(true).Length;
                        var second = TetherfallPactFormArt.Apply(null, root.transform, form);
                        var secondRendererCount =
                            root.GetComponentsInChildren<Renderer>(true).Length;

                        Check(first != null,
                            $"Form art '{form.id}' failed to build.", failures);
                        Check(ReferenceEquals(first, second),
                            $"Form art '{form.id}' created a second root on reapplication.",
                            failures);
                        Check(firstRendererCount > 0 &&
                              firstRendererCount == secondRendererCount,
                            $"Form art '{form.id}' changed renderer count when reapplied.",
                            failures);
                        Check(
                            root.GetComponentsInChildren<Collider>(true).Length == 0,
                            $"Form art '{form.id}' introduced gameplay colliders.",
                            failures);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                    }
                }
            }
        }

        private static void VerifyInvalidData(List<string> failures)
        {
            var locked = new PactProgressSaveData
            {
                pactId = TetherfallPactCatalog.CinderwingId,
                bondXp = int.MaxValue,
                bondRank = 99,
                unfoldedFormId = TetherfallPactCatalog.SuncrestFormId,
                awardedSourceIds = new List<string>
                {
                    PactBondSourceIds.PactFormed,
                    null,
                    string.Empty,
                    "source.encounter.",
                    "malformed"
                }
            };
            PactProgressionRules.NormalizeRecord(locked);
            Check(locked.bondXp == 20 && locked.bondRank == 1,
                "Invalid record scalars were trusted instead of source-derived.", failures);
            Check(string.IsNullOrEmpty(locked.unfoldedFormId),
                "Rank-locked form survived invalid-data normalization.", failures);
            Check(locked.awardedSourceIds.Count == 1,
                "Invalid source IDs survived normalization.", failures);

            var unknown = new PactProgressSaveData
            {
                pactId = "pact.not-authored",
                bondXp = 400,
                bondRank = 4,
                unfoldedFormId = "form.not-authored"
            };
            PactProgressionRules.NormalizeRecord(unknown);
            Check(string.IsNullOrEmpty(unknown.pactId) &&
                  unknown.bondXp == 0 &&
                  unknown.bondRank == 0,
                "Unknown Pact data was not neutralized.", failures);
            Check(
                !PactProgressionRules.TryAwardSource(
                    locked,
                    "source.not-valid",
                    out _,
                    out _),
                "An invalid progression source was awardable.",
                failures);
        }

        private static PactProgressSaveData FindRecord(
            List<PactProgressSaveData> records,
            string pactId)
        {
            return records.Find(record =>
                record != null &&
                string.Equals(record.pactId, pactId, StringComparison.Ordinal));
        }

        private static void InvokeNormalize(
            MethodInfo normalize,
            TetherfallSaveData data,
            List<string> failures)
        {
            try
            {
                normalize.Invoke(null, new object[] { data });
            }
            catch (TargetInvocationException exception)
            {
                failures.Add(
                    "Save normalization threw: " +
                    (exception.InnerException?.Message ?? exception.Message));
            }
            catch (Exception exception)
            {
                failures.Add($"Save normalization threw: {exception.Message}");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Check(
            bool condition,
            string message,
            List<string> failures)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
