using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public interface ITetherfallSettlementWorkerProxy
    {
        string PactInstanceId { get; }

        void SetSettlementProxyState(
            bool visibleNearSettlement,
            Vector3 workPosition,
            Vector3 workForward,
            SettlementWorkRole role,
            float efficiency);
    }

    public readonly struct SettlementSnapPoint
    {
        public SettlementSnapPoint(
            Vector3 position,
            Vector3 direction,
            bool supportsWithoutGround)
        {
            Position = position;
            Direction = direction;
            SupportsWithoutGround = supportsWithoutGround;
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public bool SupportsWithoutGround { get; }
    }

    public readonly struct SettlementSnapResult
    {
        public SettlementSnapResult(
            Vector3 position,
            TetherfallSettlementPiece source,
            bool supportsWithoutGround)
        {
            Position = position;
            Source = source;
            SupportsWithoutGround = supportsWithoutGround;
        }

        public Vector3 Position { get; }
        public TetherfallSettlementPiece Source { get; }
        public bool SupportsWithoutGround { get; }
        public bool IsValid => Source != null;
    }

    public sealed class TetherfallSettlementPiece : MonoBehaviour, IWorldInteractable
    {
        private readonly List<Transform> snapSockets = new List<Transform>();
        private readonly List<Collider> gameplayColliders = new List<Collider>(4);
        private TetherfallGame game;
        private SettlementPlacedPieceSaveData saveRecord;
        private SettlementPieceDefinition definition;
        private Transform visualRoot;
        private SettlementSimulationBand simulationBand;

        public string InstanceId => saveRecord != null ? saveRecord.instanceId : string.Empty;
        public string DefinitionId => definition != null ? definition.id : string.Empty;
        public SettlementPieceDefinition Definition => definition;
        public int Durability => saveRecord != null ? saveRecord.durability : 0;
        public int MaximumDurability => definition != null ? definition.maximumDurability : 0;
        public SettlementSimulationBand SimulationBand => simulationBand;
        public int GameplayColliderCount => gameplayColliders.Count;

        public string InteractionPrompt
        {
            get
            {
                if (definition == null)
                {
                    return "Inspect settlement piece";
                }

                return Durability < MaximumDurability
                    ? $"Repair or manage {definition.displayName}"
                    : $"Manage {definition.displayName}";
            }
        }

        public static TetherfallSettlementPiece Create(
            TetherfallGame game,
            Transform parent,
            SettlementPlacedPieceSaveData record,
            SettlementPieceDefinition definition)
        {
            if (game == null || record == null || definition == null)
            {
                return null;
            }

            var root = new GameObject($"Settlement Piece · {definition.displayName}");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            root.transform.position = record.position;
            root.transform.rotation = Quaternion.Euler(0f, record.rotationY, 0f);
            var runtime = root.AddComponent<TetherfallSettlementPiece>();
            runtime.game = game;
            runtime.saveRecord = record;
            runtime.definition = definition;
            runtime.BuildRuntime();
            return runtime;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null &&
                   owner == game &&
                   definition != null &&
                   simulationBand != SettlementSimulationBand.Abstract;
        }

        public void Interact(TetherfallGame owner)
        {
            owner?.OpenPlayerSettlementPanelForPiece(InstanceId);
        }

        public void SetDurability(int durability)
        {
            if (saveRecord == null || definition == null)
            {
                return;
            }

            saveRecord.durability = Mathf.Clamp(
                durability,
                0,
                definition.maximumDurability);
        }

        public void SetSimulationBand(SettlementSimulationBand band)
        {
            simulationBand = band;
            var abstracted = band == SettlementSimulationBand.Abstract;
            if (visualRoot != null && visualRoot.gameObject.activeSelf == abstracted)
            {
                visualRoot.gameObject.SetActive(!abstracted);
            }

            foreach (var gameplayCollider in gameplayColliders)
            {
                if (gameplayCollider != null)
                {
                    gameplayCollider.enabled = !abstracted;
                }
            }
        }

        public void CopyTransformToRecord()
        {
            if (saveRecord == null)
            {
                return;
            }

            saveRecord.position = transform.position;
            saveRecord.rotationY = transform.eulerAngles.y;
        }

        public void CollectSnapPoints(List<SettlementSnapPoint> destination)
        {
            if (destination == null || definition == null || !definition.UsesStructuralSockets)
            {
                return;
            }

            for (var index = 0; index < snapSockets.Count; index++)
            {
                var socket = snapSockets[index];
                if (socket == null)
                {
                    continue;
                }

                var localDirection = socket.name switch
                {
                    "Snap_N" => Vector3.forward,
                    "Snap_E" => Vector3.right,
                    "Snap_S" => Vector3.back,
                    "Snap_W" => Vector3.left,
                    "Snap_Up" => Vector3.up,
                    "Snap_Down" => Vector3.down,
                    _ => Vector3.zero
                };
                if (localDirection == Vector3.zero)
                {
                    continue;
                }

                var worldDirection = transform.TransformDirection(localDirection).normalized;
                destination.Add(new SettlementSnapPoint(
                    socket.position,
                    worldDirection,
                    socket.name == "Snap_Up"));
            }
        }

        public bool TryGetWorkSocketPose(
            int slotIndex,
            out Vector3 position,
            out Vector3 forward)
        {
            position = transform.position;
            forward = transform.forward;
            if (definition == null || definition.workSlots <= 0)
            {
                return false;
            }

            var boundedSlot = Mathf.Clamp(slotIndex, 0, definition.workSlots - 1);
            var slotSpacing = Mathf.Min(
                1.25f,
                Mathf.Max(0.55f, definition.footprint.x / definition.workSlots));
            var centeredSlot =
                boundedSlot - (definition.workSlots - 1) * 0.5f;
            var localPosition = new Vector3(
                centeredSlot * slotSpacing,
                0.05f,
                -definition.footprint.z * 0.5f - 0.72f);
            position = transform.TransformPoint(localPosition);
            forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            return true;
        }

        private void BuildRuntime()
        {
            BuildGameplayColliders();

            var visuals = new GameObject("Collider-free art");
            visuals.transform.SetParent(transform, false);
            visualRoot = visuals.transform;
            TetherfallSettlementArtAdapter.Build(
                visualRoot,
                definition.artId,
                saveRecord.visualSeed,
                game);

            if (definition.UsesStructuralSockets)
            {
                CreateSocket("Snap_N", new Vector3(0f, 0f, definition.footprint.z * 0.5f));
                CreateSocket("Snap_E", new Vector3(definition.footprint.x * 0.5f, 0f, 0f));
                CreateSocket("Snap_S", new Vector3(0f, 0f, -definition.footprint.z * 0.5f));
                CreateSocket("Snap_W", new Vector3(-definition.footprint.x * 0.5f, 0f, 0f));
                CreateSocket("Snap_Up", new Vector3(0f, definition.footprint.y, 0f));
                CreateSocket("Snap_Down", Vector3.zero);
            }

            simulationBand = SettlementSimulationBand.Near;
            game.RegisterInteractable(this);
        }

        private void BuildGameplayColliders()
        {
            var size = new Vector3(
                Mathf.Max(0.2f, definition.footprint.x),
                Mathf.Max(0.12f, definition.footprint.y),
                Mathf.Max(0.2f, definition.footprint.z));
            if (definition.role == SettlementPieceRole.Doorway ||
                definition.role == SettlementPieceRole.Gate)
            {
                var openingWidth =
                    definition.role == SettlementPieceRole.Gate ? 2.4f : 1.4f;
                var clearHeight = Mathf.Min(2.3f, size.y - 0.2f);
                var sideWidth = Mathf.Max(0.2f, (size.x - openingWidth) * 0.5f);
                var sideOffset = openingWidth * 0.5f + sideWidth * 0.5f;
                AddSimpleBox(
                    gameObject,
                    new Vector3(-sideOffset, size.y * 0.5f, 0f),
                    new Vector3(sideWidth, size.y, size.z));
                AddSimpleBox(
                    gameObject,
                    new Vector3(sideOffset, size.y * 0.5f, 0f),
                    new Vector3(sideWidth, size.y, size.z));
                AddSimpleBox(
                    gameObject,
                    new Vector3(0f, clearHeight + (size.y - clearHeight) * 0.5f, 0f),
                    new Vector3(openingWidth, size.y - clearHeight, size.z));
                return;
            }

            if (definition.role == SettlementPieceRole.Stair ||
                definition.role == SettlementPieceRole.Ramp)
            {
                var colliderRoot = new GameObject("Gameplay Collider · Traversable slope");
                colliderRoot.transform.SetParent(transform, false);
                colliderRoot.transform.localPosition =
                    new Vector3(0f, size.y * 0.42f, 0f);
                colliderRoot.transform.localRotation = Quaternion.Euler(
                    definition.role == SettlementPieceRole.Stair ? -24f : -18f,
                    0f,
                    0f);
                AddSimpleBox(
                    colliderRoot,
                    Vector3.zero,
                    new Vector3(size.x, 0.34f, size.z * 1.02f));
                return;
            }

            if (definition.role == SettlementPieceRole.PitchedRoof)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    var colliderRoot = new GameObject(
                        side < 0
                            ? "Gameplay Collider · Left roof"
                            : "Gameplay Collider · Right roof");
                    colliderRoot.transform.SetParent(transform, false);
                    colliderRoot.transform.localPosition =
                        new Vector3(side * size.x * 0.23f, size.y * 0.46f, 0f);
                    colliderRoot.transform.localRotation =
                        Quaternion.Euler(0f, 0f, side * -30f);
                    AddSimpleBox(
                        colliderRoot,
                        Vector3.zero,
                        new Vector3(size.x * 0.6f, 0.24f, size.z));
                }

                return;
            }

            AddSimpleBox(gameObject, definition.colliderCenter, size);
        }

        private void AddSimpleBox(
            GameObject host,
            Vector3 localCenter,
            Vector3 localSize)
        {
            var collider = host.AddComponent<BoxCollider>();
            collider.center = localCenter;
            collider.size = localSize;
            collider.isTrigger = false;
            gameplayColliders.Add(collider);
        }

        private void CreateSocket(string socketName, Vector3 localPosition)
        {
            var socket = new GameObject(socketName).transform;
            socket.SetParent(transform, false);
            socket.localPosition = localPosition;
            snapSockets.Add(socket);
        }
    }

    public static class TetherfallSettlementArtAdapter
    {
        private static readonly Dictionary<string, Material> Materials =
            new Dictionary<string, Material>(StringComparer.Ordinal);

        // This is the sole gameplay-to-art seam. The confirmed concurrent art API
        // accepts its stable string ID before the parent and is deterministic itself.
        public static Transform Build(
            Transform parent,
            string artId,
            int seed,
            TetherfallGame game)
        {
            if (parent == null || game == null)
            {
                return parent;
            }

            if (TetherfallSettlementArtCatalog.TryGet(artId, out _))
            {
                TetherfallSettlementArt.Build(artId, parent);
                return parent;
            }

            BuildFallback(parent, artId ?? string.Empty, seed, game);
            return parent;
        }

        private static void BuildFallback(
            Transform parent,
            string artId,
            int seed,
            TetherfallGame game)
        {
            var timber = GetMaterial(
                game,
                "timber",
                new Color(0.38f, 0.22f, 0.12f),
                0.02f,
                0.22f);
            var lightTimber = GetMaterial(
                game,
                "light-timber",
                new Color(0.58f, 0.36f, 0.18f),
                0.02f,
                0.24f);
            var stone = GetMaterial(
                game,
                "stone",
                new Color(0.29f, 0.33f, 0.34f),
                0.02f,
                0.18f);
            var copper = GetMaterial(
                game,
                "copper",
                new Color(0.67f, 0.36f, 0.16f),
                0.55f,
                0.4f);
            var canvas = GetMaterial(
                game,
                "canvas",
                new Color(0.57f, 0.49f, 0.34f),
                0f,
                0.12f);
            var soil = GetMaterial(
                game,
                "soil",
                new Color(0.18f, 0.12f, 0.07f),
                0f,
                0.08f);
            var teal = GetMaterial(
                game,
                "threadglass",
                new Color(0.25f, 0.85f, 0.76f),
                0.18f,
                0.68f);

            if (artId.Contains("/core/"))
            {
                CreatePart(parent, "Warm stone foot", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(4.8f, 0.9f, 4.8f), Quaternion.identity, stone);
                CreatePart(parent, "Threadheart timber", PrimitiveType.Cylinder, new Vector3(0f, 1.75f, 0f), new Vector3(3.2f, 2.8f, 3.2f), Quaternion.identity, timber);
                CreatePart(parent, "Copper crown", PrimitiveType.Cylinder, new Vector3(0f, 3.25f, 0f), new Vector3(2.4f, 0.3f, 2.4f), Quaternion.identity, copper);
                CreatePart(parent, "Threadglass seam", PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(3.28f, 0.12f, 3.28f), Quaternion.identity, teal);
                return;
            }

            if (artId.Contains("foundation"))
            {
                CreatePart(parent, "Foundation feet", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(4f, 0.32f, 4f), Quaternion.identity, artId.Contains("stone") ? stone : timber);
                CreatePart(parent, "Foundation frame", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(3.75f, 0.18f, 3.75f), Quaternion.identity, lightTimber);
                return;
            }

            if (artId.Contains("wall_doorway") || artId.Contains("/gate"))
            {
                CreatePart(parent, "Left jamb", PrimitiveType.Cube, new Vector3(-1.45f, 1.5f, 0f), new Vector3(1.1f, 3f, 0.35f), Quaternion.identity, timber);
                CreatePart(parent, "Right jamb", PrimitiveType.Cube, new Vector3(1.45f, 1.5f, 0f), new Vector3(1.1f, 3f, 0.35f), Quaternion.identity, timber);
                CreatePart(parent, "Lintel", PrimitiveType.Cube, new Vector3(0f, 2.72f, 0f), new Vector3(1.8f, 0.55f, 0.4f), Quaternion.identity, copper);
                return;
            }

            if (artId.Contains("wall_window"))
            {
                CreatePart(parent, "Wall lower", PrimitiveType.Cube, new Vector3(0f, 0.65f, 0f), new Vector3(4f, 1.3f, 0.32f), Quaternion.identity, timber);
                CreatePart(parent, "Wall upper", PrimitiveType.Cube, new Vector3(0f, 2.55f, 0f), new Vector3(4f, 0.9f, 0.32f), Quaternion.identity, timber);
                CreatePart(parent, "Window frame", PrimitiveType.Cube, new Vector3(0f, 1.68f, 0f), new Vector3(2f, 0.12f, 0.4f), Quaternion.identity, copper);
                return;
            }

            if (artId.Contains("wall_straight") || artId.Contains("/fence"))
            {
                var height = artId.Contains("/fence") ? 1.25f : 3f;
                CreatePart(parent, "Wall infill", PrimitiveType.Cube, new Vector3(0f, height * 0.5f, 0f), new Vector3(3.55f, height, 0.28f), Quaternion.identity, canvas);
                CreatePart(parent, "Left frame", PrimitiveType.Cube, new Vector3(-1.88f, height * 0.5f, 0f), new Vector3(0.24f, height, 0.38f), Quaternion.identity, timber);
                CreatePart(parent, "Right frame", PrimitiveType.Cube, new Vector3(1.88f, height * 0.5f, 0f), new Vector3(0.24f, height, 0.38f), Quaternion.identity, timber);
                return;
            }

            if (artId.Contains("/floor"))
            {
                CreatePart(parent, "Floor boards", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(4f, 0.24f, 4f), Quaternion.identity, lightTimber);
                CreatePart(parent, "Floor seam", PrimitiveType.Cube, new Vector3(0f, 0.255f, 0f), new Vector3(0.08f, 0.03f, 3.8f), Quaternion.identity, teal);
                return;
            }

            if (artId.Contains("/stair") || artId.Contains("/ramp"))
            {
                for (var index = 0; index < 3; index++)
                {
                    CreatePart(
                        parent,
                        $"Rise {index + 1}",
                        PrimitiveType.Cube,
                        new Vector3(0f, 0.4f + index * 0.55f, -1.25f + index * 1.2f),
                        new Vector3(3.6f, 0.8f + index * 0.2f, 1.2f),
                        Quaternion.identity,
                        lightTimber);
                }

                return;
            }

            if (artId.Contains("/roof"))
            {
                CreatePart(parent, "Left roof", PrimitiveType.Cube, new Vector3(-0.92f, 0.7f, 0f), new Vector3(2.35f, 0.18f, 4f), Quaternion.Euler(0f, 0f, 30f), canvas);
                CreatePart(parent, "Right roof", PrimitiveType.Cube, new Vector3(0.92f, 0.7f, 0f), new Vector3(2.35f, 0.18f, 4f), Quaternion.Euler(0f, 0f, -30f), canvas);
                CreatePart(parent, "Roof seam", PrimitiveType.Cube, new Vector3(0f, 1.35f, 0f), new Vector3(0.12f, 0.12f, 4f), Quaternion.identity, copper);
                return;
            }

            if (artId.Contains("/bed_"))
            {
                CreatePart(parent, "Bed frame", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(1.8f, 0.35f, 2.6f), Quaternion.identity, timber);
                CreatePart(parent, "Bed roll", PrimitiveType.Cube, new Vector3(0f, 0.48f, 0.05f), new Vector3(1.62f, 0.24f, 2.35f), Quaternion.identity, artId.Contains("pact") ? canvas : teal);
                CreatePart(parent, "Pillow", PrimitiveType.Cube, new Vector3(0f, 0.68f, 0.82f), new Vector3(1.2f, 0.2f, 0.5f), Quaternion.identity, canvas);
                return;
            }

            if (artId.Contains("/storage/"))
            {
                CreatePart(parent, "Storage body", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), artId.Contains("pallet") ? new Vector3(2.2f, 0.9f, 2.2f) : new Vector3(1.5f, 0.95f, 1f), Quaternion.identity, timber);
                CreatePart(parent, "Copper straps", PrimitiveType.Cube, new Vector3(0f, 0.83f, 0f), artId.Contains("pallet") ? new Vector3(2.28f, 0.12f, 0.3f) : new Vector3(1.58f, 0.12f, 0.25f), Quaternion.identity, copper);
                return;
            }

            if (artId.Contains("/station/"))
            {
                CreatePart(parent, "Station base", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(2.7f, 1.05f, 1.55f), Quaternion.identity, artId.Contains("forge") ? stone : timber);
                CreatePart(parent, "Work top", PrimitiveType.Cube, new Vector3(0f, 1.16f, 0f), new Vector3(2.9f, 0.18f, 1.72f), Quaternion.identity, copper);
                CreatePart(parent, "Tool silhouette", PrimitiveType.Cylinder, new Vector3(0.65f, 1.45f, 0f), new Vector3(0.22f, 0.65f, 0.22f), Quaternion.Euler(0f, 0f, 70f), teal);
                CreatePart(parent, "Canvas shelf", PrimitiveType.Cube, new Vector3(-0.7f, 1.48f, 0f), new Vector3(0.85f, 0.42f, 0.7f), Quaternion.identity, canvas);
                return;
            }

            if (artId.Contains("farm_plot") || artId.Contains("/planter"))
            {
                var wide = artId.Contains("farm_plot");
                CreatePart(parent, "Timber bed", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), wide ? new Vector3(4f, 0.3f, 4f) : new Vector3(2f, 0.5f, 2f), Quaternion.identity, timber);
                CreatePart(parent, "Rich soil", PrimitiveType.Cube, new Vector3(0f, wide ? 0.34f : 0.46f, 0f), wide ? new Vector3(3.65f, 0.12f, 3.65f) : new Vector3(1.7f, 0.12f, 1.7f), Quaternion.identity, soil);
                return;
            }

            if (artId.Contains("water_collector"))
            {
                CreatePart(parent, "Collector stand", PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(1.8f, 1.5f, 1.8f), Quaternion.identity, timber);
                CreatePart(parent, "Canvas funnel", PrimitiveType.Cylinder, new Vector3(0f, 1.75f, 0f), new Vector3(2.5f, 0.5f, 2.5f), Quaternion.identity, canvas);
                CreatePart(parent, "Water seam", PrimitiveType.Cylinder, new Vector3(0f, 1.52f, 0f), new Vector3(1.75f, 0.08f, 1.75f), Quaternion.identity, teal);
                return;
            }

            if (artId.Contains("feeding_trough"))
            {
                CreatePart(parent, "Trough", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(2.8f, 0.7f, 1f), Quaternion.identity, timber);
                CreatePart(parent, "Trough lip", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f), new Vector3(2.95f, 0.12f, 1.15f), Quaternion.identity, copper);
                return;
            }

            if (artId.Contains("watch_post"))
            {
                CreatePart(parent, "Watch tower", PrimitiveType.Cube, new Vector3(0f, 1.8f, 0f), new Vector3(2.4f, 3.6f, 2.4f), Quaternion.identity, timber);
                CreatePart(parent, "Watch deck", PrimitiveType.Cube, new Vector3(0f, 3.55f, 0f), new Vector3(3.4f, 0.24f, 3.4f), Quaternion.identity, lightTimber);
                CreatePart(parent, "Watch seam", PrimitiveType.Cube, new Vector3(0f, 3.72f, 0f), new Vector3(2.6f, 0.08f, 0.14f), Quaternion.identity, teal);
                return;
            }

            if (artId.Contains("lantern_post"))
            {
                CreatePart(parent, "Lantern post", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f), new Vector3(0.24f, 3f, 0.24f), Quaternion.identity, timber);
                CreatePart(parent, "Threadglass lantern", PrimitiveType.Cube, new Vector3(0f, 2.85f, 0f), new Vector3(0.55f, 0.7f, 0.55f), Quaternion.identity, teal);
                return;
            }

            if (artId.Contains("/banner"))
            {
                CreatePart(parent, "Banner pole", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f), new Vector3(0.16f, 3f, 0.16f), Quaternion.identity, timber);
                CreatePart(parent, "Banner cloth", PrimitiveType.Cube, new Vector3(0.45f, 2.15f, 0f), new Vector3(0.9f, 1.1f, 0.08f), Quaternion.identity, canvas);
                CreatePart(parent, "Accord mark", PrimitiveType.Cube, new Vector3(0.45f, 2.15f, -0.05f), new Vector3(0.12f, 0.72f, 0.04f), Quaternion.identity, teal);
                return;
            }

            var seedOffset = (Mathf.Abs(seed) % 7) * 0.01f;
            CreatePart(parent, "Settlement fallback", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(1.5f + seedOffset, 1f, 1.5f), Quaternion.identity, timber);
            CreatePart(parent, "Threadglass function seam", PrimitiveType.Cube, new Vector3(0f, 1.02f, 0f), new Vector3(1.2f, 0.08f, 0.12f), Quaternion.identity, teal);
        }

        private static Transform CreatePart(
            Transform parent,
            string partName,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part.transform;
        }

        private static Material GetMaterial(
            TetherfallGame game,
            string key,
            Color color,
            float metallic,
            float smoothness)
        {
            if (!Materials.TryGetValue(key, out var material) || material == null)
            {
                material = game.CreateMaterial(color, metallic, smoothness);
                Materials[key] = material;
            }

            return material;
        }
    }

    internal static class TetherfallSettlementLegacyPlacementInput
    {
        public static Vector2 PointerPosition => Input.mousePosition;
        public static bool CommitPressed => Input.GetMouseButtonDown(0);
        public static bool CancelPressed => Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);

        public static int RotationSteps
        {
            get
            {
                var steps = 0;
                var scroll = Input.mouseScrollDelta.y;
                if (scroll > 0.01f || Input.GetKeyDown(KeyCode.R))
                {
                    steps++;
                }

                if (scroll < -0.01f)
                {
                    steps--;
                }

                return steps;
            }
        }
    }

    public sealed class TetherfallSettlementPlacementController
    {
        private const float MaximumCameraRayDistance = 220f;
        private const float GroundProbeHeight = 6f;
        private const float GroundProbeDistance = 16f;
        private const float MaximumGroundHeightSpread = 0.42f;

        private readonly TetherfallGame game;
        private readonly Transform root;
        private readonly RaycastHit[] cameraHits = new RaycastHit[64];
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private readonly Collider[] overlapHits = new Collider[96];
        private SettlementPieceDefinition definition;
        private GameObject ghost;
        private Renderer[] ghostRenderers = Array.Empty<Renderer>();
        private Material validMaterial;
        private Material invalidMaterial;
        private Camera cachedCamera;
        private float rotationY;
        private Vector3 candidatePosition;
        private bool candidateValid;
        private string failureText = string.Empty;
        private Collider supportCollider;
        private TetherfallSettlementPiece snapSource;
        private bool socketSupported;

        public TetherfallSettlementPlacementController(TetherfallGame game, Transform root)
        {
            this.game = game;
            this.root = root;
        }

        public bool IsActive => definition != null && ghost != null;
        public string FailureText => failureText;
        public Vector3 CandidatePosition => candidatePosition;
        public float CandidateRotationY => rotationY;
        public SettlementPieceDefinition ActiveDefinition => definition;
        public bool CandidateValid => candidateValid;

        public bool Begin(SettlementPieceDefinition selected, out string failure)
        {
            failure = string.Empty;
            Cancel(false);
            if (selected == null)
            {
                failure = "That settlement catalog entry is missing.";
                return false;
            }

            definition = selected;
            rotationY = 0f;
            validMaterial = game.CreateMaterial(
                new Color(0.22f, 0.9f, 0.72f, 0.48f),
                0.05f,
                0.4f);
            invalidMaterial = game.CreateMaterial(
                new Color(0.95f, 0.24f, 0.16f, 0.52f),
                0.02f,
                0.3f);
            SetTransparent(validMaterial);
            SetTransparent(invalidMaterial);

            ghost = new GameObject($"Settlement Ghost · {selected.displayName}");
            if (root != null)
            {
                ghost.transform.SetParent(root, false);
            }

            TetherfallSettlementArtAdapter.Build(
                ghost.transform,
                selected.artId,
                0,
                game);
            SetLayerRecursively(ghost.transform, 2);
            ghostRenderers = ghost.GetComponentsInChildren<Renderer>(true);
            ApplyGhostMaterial(false);
            failureText = "Aim at supported ground inside the Threadheart claim.";
            return true;
        }

        public bool Tick()
        {
            if (!IsActive)
            {
                return false;
            }

            if (TetherfallSettlementLegacyPlacementInput.CancelPressed)
            {
                Cancel(true);
                return true;
            }

            var rotationSteps = TetherfallSettlementLegacyPlacementInput.RotationSteps;
            if (rotationSteps != 0)
            {
                rotationY = TetherfallSettlementRules.NormalizeRotation(
                    rotationY + rotationSteps * definition.RotationStepDegrees,
                    definition.rotationPolicy);
            }

            EvaluateCandidate();
            ghost.transform.position = candidatePosition;
            ghost.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            ApplyGhostMaterial(candidateValid);

            if (TetherfallSettlementLegacyPlacementInput.CommitPressed)
            {
                if (!candidateValid)
                {
                    game.Notify(failureText);
                    return true;
                }

                if (game.TryCommitPlayerSettlementPlacement(
                        definition,
                        candidatePosition,
                        rotationY,
                        out var failure))
                {
                    var placedName = definition.displayName;
                    Cancel(false);
                    game.Notify($"{placedName} placed.");
                }
                else
                {
                    failureText = failure;
                    candidateValid = false;
                    ApplyGhostMaterial(false);
                    game.Notify(failure);
                }
            }

            return true;
        }

        public void Cancel(bool notify)
        {
            if (ghost != null)
            {
                UnityEngine.Object.Destroy(ghost);
            }

            if (validMaterial != null)
            {
                UnityEngine.Object.Destroy(validMaterial);
            }

            if (invalidMaterial != null)
            {
                UnityEngine.Object.Destroy(invalidMaterial);
            }

            definition = null;
            ghost = null;
            ghostRenderers = Array.Empty<Renderer>();
            candidateValid = false;
            supportCollider = null;
            snapSource = null;
            socketSupported = false;
            failureText = string.Empty;
            if (notify)
            {
                game.Notify("Settlement placement cancelled.");
            }
        }

        public bool Revalidate(out string failure)
        {
            EvaluateCandidate();
            failure = failureText;
            return candidateValid;
        }

        private void EvaluateCandidate()
        {
            candidateValid = false;
            supportCollider = null;
            snapSource = null;
            socketSupported = false;
            if (!TryGetCameraTarget(out var targetPoint))
            {
                failureText = "No build surface is under the camera ray.";
                return;
            }

            var corePosition = game.GetPlayerSettlementCorePosition();
            var gridOrigin = game.HasClaimedPlayerSettlement
                ? corePosition
                : Vector3.zero;
            candidatePosition = TetherfallSettlementRules.SnapToGrid(targetPoint, gridOrigin);
            if (definition.UsesStructuralSockets &&
                game.TryFindPlayerSettlementSnap(
                    definition,
                    targetPoint,
                    rotationY,
                    out var snap))
            {
                candidatePosition = snap.Position;
                snapSource = snap.Source;
                socketSupported = snap.SupportsWithoutGround;
            }

            if (!socketSupported &&
                !TryResolveGround(ref candidatePosition, out supportCollider, out var groundFailure))
            {
                failureText = groundFailure;
                return;
            }

            if (!game.ValidatePlayerSettlementPlacement(
                    definition,
                    candidatePosition,
                    out failureText))
            {
                return;
            }

            if (!ValidateOverlap(out failureText))
            {
                return;
            }

            candidateValid = true;
            failureText = definition.UsesStructuralSockets
                ? $"Ready: {definition.displayName} · {definition.RotationStepDegrees:0}° rotation · 2 m grid/sockets."
                : $"Ready: {definition.displayName} · {definition.RotationStepDegrees:0}° rotation.";
        }

        private bool TryGetCameraTarget(out Vector3 point)
        {
            point = Vector3.zero;
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
            {
                cachedCamera = Camera.main;
            }

            if (cachedCamera == null)
            {
                failureText = "The placement camera is unavailable.";
                return false;
            }

            var ray = cachedCamera.ScreenPointToRay(
                TetherfallSettlementLegacyPlacementInput.PointerPosition);
            var hitCount = Physics.RaycastNonAlloc(
                ray,
                cameraHits,
                MaximumCameraRayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (hitCount >= cameraHits.Length)
            {
                failureText =
                    "Camera-ray buffer is saturated; aim at a less crowded surface.";
                return false;
            }

            var nearestDistance = float.PositiveInfinity;
            var found = false;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = cameraHits[index];
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(ghost.transform) ||
                    hit.collider.GetComponentInParent<PlayerAvatar>() != null ||
                    hit.collider.GetComponentInParent<PactCreature>() != null ||
                    hit.collider.GetComponentInParent<HumanCompanion>() != null)
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    point = hit.point;
                    found = true;
                }
            }

            return found;
        }

        private bool TryResolveGround(
            ref Vector3 position,
            out Collider centerSupport,
            out string failure)
        {
            centerSupport = null;
            failure = string.Empty;
            var rotation = Quaternion.Euler(0f, rotationY, 0f);
            var halfX = Mathf.Max(0.1f, definition.footprint.x * 0.42f);
            var halfZ = Mathf.Max(0.1f, definition.footprint.z * 0.42f);
            var offsets = new[]
            {
                Vector3.zero,
                rotation * new Vector3(halfX, 0f, halfZ),
                rotation * new Vector3(-halfX, 0f, halfZ),
                rotation * new Vector3(halfX, 0f, -halfZ),
                rotation * new Vector3(-halfX, 0f, -halfZ)
            };
            var minimumHeight = float.PositiveInfinity;
            var maximumHeight = float.NegativeInfinity;
            var centerHeight = position.y;
            for (var sampleIndex = 0; sampleIndex < offsets.Length; sampleIndex++)
            {
                var origin = position + offsets[sampleIndex] + Vector3.up * GroundProbeHeight;
                var hitCount = Physics.RaycastNonAlloc(
                    origin,
                    Vector3.down,
                    groundHits,
                    GroundProbeDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
                if (hitCount >= groundHits.Length)
                {
                    failure =
                        $"Ground probe {sampleIndex + 1}/5 is saturated; choose a clearer site.";
                    return false;
                }

                var nearestDistance = float.PositiveInfinity;
                RaycastHit nearest = default;
                var found = false;
                for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    var hit = groundHits[hitIndex];
                    if (hit.collider == null ||
                        hit.collider.transform.IsChildOf(ghost.transform) ||
                        hit.collider.GetComponentInParent<PlayerAvatar>() != null ||
                        hit.collider.GetComponentInParent<PactCreature>() != null ||
                        hit.collider.GetComponentInParent<HumanCompanion>() != null ||
                        hit.collider.GetComponentInParent<Enemy>() != null ||
                        hit.collider.GetComponentInParent<TetherfallSettlementPiece>() != null)
                    {
                        continue;
                    }

                    if (hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        nearest = hit;
                        found = true;
                    }
                }

                if (!found)
                {
                    failure = $"Ground support is missing below footprint sample {sampleIndex + 1}/5.";
                    return false;
                }

                var slope = Vector3.Angle(nearest.normal, Vector3.up);
                if (slope > definition.maximumSlopeDegrees)
                {
                    failure =
                        $"Slope {slope:0.#}° exceeds {definition.maximumSlopeDegrees:0.#}° for {definition.displayName}.";
                    return false;
                }

                minimumHeight = Mathf.Min(minimumHeight, nearest.point.y);
                maximumHeight = Mathf.Max(maximumHeight, nearest.point.y);
                if (sampleIndex == 0)
                {
                    centerHeight = nearest.point.y;
                    centerSupport = nearest.collider;
                }
            }

            if (maximumHeight - minimumHeight > MaximumGroundHeightSpread)
            {
                failure =
                    $"Ground varies by {maximumHeight - minimumHeight:0.##} m; maximum is {MaximumGroundHeightSpread:0.##} m.";
                return false;
            }

            position.y = centerHeight;
            return true;
        }

        private bool ValidateOverlap(out string failure)
        {
            failure = string.Empty;
            var rotation = Quaternion.Euler(0f, rotationY, 0f);
            var center = candidatePosition + rotation * definition.colliderCenter;
            var halfExtents = new Vector3(
                Mathf.Max(0.05f, definition.footprint.x * 0.5f - 0.06f),
                Mathf.Max(0.05f, definition.footprint.y * 0.5f - 0.04f),
                Mathf.Max(0.05f, definition.footprint.z * 0.5f - 0.06f));
            var hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                overlapHits,
                rotation,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (hitCount >= overlapHits.Length)
            {
                failure = "Placement overlap buffer is saturated; choose a clearer site.";
                return false;
            }

            for (var index = 0; index < hitCount; index++)
            {
                var collider = overlapHits[index];
                if (collider == null ||
                    collider == supportCollider ||
                    collider.transform.IsChildOf(ghost.transform))
                {
                    continue;
                }

                var existingPiece = collider.GetComponentInParent<TetherfallSettlementPiece>();
                if (existingPiece != null &&
                    existingPiece == snapSource &&
                    !collider.bounds.Intersects(new Bounds(center, halfExtents * 1.8f)))
                {
                    continue;
                }

                var blocker = existingPiece != null
                    ? existingPiece.Definition?.displayName
                    : collider.GetComponentInParent<PlayerAvatar>() != null
                        ? "the Wayfinder"
                        : collider.GetComponentInParent<PactCreature>() != null
                            ? "a Pact creature"
                            : collider.GetComponentInParent<HumanCompanion>() != null
                                ? "a companion"
                                : collider.name;
                failure = $"Placement overlaps {blocker ?? "another collider"}.";
                return false;
            }

            return true;
        }

        private void ApplyGhostMaterial(bool valid)
        {
            var material = valid ? validMaterial : invalidMaterial;
            foreach (var renderer in ghostRenderers)
            {
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        private static void SetTransparent(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetOverrideTag("RenderType", "Transparent");
                if (material.HasProperty("_SrcBlend"))
                {
                    material.SetInt("_SrcBlend", 5);
                }

                if (material.HasProperty("_DstBlend"))
                {
                    material.SetInt("_DstBlend", 10);
                }

                if (material.HasProperty("_ZWrite"))
                {
                    material.SetInt("_ZWrite", 0);
                }

                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            material.renderQueue = 3000;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (var index = 0; index < root.childCount; index++)
            {
                SetLayerRecursively(root.GetChild(index), layer);
            }
        }
    }
}
