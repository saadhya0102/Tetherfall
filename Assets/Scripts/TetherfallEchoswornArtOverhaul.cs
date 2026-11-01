using UnityEngine;
using UnityEngine.Rendering;

namespace Tetherfall
{
    /// <summary>
    /// Adds collider-free, low-poly procedural art to the four original Pact roots.
    /// The generated art is static: there is no per-frame allocation or animation component.
    /// </summary>
    public static class TetherfallEchoswornArtOverhaul
    {
        private const string ArtRootPrefix = "Tetherfall Echosworn Art Overhaul";
        private const string RuntimeMaterialPath = "TetherfallRuntimeMaterial";

        private static Material runtimeMaterialPrototype;
        private static bool runtimeMaterialSearched;
        private static Mesh facetedRoundMesh;
        private static Mesh boxMesh;
        private static Mesh bladeMesh;
        private static Mesh columnMesh;
        private static Mesh taperedColumnMesh;
        private static Mesh spikeMesh;

        /// <summary>
        /// Builds the matching Echosworn silhouette beneath an existing Pact root.
        /// Repeated calls are idempotent and return the already-built art root.
        /// </summary>
        public static Transform Build(Transform pactRoot, string creatureName)
        {
            if (pactRoot == null)
            {
                Debug.LogError("Tetherfall Echosworn art requires a Pact root.");
                return null;
            }

            if (!IsSupported(creatureName))
            {
                Debug.LogWarning(
                    $"No dedicated Tetherfall Echosworn art profile exists for '{creatureName}'; " +
                    "using the neutral Mistling silhouette.");
            }

            var artRootName = $"{ArtRootPrefix} [{creatureName}]";
            var existing = pactRoot.Find(artRootName);
            if (existing != null)
            {
                return existing;
            }

            var artRoot = new GameObject(artRootName).transform;
            artRoot.gameObject.layer = pactRoot.gameObject.layer;
            artRoot.SetParent(pactRoot, false);
            artRoot.localPosition = Vector3.zero;
            artRoot.localRotation = Quaternion.identity;

            // PactCreature scales its primitive root non-uniformly. Cancel that authored
            // base scale so these silhouettes retain their intended proportions, while
            // still inheriting combat pulses and all movement from the Pact root.
            artRoot.localScale = new Vector3(
                SafeReciprocal(pactRoot.localScale.x),
                SafeReciprocal(pactRoot.localScale.y),
                SafeReciprocal(pactRoot.localScale.z));

            switch (creatureName)
            {
                case "Bramblet":
                    BuildBramblet(artRoot);
                    break;
                case "Slateback":
                    BuildSlateback(artRoot);
                    break;
                case "Mistling":
                    BuildMistling(artRoot);
                    break;
                case "Cinderwing":
                    BuildCinderwing(artRoot);
                    break;
                default:
                    BuildMistling(artRoot);
                    break;
            }

            return artRoot;
        }

        /// <summary>
        /// Convenience overload for Pact roots whose GameObject name is the creature name.
        /// </summary>
        public static Transform Build(Transform pactRoot)
        {
            return Build(pactRoot, pactRoot != null ? pactRoot.name : string.Empty);
        }

        private static bool IsSupported(string creatureName)
        {
            return creatureName == "Bramblet" ||
                   creatureName == "Slateback" ||
                   creatureName == "Mistling" ||
                   creatureName == "Cinderwing";
        }

