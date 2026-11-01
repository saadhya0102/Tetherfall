using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public static class FenlightCausewayPresentationBuilder
    {
        private static bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            built = false;
        }

        public static void Build(TetherfallGame game)
        {
            if (built || game == null)
            {
                return;
            }

            built = true;
            var root = new GameObject("Fenlight Causeway presentation");
            root.transform.position = RestorationCatalog.Get(RestorationCatalog.FenlightCausewayId).position;
            root.AddComponent<FenlightCausewayPresentation>().Initialize(game);
        }
    }

    public sealed class FenlightCausewayPresentation : MonoBehaviour
    {
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly List<Transform> animatedAccents = new List<Transform>();
        private readonly GameObject[] stateRoots = new GameObject[4];

        private TetherfallGame game;
        private FenlightPalette palette;
        private int currentTier = -1;

        public int CurrentTier => currentTier;

        public void Initialize(TetherfallGame owner)
        {
            game = owner;
            palette = new FenlightPalette(game, ownedMaterials);
            BuildStateZero();
            BuildStateOne();
            BuildStateTwo();
            BuildStateThree();
            ApplyTier(Mathf.Clamp(game.FenlightTier, 0, 3));
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            var desiredTier = Mathf.Clamp(game.FenlightTier, 0, 3);
            if (desiredTier != currentTier)
            {
                ApplyTier(desiredTier);
            }

            for (var index = animatedAccents.Count - 1; index >= 0; index--)
            {
                var accent = animatedAccents[index];
                if (accent == null)
                {
                    animatedAccents.RemoveAt(index);
                    continue;
                }

                if (!accent.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var phase = Time.time * 1.6f + index * 0.73f;
                var pulse = 0.92f + Mathf.Sin(phase) * 0.1f;
                accent.localScale = Vector3.one * pulse;
                accent.Rotate(0f, 32f * Time.deltaTime, 0f, Space.Self);
            }
        }

        private void OnDestroy()
        {
            foreach (var material in ownedMaterials)
            {
                if (material != null)
                {
                    Object.Destroy(material);
                }
            }

            ownedMaterials.Clear();
        }

        private void ApplyTier(int tier)
        {
            currentTier = tier;
            for (var index = 0; index < stateRoots.Length; index++)
            {
                if (stateRoots[index] != null)
                {
                    stateRoots[index].SetActive(index == tier);
                }
            }
        }

        private void BuildStateZero()
        {
            var root = CreateStateRoot(0, "Drowned route");
            CreatePrimitive(
                PrimitiveType.Cube,
                "Fenlight submerged beam",
                root.transform,
                new Vector3(-1.5f, 0.32f, -0.4f),
                new Vector3(7.2f, 0.22f, 0.58f),
                Quaternion.Euler(4f, -12f, 8f),
                palette.drownedTimber);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Fenlight broken deck",
                root.transform,
                new Vector3(2.45f, 0.19f, 0.48f),
                new Vector3(3.1f, 0.16f, 1.1f),
                Quaternion.Euler(-3f, 9f, -6f),
                palette.drownedTimber);

            for (var index = 0; index < 4; index++)
            {
                var post = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Fenlight leaning mooring",
                    root.transform,
                    new Vector3(-3.2f + index * 2.1f, 0.92f + index % 2 * 0.12f, index % 2 == 0 ? -0.92f : 0.96f),
                    new Vector3(0.15f, 0.92f, 0.15f),
                    Quaternion.Euler(index % 2 == 0 ? 9f : -11f, index * 19f, index % 2 == 0 ? -8f : 7f),
                    palette.drownedTimber);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Fenlight drowned cap",
                    post.transform,
                    new Vector3(0f, 0.98f, 0f),
                    Vector3.one * 0.82f,
                    Quaternion.identity,
                    palette.peatStone);
            }

            CreateReedClump(root.transform, new Vector3(-4.2f, 0f, 1.56f), 3, palette.reed);
            CreateReedClump(root.transform, new Vector3(3.75f, 0f, -1.45f), 3, palette.reed);
            CreateReedClump(root.transform, new Vector3(4.7f, 0f, 1.26f), 3, palette.reed);
        }

        private void BuildStateOne()
        {
            var root = CreateStateRoot(1, "Rehung lantern chain");
            CreatePrimitive(
                PrimitiveType.Cube,
                "Fenlight chain spine",
                root.transform,
                new Vector3(0f, 2.48f, 0.45f),
                new Vector3(7.8f, 0.055f, 0.055f),
                Quaternion.Euler(0f, 0f, -4f),
                palette.copper);

            for (var index = 0; index < 4; index++)
            {
                var x = -3.15f + index * 2.1f;
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Fenlight chain post",
                    root.transform,
                    new Vector3(x, 1.3f, 0.45f),
                    new Vector3(0.16f, 1.3f, 0.16f),
                    Quaternion.Euler(0f, index * 9f, 0f),
                    palette.drownedTimber);
                CreateLantern(root.transform, new Vector3(x + 0.5f, 2.08f + index % 2 * 0.16f, 0.45f), index);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Fenlight safe edge",
                root.transform,
                new Vector3(0.1f, 0.46f, -1.12f),
                new Vector3(7.3f, 0.1f, 0.13f),
                Quaternion.Euler(0f, -2f, 0f),
                palette.copper);
            CreateReedClump(root.transform, new Vector3(-4.25f, 0f, 1.55f), 3, palette.reed);
            CreateReedClump(root.transform, new Vector3(4.05f, 0f, -1.46f), 3, palette.reed);
        }

        private void BuildStateTwo()
        {
            var root = CreateStateRoot(2, "Defended signal");
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Fenlight defended plinth",
                root.transform,
                new Vector3(0f, 0.52f, 0f),
                new Vector3(1.1f, 0.52f, 1.1f),
                Quaternion.identity,
                palette.peatStone);
            var signal = CreatePrimitive(
                PrimitiveType.Sphere,
                "Fenlight exposed signal",
                root.transform,
                new Vector3(0f, 2.18f, 0f),
                Vector3.one * 0.7f,
                Quaternion.identity,
                palette.tealGlow);
            animatedAccents.Add(signal.transform);

            for (var index = 0; index < 3; index++)
            {
                var angle = (index * 120f + 15f) * Mathf.Deg2Rad;
                var shard = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Fenlight warning shard",
                    root.transform,
                    new Vector3(Mathf.Cos(angle) * 1.18f, 1.52f + index % 2 * 0.22f, Mathf.Sin(angle) * 1.18f),
                    new Vector3(0.14f, 0.62f, 0.14f),
                    Quaternion.Euler(20f, index * 120f, 19f),
                    palette.warningGlow);
                animatedAccents.Add(shard.transform);
            }

            var wake = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Fenlight breach wake",
                root.transform,
                new Vector3(0f, 0.07f, 0f),
                new Vector3(3.9f, 0.025f, 3.9f),
                Quaternion.identity,
                palette.waterWake);
            animatedAccents.Add(wake.transform);
            CreateLantern(root.transform, new Vector3(-3.25f, 2.04f, 0.78f), 1);
            CreateLantern(root.transform, new Vector3(3.25f, 2.18f, -0.76f), 2);
        }

        private void BuildStateThree()
        {
            var root = CreateStateRoot(3, "Raised causeway");
            CreatePrimitive(
                PrimitiveType.Cube,
                "Fenlight repaired deck dressing",
                root.transform,
                new Vector3(0f, 1.37f, -0.1f),
                new Vector3(8.3f, 0.08f, 2.38f),
                Quaternion.identity,
                palette.drownedTimber);

            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Fenlight copper rail dressing",
                    root.transform,
                    new Vector3(0f, 2.04f, side * 1.07f),
                    new Vector3(8.18f, 0.09f, 0.09f),
                    Quaternion.identity,
                    palette.copper);
                for (var index = 0; index < 4; index++)
                {
                    CreatePrimitive(
                        PrimitiveType.Cylinder,
                        "Fenlight rail upright",
                        root.transform,
                        new Vector3(-3.45f + index * 2.3f, 1.76f, side * 1.07f),
                        new Vector3(0.075f, 0.4f, 0.075f),
                        Quaternion.identity,
                        palette.copper);
                }
            }

            for (var index = 0; index < 4; index++)
            {
                var marker = CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Fenlight route rhythm",
                    root.transform,
                    new Vector3(-2.85f + index * 1.9f, 1.76f, index % 2 == 0 ? -1.16f : 1.16f),
                    Vector3.one * 0.18f,
                    Quaternion.identity,
                    palette.tealGlow);
                animatedAccents.Add(marker.transform);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Fenlight repaired crossbeam",
                root.transform,
                new Vector3(-3.2f, 0.72f, -0.1f),
                new Vector3(0.36f, 0.74f, 2.2f),
                Quaternion.Euler(0f, 0f, -6f),
                palette.drownedTimber);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Fenlight repaired crossbeam",
                root.transform,
                new Vector3(3.2f, 0.72f, -0.1f),
                new Vector3(0.36f, 0.74f, 2.2f),
                Quaternion.Euler(0f, 0f, 6f),
                palette.drownedTimber);
            CreateReedClump(root.transform, new Vector3(-4.45f, 0f, 1.5f), 2, palette.reed);
            CreateReedClump(root.transform, new Vector3(4.42f, 0f, -1.42f), 2, palette.reed);
        }

        private GameObject CreateStateRoot(int tier, string title)
        {
            var root = new GameObject($"Fenlight art tier {tier} · {title}");
            root.transform.SetParent(transform, false);
            stateRoots[tier] = root;
            return root;
        }

        private GameObject CreateLantern(Transform parent, Vector3 localPosition, int index)
        {
            var lantern = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Fenlight route lantern",
                parent,
                localPosition,
                new Vector3(0.16f, 0.36f, 0.16f),
                Quaternion.Euler(0f, index * 24f, 0f),
                palette.tealGlow);
            var cap = CreatePrimitive(
                PrimitiveType.Sphere,
                "Fenlight lantern cap",
                lantern.transform,
                new Vector3(0f, 0.95f, 0f),
                Vector3.one * 0.82f,
                Quaternion.identity,
                palette.copper);
            cap.transform.localScale = Vector3.one * 0.13f;
            animatedAccents.Add(lantern.transform);
            return lantern;
        }

        private static void CreateReedClump(Transform parent, Vector3 center, int count, Material material)
        {
            for (var index = 0; index < count; index++)
            {
                var angle = index * Mathf.PI * 2f / count;
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Fenlight reed dressing",
                    parent,
                    center + new Vector3(Mathf.Cos(angle) * 0.34f, 0.42f + index % 3 * 0.1f, Mathf.Sin(angle) * 0.28f),
                    new Vector3(0.028f, 0.42f + index % 2 * 0.12f, 0.028f),
                    Quaternion.Euler(index % 2 == 0 ? 9f : -8f, index * 41f, index % 3 * 5f),
                    material);
            }
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitive,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var visual = GameObject.CreatePrimitive(primitive);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.transform.localRotation = localRotation;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(visual.GetComponent<Collider>());
            return visual;
        }

        private sealed class FenlightPalette
        {
            public readonly Material drownedTimber;
            public readonly Material peatStone;
            public readonly Material reed;
            public readonly Material copper;
            public readonly Material tealGlow;
            public readonly Material warningGlow;
            public readonly Material waterWake;

            public FenlightPalette(TetherfallGame game, List<Material> ownedMaterials)
            {
                drownedTimber = Track(
                    game.CreateMaterial(new Color(0.18f, 0.25f, 0.24f), 0.08f, 0.32f),
                    ownedMaterials);
                peatStone = Track(
                    game.CreateMaterial(new Color(0.16f, 0.29f, 0.31f), 0.04f, 0.38f),
                    ownedMaterials);
                reed = Track(
                    game.CreateMaterial(new Color(0.18f, 0.38f, 0.31f), 0f, 0.24f),
                    ownedMaterials);
                copper = Track(
                    game.CreateMaterial(new Color(0.58f, 0.36f, 0.17f), 0.34f, 0.56f),
                    ownedMaterials);
                tealGlow = Track(
                    game.CreateGlowMaterial(new Color(0.29f, 0.84f, 0.78f), 1.65f, 0.7f),
                    ownedMaterials);
                warningGlow = Track(
                    game.CreateGlowMaterial(new Color(0.92f, 0.46f, 0.18f), 1.6f, 0.62f),
                    ownedMaterials);
                waterWake = Track(
                    game.CreateTransparentMaterial(new Color(0.2f, 0.78f, 0.75f, 0.28f), 0.66f),
                    ownedMaterials);
            }

            private static Material Track(Material material, List<Material> ownedMaterials)
            {
                if (material != null)
                {
                    ownedMaterials.Add(material);
                }

                return material;
            }
        }
    }
}
