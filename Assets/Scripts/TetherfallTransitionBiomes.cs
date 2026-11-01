using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum TransitionBiomeBlendMode
    {
        AlongSegment,
        AcrossSegment
    }

    public enum TransitionBiomeLandmarkStyle
    {
        ThreadglassBraid,
        CopperRootGate,
        WindglassFork,
        TideglassNeedle
    }

    public readonly struct TransitionResourceSpec
    {
        public string Id { get; }
        public ResourceKind Kind { get; }
        public int Amount { get; }
        public Color Color { get; }

        public TransitionResourceSpec(string id, ResourceKind kind, int amount, Color color)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Amount = Mathf.Max(1, amount);
            Color = color;
        }
    }

    public readonly struct TransitionBiomeBudgetEstimate
    {
        public int ShellRenderers { get; }
        public int LandmarkDressingRenderers { get; }
        public int SolidRouteColliders { get; }
        public int ResourceInteractables { get; }
        public int Camps { get; }
        public int Secrets { get; }
        public int AddedLights { get; }

        public TransitionBiomeBudgetEstimate(
            int shellRenderers,
            int landmarkDressingRenderers,
            int solidRouteColliders,
            int resourceInteractables,
            int camps,
            int secrets,
            int addedLights)
        {
            ShellRenderers = shellRenderers;
            LandmarkDressingRenderers = landmarkDressingRenderers;
            SolidRouteColliders = solidRouteColliders;
            ResourceInteractables = resourceInteractables;
            Camps = camps;
            Secrets = secrets;
            AddedLights = addedLights;
        }
    }

    public sealed class TransitionBiomeDefinition
    {
        private readonly TransitionResourceSpec[] resources;

        public string Id { get; }
        public string AmbientLocaleName { get; }
        public string DiscoveryText { get; }
        public string ParentAProgressionRegion { get; }
        public string ParentBProgressionRegion { get; }
        public string ParentAPresentationRegion { get; }
        public string ParentBPresentationRegion { get; }
        public string ConnectorRegionId { get; }
        public Vector3 SegmentStart { get; }
        public Vector3 SegmentEnd { get; }
        public TransitionBiomeBlendMode BlendMode { get; }
        public Vector3 ParentBSide { get; }
        public float CorridorHalfWidth { get; }
        public float SoftFalloff { get; }
        public float CoreHalfWidth { get; }
        public Color MapTint { get; }
        public TransitionBiomeLandmarkStyle LandmarkStyle { get; }
        public float ContentSide { get; }
        public string EncounterDefinitionId { get; }
        public string EncounterPersistenceId { get; }
        public string SecretId { get; }
        public string SecretName { get; }
        public string SecretInscription { get; }
        public ResourceKind SecretReward { get; }
        public int SecretRewardAmount { get; }

        public int ResourceCount => resources.Length;

        public TransitionBiomeDefinition(
            string id,
            string ambientLocaleName,
            string discoveryText,
            string parentAProgressionRegion,
            string parentBProgressionRegion,
            string parentAPresentationRegion,
            string parentBPresentationRegion,
            string connectorRegionId,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            TransitionBiomeBlendMode blendMode,
            Vector3 parentBSide,
            float corridorHalfWidth,
            float softFalloff,
            float coreHalfWidth,
            Color mapTint,
            TransitionBiomeLandmarkStyle landmarkStyle,
            float contentSide,
            TransitionResourceSpec[] resourceSpecs,
            string encounterDefinitionId,
            string encounterPersistenceId,
            string secretId,
            string secretName,
            string secretInscription,
            ResourceKind secretReward,
            int secretRewardAmount)
        {
            Id = id ?? string.Empty;
            AmbientLocaleName = ambientLocaleName ?? string.Empty;
            DiscoveryText = discoveryText ?? string.Empty;
            ParentAProgressionRegion = parentAProgressionRegion ?? string.Empty;
            ParentBProgressionRegion = parentBProgressionRegion ?? string.Empty;
            ParentAPresentationRegion = parentAPresentationRegion ?? ParentAProgressionRegion;
            ParentBPresentationRegion = parentBPresentationRegion ?? ParentBProgressionRegion;
            ConnectorRegionId = connectorRegionId ?? string.Empty;
            SegmentStart = segmentStart;
            SegmentEnd = segmentEnd;
            BlendMode = blendMode;
            ParentBSide = parentBSide;
            CorridorHalfWidth = Mathf.Max(0.1f, corridorHalfWidth);
            SoftFalloff = Mathf.Max(0f, softFalloff);
            CoreHalfWidth = Mathf.Clamp(coreHalfWidth, 0.1f, CorridorHalfWidth);
            MapTint = mapTint;
            LandmarkStyle = landmarkStyle;
            ContentSide = contentSide < 0f ? -1f : 1f;
            EncounterDefinitionId = encounterDefinitionId ?? string.Empty;
            EncounterPersistenceId = encounterPersistenceId ?? string.Empty;
            SecretId = secretId ?? string.Empty;
            SecretName = secretName ?? string.Empty;
            SecretInscription = secretInscription ?? string.Empty;
            SecretReward = secretReward;
            SecretRewardAmount = Mathf.Max(1, secretRewardAmount);

            if (resourceSpecs == null || resourceSpecs.Length == 0)
            {
                resources = Array.Empty<TransitionResourceSpec>();
            }
            else
            {
                resources = new TransitionResourceSpec[resourceSpecs.Length];
                Array.Copy(resourceSpecs, resources, resourceSpecs.Length);
            }
        }

        public TransitionResourceSpec GetResource(int index)
        {
            if (index < 0 || index >= resources.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return resources[index];
        }
    }

    public readonly struct TransitionBiomeProjection
    {
        public float RawAlongT { get; }
        public float AlongT { get; }
        public float BlendT { get; }
        public float DistanceToSegment { get; }
        public float LateralDistance { get; }
        public float SignedCrossDistance { get; }
        public bool IsInsideInfluence { get; }
        public bool IsInSeamCore { get; }

        internal TransitionBiomeProjection(
            float rawAlongT,
            float alongT,
            float blendT,
            float distanceToSegment,
            float lateralDistance,
            float signedCrossDistance,
            bool isInsideInfluence,
            bool isInSeamCore)
        {
            RawAlongT = rawAlongT;
            AlongT = alongT;
            BlendT = blendT;
            DistanceToSegment = distanceToSegment;
            LateralDistance = lateralDistance;
            SignedCrossDistance = signedCrossDistance;
            IsInsideInfluence = isInsideInfluence;
            IsInSeamCore = isInSeamCore;
        }
    }

    public readonly struct RegionLocaleQuery
    {
        public string ProgressionRegion { get; }
        public string AmbientLocaleName { get; }
        public string TransitionId { get; }
        public float BlendT { get; }
        public bool InSeamCore { get; }

        public bool HasTransition => !string.IsNullOrEmpty(TransitionId);

        public RegionLocaleQuery(
            string progressionRegion,
            string ambientLocaleName,
            string transitionId,
            float blendT,
            bool inSeamCore)
        {
            ProgressionRegion = progressionRegion ?? string.Empty;
            AmbientLocaleName = ambientLocaleName ?? ProgressionRegion;
            TransitionId = transitionId ?? string.Empty;
            BlendT = Mathf.Clamp01(blendT);
            InSeamCore = inSeamCore;
        }

        public static RegionLocaleQuery ForProgressionRegion(string progressionRegion)
        {
            var stableRegion = progressionRegion ?? string.Empty;
            return new RegionLocaleQuery(stableRegion, stableRegion, string.Empty, 0f, false);
        }
    }

    public struct TransitionLocaleHysteresisState
    {
        private string activeTransitionId;

        public string ActiveTransitionId => activeTransitionId ?? string.Empty;
        public bool IsActive => !string.IsNullOrEmpty(activeTransitionId);

        public void Reset()
        {
            activeTransitionId = null;
        }

        internal void Activate(string transitionId)
        {
            activeTransitionId = transitionId;
        }
    }

    public readonly struct TransitionMapSeamBand
    {
        public string TransitionId { get; }
        public Vector2 Start { get; }
        public Vector2 End { get; }
        public float Thickness { get; }
        public Color Tint { get; }
        public int FeatherLayer { get; }

        public TransitionMapSeamBand(
            string transitionId,
            Vector2 start,
            Vector2 end,
            float thickness,
            Color tint,
            int featherLayer)
        {
            TransitionId = transitionId ?? string.Empty;
            Start = start;
            End = end;
            Thickness = Mathf.Max(0f, thickness);
            Tint = tint;
            FeatherLayer = featherLayer;
        }
    }

    public static class TetherfallTransitionBiomes
    {
        public const int V1SeamCount = 12;
        public const int ResourcesPerSeam = 4;
        public const int MaxLandmarkDressingRenderers = 8;
        public const int MaxSolidRouteColliders = 4;
        public const int MaxActiveStaticShells = 3;
        public const int MaxActiveDetailRoots = 2;
        public const int MapBandsPerSeam = 3;
        public const float ActivationRefreshSeconds = 0.25f;
        public const float LocaleExitPadding = 8f;
        public const float StaticPrewakeDistance = 92f;
        public const float StaticSleepDistance = 116f;
        public const float DetailWakeDistance = 52f;
        public const float DetailSleepDistance = 70f;

        private const string WorldRootName = "Transition Biome Seams";
        private const string TransitionPrefix = "transition.";
        private const string ResourcePrefix = "resource.transition.";
        private const string EncounterPrefix = "encounter.transition.";
        private const string SecretPrefix = "secret.transition.";

        private static readonly TransitionBiomeDefinition[] V1DefinitionsInternal =
            CreateV1Definitions();
        private static readonly IReadOnlyList<TransitionBiomeDefinition> V1View =
            Array.AsReadOnly(V1DefinitionsInternal);
        private static readonly List<TransitionBiomeDefinition> RegisteredAnnexDefinitions =
            new List<TransitionBiomeDefinition>();

        public static IReadOnlyList<TransitionBiomeDefinition> V1Definitions => V1View;
        public static int DefinitionCount =>
            V1DefinitionsInternal.Length + RegisteredAnnexDefinitions.Count;
        public static int RegisteredAnnexSeamCount => RegisteredAnnexDefinitions.Count;
        public static int MapSeamBandCount => DefinitionCount * MapBandsPerSeam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLocalRegistry()
        {
            RegisteredAnnexDefinitions.Clear();
        }

        public static TransitionBiomeDefinition GetDefinitionAt(int index)
        {
            if (index < 0)
            {
                return null;
            }

            if (index < V1DefinitionsInternal.Length)
            {
                return V1DefinitionsInternal[index];
            }

            var annexIndex = index - V1DefinitionsInternal.Length;
            return annexIndex >= 0 && annexIndex < RegisteredAnnexDefinitions.Count
                ? RegisteredAnnexDefinitions[annexIndex]
                : null;
        }

        public static TransitionBiomeDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var index = 0; index < V1DefinitionsInternal.Length; index++)
            {
                var definition = V1DefinitionsInternal[index];
                if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            for (var index = 0; index < RegisteredAnnexDefinitions.Count; index++)
            {
                var definition = RegisteredAnnexDefinitions[index];
                if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static bool IsTransitionLocaleName(string localeName)
        {
            if (string.IsNullOrEmpty(localeName))
            {
                return false;
            }

            for (var index = 0; index < DefinitionCount; index++)
            {
                var definition = GetDefinitionAt(index);
                if (definition != null &&
                    string.Equals(
                        definition.AmbientLocaleName,
                        localeName,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RegisterAdditionalSeam(TransitionBiomeDefinition definition)
        {
            if (!IsValidRegistration(definition) ||
                Get(definition.Id) != null ||
                HasRegistrationCollision(definition))
            {
                return false;
            }

            RegisteredAnnexDefinitions.Add(definition);
            return true;
        }

        public static bool RegisterAnnexConnectorSeam(
            string transitionId,
            string parentProgressionRegion,
            string parentPresentationRegion,
            string annexPresentationRegion,
            string ambientLocaleName,
            string discoveryText,
            Vector3 connectorStart,
            Vector3 connectorEnd,
            float connectorWidth,
            ResourceKind parentResource,
            ResourceKind annexResource,
            string encounterDefinitionId,
            Color mapTint,
            float contentSide = 1f)
        {
            var resources = CreateResourceSet(
                transitionId,
                parentResource,
                parentResource,
                annexResource,
                annexResource,
                mapTint);
            var definition = new TransitionBiomeDefinition(
                transitionId,
                ambientLocaleName,
                discoveryText,
                parentProgressionRegion,
                parentProgressionRegion,
                parentPresentationRegion,
                annexPresentationRegion,
                string.Empty,
                connectorStart,
                connectorEnd,
                TransitionBiomeBlendMode.AlongSegment,
                Vector3.zero,
                Mathf.Max(4f, connectorWidth * 0.5f),
                10f,
                Mathf.Max(2.5f, connectorWidth * 0.32f),
                mapTint,
                TransitionBiomeLandmarkStyle.ThreadglassBraid,
                contentSide,
                resources,
                encounterDefinitionId,
                "encounter." + transitionId + ".seam_patrol",
                "secret." + transitionId + ".threadglass_memory",
                ambientLocaleName + " Threadglass Memory",
                "A parent-route slate records the annex as another reach of the same region.",
                annexResource,
                2);
            return RegisterAdditionalSeam(definition);
        }

        public static bool TryProject(
            TransitionBiomeDefinition definition,
            Vector3 position,
            out TransitionBiomeProjection projection)
        {
            projection = default;
            if (definition == null ||
                !IsFinite(position) ||
                !IsFinite(definition.SegmentStart) ||
                !IsFinite(definition.SegmentEnd))
            {
                return false;
            }

            var deltaX = definition.SegmentEnd.x - definition.SegmentStart.x;
            var deltaZ = definition.SegmentEnd.z - definition.SegmentStart.z;
            var lengthSquared = deltaX * deltaX + deltaZ * deltaZ;
            if (lengthSquared <= 0.0001f || !IsFinite(lengthSquared))
            {
                return false;
            }

            var pointX = position.x - definition.SegmentStart.x;
            var pointZ = position.z - definition.SegmentStart.z;
            var rawAlongT = (pointX * deltaX + pointZ * deltaZ) / lengthSquared;
            var alongT = Mathf.Clamp01(rawAlongT);
            var closestX = definition.SegmentStart.x + deltaX * alongT;
            var closestZ = definition.SegmentStart.z + deltaZ * alongT;
            var distanceX = position.x - closestX;
            var distanceZ = position.z - closestZ;
            var distanceToSegment = Mathf.Sqrt(
                distanceX * distanceX + distanceZ * distanceZ);

            var infiniteX = definition.SegmentStart.x + deltaX * rawAlongT;
            var infiniteZ = definition.SegmentStart.z + deltaZ * rawAlongT;
            var lateralX = position.x - infiniteX;
            var lateralZ = position.z - infiniteZ;
            var lateralDistance = Mathf.Sqrt(
                lateralX * lateralX + lateralZ * lateralZ);

            var signedCrossDistance = 0f;
            float blendT;
            bool isInCore;
            if (definition.BlendMode == TransitionBiomeBlendMode.AcrossSegment)
            {
                var sideX = definition.ParentBSide.x;
                var sideZ = definition.ParentBSide.z;
                var sideLength = Mathf.Sqrt(sideX * sideX + sideZ * sideZ);
                if (sideLength <= 0.0001f || !IsFinite(sideLength))
                {
                    return false;
                }

                sideX /= sideLength;
                sideZ /= sideLength;
                signedCrossDistance = lateralX * sideX + lateralZ * sideZ;
                var linearBlend = Mathf.InverseLerp(
                    -definition.CorridorHalfWidth,
                    definition.CorridorHalfWidth,
                    signedCrossDistance);
                blendT = Smooth01(linearBlend);
                isInCore =
                    rawAlongT >= 0f &&
                    rawAlongT <= 1f &&
                    Mathf.Abs(signedCrossDistance) <= definition.CoreHalfWidth;
            }
            else
            {
                blendT = Smooth01(alongT);
                isInCore =
                    rawAlongT >= 0.2f &&
                    rawAlongT <= 0.8f &&
                    lateralDistance <= definition.CoreHalfWidth;
            }

            var influenceRadius =
                definition.CorridorHalfWidth + definition.SoftFalloff;
            var isInsideInfluence = distanceToSegment <= influenceRadius;
            if (!IsFinite(rawAlongT) ||
                !IsFinite(blendT) ||
                !IsFinite(distanceToSegment) ||
                !IsFinite(lateralDistance) ||
                !IsFinite(signedCrossDistance))
            {
                return false;
            }

            projection = new TransitionBiomeProjection(
                rawAlongT,
                alongT,
                blendT,
                distanceToSegment,
                lateralDistance,
                signedCrossDistance,
                isInsideInfluence,
                isInCore);
            return true;
        }

        public static bool TryGetLocale(
            Vector3 position,
            string resolvedProgressionRegion,
            ref TransitionLocaleHysteresisState state,
            out RegionLocaleQuery locale)
        {
            if (state.IsActive)
            {
                var heldDefinition = Get(state.ActiveTransitionId);
                if (heldDefinition != null &&
                    TryProject(heldDefinition, position, out var heldProjection) &&
                    IsInsideLocaleExitEnvelope(heldDefinition, heldProjection))
                {
                    locale = CreateLocale(
                        heldDefinition,
                        heldProjection,
                        resolvedProgressionRegion,
                        true);
                    return true;
                }

                state.Reset();
            }

            TransitionBiomeDefinition bestDefinition = null;
            var bestProjection = default(TransitionBiomeProjection);
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < DefinitionCount; index++)
            {
                var definition = GetDefinitionAt(index);
                if (!TryProject(definition, position, out var candidate) ||
                    !candidate.IsInsideInfluence)
                {
                    continue;
                }

                var score =
                    candidate.DistanceToSegment * candidate.DistanceToSegment +
                    Mathf.Abs(candidate.BlendT - 0.5f) * 0.01f;
                if (candidate.IsInSeamCore)
                {
                    score -= 100000f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestDefinition = definition;
                    bestProjection = candidate;
                }
            }

            if (bestDefinition == null)
            {
                locale = RegionLocaleQuery.ForProgressionRegion(
                    SanitizeBaseProgressionRegion(resolvedProgressionRegion));
                return false;
            }

            if (bestProjection.IsInSeamCore)
            {
                state.Activate(bestDefinition.Id);
            }

            locale = CreateLocale(
                bestDefinition,
                bestProjection,
                resolvedProgressionRegion,
                state.IsActive &&
                string.Equals(
                    state.ActiveTransitionId,
                    bestDefinition.Id,
                    StringComparison.Ordinal));
            return true;
        }

        public static bool TryGetLocale(
            Vector3 position,
            string resolvedProgressionRegion,
            out RegionLocaleQuery locale)
        {
            var state = default(TransitionLocaleHysteresisState);
            return TryGetLocale(position, resolvedProgressionRegion, ref state, out locale);
        }

        public static bool TryGetBlendedPresentation(
            RegionLocaleQuery locale,
            out RegionPresentation.Definition presentation,
            out float fogDensity)
        {
            var definition = Get(locale.TransitionId);
            if (definition == null)
            {
                presentation = default;
                fogDensity = 0f;
                return false;
            }

            var parentA = RegionPresentation.Get(definition.ParentAPresentationRegion);
            var parentB = RegionPresentation.Get(definition.ParentBPresentationRegion);
            var blendT = Mathf.Clamp01(locale.BlendT);
            presentation = new RegionPresentation.Definition(
                Color.Lerp(parentA.ambient, parentB.ambient, blendT),
                Color.Lerp(parentA.fog, parentB.fog, blendT),
                Color.Lerp(parentA.sun, parentB.sun, blendT),
                Color.Lerp(parentA.sky, parentB.sky, blendT),
                Color.Lerp(parentA.ground, parentB.ground, blendT),
                definition.DiscoveryText);
            fogDensity = Mathf.Lerp(
                GetFogDensity(definition.ParentAPresentationRegion),
                GetFogDensity(definition.ParentBPresentationRegion),
                blendT);
            return IsFinite(fogDensity);
        }

        public static bool TryGetMapSeamBand(
            int bandIndex,
            Rect mapRect,
            out TransitionMapSeamBand band)
        {
            band = default;
            if (bandIndex < 0 || bandIndex >= MapSeamBandCount)
            {
                return false;
            }

            var definitionIndex = bandIndex / MapBandsPerSeam;
            var featherLayer = bandIndex % MapBandsPerSeam;
            var definition = GetDefinitionAt(definitionIndex);
            if (definition == null ||
                !TetherfallWorldMapRules.TryProjectWorldToMap(
                    definition.SegmentStart,
                    mapRect,
                    out var start) ||
                !TetherfallWorldMapRules.TryProjectWorldToMap(
                    definition.SegmentEnd,
                    mapRect,
                    out var end))
            {
                return false;
            }

            var worldWidth = Mathf.Max(
                0.01f,
                TetherfallWorldMapRules.MaximumWorldX -
                TetherfallWorldMapRules.MinimumWorldX);
            var worldDepth = Mathf.Max(
                0.01f,
                TetherfallWorldMapRules.MaximumWorldZ -
                TetherfallWorldMapRules.MinimumWorldZ);
            var pixelsPerWorldUnit =
                (mapRect.width / worldWidth + mapRect.height / worldDepth) * 0.5f;
            var baseThickness =
                (definition.CorridorHalfWidth + definition.SoftFalloff) *
                2f *
                pixelsPerWorldUnit;
            var thicknessFactor = featherLayer == 0
                ? 1.35f
                : featherLayer == 1
                    ? 0.92f
                    : 0.56f;
            var alpha = featherLayer == 0
                ? 0.07f
                : featherLayer == 1
                    ? 0.13f
                    : 0.24f;
            var tint = definition.MapTint;
            tint.a = alpha;
            band = new TransitionMapSeamBand(
                definition.Id,
                start,
                end,
                Mathf.Max(1f, baseThickness * thicknessFactor),
                tint,
                featherLayer);
            return IsFinite(start.x) &&
                   IsFinite(start.y) &&
                   IsFinite(end.x) &&
                   IsFinite(end.y);
        }

        public static TransitionBiomeBudgetEstimate EstimateBudget(
            TransitionBiomeDefinition definition)
        {
            if (definition == null)
            {
                return new TransitionBiomeBudgetEstimate(0, 0, 0, 0, 0, 0, 0);
            }

            return new TransitionBiomeBudgetEstimate(
                3,
                6,
                1,
                definition.ResourceCount,
                1,
                1,
                0);
        }

        public static TransitionBiomeDirector Build(
            TetherfallGame game,
            Transform parent)
        {
            if (game == null)
            {
                return null;
            }

            var rootParent = parent != null ? parent : game.transform;
            var existing = rootParent.Find(WorldRootName);
            var worldRoot = existing != null
                ? existing.gameObject
                : new GameObject(WorldRootName);
            if (existing == null)
            {
                worldRoot.transform.SetParent(rootParent, false);
            }

            var world = worldRoot.GetComponent<TransitionBiomeWorld>();
            if (world == null)
            {
                world = worldRoot.AddComponent<TransitionBiomeWorld>();
            }

            world.Initialize(game);
            world.SynchronizeCatalog();

            var director = worldRoot.GetComponent<TransitionBiomeDirector>();
            if (director == null)
            {
                director = worldRoot.AddComponent<TransitionBiomeDirector>();
            }

            director.Initialize(game, world.Runtimes);
            return director;
        }

        internal static TransitionBiomeRuntime BuildRuntime(
            TetherfallGame game,
            Transform worldRoot,
            TransitionBiomeDefinition definition)
        {
            var seamRoot = new GameObject(definition.AmbientLocaleName);
            seamRoot.transform.SetParent(worldRoot, false);

            var staticRoot = new GameObject("Streamed corridor shell");
            staticRoot.transform.SetParent(seamRoot.transform, false);
            var detailRoot = new GameObject("Streamed seam ecology");
            detailRoot.transform.SetParent(seamRoot.transform, false);

            BuildCorridorShell(game, definition, staticRoot.transform);
            BuildLandmarkDressing(game, definition, detailRoot.transform);
            BuildResources(game, definition, detailRoot.transform);
            BuildCamp(game, definition, detailRoot.transform);
            TransitionSecretInteractable.Create(game, definition, detailRoot.transform);

            staticRoot.SetActive(false);
            detailRoot.SetActive(false);
            return new TransitionBiomeRuntime(definition, staticRoot, detailRoot);
        }

        private static void BuildCorridorShell(
            TetherfallGame game,
            TransitionBiomeDefinition definition,
            Transform root)
        {
            var parentA = RegionPresentation.Get(definition.ParentAPresentationRegion);
            var parentB = RegionPresentation.Get(definition.ParentBPresentationRegion);
            var blendedGround = Color.Lerp(parentA.ground, parentB.ground, 0.5f);
            var groundMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                blendedGround,
                0.08f,
                0.34f);
            var shoulderColor = Color.Lerp(
                Color.Lerp(parentA.fog, parentB.fog, 0.5f),
                blendedGround,
                0.55f);
            shoulderColor.a = 0.2f;
            var shoulderMaterial = RuntimeEffectMaterialCache.GetTransparent(
                game,
                shoulderColor,
                0.28f);

            GetSegmentFrame(
                definition,
                out var midpoint,
                out var forward,
                out var right,
                out var length);
            CreateWorldPiece(
                root,
                "Soft blended route bed",
                PrimitiveType.Cube,
                midpoint + Vector3.down * 0.025f,
                new Vector3(
                    definition.CorridorHalfWidth * 2f,
                    0.09f,
                    length + 1.5f),
                forward,
                groundMaterial,
                true);

            var shoulderWidth = Mathf.Max(1.5f, definition.SoftFalloff);
            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                var offset =
                    definition.CorridorHalfWidth + shoulderWidth * 0.5f;
                CreateWorldPiece(
                    root,
                    sideIndex == 0
                        ? "Parent A soft shoulder"
                        : "Parent B soft shoulder",
                    PrimitiveType.Cube,
                    midpoint + right * side * offset + Vector3.down * 0.035f,
                    new Vector3(shoulderWidth, 0.055f, length + 1f),
                    forward,
                    shoulderMaterial,
                    false);
            }
        }

        private static void BuildLandmarkDressing(
            TetherfallGame game,
            TransitionBiomeDefinition definition,
            Transform root)
        {
            GetSegmentFrame(
                definition,
                out _,
                out var forward,
                out var right,
                out _);
            var anchor = Vector3.Lerp(
                definition.SegmentStart,
                definition.SegmentEnd,
                definition.BlendMode == TransitionBiomeBlendMode.AlongSegment
                    ? 0.56f
                    : 0.5f);
            anchor +=
                right *
                definition.ContentSide *
                Mathf.Max(2f, definition.CorridorHalfWidth - 1.6f);

            var landmark = new GameObject("Mixed-parent Threadglass waymark");
            landmark.transform.SetParent(root, false);
            landmark.transform.position = anchor;
            landmark.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            var parentA = RegionPresentation.Get(definition.ParentAPresentationRegion);
            var parentB = RegionPresentation.Get(definition.ParentBPresentationRegion);
            var parentAMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                Color.Lerp(parentA.ground, parentA.ambient, 0.36f),
                0.03f,
                0.3f);
            var parentBMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                Color.Lerp(parentB.ground, parentB.ambient, 0.36f),
                0.03f,
                0.3f);
            var copperMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.76f, 0.43f, 0.2f),
                0.42f,
                0.55f);
            var threadglassMaterial = RuntimeEffectMaterialCache.GetGlow(
                game,
                Color.Lerp(
                    new Color(0.27f, 0.84f, 0.76f),
                    definition.MapTint,
                    0.32f),
                0.88f,
                0.68f);

            var tallPrimitive =
                definition.LandmarkStyle == TransitionBiomeLandmarkStyle.CopperRootGate
                    ? PrimitiveType.Cylinder
                    : PrimitiveType.Cube;
            var crownPrimitive =
                definition.LandmarkStyle == TransitionBiomeLandmarkStyle.TideglassNeedle
                    ? PrimitiveType.Cube
                    : PrimitiveType.Sphere;
            CreateLocalPiece(
                landmark.transform,
                "Parent A ecology pier",
                tallPrimitive,
                new Vector3(-1.25f, 1.55f, 0f),
                new Vector3(0.42f, 3.1f, 0.42f),
                Quaternion.Euler(0f, 0f, -7f),
                parentAMaterial);
            CreateLocalPiece(
                landmark.transform,
                "Parent B ecology pier",
                tallPrimitive,
                new Vector3(1.25f, 1.72f, 0.1f),
                new Vector3(0.38f, 3.44f, 0.38f),
                Quaternion.Euler(0f, 0f, 8f),
                parentBMaterial);
            CreateLocalPiece(
                landmark.transform,
                "Copper shared-route yoke",
                PrimitiveType.Cube,
                new Vector3(0f, 2.74f, 0.02f),
                new Vector3(2.85f, 0.14f, 0.18f),
                Quaternion.Euler(0f, 0f, 2f),
                copperMaterial);
            CreateLocalPiece(
                landmark.transform,
                "Threadglass seam lens",
                crownPrimitive,
                new Vector3(0f, 2.91f, 0f),
                definition.LandmarkStyle == TransitionBiomeLandmarkStyle.TideglassNeedle
                    ? new Vector3(0.18f, 1.2f, 0.18f)
                    : new Vector3(0.5f, 0.23f, 0.5f),
                definition.LandmarkStyle == TransitionBiomeLandmarkStyle.WindglassFork
                    ? Quaternion.Euler(0f, 0f, 22f)
                    : Quaternion.identity,
                threadglassMaterial);
            CreateLocalPiece(
                landmark.transform,
                "Parent A low ecology",
                PrimitiveType.Sphere,
                new Vector3(-1.85f, 0.32f, 0.22f),
                new Vector3(1.15f, 0.54f, 0.9f),
                Quaternion.Euler(0f, 18f, 0f),
                parentAMaterial);
            CreateLocalPiece(
                landmark.transform,
                "Parent B low ecology",
                definition.LandmarkStyle == TransitionBiomeLandmarkStyle.WindglassFork
                    ? PrimitiveType.Cylinder
                    : PrimitiveType.Sphere,
                new Vector3(1.85f, 0.36f, -0.18f),
                new Vector3(1.08f, 0.62f, 0.86f),
                Quaternion.Euler(0f, -21f, 0f),
                parentBMaterial);
        }

        private static void BuildResources(
            TetherfallGame game,
            TransitionBiomeDefinition definition,
            Transform root)
        {
            GetSegmentFrame(
                definition,
                out _,
                out _,
                out var right,
                out _);
            for (var index = 0; index < definition.ResourceCount; index++)
            {
                var alongT = index == 0
                    ? 0.2f
                    : index == 1
                        ? 0.38f
                        : index == 2
                            ? 0.62f
                            : 0.8f;
                var side = index % 2 == 0 ? -1f : 1f;
                var edgeOffset = Mathf.Max(2f, definition.CorridorHalfWidth - 1.45f);
                var position =
                    Vector3.Lerp(
                        definition.SegmentStart,
                        definition.SegmentEnd,
                        alongT) +
                    right * side * edgeOffset +
                    Vector3.up * 0.62f;
                TransitionResourceInteractable.Create(
                    game,
                    definition.GetResource(index),
                    position,
                    root);
            }
        }

        private static void BuildCamp(
            TetherfallGame game,
            TransitionBiomeDefinition definition,
            Transform root)
        {
            if (EncounterCatalog.Get(definition.EncounterDefinitionId) == null)
            {
                Debug.LogError(
                    $"Transition seam '{definition.Id}' references missing encounter " +
                    $"'{definition.EncounterDefinitionId}'.");
                return;
            }

            GetSegmentFrame(
                definition,
                out _,
                out var forward,
                out var right,
                out _);
            Vector3 campPosition;
            if (definition.BlendMode == TransitionBiomeBlendMode.AlongSegment)
            {
                campPosition =
                    definition.SegmentStart -
                    forward * 12f +
                    right *
                    definition.ContentSide *
                    (definition.CorridorHalfWidth + 18f);
            }
            else
            {
                var parentBSide = HorizontalNormalized(definition.ParentBSide);
                campPosition =
                    Vector3.Lerp(
                        definition.SegmentStart,
                        definition.SegmentEnd,
                        0.24f) -
                    parentBSide * (definition.CorridorHalfWidth + 18f);
            }

            campPosition.y = 0f;
            var camp = EnemyCamp.Create(
                game,
                definition.EncounterDefinitionId,
                campPosition,
                definition.EncounterPersistenceId);
            camp.transform.SetParent(root, true);

            var lights = camp.GetComponentsInChildren<Light>(true);
            for (var index = 0; index < lights.Length; index++)
            {
                if (lights[index] == null)
                {
                    continue;
                }

                lights[index].enabled = false;
                UnityEngine.Object.Destroy(lights[index]);
            }
        }

        private static RegionLocaleQuery CreateLocale(
            TransitionBiomeDefinition definition,
            TransitionBiomeProjection projection,
            string resolvedProgressionRegion,
            bool useAmbientTransitionName)
        {
            var progressionRegion = ResolveParentProgressionRegion(
                definition,
                projection.BlendT,
                resolvedProgressionRegion);
            return new RegionLocaleQuery(
                progressionRegion,
                useAmbientTransitionName
                    ? definition.AmbientLocaleName
                    : progressionRegion,
                definition.Id,
                projection.BlendT,
                projection.IsInSeamCore);
        }

        private static string ResolveParentProgressionRegion(
            TransitionBiomeDefinition definition,
            float blendT,
            string resolvedProgressionRegion)
        {
            if (string.Equals(
                    resolvedProgressionRegion,
                    definition.ParentAProgressionRegion,
                    StringComparison.Ordinal) ||
                string.Equals(
                    resolvedProgressionRegion,
                    definition.ParentBProgressionRegion,
                    StringComparison.Ordinal))
            {
                return resolvedProgressionRegion;
            }

            if (string.Equals(
                    definition.ParentAProgressionRegion,
                    definition.ParentBProgressionRegion,
                    StringComparison.Ordinal))
            {
                return definition.ParentAProgressionRegion;
            }

            return blendT < 0.5f
                ? definition.ParentAProgressionRegion
                : definition.ParentBProgressionRegion;
        }

        private static bool IsInsideLocaleExitEnvelope(
            TransitionBiomeDefinition definition,
            TransitionBiomeProjection projection)
        {
            return projection.DistanceToSegment <=
                   definition.CoreHalfWidth +
                   LocaleExitPadding;
        }

        private static string SanitizeBaseProgressionRegion(string region)
        {
            return IsTransitionLocaleName(region) ? string.Empty : region ?? string.Empty;
        }

        private static float GetFogDensity(string region)
        {
            var fallback = string.Equals(region, "Morrowfen", StringComparison.Ordinal)
                ? 0.0052f
                : string.Equals(region, "Cinderwold Verge", StringComparison.Ordinal)
                    ? 0.0042f
                    : 0.0032f;
            return TetherfallRegionPacks.GetFogDensity(region, fallback);
        }

        private static bool IsValidRegistration(TransitionBiomeDefinition definition)
        {
            if (definition == null ||
                !definition.Id.StartsWith(TransitionPrefix, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(definition.AmbientLocaleName) ||
                string.IsNullOrWhiteSpace(definition.ParentAProgressionRegion) ||
                string.IsNullOrWhiteSpace(definition.ParentBProgressionRegion) ||
                string.IsNullOrWhiteSpace(definition.ParentAPresentationRegion) ||
                string.IsNullOrWhiteSpace(definition.ParentBPresentationRegion) ||
                definition.ResourceCount != ResourcesPerSeam ||
                !definition.EncounterPersistenceId.StartsWith(
                    EncounterPrefix,
                    StringComparison.Ordinal) ||
                !definition.EncounterPersistenceId.StartsWith(
                    "encounter." + definition.Id + ".",
                    StringComparison.Ordinal) ||
                !definition.SecretId.StartsWith(SecretPrefix, StringComparison.Ordinal) ||
                !definition.SecretId.StartsWith(
                    "secret." + definition.Id + ".",
                    StringComparison.Ordinal) ||
                EncounterCatalog.Get(definition.EncounterDefinitionId) == null)
            {
                return false;
            }

            for (var index = 0; index < definition.ResourceCount; index++)
            {
                var resource = definition.GetResource(index);
                if (!resource.Id.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                    !resource.Id.StartsWith(
                        "resource." + definition.Id + ".",
                        StringComparison.Ordinal))
                {
                    return false;
                }

                for (var priorIndex = 0; priorIndex < index; priorIndex++)
                {
                    if (string.Equals(
                            definition.GetResource(priorIndex).Id,
                            resource.Id,
                            StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return TryProject(
                definition,
                (definition.SegmentStart + definition.SegmentEnd) * 0.5f,
                out _);
        }

        private static bool HasRegistrationCollision(
            TransitionBiomeDefinition candidate)
        {
            for (var definitionIndex = 0;
                 definitionIndex < DefinitionCount;
                 definitionIndex++)
            {
                var existing = GetDefinitionAt(definitionIndex);
                if (existing == null)
                {
                    continue;
                }

                if (string.Equals(
                        existing.AmbientLocaleName,
                        candidate.AmbientLocaleName,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        existing.EncounterPersistenceId,
                        candidate.EncounterPersistenceId,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        existing.SecretId,
                        candidate.SecretId,
                        StringComparison.Ordinal))
                {
                    return true;
                }

                for (var candidateResourceIndex = 0;
                     candidateResourceIndex < candidate.ResourceCount;
                     candidateResourceIndex++)
                {
                    var candidateResource =
                        candidate.GetResource(candidateResourceIndex);
                    for (var existingResourceIndex = 0;
                         existingResourceIndex < existing.ResourceCount;
                         existingResourceIndex++)
                    {
                        if (string.Equals(
                                existing.GetResource(existingResourceIndex).Id,
                                candidateResource.Id,
                                StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void GetSegmentFrame(
            TransitionBiomeDefinition definition,
            out Vector3 midpoint,
            out Vector3 forward,
            out Vector3 right,
            out float length)
        {
            var delta = definition.SegmentEnd - definition.SegmentStart;
            delta.y = 0f;
            length = Mathf.Max(0.01f, delta.magnitude);
            forward = delta / length;
            right = new Vector3(forward.z, 0f, -forward.x);
            midpoint = (definition.SegmentStart + definition.SegmentEnd) * 0.5f;
            midpoint.y = 0f;
        }

        private static Vector3 HorizontalNormalized(Vector3 value)
        {
            value.y = 0f;
            var magnitude = value.magnitude;
            return magnitude > 0.0001f ? value / magnitude : Vector3.right;
        }

        private static GameObject CreateWorldPiece(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Vector3 forward,
            Material material,
            bool solid)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, true);
            piece.transform.position = position;
            piece.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid)
            {
                RemoveCollider(piece);
            }

            return piece;
        }

        private static GameObject CreateLocalPiece(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            piece.transform.localRotation = localRotation;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(piece);
            return piece;
        }

        internal static void RemoveCollider(GameObject target)
        {
            var itemCollider = target != null ? target.GetComponent<Collider>() : null;
            if (itemCollider == null)
            {
                return;
            }

            itemCollider.enabled = false;
            UnityEngine.Object.Destroy(itemCollider);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static TransitionBiomeDefinition[] CreateV1Definitions()
        {
            return new[]
            {
                CreateV1(
                    "transition.alderwake.morrowfen",
                    "Alderwake–Morrowfen Verge",
                    "Rainroot and lantern reed share one wet Threadglass bearing.",
                    "Alderwake Basin",
                    "Morrowfen",
                    string.Empty,
                    new Vector3(80f, 0f, -100f),
                    new Vector3(80f, 0f, 100f),
                    TransitionBiomeBlendMode.AcrossSegment,
                    Vector3.right,
                    12f,
                    10f,
                    5f,
                    new Color(0.28f, 0.58f, 0.53f),
                    TransitionBiomeLandmarkStyle.ThreadglassBraid,
                    -1f,
                    ResourceKind.Resin,
                    ResourceKind.Timber,
                    ResourceKind.Herbs,
                    ResourceKind.Chimesalt,
                    "encounter.wild.alderwake.mossbell_striders",
                    ResourceKind.Herbs),
                CreateV1(
                    "transition.alderwake.cinderwold",
                    "Alderwake–Cinderwold Verge",
                    "Moss-dark roots hold copper ash where the two grounds exchange heat.",
                    "Alderwake Basin",
                    "Cinderwold Verge",
                    string.Empty,
                    new Vector3(-80f, 0f, -100f),
                    new Vector3(-80f, 0f, 100f),
                    TransitionBiomeBlendMode.AcrossSegment,
                    Vector3.left,
                    12f,
                    10f,
                    5f,
                    new Color(0.5f, 0.36f, 0.25f),
                    TransitionBiomeLandmarkStyle.CopperRootGate,
                    1f,
                    ResourceKind.Stone,
                    ResourceKind.Resin,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    "encounter.wild.cinderwold.prismtail_kitelizards",
                    ResourceKind.Ore),
                CreateV1(
                    "transition.alderwake.glassmere",
                    "Alderwake–Glassmere Verge",
                    "Rainroot terraces flatten into saltglass around a shared tuning braid.",
                    "Alderwake Basin",
                    "Glassmere Steppe",
                    "region.glassmere",
                    new Vector3(0f, 0f, 128f),
                    new Vector3(0f, 0f, 152f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    7f,
                    10f,
                    4.5f,
                    new Color(0.4f, 0.72f, 0.68f),
                    TransitionBiomeLandmarkStyle.ThreadglassBraid,
                    1f,
                    ResourceKind.Resin,
                    ResourceKind.Timber,
                    ResourceKind.Chimesalt,
                    ResourceKind.Fiber,
                    "encounter.wild.alderwake.copperquill_nesters",
                    ResourceKind.Chimesalt),
                CreateV1(
                    "transition.morrowfen.rimecoil",
                    "Morrowfen–Rimecoil Verge",
                    "Lantern water skins over into blue ice while copper reeds turn with the wind.",
                    "Morrowfen",
                    "Rimecoil Range",
                    "region.rimecoil",
                    new Vector3(170f, 0f, 128f),
                    new Vector3(170f, 0f, 152f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    6f,
                    10f,
                    4f,
                    new Color(0.33f, 0.49f, 0.68f),
                    TransitionBiomeLandmarkStyle.WindglassFork,
                    -1f,
                    ResourceKind.Herbs,
                    ResourceKind.Chimesalt,
                    ResourceKind.Ore,
                    ResourceKind.Fiber,
                    "encounter.wild.morrowfen.tideveil_skimmers",
                    ResourceKind.Ore),
                CreateV1(
                    "transition.cinderwold.sunken_weald",
                    "Cinderwold–Sunken Weald Verge",
                    "Cooling ash shelves catch drowned roots beneath an amber Threadglass knot.",
                    "Cinderwold Verge",
                    "Sunken Weald",
                    "region.sunken_weald",
                    new Vector3(-170f, 0f, 128f),
                    new Vector3(-170f, 0f, 152f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    6.5f,
                    10f,
                    4.2f,
                    new Color(0.58f, 0.45f, 0.24f),
                    TransitionBiomeLandmarkStyle.CopperRootGate,
                    1f,
                    ResourceKind.Ore,
                    ResourceKind.Stone,
                    ResourceKind.Herbs,
                    ResourceKind.Resin,
                    "encounter.wild.cinderwold.emberhorn_burrowers",
                    ResourceKind.Resin),
                CreateV1(
                    "transition.cinderwold.prismscar",
                    "Cinderwold–Prismscar Verge",
                    "Ember seams split into violet flint beneath a low copper meridian.",
                    "Cinderwold Verge",
                    "Prismscar Downs",
                    "region.prismscar",
                    new Vector3(-90f, 0f, -128f),
                    new Vector3(-90f, 0f, -152f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    6.5f,
                    10f,
                    4.2f,
                    new Color(0.67f, 0.37f, 0.44f),
                    TransitionBiomeLandmarkStyle.TideglassNeedle,
                    1f,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    ResourceKind.Ore,
                    "encounter.wild.cinderwold.prismtail_kitelizards",
                    ResourceKind.Ore),
                CreateV1(
                    "transition.morrowfen.galecradle",
                    "Morrowfen–Galecradle Verge",
                    "Reed mist thins into ochre wind around a forked Threadglass vane.",
                    "Morrowfen",
                    "Galecradle Mesa",
                    "region.galecradle",
                    new Vector3(100f, 0f, -128f),
                    new Vector3(100f, 0f, -152f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    7f,
                    10f,
                    4.5f,
                    new Color(0.38f, 0.61f, 0.52f),
                    TransitionBiomeLandmarkStyle.WindglassFork,
                    -1f,
                    ResourceKind.Herbs,
                    ResourceKind.Resin,
                    ResourceKind.Fiber,
                    ResourceKind.Ore,
                    "encounter.wild.morrowfen.gloamreed_lanternbacks",
                    ResourceKind.Fiber),
                CreateV1(
                    "transition.cinderwold.lumenwaste",
                    "Cinderwold–Lumenwaste Verge",
                    "Banked cinders become phosphor sand beneath shaded copper lenses.",
                    "Cinderwold Verge",
                    "Lumenwaste Reach",
                    "region.lumenwaste",
                    new Vector3(-238f, 0f, 120f),
                    new Vector3(-308f, 0f, 120f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    7f,
                    10f,
                    4.5f,
                    new Color(0.7f, 0.53f, 0.2f),
                    TransitionBiomeLandmarkStyle.TideglassNeedle,
                    -1f,
                    ResourceKind.Ore,
                    ResourceKind.Stone,
                    ResourceKind.Chimesalt,
                    ResourceKind.Fiber,
                    "encounter.wild.cinderwold.emberhorn_burrowers",
                    ResourceKind.Chimesalt),
                CreateV1(
                    "transition.morrowfen.brassroot",
                    "Morrowfen–Brassroot Verge",
                    "Wet reed roots harden into warm foundry brass along the intake line.",
                    "Morrowfen",
                    "Brassroot Deeps",
                    "region.brassroot",
                    new Vector3(238f, 0f, 110f),
                    new Vector3(292f, 0f, 110f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    7f,
                    10f,
                    4.5f,
                    new Color(0.5f, 0.4f, 0.28f),
                    TransitionBiomeLandmarkStyle.CopperRootGate,
                    1f,
                    ResourceKind.Herbs,
                    ResourceKind.Chimesalt,
                    ResourceKind.Ore,
                    ResourceKind.Resin,
                    "encounter.wild.morrowfen.tideveil_skimmers",
                    ResourceKind.Ore),
                CreateV1(
                    "transition.sunken_weald.cloudbarrow",
                    "Sunken Weald–Cloudbarrow Verge",
                    "Flood roots rise into grassbound processional ribs below the cloud shelf.",
                    "Sunken Weald",
                    "Cloudbarrow Expanse",
                    "region.cloudbarrow",
                    new Vector3(-230f, 0f, 288f),
                    new Vector3(-230f, 0f, 322f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    7f,
                    10f,
                    4.5f,
                    new Color(0.55f, 0.7f, 0.4f),
                    TransitionBiomeLandmarkStyle.CopperRootGate,
                    1f,
                    ResourceKind.Herbs,
                    ResourceKind.Resin,
                    ResourceKind.Fiber,
                    ResourceKind.Stone,
                    "encounter.wild.morrowfen.gloamreed_lanternbacks",
                    ResourceKind.Fiber),
                CreateV1(
                    "transition.rimecoil.nightloom",
                    "Rimecoil–Nightloom Verge",
                    "Blue ice darkens beneath woven roots that carry violet cold-light.",
                    "Rimecoil Range",
                    "Nightloom Hollow",
                    "region.nightloom",
                    new Vector3(225f, 0f, 288f),
                    new Vector3(225f, 0f, 322f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    7f,
                    10f,
                    4.5f,
                    new Color(0.44f, 0.36f, 0.69f),
                    TransitionBiomeLandmarkStyle.ThreadglassBraid,
                    -1f,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    ResourceKind.Resin,
                    ResourceKind.Fiber,
                    "encounter.wild.alderwake.copperquill_nesters",
                    ResourceKind.Resin),
                CreateV1(
                    "transition.prismscar.tideglass",
                    "Prismscar–Tideglass Verge",
                    "Violet meridian stone descends into black sand and a motionless blue tide.",
                    "Prismscar Downs",
                    "Tideglass Strand",
                    "region.tideglass",
                    new Vector3(-21f, 0f, -288f),
                    new Vector3(-21f, 0f, -342f),
                    TransitionBiomeBlendMode.AlongSegment,
                    Vector3.zero,
                    7f,
                    10f,
                    4.5f,
                    new Color(0.39f, 0.38f, 0.68f),
                    TransitionBiomeLandmarkStyle.TideglassNeedle,
                    1f,
                    ResourceKind.Stone,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    "encounter.wild.cinderwold.prismtail_kitelizards",
                    ResourceKind.Chimesalt)
            };
        }

        private static TransitionBiomeDefinition CreateV1(
            string id,
            string ambientLocaleName,
            string discoveryText,
            string parentA,
            string parentB,
            string connectorRegionId,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            TransitionBiomeBlendMode blendMode,
            Vector3 parentBSide,
            float corridorHalfWidth,
            float softFalloff,
            float coreHalfWidth,
            Color mapTint,
            TransitionBiomeLandmarkStyle landmarkStyle,
            float contentSide,
            ResourceKind resourceA1,
            ResourceKind resourceA2,
            ResourceKind resourceB1,
            ResourceKind resourceB2,
            string encounterDefinitionId,
            ResourceKind secretReward)
        {
            return new TransitionBiomeDefinition(
                id,
                ambientLocaleName,
                discoveryText,
                parentA,
                parentB,
                parentA,
                parentB,
                connectorRegionId,
                segmentStart,
                segmentEnd,
                blendMode,
                parentBSide,
                corridorHalfWidth,
                softFalloff,
                coreHalfWidth,
                mapTint,
                landmarkStyle,
                contentSide,
                CreateResourceSet(
                    id,
                    resourceA1,
                    resourceA2,
                    resourceB1,
                    resourceB2,
                    mapTint),
                encounterDefinitionId,
                "encounter." + id + ".seam_patrol",
                "secret." + id + ".threadglass_memory",
                ambientLocaleName + " Threadglass Memory",
                "A copper-bound route slate records two ecologies without naming either the lesser.",
                secretReward,
                2);
        }

        private static TransitionResourceSpec[] CreateResourceSet(
            string transitionId,
            ResourceKind resourceA1,
            ResourceKind resourceA2,
            ResourceKind resourceB1,
            ResourceKind resourceB2,
            Color accent)
        {
            return new[]
            {
                CreateResource(transitionId, "a", resourceA1, 1, accent),
                CreateResource(transitionId, "a", resourceA2, 2, accent),
                CreateResource(transitionId, "b", resourceB1, 1, accent),
                CreateResource(transitionId, "b", resourceB2, 2, accent)
            };
        }

        private static TransitionResourceSpec CreateResource(
            string transitionId,
            string parentSide,
            ResourceKind kind,
            int ordinal,
            Color accent)
        {
            var id =
                "resource." +
                transitionId +
                "." +
                parentSide +
                "." +
                ResourceSlug(kind) +
                "." +
                ordinal;
            return new TransitionResourceSpec(
                id,
                kind,
                kind == ResourceKind.Chimesalt ? 2 : 1,
                Color.Lerp(ResourceColor(kind), accent, 0.22f));
        }

        private static string ResourceSlug(ResourceKind kind)
        {
            return kind switch
            {
                ResourceKind.Timber => "timber",
                ResourceKind.Stone => "stone",
                ResourceKind.Resin => "resin",
                ResourceKind.Chimesalt => "chimesalt",
                ResourceKind.Herbs => "herbs",
                ResourceKind.Ore => "ore",
                ResourceKind.Fiber => "fiber",
                _ => "resource"
            };
        }

        private static Color ResourceColor(ResourceKind kind)
        {
            return kind switch
            {
                ResourceKind.Timber => new Color(0.48f, 0.31f, 0.15f),
                ResourceKind.Stone => new Color(0.48f, 0.5f, 0.52f),
                ResourceKind.Resin => new Color(0.84f, 0.5f, 0.2f),
                ResourceKind.Chimesalt => new Color(0.58f, 0.82f, 0.94f),
                ResourceKind.Herbs => new Color(0.48f, 0.75f, 0.38f),
                ResourceKind.Ore => new Color(0.62f, 0.52f, 0.46f),
                ResourceKind.Fiber => new Color(0.58f, 0.76f, 0.66f),
                _ => Color.gray
            };
        }
    }

    internal sealed class TransitionBiomeRuntime
    {
        public TransitionBiomeDefinition Definition { get; }
        public GameObject StaticRoot { get; }
        public GameObject DetailRoot { get; }

        public TransitionBiomeRuntime(
            TransitionBiomeDefinition definition,
            GameObject staticRoot,
            GameObject detailRoot)
        {
            Definition = definition;
            StaticRoot = staticRoot;
            DetailRoot = detailRoot;
        }
    }

    internal sealed class TransitionBiomeWorld : MonoBehaviour
    {
        private readonly List<TransitionBiomeRuntime> runtimes =
            new List<TransitionBiomeRuntime>();
        private TetherfallGame game;

        internal List<TransitionBiomeRuntime> Runtimes => runtimes;

        internal void Initialize(TetherfallGame owner)
        {
            game = owner;
        }

        internal void SynchronizeCatalog()
        {
            if (game == null)
            {
                return;
            }

            for (var definitionIndex = 0;
                 definitionIndex < TetherfallTransitionBiomes.DefinitionCount;
                 definitionIndex++)
            {
                var definition =
                    TetherfallTransitionBiomes.GetDefinitionAt(definitionIndex);
                if (definition == null || Contains(definition.Id))
                {
                    continue;
                }

                runtimes.Add(
                    TetherfallTransitionBiomes.BuildRuntime(
                        game,
                        transform,
                        definition));
            }
        }

        private bool Contains(string transitionId)
        {
            for (var index = 0; index < runtimes.Count; index++)
            {
                var runtime = runtimes[index];
                if (runtime != null &&
                    runtime.Definition != null &&
                    string.Equals(
                        runtime.Definition.Id,
                        transitionId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [DefaultExecutionOrder(-9990)]
    public sealed class TransitionBiomeDirector : MonoBehaviour
    {
        private readonly List<TransitionBiomeRuntime> runtimes =
            new List<TransitionBiomeRuntime>();
        private readonly TransitionBiomeRuntime[] staticSelection =
            new TransitionBiomeRuntime[TetherfallTransitionBiomes.MaxActiveStaticShells];
        private readonly float[] staticScores =
            new float[TetherfallTransitionBiomes.MaxActiveStaticShells];
        private readonly TransitionBiomeRuntime[] detailSelection =
            new TransitionBiomeRuntime[TetherfallTransitionBiomes.MaxActiveDetailRoots];
        private readonly float[] detailScores =
            new float[TetherfallTransitionBiomes.MaxActiveDetailRoots];

        private TetherfallGame game;
        private float nextActivationRefreshAt;
        private TransitionLocaleHysteresisState localeState;

        public RegionLocaleQuery CurrentLocale { get; private set; }
        public bool HasCurrentTransitionLocale => CurrentLocale.HasTransition;

        internal void Initialize(
            TetherfallGame owner,
            List<TransitionBiomeRuntime> transitionRuntimes)
        {
            game = owner;
            runtimes.Clear();
            if (transitionRuntimes != null)
            {
                for (var index = 0; index < transitionRuntimes.Count; index++)
                {
                    runtimes.Add(transitionRuntimes[index]);
                }
            }

            localeState.Reset();
            CurrentLocale = RegionLocaleQuery.ForProgressionRegion(
                owner != null ? owner.CurrentRegion : string.Empty);
            nextActivationRefreshAt = 0f;
        }

        private void Update()
        {
            if (game == null ||
                game.Player == null ||
                Time.unscaledTime < nextActivationRefreshAt)
            {
                return;
            }

            nextActivationRefreshAt =
                Time.unscaledTime + TetherfallTransitionBiomes.ActivationRefreshSeconds;
            var playerPosition = game.PlayerTransform.position;
            RefreshLocale(playerPosition);
            RefreshActivation(playerPosition);
        }

        private void RefreshLocale(Vector3 playerPosition)
        {
            var pack = TetherfallRegionPacks.FindPlayable(playerPosition);
            var progressionRegion = pack != null
                ? pack.displayName
                : TetherfallRegionPacks.ResolveLegacyRegionName(playerPosition);
            if (!TetherfallTransitionBiomes.TryGetLocale(
                    playerPosition,
                    progressionRegion,
                    ref localeState,
                    out var locale))
            {
                locale = RegionLocaleQuery.ForProgressionRegion(progressionRegion);
            }

            CurrentLocale = locale;
        }

        private void RefreshActivation(Vector3 playerPosition)
        {
            ClearSelection(staticSelection, staticScores);
            ClearSelection(detailSelection, detailScores);

            for (var index = 0; index < runtimes.Count; index++)
            {
                var runtime = runtimes[index];
                if (runtime == null ||
                    runtime.Definition == null ||
                    !TetherfallTransitionBiomes.TryProject(
                        runtime.Definition,
                        playerPosition,
                        out var projection))
                {
                    continue;
                }

                var distance = projection.DistanceToSegment;
                var staticActive =
                    runtime.StaticRoot != null && runtime.StaticRoot.activeSelf;
                var staticLimit = staticActive
                    ? TetherfallTransitionBiomes.StaticSleepDistance
                    : TetherfallTransitionBiomes.StaticPrewakeDistance;
                if (distance <= staticLimit)
                {
                    var score = distance * distance - (staticActive ? 400f : 0f);
                    if (projection.IsInSeamCore)
                    {
                        score -= 20000f;
                    }

                    InsertCandidate(
                        runtime,
                        score,
                        staticSelection,
                        staticScores);
                }

                var detailActive =
                    runtime.DetailRoot != null && runtime.DetailRoot.activeSelf;
                var detailLimit = detailActive
                    ? TetherfallTransitionBiomes.DetailSleepDistance
                    : TetherfallTransitionBiomes.DetailWakeDistance;
                if (distance <= detailLimit)
                {
                    var score = distance * distance - (detailActive ? 225f : 0f);
                    if (projection.IsInSeamCore)
                    {
                        score -= 10000f;
                    }

                    InsertCandidate(
                        runtime,
                        score,
                        detailSelection,
                        detailScores);
                }
            }

            for (var detailIndex = 0;
                 detailIndex < detailSelection.Length;
                 detailIndex++)
            {
                EnsureSelected(detailSelection[detailIndex], staticSelection);
            }

            for (var index = 0; index < runtimes.Count; index++)
            {
                var runtime = runtimes[index];
                if (runtime == null)
                {
                    continue;
                }

                SetRootActive(
                    runtime.StaticRoot,
                    IsSelected(runtime, staticSelection));
                SetRootActive(
                    runtime.DetailRoot,
                    IsSelected(runtime, detailSelection));
            }
        }

        private static void EnsureSelected(
            TransitionBiomeRuntime runtime,
            TransitionBiomeRuntime[] selection)
        {
            if (runtime == null || IsSelected(runtime, selection))
            {
                return;
            }

            for (var index = selection.Length - 1; index > 0; index--)
            {
                selection[index] = selection[index - 1];
            }

            selection[0] = runtime;
        }

        private static void InsertCandidate(
            TransitionBiomeRuntime runtime,
            float score,
            TransitionBiomeRuntime[] selection,
            float[] scores)
        {
            for (var index = 0; index < selection.Length; index++)
            {
                if (selection[index] != null && score >= scores[index])
                {
                    continue;
                }

                for (var shift = selection.Length - 1; shift > index; shift--)
                {
                    selection[shift] = selection[shift - 1];
                    scores[shift] = scores[shift - 1];
                }

                selection[index] = runtime;
                scores[index] = score;
                return;
            }
        }

        private static void ClearSelection(
            TransitionBiomeRuntime[] selection,
            float[] scores)
        {
            for (var index = 0; index < selection.Length; index++)
            {
                selection[index] = null;
                scores[index] = float.PositiveInfinity;
            }
        }

        private static bool IsSelected(
            TransitionBiomeRuntime runtime,
            TransitionBiomeRuntime[] selection)
        {
            for (var index = 0; index < selection.Length; index++)
            {
                if (ReferenceEquals(runtime, selection[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetRootActive(GameObject root, bool active)
        {
            if (root != null && root.activeSelf != active)
            {
                root.SetActive(active);
            }
        }
    }

    public sealed class TransitionResourceInteractable : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private TransitionResourceSpec spec;
        private Renderer visualRenderer;
        private Collider interactionCollider;
        private bool available;

        public string PersistenceId => spec.Id;
        public ResourceKind Kind => spec.Kind;
        public bool IsAvailable => available;
        public string InteractionPrompt => available
            ? $"Gather seam-grown {ResourceName(spec.Kind)}"
            : "This seam resource has already been gathered";

        internal static TransitionResourceInteractable Create(
            TetherfallGame game,
            TransitionResourceSpec spec,
            Vector3 position,
            Transform parent)
        {
            var primitive = spec.Kind == ResourceKind.Timber
                ? PrimitiveType.Cylinder
                : spec.Kind == ResourceKind.Herbs || spec.Kind == ResourceKind.Fiber
                    ? PrimitiveType.Sphere
                    : PrimitiveType.Cube;
            var root = GameObject.CreatePrimitive(primitive);
            root.name = spec.Id;
            root.transform.SetParent(parent, true);
            root.transform.position = position;
            root.transform.localScale = spec.Kind == ResourceKind.Timber
                ? new Vector3(0.36f, 0.72f, 0.36f)
                : spec.Kind == ResourceKind.Herbs || spec.Kind == ResourceKind.Fiber
                    ? new Vector3(0.78f, 0.46f, 0.68f)
                    : Vector3.one * 0.74f;

            var node = root.AddComponent<TransitionResourceInteractable>();
            node.game = game;
            node.spec = spec;
            node.visualRenderer = root.GetComponent<Renderer>();
            node.interactionCollider = root.GetComponent<Collider>();
            node.visualRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                spec.Color,
                spec.Kind == ResourceKind.Chimesalt ? 0.3f : 0.05f,
                0.48f);
            game.RegisterInteractable(node);
            node.RefreshState();
            return node;
        }

        private void OnEnable()
        {
            RefreshState();
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && available;
        }

        public void Interact(TetherfallGame owner)
        {
            if (owner == null ||
                !available ||
                !owner.TryGatherTransitionResource(spec))
            {
                return;
            }

            available = false;
            ApplyAvailability();
        }

        private void RefreshState()
        {
            if (game == null || string.IsNullOrEmpty(spec.Id))
            {
                return;
            }

            available = !game.IsTransitionContentRecovered(spec.Id);
            ApplyAvailability();
        }

        private void ApplyAvailability()
        {
            if (visualRenderer != null)
            {
                visualRenderer.enabled = available;
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = available;
            }
        }

        private static string ResourceName(ResourceKind kind)
        {
            return kind switch
            {
                ResourceKind.Timber => "timber",
                ResourceKind.Stone => "stone",
                ResourceKind.Resin => "resin",
                ResourceKind.Chimesalt => "Chimesalt",
                ResourceKind.Herbs => "herbs",
                ResourceKind.Ore => "ore",
                ResourceKind.Fiber => "fiber",
                _ => "material"
            };
        }
    }

    public sealed class TransitionSecretInteractable : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private TransitionBiomeDefinition definition;
        private Renderer shellRenderer;
        private Renderer threadglassRenderer;
        private Collider interactionCollider;
        private bool recovered;

        public string SecretId =>
            definition != null ? definition.SecretId : string.Empty;
        public bool IsRecovered => recovered;
        public string InteractionPrompt => recovered
            ? "Transition memory recovered"
            : definition != null
                ? $"Recover {definition.SecretName}"
                : "Recover transition memory";

        internal static TransitionSecretInteractable Create(
            TetherfallGame game,
            TransitionBiomeDefinition definition,
            Transform parent)
        {
            GetSecretPosition(definition, out var position);
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = definition.SecretId;
            root.transform.SetParent(parent, true);
            root.transform.position = position;
            root.transform.localScale = new Vector3(0.92f, 0.68f, 0.82f);

            var secret = root.AddComponent<TransitionSecretInteractable>();
            secret.game = game;
            secret.definition = definition;
            secret.shellRenderer = root.GetComponent<Renderer>();
            secret.interactionCollider = root.GetComponent<Collider>();
            secret.shellRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                Color.Lerp(definition.MapTint, new Color(0.11f, 0.09f, 0.08f), 0.64f),
                0.16f,
                0.4f);

            var crest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crest.name = "Semantic Threadglass memory";
            crest.transform.SetParent(root.transform, false);
            crest.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            crest.transform.localScale = new Vector3(0.28f, 0.1f, 0.28f);
            secret.threadglassRenderer = crest.GetComponent<Renderer>();
            secret.threadglassRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetGlow(
                game,
                Color.Lerp(definition.MapTint, new Color(0.3f, 0.9f, 0.8f), 0.58f),
                0.9f,
                0.65f);
            TetherfallTransitionBiomes.RemoveCollider(crest);

            game.RegisterInteractable(secret);
            secret.RefreshState();
            return secret;
        }

        private void OnEnable()
        {
            RefreshState();
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && !recovered;
        }

        public void Interact(TetherfallGame owner)
        {
            if (owner == null ||
                recovered ||
                definition == null ||
                !owner.TryRecoverTransitionSecret(definition))
            {
                return;
            }

            recovered = true;
            ApplyRecoveredState();
        }

        private void RefreshState()
        {
            if (game == null || definition == null)
            {
                return;
            }

            recovered = game.IsTransitionContentRecovered(definition.SecretId);
            ApplyRecoveredState();
        }

        private void ApplyRecoveredState()
        {
            if (shellRenderer != null)
            {
                shellRenderer.enabled = true;
            }

            if (threadglassRenderer != null)
            {
                threadglassRenderer.enabled = !recovered;
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = !recovered;
            }
        }

        private static void GetSecretPosition(
            TransitionBiomeDefinition definition,
            out Vector3 position)
        {
            var delta = definition.SegmentEnd - definition.SegmentStart;
            delta.y = 0f;
            var length = Mathf.Max(0.01f, delta.magnitude);
            var forward = delta / length;
            var right = new Vector3(forward.z, 0f, -forward.x);
            if (definition.BlendMode == TransitionBiomeBlendMode.AlongSegment)
            {
                position =
                    definition.SegmentStart -
                    forward * 6f +
                    right *
                    definition.ContentSide *
                    (definition.CorridorHalfWidth + 4f);
            }
            else
            {
                var parentBSide = definition.ParentBSide;
                parentBSide.y = 0f;
                parentBSide = parentBSide.sqrMagnitude > 0.0001f
                    ? parentBSide.normalized
                    : Vector3.right;
                position =
                    Vector3.Lerp(
                        definition.SegmentStart,
                        definition.SegmentEnd,
                        0.78f) -
                    parentBSide * (definition.CorridorHalfWidth + 4f);
            }

            position.y = 0.44f;
        }
    }

    public sealed partial class TetherfallGame
    {
        internal bool IsTransitionContentRecovered(string semanticId)
        {
            return !string.IsNullOrEmpty(semanticId) &&
                   recoveredFieldCacheIds.Contains(semanticId);
        }

        internal bool TryGatherTransitionResource(TransitionResourceSpec spec)
        {
            if (string.IsNullOrEmpty(spec.Id) ||
                !spec.Id.StartsWith(
                    "resource.transition.",
                    StringComparison.Ordinal) ||
                !recoveredFieldCacheIds.Add(spec.Id))
            {
                return false;
            }

            RecordResourceNodeGathered();
            AddResource(
                spec.Kind,
                Mathf.Max(1, spec.Amount),
                ResourceAcquisitionSource.WorldGather);
            Notify($"Gathered seam-grown {spec.Kind}.");
            RequestSave();
            return true;
        }

        internal bool TryRecoverTransitionSecret(
            TransitionBiomeDefinition definition)
        {
            if (definition == null ||
                string.IsNullOrEmpty(definition.SecretId) ||
                !definition.SecretId.StartsWith(
                    "secret.transition.",
                    StringComparison.Ordinal) ||
                !recoveredFieldCacheIds.Add(definition.SecretId))
            {
                return false;
            }

            AddResource(
                definition.SecretReward,
                Mathf.Max(1, definition.SecretRewardAmount));
            Notify(definition.SecretInscription, 6f);
            RequestSave();
            return true;
        }
    }
}