        private static void BuildBramblet(Transform root)
        {
            var bark = CreateMaterial(
                "Bramblet seed bark",
                new Color(0.18f, 0.13f, 0.075f),
                0.02f,
                0.2f);
            var moss = CreateMaterial(
                "Bramblet moss",
                new Color(0.17f, 0.43f, 0.24f),
                0f,
                0.28f);
            var leaf = CreateMaterial(
                "Bramblet leaf",
                new Color(0.39f, 0.7f, 0.27f),
                0f,
                0.32f);
            var leafEdge = CreateMaterial(
                "Bramblet leaf edge",
                new Color(0.68f, 0.82f, 0.31f),
                0f,
                0.38f);
            var rootFiber = CreateMaterial(
                "Bramblet root fiber",
                new Color(0.34f, 0.21f, 0.11f),
                0.01f,
                0.18f);
            var threadglass = CreateMaterial(
                "Bramblet Threadglass dew",
                new Color(0.3f, 0.91f, 0.7f),
                0.08f,
                0.68f,
                1.55f);
            var eye = CreateMaterial(
                "Bramblet ink eye",
                new Color(0.018f, 0.035f, 0.026f),
                0f,
                0.5f);

            AddRound(
                root,
                "Bramblet split-seed body",
                new Vector3(0f, -0.02f, -0.08f),
                new Vector3(1.28f, 0.84f, 1.48f),
                Quaternion.Euler(-4f, 0f, 0f),
                bark);
            AddRound(
                root,
                "Bramblet moss mantle",
                new Vector3(0f, 0.25f, -0.1f),
                new Vector3(1.13f, 0.43f, 1.18f),
                Quaternion.Euler(-7f, 0f, 0f),
                moss);
            AddRound(
                root,
                "Bramblet seed face",
                new Vector3(0f, 0.15f, 0.68f),
                new Vector3(0.78f, 0.65f, 0.72f),
                Quaternion.Euler(8f, 0f, 0f),
                moss);
            AddRound(
                root,
                "Bramblet root snout",
                new Vector3(0f, 0.02f, 1.01f),
                new Vector3(0.39f, 0.28f, 0.38f),
                Quaternion.identity,
                rootFiber);

            AddTapered(
                root,
                "Bramblet left fore-root",
                new Vector3(-0.38f, -0.48f, 0.38f),
                new Vector3(0.2f, 0.62f, 0.2f),
                Quaternion.Euler(4f, 0f, -8f),
                rootFiber);
            AddTapered(
                root,
                "Bramblet right fore-root",
                new Vector3(0.38f, -0.48f, 0.38f),
                new Vector3(0.2f, 0.62f, 0.2f),
                Quaternion.Euler(4f, 0f, 8f),
                rootFiber);
            AddTapered(
                root,
                "Bramblet left hind-root",
                new Vector3(-0.4f, -0.48f, -0.4f),
                new Vector3(0.22f, 0.58f, 0.22f),
                Quaternion.Euler(-5f, 0f, -9f),
                rootFiber);
            AddTapered(
                root,
                "Bramblet right hind-root",
                new Vector3(0.4f, -0.48f, -0.4f),
                new Vector3(0.22f, 0.58f, 0.22f),
                Quaternion.Euler(-5f, 0f, 9f),
                rootFiber);

            AddBlade(
                root,
                "Bramblet left crown leaf",
                new Vector3(-0.29f, 0.59f, 0.34f),
                new Vector3(0.34f, 0.08f, 0.72f),
                Quaternion.Euler(-57f, -12f, -24f),
                leaf);
            AddBlade(
                root,
                "Bramblet right crown leaf",
                new Vector3(0.29f, 0.59f, 0.34f),
                new Vector3(0.34f, 0.08f, 0.72f),
                Quaternion.Euler(-57f, 12f, 24f),
                leaf);
            AddBlade(
                root,
                "Bramblet high sprout",
                new Vector3(0f, 0.76f, 0.04f),
                new Vector3(0.3f, 0.07f, 0.67f),
                Quaternion.Euler(-72f, 0f, 8f),
                leafEdge);
            AddBlade(
                root,
                "Bramblet shoulder leaf",
                new Vector3(-0.55f, 0.31f, -0.1f),
                new Vector3(0.3f, 0.07f, 0.66f),
                Quaternion.Euler(-18f, -62f, -12f),
                leaf);

            AddRound(
                root,
                "Bramblet left eye",
                new Vector3(-0.19f, 0.25f, 1.025f),
                Vector3.one * 0.105f,
                Quaternion.identity,
                eye);
            AddRound(
                root,
                "Bramblet right eye",
                new Vector3(0.19f, 0.25f, 1.025f),
                Vector3.one * 0.105f,
                Quaternion.identity,
                eye);
            AddRound(
                root,
                "Bramblet brow dew",
                new Vector3(0f, 0.42f, 1.005f),
                new Vector3(0.13f, 0.18f, 0.08f),
                Quaternion.identity,
                threadglass,
                false);

            var tailStart = new Vector3(0.08f, 0.02f, -0.75f);
            for (var index = 0; index < 6; index++)
            {
                var t = (index + 1f) / 6f;
                var tailEnd = new Vector3(
                    Mathf.Sin(t * Mathf.PI * 1.18f) * 0.52f,
                    0.02f + t * 0.38f,
                    -0.75f - t * 0.68f);
                AddBeam(
                    root,
                    $"Bramblet curling vine {index + 1}",
                    tailStart,
                    tailEnd,
                    0.085f - index * 0.007f,
                    rootFiber);
                tailStart = tailEnd;
            }

            AddBlade(
                root,
                "Bramblet vine-tip leaf",
                tailStart + new Vector3(0f, 0.08f, -0.08f),
                new Vector3(0.27f, 0.055f, 0.5f),
                Quaternion.Euler(-24f, 18f, 20f),
                leafEdge);
        }

