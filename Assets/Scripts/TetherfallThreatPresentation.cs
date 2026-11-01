using UnityEngine;
using UnityEngine.Rendering;

namespace Tetherfall
{
    public enum TetherfallThreatStatusKind
    {
        None,
        Ashguard,
        Staggered,
        Exposed
    }

    public struct TetherfallThreatPriorityKey
    {
        public TetherfallThreatPriorityKey(
            bool isFocus,
            bool recentlyDamaged,
            float lastDamagedAt,
            float distanceSquared,
            string stableId,
            int runtimeId)
        {
            IsFocus = isFocus;
            RecentlyDamaged = recentlyDamaged;
            LastDamagedAt = lastDamagedAt;
            DistanceSquared = distanceSquared;
            StableId = stableId ?? string.Empty;
            RuntimeId = runtimeId;
        }

        public bool IsFocus { get; }
        public bool RecentlyDamaged { get; }
        public float LastDamagedAt { get; }
        public float DistanceSquared { get; }
        public string StableId { get; }
        public int RuntimeId { get; }
    }

    public static class TetherfallThreatPresentationRules
    {
        public const int MaximumThreatPlates = 4;
        public const int MaximumEdgePips = 3;
        public const int MaximumOutlineHullRenderers = 4;
        public const int MaximumSharedMaterials = 3;
        public const int ActualSharedMaterialCount = 1;
        public const float RecentDamageSeconds = 4.5f;
        public const float NearbyThreatRange = 18f;
        public const float BossStripRange = 60f;
        public const float MinimumLayoutWidth = 1280f;
        public const float MinimumLayoutHeight = 720f;
        public const float LargeLayoutWidth = 1920f;
        public const float LargeLayoutHeight = 1080f;

        private const float ViewportMargin = 12f;
        private const float PlateBottomReserve = 106f;
        private const float EdgePipMargin = 24f;

