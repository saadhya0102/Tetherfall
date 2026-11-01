using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Builds a lightweight articulated resident silhouette for the city crowd.
    /// The named collider-free pivots are consumed by HumanProceduralAnimator.
    /// </summary>
    public static class TetherfallCityResidentArt
    {
        private static readonly Dictionary<int, Material> Materials =
            new Dictionary<int, Material>();

        public static Transform Build(
            TetherfallGame game,
            Transform actorRoot,
            Color accent,
            string role,
            string identity)
        {
            if (game == null || actorRoot == null)
            {
                return null;
            }

            var visual = new GameObject("Resident Visual").transform;
            visual.SetParent(actorRoot, false);

            var seed = StableHash(identity);
            var warmSkin = Color.Lerp(
                new Color(0.38f, 0.2f, 0.14f),
                new Color(0.92f, 0.67f, 0.48f),
                0.24f + Mathf.Abs(seed % 61) / 100f);
            var cloth = Color.Lerp(accent, new Color(0.16f, 0.2f, 0.23f), 0.28f);
            var clothShadow = Color.Lerp(cloth, Color.black, 0.25f);
            var trim = Color.Lerp(accent, new Color(0.96f, 0.79f, 0.44f), 0.38f);
            var hair = Color.Lerp(
                new Color(0.1f, 0.065f, 0.05f),
                new Color(0.42f, 0.22f, 0.11f),
                Mathf.Abs(seed % 47) / 46f);

            var skinMaterial = GetMaterial(game, warmSkin, 0f, 0.42f);
            var clothMaterial = GetMaterial(game, cloth, 0f, 0.34f);
            var clothShadowMaterial = GetMaterial(game, clothShadow, 0f, 0.27f);
            var trimMaterial = GetMaterial(game, trim, 0.1f, 0.48f);
            var hairMaterial = GetMaterial(game, hair, 0f, 0.24f);
            var leatherMaterial = GetMaterial(game, new Color(0.22f, 0.115f, 0.065f), 0f, 0.3f);
            var eyeMaterial = GetMaterial(game, new Color(0.025f, 0.04f, 0.045f), 0f, 0.6f);

            var pelvis = CreatePivot("Rig Root - Pelvis", visual, new Vector3(0f, -0.24f, 0f));
            CreateMesh(
                "Layered belt coat",
                PrimitiveType.Cube,
                pelvis,
                new Vector3(0f, 0.03f, 0f),
                new Vector3(0.48f, 0.3f, 0.32f),
                clothShadowMaterial);
            CreateMesh(
                "Threadglass belt",
                PrimitiveType.Cube,
                pelvis,
                new Vector3(0f, 0.15f, 0.01f),
                new Vector3(0.52f, 0.075f, 0.34f),
                trimMaterial);

            var chest = CreatePivot("Rig Root - Chest", visual, new Vector3(0f, 0.2f, 0f));
            CreateMesh(
                "Tailored torso",
                PrimitiveType.Cube,
                chest,
                Vector3.zero,
                new Vector3(0.54f, 0.6f, 0.34f),
                clothMaterial);
            CreateMesh(
                "Cross-body sash",
                PrimitiveType.Cube,
                chest,
                new Vector3(0.02f, 0.01f, 0.185f),
                new Vector3(0.14f, 0.63f, 0.035f),
                trimMaterial,
                Quaternion.Euler(0f, 0f, -24f));
            CreateMesh(
                "Short mantle",
                PrimitiveType.Cube,
                chest,
                new Vector3(0f, 0.17f, -0.2f),
                new Vector3(0.62f, 0.25f, 0.08f),
                clothShadowMaterial,
                Quaternion.Euler(12f, 0f, 0f));

            BuildArm(
                chest,
                -1f,
                skinMaterial,
                clothMaterial,
                leatherMaterial);
            BuildArm(
                chest,
                1f,
                skinMaterial,
                clothMaterial,
                leatherMaterial);
            BuildLeg(
                pelvis,
                -1f,
                clothShadowMaterial,
                leatherMaterial);
            BuildLeg(
                pelvis,
                1f,
                clothShadowMaterial,
                leatherMaterial);

            var head = CreatePivot("Rig Root - Head", visual, new Vector3(0f, 0.8f, 0f));
            CreateMesh(
                "Resident head",
                PrimitiveType.Sphere,
                head,
                Vector3.zero,
                new Vector3(0.34f, 0.39f, 0.32f),
                skinMaterial);
            CreateMesh(
                "Nose",
                PrimitiveType.Sphere,
                head,
                new Vector3(0f, -0.015f, 0.295f),
                new Vector3(0.07f, 0.09f, 0.08f),
                skinMaterial);
            CreateMesh(
                "Left eye",
                PrimitiveType.Sphere,
                head,
                new Vector3(-0.105f, 0.055f, 0.292f),
                new Vector3(0.045f, 0.052f, 0.035f),
                eyeMaterial);
            CreateMesh(
                "Right eye",
                PrimitiveType.Sphere,
                head,
                new Vector3(0.105f, 0.055f, 0.292f),
                new Vector3(0.045f, 0.052f, 0.035f),
                eyeMaterial);
            BuildHair(head, hairMaterial, seed);
            BuildRoleDressing(
                role,
                seed,
                chest,
                pelvis,
                trimMaterial,
                leatherMaterial,
                clothShadowMaterial);
            return visual;
        }

        public static Transform BuildCrowd(
            TetherfallGame game,
            Transform actorRoot,
            Color accent,
            string identity)
        {
            if (game == null || actorRoot == null)
            {
                return null;
            }

            var visual = new GameObject("Resident Crowd Visual").transform;
            visual.SetParent(actorRoot, false);
            var seed = StableHash(identity);
            var skin = Color.Lerp(
                new Color(0.4f, 0.22f, 0.15f),
                new Color(0.9f, 0.66f, 0.48f),
                0.28f + Mathf.Abs(seed % 53) / 100f);
            var cloth = Color.Lerp(accent, new Color(0.13f, 0.17f, 0.2f), 0.22f);
            var shadow = Color.Lerp(cloth, Color.black, 0.28f);
            var hair = Color.Lerp(
                new Color(0.08f, 0.05f, 0.04f),
                new Color(0.38f, 0.2f, 0.1f),
                Mathf.Abs(seed % 41) / 40f);
            var skinMaterial = GetMaterial(game, skin, 0f, 0.4f);
            var clothMaterial = GetMaterial(game, cloth, 0f, 0.32f);
            var shadowMaterial = GetMaterial(game, shadow, 0f, 0.26f);
            var hairMaterial = GetMaterial(game, hair, 0f, 0.22f);

            var pelvis = CreatePivot("Rig Root - Pelvis", visual, new Vector3(0f, -0.24f, 0f));
            CreateMesh(
                "Crowd coat",
                PrimitiveType.Cube,
                pelvis,
                Vector3.zero,
                new Vector3(0.48f, 0.31f, 0.32f),
                shadowMaterial);

            var chest = CreatePivot("Rig Root - Chest", visual, new Vector3(0f, 0.2f, 0f));
            CreateMesh(
                "Crowd torso",
                PrimitiveType.Cube,
                chest,
                Vector3.zero,
                new Vector3(0.54f, 0.61f, 0.34f),
                clothMaterial);

            BuildCrowdArm(chest, -1f, clothMaterial);
            BuildCrowdArm(chest, 1f, clothMaterial);
            BuildCrowdLeg(pelvis, -1f, shadowMaterial);
            BuildCrowdLeg(pelvis, 1f, shadowMaterial);

            var head = CreatePivot("Rig Root - Head", visual, new Vector3(0f, 0.8f, 0f));
            CreateMesh(
                "Crowd head",
                PrimitiveType.Sphere,
                head,
                Vector3.zero,
                new Vector3(0.34f, 0.39f, 0.32f),
                skinMaterial);
            CreateMesh(
                "Crowd hair",
                PrimitiveType.Sphere,
                head,
                new Vector3(0f, 0.19f, -0.025f),
                new Vector3(0.365f, 0.225f, 0.34f),
                hairMaterial);
            return visual;
        }

        private static void BuildCrowdArm(
            Transform chest,
            float side,
            Material sleeve)
        {
            var sideName = side < 0f ? "Left" : "Right";
            var shoulder = CreatePivot(
                $"Rig Root - {sideName} Shoulder",
                chest,
                new Vector3(side * 0.38f, 0.19f, 0f));
            CreateMesh(
                $"{sideName} crowd arm",
                PrimitiveType.Capsule,
                shoulder,
                new Vector3(0f, -0.28f, 0f),
                new Vector3(0.15f, 0.55f, 0.15f),
                sleeve);
            var elbow = CreatePivot(
                $"Rig Root - {sideName} Elbow",
                shoulder,
                new Vector3(0f, -0.37f, 0f));
            CreatePivot(
                $"Rig Root - {sideName} Hand",
                elbow,
                new Vector3(0f, -0.31f, 0f));
        }

        private static void BuildCrowdLeg(
            Transform pelvis,
            float side,
            Material trouser)
        {
            var sideName = side < 0f ? "Left" : "Right";
            var hip = CreatePivot(
                $"Rig Root - {sideName} Hip",
                pelvis,
                new Vector3(side * 0.18f, -0.15f, 0f));
            CreateMesh(
                $"{sideName} crowd leg",
                PrimitiveType.Capsule,
                hip,
                new Vector3(0f, -0.39f, 0f),
                new Vector3(0.18f, 0.77f, 0.18f),
                trouser);
            var knee = CreatePivot(
                $"Rig Root - {sideName} Knee",
                hip,
                new Vector3(0f, -0.44f, 0f));
            CreatePivot(
                $"Rig Root - {sideName} Foot",
                knee,
                new Vector3(0f, -0.41f, 0.08f));
        }

        private static void BuildArm(
            Transform chest,
            float side,
            Material skin,
            Material sleeve,
            Material leather)
        {
            var sideName = side < 0f ? "Left" : "Right";
            var shoulder = CreatePivot(
                $"Rig Root - {sideName} Shoulder",
                chest,
                new Vector3(side * 0.38f, 0.19f, 0f));
            CreateMesh(
                $"{sideName} upper sleeve",
                PrimitiveType.Capsule,
                shoulder,
                new Vector3(0f, -0.18f, 0f),
                new Vector3(0.16f, 0.36f, 0.16f),
                sleeve);

            var elbow = CreatePivot(
                $"Rig Root - {sideName} Elbow",
                shoulder,
                new Vector3(0f, -0.37f, 0f));
            CreateMesh(
                $"{sideName} forearm",
                PrimitiveType.Capsule,
                elbow,
                new Vector3(0f, -0.15f, 0f),
                new Vector3(0.135f, 0.3f, 0.135f),
                skin);

            var hand = CreatePivot(
                $"Rig Root - {sideName} Hand",
                elbow,
                new Vector3(0f, -0.31f, 0f));
            CreateMesh(
                $"{sideName} glove",
                PrimitiveType.Sphere,
                hand,
                Vector3.zero,
                new Vector3(0.14f, 0.17f, 0.13f),
                leather);
        }

        private static void BuildLeg(
            Transform pelvis,
            float side,
            Material trouser,
            Material leather)
        {
            var sideName = side < 0f ? "Left" : "Right";
            var hip = CreatePivot(
                $"Rig Root - {sideName} Hip",
                pelvis,
                new Vector3(side * 0.18f, -0.15f, 0f));
            CreateMesh(
                $"{sideName} trouser",
                PrimitiveType.Capsule,
                hip,
                new Vector3(0f, -0.22f, 0f),
                new Vector3(0.19f, 0.44f, 0.19f),
                trouser);

            var knee = CreatePivot(
                $"Rig Root - {sideName} Knee",
                hip,
                new Vector3(0f, -0.44f, 0f));
            CreateMesh(
                $"{sideName} boot shaft",
                PrimitiveType.Capsule,
                knee,
                new Vector3(0f, -0.2f, 0f),
                new Vector3(0.165f, 0.4f, 0.17f),
                leather);

            var foot = CreatePivot(
                $"Rig Root - {sideName} Foot",
                knee,
                new Vector3(0f, -0.41f, 0.08f));
            CreateMesh(
                $"{sideName} boot",
                PrimitiveType.Cube,
                foot,
                new Vector3(0f, 0f, 0.09f),
                new Vector3(0.2f, 0.14f, 0.38f),
                leather);
        }

        private static void BuildHair(Transform head, Material material, int seed)
        {
            CreateMesh(
                "Hair crown",
                PrimitiveType.Sphere,
                head,
                new Vector3(0f, 0.19f, -0.025f),
                new Vector3(0.365f, 0.225f, 0.34f),
                material);

            var style = Mathf.Abs(seed % 3);
            if (style == 0)
            {
                CreateMesh(
                    "Hair knot",
                    PrimitiveType.Sphere,
                    head,
                    new Vector3(0f, 0.32f, -0.13f),
                    new Vector3(0.17f, 0.17f, 0.17f),
                    material);
            }
            else if (style == 1)
            {
                CreateMesh(
                    "Side braid",
                    PrimitiveType.Capsule,
                    head,
                    new Vector3(-0.27f, -0.13f, -0.06f),
                    new Vector3(0.075f, 0.32f, 0.075f),
                    material,
                    Quaternion.Euler(0f, 0f, -8f));
            }
            else
            {
                CreateMesh(
                    "Swept fringe",
                    PrimitiveType.Cube,
                    head,
                    new Vector3(0.08f, 0.17f, 0.23f),
                    new Vector3(0.25f, 0.07f, 0.08f),
                    material,
                    Quaternion.Euler(0f, 0f, -16f));
            }
        }

        private static void BuildRoleDressing(
            string role,
            int seed,
            Transform chest,
            Transform pelvis,
            Material trim,
            Material leather,
            Material darkCloth)
        {
            var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
            if (normalizedRole.Contains("marshal") ||
                normalizedRole.Contains("guard") ||
                normalizedRole.Contains("breaker"))
            {
                CreateMesh(
                    "Civic shoulder guard",
                    PrimitiveType.Sphere,
                    chest,
                    new Vector3(0.38f, 0.2f, 0f),
                    new Vector3(0.25f, 0.14f, 0.3f),
                    trim);
                CreateMesh(
                    "Back spear",
                    PrimitiveType.Cylinder,
                    chest,
                    new Vector3(-0.28f, 0.04f, -0.25f),
                    new Vector3(0.045f, 0.88f, 0.045f),
                    leather,
                    Quaternion.Euler(0f, 0f, -13f));
                return;
            }

            if (normalizedRole.Contains("archive") ||
                normalizedRole.Contains("scribe") ||
                normalizedRole.Contains("wright") ||
                normalizedRole.Contains("mage"))
            {
                CreateMesh(
                    "Field folio",
                    PrimitiveType.Cube,
                    pelvis,
                    new Vector3(0.34f, 0f, 0.08f),
                    new Vector3(0.2f, 0.28f, 0.09f),
                    leather,
                    Quaternion.Euler(0f, 0f, 8f));
                CreateMesh(
                    "Folio clasp",
                    PrimitiveType.Cube,
                    pelvis,
                    new Vector3(0.34f, 0f, 0.13f),
                    new Vector3(0.12f, 0.035f, 0.025f),
                    trim);
                return;
            }

            if (normalizedRole.Contains("broker") ||
                normalizedRole.Contains("market") ||
                normalizedRole.Contains("grower") ||
                normalizedRole.Contains("ferrier") ||
                normalizedRole.Contains("inn"))
            {
                CreateMesh(
                    "Merchant apron",
                    PrimitiveType.Cube,
                    chest,
                    new Vector3(0f, -0.13f, 0.2f),
                    new Vector3(0.4f, 0.43f, 0.045f),
                    darkCloth);
                CreateMesh(
                    "Coin tally",
                    PrimitiveType.Cylinder,
                    pelvis,
                    new Vector3(-0.34f, 0.03f, 0.07f),
                    new Vector3(0.1f, 0.04f, 0.1f),
                    trim,
                    Quaternion.Euler(90f, 0f, 0f));
                return;
            }

            if ((seed & 1) == 0)
            {
                CreateMesh(
                    "Travel satchel",
                    PrimitiveType.Cube,
                    pelvis,
                    new Vector3(-0.34f, -0.01f, 0.03f),
                    new Vector3(0.2f, 0.25f, 0.14f),
                    leather);
            }
        }

        private static Transform CreatePivot(
            string name,
            Transform parent,
            Vector3 localPosition)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        private static GameObject CreateMesh(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion? localRotation = null)
        {
            var mesh = GameObject.CreatePrimitive(primitive);
            mesh.name = name;
            mesh.transform.SetParent(parent, false);
            mesh.transform.localPosition = localPosition;
            mesh.transform.localRotation = localRotation ?? Quaternion.identity;
            mesh.transform.localScale = localScale;
            var renderer = mesh.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var collider = mesh.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(collider);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            return mesh;
        }

        private static Material GetMaterial(
            TetherfallGame game,
            Color color,
            float metallic,
            float smoothness)
        {
            var color32 = (Color32)color;
            var key = 17;
            unchecked
            {
                key = key * 31 + color32.r;
                key = key * 31 + color32.g;
                key = key * 31 + color32.b;
                key = key * 31 + Mathf.RoundToInt(metallic * 255f);
                key = key * 31 + Mathf.RoundToInt(smoothness * 255f);
            }

            if (Materials.TryGetValue(key, out var material) && material != null)
            {
                return material;
            }

            material = game.CreateMaterial(color, metallic, smoothness);
            Materials[key] = material;
            return material;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                var safe = value ?? string.Empty;
                for (var index = 0; index < safe.Length; index++)
                {
                    hash = hash * 31 + safe[index];
                }

                return hash;
            }
        }
    }
}