        private static void BuildSlateback(Transform root)
        {
            var understone = CreateMaterial(
                "Slateback understone",
                new Color(0.105f, 0.135f, 0.15f),
                0.06f,
                0.24f);
            var slate = CreateMaterial(
                "Slateback slate",
                new Color(0.25f, 0.3f, 0.38f),
                0.12f,
                0.34f);
            var slateEdge = CreateMaterial(
                "Slateback slate edge",
                new Color(0.42f, 0.47f, 0.57f),
                0.1f,
                0.38f);
            var copper = CreateMaterial(
                "Slateback routework tack",
                new Color(0.67f, 0.34f, 0.15f),
                0.48f,
                0.58f);
            var saddle = CreateMaterial(
                "Slateback field saddle",
                new Color(0.31f, 0.2f, 0.12f),
                0f,
                0.22f);
            var threadglass = CreateMaterial(
                "Slateback Threadglass seam",
                new Color(0.26f, 0.79f, 0.74f),
                0.12f,
                0.64f,
                1.25f);
            var eye = CreateMaterial(
                "Slateback eye",
                new Color(0.75f, 0.87f, 0.68f),
                0.04f,
                0.55f,
                0.65f);

            AddRound(
                root,
                "Slateback low carrying body",
                new Vector3(0f, 0.02f, -0.14f),
                new Vector3(2.2f, 1.3f, 3.22f),
                Quaternion.identity,
                understone);
            AddRound(
                root,
                "Slateback broad head",
                new Vector3(0f, 0.08f, 1.63f),
                new Vector3(1.28f, 0.88f, 1.18f),
                Quaternion.Euler(-3f, 0f, 0f),
                slate);
            AddRound(
                root,
                "Slateback chisel muzzle",
                new Vector3(0f, -0.03f, 2.19f),
                new Vector3(0.88f, 0.52f, 0.58f),
                Quaternion.identity,
                slateEdge);

            AddTapered(
                root,
                "Slateback left foreleg",
                new Vector3(-0.72f, -0.73f, 0.98f),
                new Vector3(0.42f, 1.16f, 0.42f),
                Quaternion.Euler(3f, 0f, -3f),
                understone);
            AddTapered(
                root,
                "Slateback right foreleg",
                new Vector3(0.72f, -0.73f, 0.98f),
                new Vector3(0.42f, 1.16f, 0.42f),
                Quaternion.Euler(3f, 0f, 3f),
                understone);
            AddTapered(
                root,
                "Slateback left hindleg",
                new Vector3(-0.72f, -0.72f, -1.08f),
                new Vector3(0.46f, 1.18f, 0.46f),
                Quaternion.Euler(-3f, 0f, -4f),
                understone);
            AddTapered(
                root,
                "Slateback right hindleg",
                new Vector3(0.72f, -0.72f, -1.08f),
                new Vector3(0.46f, 1.18f, 0.46f),
                Quaternion.Euler(-3f, 0f, 4f),
                understone);

            for (var index = 0; index < 5; index++)
            {
                var distanceFromCenter = Mathf.Abs(index - 2);
                AddBlade(
                    root,
                    $"Slateback overlapping shell plate {index + 1}",
                    new Vector3(0f, 0.69f, -1.02f + index * 0.51f),
                    new Vector3(1.88f - distanceFromCenter * 0.13f, 0.17f, 0.82f),
                    Quaternion.Euler(0f, index % 2 == 0 ? 0f : 180f, 0f),
                    index % 2 == 0 ? slate : slateEdge);
            }

            AddBlade(
                root,
                "Slateback left cheek shield",
                new Vector3(-0.62f, 0.26f, 1.63f),
                new Vector3(0.56f, 0.13f, 0.86f),
                Quaternion.Euler(12f, -18f, 68f),
                slateEdge);
            AddBlade(
                root,
                "Slateback right cheek shield",
                new Vector3(0.62f, 0.26f, 1.63f),
                new Vector3(0.56f, 0.13f, 0.86f),
                Quaternion.Euler(12f, 18f, -68f),
                slateEdge);
            AddRound(
                root,
                "Slateback left eye",
                new Vector3(-0.37f, 0.25f, 2.13f),
                new Vector3(0.105f, 0.12f, 0.075f),
                Quaternion.identity,
                eye,
                false);
            AddRound(
                root,
                "Slateback right eye",
                new Vector3(0.37f, 0.25f, 2.13f),
                new Vector3(0.105f, 0.12f, 0.075f),
                Quaternion.identity,
                eye,
                false);

            AddBeam(
                root,
                "Slateback left flank seam",
                new Vector3(-1.08f, 0.19f, -0.9f),
                new Vector3(-1.08f, 0.26f, 0.94f),
                0.055f,
                threadglass);
            AddBeam(
                root,
                "Slateback right flank seam",
                new Vector3(1.08f, 0.19f, -0.9f),
                new Vector3(1.08f, 0.26f, 0.94f),
                0.055f,
                threadglass);

            // The contrasting, low saddle outline leaves the complete centerline and
            // rider volume clear. Nothing projects into the Wayfinder's mounted pose.
            AddBox(
                root,
                "Slateback readable saddle pad",
                new Vector3(0f, 0.98f, -0.18f),
                new Vector3(1.28f, 0.12f, 1.24f),
                Quaternion.Euler(0f, 0f, 0f),
                saddle);
            AddBeam(
                root,
                "Slateback left saddle rail",
                new Vector3(-0.67f, 1.08f, -0.75f),
                new Vector3(-0.67f, 1.08f, 0.38f),
                0.075f,
                copper);
            AddBeam(
                root,
                "Slateback right saddle rail",
                new Vector3(0.67f, 1.08f, -0.75f),
                new Vector3(0.67f, 1.08f, 0.38f),
                0.075f,
                copper);
            AddBeam(
                root,
                "Slateback saddle front rule",
                new Vector3(-0.67f, 1.09f, 0.4f),
                new Vector3(0.67f, 1.09f, 0.4f),
                0.075f,
                copper);
            AddBeam(
                root,
                "Slateback saddle rear rule",
                new Vector3(-0.67f, 1.09f, -0.77f),
                new Vector3(0.67f, 1.09f, -0.77f),
                0.075f,
                copper);
            AddBeam(
                root,
                "Slateback left chest strap",
                new Vector3(-0.79f, 0.63f, 0.55f),
                new Vector3(-1.02f, -0.15f, 0.78f),
                0.07f,
                copper);
            AddBeam(
                root,
                "Slateback right chest strap",
                new Vector3(0.79f, 0.63f, 0.55f),
                new Vector3(1.02f, -0.15f, 0.78f),
                0.07f,
                copper);

            AddBeam(
                root,
                "Slateback stone tail",
                new Vector3(0f, 0.12f, -1.65f),
                new Vector3(0f, -0.04f, -2.18f),
                0.24f,
                slate);
            AddTapered(
                root,
                "Slateback tail cap",
                new Vector3(0f, -0.08f, -2.31f),
                new Vector3(0.3f, 0.46f, 0.3f),
                Quaternion.Euler(90f, 0f, 0f),
                slateEdge);
        }

