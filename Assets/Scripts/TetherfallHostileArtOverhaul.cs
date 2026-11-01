using System;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Builds collider-free, region-specific hostile art from Unity primitives and
    /// attaches one lightweight procedural animation layer to the generated visual.
    /// </summary>
    public static class TetherfallHostileArtOverhaul
    {
        private const string ArtRootName = "Tetherfall Hostile Art Overhaul";

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        private static readonly int MetallicProperty = Shader.PropertyToID("_Metallic");
        private static readonly int GlossinessProperty = Shader.PropertyToID("_Glossiness");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_Smoothness");

        private static Material sharedGlowMaterial;
        private static MaterialPropertyBlock sharedPropertyBlock;

        private struct ArtContext
        {
            public Transform root;
            public Material surfaceMaterial;
            public Material glowMaterial;
            public float scale;
            public Color baseColor;

            public ArtContext(
                Transform artRoot,
                Material surface,
                Material glow,
                float definitionScale,
                Color definitionColor)
            {
                root = artRoot;
                surfaceMaterial = surface;
                glowMaterial = glow;
                scale = Mathf.Max(0.1f, definitionScale);
                baseColor = Opaque(definitionColor);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            sharedGlowMaterial = null;
            sharedPropertyBlock = null;
        }

        /// <summary>
        /// Returns true for every authored enemy definition present in the current catalog.
        /// Unknown runtime definitions still receive an attack-style fallback from Build.
        /// </summary>
        public static bool Supports(string enemyId)
        {
            switch (enemyId)
            {
                case "enemy.hollow_raider":
                case "enemy.hollow_scout":
                case "enemy.mossglass_stalker":
                case "enemy.fen_spitter":
                case "enemy.fen_siltcaller":
                case "enemy.relay_breaker":
                case "enemy.cinder_echo":
                case "enemy.ashjaw_brute":
                case "enemy.vault_wisp":
                case "enemy.elite.mossfang_matriarch":
                case "enemy.elite.siltglass_heron":
                case "enemy.elite.cindermaw":
                case "enemy.elite.mirewarden":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Applies an idempotent procedural silhouette and equipment set to an Enemy root.
        /// Generated primitives have their colliders disabled before this method returns.
        /// </summary>
        public static Transform Build(Enemy enemyRoot, EnemyDefinition definition)
        {
            if (enemyRoot == null || definition == null)
            {
                return null;
            }

            var host = enemyRoot.transform;
            var existing = host.Find(ArtRootName);
            if (existing != null)
            {
                TetherfallHostileAnimation.Attach(enemyRoot, existing, definition);
                return existing;
            }

            var surfaceMaterial = FindBodyMaterial(host);
            RemoveLegacyEnemyDress(host);

            var artObject = new GameObject(ArtRootName);
            artObject.layer = enemyRoot.gameObject.layer;
            var artRoot = artObject.transform;
            artRoot.SetParent(host, false);
            artRoot.localPosition = Vector3.zero;
            artRoot.localRotation = Quaternion.identity;
            artRoot.localScale = Vector3.one;

            var context = new ArtContext(
                artRoot,
                surfaceMaterial,
                GetGlowMaterial(surfaceMaterial),
                definition.scale,
                definition.color);

            switch (definition.id ?? string.Empty)
            {
                case "enemy.hollow_raider":
                    BuildAlderwakeHollowRaider(context);
                    break;
                case "enemy.hollow_scout":
                    BuildAlderwakeHollowScout(context);
                    break;
                case "enemy.mossglass_stalker":
                    BuildAlderwakeMossglassStalker(context);
                    break;
                case "enemy.fen_spitter":
                    BuildMorrowfenFenSpitter(context);
                    break;
                case "enemy.fen_siltcaller":
                    BuildMorrowfenSiltcaller(context);
                    break;
                case "enemy.relay_breaker":
                    BuildRelayBreaker(context);
                    break;
                case "enemy.cinder_echo":
                    BuildCinderwoldCinderEcho(context);
                    break;
                case "enemy.ashjaw_brute":
                    BuildCinderwoldAshjawBrute(context);
                    break;
                case "enemy.vault_wisp":
                    BuildSkyvaultWisp(context);
                    break;
                case "enemy.elite.mossfang_matriarch":
                    BuildAlderwakeMossfangMatriarch(context);
                    break;
                case "enemy.elite.siltglass_heron":
                    BuildMorrowfenSiltglassHeron(context);
                    break;
                case "enemy.elite.cindermaw":
                    BuildCinderwoldCindermaw(context);
                    break;
                case "enemy.elite.mirewarden":
                    BuildMorrowfenMirewarden(context);
                    break;
                default:
                    BuildAttackStyleFallback(context, definition.attackStyle);
                    break;
            }

            AddCombatReadabilityTell(context, definition);
            if (!string.IsNullOrEmpty(definition.id) &&
                definition.id.StartsWith("enemy.elite.", StringComparison.Ordinal))
            {
                AddEliteCrown(context);
            }

            TetherfallHostileAnimation.Attach(enemyRoot, artRoot, definition);
            return artRoot;
        }

        private static void BuildAlderwakeHollowRaider(ArtContext context)
        {
            var bark = Darken(context.baseColor, 0.34f);
            var moss = Color.Lerp(context.baseColor, new Color(0.24f, 0.5f, 0.25f), 0.46f);
            var bone = new Color(0.74f, 0.64f, 0.46f);
            var brass = new Color(0.78f, 0.49f, 0.2f);

            Piece(context, PrimitiveType.Sphere, "Alderwake raider bark hood",
                new Vector3(0f, 1.48f, 0.02f), new Vector3(0.5f, 0.4f, 0.44f), bark);
            Piece(context, PrimitiveType.Cube, "Alderwake raider hollow mask",
                new Vector3(0f, 1.47f, 0.38f), new Vector3(0.32f, 0.23f, 0.1f), bone,
                new Vector3(-5f, 0f, 0f), false, 0.04f, 0.3f);
            Piece(context, PrimitiveType.Cube, "Alderwake raider left mantle",
                new Vector3(-0.43f, 1.2f, 0.01f), new Vector3(0.5f, 0.17f, 0.36f), moss,
                new Vector3(0f, 0f, 13f));
            Piece(context, PrimitiveType.Cube, "Alderwake raider right mantle",
                new Vector3(0.43f, 1.2f, 0.01f), new Vector3(0.5f, 0.17f, 0.36f), moss,
                new Vector3(0f, 0f, -13f));
            Piece(context, PrimitiveType.Cube, "Alderwake raider split cuirass",
                new Vector3(0f, 1.03f, 0.28f), new Vector3(0.62f, 0.5f, 0.2f), bark,
                new Vector3(-7f, 0f, 0f));
            Piece(context, PrimitiveType.Cylinder, "Alderwake raider buckler",
                new Vector3(-0.58f, 1.01f, 0.32f), new Vector3(0.34f, 0.07f, 0.34f), moss,
                new Vector3(90f, 0f, 0f), false, 0.12f, 0.34f);
            Piece(context, PrimitiveType.Sphere, "Alderwake raider buckler boss",
                new Vector3(-0.58f, 1.01f, 0.41f), Vector3.one * 0.12f, brass);
            Rod(context, "Alderwake raider cleaver grip",
                new Vector3(0.5f, 0.58f, 0.17f), new Vector3(0.66f, 1.16f, 0.2f), 0.045f, bark);
            Piece(context, PrimitiveType.Cube, "Alderwake raider cleaver",
                new Vector3(0.71f, 1.34f, 0.21f), new Vector3(0.18f, 0.47f, 0.09f), brass,
                new Vector3(0f, 0f, -18f), false, 0.35f, 0.5f);
            Piece(context, PrimitiveType.Cube, "Alderwake raider moss pennant",
                new Vector3(-0.22f, 1.68f, -0.13f), new Vector3(0.13f, 0.3f, 0.05f), moss,
                new Vector3(18f, 0f, -24f));
        }

        private static void BuildAlderwakeHollowScout(ArtContext context)
        {
            var bark = Darken(context.baseColor, 0.4f);
            var leaf = Color.Lerp(context.baseColor, new Color(0.34f, 0.65f, 0.3f), 0.5f);
            var bone = new Color(0.72f, 0.66f, 0.52f);
            var thread = new Color(0.36f, 0.91f, 0.88f);

            Piece(context, PrimitiveType.Cube, "Alderwake scout long coat",
                new Vector3(0f, 0.98f, 0.08f), new Vector3(0.5f, 0.92f, 0.34f), bark,
                new Vector3(4f, 0f, 0f));
            Piece(context, PrimitiveType.Sphere, "Alderwake scout pointed hood",
                new Vector3(0f, 1.55f, 0.03f), new Vector3(0.43f, 0.5f, 0.42f), leaf);
            Piece(context, PrimitiveType.Cube, "Alderwake scout narrow mask",
                new Vector3(0f, 1.53f, 0.39f), new Vector3(0.25f, 0.3f, 0.08f), bone,
                new Vector3(-4f, 0f, 0f));
            Piece(context, PrimitiveType.Cube, "Alderwake scout asymmetric cape",
                new Vector3(-0.36f, 1.2f, -0.12f), new Vector3(0.45f, 0.68f, 0.12f), leaf,
                new Vector3(10f, -8f, -16f));

            Rod(context, "Alderwake scout upper bow limb",
                new Vector3(0.56f, 1.12f, 0.25f), new Vector3(0.72f, 1.82f, 0.24f), 0.045f, bone);
            Rod(context, "Alderwake scout lower bow limb",
                new Vector3(0.56f, 1.12f, 0.25f), new Vector3(0.75f, 0.42f, 0.23f), 0.045f, bone);
            Rod(context, "Alderwake scout bow string upper",
                new Vector3(0.57f, 1.12f, 0.27f), new Vector3(0.72f, 1.82f, 0.26f), 0.012f, thread, true);
            Rod(context, "Alderwake scout bow string lower",
                new Vector3(0.57f, 1.12f, 0.27f), new Vector3(0.75f, 0.42f, 0.25f), 0.012f, thread, true);
            Piece(context, PrimitiveType.Sphere, "Alderwake scout thread nock",
                new Vector3(0.55f, 1.12f, 0.33f), Vector3.one * 0.1f, thread,
                default(Vector3), true);

            Piece(context, PrimitiveType.Cylinder, "Alderwake scout quiver",
                new Vector3(-0.4f, 1.08f, -0.27f), new Vector3(0.13f, 0.5f, 0.13f), bark,
                new Vector3(13f, 0f, -12f));
            Rod(context, "Alderwake scout high arrow",
                new Vector3(-0.5f, 1.2f, -0.25f), new Vector3(-0.38f, 1.84f, -0.22f), 0.018f, bone);
            Rod(context, "Alderwake scout low arrow",
                new Vector3(-0.36f, 1.18f, -0.27f), new Vector3(-0.2f, 1.76f, -0.24f), 0.018f, bone);
        }

        private static void BuildAlderwakeMossglassStalker(ArtContext context)
        {
            var hide = Darken(context.baseColor, 0.3f);
            var moss = Color.Lerp(context.baseColor, new Color(0.23f, 0.66f, 0.29f), 0.48f);
            var glass = new Color(0.47f, 0.88f, 0.62f);
            var claw = new Color(0.75f, 0.72f, 0.5f);

            Piece(context, PrimitiveType.Capsule, "Alderwake stalker long torso",
                new Vector3(0f, 0.78f, 0f), new Vector3(0.52f, 0.72f, 0.44f), hide,
                new Vector3(90f, 0f, 0f));
            Piece(context, PrimitiveType.Sphere, "Alderwake stalker haunch",
                new Vector3(0f, 0.68f, -0.48f), new Vector3(0.72f, 0.58f, 0.72f), moss);
            Piece(context, PrimitiveType.Sphere, "Alderwake stalker wedge head",
                new Vector3(0f, 0.88f, 0.7f), new Vector3(0.55f, 0.36f, 0.68f), hide);
            Piece(context, PrimitiveType.Cube, "Alderwake stalker glass muzzle",
                new Vector3(0f, 0.82f, 1.02f), new Vector3(0.38f, 0.2f, 0.3f), glass,
                new Vector3(7f, 0f, 0f), true);

            for (var side = -1; side <= 1; side += 2)
            {
                Rod(context, "Alderwake stalker foreleg",
                    new Vector3(0.34f * side, 0.74f, 0.42f),
                    new Vector3(0.46f * side, 0.2f, 0.58f), 0.085f, hide);
                Rod(context, "Alderwake stalker hindleg",
                    new Vector3(0.38f * side, 0.68f, -0.4f),
                    new Vector3(0.54f * side, 0.2f, -0.55f), 0.1f, moss);
                Piece(context, PrimitiveType.Cube, "Alderwake stalker foreclaw",
                    new Vector3(0.46f * side, 0.16f, 0.72f), new Vector3(0.14f, 0.07f, 0.3f), claw,
                    new Vector3(-8f, 0f, 0f));
            }

            for (var index = 0; index < 3; index++)
            {
                Piece(context, PrimitiveType.Cube, "Alderwake stalker dorsal mossglass",
                    new Vector3(0f, 1.18f + index * 0.04f, -0.34f + index * 0.34f),
                    new Vector3(0.12f, 0.42f - index * 0.05f, 0.18f), glass,
                    new Vector3(8f, index * 13f, index == 1 ? 0f : index == 0 ? 18f : -18f), true);
            }

            Rod(context, "Alderwake stalker thorn tail",
                new Vector3(0f, 0.72f, -0.68f), new Vector3(0.22f, 0.84f, -1.28f), 0.07f, moss);
        }

        private static void BuildMorrowfenFenSpitter(ArtContext context)
        {
            var silt = Darken(context.baseColor, 0.4f);
            var skin = Color.Lerp(context.baseColor, new Color(0.26f, 0.58f, 0.48f), 0.42f);
            var gland = new Color(0.3f, 0.9f, 0.76f);
            var eye = new Color(0.78f, 0.96f, 0.66f);

            Piece(context, PrimitiveType.Sphere, "Morrowfen spitter silt belly",
                new Vector3(0f, 0.64f, -0.08f), new Vector3(0.82f, 0.58f, 0.78f), silt);
            Piece(context, PrimitiveType.Sphere, "Morrowfen spitter plated head",
                new Vector3(0f, 1.02f, 0.42f), new Vector3(0.68f, 0.5f, 0.72f), skin);
            Piece(context, PrimitiveType.Sphere, "Morrowfen spitter throat reservoir",
                new Vector3(0f, 0.79f, 0.69f), new Vector3(0.42f, 0.4f, 0.38f), gland,
                default(Vector3), true);
            Piece(context, PrimitiveType.Cylinder, "Morrowfen spitter triple nozzle",
                new Vector3(0f, 1.01f, 0.86f), new Vector3(0.14f, 0.3f, 0.14f), silt,
                new Vector3(90f, 0f, 0f));

            for (var side = -1; side <= 1; side += 2)
            {
                Piece(context, PrimitiveType.Sphere, "Morrowfen spitter lantern eye",
                    new Vector3(0.24f * side, 1.22f, 0.74f), Vector3.one * 0.09f, eye,
                    default(Vector3), true);
                Rod(context, "Morrowfen spitter forelimb",
                    new Vector3(0.37f * side, 0.63f, 0.32f),
                    new Vector3(0.64f * side, 0.2f, 0.48f), 0.1f, skin);
                Rod(context, "Morrowfen spitter hindlimb",
                    new Vector3(0.4f * side, 0.58f, -0.35f),
                    new Vector3(0.72f * side, 0.18f, -0.46f), 0.11f, silt);
            }

            Piece(context, PrimitiveType.Cube, "Morrowfen spitter left reed fin",
                new Vector3(-0.5f, 0.9f, -0.23f), new Vector3(0.1f, 0.52f, 0.36f), skin,
                new Vector3(12f, 18f, -26f));
            Piece(context, PrimitiveType.Cube, "Morrowfen spitter right reed fin",
                new Vector3(0.5f, 0.9f, -0.23f), new Vector3(0.1f, 0.52f, 0.36f), skin,
                new Vector3(12f, -18f, 26f));
        }

        private static void BuildMorrowfenSiltcaller(ArtContext context)
        {
            var silt = Darken(context.baseColor, 0.5f);
            var reed = new Color(0.34f, 0.5f, 0.36f);
            var glass = new Color(0.32f, 0.92f, 0.88f);
            var bone = new Color(0.68f, 0.76f, 0.64f);

            Piece(context, PrimitiveType.Cube, "Morrowfen siltcaller split robe",
                new Vector3(0f, 0.88f, 0f), new Vector3(0.62f, 1.14f, 0.42f), silt,
                new Vector3(3f, 0f, 0f));
            Piece(context, PrimitiveType.Sphere, "Morrowfen siltcaller drowned cowl",
                new Vector3(0f, 1.53f, 0.02f), new Vector3(0.5f, 0.46f, 0.48f), reed);
            Piece(context, PrimitiveType.Cube, "Morrowfen siltcaller glass visor",
                new Vector3(0f, 1.52f, 0.42f), new Vector3(0.34f, 0.17f, 0.08f), glass,
                default(Vector3), true);
            Piece(context, PrimitiveType.Cube, "Morrowfen siltcaller left reed mantle",
                new Vector3(-0.47f, 1.23f, -0.06f), new Vector3(0.2f, 0.68f, 0.38f), reed,
                new Vector3(8f, 0f, -20f));
            Piece(context, PrimitiveType.Cube, "Morrowfen siltcaller right reed mantle",
                new Vector3(0.47f, 1.23f, -0.06f), new Vector3(0.2f, 0.68f, 0.38f), reed,
                new Vector3(8f, 0f, 20f));

            Rod(context, "Morrowfen siltcaller staff",
                new Vector3(0.62f, 0.3f, 0.06f), new Vector3(0.62f, 1.78f, 0.06f), 0.055f, bone);
            Piece(context, PrimitiveType.Cylinder, "Morrowfen siltcaller focus cage",
                new Vector3(0.62f, 1.8f, 0.06f), new Vector3(0.23f, 0.08f, 0.23f), bone,
                default(Vector3), false, 0.2f, 0.48f);
            Piece(context, PrimitiveType.Sphere, "Morrowfen siltcaller focus",
                new Vector3(0.62f, 1.82f, 0.06f), Vector3.one * 0.14f, glass,
                default(Vector3), true);
            Piece(context, PrimitiveType.Cylinder, "Morrowfen siltcaller left silt jar",
                new Vector3(-0.31f, 0.86f, -0.31f), new Vector3(0.13f, 0.24f, 0.13f), reed);
            Piece(context, PrimitiveType.Cylinder, "Morrowfen siltcaller right silt jar",
                new Vector3(0.04f, 0.82f, -0.37f), new Vector3(0.11f, 0.21f, 0.11f), reed);
        }

        private static void BuildRelayBreaker(ArtContext context)
        {
            var iron = Darken(context.baseColor, 0.58f);
            var plate = Color.Lerp(context.baseColor, new Color(0.54f, 0.31f, 0.2f), 0.52f);
            var copper = new Color(0.78f, 0.4f, 0.16f);
            var relay = new Color(0.3f, 0.88f, 0.84f);

            Piece(context, PrimitiveType.Cube, "Relay breaker slab cuirass",
                new Vector3(0f, 1.02f, 0.2f), new Vector3(0.94f, 0.82f, 0.5f), iron,
                new Vector3(-4f, 0f, 0f), false, 0.2f, 0.33f);
            Piece(context, PrimitiveType.Cube, "Relay breaker left block pauldron",
                new Vector3(-0.65f, 1.25f, 0.05f), new Vector3(0.52f, 0.34f, 0.48f), plate,
                new Vector3(0f, 0f, 12f), false, 0.28f, 0.42f);
            Piece(context, PrimitiveType.Cube, "Relay breaker right block pauldron",
                new Vector3(0.65f, 1.25f, 0.05f), new Vector3(0.52f, 0.34f, 0.48f), plate,
                new Vector3(0f, 0f, -12f), false, 0.28f, 0.42f);
            Piece(context, PrimitiveType.Cube, "Relay breaker visor",
                new Vector3(0f, 1.55f, 0.39f), new Vector3(0.42f, 0.17f, 0.12f), relay,
                default(Vector3), true);

            Rod(context, "Relay breaker maul haft",
                new Vector3(0.68f, 0.34f, 0.12f), new Vector3(0.7f, 1.42f, 0.18f), 0.07f, copper);
            Piece(context, PrimitiveType.Cube, "Relay breaker maul head",
                new Vector3(0.7f, 1.58f, 0.2f), new Vector3(0.7f, 0.28f, 0.34f), iron,
                new Vector3(0f, 0f, -4f), false, 0.4f, 0.4f);
            Piece(context, PrimitiveType.Cube, "Relay breaker maul warning face",
                new Vector3(0.7f, 1.58f, 0.39f), new Vector3(0.46f, 0.12f, 0.05f), copper,
                default(Vector3), true);

            Rod(context, "Relay breaker left fork",
                new Vector3(-0.32f, 1.18f, -0.28f), new Vector3(-0.38f, 1.86f, -0.28f), 0.045f, copper);
            Rod(context, "Relay breaker right fork",
                new Vector3(0.32f, 1.18f, -0.28f), new Vector3(0.38f, 1.86f, -0.28f), 0.045f, copper);
            Piece(context, PrimitiveType.Sphere, "Relay breaker left stolen relay",
                new Vector3(-0.38f, 1.86f, -0.28f), Vector3.one * 0.1f, relay,
                default(Vector3), true);
            Piece(context, PrimitiveType.Sphere, "Relay breaker right stolen relay",
                new Vector3(0.38f, 1.86f, -0.28f), Vector3.one * 0.1f, relay,
                default(Vector3), true);
        }

        private static void BuildCinderwoldCinderEcho(ArtContext context)
        {
            var ash = Darken(context.baseColor, 0.62f);
            var ember = new Color(1f, 0.31f, 0.1f);
            var copper = new Color(0.72f, 0.32f, 0.14f);
            var soot = new Color(0.13f, 0.09f, 0.1f);

            Piece(context, PrimitiveType.Cube, "Cinderwold echo angular shell",
                new Vector3(0f, 1.03f, 0.14f), new Vector3(0.58f, 0.9f, 0.38f), ash,
                new Vector3(0f, 0f, 4f));
            Piece(context, PrimitiveType.Sphere, "Cinderwold echo soot hood",
                new Vector3(0f, 1.52f, 0.01f), new Vector3(0.43f, 0.44f, 0.4f), soot);
            Piece(context, PrimitiveType.Cube, "Cinderwold echo ember visor",
                new Vector3(0f, 1.5f, 0.38f), new Vector3(0.34f, 0.1f, 0.08f), ember,
                new Vector3(0f, 0f, -6f), true);
            Piece(context, PrimitiveType.Cube, "Cinderwold echo left ash vane",
                new Vector3(-0.43f, 1.15f, -0.2f), new Vector3(0.14f, 0.65f, 0.34f), copper,
                new Vector3(12f, 0f, -28f));
            Piece(context, PrimitiveType.Cube, "Cinderwold echo right ash vane",
                new Vector3(0.43f, 1.15f, -0.2f), new Vector3(0.14f, 0.65f, 0.34f), copper,
                new Vector3(12f, 0f, 28f));

            Rod(context, "Cinderwold echo left blade grip",
                new Vector3(-0.46f, 0.62f, 0.18f), new Vector3(-0.57f, 0.96f, 0.23f), 0.04f, soot);
            Piece(context, PrimitiveType.Cube, "Cinderwold echo left blade",
                new Vector3(-0.66f, 1.22f, 0.27f), new Vector3(0.09f, 0.62f, 0.08f), ember,
                new Vector3(0f, 0f, 20f), true, 0.26f, 0.48f);
            Rod(context, "Cinderwold echo right blade grip",
                new Vector3(0.46f, 0.62f, 0.18f), new Vector3(0.57f, 0.96f, 0.23f), 0.04f, soot);
            Piece(context, PrimitiveType.Cube, "Cinderwold echo right blade",
                new Vector3(0.66f, 1.22f, 0.27f), new Vector3(0.09f, 0.62f, 0.08f), ember,
                new Vector3(0f, 0f, -20f), true, 0.26f, 0.48f);
            Piece(context, PrimitiveType.Sphere, "Cinderwold echo sternum coal",
                new Vector3(0f, 1.11f, 0.39f), Vector3.one * 0.11f, ember,
                default(Vector3), true);
        }

        private static void BuildCinderwoldAshjawBrute(ArtContext context)
        {
            var basalt = Darken(context.baseColor, 0.58f);
            var iron = new Color(0.22f, 0.13f, 0.12f);
            var ember = new Color(1f, 0.3f, 0.08f);
            var tooth = new Color(0.82f, 0.63f, 0.42f);

            Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw furnace torso",
                new Vector3(0f, 1.02f, 0.14f), new Vector3(1.06f, 0.86f, 0.56f), basalt,
                new Vector3(-5f, 0f, 0f), false, 0.18f, 0.28f);
            Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw left shoulder",
                new Vector3(-0.71f, 1.26f, 0f), new Vector3(0.55f, 0.42f, 0.55f), iron,
                new Vector3(0f, 0f, 12f), false, 0.25f, 0.36f);
            Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw right shoulder",
                new Vector3(0.71f, 1.26f, 0f), new Vector3(0.55f, 0.42f, 0.55f), iron,
                new Vector3(0f, 0f, -12f), false, 0.25f, 0.36f);
            Piece(context, PrimitiveType.Sphere, "Cinderwold ashjaw low skull",
                new Vector3(0f, 1.43f, 0.24f), new Vector3(0.58f, 0.42f, 0.52f), basalt);
            Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw upper jaw",
                new Vector3(0f, 1.31f, 0.58f), new Vector3(0.62f, 0.2f, 0.46f), iron,
                new Vector3(-7f, 0f, 0f));
            Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw lower jaw",
                new Vector3(0f, 1.14f, 0.6f), new Vector3(0.55f, 0.13f, 0.4f), ember,
                new Vector3(8f, 0f, 0f), true);

            for (var index = -1; index <= 1; index++)
            {
                Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw tooth",
                    new Vector3(index * 0.18f, 1.24f, 0.83f), new Vector3(0.07f, 0.18f, 0.08f), tooth,
                    new Vector3(0f, 0f, index * 7f));
            }

            Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw left gauntlet",
                new Vector3(-0.76f, 0.7f, 0.31f), new Vector3(0.48f, 0.48f, 0.5f), iron,
                new Vector3(4f, 0f, -8f), false, 0.35f, 0.34f);
            Piece(context, PrimitiveType.Cube, "Cinderwold ashjaw right gauntlet",
                new Vector3(0.76f, 0.7f, 0.31f), new Vector3(0.48f, 0.48f, 0.5f), iron,
                new Vector3(4f, 0f, 8f), false, 0.35f, 0.34f);
            Rod(context, "Cinderwold ashjaw left chimney",
                new Vector3(-0.3f, 1.34f, -0.29f), new Vector3(-0.38f, 1.91f, -0.31f), 0.09f, iron);
            Rod(context, "Cinderwold ashjaw right chimney",
                new Vector3(0.3f, 1.34f, -0.29f), new Vector3(0.38f, 1.91f, -0.31f), 0.09f, iron);
        }

        private static void BuildSkyvaultWisp(ArtContext context)
        {
            var voidColor = Darken(context.baseColor, 0.52f);
            var violet = Color.Lerp(context.baseColor, new Color(0.68f, 0.38f, 0.86f), 0.52f);
            var thread = new Color(0.44f, 0.9f, 1f);

            Piece(context, PrimitiveType.Sphere, "Skyvault wisp inner lens",
                new Vector3(0f, 1.02f, 0.12f), Vector3.one * 0.42f, thread,
                default(Vector3), true);
            Piece(context, PrimitiveType.Cube, "Skyvault wisp upper prism",
                new Vector3(0f, 1.47f, 0f), new Vector3(0.24f, 0.58f, 0.22f), violet,
                new Vector3(16f, 0f, 45f), true);
            Piece(context, PrimitiveType.Cube, "Skyvault wisp left prism",
                new Vector3(-0.43f, 1.07f, 0f), new Vector3(0.2f, 0.56f, 0.2f), violet,
                new Vector3(8f, 16f, -35f), true);
            Piece(context, PrimitiveType.Cube, "Skyvault wisp right prism",
                new Vector3(0.43f, 1.07f, 0f), new Vector3(0.2f, 0.56f, 0.2f), violet,
                new Vector3(8f, -16f, 35f), true);

            for (var index = 0; index < 6; index++)
            {
                var angle = index * Mathf.PI * 2f / 6f;
                Piece(context, PrimitiveType.Cube, "Skyvault wisp orbit shard",
                    new Vector3(Mathf.Cos(angle) * 0.66f, 1.08f + Mathf.Sin(angle * 2f) * 0.13f,
                        Mathf.Sin(angle) * 0.48f),
                    new Vector3(0.08f, 0.28f, 0.08f), index % 2 == 0 ? thread : violet,
                    new Vector3(index * 19f, index * 60f, 24f), true);
            }

            Rod(context, "Skyvault wisp left trailing filament",
                new Vector3(-0.2f, 0.84f, -0.08f), new Vector3(-0.37f, 0.25f, -0.31f), 0.035f, thread, true);
            Rod(context, "Skyvault wisp right trailing filament",
                new Vector3(0.2f, 0.84f, -0.08f), new Vector3(0.37f, 0.25f, -0.31f), 0.035f, thread, true);
            Piece(context, PrimitiveType.Cube, "Skyvault wisp trailing keystone",
                new Vector3(0f, 0.38f, -0.22f), new Vector3(0.16f, 0.38f, 0.12f), voidColor,
                new Vector3(18f, 0f, 45f));
        }

        private static void BuildAlderwakeMossfangMatriarch(ArtContext context)
        {
            var hide = Darken(context.baseColor, 0.4f);
            var moss = Color.Lerp(context.baseColor, new Color(0.38f, 0.78f, 0.3f), 0.45f);
            var glass = new Color(0.52f, 0.96f, 0.56f);
            var fang = new Color(0.88f, 0.74f, 0.45f);

            Piece(context, PrimitiveType.Capsule, "Alderwake matriarch long body",
                new Vector3(0f, 0.9f, -0.04f), new Vector3(0.74f, 0.9f, 0.64f), hide,
                new Vector3(90f, 0f, 0f));
            Piece(context, PrimitiveType.Sphere, "Alderwake matriarch armored haunch",
                new Vector3(0f, 0.78f, -0.56f), new Vector3(0.9f, 0.72f, 0.86f), moss);
            Piece(context, PrimitiveType.Sphere, "Alderwake matriarch crowned skull",
                new Vector3(0f, 1.09f, 0.76f), new Vector3(0.74f, 0.52f, 0.72f), hide);
            Piece(context, PrimitiveType.Cube, "Alderwake matriarch glass jaw",
                new Vector3(0f, 0.98f, 1.13f), new Vector3(0.54f, 0.22f, 0.48f), glass,
                new Vector3(8f, 0f, 0f), true);
            Piece(context, PrimitiveType.Cube, "Alderwake matriarch left fang",
                new Vector3(-0.22f, 0.83f, 1.29f), new Vector3(0.09f, 0.34f, 0.1f), fang,
                new Vector3(18f, 0f, -8f));
            Piece(context, PrimitiveType.Cube, "Alderwake matriarch right fang",
                new Vector3(0.22f, 0.83f, 1.29f), new Vector3(0.09f, 0.34f, 0.1f), fang,
                new Vector3(18f, 0f, 8f));

            for (var side = -1; side <= 1; side += 2)
            {
                Rod(context, "Alderwake matriarch foreleg",
                    new Vector3(0.48f * side, 0.82f, 0.46f),
                    new Vector3(0.62f * side, 0.18f, 0.58f), 0.13f, hide);
                Rod(context, "Alderwake matriarch hindleg",
                    new Vector3(0.5f * side, 0.72f, -0.48f),
                    new Vector3(0.68f * side, 0.18f, -0.58f), 0.15f, moss);
                Rod(context, "Alderwake matriarch crown antler",
                    new Vector3(0.29f * side, 1.32f, 0.72f),
                    new Vector3(0.58f * side, 1.92f, 0.44f), 0.055f, glass, true);
                Rod(context, "Alderwake matriarch crown tine",
                    new Vector3(0.46f * side, 1.65f, 0.55f),
                    new Vector3(0.76f * side, 1.82f, 0.62f), 0.04f, glass, true);
            }

            for (var index = 0; index < 3; index++)
            {
                Piece(context, PrimitiveType.Cube, "Alderwake matriarch dorsal blade",
                    new Vector3(0f, 1.42f, -0.48f + index * 0.4f),
                    new Vector3(0.16f, 0.52f - index * 0.05f, 0.24f), glass,
                    new Vector3(8f, index * 11f, index == 1 ? 0f : index == 0 ? 18f : -18f), true);
            }

            Rod(context, "Alderwake matriarch thorn tail",
                new Vector3(0f, 0.83f, -0.84f), new Vector3(-0.28f, 1.05f, -1.5f), 0.09f, moss);
        }

        private static void BuildMorrowfenSiltglassHeron(ArtContext context)
        {
            var silt = Darken(context.baseColor, 0.48f);
            var feather = Color.Lerp(context.baseColor, new Color(0.5f, 0.8f, 0.72f), 0.38f);
            var glass = new Color(0.42f, 0.96f, 0.96f);
            var beak = new Color(0.78f, 0.66f, 0.38f);

            Piece(context, PrimitiveType.Capsule, "Morrowfen heron narrow breast",
                new Vector3(0f, 1.18f, 0f), new Vector3(0.47f, 0.72f, 0.4f), silt);
            Rod(context, "Morrowfen heron long neck",
                new Vector3(0f, 1.36f, 0.12f), new Vector3(0f, 1.91f, 0.4f), 0.14f, feather);
            Piece(context, PrimitiveType.Sphere, "Morrowfen heron glass head",
                new Vector3(0f, 1.94f, 0.45f), new Vector3(0.42f, 0.32f, 0.42f), glass,
                default(Vector3), true);
            Piece(context, PrimitiveType.Cube, "Morrowfen heron needle beak",
                new Vector3(0f, 1.9f, 0.88f), new Vector3(0.13f, 0.1f, 0.72f), beak,
                new Vector3(-4f, 0f, 0f), false, 0.28f, 0.5f);

            Piece(context, PrimitiveType.Cube, "Morrowfen heron left folded wing",
                new Vector3(-0.47f, 1.18f, -0.02f), new Vector3(0.24f, 0.88f, 0.5f), feather,
                new Vector3(10f, 3f, -18f));
            Piece(context, PrimitiveType.Cube, "Morrowfen heron right folded wing",
                new Vector3(0.47f, 1.18f, -0.02f), new Vector3(0.24f, 0.88f, 0.5f), feather,
                new Vector3(10f, -3f, 18f));

            Rod(context, "Morrowfen heron left stilt leg",
                new Vector3(-0.18f, 0.82f, 0f), new Vector3(-0.2f, 0.14f, 0.08f), 0.055f, beak);
            Rod(context, "Morrowfen heron right stilt leg",
                new Vector3(0.18f, 0.82f, 0f), new Vector3(0.2f, 0.14f, 0.08f), 0.055f, beak);
            Rod(context, "Morrowfen heron left foot",
                new Vector3(-0.2f, 0.14f, 0.08f), new Vector3(-0.2f, 0.12f, 0.44f), 0.035f, beak);
            Rod(context, "Morrowfen heron right foot",
                new Vector3(0.2f, 0.14f, 0.08f), new Vector3(0.2f, 0.12f, 0.44f), 0.035f, beak);

            for (var index = -1; index <= 1; index++)
            {
                Rod(context, "Morrowfen heron tail quill",
                    new Vector3(index * 0.16f, 1f, -0.28f),
                    new Vector3(index * 0.26f, 0.58f, -0.9f), 0.045f,
                    index == 0 ? glass : feather, index == 0);
            }

            Piece(context, PrimitiveType.Sphere, "Morrowfen heron sight eye",
                new Vector3(0f, 1.98f, 0.76f), Vector3.one * 0.08f, glass,
                default(Vector3), true);
        }

        private static void BuildCinderwoldCindermaw(ArtContext context)
        {
            var basalt = Darken(context.baseColor, 0.68f);
            var plate = new Color(0.32f, 0.14f, 0.1f);
            var copper = new Color(0.76f, 0.3f, 0.11f);
            var ember = new Color(1f, 0.25f, 0.06f);
            var fracture = new Color(0.3f, 0.9f, 0.86f);

            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw bastion chest",
                new Vector3(0f, 1.04f, 0.12f), new Vector3(1.18f, 0.94f, 0.64f), basalt,
                new Vector3(-4f, 0f, 0f), false, 0.22f, 0.3f);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw left bastion",
                new Vector3(-0.79f, 1.28f, 0f), new Vector3(0.62f, 0.48f, 0.62f), plate,
                new Vector3(0f, 0f, 13f), false, 0.34f, 0.4f);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw right bastion",
                new Vector3(0.79f, 1.28f, 0f), new Vector3(0.62f, 0.48f, 0.62f), plate,
                new Vector3(0f, 0f, -13f), false, 0.34f, 0.4f);
            Piece(context, PrimitiveType.Sphere, "Cinderwold cindermaw furnace skull",
                new Vector3(0f, 1.48f, 0.25f), new Vector3(0.66f, 0.48f, 0.58f), basalt);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw crushing jaw",
                new Vector3(0f, 1.25f, 0.66f), new Vector3(0.76f, 0.32f, 0.56f), plate,
                new Vector3(7f, 0f, 0f), false, 0.28f, 0.38f);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw molten bite",
                new Vector3(0f, 1.17f, 0.91f), new Vector3(0.5f, 0.11f, 0.15f), ember,
                default(Vector3), true);

            Rod(context, "Cinderwold cindermaw left furnace horn",
                new Vector3(-0.3f, 1.68f, 0.18f), new Vector3(-0.68f, 1.98f, -0.04f), 0.08f, copper);
            Rod(context, "Cinderwold cindermaw right furnace horn",
                new Vector3(0.3f, 1.68f, 0.18f), new Vector3(0.68f, 1.98f, -0.04f), 0.08f, copper);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw left crossweave seam",
                new Vector3(-0.56f, 1.13f, 0.47f), new Vector3(0.08f, 0.42f, 0.06f), fracture,
                new Vector3(0f, 0f, -24f), true);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw right crossweave seam",
                new Vector3(0.56f, 1.13f, 0.47f), new Vector3(0.08f, 0.42f, 0.06f), fracture,
                new Vector3(0f, 0f, 24f), true);

            Rod(context, "Cinderwold cindermaw quake maul haft",
                new Vector3(0.78f, 0.3f, 0.05f), new Vector3(0.88f, 1.31f, 0.13f), 0.085f, copper);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw quake maul",
                new Vector3(0.88f, 1.5f, 0.13f), new Vector3(0.84f, 0.36f, 0.46f), plate,
                new Vector3(0f, 0f, -5f), false, 0.42f, 0.42f);
            Piece(context, PrimitiveType.Cube, "Cinderwold cindermaw maul ember",
                new Vector3(0.88f, 1.5f, 0.38f), new Vector3(0.58f, 0.15f, 0.05f), ember,
                default(Vector3), true);
        }

        private static void BuildMorrowfenMirewarden(ArtContext context)
        {
            var armor = Darken(context.baseColor, 0.48f);
            var silt = new Color(0.17f, 0.23f, 0.3f);
            var reed = new Color(0.31f, 0.47f, 0.36f);
            var lantern = new Color(0.38f, 0.9f, 0.9f);
            var warning = new Color(0.96f, 0.39f, 0.16f);

            Piece(context, PrimitiveType.Cube, "Morrowfen mirewarden plated coat",
                new Vector3(0f, 1.02f, 0.12f), new Vector3(0.86f, 1.02f, 0.52f), armor,
                new Vector3(2f, 0f, 0f), false, 0.24f, 0.38f);
            Piece(context, PrimitiveType.Cylinder, "Morrowfen mirewarden bell helm",
                new Vector3(0f, 1.58f, 0.02f), new Vector3(0.42f, 0.32f, 0.42f), silt,
                default(Vector3), false, 0.32f, 0.4f);
            Piece(context, PrimitiveType.Cube, "Morrowfen mirewarden flooded visor",
                new Vector3(0f, 1.54f, 0.41f), new Vector3(0.42f, 0.14f, 0.08f), lantern,
                default(Vector3), true);
            Piece(context, PrimitiveType.Cube, "Morrowfen mirewarden left shoulder plate",
                new Vector3(-0.62f, 1.28f, 0f), new Vector3(0.48f, 0.3f, 0.5f), armor,
                new Vector3(0f, 0f, 14f), false, 0.3f, 0.42f);
            Piece(context, PrimitiveType.Cube, "Morrowfen mirewarden right shoulder plate",
                new Vector3(0.62f, 1.28f, 0f), new Vector3(0.48f, 0.3f, 0.5f), armor,
                new Vector3(0f, 0f, -14f), false, 0.3f, 0.42f);

            Piece(context, PrimitiveType.Cube, "Morrowfen mirewarden tower shield",
                new Vector3(-0.72f, 0.94f, 0.35f), new Vector3(0.5f, 1.2f, 0.18f), silt,
                new Vector3(-4f, -8f, -3f), false, 0.38f, 0.4f);
            Piece(context, PrimitiveType.Cube, "Morrowfen mirewarden shield channel",
                new Vector3(-0.72f, 0.95f, 0.46f), new Vector3(0.12f, 0.82f, 0.05f), lantern,
                default(Vector3), true);

            Rod(context, "Morrowfen mirewarden anchor haft",
                new Vector3(0.65f, 0.3f, 0.08f), new Vector3(0.7f, 1.48f, 0.12f), 0.07f, reed);
            Piece(context, PrimitiveType.Cube, "Morrowfen mirewarden anchor crossbar",
                new Vector3(0.7f, 1.59f, 0.12f), new Vector3(0.66f, 0.16f, 0.18f), armor,
                new Vector3(0f, 0f, -4f), false, 0.34f, 0.4f);
            Rod(context, "Morrowfen mirewarden anchor left fluke",
                new Vector3(0.43f, 1.58f, 0.12f), new Vector3(0.24f, 1.83f, 0.1f), 0.055f, warning, true);
            Rod(context, "Morrowfen mirewarden anchor right fluke",
                new Vector3(0.97f, 1.58f, 0.12f), new Vector3(1.16f, 1.83f, 0.1f), 0.055f, warning, true);

            for (var index = -1; index <= 1; index++)
            {
                Rod(context, "Morrowfen mirewarden reed crest",
                    new Vector3(index * 0.16f, 1.76f, -0.08f),
                    new Vector3(index * 0.28f, 2.04f - Mathf.Abs(index) * 0.08f, -0.14f),
                    0.035f, reed);
            }
        }

        private static void BuildAttackStyleFallback(ArtContext context, EnemyAttackStyle attackStyle)
        {
            var shell = Darken(context.baseColor, 0.35f);
            var melee = new Color(0.94f, 0.66f, 0.27f);
            var ranged = new Color(0.32f, 0.9f, 0.9f);
            var heavy = new Color(0.96f, 0.37f, 0.14f);

            Piece(context, PrimitiveType.Sphere, "Fallback hostile hood",
                new Vector3(0f, 1.5f, 0f), new Vector3(0.48f, 0.42f, 0.46f), shell);
            Piece(context, PrimitiveType.Cube, "Fallback hostile mask",
                new Vector3(0f, 1.49f, 0.4f), new Vector3(0.35f, 0.2f, 0.08f),
                attackStyle == EnemyAttackStyle.Ranged ? ranged :
                attackStyle == EnemyAttackStyle.Heavy ? heavy : melee,
                default(Vector3), attackStyle == EnemyAttackStyle.Ranged);

            if (attackStyle == EnemyAttackStyle.Heavy)
            {
                Piece(context, PrimitiveType.Cube, "Fallback heavy left pauldron",
                    new Vector3(-0.58f, 1.2f, 0f), new Vector3(0.5f, 0.3f, 0.44f), shell,
                    new Vector3(0f, 0f, 15f));
                Piece(context, PrimitiveType.Cube, "Fallback heavy right pauldron",
                    new Vector3(0.58f, 1.2f, 0f), new Vector3(0.5f, 0.3f, 0.44f), shell,
                    new Vector3(0f, 0f, -15f));
            }
            else
            {
                Rod(context, attackStyle == EnemyAttackStyle.Ranged ?
                        "Fallback channeling staff" : "Fallback path blade",
                    new Vector3(0.48f, 0.38f, 0.12f), new Vector3(0.58f, 1.54f, 0.18f), 0.055f,
                    attackStyle == EnemyAttackStyle.Ranged ? ranged : melee,
                    attackStyle == EnemyAttackStyle.Ranged);
            }
        }

        private static void AddCombatReadabilityTell(ArtContext context, EnemyDefinition definition)
        {
            var meleeTell = new Color(1f, 0.69f, 0.27f);
            var rangedTell = new Color(0.32f, 0.94f, 0.94f);
            var heavyTell = new Color(1f, 0.32f, 0.1f);

            switch (definition.specialMove)
            {
                case EnemySpecialMove.QuickLunge:
                    Rod(context, "Quick-lunge left chevron",
                        new Vector3(-0.22f, 1.04f, 0.62f), new Vector3(0f, 0.9f, 0.8f),
                        0.027f, meleeTell, true);
                    Rod(context, "Quick-lunge right chevron",
                        new Vector3(0.22f, 1.04f, 0.62f), new Vector3(0f, 0.9f, 0.8f),
                        0.027f, meleeTell, true);
                    break;

                case EnemySpecialMove.TripleShot:
                    for (var index = -1; index <= 1; index++)
                    {
                        Piece(context, PrimitiveType.Sphere, "Triple-shot charge bead",
                            new Vector3(index * 0.16f, 1.31f - Mathf.Abs(index) * 0.04f, 0.72f),
                            Vector3.one * 0.075f, rangedTell, default(Vector3), true);
                    }
                    break;

                case EnemySpecialMove.Shockwave:
                    Piece(context, PrimitiveType.Cube, "Shockwave left warning plate",
                        new Vector3(-0.5f, 0.57f, 0.46f), new Vector3(0.18f, 0.13f, 0.07f), heavyTell,
                        new Vector3(0f, 0f, -12f), true);
                    Piece(context, PrimitiveType.Cube, "Shockwave right warning plate",
                        new Vector3(0.5f, 0.57f, 0.46f), new Vector3(0.18f, 0.13f, 0.07f), heavyTell,
                        new Vector3(0f, 0f, 12f), true);
                    break;

                case EnemySpecialMove.GroundBurst:
                    for (var index = 0; index < 3; index++)
                    {
                        var angle = index * Mathf.PI * 2f / 3f;
                        Piece(context, PrimitiveType.Cube, "Ground-burst collar shard",
                            new Vector3(Mathf.Cos(angle) * 0.36f, 1.39f,
                                0.1f + Mathf.Sin(angle) * 0.28f),
                            new Vector3(0.06f, 0.25f, 0.06f), rangedTell,
                            new Vector3(18f, index * 120f, 14f), true);
                    }
                    break;

                default:
                    Piece(context, PrimitiveType.Cube, "Melee readiness notch",
                        new Vector3(0f, 1.13f, 0.51f), new Vector3(0.2f, 0.07f, 0.06f), meleeTell,
                        new Vector3(0f, 0f, -12f), true);
                    break;
            }
        }

        private static void AddEliteCrown(ArtContext context)
        {
            var crown = new Color(1f, 0.78f, 0.32f);
            for (var index = -1; index <= 1; index++)
            {
                Piece(context, PrimitiveType.Cube, "Elite threat crown",
                    new Vector3(index * 0.22f, 1.91f + (index == 0 ? 0.1f : 0f), -0.1f),
                    new Vector3(0.07f, index == 0 ? 0.28f : 0.22f, 0.07f), crown,
                    new Vector3(12f, 0f, index * -16f), true);
            }
        }

        private static Transform Piece(
            ArtContext context,
            PrimitiveType primitive,
            string pieceName,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Vector3 localEuler = default(Vector3),
            bool glow = false,
            float metallic = 0.08f,
            float smoothness = 0.38f)
        {
            var item = GameObject.CreatePrimitive(primitive);
            item.name = pieceName;
            item.layer = context.root.gameObject.layer;
            item.transform.SetParent(context.root, false);
            item.transform.localPosition = localPosition * context.scale;
            item.transform.localScale = localScale * context.scale;
            item.transform.localRotation = Quaternion.Euler(localEuler);

            var generatedCollider = item.GetComponent<Collider>();
            if (generatedCollider != null)
            {
                generatedCollider.enabled = false;
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(generatedCollider);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(generatedCollider);
                }
            }

            var renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = glow ? context.glowMaterial : context.surfaceMaterial;
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                ApplyRendererProperties(renderer, Opaque(color), glow, metallic, smoothness);
            }

            return item.transform;
        }

        private static Transform Rod(
            ArtContext context,
            string pieceName,
            Vector3 start,
            Vector3 end,
            float radius,
            Color color,
            bool glow = false)
        {
            var direction = end - start;
            var length = direction.magnitude;
            if (length <= 0.0001f)
            {
                return null;
            }

            var rod = Piece(
                context,
                PrimitiveType.Cylinder,
                pieceName,
                (start + end) * 0.5f,
                new Vector3(radius, length * 0.5f, radius),
                color,
                default(Vector3),
                glow,
                glow ? 0.1f : 0.22f,
                glow ? 0.58f : 0.42f);
            rod.localRotation = Quaternion.FromToRotation(Vector3.up, direction / length);
            return rod;
        }

        private static void ApplyRendererProperties(
            Renderer renderer,
            Color color,
            bool glow,
            float metallic,
            float smoothness)
        {
            if (sharedPropertyBlock == null)
            {
                sharedPropertyBlock = new MaterialPropertyBlock();
            }

            sharedPropertyBlock.Clear();
            sharedPropertyBlock.SetColor(ColorProperty, color);
            sharedPropertyBlock.SetColor(BaseColorProperty, color);
            sharedPropertyBlock.SetColor(
                EmissionColorProperty,
                glow ? color * 1.75f : Color.black);
            sharedPropertyBlock.SetFloat(MetallicProperty, Mathf.Clamp01(metallic));
            sharedPropertyBlock.SetFloat(GlossinessProperty, Mathf.Clamp01(smoothness));
            sharedPropertyBlock.SetFloat(SmoothnessProperty, Mathf.Clamp01(smoothness));
            renderer.SetPropertyBlock(sharedPropertyBlock);
        }

        private static Material FindBodyMaterial(Transform host)
        {
            for (var index = 0; index < host.childCount; index++)
            {
                var child = host.GetChild(index);
                var renderer = child.GetComponent<Renderer>();
                if (renderer != null &&
                    child.name.EndsWith(" Visual", StringComparison.Ordinal))
                {
                    return renderer.sharedMaterial;
                }
            }

            var fallbackRenderer = host.GetComponentInChildren<Renderer>();
            return fallbackRenderer != null ? fallbackRenderer.sharedMaterial : null;
        }

        private static Material GetGlowMaterial(Material surfaceMaterial)
        {
            if (sharedGlowMaterial != null)
            {
                return sharedGlowMaterial;
            }

            var game = TetherfallGame.Instance;
            if (game != null)
            {
                sharedGlowMaterial = game.CreateGlowMaterial(Color.white, 1.75f, 0.58f);
            }

            if (sharedGlowMaterial == null && surfaceMaterial != null)
            {
                sharedGlowMaterial = new Material(surfaceMaterial)
                {
                    name = "Tetherfall Hostile Art Shared Glow"
                };
                if (sharedGlowMaterial.HasProperty(EmissionColorProperty))
                {
                    sharedGlowMaterial.EnableKeyword("_EMISSION");
                    sharedGlowMaterial.SetColor(EmissionColorProperty, Color.white * 1.75f);
                }
            }

            return sharedGlowMaterial != null ? sharedGlowMaterial : surfaceMaterial;
        }

        private static void RemoveLegacyEnemyDress(Transform host)
        {
            for (var index = host.childCount - 1; index >= 0; index--)
            {
                var child = host.GetChild(index);
                if (!IsLegacyEnemyDressName(child.name))
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static bool IsLegacyEnemyDressName(string objectName)
        {
            switch (objectName)
            {
                case "Enemy mask":
                case "Ground-burst silt focus":
                case "Ground-burst mote":
                case "Enemy left pauldron":
                case "Enemy right pauldron":
                case "Enemy channeling staff":
                case "Enemy blade":
                    return true;
                default:
                    return false;
            }
        }

        private static Color Darken(Color color, float amount)
        {
            return Opaque(Color.Lerp(color, Color.black, Mathf.Clamp01(amount)));
        }

        private static Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }
    }
}
