using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Small articulated rig driven by CapturedPactActor. It has no component Update,
    /// physics body, light, animator controller, or runtime-instantiated frame data.
    /// </summary>
    public sealed class CapturedPactArtRig
    {
        private readonly Transform artRoot;
        private readonly Transform body;
        private readonly Transform head;
        private readonly Transform tailNear;
        private readonly Transform tailFar;
        private readonly Transform[] movingAppendages;
        private readonly Quaternion[] appendageRestRotations;
        private readonly Material[] ownedMaterials;
        private readonly Vector3 bodyRestPosition;
        private readonly Quaternion headRestRotation;
        private readonly Quaternion tailNearRestRotation;
        private readonly Quaternion tailFarRestRotation;
        private bool disposed;

        internal CapturedPactArtRig(
            Transform artRoot,
            Transform body,
            Transform head,
            Transform tailNear,
            Transform tailFar,
            Transform[] movingAppendages,
            Material[] ownedMaterials,
            int rendererCount)
        {
            this.artRoot = artRoot;
            this.body = body;
            this.head = head;
            this.tailNear = tailNear;
            this.tailFar = tailFar;
            this.movingAppendages = movingAppendages ?? new Transform[0];
            this.ownedMaterials = ownedMaterials ?? new Material[0];
            RendererCount = rendererCount;
            bodyRestPosition = body != null ? body.localPosition : Vector3.zero;
            headRestRotation = head != null ? head.localRotation : Quaternion.identity;
            tailNearRestRotation = tailNear != null
                ? tailNear.localRotation
                : Quaternion.identity;
            tailFarRestRotation = tailFar != null
                ? tailFar.localRotation
                : Quaternion.identity;
            appendageRestRotations = new Quaternion[this.movingAppendages.Length];
            for (var index = 0; index < this.movingAppendages.Length; index++)
            {
                appendageRestRotations[index] = this.movingAppendages[index] != null
                    ? this.movingAppendages[index].localRotation
                    : Quaternion.identity;
            }
        }

        public int RendererCount { get; }
        public int MaterialCount => ownedMaterials.Length;
        public Transform ArtRoot => artRoot;

        public void Tick(float time, Vector3 velocity, bool commandFocus)
        {
            if (disposed || artRoot == null)
            {
                return;
            }

            var planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            var effort = Mathf.Clamp01(planarSpeed / 7f);
            var cadence = time * Mathf.Lerp(2.4f, 8.2f, effort);
            if (body != null)
            {
                body.localPosition = bodyRestPosition +
                                     Vector3.up *
                                     (Mathf.Sin(cadence * 2f) *
                                      Mathf.Lerp(0.012f, 0.055f, effort));
            }

            if (head != null)
            {
                var listeningYaw = Mathf.Sin(time * 1.35f) * (commandFocus ? 8f : 3f);
                var listeningPitch = commandFocus ? -5f : Mathf.Sin(time * 0.9f) * 2f;
                head.localRotation = headRestRotation *
                                     Quaternion.Euler(
                                         listeningPitch,
                                         listeningYaw,
                                         0f);
            }

            for (var index = 0; index < movingAppendages.Length; index++)
            {
                var appendage = movingAppendages[index];
                if (appendage == null)
                {
                    continue;
                }

                var side = (index & 1) == 0 ? 1f : -1f;
                var phase = cadence + index * Mathf.PI * 0.5f;
                var sweep = Mathf.Sin(phase) * Mathf.Lerp(4f, 28f, effort);
                appendage.localRotation = appendageRestRotations[index] *
                                           Quaternion.Euler(sweep * side, 0f, sweep * 0.14f);
            }

            var tailSweep = Mathf.Sin(time * 2.1f + planarSpeed * 0.18f) *
                            (commandFocus ? 18f : 11f);
            if (tailNear != null)
            {
                tailNear.localRotation = tailNearRestRotation *
                                         Quaternion.Euler(0f, tailSweep, 0f);
            }

            if (tailFar != null)
            {
                tailFar.localRotation = tailFarRestRotation *
                                        Quaternion.Euler(0f, -tailSweep * 1.35f, 0f);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            for (var index = 0; index < ownedMaterials.Length; index++)
            {
                var material = ownedMaterials[index];
                if (material == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(material);
                }
                else
                {
                    Object.DestroyImmediate(material);
                }

                ownedMaterials[index] = null;
            }
        }
    }

    public static class TetherfallCapturedPactArt
    {
        public const int MaximumRenderersPerActor = 24;
        public const int MaximumMaterialsPerActor = 4;
        public const int MaximumLightsPerActor = 0;
        public const int MaximumCollidersPerActor = 0;

        public static CapturedPactArtRig Build(
            TetherfallGame game,
            Transform actorRoot,
            PactSpeciesDefinition species)
        {
            if (actorRoot == null || species?.art == null)
            {
                return null;
            }

            var art = species.art;
            var materials = new[]
            {
                CreateMaterial(game, art.surfaceColor, false),
                CreateMaterial(game, art.accentColor, false),
                CreateMaterial(game, art.resonanceColor, true),
                CreateMaterial(game, art.eyeColor, true)
            };
            var rendererCount = 0;
            var artRoot = new GameObject("Original Accord fallback art").transform;
            artRoot.SetParent(actorRoot, false);
            artRoot.localPosition = Vector3.zero;
            artRoot.localRotation = Quaternion.identity;
            artRoot.localScale = Vector3.one * 0.88f;

            var bodyHeight = art.bodyPlan == PactArtBodyPlan.Finned ? 0.82f : 0.92f;
            var body = Part(
                artRoot,
                PrimitiveType.Sphere,
                "Layered body mass",
                new Vector3(0f, bodyHeight, 0f),
                art.bodyScale,
                Quaternion.identity,
                materials[0],
                ref rendererCount);
            Part(
                body,
                PrimitiveType.Sphere,
                "Shoulder mantle",
                new Vector3(0f, 0.08f, 0.28f),
                new Vector3(0.9f, 0.82f, 0.72f),
                Quaternion.identity,
                materials[1],
                ref rendererCount);

            var headPosition = art.bodyPlan == PactArtBodyPlan.Burrower
                ? new Vector3(0f, bodyHeight + 0.1f, 0.72f)
                : new Vector3(0f, bodyHeight + 0.27f, 0.7f);
            var head = Part(
                artRoot,
                PrimitiveType.Sphere,
                "Listening head",
                headPosition,
                art.headScale,
                Quaternion.identity,
                materials[0],
                ref rendererCount);
            Part(
                head,
                PrimitiveType.Sphere,
                "Resonance muzzle",
                new Vector3(0f, -0.08f, 0.48f),
                new Vector3(0.58f, 0.42f, 0.54f),
                Quaternion.identity,
                materials[1],
                ref rendererCount);
            BuildEyes(head, materials[3], ref rendererCount);
            BuildCrest(head, art, materials[2], ref rendererCount);

            var appendages = new Transform[4];
            switch (art.bodyPlan)
            {
                case PactArtBodyPlan.Winged:
                    BuildWings(
                        artRoot,
                        materials[1],
                        materials[2],
                        appendages,
                        ref rendererCount,
                        false);
                    break;
                case PactArtBodyPlan.Finned:
                    BuildWings(
                        artRoot,
                        materials[1],
                        materials[2],
                        appendages,
                        ref rendererCount,
                        true);
                    break;
                case PactArtBodyPlan.Kitelizard:
                    BuildKiteSails(
                        artRoot,
                        materials[1],
                        materials[2],
                        appendages,
                        ref rendererCount);
                    break;
                default:
                    BuildLegs(
                        artRoot,
                        art,
                        materials[0],
                        materials[1],
                        appendages,
                        ref rendererCount);
                    break;
            }

            var tailNear = Beam(
                artRoot,
                "Tail near joint",
                new Vector3(0f, bodyHeight, -0.58f),
                new Vector3(0f, bodyHeight - 0.08f, -0.58f - art.tailLength * 0.5f),
                art.bodyPlan == PactArtBodyPlan.Kitelizard ? 0.13f : 0.16f,
                materials[1],
                ref rendererCount);
            var tailFar = Beam(
                tailNear,
                "Tail far joint",
                Vector3.zero,
                new Vector3(0f, 0.02f, -art.tailLength * 0.52f),
                art.bodyPlan == PactArtBodyPlan.Kitelizard ? 0.09f : 0.12f,
                materials[2],
                ref rendererCount);

            Beam(
                body,
                "Left resonance seam",
                new Vector3(-0.34f, 0.12f, -0.3f),
                new Vector3(-0.3f, 0.14f, 0.32f),
                0.035f,
                materials[2],
                ref rendererCount);
            Beam(
                body,
                "Right resonance seam",
                new Vector3(0.34f, 0.12f, -0.3f),
                new Vector3(0.3f, 0.14f, 0.32f),
                0.035f,
                materials[2],
                ref rendererCount);

            return new CapturedPactArtRig(
                artRoot,
                body,
                head,
                tailNear,
                tailFar,
                appendages,
                materials,
                rendererCount);
        }

        private static void BuildEyes(
            Transform head,
            Material eyeMaterial,
            ref int rendererCount)
        {
            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                Part(
                    head,
                    PrimitiveType.Sphere,
                    side < 0f ? "Left clear eye" : "Right clear eye",
                    new Vector3(side * 0.28f, 0.12f, 0.4f),
                    Vector3.one * 0.11f,
                    Quaternion.identity,
                    eyeMaterial,
                    ref rendererCount);
            }
        }

        private static void BuildCrest(
            Transform head,
            PactArtProfile art,
            Material resonanceMaterial,
            ref int rendererCount)
        {
            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                Beam(
                    head,
                    side < 0f ? "Left listening crest" : "Right listening crest",
                    new Vector3(side * 0.18f, 0.34f, 0.02f),
                    new Vector3(
                        side * (0.3f + art.crestHeight * 0.24f),
                        0.36f + art.crestHeight,
                        -0.08f),
                    0.07f,
                    resonanceMaterial,
                    ref rendererCount);
            }
        }

        private static void BuildLegs(
            Transform root,
            PactArtProfile art,
            Material limbMaterial,
            Material footMaterial,
            Transform[] appendages,
            ref int rendererCount)
        {
            for (var index = 0; index < 4; index++)
            {
                var left = (index & 1) == 0;
                var front = index < 2;
                var x = left ? -0.42f : 0.42f;
                var z = front ? 0.42f : -0.42f;
                var upper = Beam(
                    root,
                    $"{(left ? "Left" : "Right")} {(front ? "front" : "rear")} stride",
                    new Vector3(x, 0.82f, z),
                    new Vector3(
                        x * 1.06f,
                        Mathf.Max(0.12f, 0.82f - art.legLength),
                        z + (front ? 0.08f : -0.08f)),
                    art.bodyPlan == PactArtBodyPlan.Burrower ? 0.19f : 0.13f,
                    limbMaterial,
                    ref rendererCount);
                appendages[index] = upper;
                Part(
                    upper,
                    PrimitiveType.Sphere,
                    "Broad route foot",
                    new Vector3(0f, -art.legLength * 0.48f, 0.05f),
                    new Vector3(0.24f, 0.12f, 0.3f),
                    Quaternion.identity,
                    footMaterial,
                    ref rendererCount);
            }
        }

        private static void BuildWings(
            Transform root,
            Material wingMaterial,
            Material edgeMaterial,
            Transform[] appendages,
            ref int rendererCount,
            bool fins)
        {
            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                var near = Beam(
                    root,
                    side < 0f ? "Left near sail" : "Right near sail",
                    new Vector3(side * 0.36f, 0.98f, 0.18f),
                    new Vector3(side * (fins ? 0.98f : 1.18f), fins ? 0.9f : 1.08f, -0.02f),
                    fins ? 0.18f : 0.14f,
                    wingMaterial,
                    ref rendererCount);
                var far = Beam(
                    near,
                    "Faceted outer sail",
                    Vector3.zero,
                    new Vector3(side * (fins ? 0.62f : 0.78f), -0.08f, -0.34f),
                    fins ? 0.13f : 0.1f,
                    edgeMaterial,
                    ref rendererCount);
                appendages[sideIndex * 2] = near;
                appendages[sideIndex * 2 + 1] = far;
            }
        }

        private static void BuildKiteSails(
            Transform root,
            Material sailMaterial,
            Material edgeMaterial,
            Transform[] appendages,
            ref int rendererCount)
        {
            BuildWings(
                root,
                sailMaterial,
                edgeMaterial,
                appendages,
                ref rendererCount,
                true);
        }

        private static Transform Beam(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            Material material,
            ref int rendererCount)
        {
            var direction = end - start;
            var length = direction.magnitude;
            if (length <= 0.001f)
            {
                direction = Vector3.up;
                length = 0.001f;
            }

            return Part(
                parent,
                PrimitiveType.Cylinder,
                name,
                (start + end) * 0.5f,
                new Vector3(width, length * 0.5f, width),
                Quaternion.FromToRotation(Vector3.up, direction / length),
                material,
                ref rendererCount);
        }

        private static Transform Part(
            Transform parent,
            PrimitiveType primitive,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            ref int rendererCount)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                rendererCount++;
            }

            return part.transform;
        }

        private static Material CreateMaterial(
            TetherfallGame game,
            Color color,
            bool glow)
        {
            if (game != null)
            {
                return glow
                    ? game.CreateGlowMaterial(color, 1.5f, 0.62f)
                    : game.CreateMaterial(color, 0.08f, 0.38f);
            }

            var shader = Shader.Find("Standard") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Unlit/Color");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "Tetherfall Accord fallback verification material",
                hideFlags = HideFlags.HideAndDontSave,
                color = color
            };
            if (glow && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }

            return material;
        }
    }
}
