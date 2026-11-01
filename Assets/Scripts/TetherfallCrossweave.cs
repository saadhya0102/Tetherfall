using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private const float CrossweaveRelaySeconds = 2.25f;
        private const float CrossweaveCooldownSeconds = 4.5f;
        private const float CrossweaveResponderRange = 18f;

        private CrossweaveDefinition activeCrossweave;
        private CrossweaveResponder crossweaveResponder;
        private Enemy crossweaveEnemy;
        private DungeonBoss crossweaveBoss;
        private float crossweaveEndsAt;
        private float nextCrossweaveAt;
        private int crossweaveToken;
        private bool crossweaveWeaponSavePending;
        private float crossweaveWeaponSaveAt;
        private readonly RaycastHit[] crossweaveLineHitBuffer = new RaycastHit[24];

        public bool IsCrossweaveActive => activeCrossweave != null && Time.time < crossweaveEndsAt;

        private void UpdateCrossweave()
        {
            if (crossweaveWeaponSavePending && Time.time >= crossweaveWeaponSaveAt)
            {
                crossweaveWeaponSavePending = false;
                RequestSave();
            }

            if (!IsCrossweaveActive)
            {
                if (activeCrossweave != null)
                {
                    ClearCrossweave();
                }

                return;
            }

            if (GameplayPaused)
            {
                crossweaveEndsAt += Time.deltaTime;
                return;
            }

            if (!HasLiveCrossweaveTarget() ||
                !IsCrossweaveTargetFocused() ||
                crossweaveResponder == null ||
                !crossweaveResponder.IsAvailable(this))
            {
                ClearCrossweave();
            }
        }

        public void ArmCrossweaveFromWeaponSwap(WeaponDiscipline previousWeapon, WeaponDiscipline nextWeapon)
        {
            ClearCrossweave();
            if (previousWeapon == nextWeapon ||
                GameplayPaused ||
                Player == null ||
                Time.time < nextCrossweaveAt ||
                !TryGetCrossweaveTarget(out var enemy, out var boss))
            {
                return;
            }

            var definition = CrossweaveCatalog.Get(nextWeapon);
            var responder = GetCrossweaveResponder(definition);
            if (definition == null || responder == null)
            {
                return;
            }

            activeCrossweave = definition;
            crossweaveResponder = responder;
            crossweaveEnemy = enemy;
            crossweaveBoss = boss;
            crossweaveEndsAt = Time.time + CrossweaveRelaySeconds;
            nextCrossweaveAt = Time.time + CrossweaveCooldownSeconds;
            crossweaveToken++;
            if (crossweaveToken <= 0)
            {
                crossweaveToken = 1;
            }

            var target = enemy != null ? enemy.transform : boss.transform;
            CrossweaveRelayMark.Attach(
                this,
                target,
                definition,
                responder.DisplayName,
                crossweaveEndsAt);
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                $"{definition.displayName} prime",
                target.position + Vector3.up * 0.08f,
                new Vector3(0.24f, 0.03f, 0.24f),
                RuntimeEffectMaterialCache.GetTransparent(
                    this,
                    new Color(definition.color.r, definition.color.g, definition.color.b, 0.54f),
                    0.7f),
                0.24f,
                3.6f);
            Notify($"{definition.displayName}: strike to call {responder.DisplayName}.");
        }

        public void RequestCrossweaveWeaponSave()
        {
            crossweaveWeaponSavePending = true;
            crossweaveWeaponSaveAt = Time.time + 1.2f;
        }

        public int CaptureCrossweaveRelayToken(WeaponDiscipline weapon)
        {
            return IsCrossweaveActive && activeCrossweave.weapon == weapon
                ? crossweaveToken
                : 0;
        }

        public void TryResolveCrossweave(Enemy target, int relayToken = 0)
        {
            if (target == null || target.IsDefeated || !CanResolveCrossweave(target, relayToken))
            {
                return;
            }

            if (!crossweaveResponder.CanReach(this, target.transform, CrossweaveResponderRange) ||
                !crossweaveResponder.TryRespond(target))
            {
                ClearCrossweave();
                return;
            }

            RegisterOvertone(target, OvertonePitch.Bind, $"{activeCrossweave.displayName} response");
            var responderName = crossweaveResponder.DisplayName;
            var brokeAshguard = !target.IsDefeated && target.TryBreakAshguardFromCrossweave(responderName);
            Notify(brokeAshguard
                ? $"{responderName} answers the {activeCrossweave.cue} relay and fractures Ashguard."
                : $"{responderName} answers the {activeCrossweave.cue} relay.");
            ClearCrossweave();
        }

        public void TryResolveCrossweave(DungeonBoss target, int relayToken = 0)
        {
            if (target == null || target.IsDefeated || !CanResolveCrossweave(target, relayToken))
            {
                return;
            }

            if (!crossweaveResponder.CanReach(this, target.transform, CrossweaveResponderRange) ||
                !crossweaveResponder.TryRespond(target))
            {
                ClearCrossweave();
                return;
            }

            RegisterOvertone(target, OvertonePitch.Bind, $"{activeCrossweave.displayName} response");
            Notify($"{crossweaveResponder.DisplayName} answers the {activeCrossweave.cue} relay.");
            ClearCrossweave();
        }

        public void ClearCrossweave()
        {
            if (crossweaveEnemy != null)
            {
                CrossweaveRelayMark.Clear(crossweaveEnemy.transform);
            }

            if (crossweaveBoss != null)
            {
                CrossweaveRelayMark.Clear(crossweaveBoss.transform);
            }

            activeCrossweave = null;
            crossweaveResponder = null;
            crossweaveEnemy = null;
            crossweaveBoss = null;
            crossweaveEndsAt = 0f;
        }

        private bool CanResolveCrossweave(IExpeditionDamageable target, int relayToken)
        {
            if (!IsCrossweaveActive ||
                (relayToken != 0 && relayToken != crossweaveToken) ||
                !IsCrossweaveTargetFocused())
            {
                return false;
            }

            return ReferenceEquals(target, crossweaveEnemy) || ReferenceEquals(target, crossweaveBoss);
        }

        private bool TryGetCrossweaveTarget(out Enemy enemy, out DungeonBoss boss)
        {
            enemy = null;
            boss = null;
            if (CombatTarget == null)
            {
                return false;
            }

            if (lockedEnemy != null &&
                !lockedEnemy.IsDefeated &&
                CombatTarget == lockedEnemy.transform)
            {
                enemy = lockedEnemy;
                return true;
            }

            if (lockedBoss != null &&
                !lockedBoss.IsDefeated &&
                CombatTarget == lockedBoss.transform)
            {
                boss = lockedBoss;
                return true;
            }

            return false;
        }

        private bool HasLiveCrossweaveTarget()
        {
            return crossweaveEnemy != null && !crossweaveEnemy.IsDefeated ||
                   crossweaveBoss != null && !crossweaveBoss.IsDefeated;
        }

        private bool IsCrossweaveTargetFocused()
        {
            return crossweaveEnemy != null && CombatTarget == crossweaveEnemy.transform ||
                   crossweaveBoss != null && CombatTarget == crossweaveBoss.transform;
        }

        private CrossweaveResponder GetCrossweaveResponder(CrossweaveDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            for (var pass = 0; pass < 2; pass++)
            {
                var focusOnly = pass == 0;
                for (var slot = 0; slot < ActiveCombatPactCount; slot++)
                {
                    if (!TryGetActivePactActor(
                            slot,
                            out var record,
                            out var storyActor,
                            out var capturedActor) ||
                        record == null ||
                        (focusOnly &&
                         !string.Equals(
                             record.instanceId,
                             PactCommandFocusInstanceId,
                             System.StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    if (storyActor != null &&
                        CrossweaveCatalog.GetPactAffinity(storyActor) ==
                        definition.affinity)
                    {
                        return new CrossweaveResponder(storyActor);
                    }

                    if (capturedActor != null &&
                        CrossweaveCatalog.GetPactAffinity(
                            capturedActor.ResonanceAffinity) ==
                        definition.affinity)
                    {
                        return new CrossweaveResponder(capturedActor);
                    }
                }
            }

            foreach (var companion in party)
            {
                if (companion != null &&
                    companion.IsRecruited &&
                    !companion.IsReserve &&
                    CrossweaveCatalog.GetCompanionAffinity(companion) == definition.affinity)
                {
                    return new CrossweaveResponder(companion);
                }
            }

            return null;
        }

        internal bool HasCrossweaveLineOfSight(Transform source, Transform target)
        {
            if (source == null || target == null)
            {
                return false;
            }

            var origin = source.position + Vector3.up * 0.8f;
            var destination = target.position + Vector3.up * 0.9f;
            var path = destination - origin;
            var distance = path.magnitude;
            if (distance <= 0.01f)
            {
                return true;
            }

            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                path / distance,
                crossweaveLineHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = crossweaveLineHitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<PlayerAvatar>() != null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                }
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                crossweaveLineHitBuffer[hitIndex] = default;
            }

            if (closestCollider == null)
            {
                return false;
            }

            var hitTransform = closestCollider.transform;
            return hitTransform == target ||
                   hitTransform.IsChildOf(target) ||
                   target.IsChildOf(hitTransform);
        }

        private sealed class CrossweaveResponder
        {
            private readonly PactCreature pact;
            private readonly CapturedPactActor capturedPact;
            private readonly HumanCompanion companion;

            public CrossweaveResponder(PactCreature source)
            {
                pact = source;
            }

            public CrossweaveResponder(CapturedPactActor source)
            {
                capturedPact = source;
            }

            public CrossweaveResponder(HumanCompanion source)
            {
                companion = source;
            }

            public string DisplayName => pact != null ? pact.CreatureName :
                capturedPact != null ? capturedPact.DisplayName :
                companion != null ? companion.CompanionName : "Field ally";

            public bool IsAvailable(TetherfallGame game)
            {
                return (pact != null &&
                        pact.IsPacted &&
                        !pact.IsMount &&
                        pact.IsFieldActive &&
                        pact.isActiveAndEnabled) ||
                       (capturedPact != null &&
                        capturedPact.IsFieldActive) ||
                       (companion != null &&
                        companion.IsRecruited &&
                        !companion.IsReserve);
            }

            public bool CanReach(TetherfallGame game, Transform target, float range)
            {
                var origin = pact != null
                    ? pact.transform
                    : capturedPact != null
                        ? capturedPact.transform
                        : companion != null
                            ? companion.transform
                            : null;
                return origin != null &&
                       target != null &&
                       Vector3.Distance(origin.position, target.position) <= range &&
                       game.HasCrossweaveLineOfSight(origin, target);
            }

            public bool TryRespond(Enemy target)
            {
                return pact != null
                    ? pact.TriggerCrossweaveResponse(target)
                    : capturedPact != null
                        ? capturedPact.TriggerCrossweaveResponse(target)
                        : companion != null &&
                          companion.TriggerCrossweaveResponse(target);
            }

            public bool TryRespond(DungeonBoss target)
            {
                return pact != null
                    ? pact.TriggerCrossweaveResponse(target)
                    : capturedPact != null
                        ? capturedPact.TriggerCrossweaveResponse(target)
                        : companion != null &&
                          companion.TriggerCrossweaveResponse(target);
            }
        }
    }

    public sealed class CrossweaveRelayMark : MonoBehaviour
    {
        private static readonly Dictionary<int, Material> CachedMaterials = new Dictionary<int, Material>();

        private TetherfallGame game;
        private Transform visualRoot;
        private TextMesh label;
        private float expiresAt;
        private string cue;
        private string responderName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCachedMaterials()
        {
            CachedMaterials.Clear();
        }

        public static void Attach(
            TetherfallGame owner,
            Transform target,
            CrossweaveDefinition definition,
            string responderName,
            float newExpiresAt)
        {
            if (owner == null || target == null || definition == null)
            {
                return;
            }

            var mark = target.GetComponent<CrossweaveRelayMark>();
            if (mark == null)
            {
                mark = target.gameObject.AddComponent<CrossweaveRelayMark>();
            }

            mark.Initialize(owner, definition, responderName, newExpiresAt);
        }

        public static void Clear(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var mark = target.GetComponent<CrossweaveRelayMark>();
            if (mark != null)
            {
                Object.Destroy(mark);
            }
        }

        private void Initialize(
            TetherfallGame owner,
            CrossweaveDefinition definition,
            string newResponderName,
            float newExpiresAt)
        {
            game = owner;
            expiresAt = newExpiresAt;
            cue = definition.cue;
            responderName = newResponderName;
            if (visualRoot == null)
            {
                visualRoot = new GameObject("Crossweave relay visual").transform;
                visualRoot.SetParent(transform, false);

                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Crossweave relay ring";
                ring.transform.SetParent(visualRoot, false);
                ring.transform.localPosition = new Vector3(0f, 0.13f, 0f);
                ring.transform.localScale = new Vector3(1.22f, 0.022f, 1.22f);
                Object.Destroy(ring.GetComponent<Collider>());

                for (var index = 0; index < 4; index++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = "Crossweave relay shard";
                    shard.transform.SetParent(visualRoot, false);
                    var angle = (index * 90f + 45f) * Mathf.Deg2Rad;
                    shard.transform.localPosition = new Vector3(
                        Mathf.Cos(angle) * 0.82f,
                        1.18f,
                        Mathf.Sin(angle) * 0.82f);
                    shard.transform.localScale = new Vector3(0.1f, 0.26f, 0.1f);
                    shard.transform.localRotation = Quaternion.Euler(20f, index * 90f, 20f);
                    Object.Destroy(shard.GetComponent<Collider>());
                }

                var textRoot = new GameObject("Crossweave relay label");
                textRoot.transform.SetParent(visualRoot, false);
                textRoot.transform.localPosition = new Vector3(0f, 2.35f, 0f);
                label = textRoot.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.characterSize = 0.1f;
                label.fontSize = 54;
                label.fontStyle = FontStyle.Bold;
            }

            var material = GetMaterial(definition.color);
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>())
            {
                if (renderer.GetComponent<TextMesh>() == null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            if (label != null)
            {
                label.color = Color.Lerp(definition.color, Color.white, 0.28f);
            }
        }

        private void Update()
        {
            if (game == null || Time.time >= expiresAt)
            {
                Object.Destroy(this);
                return;
            }

            if (game.GameplayPaused)
            {
                expiresAt += Time.deltaTime;
                return;
            }

            var remaining = Mathf.Max(0f, expiresAt - Time.time);
            if (label != null)
            {
                label.text = string.IsNullOrEmpty(responderName)
                    ? $"{cue} {Mathf.CeilToInt(remaining)}"
                    : $"{cue} · {responderName} · {Mathf.CeilToInt(remaining)}";
            }

            if (visualRoot != null)
            {
                visualRoot.Rotate(0f, 105f * Time.deltaTime, 0f, Space.World);
                var pulse = 1f + Mathf.Sin(Time.time * 11f) * 0.08f;
                visualRoot.localScale = Vector3.one * pulse;
            }
        }

        private void OnDestroy()
        {
            if (visualRoot != null)
            {
                Object.Destroy(visualRoot.gameObject);
            }
        }

        private Material GetMaterial(Color color)
        {
            var color32 = (Color32)color;
            var key = color32.r << 24 | color32.g << 16 | color32.b << 8 | color32.a;
            if (!CachedMaterials.TryGetValue(key, out var material) || material == null)
            {
                material = game.CreateGlowMaterial(color, 1.65f, 0.62f);
                CachedMaterials[key] = material;
            }

            return material;
        }
    }
}