        public static int ComparePriority(
            TetherfallThreatPriorityKey left,
            TetherfallThreatPriorityKey right)
        {
            var comparison = right.IsFocus.CompareTo(left.IsFocus);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = right.RecentlyDamaged.CompareTo(left.RecentlyDamaged);
            if (comparison != 0)
            {
                return comparison;
            }

            if (left.RecentlyDamaged && right.RecentlyDamaged)
            {
                comparison = right.LastDamagedAt.CompareTo(left.LastDamagedAt);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            comparison = left.DistanceSquared.CompareTo(right.DistanceSquared);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = string.CompareOrdinal(left.StableId, right.StableId);
            return comparison != 0
                ? comparison
                : left.RuntimeId.CompareTo(right.RuntimeId);
        }

        public static TetherfallThreatStatusKind ResolveEnemyStatus(
            float ashguardRemaining,
            float staggerRemaining,
            float exposeRemaining)
        {
            if (ashguardRemaining > 0f)
            {
                return TetherfallThreatStatusKind.Ashguard;
            }

            if (staggerRemaining > 0f)
            {
                return TetherfallThreatStatusKind.Staggered;
            }

            return exposeRemaining > 0f
                ? TetherfallThreatStatusKind.Exposed
                : TetherfallThreatStatusKind.None;
        }

        public static TetherfallThreatStatusKind ResolveBossStatus(
            float staggerRemaining,
            float exposeRemaining)
        {
            return ResolveEnemyStatus(0f, staggerRemaining, exposeRemaining);
        }

        public static float GetOutlineWidth(bool focused, bool reinforce)
        {
            var width = focused ? 0.082f : 0.032f;
            return reinforce ? width * 1.42f : width;
        }

        public static Vector2 GetPlateSize(float logicalWidth)
        {
            var safeWidth = IsFinite(logicalWidth)
                ? Mathf.Max(1f, logicalWidth)
                : MinimumLayoutWidth;
            return new Vector2(
                Mathf.Clamp(safeWidth * 0.175f, 214f, 252f),
                86f);
        }

        public static Rect GetBossStripRect(
            float logicalWidth,
            float logicalHeight)
        {
            var safeWidth = IsFinite(logicalWidth)
                ? Mathf.Max(1f, logicalWidth)
                : MinimumLayoutWidth;
            var safeHeight = IsFinite(logicalHeight)
                ? Mathf.Max(1f, logicalHeight)
                : MinimumLayoutHeight;
            var width = Mathf.Min(
                Mathf.Clamp(safeWidth * 0.36f, 380f, 560f),
                Mathf.Max(1f, safeWidth - ViewportMargin * 2f));
            var height = Mathf.Min(92f, Mathf.Max(1f, safeHeight - ViewportMargin * 2f));
            return new Rect(
                Mathf.Max(ViewportMargin, safeWidth * 0.5f - width * 0.5f),
                ViewportMargin,
                width,
                height);
        }

        public static Rect GetPlateRect(
            Vector2 anchor,
            float logicalWidth,
            float logicalHeight,
            float topBoundary)
        {
            var size = GetPlateSize(logicalWidth);
            var desired = new Rect(
                anchor.x - size.x * 0.5f,
                anchor.y - size.y - 18f,
                size.x,
                size.y);
            return ClampPlateRect(
                desired,
                logicalWidth,
                logicalHeight,
                topBoundary);
        }

        public static Rect ClampPlateRect(
            Rect desired,
            float logicalWidth,
            float logicalHeight,
            float topBoundary)
        {
            var safeWidth = IsFinite(logicalWidth)
                ? Mathf.Max(1f, logicalWidth)
                : MinimumLayoutWidth;
            var safeHeight = IsFinite(logicalHeight)
                ? Mathf.Max(1f, logicalHeight)
                : MinimumLayoutHeight;
            var minimumX = ViewportMargin;
            var maximumX = Mathf.Max(
                minimumX,
                safeWidth - ViewportMargin - desired.width);
            var minimumY = Mathf.Clamp(
                topBoundary,
                ViewportMargin,
                Mathf.Max(ViewportMargin, safeHeight - desired.height));
            var maximumY = Mathf.Max(
                minimumY,
                safeHeight - PlateBottomReserve - desired.height);
            desired.x = Mathf.Clamp(desired.x, minimumX, maximumX);
            desired.y = Mathf.Clamp(desired.y, minimumY, maximumY);
            return desired;
        }

        public static Vector2 ClampEdgePip(
            Vector2 screenPoint,
            float logicalWidth,
            float logicalHeight)
        {
            var safeWidth = IsFinite(logicalWidth)
                ? Mathf.Max(1f, logicalWidth)
                : MinimumLayoutWidth;
            var safeHeight = IsFinite(logicalHeight)
                ? Mathf.Max(1f, logicalHeight)
                : MinimumLayoutHeight;
            return new Vector2(
                Mathf.Clamp(
                    screenPoint.x,
                    EdgePipMargin,
                    Mathf.Max(EdgePipMargin, safeWidth - EdgePipMargin)),
                Mathf.Clamp(
                    screenPoint.y,
                    EdgePipMargin,
                    Mathf.Max(
                        EdgePipMargin,
                        safeHeight - PlateBottomReserve)));
        }

        public static bool IsInsideViewport(
            Rect rect,
            float logicalWidth,
            float logicalHeight)
        {
            return rect.xMin >= 0f &&
                   rect.yMin >= 0f &&
                   rect.xMax <= logicalWidth &&
                   rect.yMax <= logicalHeight;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    [DefaultExecutionOrder(900)]
    public sealed class TetherfallThreatPresentation : MonoBehaviour
    {
        private static readonly int OutlineColorId =
            Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId =
            Shader.PropertyToID("_OutlineWidth");

        private static readonly Color DeepSlate =
            new Color(13f / 255f, 28f / 255f, 36f / 255f, 0.93f);
        private static readonly Color Ink =
            new Color(3f / 255f, 8f / 255f, 10f / 255f, 0.9f);
        private static readonly Color RouteworkCopper =
            new Color(196f / 255f, 120f / 255f, 56f / 255f, 0.96f);
        private static readonly Color ThreadglassTeal =
            new Color(74f / 255f, 201f / 255f, 189f / 255f, 0.98f);
        private static readonly Color WarmParchment =
            new Color(240f / 255f, 224f / 255f, 186f / 255f, 1f);
        private static readonly Color ClearMist =
            new Color(214f / 255f, 232f / 255f, 230f / 255f, 1f);
        private static readonly Color MutedReed =
            new Color(150f / 255f, 181f / 255f, 184f / 255f, 1f);
        private static readonly Color DangerCinder =
            new Color(199f / 255f, 69f / 255f, 56f / 255f, 0.98f);

        private readonly Enemy[] enemyScanBuffer =
            new Enemy[Enemy.MaximumActiveEnemyCopyCount];
        private readonly ThreatCandidate[] candidateBuffer =
            new ThreatCandidate[Enemy.MaximumActiveEnemyCopyCount];
        private readonly Enemy[] selectedEnemies =
            new Enemy[TetherfallThreatPresentationRules.MaximumThreatPlates];
        private readonly Rect[] drawnPlateRects =
            new Rect[TetherfallThreatPresentationRules.MaximumThreatPlates];
        private readonly OutlineSlot[] outlineSlots =
            new OutlineSlot[TetherfallThreatPresentationRules.MaximumOutlineHullRenderers];

        private static TetherfallThreatPresentation instance;

        private TetherfallGame game;
        private Camera worldCamera;
        private DungeonBoss visibleBoss;
        private Transform cachedFocus;
        private Material outlineMaterial;
        private Mesh outlineMesh;
        private Texture2D slateTexture;
        private Texture2D inkTexture;
        private Texture2D copperTexture;
        private Texture2D focusTexture;
        private Texture2D hostileTexture;
        private Texture2D parchmentTexture;
        private GUIStyle nameStyle;
        private GUIStyle bossNameStyle;
        private GUIStyle metaStyle;
        private GUIStyle metaRightStyle;
        private GUIStyle bandStyle;
        private GUIStyle statusStyle;
        private int selectedEnemyCount;
        private int previousScanCount;
        private int nextNonFocusRefreshFrame;
        private bool stylesReady;

        public static bool IsActive =>
            instance != null && instance.isActiveAndEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null)
            {
                return;
            }

            var existing = FindAnyObjectByType<TetherfallThreatPresentation>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            new GameObject("Tetherfall Threat Presentation")
                .AddComponent<TetherfallThreatPresentation>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            CreateTextures();
            CreateOutlinePool();
            TetherfallAccessibilityRuntime.SettingsChanged +=
                OnAccessibilitySettingsChanged;
        }

        private void Update()
        {
            if (game == null)
            {
                game = TetherfallGame.Instance;
            }

            if (game == null || game.Player == null)
            {
                HideAllOutlines();
                return;
            }

            if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            {
                worldCamera = Camera.main;
            }

            var focus = game.CombatTarget;
            var boss = ResolveVisibleBoss(focus);
            if (focus != cachedFocus ||
                boss != visibleBoss ||
                Time.frameCount >= nextNonFocusRefreshFrame)
            {
                cachedFocus = focus;
                visibleBoss = boss;
                RefreshSelection(focus);
                nextNonFocusRefreshFrame = Time.frameCount + 2;
            }

            UpdateOutlineHulls(focus);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint ||
                game == null ||
                game.Player == null ||
                worldCamera == null ||
                (game.GameplayPaused && !game.CombatHitstopActive))
            {
                return;
            }

            EnsureStyles();
            var previousMatrix = GUI.matrix;
            var hudScale = Mathf.Max(
                0.01f,
                TetherfallAccessibilityRuntime.HudScale);
            GUI.matrix =
                previousMatrix *
                Matrix4x4.Scale(new Vector3(hudScale, hudScale, 1f));
            try
            {
                var logicalWidth = Screen.width / hudScale;
                var logicalHeight = Screen.height / hudScale;
                var bossVisible = IsBossAvailable(visibleBoss);
                var bossRect = bossVisible
                    ? TetherfallThreatPresentationRules.GetBossStripRect(
                        logicalWidth,
                        logicalHeight)
                    : default;
                if (bossVisible)
                {
                    DrawBossStrip(bossRect, visibleBoss, cachedFocus);
                }

                var topBoundary = bossVisible
                    ? bossRect.yMax + 8f
                    : 16f;
                var drawnPlateCount = 0;
                var edgePipCount = 0;
                for (var index = 0;
                     index < selectedEnemyCount &&
                     index < TetherfallThreatPresentationRules.MaximumThreatPlates;
                     index++)
                {
                    var enemy = selectedEnemies[index];
                    if (!IsEnemyAwake(enemy))
                    {
                        continue;
                    }

                    var anchor = enemy.transform.position +
                                 Vector3.up * GetEnemyAnchorHeight(enemy);
                    var projected = worldCamera.WorldToScreenPoint(anchor);
                    var logicalPoint = new Vector2(
                        projected.x / hudScale,
                        (Screen.height - projected.y) / hudScale);
                    var onScreen =
                        projected.z > 0f &&
                        logicalPoint.x >= 0f &&
                        logicalPoint.x <= logicalWidth &&
                        logicalPoint.y >= 0f &&
                        logicalPoint.y <= logicalHeight;
                    var focused = cachedFocus == enemy.transform;
                    if (!onScreen)
                    {
                        if (edgePipCount <
                            TetherfallThreatPresentationRules.MaximumEdgePips)
                        {
                            DrawEdgePip(
                                GetEdgePipPoint(
                                    logicalPoint,
                                    projected.z <= 0f,
                                    logicalWidth,
                                    logicalHeight),
                                enemy.AttackStyle,
                                focused);
                            edgePipCount++;
                        }

                        continue;
                    }

                    var plateRect =
                        TetherfallThreatPresentationRules.GetPlateRect(
                            logicalPoint,
                            logicalWidth,
                            logicalHeight,
                            topBoundary);
                    plateRect = ResolvePlateOverlap(
                        plateRect,
                        drawnPlateCount,
                        logicalWidth,
                        logicalHeight,
                        topBoundary);
                    drawnPlateRects[drawnPlateCount++] = plateRect;
                    DrawEnemyPlate(plateRect, enemy, focused);
                }
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        private DungeonBoss ResolveVisibleBoss(Transform focus)
        {
            var focusedBoss =
                focus != null ? focus.GetComponent<DungeonBoss>() : null;
            var boss = focusedBoss != null
                ? focusedBoss
                : game.ActiveDungeonBoss;
            if (!IsBossAvailable(boss))
            {
                return null;
            }

            if (focusedBoss == boss)
            {
                return boss;
            }

            return HorizontalDistanceSquared(
                       game.PlayerTransform.position,
                       boss.transform.position) <=
                   TetherfallThreatPresentationRules.BossStripRange *
                   TetherfallThreatPresentationRules.BossStripRange
                ? boss
                : null;
        }

        private void RefreshSelection(Transform focus)
        {
            for (var index = 0; index < selectedEnemies.Length; index++)
            {
                selectedEnemies[index] = null;
            }

            var candidateCount = 0;
            var focusedEnemy =
                focus != null ? focus.GetComponent<Enemy>() : null;
            if (IsEnemyAwake(focusedEnemy))
            {
                candidateCount = TryAddCandidate(
                    candidateCount,
                    focusedEnemy,
                    true);
            }

            for (var index = 0; index < previousScanCount; index++)
            {
                enemyScanBuffer[index] = null;
            }

            previousScanCount =
                Enemy.CopyActiveEnemiesNonAlloc(enemyScanBuffer);
            for (var index = 0; index < previousScanCount; index++)
            {
                candidateCount = TryAddCandidate(
                    candidateCount,
                    enemyScanBuffer[index],
                    enemyScanBuffer[index] == focusedEnemy);
            }

            for (var index = 1; index < candidateCount; index++)
            {
                var candidate = candidateBuffer[index];
                var insertionIndex = index - 1;
                while (insertionIndex >= 0 &&
                       TetherfallThreatPresentationRules.ComparePriority(
                           candidate.Key,
                           candidateBuffer[insertionIndex].Key) < 0)
                {
                    candidateBuffer[insertionIndex + 1] =
                        candidateBuffer[insertionIndex];
                    insertionIndex--;
                }

                candidateBuffer[insertionIndex + 1] = candidate;
            }

            var enemyLimit =
                TetherfallThreatPresentationRules.MaximumThreatPlates -
                (visibleBoss != null ? 1 : 0);
            selectedEnemyCount = Mathf.Min(candidateCount, enemyLimit);
            for (var index = 0; index < selectedEnemyCount; index++)
            {
                selectedEnemies[index] = candidateBuffer[index].Enemy;
            }

            for (var index = 0; index < candidateCount; index++)
            {
                candidateBuffer[index] = default;
            }

            BindOutlineTargets();
        }

        private int TryAddCandidate(
            int candidateCount,
            Enemy enemy,
            bool focused)
        {
            if (!IsEnemyAwake(enemy) ||
                candidateCount >= candidateBuffer.Length)
            {
                return candidateCount;
            }

            for (var index = 0; index < candidateCount; index++)
            {
                if (candidateBuffer[index].Enemy == enemy)
                {
                    return candidateCount;
                }
            }

            var distanceSquared = HorizontalDistanceSquared(
                game.PlayerTransform.position,
                enemy.transform.position);
            var lastDamagedAt = enemy.LastDamagedAt;
            var recentlyDamaged =
                !float.IsNaN(lastDamagedAt) &&
                !float.IsInfinity(lastDamagedAt) &&
                lastDamagedAt <= Time.time &&
                Time.time - lastDamagedAt <=
                TetherfallThreatPresentationRules.RecentDamageSeconds;
            if (!focused &&
                !recentlyDamaged &&
                distanceSquared >
                TetherfallThreatPresentationRules.NearbyThreatRange *
                TetherfallThreatPresentationRules.NearbyThreatRange)
            {
                return candidateCount;
            }

            candidateBuffer[candidateCount] = new ThreatCandidate
            {
                Enemy = enemy,
                Key = new TetherfallThreatPriorityKey(
                    focused,
                    recentlyDamaged,
                    lastDamagedAt,
                    distanceSquared,
                    enemy.EnemyId,
                    enemy.GetInstanceID())
            };
            return candidateCount + 1;
        }

        private void BindOutlineTargets()
        {
            var slotIndex = 0;
            if (IsBossAvailable(visibleBoss))
            {
                BindOutlineSlot(slotIndex++, visibleBoss.transform, null, visibleBoss);
            }

            for (var index = 0;
                 index < selectedEnemyCount &&
                 slotIndex < outlineSlots.Length;
                 index++)
            {
                var enemy = selectedEnemies[index];
                BindOutlineSlot(
                    slotIndex++,
                    enemy != null ? enemy.transform : null,
                    enemy,
                    null);
            }

            while (slotIndex < outlineSlots.Length)
            {
                BindOutlineSlot(slotIndex++, null, null, null);
            }
        }

        private void BindOutlineSlot(
            int index,
            Transform target,
            Enemy enemy,
            DungeonBoss boss)
        {
            var slot = outlineSlots[index];
            if (slot == null || slot.Target == target)
            {
                return;
            }

            slot.Target = target;
            slot.Enemy = enemy;
            slot.Boss = boss;
            if (target == null)
            {
                slot.Renderer.enabled = false;
                return;
            }

            if (boss != null)
            {
                var body = target.GetComponent<Renderer>();
                if (body != null)
                {
                    var bounds = body.bounds;
                    slot.VerticalOffset = bounds.min.y - target.position.y;
                    slot.BaseScale = new Vector3(
                        Mathf.Max(0.5f, bounds.size.x / 1.5f * 1.04f),
                        Mathf.Max(1f, bounds.size.y * 1.04f),
                        Mathf.Max(0.5f, bounds.size.z / 1.5f * 1.04f));
                }
                else
                {
                    slot.VerticalOffset = -2.8f;
                    slot.BaseScale = new Vector3(3.8f, 6.2f, 3.8f);
                }

                return;
            }

            var controller = target.GetComponent<CharacterController>();
            var height = controller != null
                ? controller.height * Mathf.Abs(target.lossyScale.y)
                : enemy != null && enemy.AttackStyle == EnemyAttackStyle.Heavy
                    ? 2.5f
                    : 1.9f;
            var radius = controller != null
                ? controller.radius *
                  Mathf.Max(
                      Mathf.Abs(target.lossyScale.x),
                      Mathf.Abs(target.lossyScale.z))
                : 0.48f;
            var widthFactor =
                enemy != null && enemy.AttackStyle == EnemyAttackStyle.Ranged
                    ? 1.2f
                    : enemy != null && enemy.AttackStyle == EnemyAttackStyle.Heavy
                        ? 1.14f
                        : 1f;
            if (enemy != null && enemy.IsElite)
            {
                widthFactor *= 1.08f;
                height *= 1.05f;
            }

            slot.VerticalOffset = controller != null
                ? (controller.center.y - controller.height * 0.5f) *
                  Mathf.Abs(target.lossyScale.y)
                : 0f;
            var diameter = Mathf.Max(0.65f, radius * 2f * 1.24f);
            slot.BaseScale = new Vector3(
                diameter / 1.5f * widthFactor,
                Mathf.Max(1.2f, height * 1.06f),
                diameter / 1.5f * widthFactor);
        }

        private void UpdateOutlineHulls(Transform focus)
        {
            var reinforce =
                TetherfallAccessibilityRuntime.ShouldReinforceColorCues;
            for (var index = 0; index < outlineSlots.Length; index++)
            {
                var slot = outlineSlots[index];
                if (slot == null)
                {
                    continue;
                }

                var available =
                    slot.Target != null &&
                    slot.Target.gameObject.activeInHierarchy &&
                    (slot.Enemy == null || !slot.Enemy.IsDefeated) &&
                    (slot.Boss == null || !slot.Boss.IsDefeated);
                if (!available)
                {
                    slot.Renderer.enabled = false;
                    continue;
                }

                var focused = focus == slot.Target;
                var shapeScale = focused
                    ? 1.055f
                    : reinforce
                        ? 1.025f
                        : 1f;
                slot.Transform.position =
                    slot.Target.position +
                    Vector3.up * slot.VerticalOffset;
                slot.Transform.rotation = Quaternion.Euler(
                    0f,
                    slot.Target.eulerAngles.y,
                    0f);
                slot.Transform.localScale = new Vector3(
                    slot.BaseScale.x * shapeScale,
                    slot.BaseScale.y,
                    slot.BaseScale.z * shapeScale);
                var originalColor = focused
                    ? ThreadglassTeal
                    : DangerCinder;
                var resolved =
                    TetherfallAccessibilityRuntime.ResolveColorCue(
                        originalColor,
                        TetherfallAccessibilityColorCue.Hostile);
                resolved.a = focused ? 0.94f : 0.68f;
                slot.PropertyBlock.Clear();
                slot.PropertyBlock.SetColor(OutlineColorId, resolved);
                slot.PropertyBlock.SetFloat(
                    OutlineWidthId,
                    TetherfallThreatPresentationRules.GetOutlineWidth(
                        focused,
                        reinforce));
                slot.Renderer.SetPropertyBlock(slot.PropertyBlock);
                slot.Renderer.enabled = true;
            }
        }

        private void DrawEnemyPlate(
            Rect rect,
            Enemy enemy,
            bool focused)
        {
            GUI.DrawTexture(rect, slateTexture);
            GUI.DrawTexture(
                new Rect(rect.x, rect.y, rect.width, 2f),
                copperTexture);
            if (focused)
            {
                GUI.DrawTexture(
                    new Rect(rect.x, rect.y, 1f, rect.height),
                    focusTexture);
            }

            if (TetherfallAccessibilityRuntime.ShouldReinforceColorCues)
            {
                DrawRectOutline(rect, parchmentTexture, 1f);
            }

            var signalTexture = focused ? focusTexture : hostileTexture;
            DrawArchetypeMark(
                new Rect(rect.x + 10f, rect.y + 7f, 17f, 17f),
                enemy.AttackStyle,
                signalTexture);
            var displayName = string.IsNullOrWhiteSpace(enemy.gameObject.name)
                ? enemy.EnemyName
                : enemy.gameObject.name;
            GUI.Label(
                new Rect(rect.x + 34f, rect.y + 4f, rect.width - 108f, 19f),
                displayName,
                nameStyle);
            GUI.Label(
                new Rect(rect.x + rect.width - 72f, rect.y + 4f, 62f, 19f),
                $"BAND {enemy.ThreatBand}",
                bandStyle);

            var archetype = GetArchetypeLabel(enemy.AttackStyle);
            var metadata = enemy.IsElite
                ? $"{archetype}  ELITE"
                : archetype;
            if (focused)
            {
                metadata += "  THREADGLASS";
            }

            GUI.Label(
                new Rect(rect.x + 34f, rect.y + 23f, rect.width - 44f, 15f),
                metadata,
                metaStyle);

            var healthRect =
                new Rect(rect.x + 10f, rect.y + 40f, rect.width - 20f, 8f);
            DrawBar(
                healthRect,
                enemy.Health,
                enemy.MaxHealth,
                hostileTexture,
                true);
            GUI.Label(
                new Rect(rect.x + 10f, rect.y + 49f, rect.width * 0.54f, 14f),
                $"HP {enemy.Health}/{enemy.MaxHealth}",
                metaStyle);
            GUI.Label(
                new Rect(
                    rect.x + rect.width * 0.48f,
                    rect.y + 49f,
                    rect.width * 0.48f - 10f,
                    14f),
                $"POISE {Mathf.CeilToInt(enemy.Poise)}/{Mathf.CeilToInt(enemy.PoiseThreshold)}",
                metaRightStyle);
            DrawBar(
                new Rect(rect.x + 10f, rect.y + 64f, rect.width - 20f, 4f),
                enemy.Poise,
                enemy.PoiseThreshold,
                focusTexture,
                false);

            var statusKind =
                TetherfallThreatPresentationRules.ResolveEnemyStatus(
                    enemy.AshguardRemaining,
                    enemy.StaggerRemaining,
                    enemy.ExposeRemaining);
            var status = GetEnemyStatusText(statusKind, enemy);
            if (!string.IsNullOrEmpty(status))
            {
                statusStyle.normal.textColor =
                    GetStatusColor(statusKind);
                GUI.Label(
                    new Rect(rect.x + 10f, rect.y + 69f, rect.width - 20f, 15f),
                    status,
                    statusStyle);
            }
        }

        private void DrawBossStrip(
            Rect rect,
            DungeonBoss boss,
            Transform focus)
        {
            GUI.DrawTexture(rect, slateTexture);
            GUI.DrawTexture(
                new Rect(rect.x, rect.y, rect.width, 3f),
                copperTexture);
            var focused = focus == boss.transform;
            if (focused)
            {
                GUI.DrawTexture(
                    new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                    focusTexture);
            }

            if (TetherfallAccessibilityRuntime.ShouldReinforceColorCues)
            {
                DrawRectOutline(rect, parchmentTexture, 1f);
            }

            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 6f, rect.width * 0.58f, 21f),
                boss.BossName,
                bossNameStyle);
            GUI.Label(
                new Rect(
                    rect.x + rect.width * 0.52f,
                    rect.y + 6f,
                    rect.width * 0.48f - 14f,
                    21f),
                $"PHASE {boss.Phase}  BAND {boss.ThreatBand}",
                bandStyle);

            var healthRect =
                new Rect(rect.x + 14f, rect.y + 32f, rect.width - 28f, 10f);
            DrawBar(
                healthRect,
                boss.Health,
                boss.MaxHealth,
                hostileTexture,
                true);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 44f, rect.width * 0.5f, 16f),
                $"HP {boss.Health}/{boss.MaxHealth}",
                metaStyle);
            GUI.Label(
                new Rect(
                    rect.x + rect.width * 0.48f,
                    rect.y + 44f,
                    rect.width * 0.52f - 14f,
                    16f),
                $"POISE {Mathf.CeilToInt(boss.Poise)}/{Mathf.CeilToInt(boss.PoiseThreshold)}",
                metaRightStyle);
            DrawBar(
                new Rect(rect.x + 14f, rect.y + 62f, rect.width - 28f, 5f),
                boss.Poise,
                boss.PoiseThreshold,
                focusTexture,
                false);