        private static void BuildMistling(Transform root)
        {
            var deepMist = CreateMaterial(
                "Mistling deep mist",
                new Color(0.075f, 0.19f, 0.23f),
                0.02f,
                0.38f);
            var clearMist = CreateMaterial(
                "Mistling clear veil",
                new Color(0.62f, 0.84f, 0.85f, 0.34f),
                0f,
                0.66f,
                0f,
                true);
            var reed = CreateMaterial(
                "Mistling lantern reed",
                new Color(0.34f, 0.51f, 0.48f),
                0.03f,
                0.3f);
            var copper = CreateMaterial(
                "Mistling lantern copper",
                new Color(0.66f, 0.37f, 0.18f),
                0.42f,
                0.58f);
            var threadglass = CreateMaterial(
                "Mistling lantern breath",
                new Color(0.3f, 0.88f, 0.86f),
                0.08f,
                0.7f,
                1.85f);
            var pale = CreateMaterial(
                "Mistling pale wisp",
                new Color(0.77f, 0.9f, 0.88f),
                0f,
                0.42f);
            var eye = CreateMaterial(
                "Mistling dark eye",
                new Color(0.015f, 0.055f, 0.07f),
                0f,
                0.54f);

            AddRound(
                root,
                "Mistling rain-seed core",
                new Vector3(0f, 0.14f, -0.02f),
                new Vector3(0.82f, 1.02f, 0.78f),
                Quaternion.Euler(-4f, 0f, 0f),
                deepMist);
            AddRound(
                root,
                "Mistling faceted veil",
                new Vector3(0f, 0.18f, -0.02f),
                new Vector3(1.22f, 1.28f, 1.06f),
                Quaternion.Euler(0f, 16f, 0f),
                clearMist,
                false);
            AddRound(
                root,
                "Mistling lantern breath",
                new Vector3(0f, 0.12f, 0.35f),
                new Vector3(0.34f, 0.46f, 0.28f),
                Quaternion.identity,
                threadglass,
                false);

            AddRing(
                root,
                "Mistling polygon halo",
                new Vector3(0f, 0.68f, -0.02f),
                0.66f,
                0.035f,
                Quaternion.identity,
                copper,
                12);
            AddBeam(
                root,
                "Mistling halo front stay",
                new Vector3(0f, 0.66f, 0.64f),
                new Vector3(0f, 0.18f, 0.49f),
                0.035f,
                copper);
            AddBeam(
                root,
                "Mistling halo left stay",
                new Vector3(-0.66f, 0.66f, -0.02f),
                new Vector3(-0.43f, 0.14f, -0.02f),
                0.035f,
                copper);
            AddBeam(
                root,
                "Mistling halo right stay",
                new Vector3(0.66f, 0.66f, -0.02f),
                new Vector3(0.43f, 0.14f, -0.02f),
                0.035f,
                copper);

            AddBlade(
                root,
                "Mistling left veil fin",
                new Vector3(-0.55f, 0.08f, -0.03f),
                new Vector3(0.42f, 0.055f, 0.9f),
                Quaternion.Euler(-25f, -64f, -20f),
                clearMist,
                false);
            AddBlade(
                root,
                "Mistling right veil fin",
                new Vector3(0.55f, 0.08f, -0.03f),
                new Vector3(0.42f, 0.055f, 0.9f),
                Quaternion.Euler(-25f, 64f, 20f),
                clearMist,
                false);

            AddMistlingTendril(
                root,
                "Mistling left reed tendril",
                new Vector3(-0.34f, -0.32f, 0.08f),
                -1f,
                0.08f,
                reed,
                pale);
            AddMistlingTendril(
                root,
                "Mistling right reed tendril",
                new Vector3(0.34f, -0.32f, 0.06f),
                1f,
                0.06f,
                reed,
                pale);
            AddMistlingTendril(
                root,
                "Mistling rear reed tendril",
                new Vector3(0.05f, -0.34f, -0.28f),
                -0.4f,
                -0.22f,
                reed,
                pale);

            AddRound(
                root,
                "Mistling left eye",
                new Vector3(-0.17f, 0.31f, 0.5f),
                new Vector3(0.075f, 0.095f, 0.055f),
                Quaternion.identity,
                eye);
            AddRound(
                root,
                "Mistling right eye",
                new Vector3(0.17f, 0.31f, 0.5f),
                new Vector3(0.075f, 0.095f, 0.055f),
                Quaternion.identity,
                eye);
            AddRound(
                root,
                "Mistling high wisp",
                new Vector3(-0.38f, 0.94f, -0.08f),
                Vector3.one * 0.11f,
                Quaternion.identity,
                threadglass,
                false);
            AddRound(
                root,
                "Mistling low wisp",
                new Vector3(0.52f, -0.18f, -0.22f),
                Vector3.one * 0.08f,
                Quaternion.identity,
                threadglass,
                false);
        }

