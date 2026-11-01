using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Adds collider-free authored procedural accents for chosen greater Pact forms.
    /// A form root is keyed by stable form ID, so applying the same form repeatedly is
    /// idempotent and safe during save restoration.
    /// </summary>
    public static class TetherfallPactFormArt
    {
        private const string RootPrefix = "Tetherfall Pact Unfolding Art";
        private static Material verificationFallbackMaterial;

        public static Transform Apply(
            TetherfallGame game,
            Transform pactRoot,
            PactFormDefinition form)
        {
            if (pactRoot == null || form == null || string.IsNullOrWhiteSpace(form.id))
            {
                return null;
            }

            var rootName = GetRootName(form.id);
            for (var index = 0; index < pactRoot.childCount; index++)
            {
                var child = pactRoot.GetChild(index);
                if (child.name.StartsWith(RootPrefix, System.StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(child.name == rootName);
                }
            }

            var existing = pactRoot.Find(rootName);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            var artRoot = new GameObject(rootName).transform;
            artRoot.gameObject.layer = pactRoot.gameObject.layer;
            artRoot.SetParent(pactRoot, false);
            artRoot.localPosition = Vector3.zero;
            artRoot.localRotation = Quaternion.identity;
            artRoot.localScale = new Vector3(
                SafeReciprocal(pactRoot.localScale.x),
                SafeReciprocal(pactRoot.localScale.y),
                SafeReciprocal(pactRoot.localScale.z));

            switch (form.id)
            {
                case TetherfallPactCatalog.GrovewardenFormId:
                    BuildGrovewarden(game, artRoot);
                    break;
                case TetherfallPactCatalog.RidgebearerFormId:
                    BuildRidgebearer(game, artRoot);
                    break;
                case TetherfallPactCatalog.RainwardenFormId:
                    BuildRainwarden(game, artRoot);
                    break;
                case TetherfallPactCatalog.SuncrestFormId:
                    BuildSuncrest(game, artRoot);
                    break;
                default:
                    Object.DestroyImmediate(artRoot.gameObject);
                    return null;
            }

            return artRoot;
        }

        public static string GetRootName(string formId)
        {
            return $"{RootPrefix} [{formId}]";
        }

        private static void BuildGrovewarden(TetherfallGame game, Transform root)
        {
            var bark = Material(game, new Color(0.2f, 0.12f, 0.055f), false);
            var leaf = Material(game, new Color(0.42f, 0.78f, 0.27f), false);
            var threadglass = Material(game, new Color(0.28f, 0.96f, 0.66f), true);

            Beam(root, "Grovewarden left crown bough", new Vector3(-0.14f, 0.56f, 0.12f),
                new Vector3(-0.82f, 1.32f, -0.04f), 0.1f, bark);
            Beam(root, "Grovewarden right crown bough", new Vector3(0.14f, 0.56f, 0.12f),
                new Vector3(0.82f, 1.32f, -0.04f), 0.1f, bark);
            Beam(root, "Grovewarden high crown bough", new Vector3(0f, 0.62f, -0.02f),
                new Vector3(0f, 1.55f, -0.2f), 0.11f, bark);
            Part(root, PrimitiveType.Cube, "Grovewarden left canopy leaf",
                new Vector3(-0.8f, 1.34f, -0.02f), new Vector3(0.62f, 0.08f, 0.82f),
                Quaternion.Euler(-18f, -24f, -14f), leaf);
            Part(root, PrimitiveType.Cube, "Grovewarden right canopy leaf",
                new Vector3(0.8f, 1.34f, -0.02f), new Vector3(0.62f, 0.08f, 0.82f),
                Quaternion.Euler(-18f, 24f, 14f), leaf);
            Part(root, PrimitiveType.Cube, "Grovewarden high canopy leaf",
                new Vector3(0f, 1.56f, -0.18f), new Vector3(0.7f, 0.08f, 0.94f),
                Quaternion.Euler(-30f, 0f, 0f), leaf);
            Part(root, PrimitiveType.Sphere, "Grovewarden heart dew",
                new Vector3(0f, 0.48f, 0.9f), new Vector3(0.2f, 0.25f, 0.12f),
                Quaternion.identity, threadglass);
            Ring(root, "Grovewarden rootguard ring", new Vector3(0f, -0.48f, 0f),
                0.92f, 0.045f, bark);
        }

        private static void BuildRidgebearer(TetherfallGame game, Transform root)
        {
            var slate = Material(game, new Color(0.34f, 0.4f, 0.5f), false);
            var edge = Material(game, new Color(0.58f, 0.62f, 0.7f), false);
            var copper = Material(game, new Color(0.78f, 0.39f, 0.14f), true);
            var threadglass = Material(game, new Color(0.28f, 0.88f, 0.78f), true);

            for (var index = 0; index < 4; index++)
            {
                Part(
                    root,
                    PrimitiveType.Cube,
                    $"Ridgebearer raised shell plate {index + 1}",
                    new Vector3(0f, 1.0f + index * 0.08f, -0.95f + index * 0.62f),
                    new Vector3(1.82f - index * 0.08f, 0.18f, 0.7f),
                    Quaternion.Euler(-5f + index * 3f, 0f, 0f),
                    index % 2 == 0 ? slate : edge);
            }

            Beam(root, "Ridgebearer left route horn", new Vector3(-0.5f, 0.42f, 1.75f),
                new Vector3(-0.78f, 1.06f, 2.2f), 0.09f, copper);
            Beam(root, "Ridgebearer right route horn", new Vector3(0.5f, 0.42f, 1.75f),
                new Vector3(0.78f, 1.06f, 2.2f), 0.09f, copper);
            Beam(root, "Ridgebearer left resonant flank", new Vector3(-1.2f, 0.18f, -1.0f),
                new Vector3(-1.2f, 0.22f, 1.0f), 0.055f, threadglass);
            Beam(root, "Ridgebearer right resonant flank", new Vector3(1.2f, 0.18f, -1.0f),
                new Vector3(1.2f, 0.22f, 1.0f), 0.055f, threadglass);
            Part(root, PrimitiveType.Cube, "Ridgebearer saddle crest",
                new Vector3(0f, 1.28f, -0.16f), new Vector3(1.1f, 0.12f, 0.34f),
                Quaternion.identity, copper);
        }

        private static void BuildRainwarden(TetherfallGame game, Transform root)
        {
            var mist = Material(game, new Color(0.55f, 0.85f, 0.88f), false);
            var copper = Material(game, new Color(0.72f, 0.42f, 0.19f), false);
            var threadglass = Material(game, new Color(0.33f, 0.96f, 0.93f), true);

            Ring(root, "Rainwarden broad halo", new Vector3(0f, 0.82f, -0.04f),
                0.94f, 0.045f, copper);
            Ring(root, "Rainwarden lower current ring", new Vector3(0f, -0.2f, -0.08f),
                0.72f, 0.032f, threadglass);
            Part(root, PrimitiveType.Cube, "Rainwarden left veil wing",
                new Vector3(-0.9f, 0.08f, -0.06f), new Vector3(0.84f, 0.06f, 1.2f),
                Quaternion.Euler(-24f, -62f, -18f), mist);
            Part(root, PrimitiveType.Cube, "Rainwarden right veil wing",
                new Vector3(0.9f, 0.08f, -0.06f), new Vector3(0.84f, 0.06f, 1.2f),
                Quaternion.Euler(-24f, 62f, 18f), mist);
            Part(root, PrimitiveType.Sphere, "Rainwarden high rain pearl",
                new Vector3(-0.72f, 1.12f, -0.14f), Vector3.one * 0.18f,
                Quaternion.identity, threadglass);
            Part(root, PrimitiveType.Sphere, "Rainwarden low rain pearl",
                new Vector3(0.78f, -0.18f, 0.06f), Vector3.one * 0.14f,
                Quaternion.identity, threadglass);
            Beam(root, "Rainwarden left halo stay", new Vector3(-0.9f, 0.82f, -0.04f),
                new Vector3(-0.38f, 0.18f, 0f), 0.035f, copper);
            Beam(root, "Rainwarden right halo stay", new Vector3(0.9f, 0.82f, -0.04f),
                new Vector3(0.38f, 0.18f, 0f), 0.035f, copper);
        }

        private static void BuildSuncrest(TetherfallGame game, Transform root)
        {
            var ember = Material(game, new Color(0.9f, 0.22f, 0.07f), false);
            var hot = Material(game, new Color(1f, 0.54f, 0.12f), true);
            var copper = Material(game, new Color(0.76f, 0.32f, 0.1f), false);

            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                for (var feather = 0; feather < 3; feather++)
                {
                    var x = side * (0.86f + feather * 0.34f);
                    var z = 0.02f - feather * 0.2f;
                    Part(
                        root,
                        PrimitiveType.Cube,
                        side < 0f
                            ? $"Suncrest left greater wing feather {feather + 1}"
                            : $"Suncrest right greater wing feather {feather + 1}",
                        new Vector3(x, 0.16f - feather * 0.04f, z),
                        new Vector3(0.56f, 0.06f, 1.36f + feather * 0.16f),
                        Quaternion.Euler(
                            4f,
                            side * (72f + feather * 10f),
                            side * (12f + feather * 3f)),
                        feather == 2 ? hot : ember);
                    Beam(
                        root,
                        side < 0f
                            ? $"Suncrest left bright wing quill {feather + 1}"
                            : $"Suncrest right bright wing quill {feather + 1}",
                        new Vector3(side * 0.3f, 0.14f, 0.08f),
                        new Vector3(x * 1.22f, 0.14f, z - 0.3f),
                        0.035f,
                        feather == 2 ? hot : copper);
                }
            }

            Part(root, PrimitiveType.Cube, "Suncrest high ember crest",
                new Vector3(0f, 0.92f, 0.34f), new Vector3(0.3f, 0.07f, 0.92f),
                Quaternion.Euler(-62f, 0f, 0f), hot);
            Ring(root, "Suncrest thermal crown", new Vector3(0f, 0.32f, -0.12f),
                0.72f, 0.04f, hot);
            Part(root, PrimitiveType.Sphere, "Suncrest ember heart",
                new Vector3(0f, -0.08f, 0.48f), new Vector3(0.32f, 0.38f, 0.26f),
                Quaternion.identity, hot);
        }

        private static void Ring(
            Transform parent,
            string name,
            Vector3 center,
            float radius,
            float width,
            Material material)
        {
            const int segments = 12;
            var previous = center + new Vector3(radius, 0f, 0f);
            for (var index = 1; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var next = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                Beam(parent, $"{name} {index}", previous, next, width, material);
                previous = next;
            }
        }

        private static void Beam(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            Material material)
        {
            var direction = end - start;
            var length = direction.magnitude;
            if (length <= 0.0001f)
            {
                return;
            }

            Part(
                parent,
                PrimitiveType.Cylinder,
                name,
                (start + end) * 0.5f,
                new Vector3(width, length * 0.5f, width),
                Quaternion.FromToRotation(Vector3.up, direction / length),
                material);
        }

        private static Transform Part(
            Transform parent,
            PrimitiveType primitive,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var item = GameObject.CreatePrimitive(primitive);
            item.name = name;
            item.layer = parent.gameObject.layer;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.transform.localRotation = localRotation;
            var collider = item.GetComponent<Collider>();
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

            var renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return item.transform;
        }

        private static Material Material(TetherfallGame game, Color color, bool glow)
        {
            if (game != null)
            {
                return glow
                    ? game.CreateGlowMaterial(color, 1.65f, 0.62f)
                    : game.CreateMaterial(color, 0.08f, 0.38f);
            }

            if (verificationFallbackMaterial == null)
            {
                var shader = Shader.Find("Standard") ??
                             Shader.Find("Universal Render Pipeline/Lit") ??
                             Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    return null;
                }

                verificationFallbackMaterial = new Material(shader)
                {
                    name = "Tetherfall Pact form verification material",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            verificationFallbackMaterial.color = color;
            return verificationFallbackMaterial;
        }

        private static float SafeReciprocal(float value)
        {
            return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
        }
    }
}
