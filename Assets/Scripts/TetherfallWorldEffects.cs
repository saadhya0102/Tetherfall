using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Tetherfall
{
    public static class WorldPresentationBuilder
    {
        private static bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            built = false;
        }

        public static void Build(TetherfallGame game)
        {
            if (built)
            {
                return;
            }

            built = true;
            BuildAlderwake(game);
            BuildMorrowfen(game);
            BuildCinderwold(game);
        }

        private static void BuildAlderwake(TetherfallGame game)
        {
            CreateRouteLantern(game, new Vector3(-12f, 0f, -4f), new Color(0.36f, 0.92f, 0.72f));
            CreateRouteLantern(game, new Vector3(8f, 0f, 12f), new Color(0.36f, 0.92f, 0.72f));
            CreateRouteLantern(game, new Vector3(29f, 0f, 10f), new Color(0.88f, 0.58f, 0.26f));
            CreateRegionalAtmosphere(
                game,
                "Alderwake threadfire drift",
                new Vector3(-6f, 1.35f, 14f),
                new Vector3(34f, 2.3f, 27f),
                RegionalAtmosphereKind.Alderwake,
                48,
                54f,
                0f);
            CreatePathStones(game, new Vector3(-19f, 0.04f, -2f), new Vector3(27f, 0.04f, 14f), 11, new Color(0.35f, 0.34f, 0.25f));
            CreateWildflowerPatch(game, new Vector3(24f, 0f, 10f), new Color(0.7f, 0.83f, 0.35f), 13);
        }

        private static void BuildMorrowfen(TetherfallGame game)
        {
            CreateRouteLantern(game, new Vector3(102f, 0f, 4f), new Color(0.31f, 0.82f, 0.76f));
            CreateRouteLantern(game, new Vector3(144f, 0f, 13f), new Color(0.4f, 0.88f, 0.82f));
            CreateRouteLantern(game, new Vector3(176f, 0f, -17f), new Color(0.36f, 0.72f, 0.83f));
            CreateRegionalAtmosphere(
                game,
                "Morrowfen veil glimmers",
                new Vector3(139f, 1.25f, 3f),
                new Vector3(82f, 2.4f, 58f),
                RegionalAtmosphereKind.Morrowfen,
                56,
                76f,
                0.25f);
            CreateLilyLights(game, new Vector3(126f, 0.14f, 9f), new Vector3(72f, 0f, 23f), 18);
        }

        private static void BuildCinderwold(TetherfallGame game)
        {
            CreateRouteLantern(game, new Vector3(-103f, 0f, -8f), new Color(0.98f, 0.48f, 0.22f));
            CreateRouteLantern(game, new Vector3(-135f, 0f, 27f), new Color(0.91f, 0.35f, 0.19f));
            CreateRouteLantern(game, new Vector3(-170f, 0f, 62f), new Color(0.84f, 0.31f, 0.17f));
            CreateRegionalAtmosphere(
                game,
                "Cinderwold ember-ash braid",
                new Vector3(-145f, 1.45f, 3f),
                new Vector3(112f, 3.6f, 78f),
                RegionalAtmosphereKind.Cinderwold,
                64,
                92f,
                0.5f);
            CreateCrystalGarden(game, new Vector3(-132f, 0f, 7f), 11, new Color(0.9f, 0.31f, 0.15f));
            CreateCrystalGarden(game, new Vector3(-178f, 0f, -18f), 9, new Color(0.77f, 0.24f, 0.13f));
        }

        private static void CreateRouteLantern(TetherfallGame game, Vector3 position, Color glowColor)
        {
            var root = new GameObject("Threadglass route lantern");
            root.transform.position = position;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            post.transform.localScale = new Vector3(0.12f, 1.25f, 0.12f);
            post.GetComponent<Renderer>().sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.18f, 0.12f, 0.08f),
                0.05f,
                0.28f);
            Object.Destroy(post.GetComponent<Collider>());

            var bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.transform.SetParent(root.transform, false);
            bracket.transform.localPosition = new Vector3(0.18f, 2.25f, 0f);
            bracket.transform.localScale = new Vector3(0.42f, 0.08f, 0.08f);
            bracket.GetComponent<Renderer>().sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.66f, 0.37f, 0.16f),
                0.35f,
                0.45f);
            Object.Destroy(bracket.GetComponent<Collider>());

            var lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lantern.transform.SetParent(root.transform, false);
            lantern.transform.localPosition = new Vector3(0.36f, 2.04f, 0f);
            lantern.transform.localScale = Vector3.one * 0.34f;
            lantern.GetComponent<Renderer>().sharedMaterial = RuntimeEffectMaterialCache.GetGlow(
                game,
                glowColor,
                1.9f,
                0.7f);
            Object.Destroy(lantern.GetComponent<Collider>());
            lantern.AddComponent<RouteGlow>().Initialize(0.09f, 0.8f);

            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.intensity = 1.15f;
            light.range = 6.6f;
            light.shadows = LightShadows.None;
        }

        private enum RegionalAtmosphereKind
        {
            Alderwake,
            Morrowfen,
            Cinderwold
        }

        private static void CreateRegionalAtmosphere(
            TetherfallGame game,
            string name,
            Vector3 center,
            Vector3 area,
            RegionalAtmosphereKind kind,
            int maxParticles,
            float cullDistance,
            float pollOffset)
        {
            var root = new GameObject(name);
            root.transform.position = center;

            Color colorMin;
            Color colorMax;
            float lifetimeMin;
            float lifetimeMax;
            float sizeMin;
            float sizeMax;
            float emissionRate;
            float horizontalDrift;
            float riseMin;
            float riseMax;
            float noiseStrength;
            float noiseFrequency;
            float noiseScroll;
            float gravity;
            float rotationSpeed;
            float peakAlpha;

            switch (kind)
            {
                case RegionalAtmosphereKind.Morrowfen:
                    colorMin = new Color(0.26f, 0.74f, 0.78f, 0.62f);
                    colorMax = new Color(0.68f, 0.98f, 0.9f, 0.74f);
                    lifetimeMin = 8f;
                    lifetimeMax = 12f;
                    sizeMin = 0.085f;
                    sizeMax = 0.21f;
                    emissionRate = 5f;
                    horizontalDrift = 0.045f;
                    riseMin = -0.01f;
                    riseMax = 0.045f;
                    noiseStrength = 0.12f;
                    noiseFrequency = 0.16f;
                    noiseScroll = 0.045f;
                    gravity = -0.002f;
                    rotationSpeed = 0.28f;
                    peakAlpha = 0.72f;
                    break;
                case RegionalAtmosphereKind.Cinderwold:
                    colorMin = new Color(0.38f, 0.12f, 0.055f, 0.62f);
                    colorMax = new Color(1f, 0.42f, 0.16f, 0.88f);
                    lifetimeMin = 5.8f;
                    lifetimeMax = 8.6f;
                    sizeMin = 0.065f;
                    sizeMax = 0.17f;
                    emissionRate = 8.1f;
                    horizontalDrift = 0.11f;
                    riseMin = 0.16f;
                    riseMax = 0.38f;
                    noiseStrength = 0.24f;
                    noiseFrequency = 0.34f;
                    noiseScroll = 0.12f;
                    gravity = -0.008f;
                    rotationSpeed = 0.72f;
                    peakAlpha = 0.86f;
                    break;
                default:
                    colorMin = new Color(0.42f, 0.97f, 0.62f, 0.9f);
                    colorMax = new Color(1f, 0.76f, 0.3f, 0.78f);
                    lifetimeMin = 5.5f;
                    lifetimeMax = 8.2f;
                    sizeMin = 0.055f;
                    sizeMax = 0.135f;
                    emissionRate = 6.4f;
                    horizontalDrift = 0.055f;
                    riseMin = 0.015f;
                    riseMax = 0.09f;
                    noiseStrength = 0.16f;
                    noiseFrequency = 0.28f;
                    noiseScroll = 0.08f;
                    gravity = 0f;
                    rotationSpeed = 0.65f;
                    peakAlpha = 0.9f;
                    break;
            }

            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = (uint)(kind + 1) * 191u;

            var main = particles.main;
            main.duration = 12f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(colorMin, colorMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.up * area.y * 0.5f;
            shape.scale = area;
            shape.boxThickness = Vector3.one;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-horizontalDrift, horizontalDrift);
            velocity.y = new ParticleSystem.MinMaxCurve(riseMin, riseMax);
            velocity.z = new ParticleSystem.MinMaxCurve(-horizontalDrift, horizontalDrift);

            var noise = particles.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.separateAxes = false;
            noise.strength = new ParticleSystem.MinMaxCurve(noiseStrength * 0.72f, noiseStrength);
            noise.frequency = noiseFrequency;
            noise.scrollSpeed = noiseScroll;
            noise.damping = true;
            noise.octaveCount = 1;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateAtmosphereFade(peakAlpha));

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.52f),
                    new Keyframe(0.16f, 1f),
                    new Keyframe(0.7f, 0.82f),
                    new Keyframe(1f, 0.16f)));

            var rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-rotationSpeed, rotationSpeed);

            var collision = particles.collision;
            collision.enabled = false;
            var lights = particles.lights;
            lights.enabled = false;
            var trails = particles.trails;
            trails.enabled = false;

            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;
            particleRenderer.sortMode = ParticleSystemSortMode.None;
            particleRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetSoftParticleMaterial(game);
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.lightProbeUsage = LightProbeUsage.Off;
            particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            particleRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            particleRenderer.allowOcclusionWhenDynamic = true;
            particleRenderer.minParticleSize = 0f;
            particleRenderer.maxParticleSize = 0.08f;

            var noiseMargin = noiseStrength * lifetimeMax * 0.5f + 0.5f;
            var horizontalMargin = horizontalDrift * lifetimeMax + noiseMargin + 0.5f;
            var gravityDisplacement = 0.5f * Physics.gravity.y * gravity * lifetimeMax * lifetimeMax;
            var minimumY = Mathf.Min(0f, riseMin * lifetimeMax + gravityDisplacement) - noiseMargin - 0.5f;
            var maximumY = area.y +
                           Mathf.Max(0f, riseMax * lifetimeMax + gravityDisplacement) +
                           noiseMargin +
                           0.5f;
            particleRenderer.localBounds = new Bounds(
                new Vector3(0f, (minimumY + maximumY) * 0.5f, 0f),
                new Vector3(
                    area.x + horizontalMargin * 2f,
                    maximumY - minimumY,
                    area.z + horizontalMargin * 2f));

            root.AddComponent<RegionalAtmosphereCuller>().Initialize(
                game,
                particles,
                particleRenderer,
                game.PlayerTransform,
                cullDistance,
                pollOffset);
        }

        private static Gradient CreateAtmosphereFade(float peakAlpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(peakAlpha, 0.12f),
                    new GradientAlphaKey(peakAlpha * 0.62f, 0.56f),
                    new GradientAlphaKey(peakAlpha * 0.92f, 0.78f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static void CreatePathStones(TetherfallGame game, Vector3 start, Vector3 end, int count, Color color)
        {
            var stoneMaterial = RuntimeEffectMaterialCache.GetOpaque(game, color, 0.04f, 0.38f);
            for (var index = 0; index < count; index++)
            {
                var t = index / (float)(count - 1);
                var point = Vector3.Lerp(start, end, t);
                point.x += Mathf.Sin(index * 1.9f) * 0.75f;
                point.z += Mathf.Cos(index * 1.21f) * 0.42f;
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = "Set route stone";
                stone.transform.position = point;
                stone.transform.localScale = new Vector3(1.15f, 0.08f, 0.78f);
                stone.transform.rotation = Quaternion.Euler(0f, index * 27f, index % 2 == 0 ? 2.5f : -2.5f);
                stone.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
                Object.Destroy(stone.GetComponent<Collider>());
            }
        }

        private static void CreateWildflowerPatch(TetherfallGame game, Vector3 center, Color color, int count)
        {
            var stemMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.23f, 0.47f, 0.25f),
                0f,
                0.2f);
            var bloomMaterial = RuntimeEffectMaterialCache.GetGlow(game, color, 0.48f, 0.35f);
            for (var index = 0; index < count; index++)
            {
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Alderwake wildflower";
                stem.transform.position = center + new Vector3(
                    Mathf.Sin(index * 2.37f) * 4.8f,
                    0.34f,
                    Mathf.Cos(index * 1.41f) * 3.2f);
                stem.transform.localScale = new Vector3(0.035f, 0.34f, 0.035f);
                stem.GetComponent<Renderer>().sharedMaterial = stemMaterial;
                Object.Destroy(stem.GetComponent<Collider>());

                var bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bloom.transform.SetParent(stem.transform, false);
                bloom.transform.localPosition = new Vector3(0f, 1.08f, 0f);
                bloom.transform.localScale = Vector3.one * 2.7f;
                bloom.GetComponent<Renderer>().sharedMaterial = bloomMaterial;
                Object.Destroy(bloom.GetComponent<Collider>());
            }
        }

        private static void CreateLilyLights(TetherfallGame game, Vector3 center, Vector3 area, int count)
        {
            var lilyMaterial = RuntimeEffectMaterialCache.GetGlow(
                game,
                new Color(0.23f, 0.71f, 0.68f),
                0.55f,
                0.55f);
            for (var index = 0; index < count; index++)
            {
                var lily = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lily.name = "Fen lily light";
                lily.transform.position = center + new Vector3(
                    Mathf.Sin(index * 1.37f) * area.x * 0.5f,
                    0.17f,
                    Mathf.Cos(index * 2.07f) * area.z * 0.5f);
                lily.transform.localScale = new Vector3(0.33f + index % 3 * 0.08f, 0.025f, 0.33f + index % 3 * 0.08f);
                lily.GetComponent<Renderer>().sharedMaterial = lilyMaterial;
                Object.Destroy(lily.GetComponent<Collider>());
                lily.AddComponent<RouteGlow>().Initialize(0.025f, 0.5f + index % 4 * 0.12f);
            }
        }

        private static void CreateCrystalGarden(TetherfallGame game, Vector3 center, int count, Color color)
        {
            var crystalMaterial = RuntimeEffectMaterialCache.GetGlow(game, color, 1.25f, 0.72f);
            for (var index = 0; index < count; index++)
            {
                var crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crystal.name = "Cinder crystal";
                crystal.transform.position = center + new Vector3(
                    Mathf.Sin(index * 1.71f) * 5.4f,
                    0.35f + index % 3 * 0.14f,
                    Mathf.Cos(index * 2.29f) * 4.1f);
                crystal.transform.localScale = new Vector3(
                    0.12f + index % 3 * 0.04f,
                    0.35f + index % 4 * 0.14f,
                    0.12f + index % 3 * 0.04f);
                crystal.transform.rotation = Quaternion.Euler(index * 17f % 25f, index * 43f, index * 11f % 20f);
                crystal.GetComponent<Renderer>().sharedMaterial = crystalMaterial;
                Object.Destroy(crystal.GetComponent<Collider>());
                crystal.AddComponent<RouteGlow>().Initialize(0.055f, 0.65f + index % 5 * 0.09f);
            }
        }
    }

    public sealed class RegionalAtmosphereCuller : MonoBehaviour
    {
        private const float PollInterval = 0.75f;

        private TetherfallGame game;
        private ParticleSystem particles;
        private ParticleSystemRenderer particleRenderer;
        private Transform observer;
        private float cullDistanceSquared;
        private float nextPollAt;
        private bool applicationPaused;
        private bool simulationEnabled;
        private bool initialized;

        public void Initialize(
            TetherfallGame owner,
            ParticleSystem particleSystem,
            ParticleSystemRenderer renderer,
            Transform distanceObserver,
            float cullDistance,
            float pollOffset)
        {
            game = owner;
            particles = particleSystem;
            particleRenderer = renderer;
            observer = distanceObserver;
            cullDistanceSquared = cullDistance * cullDistance;
            nextPollAt = Time.unscaledTime + Mathf.Max(0f, pollOffset);
            initialized = true;
            RefreshState(true);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now < nextPollAt)
            {
                return;
            }

            nextPollAt = now + PollInterval;
            RefreshState(false);
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (initialized)
            {
                RefreshState(true);
            }
        }

        private void RefreshState(bool force)
        {
            if (particles == null || particleRenderer == null)
            {
                return;
            }

            if (observer == null && game != null)
            {
                observer = game.PlayerTransform;
            }

            var beyondRange = observer != null &&
                              (observer.position - transform.position).sqrMagnitude > cullDistanceSquared;
            var shouldRender = !beyondRange;
            var shouldSimulate = shouldRender &&
                                 !applicationPaused &&
                                 (game == null || !game.GameplayPaused);

            if (force || particleRenderer.enabled != shouldRender)
            {
                particleRenderer.enabled = shouldRender;
            }

            if (!force && simulationEnabled == shouldSimulate)
            {
                return;
            }

            simulationEnabled = shouldSimulate;
            if (shouldSimulate)
            {
                particles.Play(true);
            }
            else if (particles.isPlaying)
            {
                particles.Pause(true);
            }
        }
    }

    public sealed class AmbientMote : MonoBehaviour
    {
        private Vector3 anchor;
        private float amplitude;
        private float frequency;
        private float phase;

        public void Initialize(Vector3 localAnchor, float movementAmplitude, float movementFrequency, float movementPhase)
        {
            anchor = localAnchor;
            amplitude = movementAmplitude;
            frequency = movementFrequency;
            phase = movementPhase;
            transform.localPosition = anchor;
        }

        private void Update()
        {
            var time = Time.time * frequency + phase;
            transform.localPosition = anchor + new Vector3(
                Mathf.Sin(time * 0.77f) * amplitude,
                Mathf.Sin(time * 1.13f) * amplitude * 0.6f,
                Mathf.Cos(time * 0.63f) * amplitude);
        }
    }

    public sealed class RouteGlow : MonoBehaviour
    {
        private Renderer visual;
        private Vector3 anchorScale;
        private float intensity;
        private float phase;

        public void Initialize(float lightIntensity, float animationPhase)
        {
            visual = GetComponent<Renderer>();
            anchorScale = transform.localScale;
            intensity = lightIntensity;
            phase = animationPhase;
        }

        private void Update()
        {
            if (visual == null)
            {
                return;
            }

            var pulse = 0.86f + Mathf.Sin(Time.time * 2.1f + phase) * intensity;
            visual.transform.localScale = anchorScale * pulse;
        }
    }

    public sealed class WaterSurface : MonoBehaviour
    {
        private Material material;
        private Color baseColor;
        private Vector3 anchor;
        private float phase;

        public static void Attach(GameObject root, Material waterMaterial, Color color)
        {
            var surface = root.AddComponent<WaterSurface>();
            // This material remains instance-owned because Update mutates its color.
            surface.material = waterMaterial;
            surface.baseColor = color;
            surface.anchor = root.transform.position;
            surface.phase = root.transform.position.x * 0.07f + root.transform.position.z * 0.11f;
        }

        private void Update()
        {
            var shimmer = 0.72f + Mathf.Sin(Time.time * 0.9f + phase) * 0.12f;
            if (material != null)
            {
                material.color = new Color(baseColor.r * shimmer, baseColor.g * shimmer, baseColor.b * shimmer, baseColor.a);
            }

            transform.position = anchor + Vector3.up * Mathf.Sin(Time.time * 0.75f + phase) * 0.018f;
        }
    }

    public sealed class FoliageSway : MonoBehaviour
    {
        private Quaternion anchorRotation;
        private float phase;
        private float amplitude;
        private bool initialized;

        public static void Attach(GameObject root, float movementAmplitude)
        {
            var sway = root.AddComponent<FoliageSway>();
            sway.phase = root.transform.position.x * 0.31f + root.transform.position.z * 0.17f;
            sway.amplitude = movementAmplitude;
        }

        private void Start()
        {
            anchorRotation = transform.localRotation;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            var roll = Mathf.Sin(Time.time * 0.72f + phase) * amplitude;
            var pitch = Mathf.Cos(Time.time * 0.58f + phase) * amplitude * 0.38f;
            transform.localRotation = anchorRotation * Quaternion.Euler(pitch, 0f, roll);
        }
    }

    public static class RuntimeEffectMaterialCache
    {
        private const int SoftParticleTextureSize = 32;

        private static readonly Dictionary<ulong, Material> OpaqueMaterials = new Dictionary<ulong, Material>();
        private static readonly Dictionary<ulong, Material> GlowMaterials = new Dictionary<ulong, Material>();
        private static readonly Dictionary<ulong, Material> TransparentMaterials = new Dictionary<ulong, Material>();
        private static Material softParticleMaterial;
        private static Texture2D softParticleTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            OpaqueMaterials.Clear();
            GlowMaterials.Clear();
            TransparentMaterials.Clear();
            softParticleMaterial = null;
            softParticleTexture = null;
        }

        public static Material GetOpaque(
            TetherfallGame game,
            Color color,
            float metallic = 0f,
            float smoothness = 0.25f)
        {
            var key = MaterialKey(color, metallic, smoothness);
            if (!OpaqueMaterials.TryGetValue(key, out var material) || material == null)
            {
                material = game.CreateMaterial(color, metallic, smoothness);
                OpaqueMaterials[key] = material;
            }

            return material;
        }

        public static Material GetGlow(
            TetherfallGame game,
            Color color,
            float intensity = 1.4f,
            float smoothness = 0.45f)
        {
            var key = MaterialKey(color, intensity, smoothness);
            if (!GlowMaterials.TryGetValue(key, out var material) || material == null)
            {
                material = game.CreateGlowMaterial(color, intensity, smoothness);
                GlowMaterials[key] = material;
            }

            return material;
        }

        public static Material GetTransparent(TetherfallGame game, Color color, float smoothness)
        {
            var key = MaterialKey(color, smoothness, 0f);
            if (!TransparentMaterials.TryGetValue(key, out var material) || material == null)
            {
                material = game.CreateTransparentMaterial(color, smoothness);
                TransparentMaterials[key] = material;
            }

            return material;
        }

        public static Material GetSoftParticleMaterial(TetherfallGame game)
        {
            if (softParticleMaterial != null)
            {
                return softParticleMaterial;
            }

            var shader = Shader.Find("Particles/Standard Unlit") ??
                         Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
                         Shader.Find("Unlit/Transparent");
            softParticleMaterial = shader != null
                ? new Material(shader)
                : game.CreateTransparentMaterial(Color.white, 0f);
            if (softParticleMaterial == null)
            {
                return null;
            }

            softParticleMaterial.name = "Tetherfall shared soft atmosphere";
            softParticleMaterial.mainTexture = GetSoftParticleTexture();
            if (softParticleMaterial.HasProperty("_Color"))
            {
                softParticleMaterial.SetColor("_Color", Color.white);
            }

            if (softParticleMaterial.HasProperty("_TintColor"))
            {
                softParticleMaterial.SetColor("_TintColor", Color.white);
            }

            if (softParticleMaterial.HasProperty("_SrcBlend"))
            {
                softParticleMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            }

            if (softParticleMaterial.HasProperty("_DstBlend"))
            {
                softParticleMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            }

            if (softParticleMaterial.HasProperty("_ZWrite"))
            {
                softParticleMaterial.SetInt("_ZWrite", 0);
            }

            softParticleMaterial.SetOverrideTag("RenderType", "Transparent");
            softParticleMaterial.DisableKeyword("_ALPHATEST_ON");
            softParticleMaterial.EnableKeyword("_ALPHABLEND_ON");
            softParticleMaterial.renderQueue = (int)RenderQueue.Transparent;
            return softParticleMaterial;
        }

        private static Texture2D GetSoftParticleTexture()
        {
            if (softParticleTexture != null)
            {
                return softParticleTexture;
            }

            softParticleTexture = new Texture2D(
                SoftParticleTextureSize,
                SoftParticleTextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "Tetherfall soft thread particle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };

            var pixels = new Color32[SoftParticleTextureSize * SoftParticleTextureSize];
            for (var y = 0; y < SoftParticleTextureSize; y++)
            {
                for (var x = 0; x < SoftParticleTextureSize; x++)
                {
                    var normalizedX = ((x + 0.5f) / SoftParticleTextureSize - 0.5f) * 2f;
                    var normalizedY = ((y + 0.5f) / SoftParticleTextureSize - 0.5f) * 2f;
                    var radius = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                    var radial = Mathf.Clamp01(1f - radius);
                    var softCore = radial * radial * (3f - 2f * radial);
                    var thread = Mathf.Clamp01(
                        1f - Mathf.Min(Mathf.Abs(normalizedX), Mathf.Abs(normalizedY)) * 13f);
                    var alpha = Mathf.Clamp01(softCore * 0.82f + thread * radial * 0.18f);
                    pixels[y * SoftParticleTextureSize + x] = new Color32(
                        255,
                        255,
                        255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            softParticleTexture.SetPixels32(pixels);
            softParticleTexture.Apply(false, true);
            return softParticleTexture;
        }

        private static ulong MaterialKey(Color color, float firstParameter, float secondParameter)
        {
            var color32 = (Color32)color;
            return (ulong)color32.r << 40 |
                   (ulong)color32.g << 32 |
                   (ulong)color32.b << 24 |
                   (ulong)color32.a << 16 |
                   (ulong)QuantizeMaterialParameter(firstParameter) << 8 |
                   QuantizeMaterialParameter(secondParameter);
        }

        private static byte QuantizeMaterialParameter(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 64f), 0, 255);
        }
    }
}