        private static void AddMistlingTendril(
            Transform root,
            string name,
            Vector3 start,
            float side,
            float depth,
            Material reed,
            Material tip)
        {
            var middle = start + new Vector3(side * 0.14f, -0.32f, depth);
            var end = middle + new Vector3(-side * 0.08f, -0.34f, depth * 0.5f);
            AddBeam(root, $"{name} upper", start, middle, 0.065f, reed);
            AddBeam(root, $"{name} lower", middle, end, 0.045f, reed);
            AddRound(
                root,
                $"{name} pearl",
                end,
                new Vector3(0.105f, 0.14f, 0.105f),
                Quaternion.identity,
                tip);
        }

        private static void BuildCinderwing(Transform root)
        {
            var cinder = CreateMaterial(
                "Cinderwing cinder",
                new Color(0.11f, 0.055f, 0.06f),
                0.06f,
                0.22f);
            var emberFeather = CreateMaterial(
                "Cinderwing ember feather",
                new Color(0.57f, 0.13f, 0.075f),
                0.04f,
                0.3f);
            var hotEdge = CreateMaterial(
                "Cinderwing hot edge",
                new Color(0.9f, 0.3f, 0.1f),
                0.08f,
                0.52f,
                1.08f);
            var copper = CreateMaterial(
                "Cinderwing copper quill",
                new Color(0.69f, 0.3f, 0.12f),
                0.46f,
                0.56f);
            var ember = CreateMaterial(
                "Cinderwing ember heart",
                new Color(1f, 0.27f, 0.07f),
                0.05f,
                0.66f,
                2.05f);
            var beak = CreateMaterial(
                "Cinderwing soot beak",
                new Color(0.055f, 0.06f, 0.065f),
                0.12f,
                0.38f);
            var eye = CreateMaterial(
                "Cinderwing eye",
                new Color(1f, 0.61f, 0.17f),
                0.05f,
                0.68f,
                1.35f);

            AddRound(
                root,
                "Cinderwing cinder body",
                new Vector3(0f, 0.02f, -0.02f),
                new Vector3(0.72f, 0.76f, 1.26f),
                Quaternion.Euler(-8f, 0f, 0f),
                cinder);
            AddRound(
                root,
                "Cinderwing ember breast",
                new Vector3(0f, -0.1f, 0.42f),
                new Vector3(0.48f, 0.48f, 0.47f),
                Quaternion.identity,
                ember,
                false);
            AddRound(
                root,
                "Cinderwing hooded head",
                new Vector3(0f, 0.25f, 0.63f),
                new Vector3(0.54f, 0.52f, 0.62f),
                Quaternion.Euler(5f, 0f, 0f),
                cinder);
            AddTapered(
                root,
                "Cinderwing beak",
                new Vector3(0f, 0.18f, 1.03f),
                new Vector3(0.28f, 0.58f, 0.28f),
                Quaternion.Euler(90f, 0f, 0f),
                beak,
                true);

            AddCinderwingWing(root, -1f, cinder, emberFeather, hotEdge, copper);
            AddCinderwingWing(root, 1f, cinder, emberFeather, hotEdge, copper);

            AddBlade(
                root,
                "Cinderwing left fork-tail",
                new Vector3(-0.2f, 0.02f, -1.04f),
                new Vector3(0.36f, 0.07f, 1.18f),
                Quaternion.Euler(8f, 194f, -10f),
                emberFeather);
            AddBlade(
                root,
                "Cinderwing right fork-tail",
                new Vector3(0.2f, 0.02f, -1.04f),
                new Vector3(0.36f, 0.07f, 1.18f),
                Quaternion.Euler(8f, 166f, 10f),
                emberFeather);
            AddBeam(
                root,
                "Cinderwing left tail quill",
                new Vector3(-0.09f, 0.04f, -0.51f),
                new Vector3(-0.38f, 0.05f, -1.57f),
                0.045f,
                copper);
            AddBeam(
                root,
                "Cinderwing right tail quill",
                new Vector3(0.09f, 0.04f, -0.51f),
                new Vector3(0.38f, 0.05f, -1.57f),
                0.045f,
                copper);

            AddBlade(
                root,
                "Cinderwing center crest",
                new Vector3(0f, 0.57f, 0.34f),
                new Vector3(0.2f, 0.055f, 0.55f),
                Quaternion.Euler(-67f, 0f, 0f),
                hotEdge);
            AddBlade(
                root,
                "Cinderwing left crest",
                new Vector3(-0.13f, 0.51f, 0.3f),
                new Vector3(0.16f, 0.045f, 0.43f),
                Quaternion.Euler(-61f, -12f, -17f),
                emberFeather);
            AddBlade(
                root,
                "Cinderwing right crest",
                new Vector3(0.13f, 0.51f, 0.3f),
                new Vector3(0.16f, 0.045f, 0.43f),
                Quaternion.Euler(-61f, 12f, 17f),
                emberFeather);

            AddRound(
                root,
                "Cinderwing left eye",
                new Vector3(-0.16f, 0.34f, 0.89f),
                new Vector3(0.07f, 0.085f, 0.055f),
                Quaternion.identity,
                eye,
                false);
            AddRound(
                root,
                "Cinderwing right eye",
                new Vector3(0.16f, 0.34f, 0.89f),
                new Vector3(0.07f, 0.085f, 0.055f),
                Quaternion.identity,
                eye,
                false);

            AddBeam(
                root,
                "Cinderwing left soot talon",
                new Vector3(-0.19f, -0.32f, 0.08f),
                new Vector3(-0.24f, -0.63f, 0.2f),
                0.055f,
                beak);
            AddBeam(
                root,
                "Cinderwing right soot talon",
                new Vector3(0.19f, -0.32f, 0.08f),
                new Vector3(0.24f, -0.63f, 0.2f),
                0.055f,
                beak);
        }

