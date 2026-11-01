using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Builds bounded, collider-free procedural art for the current dungeon bosses.
    /// The boss root's own collider is intentionally preserved because it is the
    /// gameplay hit target; only colliders created for visual primitives are removed.
    /// </summary>
    public static class TetherfallBossArtOverhaul
    {
        private const string ArtRootPrefix = "Tetherfall Boss Art Overhaul";

        public const int MaxRenderersPerBoss = 32;
        public const int MaxMaterialsPerBoss = 6;

        private struct BossPalette
        {
            public Material dark;
            public Material body;
            public Material trim;
            public Material metal;
            public Material glow;
            public Material hotGlow;
        }

        /// <summary>
        /// Returns whether the definition has a dedicated authored silhouette.
        /// </summary>
        public static bool Supports(string bossId)
        {
            return bossId == "boss.skyvault_warden" ||
                   bossId == DungeonCatalog.GlasswaterBossId ||
                   bossId == "boss.scoria_harrower";
        }

        /// <summary>
        /// Builds the matching boss silhouette beneath <paramref name="bossRoot"/>.
        /// Repeated calls with the same root and definition return the existing art.
        /// Generated art uses at most 30 renderers and six cached shared materials.
        /// </summary>
        public static Transform Build(
            TetherfallGame game,
            Transform bossRoot,
            BossDefinition definition)
        {
            if (bossRoot == null)
            {
                Debug.LogError("Tetherfall boss art requires a boss root.");
                return null;
            }

            if (definition == null || !Supports(definition.id))
            {
                Debug.LogWarning(
                    "Tetherfall boss art has no profile for '" +
                    (definition != null ? definition.id : "null") + "'.");
                return null;
            }

            if (game == null)
            {
                game = TetherfallGame.Instance;
            }

            if (game == null)
            {
                Debug.LogError("Tetherfall boss art requires a TetherfallGame material owner.");
                return null;
            }

            var artRootName = ArtRootPrefix + " [" + definition.id + "]";
            var existing = bossRoot.Find(artRootName);
            if (existing != null)
            {
                return existing;
            }

            HidePrimitiveBody(bossRoot);
            RemoveLegacyBossDress(bossRoot);

            var artObject = new GameObject(artRootName);
            artObject.layer = bossRoot.gameObject.layer;
            var artRoot = artObject.transform;
            artRoot.SetParent(bossRoot, false);
            artRoot.localPosition = Vector3.zero;
            artRoot.localRotation = Quaternion.identity;
            artRoot.localScale = Vector3.one;

            var motion = artObject.AddComponent<TetherfallBossArtMotion>();
            if (definition.id == "boss.skyvault_warden" ||
                definition.id == DungeonCatalog.GlasswaterBossId)
            {
                BuildSkyvaultWarden(
                    artRoot,
                    CreateSkyvaultPalette(game, definition),
                    motion);
            }
            else
            {
                BuildScoriaHarrower(
                    artRoot,
                    CreateScoriaPalette(game, definition),
                    motion);
            }

            return artRoot;
        }

        private static BossPalette CreateSkyvaultPalette(
            TetherfallGame game,
            BossDefinition definition)
        {
            var violet = Opaque(Color.Lerp(
                definition.baseColor,
                new Color(0.31f, 0.25f, 0.56f),
                0.42f));
            var rose = Opaque(Color.Lerp(
                definition.phaseColor,
                new Color(0.96f, 0.3f, 0.7f),
                0.35f));

            return new BossPalette
            {
                dark = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.045f, 0.055f, 0.12f),
                    0.12f,
                    0.28f),
                body = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    violet,
                    0.24f,
                    0.48f),
                trim = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.62f, 0.74f, 0.88f),
                    0.32f,
                    0.68f),
                metal = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.72f, 0.48f, 0.22f),
                    0.64f,
                    0.58f),
                glow = RuntimeEffectMaterialCache.GetGlow(
                    game,
                    new Color(0.28f, 0.9f, 1f),
                    1.85f,
                    0.72f),
                hotGlow = RuntimeEffectMaterialCache.GetGlow(
                    game,
                    rose,
                    1.75f,
                    0.7f)
            };
        }

        private static BossPalette CreateScoriaPalette(
            TetherfallGame game,
            BossDefinition definition)
        {
            var scoria = Opaque(Color.Lerp(
                definition.baseColor,
                new Color(0.32f, 0.12f, 0.075f),
                0.38f));
            var ember = Opaque(Color.Lerp(
                definition.phaseColor,
                new Color(1f, 0.24f, 0.055f),
                0.3f));

            return new BossPalette
            {
                dark = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.055f, 0.035f, 0.04f),
                    0.08f,
                    0.2f),
                body = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    scoria,
                    0.16f,
                    0.3f),
                trim = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.2f, 0.12f, 0.11f),
                    0.42f,
                    0.4f),
                metal = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.68f, 0.3f, 0.12f),
                    0.7f,
                    0.52f),
                glow = RuntimeEffectMaterialCache.GetGlow(
                    game,
                    ember,
                    1.85f,
                    0.62f),
                hotGlow = RuntimeEffectMaterialCache.GetGlow(
                    game,
                    new Color(1f, 0.74f, 0.25f),
                    2.05f,
                    0.7f)
            };
        }

        private static void BuildSkyvaultWarden(
            Transform root,
            BossPalette palette,
            TetherfallBossArtMotion motion)
        {
            // A narrow floating reliquary and rear halo make the Warden readable
            // from range. Its lens, key, and brow all project toward Unity +Z.
            var body = CreateGroup(root, "Skyvault suspended reliquary", Vector3.zero);

            AddPrimitive(
                body,
                PrimitiveType.Capsule,
                "Skyvault void spindle",
                new Vector3(0f, 0.72f, -0.03f),
                new Vector3(0.31f, 0.58f, 0.29f),
                Quaternion.identity,
                palette.dark);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault faceted breastplate",
                new Vector3(0f, 0.76f, 0.25f),
                new Vector3(0.43f, 0.5f, 0.16f),
                Quaternion.Euler(-5f, 0f, 0f),
                palette.body);
            AddPrimitive(
                body,
                PrimitiveType.Sphere,
                "Skyvault forward cyclopean lens",
                new Vector3(0f, 0.94f, 0.47f),
                new Vector3(0.2f, 0.17f, 0.1f),
                Quaternion.identity,
                palette.hotGlow);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault lens brow",
                new Vector3(0f, 1.13f, 0.32f),
                new Vector3(0.44f, 0.11f, 0.16f),
                Quaternion.Euler(-8f, 0f, 0f),
                palette.trim);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault lower keel",
                new Vector3(0f, 0.11f, -0.02f),
                new Vector3(0.17f, 0.48f, 0.17f),
                Quaternion.Euler(0f, 0f, 45f),
                palette.dark);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault forward resonance key",
                new Vector3(0f, 0.48f, 0.48f),
                new Vector3(0.095f, 0.31f, 0.09f),
                Quaternion.Euler(-7f, 0f, 0f),
                palette.glow);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault left high vane",
                new Vector3(-0.55f, 1.02f, -0.01f),
                new Vector3(0.16f, 0.58f, 0.24f),
                Quaternion.Euler(8f, -8f, -34f),
                palette.body);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault right high vane",
                new Vector3(0.55f, 1.02f, -0.01f),
                new Vector3(0.16f, 0.58f, 0.24f),
                Quaternion.Euler(8f, 8f, 34f),
                palette.body);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault left suspended talon",
                new Vector3(-0.34f, 0.13f, 0.16f),
                new Vector3(0.11f, 0.38f, 0.12f),
                Quaternion.Euler(-14f, -10f, -22f),
                palette.trim);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Skyvault right suspended talon",
                new Vector3(0.34f, 0.13f, 0.16f),
                new Vector3(0.11f, 0.38f, 0.12f),
                Quaternion.Euler(-14f, 10f, 22f),
                palette.trim);

            var crown = CreateGroup(
                body,
                "Skyvault counter-rotating crown",
                new Vector3(0f, 1.44f, -0.02f));
            AddPrimitive(
                crown,
                PrimitiveType.Cylinder,
                "Skyvault crown ring",
                Vector3.zero,
                new Vector3(0.39f, 0.065f, 0.39f),
                Quaternion.identity,
                palette.metal);
            AddPrimitive(
                crown,
                PrimitiveType.Cube,
                "Skyvault crown central needle",
                new Vector3(0f, 0.25f, 0f),
                new Vector3(0.08f, 0.42f, 0.08f),
                Quaternion.Euler(0f, 0f, 45f),
                palette.glow);
            AddPrimitive(
                crown,
                PrimitiveType.Cube,
                "Skyvault crown left needle",
                new Vector3(-0.25f, 0.17f, 0f),
                new Vector3(0.065f, 0.3f, 0.065f),
                Quaternion.Euler(0f, 0f, -24f),
                palette.trim);
            AddPrimitive(
                crown,
                PrimitiveType.Cube,
                "Skyvault crown right needle",
                new Vector3(0.25f, 0.17f, 0f),
                new Vector3(0.065f, 0.3f, 0.065f),
                Quaternion.Euler(0f, 0f, 24f),
                palette.trim);

            var halo = CreateGroup(
                root,
                "Skyvault rotating broken halo",
                new Vector3(0f, 0.79f, -0.2f));
            const int haloSegmentCount = 8;
            for (var index = 0; index < haloSegmentCount; index++)
            {
                var startAngle = index * Mathf.PI * 2f / haloSegmentCount;
                var endAngle = (index + 0.72f) * Mathf.PI * 2f / haloSegmentCount;
                var start = new Vector3(
                    Mathf.Cos(startAngle) * 0.94f,
                    Mathf.Sin(startAngle) * 0.76f,
                    0f);
                var end = new Vector3(
                    Mathf.Cos(endAngle) * 0.94f,
                    Mathf.Sin(endAngle) * 0.76f,
                    0f);
                AddRod(
                    halo,
                    "Skyvault broken halo segment " + index,
                    start,
                    end,
                    0.036f,
                    index % 2 == 0 ? palette.trim : palette.glow);
            }

            for (var index = 0; index < 4; index++)
            {
                var angle = (45f + index * 90f) * Mathf.Deg2Rad;
                AddPrimitive(
                    halo,
                    PrimitiveType.Sphere,
                    "Skyvault halo cardinal seal " + index,
                    new Vector3(
                        Mathf.Cos(angle) * 0.94f,
                        Mathf.Sin(angle) * 0.76f,
                        0f),
                    Vector3.one * 0.11f,
                    Quaternion.identity,
                    index % 2 == 0 ? palette.metal : palette.hotGlow);
            }

            motion.Configure(
                halo,
                Vector3.forward,
                9f,
                crown,
                Vector3.up,
                -24f,
                body,
                0.026f,
                1.15f,
                0.4f);
        }

        private static void BuildScoriaHarrower(
            Transform root,
            BossPalette palette,
            TetherfallBossArtMotion motion)
        {
            // A low quadrupedal furnace, broad wheels, and a forward plough-jaw
            // deliberately oppose the Warden's tall floating silhouette. The jaw,
            // eyes, horns, and heat slit all point toward Unity +Z.
            var body = CreateGroup(root, "Scoria grounded furnace body", Vector3.zero);

            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria slab hull",
                new Vector3(0f, 0.31f, -0.1f),
                new Vector3(1.02f, 0.5f, 0.7f),
                Quaternion.Euler(-2f, 0f, 0f),
                palette.dark);
            AddPrimitive(
                body,
                PrimitiveType.Sphere,
                "Scoria armored carapace",
                new Vector3(0f, 0.69f, -0.28f),
                new Vector3(0.86f, 0.54f, 0.67f),
                Quaternion.identity,
                palette.body);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria forward ram skull",
                new Vector3(0f, 0.43f, 0.66f),
                new Vector3(0.7f, 0.4f, 0.7f),
                Quaternion.Euler(-7f, 0f, 0f),
                palette.trim);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria plough jaw",
                new Vector3(0f, 0.16f, 1.04f),
                new Vector3(0.67f, 0.15f, 0.39f),
                Quaternion.Euler(8f, 0f, 0f),
                palette.dark);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria furnace mouth",
                new Vector3(0f, 0.27f, 1.26f),
                new Vector3(0.45f, 0.095f, 0.065f),
                Quaternion.identity,
                palette.glow);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria copper brow",
                new Vector3(0f, 0.59f, 1.04f),
                new Vector3(0.49f, 0.12f, 0.11f),
                Quaternion.Euler(-5f, 0f, 0f),
                palette.metal);
            AddPrimitive(
                body,
                PrimitiveType.Sphere,
                "Scoria left forward eye",
                new Vector3(-0.25f, 0.61f, 1.13f),
                Vector3.one * 0.095f,
                Quaternion.identity,
                palette.hotGlow);
            AddPrimitive(
                body,
                PrimitiveType.Sphere,
                "Scoria right forward eye",
                new Vector3(0.25f, 0.61f, 1.13f),
                Vector3.one * 0.095f,
                Quaternion.identity,
                palette.hotGlow);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria left shoulder bastion",
                new Vector3(-0.83f, 0.49f, 0.2f),
                new Vector3(0.43f, 0.37f, 0.5f),
                Quaternion.Euler(0f, 0f, 12f),
                palette.body);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria right shoulder bastion",
                new Vector3(0.83f, 0.49f, 0.2f),
                new Vector3(0.43f, 0.37f, 0.5f),
                Quaternion.Euler(0f, 0f, -12f),
                palette.body);

            AddRod(
                body,
                "Scoria left front strut",
                new Vector3(-0.72f, 0.3f, 0.43f),
                new Vector3(-0.87f, -0.27f, 0.72f),
                0.115f,
                palette.trim);
            AddRod(
                body,
                "Scoria right front strut",
                new Vector3(0.72f, 0.3f, 0.43f),
                new Vector3(0.87f, -0.27f, 0.72f),
                0.115f,
                palette.trim);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria left front claw",
                new Vector3(-0.87f, -0.34f, 0.91f),
                new Vector3(0.26f, 0.1f, 0.38f),
                Quaternion.Euler(-7f, 0f, -4f),
                palette.dark);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria right front claw",
                new Vector3(0.87f, -0.34f, 0.91f),
                new Vector3(0.26f, 0.1f, 0.38f),
                Quaternion.Euler(-7f, 0f, 4f),
                palette.dark);

            AddRod(
                body,
                "Scoria left rear strut",
                new Vector3(-0.68f, 0.25f, -0.48f),
                new Vector3(-0.88f, -0.3f, -0.62f),
                0.13f,
                palette.body);
            AddRod(
                body,
                "Scoria right rear strut",
                new Vector3(0.68f, 0.25f, -0.48f),
                new Vector3(0.88f, -0.3f, -0.62f),
                0.13f,
                palette.body);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria left rear foot",
                new Vector3(-0.88f, -0.35f, -0.4f),
                new Vector3(0.29f, 0.11f, 0.38f),
                Quaternion.identity,
                palette.dark);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria right rear foot",
                new Vector3(0.88f, -0.35f, -0.4f),
                new Vector3(0.29f, 0.11f, 0.38f),
                Quaternion.identity,
                palette.dark);

            AddRod(
                body,
                "Scoria left forward horn",
                new Vector3(-0.33f, 0.66f, 0.9f),
                new Vector3(-0.58f, 0.8f, 1.44f),
                0.065f,
                palette.metal);
            AddRod(
                body,
                "Scoria right forward horn",
                new Vector3(0.33f, 0.66f, 0.9f),
                new Vector3(0.58f, 0.8f, 1.44f),
                0.065f,
                palette.metal);

            AddPrimitive(
                body,
                PrimitiveType.Cylinder,
                "Scoria furnace stack",
                new Vector3(0f, 0.91f, -0.45f),
                new Vector3(0.48f, 0.41f, 0.48f),
                Quaternion.identity,
                palette.trim);
            var furnaceCore = AddPrimitive(
                body,
                PrimitiveType.Sphere,
                "Scoria exposed furnace core",
                new Vector3(0f, 1.16f, -0.45f),
                new Vector3(0.27f, 0.2f, 0.27f),
                Quaternion.identity,
                palette.glow);
            AddRod(
                body,
                "Scoria left exhaust",
                new Vector3(-0.35f, 0.81f, -0.57f),
                new Vector3(-0.39f, 1.48f, -0.6f),
                0.09f,
                palette.dark);
            AddRod(
                body,
                "Scoria right exhaust",
                new Vector3(0.35f, 0.81f, -0.57f),
                new Vector3(0.39f, 1.48f, -0.6f),
                0.09f,
                palette.dark);
            AddRod(
                body,
                "Scoria rear harrow rail",
                new Vector3(0f, 0.42f, -0.72f),
                new Vector3(0f, 0.54f, -1.43f),
                0.085f,
                palette.metal);
            AddPrimitive(
                body,
                PrimitiveType.Cube,
                "Scoria rear harrow blade",
                new Vector3(0f, 0.55f, -1.5f),
                new Vector3(0.8f, 0.12f, 0.19f),
                Quaternion.Euler(0f, 0f, 5f),
                palette.trim);

            var leftWheel = CreateGroup(
                root,
                "Scoria left rotating flywheel",
                new Vector3(-1.05f, 0.31f, -0.13f));
            AddPrimitive(
                leftWheel,
                PrimitiveType.Cylinder,
                "Scoria left flywheel disc",
                Vector3.zero,
                new Vector3(0.43f, 0.1f, 0.43f),
                Quaternion.Euler(0f, 0f, 90f),
                palette.trim);
            AddPrimitive(
                leftWheel,
                PrimitiveType.Cube,
                "Scoria left flywheel hot spoke",
                new Vector3(-0.12f, 0.13f, 0f),
                new Vector3(0.045f, 0.3f, 0.075f),
                Quaternion.Euler(34f, 0f, 0f),
                palette.hotGlow);

            var rightWheel = CreateGroup(
                root,
                "Scoria right rotating flywheel",
                new Vector3(1.05f, 0.31f, -0.13f));
            AddPrimitive(
                rightWheel,
                PrimitiveType.Cylinder,
                "Scoria right flywheel disc",
                Vector3.zero,
                new Vector3(0.43f, 0.1f, 0.43f),
                Quaternion.Euler(0f, 0f, 90f),
                palette.trim);
            AddPrimitive(
                rightWheel,
                PrimitiveType.Cube,
                "Scoria right flywheel hot spoke",
                new Vector3(0.12f, 0.13f, 0f),
                new Vector3(0.045f, 0.3f, 0.075f),
                Quaternion.Euler(34f, 0f, 0f),
                palette.hotGlow);

            motion.Configure(
                leftWheel,
                Vector3.right,
                52f,
                rightWheel,
                Vector3.right,
                52f,
                furnaceCore,
                0.025f,
                2.2f,
                1.7f);
        }

        private static Transform CreateGroup(
            Transform parent,
            string groupName,
            Vector3 localPosition)
        {
            var groupObject = new GameObject(groupName);
            groupObject.layer = parent.gameObject.layer;
            var group = groupObject.transform;
            group.SetParent(parent, false);
            group.localPosition = localPosition;
            group.localRotation = Quaternion.identity;
            group.localScale = Vector3.one;
            return group;
        }

        private static Transform AddPrimitive(
            Transform parent,
            PrimitiveType primitive,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var item = GameObject.CreatePrimitive(primitive);
            item.name = objectName;
            item.layer = parent.gameObject.layer;
            var itemTransform = item.transform;
            itemTransform.SetParent(parent, false);
            itemTransform.localPosition = localPosition;
            itemTransform.localScale = localScale;
            itemTransform.localRotation = localRotation;

            DisableThenDestroyPrimitiveCollider(item);

            var itemRenderer = item.GetComponent<Renderer>();
            if (itemRenderer != null && material != null)
            {
                itemRenderer.sharedMaterial = material;
            }

            return itemTransform;
        }

        private static Transform AddRod(
            Transform parent,
            string objectName,
            Vector3 start,
            Vector3 end,
            float radius,
            Material material)
        {
            var direction = end - start;
            var length = direction.magnitude;
            if (length <= 0.0001f)
            {
                return null;
            }

            return AddPrimitive(
                parent,
                PrimitiveType.Cylinder,
                objectName,
                (start + end) * 0.5f,
                new Vector3(radius, length * 0.5f, radius),
                Quaternion.FromToRotation(Vector3.up, direction / length),
                material);
        }

        private static void DisableThenDestroyPrimitiveCollider(GameObject item)
        {
            var generatedCollider = item.GetComponent<Collider>();
            if (generatedCollider == null)
            {
                return;
            }

            generatedCollider.enabled = false;
            DestroySafely(generatedCollider);
        }

        private static void HidePrimitiveBody(Transform bossRoot)
        {
            // DungeonBoss retains this renderer reference for combat-state colors.
            // Keeping the component, but hiding it, avoids changing gameplay code.
            var bodyRenderer = bossRoot.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = false;
            }
        }

        private static void RemoveLegacyBossDress(Transform bossRoot)
        {
            for (var index = bossRoot.childCount - 1; index >= 0; index--)
            {
                var child = bossRoot.GetChild(index);
                if (!IsLegacyBossDress(child.name))
                {
                    continue;
                }

                var colliders = child.GetComponentsInChildren<Collider>(true);
                for (var colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    var legacyCollider = colliders[colliderIndex];
                    if (legacyCollider != null)
                    {
                        legacyCollider.enabled = false;
                        DestroySafely(legacyCollider);
                    }
                }

                child.gameObject.SetActive(false);
                DestroySafely(child.gameObject);
            }
        }

        private static bool IsLegacyBossDress(string objectName)
        {
            switch (objectName)
            {
                case "Warden core":
                case "Warden crown":
                case "Warden left prism":
                case "Warden right prism":
                    return true;
                default:
                    return false;
            }
        }

        private static void DestroySafely(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        private static Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }
    }

    /// <summary>
    /// Optional single-component motion for authored boss parts. Disable this
    /// component to make the generated model fully static. Update uses only value
    /// types and cached Transform references, producing no managed allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherfallBossArtMotion : MonoBehaviour
    {
        private Transform firstRotator;
        private Transform secondRotator;
        private Transform hoverTarget;

        private Vector3 firstAxis;
        private Vector3 secondAxis;
        private Vector3 hoverOrigin;
        private Quaternion firstOrigin;
        private Quaternion secondOrigin;

        private float firstDegreesPerSecond;
        private float secondDegreesPerSecond;
        private float hoverAmplitude;
        private float hoverFrequency;
        private float phaseOffset;
        private bool configured;

        internal void Configure(
            Transform first,
            Vector3 firstRotationAxis,
            float firstSpeed,
            Transform second,
            Vector3 secondRotationAxis,
            float secondSpeed,
            Transform hovering,
            float amplitude,
            float frequency,
            float phase)
        {
            firstRotator = first;
            secondRotator = second;
            hoverTarget = hovering;
            firstAxis = firstRotationAxis.sqrMagnitude > 0.0001f
                ? firstRotationAxis.normalized
                : Vector3.up;
            secondAxis = secondRotationAxis.sqrMagnitude > 0.0001f
                ? secondRotationAxis.normalized
                : Vector3.up;
            firstDegreesPerSecond = firstSpeed;
            secondDegreesPerSecond = secondSpeed;
            hoverAmplitude = Mathf.Max(0f, amplitude);
            hoverFrequency = Mathf.Max(0f, frequency);
            phaseOffset = phase;

            firstOrigin = firstRotator != null
                ? firstRotator.localRotation
                : Quaternion.identity;
            secondOrigin = secondRotator != null
                ? secondRotator.localRotation
                : Quaternion.identity;
            hoverOrigin = hoverTarget != null
                ? hoverTarget.localPosition
                : Vector3.zero;
            configured = true;
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            var time = Time.time;
            if (firstRotator != null)
            {
                firstRotator.localRotation =
                    firstOrigin *
                    Quaternion.AngleAxis(time * firstDegreesPerSecond, firstAxis);
            }

            if (secondRotator != null)
            {
                secondRotator.localRotation =
                    secondOrigin *
                    Quaternion.AngleAxis(time * secondDegreesPerSecond, secondAxis);
            }

            if (hoverTarget != null)
            {
                var position = hoverOrigin;
                position.y += Mathf.Sin(time * hoverFrequency + phaseOffset) * hoverAmplitude;
                hoverTarget.localPosition = position;
            }
        }
    }
}