            var statusKind =
                TetherfallThreatPresentationRules.ResolveBossStatus(
                    boss.StaggerRemaining,
                    boss.ExposeRemaining);
            var status = GetBossStatusText(statusKind, boss);
            if (!string.IsNullOrEmpty(status))
            {
                statusStyle.normal.textColor =
                    GetStatusColor(statusKind);
                GUI.Label(
                    new Rect(rect.x + 14f, rect.y + 70f, rect.width - 28f, 17f),
                    status,
                    statusStyle);
            }
        }

        private void DrawBar(
            Rect rect,
            float value,
            float maximum,
            Texture2D fill,
            bool reinforceHealth)
        {
            GUI.DrawTexture(rect, inkTexture);
            var fraction = maximum > 0f
                ? Mathf.Clamp01(value / maximum)
                : 0f;
            if (fraction > 0f)
            {
                GUI.DrawTexture(
                    new Rect(rect.x, rect.y, rect.width * fraction, rect.height),
                    fill);
            }

            if (reinforceHealth &&
                TetherfallAccessibilityRuntime.ShouldReinforceColorCues)
            {
                for (var index = 1; index <= 3; index++)
                {
                    GUI.DrawTexture(
                        new Rect(
                            rect.x + rect.width * index * 0.25f,
                            rect.y,
                            1f,
                            rect.height),
                        parchmentTexture);
                }
            }
        }

        private void DrawEdgePip(
            Vector2 point,
            EnemyAttackStyle attackStyle,
            bool focused)
        {
            var rect = new Rect(point.x - 13f, point.y - 13f, 26f, 26f);
            GUI.DrawTexture(rect, slateTexture);
            var signalTexture = focused ? focusTexture : hostileTexture;
            DrawRectOutline(rect, signalTexture, focused ? 2f : 1f);
            if (TetherfallAccessibilityRuntime.ShouldReinforceColorCues)
            {
                DrawRectOutline(
                    new Rect(
                        rect.x - 2f,
                        rect.y - 2f,
                        rect.width + 4f,
                        rect.height + 4f),
                    parchmentTexture,
                    1f);
            }

            DrawArchetypeMark(
                new Rect(rect.x + 6f, rect.y + 6f, 14f, 14f),
                attackStyle,
                signalTexture);
        }

        private static string GetEnemyStatusText(
            TetherfallThreatStatusKind status,
            Enemy enemy)
        {
            return status switch
            {
                TetherfallThreatStatusKind.Ashguard =>
                    $"ASHGUARD  CROSSWEAVE  {Mathf.CeilToInt(enemy.AshguardRemaining)}s",
                TetherfallThreatStatusKind.Staggered =>
                    $"STAGGERED  {Mathf.CeilToInt(enemy.StaggerRemaining)}s",
                TetherfallThreatStatusKind.Exposed =>
                    $"EXPOSED +28%  {Mathf.CeilToInt(enemy.ExposeRemaining)}s",
                _ => string.Empty
            };
        }

        private static string GetBossStatusText(
            TetherfallThreatStatusKind status,
            DungeonBoss boss)
        {
            return status switch
            {
                TetherfallThreatStatusKind.Staggered =>
                    $"STAGGERED  {Mathf.CeilToInt(boss.StaggerRemaining)}s",
                TetherfallThreatStatusKind.Exposed =>
                    $"EXPOSED +15%  {Mathf.CeilToInt(boss.ExposeRemaining)}s",
                _ => string.Empty
            };
        }

        private Color GetStatusColor(TetherfallThreatStatusKind status)
        {
            return status switch
            {
                TetherfallThreatStatusKind.Ashguard =>
                    TetherfallAccessibilityRuntime.ResolveColorCue(
                        DangerCinder,
                        TetherfallAccessibilityColorCue.Hostile),
                TetherfallThreatStatusKind.Exposed => RouteworkCopper,
                _ => WarmParchment
            };
        }

        private static string GetArchetypeLabel(EnemyAttackStyle attackStyle)
        {
            return attackStyle switch
            {
                EnemyAttackStyle.Ranged => "RANGED",
                EnemyAttackStyle.Heavy => "HEAVY",
                _ => "MELEE"
            };
        }

        private static Vector2 GetEdgePipPoint(
            Vector2 logicalPoint,
            bool behindCamera,
            float logicalWidth,
            float logicalHeight)
        {
            if (behindCamera)
            {
                logicalPoint = new Vector2(
                    logicalWidth - logicalPoint.x,
                    logicalHeight - logicalPoint.y);
            }

            var center =
                new Vector2(logicalWidth * 0.5f, logicalHeight * 0.5f);
            var direction = logicalPoint - center;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            var horizontalScale = Mathf.Abs(direction.x) > 0.001f
                ? logicalWidth * 0.5f / Mathf.Abs(direction.x)
                : float.PositiveInfinity;
            var verticalScale = Mathf.Abs(direction.y) > 0.001f
                ? logicalHeight * 0.5f / Mathf.Abs(direction.y)
                : float.PositiveInfinity;
            var edgeDistance =
                Mathf.Min(horizontalScale, verticalScale);
            return TetherfallThreatPresentationRules.ClampEdgePip(
                center + direction * edgeDistance,
                logicalWidth,
                logicalHeight);
        }

        private Rect ResolvePlateOverlap(
            Rect rect,
            int priorCount,
            float logicalWidth,
            float logicalHeight,
            float topBoundary)
        {
            for (var pass = 0; pass < priorCount; pass++)
            {
                for (var index = 0; index < priorCount; index++)
                {
                    if (!rect.Overlaps(drawnPlateRects[index]))
                    {
                        continue;
                    }

                    var above = rect;
                    above.y =
                        drawnPlateRects[index].y -
                        rect.height -
                        6f;
                    rect.y = above.y >= topBoundary
                        ? above.y
                        : drawnPlateRects[index].yMax + 6f;
                    rect =
                        TetherfallThreatPresentationRules.ClampPlateRect(
                            rect,
                            logicalWidth,
                            logicalHeight,
                            topBoundary);
                }
            }

            return rect;
        }

        private static void DrawArchetypeMark(
            Rect rect,
            EnemyAttackStyle attackStyle,
            Texture2D texture)
        {
            switch (attackStyle)
            {
                case EnemyAttackStyle.Ranged:
                    for (var row = 0; row < 5; row++)
                    {
                        var width = 2f + row * 2.4f;
                        GUI.DrawTexture(
                            new Rect(
                                rect.center.x - width * 0.5f,
                                rect.y + 2f + row * 2.4f,
                                width,
                                2f),
                            texture);
                    }

                    break;

                case EnemyAttackStyle.Heavy:
                    DrawRectOutline(
                        new Rect(
                            rect.x + 2f,
                            rect.y + 2f,
                            rect.width - 4f,
                            rect.height - 4f),
                        texture,
                        2f);
                    GUI.DrawTexture(
                        new Rect(
                            rect.center.x - 1f,
                            rect.y + 5f,
                            2f,
                            rect.height - 10f),
                        texture);
                    break;

                default:
                    for (var step = 0; step < 4; step++)
                    {
                        var inset = Mathf.Abs(1.5f - step) * 3f;
                        GUI.DrawTexture(
                            new Rect(
                                rect.x + inset,
                                rect.y + 2f + step * 3f,
                                rect.width - inset * 2f,
                                2f),
                            texture);
                    }

                    break;
            }
        }

        private static void DrawRectOutline(
            Rect rect,
            Texture2D texture,
            float thickness)
        {
            GUI.DrawTexture(
                new Rect(rect.x, rect.y, rect.width, thickness),
                texture);
            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.yMax - thickness,
                    rect.width,
                    thickness),
                texture);
            GUI.DrawTexture(
                new Rect(rect.x, rect.y, thickness, rect.height),
                texture);
            GUI.DrawTexture(
                new Rect(
                    rect.xMax - thickness,
                    rect.y,
                    thickness,
                    rect.height),
                texture);
        }

        private void EnsureStyles()
        {
            if (stylesReady)
            {
                return;
            }

            nameStyle = CreateStyle(
                13,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                WarmParchment);
            bossNameStyle = CreateStyle(
                15,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                WarmParchment);
            metaStyle = CreateStyle(
                10,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                MutedReed);
            metaRightStyle = CreateStyle(
                10,
                FontStyle.Normal,
                TextAnchor.MiddleRight,
                MutedReed);
            bandStyle = CreateStyle(
                10,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                ClearMist);
            statusStyle = CreateStyle(
                10,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                WarmParchment);
            stylesReady = true;
        }

        private static GUIStyle CreateStyle(
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = color }
            };
        }

        private void CreateTextures()
        {
            slateTexture = CreateSolidTexture("Threat slate", DeepSlate);
            inkTexture = CreateSolidTexture("Threat ink", Ink);
            copperTexture =
                CreateSolidTexture("Threat copper", RouteworkCopper);
            focusTexture = CreateSolidTexture(
                "Threat focus",
                ResolveFocusColor());
            hostileTexture = CreateSolidTexture(
                "Threat hostile",
                ResolveHostileColor());
            parchmentTexture =
                CreateSolidTexture("Threat parchment", WarmParchment);
        }

        private void CreateOutlinePool()
        {
            outlineMesh = CreateLowPolyProxyMesh();
            var shader = Shader.Find("Tetherfall/Hostile Outline");
            if (shader == null)
            {
                Debug.LogWarning(
                    "Tetherfall hostile outline shader was not found. Threat plates remain active.");
                return;
            }

            outlineMaterial = new Material(shader)
            {
                name = "Tetherfall Hostile Outline Shared",
                hideFlags = HideFlags.DontSave
            };
            for (var index = 0; index < outlineSlots.Length; index++)
            {
                var hull = new GameObject($"Threat outline hull {index + 1}");
                hull.transform.SetParent(transform, false);
                var filter = hull.AddComponent<MeshFilter>();
                filter.sharedMesh = outlineMesh;
                var hullRenderer = hull.AddComponent<MeshRenderer>();
                hullRenderer.sharedMaterial = outlineMaterial;
                hullRenderer.shadowCastingMode = ShadowCastingMode.Off;
                hullRenderer.receiveShadows = false;
                hullRenderer.lightProbeUsage = LightProbeUsage.Off;
                hullRenderer.reflectionProbeUsage =
                    ReflectionProbeUsage.Off;
                hullRenderer.motionVectorGenerationMode =
                    MotionVectorGenerationMode.ForceNoMotion;
                hullRenderer.enabled = false;
                outlineSlots[index] = new OutlineSlot
                {
                    Transform = hull.transform,
                    Renderer = hullRenderer,
                    PropertyBlock = new MaterialPropertyBlock()
                };
            }
        }

        private static Mesh CreateLowPolyProxyMesh()
        {
            const int sideCount = 8;
            const int ringCount = 4;
            var vertices =
                new Vector3[sideCount * ringCount + 2];
            var radii = new[] { 0.34f, 0.74f, 0.7f, 0.26f };
            var heights = new[] { 0.03f, 0.2f, 0.76f, 0.97f };
            for (var ring = 0; ring < ringCount; ring++)
            {
                for (var side = 0; side < sideCount; side++)
                {
                    var angle = side * Mathf.PI * 2f / sideCount;
                    vertices[ring * sideCount + side] = new Vector3(
                        Mathf.Cos(angle) * radii[ring],
                        heights[ring],
                        Mathf.Sin(angle) * radii[ring]);
                }
            }

            var bottomIndex = sideCount * ringCount;
            var topIndex = bottomIndex + 1;
            vertices[bottomIndex] = Vector3.zero;
            vertices[topIndex] = Vector3.up;
            var triangles =
                new int[(sideCount * 2 +
                         (ringCount - 1) * sideCount * 2) * 3];
            var triangleIndex = 0;
            for (var side = 0; side < sideCount; side++)
            {
                var next = (side + 1) % sideCount;
                triangles[triangleIndex++] = bottomIndex;
                triangles[triangleIndex++] = side;
                triangles[triangleIndex++] = next;
                for (var ring = 0; ring < ringCount - 1; ring++)
                {
                    var lower = ring * sideCount;
                    var upper = (ring + 1) * sideCount;
                    triangles[triangleIndex++] = lower + side;
                    triangles[triangleIndex++] = upper + side;
                    triangles[triangleIndex++] = lower + next;
                    triangles[triangleIndex++] = lower + next;
                    triangles[triangleIndex++] = upper + side;
                    triangles[triangleIndex++] = upper + next;
                }

                var topRing = (ringCount - 1) * sideCount;
                triangles[triangleIndex++] = topRing + side;
                triangles[triangleIndex++] = topIndex;
                triangles[triangleIndex++] = topRing + next;
            }

            var mesh = new Mesh
            {
                name = "Tetherfall Low-Poly Threat Hull",
                hideFlags = HideFlags.DontSave,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnAccessibilitySettingsChanged()
        {
            UpdateSolidTexture(focusTexture, ResolveFocusColor());
            UpdateSolidTexture(hostileTexture, ResolveHostileColor());
            stylesReady = false;
        }

        private static Color ResolveFocusColor()
        {
            return TetherfallAccessibilityRuntime.ResolveColorCue(
                ThreadglassTeal,
                TetherfallAccessibilityColorCue.Friendly);
        }

        private static Color ResolveHostileColor()
        {
            return TetherfallAccessibilityRuntime.ResolveColorCue(
                DangerCinder,
                TetherfallAccessibilityColorCue.Hostile);
        }

        private static Texture2D CreateSolidTexture(
            string textureName,
            Color color)
        {
            var texture =
                new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = textureName,
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
            UpdateSolidTexture(texture, color);
            return texture;
        }

        private static void UpdateSolidTexture(
            Texture2D texture,
            Color color)
        {
            if (texture == null)
            {
                return;
            }

            texture.SetPixel(0, 0, color);
            texture.Apply(false, false);
        }

        private static float GetEnemyAnchorHeight(Enemy enemy)
        {
            var controller = enemy.GetComponent<CharacterController>();
            if (controller != null)
            {
                return Mathf.Max(
                    1.6f,
                    (controller.center.y + controller.height * 0.62f) *
                    Mathf.Abs(enemy.transform.lossyScale.y));
            }

            return enemy.AttackStyle == EnemyAttackStyle.Heavy
                ? 2.8f
                : 2.25f;
        }

        private static bool IsEnemyAwake(Enemy enemy)
        {
            return enemy != null &&
                   !enemy.IsDefeated &&
                   enemy.isActiveAndEnabled &&
                   enemy.gameObject.activeInHierarchy;
        }

        private static bool IsBossAvailable(DungeonBoss boss)
        {
            return boss != null &&
                   !boss.IsDefeated &&
                   boss.isActiveAndEnabled &&
                   boss.gameObject.activeInHierarchy;
        }

        private static float HorizontalDistanceSquared(
            Vector3 left,
            Vector3 right)
        {
            var delta = right - left;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private void HideAllOutlines()
        {
            for (var index = 0; index < outlineSlots.Length; index++)
            {
                if (outlineSlots[index] != null)
                {
                    outlineSlots[index].Renderer.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            TetherfallAccessibilityRuntime.SettingsChanged -=
                OnAccessibilitySettingsChanged;
            if (instance == this)
            {
                instance = null;
            }

            DestroyRuntimeObject(outlineMaterial);
            DestroyRuntimeObject(outlineMesh);
            DestroyRuntimeObject(slateTexture);
            DestroyRuntimeObject(inkTexture);
            DestroyRuntimeObject(copperTexture);
            DestroyRuntimeObject(focusTexture);
            DestroyRuntimeObject(hostileTexture);
            DestroyRuntimeObject(parchmentTexture);
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private struct ThreatCandidate
        {
            public Enemy Enemy;
            public TetherfallThreatPriorityKey Key;
        }

        private sealed class OutlineSlot
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
            public Transform Target;
            public Enemy Enemy;
            public DungeonBoss Boss;
            public Vector3 BaseScale;
            public float VerticalOffset;
        }
    }
}