        private static void AddCinderwingWing(
            Transform root,
            float side,
            Material cinder,
            Material emberFeather,
            Material hotEdge,
            Material copper)
        {
            var shoulder = new Vector3(side * 0.27f, 0.12f, 0.04f);
            for (var index = 0; index < 4; index++)
            {
                var yaw = side * (70f + index * 14f);
                var length = 1.48f + index * 0.17f;
                var direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                var center = shoulder + direction * (length * 0.48f);
                center.y += (index - 1.5f) * 0.035f;

                AddBlade(
                    root,
                    side < 0f
                        ? $"Cinderwing left swept feather {index + 1}"
                        : $"Cinderwing right swept feather {index + 1}",
                    center,
                    new Vector3(0.64f - index * 0.055f, 0.075f, length),
                    Quaternion.Euler(4f + index * 2f, yaw, side * (7f + index * 2f)),
                    index == 0 ? cinder : emberFeather);

                var tip = shoulder + direction * (length * 0.92f);
                AddBeam(
                    root,
                    side < 0f
                        ? $"Cinderwing left copper quill {index + 1}"
                        : $"Cinderwing right copper quill {index + 1}",
                    shoulder,
                    tip,
                    index == 3 ? 0.035f : 0.045f,
                    index == 3 ? hotEdge : copper);
            }
        }

        private static Transform AddRound(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            bool castShadows = true)
        {
            return AddMesh(
                parent,
                name,
                GetFacetedRoundMesh(),
                localPosition,
                localScale,
                localRotation,
                material,
                castShadows);
        }

        private static Transform AddBox(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            return AddMesh(
                parent,
                name,
                GetBoxMesh(),
                localPosition,
                localScale,
                localRotation,
                material,
                true);
        }

        private static Transform AddBlade(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            bool castShadows = true)
        {
            return AddMesh(
                parent,
                name,
                GetBladeMesh(),
                localPosition,
                localScale,
                localRotation,
                material,
                castShadows);
        }

        private static Transform AddTapered(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            bool pointed = false)
        {
            return AddMesh(
                parent,
                name,
                pointed ? GetSpikeMesh() : GetTaperedColumnMesh(),
                localPosition,
                localScale,
                localRotation,
                material,
                true);
        }

        private static Transform AddBeam(
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
                return null;
            }

            return AddMesh(
                parent,
                name,
                GetColumnMesh(),
                (start + end) * 0.5f,
                new Vector3(width, length, width),
                Quaternion.FromToRotation(Vector3.up, direction / length),
                material,
                true);
        }

        private static void AddRing(
            Transform parent,
            string name,
            Vector3 center,
            float radius,
            float width,
            Quaternion rotation,
            Material material,
            int segmentCount)
        {
            var previous = center + rotation * new Vector3(radius, 0f, 0f);
            for (var index = 1; index <= segmentCount; index++)
            {
                var angle = index * Mathf.PI * 2f / segmentCount;
                var next = center + rotation * new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                AddBeam(parent, $"{name} segment {index}", previous, next, width, material);
                previous = next;
            }
        }

        private static Transform AddMesh(
            Transform parent,
            string name,
            Mesh mesh,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            bool castShadows)
        {
            // Deliberately use MeshFilter/MeshRenderer instead of CreatePrimitive:
            // generated art never receives a Collider, even transiently.
            var item = new GameObject(name);
            item.layer = parent.gameObject.layer;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.transform.localRotation = localRotation;

            var filter = item.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;
            return item.transform;
        }

