using System;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Builds a collider-free, low-poly Wayfinder shell beneath named rig and equipment roots.
    /// The hierarchy is intentionally segmented so a later skeletal rig can adopt each root
    /// without requiring the procedural meshes to be rebuilt.
    /// </summary>
    public static class TetherfallWayfinderArtOverhaul
    {
        public const string ArtRootName = "Wayfinder Art Overhaul";
        public const string RigAttachmentRootName = "Wayfinder Rig Attachment Roots";

        /// <summary>
        /// Attaches the complete art hierarchy once and returns its root.
        /// </summary>
        public static Transform Attach(TetherfallGame game, Transform wayfinderVisual)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (wayfinderVisual == null)
            {
                throw new ArgumentNullException(nameof(wayfinderVisual));
            }

            HideLegacyWayfinderShell(wayfinderVisual);
            var existingRoot = FindDirectChild(wayfinderVisual, ArtRootName);
            if (existingRoot != null)
            {
                return existingRoot;
            }

            var artRoot = CreateRoot(wayfinderVisual, ArtRootName, Vector3.zero, Vector3.zero);
            var rigRoot = CreateRoot(artRoot, RigAttachmentRootName, Vector3.zero, Vector3.zero);
            var meshes = new MeshLibrary();
            var palette = new MaterialPalette(game);
            var rig = new RigHierarchy(rigRoot);

            BuildBody(meshes, palette, rig);
            BuildFieldCoat(meshes, palette, rig);
            BuildRoutework(meshes, palette, rig);

            return artRoot;
        }

        private static void BuildBody(MeshLibrary meshes, MaterialPalette palette, RigHierarchy rig)
        {
            CreatePart(
                "Body - Faceted pelvis",
                rig.Pelvis,
                meshes.Ellipsoid,
                palette.Underlayer,
                new Vector3(0f, -0.035f, 0.01f),
                Vector3.zero,
                new Vector3(0.44f, 0.33f, 0.35f));
            CreatePart(
                "Body - Inner route vest",
                rig.Chest,
                meshes.Torso,
                palette.Underlayer,
                new Vector3(0f, -0.19f, -0.015f),
                Vector3.zero,
                new Vector3(0.61f, 0.56f, 0.36f));
            CreatePart(
                "Body - Neck",
                rig.Neck,
                meshes.Cylinder,
                palette.Skin,
                new Vector3(0f, -0.035f, -0.015f),
                Vector3.zero,
                new Vector3(0.13f, 0.17f, 0.13f));

            BuildHead(meshes, palette, rig);
            BuildArm(meshes, palette, rig.LeftShoulder, rig.LeftElbow, rig.LeftHand, true);
            BuildArm(meshes, palette, rig.RightShoulder, rig.RightElbow, rig.RightHand, false);
            BuildLeg(meshes, palette, rig.LeftHip, rig.LeftKnee, rig.LeftFoot, true);
            BuildLeg(meshes, palette, rig.RightHip, rig.RightKnee, rig.RightFoot, false);
        }

        private static void BuildHead(MeshLibrary meshes, MaterialPalette palette, RigHierarchy rig)
        {
            CreatePart(
                "Head - Deep slate hood shell",
                rig.Head,
                meshes.Ellipsoid,
                palette.DeepSlate,
                new Vector3(0f, 0.045f, 0.005f),
                Vector3.zero,
                new Vector3(0.41f, 0.45f, 0.39f));
            CreatePart(
                "Head - Hood face opening",
                rig.Head,
                meshes.Ellipsoid,
                palette.Ink,
                new Vector3(0f, 0.018f, -0.198f),
                Vector3.zero,
                new Vector3(0.275f, 0.30f, 0.038f));
            CreatePart(
                "Head - Weathered face plane",
                rig.Head,
                meshes.Ellipsoid,
                palette.Skin,
                new Vector3(0f, 0.002f, -0.221f),
                Vector3.zero,
                new Vector3(0.21f, 0.23f, 0.028f));
            CreatePart(
                "Head - Quiet eye shadow",
                rig.Head,
                meshes.Box,
                palette.FaceShadow,
                new Vector3(0f, 0.043f, -0.239f),
                Vector3.zero,
                new Vector3(0.13f, 0.026f, 0.018f));
            CreatePart(
                "Head - Field scarf mask",
                rig.Head,
                meshes.TaperedDown,
                palette.CoatShadow,
                new Vector3(0f, -0.105f, -0.225f),
                new Vector3(-8f, 0f, 0f),
                new Vector3(0.255f, 0.13f, 0.055f));
            CreatePart(
                "Head - Hood crown break",
                rig.Head,
                meshes.TaperedDown,
                palette.CoatHighlight,
                new Vector3(0f, 0.255f, 0.025f),
                new Vector3(2f, 0f, 0f),
                new Vector3(0.27f, 0.13f, 0.27f));
            CreatePart(
                "Head - Copper route pin",
                rig.Head,
                meshes.Diamond,
                palette.Copper,
                new Vector3(-0.168f, 0.085f, -0.168f),
                new Vector3(0f, 0f, 18f),
                new Vector3(0.055f, 0.09f, 0.035f));
        }

        private static void BuildArm(
            MeshLibrary meshes,
            MaterialPalette palette,
            Transform shoulder,
            Transform elbow,
            Transform hand,
            bool isLeft)
        {
            var sideName = isLeft ? "Left" : "Right";

            CreatePart(
                sideName + " arm - Layered shoulder cap",
                shoulder,
                meshes.Ellipsoid,
                isLeft ? palette.CoatHighlight : palette.Coat,
                new Vector3(0f, -0.02f, 0.01f),
                Vector3.zero,
                new Vector3(0.27f, 0.19f, 0.28f));
            CreatePart(
                sideName + " arm - Tapered field sleeve",
                shoulder,
                meshes.TaperedDown,
                palette.Coat,
                new Vector3(0f, -0.17f, 0.01f),
                Vector3.zero,
                new Vector3(0.205f, 0.34f, 0.20f));
            CreatePart(
                sideName + " arm - Dark forearm wrap",
                elbow,
                meshes.Ellipsoid,
                palette.Underlayer,
                new Vector3(0f, -0.15f, 0f),
                Vector3.zero,
                new Vector3(0.17f, 0.32f, 0.165f));
            CreatePart(
                sideName + " arm - Field leather bracer",
                elbow,
                meshes.TaperedDown,
                palette.Leather,
                new Vector3(0f, -0.145f, -0.087f),
                new Vector3(-2f, 0f, 0f),
                new Vector3(0.18f, 0.235f, 0.055f));
            CreatePart(
                sideName + " arm - Bracer binding",
                elbow,
                meshes.Torus,
                palette.CopperDark,
                new Vector3(0f, -0.06f, 0f),
                new Vector3(90f, 0f, 0f),
                new Vector3(0.19f, 0.19f, 0.12f));
            CreatePart(
                sideName + " hand - Reinforced glove",
                hand,
                meshes.Ellipsoid,
                palette.Glove,
                new Vector3(0f, -0.035f, -0.012f),
                Vector3.zero,
                new Vector3(0.165f, 0.185f, 0.15f));

            if (isLeft)
            {
                CreatePart(
                    "Left shoulder - Survey mantle plate",
                    shoulder,
                    meshes.FlaredDown,
                    palette.CoatHighlight,
                    new Vector3(-0.055f, 0.002f, 0.015f),
                    new Vector3(2f, 0f, -8f),
                    new Vector3(0.33f, 0.205f, 0.32f));
                for (var stitchIndex = 0; stitchIndex < 3; stitchIndex++)
                {
                    CreatePart(
                        "Left shoulder - Copper route stitch " + (stitchIndex + 1),
                        shoulder,
                        meshes.Box,
                        palette.Copper,
                        new Vector3(-0.145f + stitchIndex * 0.07f, 0.012f, -0.153f),
                        new Vector3(0f, 0f, -8f),
                        new Vector3(0.024f, 0.11f, 0.018f));
                }
            }
            else
            {
                CreatePart(
                    "Right bracer - Threadglass bezel",
                    elbow,
                    meshes.Torus,
                    palette.Copper,
                    new Vector3(0f, -0.145f, -0.121f),
                    Vector3.zero,
                    new Vector3(0.12f, 0.12f, 0.07f));
                CreatePart(
                    "Right bracer - Threadglass signal",
                    elbow,
                    meshes.Diamond,
                    palette.Threadglass,
                    new Vector3(0f, -0.145f, -0.142f),
                    new Vector3(0f, 0f, 45f),
                    new Vector3(0.065f, 0.065f, 0.03f));
            }
        }

        private static void BuildLeg(
            MeshLibrary meshes,
            MaterialPalette palette,
            Transform hip,
            Transform knee,
            Transform foot,
            bool isLeft)
        {
            var sideName = isLeft ? "Left" : "Right";

            CreatePart(
                sideName + " leg - Field trouser thigh",
                hip,
                meshes.Ellipsoid,
                palette.Trouser,
                new Vector3(0f, -0.19f, 0f),
                Vector3.zero,
                new Vector3(0.25f, 0.43f, 0.27f));
            CreatePart(
                sideName + " leg - Padded knee",
                knee,
                meshes.Ellipsoid,
                palette.Leather,
                new Vector3(0f, -0.025f, -0.105f),
                Vector3.zero,
                new Vector3(0.205f, 0.17f, 0.115f));
            CreatePart(
                sideName + " leg - Tapered route boot",
                knee,
                meshes.TaperedDown,
                palette.Boot,
                new Vector3(0f, -0.185f, 0f),
                Vector3.zero,
                new Vector3(0.225f, 0.37f, 0.235f));
            CreatePart(
                sideName + " leg - Boot cuff",
                knee,
                meshes.Torus,
                palette.CopperDark,
                new Vector3(0f, -0.065f, 0f),
                new Vector3(90f, 0f, 0f),
                new Vector3(0.235f, 0.235f, 0.14f));
            CreatePart(
                sideName + " foot - Broad field sole",
                foot,
                meshes.TaperedDown,
                palette.Boot,
                new Vector3(0f, 0.015f, -0.105f),
                new Vector3(3f, 0f, 0f),
                new Vector3(0.25f, 0.16f, 0.39f));
            CreatePart(
                sideName + " foot - Slate toe cap",
                foot,
                meshes.FlaredDown,
                palette.DeepSlate,
                new Vector3(0f, 0.045f, -0.245f),
                new Vector3(86f, 0f, 0f),
                new Vector3(0.22f, 0.13f, 0.14f));
        }

        private static void BuildFieldCoat(
            MeshLibrary meshes,
            MaterialPalette palette,
            RigHierarchy rig)
        {
            CreatePart(
                "Coat - Tapered field coat body",
                rig.Chest,
                meshes.Torso,
                palette.Coat,
                new Vector3(0f, -0.185f, 0.018f),
                Vector3.zero,
                new Vector3(0.75f, 0.62f, 0.44f));
            CreatePart(
                "Coat - Left storm lapel",
                rig.Chest,
                meshes.TaperedDown,
                palette.CoatHighlight,
                new Vector3(-0.135f, -0.13f, -0.226f),
                new Vector3(-8f, 0f, 16f),
                new Vector3(0.19f, 0.43f, 0.052f));
            CreatePart(
                "Coat - Right storm lapel",
                rig.Chest,
                meshes.TaperedDown,
                palette.CoatShadow,
                new Vector3(0.135f, -0.13f, -0.226f),
                new Vector3(-8f, 0f, -16f),
                new Vector3(0.19f, 0.43f, 0.052f));
            CreatePart(
                "Coat - Left raised collar",
                rig.Chest,
                meshes.FlaredDown,
                palette.CoatShadow,
                new Vector3(-0.13f, 0.105f, -0.12f),
                new Vector3(-16f, 0f, 25f),
                new Vector3(0.25f, 0.19f, 0.10f));
            CreatePart(
                "Coat - Right raised collar",
                rig.Chest,
                meshes.FlaredDown,
                palette.CoatHighlight,
                new Vector3(0.13f, 0.105f, -0.12f),
                new Vector3(-16f, 0f, -25f),
                new Vector3(0.25f, 0.19f, 0.10f));
            CreatePart(
                "Coat - Woven cowl ring",
                rig.Neck,
                meshes.Torus,
                palette.MutedReed,
                new Vector3(0f, -0.085f, 0f),
                new Vector3(90f, 0f, 0f),
                new Vector3(0.52f, 0.40f, 0.30f));

            CreatePart(
                "Coat - Left split front tail",
                rig.Pelvis,
                meshes.FlaredDown,
                palette.CoatHighlight,
                new Vector3(-0.145f, -0.295f, -0.175f),
                new Vector3(4f, 0f, 3f),
                new Vector3(0.245f, 0.53f, 0.075f));
            CreatePart(
                "Coat - Right split front tail",
                rig.Pelvis,
                meshes.FlaredDown,
                palette.Coat,
                new Vector3(0.145f, -0.295f, -0.175f),
                new Vector3(4f, 0f, -3f),
                new Vector3(0.245f, 0.53f, 0.075f));
            CreatePart(
                "Coat - Broad weather back tail",
                rig.Pelvis,
                meshes.FlaredDown,
                palette.CoatShadow,
                new Vector3(0f, -0.30f, 0.155f),
                new Vector3(-4f, 0f, 0f),
                new Vector3(0.49f, 0.57f, 0.095f));
            CreatePart(
                "Coat - Left side gusset",
                rig.Pelvis,
                meshes.TaperedDown,
                palette.DeepSlate,
                new Vector3(-0.285f, -0.245f, 0.01f),
                new Vector3(0f, 0f, -7f),
                new Vector3(0.14f, 0.45f, 0.20f));
            CreatePart(
                "Coat - Right side gusset",
                rig.Pelvis,
                meshes.TaperedDown,
                palette.DeepSlate,
                new Vector3(0.285f, -0.245f, 0.01f),
                new Vector3(0f, 0f, 7f),
                new Vector3(0.14f, 0.45f, 0.20f));
            CreatePart(
                "Coat - Asymmetric route scarf tail",
                rig.Chest,
                meshes.FlaredDown,
                palette.MutedReed,
                new Vector3(0.245f, -0.335f, 0.245f),
                new Vector3(-7f, 10f, -8f),
                new Vector3(0.17f, 0.51f, 0.052f));

            CreatePart(
                "Coat - Field belt",
                rig.Pelvis,
                meshes.Torus,
                palette.Leather,
                new Vector3(0f, 0.075f, 0f),
                new Vector3(90f, 0f, 0f),
                new Vector3(0.62f, 0.47f, 0.34f));
            CreatePart(
                "Coat - Copper belt bridge",
                rig.Pelvis,
                meshes.Box,
                palette.Copper,
                new Vector3(0f, 0.073f, -0.247f),
                Vector3.zero,
                new Vector3(0.19f, 0.105f, 0.045f));
            CreatePart(
                "Coat - Threadglass belt key",
                rig.Pelvis,
                meshes.Diamond,
                palette.Threadglass,
                new Vector3(0f, 0.073f, -0.275f),
                new Vector3(0f, 0f, 45f),
                new Vector3(0.065f, 0.065f, 0.027f));
        }

        private static void BuildRoutework(
            MeshLibrary meshes,
            MaterialPalette palette,
            RigHierarchy rig)
        {
            CreatePart(
                "Threadglass chest compass - Copper bezel",
                rig.ChestThreadglassSocket,
                meshes.Torus,
                palette.Copper,
                Vector3.zero,
                Vector3.zero,
                new Vector3(0.285f, 0.285f, 0.15f));
            CreatePart(
                "Threadglass chest compass - Resonant lens",
                rig.ChestThreadglassSocket,
                meshes.Diamond,
                palette.Threadglass,
                new Vector3(0f, 0f, -0.025f),
                new Vector3(0f, 0f, 45f),
                new Vector3(0.135f, 0.185f, 0.065f));
            CreatePart(
                "Threadglass chest compass - Lower route index",
                rig.ChestThreadglassSocket,
                meshes.Box,
                palette.CopperDark,
                new Vector3(0f, -0.19f, 0.02f),
                Vector3.zero,
                new Vector3(0.055f, 0.11f, 0.045f));
            CreatePart(
                "Threadglass chest compass - Left brace",
                rig.ChestThreadglassSocket,
                meshes.Box,
                palette.CopperDark,
                new Vector3(-0.135f, -0.105f, 0.018f),
                new Vector3(0f, 0f, -34f),
                new Vector3(0.035f, 0.18f, 0.035f));
            CreatePart(
                "Threadglass chest compass - Right brace",
                rig.ChestThreadglassSocket,
                meshes.Box,
                palette.CopperDark,
                new Vector3(0.135f, -0.105f, 0.018f),
                new Vector3(0f, 0f, 34f),
                new Vector3(0.035f, 0.18f, 0.035f));

            CreatePart(
                "Back routeframe - Slate field plate",
                rig.BackEquipmentSocket,
                meshes.Torso,
                palette.DeepSlate,
                new Vector3(0f, -0.11f, 0f),
                Vector3.zero,
                new Vector3(0.41f, 0.43f, 0.10f));
            CreatePart(
                "Back routeframe - Left copper rail",
                rig.BackEquipmentSocket,
                meshes.Cylinder,
                palette.CopperDark,
                new Vector3(-0.145f, -0.10f, 0.055f),
                Vector3.zero,
                new Vector3(0.035f, 0.39f, 0.035f));
            CreatePart(
                "Back routeframe - Right copper rail",
                rig.BackEquipmentSocket,
                meshes.Cylinder,
                palette.CopperDark,
                new Vector3(0.145f, -0.10f, 0.055f),
                Vector3.zero,
                new Vector3(0.035f, 0.39f, 0.035f));
            CreatePart(
                "Back routeframe - Survey roll",
                rig.BackEquipmentSocket,
                meshes.Cylinder,
                palette.Parchment,
                new Vector3(0f, -0.30f, 0.075f),
                new Vector3(0f, 0f, 90f),
                new Vector3(0.095f, 0.38f, 0.095f));
            CreatePart(
                "Back routeframe - Survey roll binding",
                rig.BackEquipmentSocket,
                meshes.Torus,
                palette.Leather,
                new Vector3(0f, -0.30f, 0.075f),
                new Vector3(0f, 90f, 0f),
                new Vector3(0.12f, 0.12f, 0.075f));

            CreatePart(
                "Left hip utility - Chimesalt vial",
                rig.LeftHipUtilitySocket,
                meshes.Cylinder,
                palette.CopperDark,
                new Vector3(0f, -0.10f, 0f),
                Vector3.zero,
                new Vector3(0.075f, 0.19f, 0.075f));
            CreatePart(
                "Left hip utility - Chimesalt glint",
                rig.LeftHipUtilitySocket,
                meshes.Diamond,
                palette.Threadglass,
                new Vector3(0f, -0.105f, -0.055f),
                new Vector3(0f, 0f, 45f),
                new Vector3(0.05f, 0.09f, 0.03f));
            CreatePart(
                "Right hip utility - Route ledger case",
                rig.RightHipUtilitySocket,
                meshes.TaperedDown,
                palette.Leather,
                new Vector3(0f, -0.12f, 0f),
                new Vector3(0f, 0f, -4f),
                new Vector3(0.18f, 0.27f, 0.08f));
            CreatePart(
                "Right hip utility - Copper ledger clasp",
                rig.RightHipUtilitySocket,
                meshes.Box,
                palette.Copper,
                new Vector3(0f, -0.10f, -0.052f),
                Vector3.zero,
                new Vector3(0.06f, 0.08f, 0.025f));
        }

        private static Transform CreatePart(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            var partObject = new GameObject(name);
            partObject.layer = parent.gameObject.layer;
            var partTransform = partObject.transform;
            partTransform.SetParent(parent, false);
            partTransform.localPosition = localPosition;
            partTransform.localRotation = Quaternion.Euler(localEulerAngles);
            partTransform.localScale = localScale;

            var meshFilter = partObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = partObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            return partTransform;
        }

        private static Transform CreateRoot(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            var rootObject = new GameObject(name);
            rootObject.layer = parent.gameObject.layer;
            var root = rootObject.transform;
            root.SetParent(parent, false);
            root.localPosition = localPosition;
            root.localRotation = Quaternion.Euler(localEulerAngles);
            root.localScale = Vector3.one;
            return root;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (var childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                var child = parent.GetChild(childIndex);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void HideLegacyWayfinderShell(Transform wayfinderVisual)
        {
            for (var childIndex = 0; childIndex < wayfinderVisual.childCount; childIndex++)
            {
                var child = wayfinderVisual.GetChild(childIndex);
                if (!IsLegacyShellPart(child.name))
                {
                    continue;
                }

                var legacyRenderer = child.GetComponent<Renderer>();
                if (legacyRenderer != null)
                {
                    legacyRenderer.enabled = false;
                }
            }
        }

        private static bool IsLegacyShellPart(string partName)
        {
            return partName == "Wayfinder Coat" ||
                   partName == "Threadglass" ||
                   partName == "Wayfinder hood" ||
                   partName == "Wayfinder mantle" ||
                   partName == "Wayfinder Threadglass";
        }

        private sealed class RigHierarchy
        {
            public readonly Transform Pelvis;
            public readonly Transform Chest;
            public readonly Transform Neck;
            public readonly Transform Head;
            public readonly Transform LeftShoulder;
            public readonly Transform LeftElbow;
            public readonly Transform LeftHand;
            public readonly Transform RightShoulder;
            public readonly Transform RightElbow;
            public readonly Transform RightHand;
            public readonly Transform LeftHip;
            public readonly Transform LeftKnee;
            public readonly Transform LeftFoot;
            public readonly Transform RightHip;
            public readonly Transform RightKnee;
            public readonly Transform RightFoot;
            public readonly Transform ChestThreadglassSocket;
            public readonly Transform BackEquipmentSocket;
            public readonly Transform LeftHipUtilitySocket;
            public readonly Transform RightHipUtilitySocket;

            public RigHierarchy(Transform rigRoot)
            {
                Pelvis = CreateRoot(
                    rigRoot,
                    "Rig Root - Pelvis",
                    new Vector3(0f, 0.80f, 0f),
                    Vector3.zero);
                var lowerSpine = CreateRoot(
                    Pelvis,
                    "Rig Root - Lower Spine",
                    new Vector3(0f, 0.16f, 0f),
                    Vector3.zero);
                Chest = CreateRoot(
                    lowerSpine,
                    "Rig Root - Chest",
                    new Vector3(0f, 0.27f, -0.005f),
                    Vector3.zero);
                Neck = CreateRoot(
                    Chest,
                    "Rig Root - Neck",
                    new Vector3(0f, 0.27f, -0.01f),
                    Vector3.zero);
                Head = CreateRoot(
                    Neck,
                    "Rig Root - Head",
                    new Vector3(0f, 0.18f, -0.015f),
                    Vector3.zero);

                LeftShoulder = CreateRoot(
                    Chest,
                    "Rig Root - Left Shoulder",
                    new Vector3(-0.36f, 0.10f, 0f),
                    new Vector3(0f, 0f, -13f));
                LeftElbow = CreateRoot(
                    LeftShoulder,
                    "Rig Root - Left Elbow",
                    new Vector3(0f, -0.34f, 0f),
                    new Vector3(0f, 0f, 5f));
                LeftHand = CreateRoot(
                    LeftElbow,
                    "Rig Root - Left Hand",
                    new Vector3(0f, -0.31f, -0.005f),
                    Vector3.zero);

                RightShoulder = CreateRoot(
                    Chest,
                    "Rig Root - Right Shoulder",
                    new Vector3(0.36f, 0.10f, 0f),
                    new Vector3(0f, 0f, 13f));
                RightElbow = CreateRoot(
                    RightShoulder,
                    "Rig Root - Right Elbow",
                    new Vector3(0f, -0.34f, 0f),
                    new Vector3(0f, 0f, -5f));
                RightHand = CreateRoot(
                    RightElbow,
                    "Rig Root - Right Hand",
                    new Vector3(0f, -0.31f, -0.005f),
                    Vector3.zero);

                LeftHip = CreateRoot(
                    Pelvis,
                    "Rig Root - Left Hip",
                    new Vector3(-0.18f, -0.02f, 0f),
                    new Vector3(0f, 0f, -3f));
                LeftKnee = CreateRoot(
                    LeftHip,
                    "Rig Root - Left Knee",
                    new Vector3(0f, -0.39f, 0f),
                    new Vector3(0f, 0f, 2f));
                LeftFoot = CreateRoot(
                    LeftKnee,
                    "Rig Root - Left Foot",
                    new Vector3(0f, -0.34f, -0.015f),
                    Vector3.zero);

                RightHip = CreateRoot(
                    Pelvis,
                    "Rig Root - Right Hip",
                    new Vector3(0.18f, -0.02f, 0f),
                    new Vector3(0f, 0f, 3f));
                RightKnee = CreateRoot(
                    RightHip,
                    "Rig Root - Right Knee",
                    new Vector3(0f, -0.39f, 0f),
                    new Vector3(0f, 0f, -2f));
                RightFoot = CreateRoot(
                    RightKnee,
                    "Rig Root - Right Foot",
                    new Vector3(0f, -0.34f, -0.015f),
                    Vector3.zero);

                ChestThreadglassSocket = CreateRoot(
                    Chest,
                    "Socket - Chest Threadglass",
                    new Vector3(0f, -0.105f, -0.252f),
                    Vector3.zero);
                BackEquipmentSocket = CreateRoot(
                    Chest,
                    "Socket - Back Equipment",
                    new Vector3(0f, -0.09f, 0.258f),
                    Vector3.zero);
                LeftHipUtilitySocket = CreateRoot(
                    Pelvis,
                    "Socket - Left Hip Utility",
                    new Vector3(-0.31f, 0f, -0.015f),
                    new Vector3(0f, 0f, 4f));
                RightHipUtilitySocket = CreateRoot(
                    Pelvis,
                    "Socket - Right Hip Utility",
                    new Vector3(0.31f, 0f, -0.015f),
                    new Vector3(0f, 0f, -4f));

                CreateRoot(
                    Head,
                    "Socket - Headwear",
                    new Vector3(0f, 0.275f, 0f),
                    Vector3.zero);
                CreateRoot(
                    LeftShoulder,
                    "Socket - Left Shoulder Equipment",
                    new Vector3(0f, 0.08f, 0f),
                    Vector3.zero);
                CreateRoot(
                    RightShoulder,
                    "Socket - Right Shoulder Equipment",
                    new Vector3(0f, 0.08f, 0f),
                    Vector3.zero);
                CreateRoot(
                    LeftHand,
                    "Socket - Left Hand Grip",
                    new Vector3(0f, -0.08f, -0.08f),
                    Vector3.zero);
                CreateRoot(
                    RightHand,
                    "Socket - Right Hand Grip",
                    new Vector3(0f, -0.08f, -0.08f),
                    Vector3.zero);
                CreateRoot(
                    BackEquipmentSocket,
                    "Socket - Glider",
                    new Vector3(0f, 0.08f, 0.08f),
                    Vector3.zero);
            }
        }

        private sealed class MaterialPalette
        {
            public readonly Material Ink;
            public readonly Material DeepSlate;
            public readonly Material Underlayer;
            public readonly Material Coat;
            public readonly Material CoatHighlight;
            public readonly Material CoatShadow;
            public readonly Material Trouser;
            public readonly Material Leather;
            public readonly Material Boot;
            public readonly Material Glove;
            public readonly Material Skin;
            public readonly Material FaceShadow;
            public readonly Material MutedReed;
            public readonly Material Parchment;
            public readonly Material Copper;
            public readonly Material CopperDark;
            public readonly Material Threadglass;

            public MaterialPalette(TetherfallGame game)
            {
                Ink = game.CreateMaterial(new Color(0.018f, 0.03f, 0.035f), 0.02f, 0.19f);
                DeepSlate = game.CreateMaterial(new Color(0.055f, 0.11f, 0.13f), 0.04f, 0.27f);
                Underlayer = game.CreateMaterial(new Color(0.075f, 0.135f, 0.145f), 0.02f, 0.23f);
                Coat = game.CreateMaterial(new Color(0.095f, 0.255f, 0.275f), 0.04f, 0.31f);
                CoatHighlight = game.CreateMaterial(new Color(0.13f, 0.34f, 0.35f), 0.05f, 0.34f);
                CoatShadow = game.CreateMaterial(new Color(0.065f, 0.18f, 0.205f), 0.03f, 0.25f);
                Trouser = game.CreateMaterial(new Color(0.10f, 0.17f, 0.18f), 0.02f, 0.21f);
                Leather = game.CreateMaterial(new Color(0.23f, 0.15f, 0.095f), 0.03f, 0.24f);
                Boot = game.CreateMaterial(new Color(0.105f, 0.075f, 0.06f), 0.02f, 0.19f);
                Glove = game.CreateMaterial(new Color(0.12f, 0.10f, 0.08f), 0.02f, 0.18f);
                Skin = game.CreateMaterial(new Color(0.58f, 0.39f, 0.28f), 0.01f, 0.31f);
                FaceShadow = game.CreateMaterial(new Color(0.12f, 0.075f, 0.06f), 0f, 0.16f);
                MutedReed = game.CreateMaterial(new Color(0.39f, 0.53f, 0.53f), 0.02f, 0.28f);
                Parchment = game.CreateMaterial(new Color(0.68f, 0.58f, 0.39f), 0.01f, 0.25f);
                Copper = game.CreateMaterial(new Color(0.70f, 0.36f, 0.16f), 0.48f, 0.55f);
                CopperDark = game.CreateMaterial(new Color(0.38f, 0.19f, 0.10f), 0.34f, 0.42f);
                Threadglass = game.CreateGlowMaterial(new Color(0.20f, 0.78f, 0.70f), 1.75f, 0.67f);
            }
        }

        private sealed class MeshLibrary
        {
            public readonly Mesh Box;
            public readonly Mesh Ellipsoid;
            public readonly Mesh Cylinder;
            public readonly Mesh Torus;
            public readonly Mesh Torso;
            public readonly Mesh TaperedDown;
            public readonly Mesh FlaredDown;
            public readonly Mesh Diamond;

            public MeshLibrary()
            {
                Box = CreateFrustumMesh("Wayfinder Art - Box", 1f, 1f, 1f, 1f);
                Ellipsoid = CreateEllipsoidMesh("Wayfinder Art - Low Poly Ellipsoid", 10, 6);
                Cylinder = CreateCylinderMesh("Wayfinder Art - Decagonal Cylinder", 10);
                Torus = CreateTorusMesh("Wayfinder Art - Route Ring", 12, 5, 0.38f, 0.12f);
                Torso = CreateFrustumMesh("Wayfinder Art - Coat Torso", 1f, 1f, 0.70f, 0.82f);
                TaperedDown = CreateFrustumMesh("Wayfinder Art - Tapered Form", 1f, 1f, 0.72f, 0.84f);
                FlaredDown = CreateFrustumMesh("Wayfinder Art - Flared Cloth", 0.72f, 0.84f, 1f, 1f);
                Diamond = CreateDiamondMesh("Wayfinder Art - Route Diamond");
            }
        }

        private static Mesh CreateFrustumMesh(
            string meshName,
            float topWidth,
            float topDepth,
            float bottomWidth,
            float bottomDepth)
        {
            var bottomFrontLeft = new Vector3(-bottomWidth * 0.5f, -0.5f, -bottomDepth * 0.5f);
            var bottomFrontRight = new Vector3(bottomWidth * 0.5f, -0.5f, -bottomDepth * 0.5f);
            var bottomBackRight = new Vector3(bottomWidth * 0.5f, -0.5f, bottomDepth * 0.5f);
            var bottomBackLeft = new Vector3(-bottomWidth * 0.5f, -0.5f, bottomDepth * 0.5f);
            var topFrontLeft = new Vector3(-topWidth * 0.5f, 0.5f, -topDepth * 0.5f);
            var topFrontRight = new Vector3(topWidth * 0.5f, 0.5f, -topDepth * 0.5f);
            var topBackRight = new Vector3(topWidth * 0.5f, 0.5f, topDepth * 0.5f);
            var topBackLeft = new Vector3(-topWidth * 0.5f, 0.5f, topDepth * 0.5f);

            var vertices = new[]
            {
                bottomFrontLeft, topFrontLeft, topFrontRight, bottomFrontRight,
                bottomFrontRight, topFrontRight, topBackRight, bottomBackRight,
                bottomBackRight, topBackRight, topBackLeft, bottomBackLeft,
                bottomBackLeft, topBackLeft, topFrontLeft, bottomFrontLeft,
                topFrontLeft, topBackLeft, topBackRight, topFrontRight,
                bottomFrontLeft, bottomFrontRight, bottomBackRight, bottomBackLeft
            };
            var triangles = CreateQuadTriangles(6);
            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static int[] CreateQuadTriangles(int quadCount)
        {
            var triangles = new int[quadCount * 6];
            for (var quadIndex = 0; quadIndex < quadCount; quadIndex++)
            {
                var vertexIndex = quadIndex * 4;
                var triangleIndex = quadIndex * 6;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = vertexIndex + 2;
                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = vertexIndex + 2;
                triangles[triangleIndex + 5] = vertexIndex + 3;
            }

            return triangles;
        }

        private static Mesh CreateEllipsoidMesh(string meshName, int longitudeSegments, int latitudeSegments)
        {
            var rowLength = longitudeSegments + 1;
            var vertices = new Vector3[(latitudeSegments + 1) * rowLength];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[latitudeSegments * longitudeSegments * 6];
            var triangleIndex = 0;

            for (var latitude = 0; latitude <= latitudeSegments; latitude++)
            {
                var latitudeRatio = latitude / (float)latitudeSegments;
                var polarAngle = latitudeRatio * Mathf.PI;
                var ringRadius = Mathf.Sin(polarAngle) * 0.5f;
                var y = Mathf.Cos(polarAngle) * 0.5f;

                for (var longitude = 0; longitude <= longitudeSegments; longitude++)
                {
                    var longitudeRatio = longitude / (float)longitudeSegments;
                    var angle = longitudeRatio * Mathf.PI * 2f;
                    var vertexIndex = latitude * rowLength + longitude;
                    var vertex = new Vector3(
                        Mathf.Sin(angle) * ringRadius,
                        y,
                        Mathf.Cos(angle) * ringRadius);
                    vertices[vertexIndex] = vertex;
                    normals[vertexIndex] = vertex.normalized;
                }
            }

            for (var latitude = 0; latitude < latitudeSegments; latitude++)
            {
                for (var longitude = 0; longitude < longitudeSegments; longitude++)
                {
                    var current = latitude * rowLength + longitude;
                    var nextRow = current + rowLength;
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = nextRow;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = nextRow;
                    triangles[triangleIndex++] = nextRow + 1;
                }
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                normals = normals,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh CreateCylinderMesh(string meshName, int sideCount)
        {
            var sideVertexCount = (sideCount + 1) * 2;
            var capVertexCount = (sideCount + 2) * 2;
            var vertices = new Vector3[sideVertexCount + capVertexCount];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[sideCount * 12];
            var triangleIndex = 0;

            for (var side = 0; side <= sideCount; side++)
            {
                var angle = side / (float)sideCount * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var bottomIndex = side * 2;
                vertices[bottomIndex] = radial * 0.5f + Vector3.down * 0.5f;
                vertices[bottomIndex + 1] = radial * 0.5f + Vector3.up * 0.5f;
                normals[bottomIndex] = radial;
                normals[bottomIndex + 1] = radial;
            }

            for (var side = 0; side < sideCount; side++)
            {
                var bottomCurrent = side * 2;
                var topCurrent = bottomCurrent + 1;
                var bottomNext = bottomCurrent + 2;
                var topNext = bottomCurrent + 3;
                triangles[triangleIndex++] = bottomCurrent;
                triangles[triangleIndex++] = bottomNext;
                triangles[triangleIndex++] = topCurrent;
                triangles[triangleIndex++] = bottomNext;
                triangles[triangleIndex++] = topNext;
                triangles[triangleIndex++] = topCurrent;
            }

            var topCenterIndex = sideVertexCount;
            vertices[topCenterIndex] = Vector3.up * 0.5f;
            normals[topCenterIndex] = Vector3.up;
            for (var side = 0; side <= sideCount; side++)
            {
                var angle = side / (float)sideCount * Mathf.PI * 2f;
                var ringIndex = topCenterIndex + 1 + side;
                vertices[ringIndex] = new Vector3(Mathf.Sin(angle) * 0.5f, 0.5f, Mathf.Cos(angle) * 0.5f);
                normals[ringIndex] = Vector3.up;
            }

            for (var side = 0; side < sideCount; side++)
            {
                triangles[triangleIndex++] = topCenterIndex;
                triangles[triangleIndex++] = topCenterIndex + 1 + side;
                triangles[triangleIndex++] = topCenterIndex + 2 + side;
            }

            var bottomCenterIndex = topCenterIndex + sideCount + 2;
            vertices[bottomCenterIndex] = Vector3.down * 0.5f;
            normals[bottomCenterIndex] = Vector3.down;
            for (var side = 0; side <= sideCount; side++)
            {
                var angle = side / (float)sideCount * Mathf.PI * 2f;
                var ringIndex = bottomCenterIndex + 1 + side;
                vertices[ringIndex] = new Vector3(Mathf.Sin(angle) * 0.5f, -0.5f, Mathf.Cos(angle) * 0.5f);
                normals[ringIndex] = Vector3.down;
            }

            for (var side = 0; side < sideCount; side++)
            {
                triangles[triangleIndex++] = bottomCenterIndex;
                triangles[triangleIndex++] = bottomCenterIndex + 2 + side;
                triangles[triangleIndex++] = bottomCenterIndex + 1 + side;
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                normals = normals,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh CreateTorusMesh(
            string meshName,
            int ringSegments,
            int tubeSegments,
            float ringRadius,
            float tubeRadius)
        {
            var rowLength = tubeSegments + 1;
            var vertices = new Vector3[(ringSegments + 1) * rowLength];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[ringSegments * tubeSegments * 6];
            var triangleIndex = 0;

            for (var ring = 0; ring <= ringSegments; ring++)
            {
                var ringAngle = ring / (float)ringSegments * Mathf.PI * 2f;
                var ringDirection = new Vector3(Mathf.Cos(ringAngle), Mathf.Sin(ringAngle), 0f);
                var ringCenter = ringDirection * ringRadius;

                for (var tube = 0; tube <= tubeSegments; tube++)
                {
                    var tubeAngle = tube / (float)tubeSegments * Mathf.PI * 2f;
                    var normal = ringDirection * Mathf.Cos(tubeAngle) + Vector3.forward * Mathf.Sin(tubeAngle);
                    var vertexIndex = ring * rowLength + tube;
                    vertices[vertexIndex] = ringCenter + normal * tubeRadius;
                    normals[vertexIndex] = normal;
                }
            }

            for (var ring = 0; ring < ringSegments; ring++)
            {
                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var current = ring * rowLength + tube;
                    var nextRing = current + rowLength;
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = nextRing;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = nextRing;
                    triangles[triangleIndex++] = nextRing + 1;
                }
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                normals = normals,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh CreateDiamondMesh(string meshName)
        {
            var top = new Vector3(0f, 0.5f, 0f);
            var bottom = new Vector3(0f, -0.5f, 0f);
            var front = new Vector3(0f, 0f, -0.24f);
            var right = new Vector3(0.5f, 0f, 0f);
            var back = new Vector3(0f, 0f, 0.24f);
            var left = new Vector3(-0.5f, 0f, 0f);
            var vertices = new[]
            {
                top, right, front,
                top, back, right,
                top, left, back,
                top, front, left,
                bottom, front, right,
                bottom, right, back,
                bottom, back, left,
                bottom, left, front
            };
            var triangles = new int[vertices.Length];
            for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                triangles[vertexIndex] = vertexIndex;
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
