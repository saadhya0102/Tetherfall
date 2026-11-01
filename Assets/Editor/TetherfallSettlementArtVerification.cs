using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    /// <summary>
    /// Edit-mode contract verification for every stable settlement art ID and its forced
    /// no-Resources fallback.
    /// </summary>
    public static class TetherfallSettlementArtVerification
    {
        private const string MenuPath = "Tetherfall/Verify Settlement Art Pack";
        private const float MaximumVisualExtent = 64f;
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2187)]
        public static void Verify()
        {
            VerifyInternal(true);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Settlement art verification is edit-mode only.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            var fixture = new GameObject("Settlement Art Verification");
            var normalResourceCount = 0;
            var verifiedPieceCount = 0;

            try
            {
                VerifyCatalog(failures);

                foreach (var definition in TetherfallSettlementArtCatalog.All)
                {
                    var result = VerifyPiece(definition, fixture.transform, failures);
                    verifiedPieceCount++;
                    if (result.UsedPreferredResource)
                    {
                        normalResourceCount++;
                    }
                }

                VerifyBatchAndController(fixture.transform, failures);
                VerifyDocumentationAndSkill(failures);
            }
            finally
            {
                Object.DestroyImmediate(fixture);
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Settlement art verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    $"Settlement art verification passed: {assertionCount} assertions across " +
                    $"{verifiedPieceCount} stable art IDs; {normalResourceCount} normal builds used " +
                    "curated Quaternius source geometry, every ID passed forced missing-Resources " +
                    "fallback, structural/station/core budgets held, duplicate builds and cleanup " +
                    "were idempotent, and all visuals were finite, collider-free, light-free, and " +
                    "per-piece-update-free.");
            }
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            VerifyInternal(false);
        }

        private static void VerifyCatalog(List<string> failures)
        {
            var definitions = TetherfallSettlementArtCatalog.All;
            var enumValues = (TetherfallSettlementArtId[])Enum.GetValues(
                typeof(TetherfallSettlementArtId));
            Check(
                definitions.Count == enumValues.Length,
                $"Catalog contains {definitions.Count} definitions for {enumValues.Length} enum IDs.",
                failures);
            Check(
                definitions.Count == 28,
                $"First production settlement pack must contain 28 art IDs, found {definitions.Count}.",
                failures);

            var ids = new HashSet<TetherfallSettlementArtId>();
            var numericIds = new HashSet<int>();
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var sourceNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in TetherfallQuaterniusAssetCatalog.All)
            {
                sourceNames.Add(source.ResourceName);
            }

            foreach (var definition in definitions)
            {
                Check(
                    ids.Add(definition.ArtId),
                    $"Catalog repeats enum ID '{definition.ArtId}'.",
                    failures);
                Check(
                    numericIds.Add((int)definition.ArtId),
                    $"Catalog repeats numeric ID '{(int)definition.ArtId}'.",
                    failures);
                Check(
                    stableIds.Add(definition.StableId),
                    $"Catalog repeats stable ID '{definition.StableId}'.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.StableId) &&
                    definition.StableId.StartsWith("alderwake.", StringComparison.Ordinal) &&
                    definition.StableId.IndexOf(' ') < 0,
                    $"Stable ID '{definition.StableId}' is not a normalized Alderwake ID.",
                    failures);
                Check(
                    TetherfallSettlementArtCatalog.Get(definition.ArtId) == definition,
                    $"Enum lookup did not return the catalog instance for '{definition.StableId}'.",
                    failures);
                Check(
                    TetherfallSettlementArtCatalog.TryGet(definition.StableId, out var byString) &&
                    byString == definition,
                    $"String lookup failed for '{definition.StableId}'.",
                    failures);
                Check(
                    TetherfallSettlementArtCatalog.TryParse(
                        definition.StableId,
                        out var parsedId) &&
                    parsedId == definition.ArtId &&
                    string.Equals(
                        TetherfallSettlementArtCatalog.GetStableId(parsedId),
                        definition.StableId,
                        StringComparison.Ordinal),
                    $"Stable-ID round trip failed for '{definition.StableId}'.",
                    failures);
                Check(
                    IsFinite(definition.Footprint) &&
                    definition.Footprint.x > 0f &&
                    definition.Footprint.y > 0f &&
                    definition.Footprint.z > 0f &&
                    MaxAbs(definition.Footprint) <= 16f,
                    $"Catalog footprint for '{definition.StableId}' is invalid: " +
                    $"{definition.Footprint}.",
                    failures);
                VerifyDeclaredBudget(definition, failures);

                if (definition.HasPreferredResource)
                {
                    Check(
                        sourceNames.Contains(definition.PreferredResourceName),
                        $"'{definition.StableId}' references uncurated Quaternius resource " +
                        $"'{definition.PreferredResourceName}'.",
                        failures);
                    Check(
                        string.Equals(
                            definition.PreferredResourcePath,
                            TetherfallQuaterniusAssetCatalog.ResourcePrefix +
                            definition.PreferredResourceName,
                            StringComparison.Ordinal),
                        $"Preferred Resources path is unstable for '{definition.StableId}'.",
                        failures);
                }

                if (definition.HasWorkSocket)
                {
                    Check(
                        definition.WorkRadius >= 1.5f,
                        $"'{definition.StableId}' work radius is {definition.WorkRadius:0.##}m; " +
                        "Pact clearance requires at least 1.5m.",
                        failures);
                }
            }

            foreach (var enumValue in enumValues)
            {
                Check(
                    ids.Contains(enumValue),
                    $"Enum ID '{enumValue}' has no catalog definition.",
                    failures);
            }

            Check(
                !TetherfallSettlementArtCatalog.TryGet(
                    "alderwake.not_a_real_piece",
                    out _),
                "Unknown stable ID unexpectedly resolved.",
                failures);
            Check(
                !TetherfallSettlementArtCatalog.TryParse(null, out _),
                "Null stable ID unexpectedly parsed.",
                failures);
        }

        private static void VerifyDeclaredBudget(
            TetherfallSettlementArtDefinition definition,
            List<string> failures)
        {
            int expectedRenderers;
            int expectedMaterials;
            switch (definition.BudgetClass)
            {
                case TetherfallSettlementArtBudgetClass.Structural:
                    expectedRenderers = TetherfallSettlementArtCatalog.StructuralRendererBudget;
                    expectedMaterials = TetherfallSettlementArtCatalog.StructuralMaterialBudget;
                    break;
                case TetherfallSettlementArtBudgetClass.ProductionStation:
                    expectedRenderers = TetherfallSettlementArtCatalog.StationRendererBudget;
                    expectedMaterials = TetherfallSettlementArtCatalog.StationMaterialBudget;
                    break;
                case TetherfallSettlementArtBudgetClass.HeroCore:
                    expectedRenderers = TetherfallSettlementArtCatalog.HeroRendererBudget;
                    expectedMaterials = TetherfallSettlementArtCatalog.HeroMaterialBudget;
                    break;
                default:
                    Check(
                        false,
                        $"'{definition.StableId}' has unknown budget class " +
                        $"'{definition.BudgetClass}'.",
                        failures);
                    return;
            }

            Check(
                definition.MaxRendererCount == expectedRenderers &&
                definition.MaxSharedMaterialCount == expectedMaterials,
                $"'{definition.StableId}' declares {definition.MaxRendererCount} renderers and " +
                $"{definition.MaxSharedMaterialCount} materials; {definition.BudgetClass} " +
                $"requires {expectedRenderers}/{expectedMaterials}.",
                failures);
        }

        private static PieceResult VerifyPiece(
            TetherfallSettlementArtDefinition definition,
            Transform fixture,
            List<string> failures)
        {
            var parent = new GameObject("Piece fixture · " + definition.StableId).transform;
            parent.SetParent(fixture, false);
            GameObject normal = null;

            try
            {
                try
                {
                    normal = TetherfallSettlementArt.Build(definition.ArtId, parent);
                }
                catch (Exception exception)
                {
                    Check(
                        false,
                        $"Normal build for '{definition.StableId}' threw " +
                        $"{exception.GetType().Name}: {exception.Message}",
                        failures);
                    return default;
                }

                Check(normal != null, $"Build returned null for '{definition.StableId}'.", failures);
                if (normal == null)
                {
                    return default;
                }

                var normalPiece = normal.GetComponent<TetherfallSettlementArtPiece>();
                VerifyBuiltPiece(normal, normalPiece, definition, false, failures);
                var normalSnapshot = CaptureSnapshot(normal);

                var duplicate = TetherfallSettlementArt.Build(definition.ArtId, parent);
                Check(
                    ReferenceEquals(normal, duplicate),
                    $"Duplicate Build replaced the root for '{definition.StableId}'.",
                    failures);
                Check(
                    normalSnapshot.Equals(CaptureSnapshot(duplicate)),
                    $"Duplicate Build changed the hierarchy for '{definition.StableId}'.",
                    failures);
                Check(
                    CountDirectPieces(parent, definition.ArtId) == 1,
                    $"Duplicate Build created multiple roots for '{definition.StableId}'.",
                    failures);

                var rebuilt = TetherfallSettlementArt.Rebuild(definition.ArtId, parent);
                Check(
                    ReferenceEquals(normal, rebuilt),
                    $"Explicit Rebuild replaced the root for '{definition.StableId}'.",
                    failures);
                Check(
                    normalSnapshot.Equals(CaptureSnapshot(rebuilt)),
                    $"Explicit Rebuild changed deterministic counts for '{definition.StableId}'.",
                    failures);
                VerifyBuiltPiece(
                    rebuilt,
                    rebuilt.GetComponent<TetherfallSettlementArtPiece>(),
                    definition,
                    false,
                    failures);

                var usedResource = normalPiece != null && normalPiece.UsedPreferredResource;
                Check(
                    TetherfallSettlementArt.Clear(parent, definition.ArtId),
                    $"Cleanup did not remove '{definition.StableId}'.",
                    failures);
                Check(
                    CountDirectPieces(parent, definition.ArtId) == 0 &&
                    !TetherfallSettlementArt.Clear(parent, definition.ArtId),
                    $"Cleanup for '{definition.StableId}' was not idempotent.",
                    failures);

                GameObject fallback;
                try
                {
                    fallback = TetherfallSettlementArt.BuildProceduralFallback(
                        definition.ArtId,
                        parent);
                }
                catch (Exception exception)
                {
                    Check(
                        false,
                        $"Forced missing-Resources fallback for '{definition.StableId}' threw " +
                        $"{exception.GetType().Name}: {exception.Message}",
                        failures);
                    return new PieceResult(usedResource);
                }

                var fallbackPiece = fallback.GetComponent<TetherfallSettlementArtPiece>();
                VerifyBuiltPiece(fallback, fallbackPiece, definition, true, failures);
                Check(
                    fallbackPiece != null &&
                    !fallbackPiece.UsedPreferredResource &&
                    fallbackPiece.UsedProceduralFallback,
                    $"Forced fallback for '{definition.StableId}' still reports source-prefab use.",
                    failures);

                var fallbackSnapshot = CaptureSnapshot(fallback);
                var secondFallback = TetherfallSettlementArt.BuildProceduralFallback(
                    definition.ArtId,
                    parent);
                Check(
                    ReferenceEquals(fallback, secondFallback),
                    $"Repeated fallback replaced the root for '{definition.StableId}'.",
                    failures);
                Check(
                    fallbackSnapshot.Equals(CaptureSnapshot(secondFallback)),
                    $"Repeated fallback changed deterministic counts for '{definition.StableId}'.",
                    failures);

                Check(
                    TetherfallSettlementArt.Clear(parent, definition.StableId) &&
                    CountDirectPieces(parent, definition.ArtId) == 0,
                    $"Stable-string cleanup failed for '{definition.StableId}'.",
                    failures);
                return new PieceResult(usedResource);
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        private static void VerifyBuiltPiece(
            GameObject root,
            TetherfallSettlementArtPiece piece,
            TetherfallSettlementArtDefinition definition,
            bool requireFallback,
            List<string> failures)
        {
            var label = requireFallback
                ? definition.StableId + " fallback"
                : definition.StableId + " normal build";

            Check(piece != null, $"{label} has no settlement metadata.", failures);
            if (piece == null)
            {
                return;
            }

            Check(
                piece.ArtId == definition.ArtId &&
                string.Equals(piece.StableId, definition.StableId, StringComparison.Ordinal),
                $"{label} metadata does not match its catalog definition.",
                failures);
            Check(
                root.transform.parent != null &&
                root.transform.localPosition == Vector3.zero &&
                Quaternion.Angle(root.transform.localRotation, Quaternion.identity) < 0.001f &&
                Vector3.SqrMagnitude(root.transform.localScale - Vector3.one) < 0.000001f,
                $"{label} root is not at the supplied parent's identity transform.",
                failures);
            Check(
                root.transform.Find(TetherfallSettlementArt.VisualRootName) != null,
                $"{label} has no dedicated visual-art group.",
                failures);

            VerifyFiniteHierarchy(root.transform, label, failures);
            VerifyForbiddenComponents(root, label, failures);
            VerifyNoPerPieceLoops(root, label, failures);
            VerifySockets(root.transform, definition, label, failures);

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var materials = new HashSet<Material>();
            Check(
                renderers.Length > 0 &&
                renderers.Length <= definition.MaxRendererCount &&
                renderers.Length == piece.RendererCount,
                $"{label} has {renderers.Length} renderers (metadata {piece.RendererCount}); " +
                $"budget is {definition.MaxRendererCount}.",
                failures);

            foreach (var visualRenderer in renderers)
            {
                Check(
                    visualRenderer != null &&
                    visualRenderer.enabled &&
                    IsFinite(visualRenderer.bounds) &&
                    visualRenderer.bounds.size.sqrMagnitude > 0.000001f &&
                    MaxAbs(visualRenderer.bounds.size) <= MaximumVisualExtent,
                    $"{label} has an invalid, disabled, empty, or unbounded renderer at " +
                    $"'{GetHierarchyPath(visualRenderer.transform)}'.",
                    failures);
                if (visualRenderer == null)
                {
                    continue;
                }

                Check(
                    visualRenderer.sharedMaterials.Length > 0,
                    $"{label} renderer '{GetHierarchyPath(visualRenderer.transform)}' has no " +
                    "shared material slots.",
                    failures);
                foreach (var material in visualRenderer.sharedMaterials)
                {
                    Check(
                        material != null,
                        $"{label} has a missing shared material at " +
                        $"'{GetHierarchyPath(visualRenderer.transform)}'.",
                        failures);
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            Check(
                materials.Count > 0 &&
                materials.Count <= definition.MaxSharedMaterialCount &&
                materials.Count == piece.SharedMaterialCount,
                $"{label} uses {materials.Count} shared materials (metadata " +
                $"{piece.SharedMaterialCount}); budget is {definition.MaxSharedMaterialCount}.",
                failures);
            Check(
                IsFinite(piece.LocalVisualBounds) &&
                piece.LocalVisualBounds.size.sqrMagnitude > 0.000001f &&
                MaxAbs(piece.LocalVisualBounds.size) <= MaximumVisualExtent,
                $"{label} metadata contains invalid visual bounds {piece.LocalVisualBounds}.",
                failures);
            Check(
                string.Equals(piece.CullingGroupKey, "alderwake.threadheart", StringComparison.Ordinal) &&
                Mathf.Approximately(
                    piece.NearDistance,
                    TetherfallSettlementPresentationController.NearDistance) &&
                Mathf.Approximately(
                    piece.CullDistance,
                    TetherfallSettlementPresentationController.CullDistance),
                $"{label} does not expose the centralized culling metadata contract.",
                failures);

            if (requireFallback)
            {
                Check(
                    !piece.UsedPreferredResource,
                    $"{label} did not bypass Resources source art.",
                    failures);
            }
        }

        private static void VerifySockets(
            Transform root,
            TetherfallSettlementArtDefinition definition,
            string label,
            List<string> failures)
        {
            var sockets = root.Find(TetherfallSettlementArt.SocketRootName);
            if (!definition.HasSnapSockets && !definition.HasWorkSocket)
            {
                Check(sockets == null, $"{label} created unnecessary socket metadata.", failures);
                return;
            }

            Check(sockets != null, $"{label} is missing its socket group.", failures);
            if (sockets == null)
            {
                return;
            }

            if (definition.HasSnapSockets)
            {
                foreach (var socketName in new[]
                         {
                             "Snap_N", "Snap_E", "Snap_S", "Snap_W", "Snap_Up", "Snap_Down"
                         })
                {
                    Check(
                        sockets.Find(socketName) != null,
                        $"{label} is missing snap socket '{socketName}'.",
                        failures);
                }
            }

            if (definition.HasWorkSocket)
            {
                var work = sockets.Find("Socket_Work");
                Check(work != null, $"{label} is missing Socket_Work.", failures);
                Check(
                    sockets.Find("Socket_VFX") != null,
                    $"{label} is missing Socket_VFX.",
                    failures);
                if (work != null)
                {
                    var horizontal = work.localPosition;
                    horizontal.y = 0f;
                    Check(
                        horizontal.magnitude >= definition.Footprint.z * 0.5f +
                        definition.WorkRadius - 0.01f,
                        $"{label} work socket does not preserve its {definition.WorkRadius:0.##}m " +
                        "clear work radius.",
                        failures);
                }
            }
        }

        private static void VerifyForbiddenComponents(
            GameObject root,
            string label,
            List<string> failures)
        {
            Check(
                root.GetComponentsInChildren<Collider>(true).Length == 0,
                $"{label} contains a gameplay Collider.",
                failures);
            Check(
                root.GetComponentsInChildren<Collider2D>(true).Length == 0,
                $"{label} contains a Collider2D.",
                failures);
            Check(
                root.GetComponentsInChildren<Light>(true).Length == 0,
                $"{label} contains a per-piece Light.",
                failures);
            Check(
                root.GetComponentsInChildren<Camera>(true).Length == 0,
                $"{label} contains a Camera.",
                failures);
            Check(
                root.GetComponentsInChildren<AudioListener>(true).Length == 0,
                $"{label} contains an AudioListener.",
                failures);
            Check(
                root.GetComponentsInChildren<Animator>(true).Length == 0 &&
                root.GetComponentsInChildren<Animation>(true).Length == 0,
                $"{label} contains an animation driver.",
                failures);
            Check(
                root.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                root.GetComponentsInChildren<Rigidbody2D>(true).Length == 0,
                $"{label} contains gameplay physics bodies.",
                failures);
            Check(
                root.GetComponentsInChildren<ParticleSystem>(true).Length == 0,
                $"{label} contains an unbudgeted particle system.",
                failures);

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Check(
                    behaviour == null || behaviour is TetherfallSettlementArtPiece,
                    $"{label} contains unexpected visual script " +
                    $"'{(behaviour == null ? "MissingScript" : behaviour.GetType().FullName)}'.",
                    failures);
            }
        }

        private static void VerifyNoPerPieceLoops(
            GameObject root,
            string label,
            List<string> failures)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                VerifyNoFrameLoopMethods(behaviour, label, failures);
            }
        }

        private static void VerifyNoFrameLoopMethods(
            MonoBehaviour behaviour,
            string label,
            List<string> failures)
        {
            var type = behaviour.GetType();
            foreach (var methodName in new[] { "Update", "LateUpdate", "FixedUpdate" })
            {
                var method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                Check(
                    method == null,
                    $"{label} component '{type.FullName}' declares per-frame {methodName}.",
                    failures);
            }
        }

        private static void VerifyFiniteHierarchy(
            Transform root,
            string label,
            List<string> failures)
        {
            foreach (var value in root.GetComponentsInChildren<Transform>(true))
            {
                Check(
                    IsFinite(value.localPosition) &&
                    IsFinite(value.localRotation) &&
                    IsFinite(value.localScale) &&
                    MaxAbs(value.localPosition) <= MaximumVisualExtent &&
                    MaxAbs(value.localScale) <= MaximumVisualExtent &&
                    Mathf.Min(
                        Mathf.Abs(value.localScale.x),
                        Mathf.Abs(value.localScale.y),
                        Mathf.Abs(value.localScale.z)) > 0.0001f,
                    $"{label} has an invalid transform at '{GetHierarchyPath(value)}'.",
                    failures);
            }
        }

        private static void VerifyBatchAndController(
            Transform fixture,
            List<string> failures)
        {
            var batch = new GameObject("Full Threadheart Settlement Art Batch").transform;
            batch.SetParent(fixture, false);
            var expectedRendererCount = 0;
            var positionIndex = 0;

            foreach (var definition in TetherfallSettlementArtCatalog.All)
            {
                var root = TetherfallSettlementArt.Build(definition.ArtId, batch);
                root.transform.localPosition = new Vector3(
                    positionIndex % 7 * 9f,
                    0f,
                    positionIndex / 7 * 9f);
                expectedRendererCount += root.GetComponentsInChildren<Renderer>(true).Length;
                positionIndex++;
            }

            var controller =
                TetherfallSettlementArt.EnsurePresentationController(batch, null);
            Check(controller != null, "Settlement batch did not create a presentation controller.", failures);
            if (controller != null)
            {
                Check(
                    controller.RegisteredPieceCount == TetherfallSettlementArtCatalog.All.Count,
                    $"Controller registered {controller.RegisteredPieceCount} pieces instead of " +
                    $"{TetherfallSettlementArtCatalog.All.Count}.",
                    failures);
                Check(
                    controller.RegisteredRendererCount == expectedRendererCount,
                    $"Controller registered {controller.RegisteredRendererCount} renderers instead " +
                    $"of {expectedRendererCount}.",
                    failures);
                Check(
                    !controller.IsCullingActive && controller.TargetCamera == null,
                    "Controller without a camera should retain visible culling-ready groups.",
                    failures);
                VerifyNoFrameLoopMethods(controller, "settlement presentation controller", failures);

                var duplicate =
                    TetherfallSettlementArt.EnsurePresentationController(batch, null);
                Check(
                    ReferenceEquals(controller, duplicate) &&
                    batch.GetComponents<TetherfallSettlementPresentationController>().Length == 1,
                    "Repeated controller setup created another presentation driver.",
                    failures);
            }

            Check(
                Mathf.Approximately(TetherfallSettlementPresentationController.NearDistance, 70f) &&
                Mathf.Approximately(TetherfallSettlementPresentationController.CullDistance, 110f),
                "Settlement culling distances must retain the documented 70m/110m bands.",
                failures);
            Check(
                TetherfallSettlementArt.CachedMaterialCount > 0 &&
                TetherfallSettlementArt.CachedMaterialCount <= 8,
                $"Settlement authored-material cache contains " +
                $"{TetherfallSettlementArt.CachedMaterialCount} materials; expected one shared " +
                "eight-material-or-smaller palette.",
                failures);
            VerifySharedAuthoredMaterialInstances(batch, failures);

            Check(
                TetherfallSettlementArt.ClearAll(batch) ==
                TetherfallSettlementArtCatalog.All.Count,
                "Batch cleanup did not report every settlement art root.",
                failures);
            Check(
                TetherfallSettlementArt.ClearAll(batch) == 0 &&
                batch.GetComponentsInChildren<TetherfallSettlementArtPiece>(true).Length == 0,
                "Repeated batch cleanup was not idempotent.",
                failures);
        }

        private static void VerifySharedAuthoredMaterialInstances(
            Transform root,
            List<string> failures)
        {
            var byName = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var visualRenderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in visualRenderer.sharedMaterials)
                {
                    if (material == null ||
                        !material.name.StartsWith(
                            "Tetherfall Settlement · ",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (byName.TryGetValue(material.name, out var first))
                    {
                        Check(
                            ReferenceEquals(first, material),
                            $"Authored material '{material.name}' was instantiated per piece.",
                            failures);
                    }
                    else
                    {
                        byName.Add(material.name, material);
                    }
                }
            }

            Check(
                byName.Count == TetherfallSettlementArt.CachedMaterialCount,
                $"Batch references {byName.Count} authored shared materials, while the cache " +
                $"reports {TetherfallSettlementArt.CachedMaterialCount}.",
                failures);
        }

        private static void VerifyDocumentationAndSkill(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var skillRoot = Path.Combine(
                projectRoot,
                ".cursor",
                "skills",
                "stylized-3d-assets");

            VerifyDocument(
                Path.Combine(Application.dataPath, "Art", "ART_DIRECTION.md"),
                new[]
                {
                    "Alderwake Threadheart settlement kit",
                    "28 stable art IDs",
                    "70 m",
                    "110 m",
                    "zero gameplay colliders"
                },
                failures);
            VerifyDocument(
                Path.Combine(Application.dataPath, "Art", "ART_PROVENANCE.md"),
                new[]
                {
                    "TetherfallSettlementArt",
                    "existing Quaternius",
                    "no new external",
                    "28 stable"
                },
                failures);
            VerifyDocument(
                Path.Combine(skillRoot, "SKILL.md"),
                new[]
                {
                    "reference-import.md",
                    "provenance-template.md",
                    "examples.md",
                    "scripts/"
                },
                failures);
            VerifyDocument(
                Path.Combine(skillRoot, "settlement-kit.md"),
                new[]
                {
                    "TetherfallSettlementArtId",
                    "Verify Settlement Art Pack",
                    "28",
                    "110 metres"
                },
                failures);
            VerifyDocument(
                Path.Combine(skillRoot, "reference-import.md"),
                new[] { "official", "license", "checksum", "FBX" },
                failures);
            VerifyDocument(
                Path.Combine(skillRoot, "provenance-template.md"),
                new[] { "Creator", "Official URL", "SHA-256", "Retrieval date" },
                failures);
            VerifyDocument(
                Path.Combine(skillRoot, "examples.md"),
                new[] { "TetherfallSettlementArt.Build", "Clear", "RebuildGroups" },
                failures);

            foreach (var scriptName in new[]
                     {
                         "validate_fbx.py",
                         "validate_source_manifest.py",
                         "validate_provenance.py"
                     })
            {
                var scriptPath = Path.Combine(skillRoot, "scripts", scriptName);
                Check(
                    File.Exists(scriptPath),
                    $"Settlement asset skill validation script is missing: '{scriptPath}'.",
                    failures);
                if (File.Exists(scriptPath))
                {
                    var text = File.ReadAllText(scriptPath);
                    Check(
                        text.StartsWith("#!/usr/bin/env python3", StringComparison.Ordinal) &&
                        text.IndexOf("argparse", StringComparison.Ordinal) >= 0 &&
                        text.IndexOf("def main(", StringComparison.Ordinal) >= 0,
                        $"Validation script '{scriptPath}' is not a dependency-free Python CLI.",
                        failures);
                }
            }
        }

        private static void VerifyDocument(
            string path,
            IReadOnlyList<string> requiredText,
            List<string> failures)
        {
            Check(File.Exists(path), $"Required documentation is missing: '{path}'.", failures);
            if (!File.Exists(path))
            {
                return;
            }

            var text = File.ReadAllText(path);
            foreach (var required in requiredText)
            {
                Check(
                    text.IndexOf(required, StringComparison.OrdinalIgnoreCase) >= 0,
                    $"Documentation '{path}' must mention '{required}'.",
                    failures);
            }
        }

        private static int CountDirectPieces(
            Transform parent,
            TetherfallSettlementArtId artId)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index++)
            {
                var piece = parent.GetChild(index).GetComponent<TetherfallSettlementArtPiece>();
                if (piece != null && piece.ArtId == artId)
                {
                    count++;
                }
            }

            return count;
        }

        private static PieceSnapshot CaptureSnapshot(GameObject root)
        {
            var materials = new HashSet<Material>();
            foreach (var visualRenderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in visualRenderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            return new PieceSnapshot(
                root.GetComponentsInChildren<Transform>(true).Length,
                root.GetComponentsInChildren<Renderer>(true).Length,
                materials.Count);
        }

        private static string GetHierarchyPath(Transform value)
        {
            var path = value.name;
            while (value.parent != null)
            {
                value = value.parent;
                path = value.name + "/" + path;
            }

            return path;
        }

        private static float MaxAbs(Vector3 value)
        {
            return Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static bool IsFinite(Bounds value)
        {
            return IsFinite(value.center) && IsFinite(value.size);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Check(
            bool condition,
            string failure,
            List<string> failures)
        {
            assertionCount++;
            if (!condition && !failures.Contains(failure))
            {
                failures.Add(failure);
            }
        }

        private readonly struct PieceResult
        {
            public bool UsedPreferredResource { get; }

            public PieceResult(bool usedPreferredResource)
            {
                UsedPreferredResource = usedPreferredResource;
            }
        }

        private readonly struct PieceSnapshot : IEquatable<PieceSnapshot>
        {
            private readonly int transformCount;
            private readonly int rendererCount;
            private readonly int materialCount;

            public PieceSnapshot(
                int transformCount,
                int rendererCount,
                int materialCount)
            {
                this.transformCount = transformCount;
                this.rendererCount = rendererCount;
                this.materialCount = materialCount;
            }

            public bool Equals(PieceSnapshot other)
            {
                return transformCount == other.transformCount &&
                       rendererCount == other.rendererCount &&
                       materialCount == other.materialCount;
            }

            public override bool Equals(object value)
            {
                return value is PieceSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = transformCount;
                    hashCode = (hashCode * 397) ^ rendererCount;
                    hashCode = (hashCode * 397) ^ materialCount;
                    return hashCode;
                }
            }
        }
    }
}