        private static Material CreateMaterial(
            string name,
            Color color,
            float metallic,
            float smoothness,
            float emissionIntensity = 0f,
            bool transparent = false)
        {
            var prototype = GetRuntimeMaterialPrototype();
            Material material;
            if (prototype != null)
            {
                material = new Material(prototype);
            }
            else
            {
                var shader = Shader.Find("Standard") ??
                             Shader.Find("Universal Render Pipeline/Lit") ??
                             Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    Debug.LogError("Tetherfall Echosworn art could not locate a runtime shader.");
                    return null;
                }

                material = new Material(shader);
            }

            material.name = $"TF Echosworn {name}";
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)CullMode.Off);
            }

            if (emissionIntensity > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emissionIntensity);
            }

            if (transparent)
            {
                ConfigureTransparent(material);
            }

            return material;
        }

        private static Material GetRuntimeMaterialPrototype()
        {
            if (!runtimeMaterialSearched)
            {
                runtimeMaterialSearched = true;
                runtimeMaterialPrototype = Resources.Load<Material>(RuntimeMaterialPath);
            }

            return runtimeMaterialPrototype;
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Mesh GetFacetedRoundMesh()
        {
            if (facetedRoundMesh != null)
            {
                return facetedRoundMesh;
            }

            const int longitudeCount = 10;
            const int latitudeCount = 6;
            var ringCount = latitudeCount - 1;
            var sourceVertices = new Vector3[2 + ringCount * longitudeCount];
            sourceVertices[0] = new Vector3(0f, 0.5f, 0f);

            for (var latitude = 1; latitude < latitudeCount; latitude++)
            {
                var polar = latitude * Mathf.PI / latitudeCount;
                var ringRadius = Mathf.Sin(polar) * 0.5f;
                var y = Mathf.Cos(polar) * 0.5f;
                var ringOffset = 1 + (latitude - 1) * longitudeCount;
                for (var longitude = 0; longitude < longitudeCount; longitude++)
                {
                    var azimuth = longitude * Mathf.PI * 2f / longitudeCount;
                    sourceVertices[ringOffset + longitude] = new Vector3(
                        Mathf.Cos(azimuth) * ringRadius,
                        y,
                        Mathf.Sin(azimuth) * ringRadius);
                }
            }

            var bottomIndex = sourceVertices.Length - 1;
            sourceVertices[bottomIndex] = new Vector3(0f, -0.5f, 0f);
            var triangleCount = longitudeCount * 2 +
                                longitudeCount * (ringCount - 1) * 2;
            var sourceTriangles = new int[triangleCount * 3];
            var cursor = 0;

            for (var longitude = 0; longitude < longitudeCount; longitude++)
            {
                var next = (longitude + 1) % longitudeCount;
                sourceTriangles[cursor++] = 0;
                sourceTriangles[cursor++] = 1 + next;
                sourceTriangles[cursor++] = 1 + longitude;
            }

            for (var ring = 0; ring < ringCount - 1; ring++)
            {
                var upper = 1 + ring * longitudeCount;
                var lower = upper + longitudeCount;
                for (var longitude = 0; longitude < longitudeCount; longitude++)
                {
                    var next = (longitude + 1) % longitudeCount;
                    sourceTriangles[cursor++] = upper + longitude;
                    sourceTriangles[cursor++] = upper + next;
                    sourceTriangles[cursor++] = lower + longitude;
                    sourceTriangles[cursor++] = upper + next;
                    sourceTriangles[cursor++] = lower + next;
                    sourceTriangles[cursor++] = lower + longitude;
                }
            }

            var lastRing = 1 + (ringCount - 1) * longitudeCount;
            for (var longitude = 0; longitude < longitudeCount; longitude++)
            {
                var next = (longitude + 1) % longitudeCount;
                sourceTriangles[cursor++] = bottomIndex;
                sourceTriangles[cursor++] = lastRing + longitude;
                sourceTriangles[cursor++] = lastRing + next;
            }

            facetedRoundMesh = CreateFlatMesh(
                "TF Echosworn faceted round",
                sourceVertices,
                sourceTriangles);
            return facetedRoundMesh;
        }

        private static Mesh GetBoxMesh()
        {
            if (boxMesh != null)
            {
                return boxMesh;
            }

            var sourceVertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };
            var sourceTriangles = new[]
            {
                0, 3, 2, 0, 2, 1,
                4, 5, 6, 4, 6, 7,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2
            };
            boxMesh = CreateFlatMesh("TF Echosworn box", sourceVertices, sourceTriangles);
            return boxMesh;
        }

        private static Mesh GetBladeMesh()
        {
            if (bladeMesh != null)
            {
                return bladeMesh;
            }

            var outline = new[]
            {
                new Vector2(-0.5f, -0.5f),
                new Vector2(-0.38f, 0.08f),
                new Vector2(0f, 0.5f),
                new Vector2(0.38f, 0.08f),
                new Vector2(0.5f, -0.5f)
            };
            bladeMesh = CreateExtrudedOutlineMesh("TF Echosworn blade", outline, 0.5f);
            return bladeMesh;
        }

        private static Mesh GetColumnMesh()
        {
            if (columnMesh == null)
            {
                columnMesh = CreateFrustumMesh("TF Echosworn column", 1f);
            }

            return columnMesh;
        }

        private static Mesh GetTaperedColumnMesh()
        {
            if (taperedColumnMesh == null)
            {
                taperedColumnMesh = CreateFrustumMesh("TF Echosworn taper", 0.56f);
            }

            return taperedColumnMesh;
        }

        private static Mesh GetSpikeMesh()
        {
            if (spikeMesh == null)
            {
                spikeMesh = CreateFrustumMesh("TF Echosworn spike", 0.06f);
            }

            return spikeMesh;
        }

        private static Mesh CreateFrustumMesh(string name, float topRadiusRatio)
        {
            const int sideCount = 8;
            var sourceVertices = new Vector3[sideCount * 2 + 2];
            for (var index = 0; index < sideCount; index++)
            {
                var angle = index * Mathf.PI * 2f / sideCount;
                var x = Mathf.Cos(angle) * 0.5f;
                var z = Mathf.Sin(angle) * 0.5f;
                sourceVertices[index] = new Vector3(x, -0.5f, z);
                sourceVertices[sideCount + index] = new Vector3(
                    x * topRadiusRatio,
                    0.5f,
                    z * topRadiusRatio);
            }

            var bottomCenter = sideCount * 2;
            var topCenter = bottomCenter + 1;
            sourceVertices[bottomCenter] = new Vector3(0f, -0.5f, 0f);
            sourceVertices[topCenter] = new Vector3(0f, 0.5f, 0f);
            var sourceTriangles = new int[sideCount * 12];
            var cursor = 0;
            for (var index = 0; index < sideCount; index++)
            {
                var next = (index + 1) % sideCount;
                sourceTriangles[cursor++] = index;
                sourceTriangles[cursor++] = sideCount + index;
                sourceTriangles[cursor++] = next;
                sourceTriangles[cursor++] = next;
                sourceTriangles[cursor++] = sideCount + index;
                sourceTriangles[cursor++] = sideCount + next;

                sourceTriangles[cursor++] = bottomCenter;
                sourceTriangles[cursor++] = index;
                sourceTriangles[cursor++] = next;
                sourceTriangles[cursor++] = topCenter;
                sourceTriangles[cursor++] = sideCount + next;
                sourceTriangles[cursor++] = sideCount + index;
            }

            return CreateFlatMesh(name, sourceVertices, sourceTriangles);
        }

        private static Mesh CreateExtrudedOutlineMesh(
            string name,
            Vector2[] outline,
            float thickness)
        {
            var count = outline.Length;
            var sourceVertices = new Vector3[count * 2];
            var halfThickness = thickness * 0.5f;
            for (var index = 0; index < count; index++)
            {
                sourceVertices[index] = new Vector3(
                    outline[index].x,
                    halfThickness,
                    outline[index].y);
                sourceVertices[count + index] = new Vector3(
                    outline[index].x,
                    -halfThickness,
                    outline[index].y);
            }

            var triangleCount = (count - 2) * 2 + count * 2;
            var sourceTriangles = new int[triangleCount * 3];
            var cursor = 0;
            for (var index = 1; index < count - 1; index++)
            {
                sourceTriangles[cursor++] = 0;
                sourceTriangles[cursor++] = index;
                sourceTriangles[cursor++] = index + 1;
                sourceTriangles[cursor++] = count;
                sourceTriangles[cursor++] = count + index + 1;
                sourceTriangles[cursor++] = count + index;
            }

            for (var index = 0; index < count; index++)
            {
                var next = (index + 1) % count;
                sourceTriangles[cursor++] = index;
                sourceTriangles[cursor++] = count + index;
                sourceTriangles[cursor++] = next;
                sourceTriangles[cursor++] = next;
                sourceTriangles[cursor++] = count + index;
                sourceTriangles[cursor++] = count + next;
            }

            return CreateFlatMesh(name, sourceVertices, sourceTriangles);
        }

        private static Mesh CreateFlatMesh(
            string name,
            Vector3[] sourceVertices,
            int[] sourceTriangles)
        {
            var vertices = new Vector3[sourceTriangles.Length];
            var normals = new Vector3[sourceTriangles.Length];
            var triangles = new int[sourceTriangles.Length];

            for (var index = 0; index < sourceTriangles.Length; index += 3)
            {
                var a = sourceVertices[sourceTriangles[index]];
                var b = sourceVertices[sourceTriangles[index + 1]];
                var c = sourceVertices[sourceTriangles[index + 2]];
                var normal = Vector3.Cross(b - a, c - a).normalized;

                vertices[index] = a;
                vertices[index + 1] = b;
                vertices[index + 2] = c;
                normals[index] = normal;
                normals[index + 1] = normal;
                normals[index + 2] = normal;
                triangles[index] = index;
                triangles[index + 1] = index + 1;
                triangles[index + 2] = index + 2;
            }

            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                normals = normals,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float SafeReciprocal(float value)
        {
            return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
        }
    }
}
