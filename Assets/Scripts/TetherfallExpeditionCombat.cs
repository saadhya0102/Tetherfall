using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private ExpeditionDirective expeditionDirective = ExpeditionDirective.Trail;
        private ThreadlinkSeal threadlinkSeal;
        private float threadlinkEndsAt;
        private float nextThreadlinkContributionAt;
        private int threadlinkContributions;
        private Enemy threadlinkEnemy;
        private DungeonBoss threadlinkBoss;
        private float aegisWardEndsAt;
        private readonly List<ThreadlinkRallyResponder> threadlinkRallyResponders =
            new List<ThreadlinkRallyResponder>(2);
        private readonly List<ThreadlinkRallyResponder> threadlinkAvailableResponderBuffer =
            new List<ThreadlinkRallyResponder>(PactActorBudget + 2);
        private readonly List<ThreadlinkRallyResponder> threadlinkSelectedResponderBuffer =
            new List<ThreadlinkRallyResponder>(2);
        private int threadlinkRallyBeatIndex;
        private float nextThreadlinkRallyBeatAt;
        private string threadlinkBeatOneResponderId;
        private string threadlinkBeatTwoResponderId;
        private readonly RaycastHit[] expeditionLineHitBuffer = new RaycastHit[24];
        private readonly Collider[] threadlinkAreaHitBuffer = new Collider[64];
        private readonly HashSet<Enemy> threadlinkAffectedEnemies = new HashSet<Enemy>();

        public ExpeditionDirective CurrentExpeditionDirective => expeditionDirective;
        public bool IsThreadlinkActive => threadlinkSeal != ThreadlinkSeal.None && Time.time < threadlinkEndsAt;
        public bool IsThreadlinkRallyInProgress => threadlinkRallyResponders.Count > 0;
        public float AegisWardRemaining => Mathf.Max(0f, aegisWardEndsAt - Time.time);
        public float ExpeditionDamageMultiplier => expeditionDirective == ExpeditionDirective.Focus ? 1.18f :
            expeditionDirective == ExpeditionDirective.Bulwark ? 0.86f : 1f;
        public int ExpeditionWardReduction
        {
            get
            {
                var reduction = Time.time < aegisWardEndsAt ? 4 : 0;
                if (expeditionDirective == ExpeditionDirective.Bulwark && CountActiveExpeditionAllies() > 0)
                {
                    reduction += 4;
                }

                reduction += PactWardReduction;
                return reduction;
            }
        }

        public string GetExpeditionWardStatus()
        {
            var aegis = AegisWardRemaining > 0f
                ? $"Ward +4 {Mathf.CeilToInt(AegisWardRemaining)}s"
                : string.Empty;
            var bulwark = expeditionDirective == ExpeditionDirective.Bulwark &&
                          CountActiveExpeditionAllies() > 0
                ? "Bulwark +4"
                : string.Empty;
            var pactWard = PactWardReduction > 0
                ? $"Pact ward +{PactWardReduction}"
                : string.Empty;
            var status = string.IsNullOrEmpty(aegis)
                ? bulwark
                : string.IsNullOrEmpty(bulwark)
                    ? aegis
                    : $"{aegis} · {bulwark}";
            if (string.IsNullOrEmpty(status))
            {
                return pactWard;
            }

            return string.IsNullOrEmpty(pactWard) ? status : $"{status} · {pactWard}";
        }

        private void UpdateExpeditionCombat()
        {
            if (GameplayPaused)
            {
                if (threadlinkSeal != ThreadlinkSeal.None)
                {
                    threadlinkEndsAt += Time.deltaTime;
                    if (threadlinkRallyResponders.Count > 0)
                    {
                        nextThreadlinkRallyBeatAt += Time.deltaTime;
                    }
                }

                if (aegisWardEndsAt > Time.time)
                {
                    aegisWardEndsAt += Time.deltaTime;
                }

                return;
            }

            if (threadlinkSeal != ThreadlinkSeal.None &&
                (Time.time >= threadlinkEndsAt || !HasLiveThreadlinkTarget()))
            {
                ClearThreadlink();
                return;
            }

            UpdateThreadlinkRally();
        }

        public void CycleExpeditionDirective()
        {
            if (GameplayPaused)
            {
                return;
            }

            expeditionDirective = (ExpeditionDirective)(((int)expeditionDirective + 1) % 3);
            var color = GetDirectiveColor();
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Expedition directive pulse",
                PlayerTransform.position + Vector3.up * 0.06f,
                new Vector3(0.3f, 0.03f, 0.3f),
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(color.r, color.g, color.b, 0.58f), 0.66f),
                0.24f,
                4.2f);
            Notify(GetExpeditionDirectiveDescription());
            RequestSave();
        }

        public Enemy GetExpeditionEnemy(float fallbackRange)
        {
            if (PlayerTransform == null)
            {
                return null;
            }

            if (expeditionDirective == ExpeditionDirective.Focus &&
                lockedBoss != null &&
                !lockedBoss.IsDefeated)
            {
                return null;
            }

            if (expeditionDirective == ExpeditionDirective.Focus &&
                lockedEnemy != null &&
                !lockedEnemy.IsDefeated &&
                Vector3.Distance(PlayerTransform.position, lockedEnemy.transform.position) <= 25f &&
                HasExpeditionLineOfSight(lockedEnemy.transform))
            {
                return lockedEnemy;
            }

            var range = expeditionDirective == ExpeditionDirective.Focus
                ? Mathf.Max(fallbackRange, 14f)
                : expeditionDirective == ExpeditionDirective.Bulwark
                    ? Mathf.Min(fallbackRange, 8.5f)
                    : fallbackRange;
            var fallbackTarget = Enemy.FindClosest(PlayerTransform.position, range);
            return fallbackTarget != null && HasExpeditionLineOfSight(fallbackTarget.transform)
                ? fallbackTarget
                : null;
        }

        private bool HasExpeditionLineOfSight(Transform target)
        {
            if (PlayerTransform == null || target == null)
            {
                return false;
            }

            var origin = PlayerTransform.position + Vector3.up * 1.05f;
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
                expeditionLineHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = expeditionLineHitBuffer[hitIndex];
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
                expeditionLineHitBuffer[hitIndex] = default;
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

        public DungeonBoss GetExpeditionBoss(float fallbackRange)
        {
            var boss = lockedBoss != null ? lockedBoss : ActiveDungeonBoss;
            var allowedRange = expeditionDirective == ExpeditionDirective.Focus
                ? Mathf.Max(fallbackRange, 25f)
                : fallbackRange;
            if (boss == null ||
                boss.IsDefeated ||
                PlayerTransform == null ||
                Vector3.Distance(PlayerTransform.position, boss.transform.position) > allowedRange ||
                !HasExpeditionLineOfSight(boss.transform))
            {
                return null;
            }

            return boss;
        }

        public void ArmThreadlinkFromPlayerAbility()
        {
            if (CountActiveExpeditionAllies() == 0)
            {
                return;
            }

            var enemy = CombatTarget != null ? lockedEnemy : GetExpeditionEnemy(10.5f);
            var boss = enemy == null && CombatTarget != null ? lockedBoss : null;
            if (enemy == null && boss == null)
            {
                return;
            }

            ClearThreadlink();
            threadlinkSeal = ActiveWeapon switch
            {
                WeaponDiscipline.Pathblade => ThreadlinkSeal.Sever,
                WeaponDiscipline.Riftbow => ThreadlinkSeal.Latch,
                WeaponDiscipline.Threadstaff => ThreadlinkSeal.Aegis,
                _ => ThreadlinkSeal.None
            };
            threadlinkEnemy = enemy;
            threadlinkBoss = boss;
            threadlinkEndsAt = Time.time + 7.5f;
            nextThreadlinkContributionAt = Time.time;
            threadlinkContributions = 0;
            ResetThreadlinkWeaveSelection();

            var target = enemy != null ? enemy.transform : boss.transform;
            ThreadlinkMark.Attach(this, target, threadlinkSeal, threadlinkEndsAt);
            if (enemy != null)
            {
                enemy.ApplyExpose(2.8f);
            }
            else if (threadlinkSeal == ThreadlinkSeal.Latch)
            {
                boss.ApplyExpose(2.8f);
            }

            Notify($"{GetThreadlinkSealName()} sealed. T/Q steer the two-beat weave, then press V.");
        }

        public bool TryStartThreadlinkRally()
        {
            if (!IsThreadlinkActive)
            {
                return false;
            }

            if (!HasLiveThreadlinkTarget() || !IsThreadlinkTargetReachable())
            {
                ClearThreadlink();
                Notify("The Threadlink target is no longer in range. The seal unravels.");
                return true;
            }

            var availableResponders = GetSelectedThreadlinkRallyResponders();
            if (availableResponders.Count == 0)
            {
                Notify("No field ally can answer the Threadlink Rally.");
                return true;
            }

            if (!TrySpendStamina(TacticalCommandStaminaCost, true))
            {
                return true;
            }

            threadlinkRallyResponders.Clear();
            threadlinkRallyResponders.Add(availableResponders[0]);
            threadlinkRallyResponders.Add(availableResponders[1]);
            threadlinkRallyBeatIndex = 0;
            nextThreadlinkRallyBeatAt = Time.time + 0.14f;
            nextTacticalCommandAt = Time.time + 13f;

            var firstName = threadlinkRallyResponders[0].DisplayName;
            var secondName = threadlinkRallyResponders[1].DisplayName;
            Notify($"Threadlink Rally: {firstName}, then {secondName}.");
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Threadlink Rally call",
                PlayerTransform.position + Vector3.up * 0.06f,
                new Vector3(0.34f, 0.035f, 0.34f),
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(0.88f, 0.68f, 0.3f, 0.58f), 0.68f),
                0.26f,
                4.2f);
            return true;
        }

        private void UpdateThreadlinkRally()
        {
            if (threadlinkRallyResponders.Count == 0 || Time.time < nextThreadlinkRallyBeatAt)
            {
                return;
            }

            if (!IsThreadlinkActive || !HasLiveThreadlinkTarget())
            {
                ClearThreadlink();
                return;
            }

            var responder = threadlinkRallyResponders[Mathf.Min(
                threadlinkRallyBeatIndex,
                threadlinkRallyResponders.Count - 1)];
            if (responder.TryRespond(this, threadlinkRallyBeatIndex + 1))
            {
                Notify($"{responder.DisplayName} answers the Threadlink weave {threadlinkRallyBeatIndex + 1}/2.");
            }
            else
            {
                Notify($"{responder.DisplayName} cannot reach the Threadlink.");
            }

            threadlinkRallyBeatIndex++;
            if (threadlinkRallyBeatIndex >= threadlinkRallyResponders.Count || !IsThreadlinkActive)
            {
                threadlinkRallyResponders.Clear();
                return;
            }

            nextThreadlinkRallyBeatAt = Time.time + 0.42f;
        }

        private List<ThreadlinkRallyResponder> GetThreadlinkRallyResponders()
        {
            threadlinkAvailableResponderBuffer.Clear();
            for (var slot = 0; slot < ActiveCombatPactCount; slot++)
            {
                if (!TryGetActivePactActor(
                        slot,
                        out _,
                        out var storyActor,
                        out var capturedActor))
                {
                    continue;
                }

                if (storyActor != null)
                {
                    threadlinkAvailableResponderBuffer.Add(
                        new ThreadlinkRallyResponder(storyActor));
                }
                else if (capturedActor != null)
                {
                    threadlinkAvailableResponderBuffer.Add(
                        new ThreadlinkRallyResponder(capturedActor));
                }
            }

            foreach (var companion in party)
            {
                if (companion != null && companion.IsRecruited && !companion.IsReserve)
                {
                    threadlinkAvailableResponderBuffer.Add(
                        new ThreadlinkRallyResponder(companion));
                }
            }

            return threadlinkAvailableResponderBuffer;
        }

        public bool TryCycleThreadlinkWeaveBeat(bool firstBeat)
        {
            if (!IsThreadlinkActive)
            {
                return false;
            }

            if (GameplayPaused)
            {
                return true;
            }

            if (IsThreadlinkRallyInProgress)
            {
                Notify("The Threadlink weave is already in motion.");
                return true;
            }

            var responders = GetThreadlinkRallyResponders();
            if (responders.Count == 0)
            {
                Notify("No field ally can be assigned to the Threadlink weave.");
                return true;
            }

            EnsureThreadlinkWeaveSelection(responders);
            if (responders.Count == 1)
            {
                Notify($"{responders[0].DisplayName} is the only ally ready to answer the weave.");
                return true;
            }

            var blockedId = firstBeat ? threadlinkBeatTwoResponderId : threadlinkBeatOneResponderId;
            var currentId = firstBeat ? threadlinkBeatOneResponderId : threadlinkBeatTwoResponderId;
            var nextCandidate = GetNextThreadlinkResponder(
                responders,
                currentId,
                blockedId);
            if (!nextCandidate.HasValue)
            {
                return true;
            }

            var next = nextCandidate.Value;
            if (firstBeat)
            {
                threadlinkBeatOneResponderId = next.Identity;
            }
            else
            {
                threadlinkBeatTwoResponderId = next.Identity;
            }

            var beatName = firstBeat ? "Beat 1" : "Beat 2";
            Notify($"{beatName}: {next.DisplayName} · {next.WeaveLabel}.");
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                $"Threadlink {beatName} assignment",
                next.Origin.position + Vector3.up * 0.06f,
                new Vector3(0.18f, 0.025f, 0.18f),
                RuntimeEffectMaterialCache.GetTransparent(
                    this,
                    new Color(GetSealColor(threadlinkSeal).r, GetSealColor(threadlinkSeal).g, GetSealColor(threadlinkSeal).b, 0.52f),
                    0.68f),
                0.2f,
                2.1f);
            return true;
        }

        public string GetThreadlinkWeaveBeatText(int beat)
        {
            if (!IsThreadlinkActive)
            {
                return string.Empty;
            }

            var responders = GetSelectedThreadlinkRallyResponders();
            if (responders.Count == 0)
            {
                return "Unassigned";
            }

            var index = Mathf.Clamp(beat - 1, 0, responders.Count - 1);
            var responder = responders[index];
            return $"{responder.DisplayName} · {responder.WeaveLabel}";
        }

        private void ResetThreadlinkWeaveSelection()
        {
            threadlinkBeatOneResponderId = null;
            threadlinkBeatTwoResponderId = null;
            var responders = GetThreadlinkRallyResponders();
            EnsureThreadlinkWeaveSelection(responders);
        }

        private List<ThreadlinkRallyResponder> GetSelectedThreadlinkRallyResponders()
        {
            var responders = GetThreadlinkRallyResponders();
            EnsureThreadlinkWeaveSelection(responders);
            threadlinkSelectedResponderBuffer.Clear();
            if (responders.Count == 0)
            {
                return threadlinkSelectedResponderBuffer;
            }

            var first =
                FindThreadlinkResponder(
                    responders,
                    threadlinkBeatOneResponderId) ??
                responders[0];
            var second = FindThreadlinkResponder(responders, threadlinkBeatTwoResponderId);
            if (!second.HasValue ||
                (responders.Count > 1 &&
                 second.Value.Identity == first.Identity))
            {
                second = GetNextThreadlinkResponder(responders, first.Identity, first.Identity);
            }

            threadlinkSelectedResponderBuffer.Add(first);
            threadlinkSelectedResponderBuffer.Add(second ?? first);
            return threadlinkSelectedResponderBuffer;
        }

        private void EnsureThreadlinkWeaveSelection(List<ThreadlinkRallyResponder> responders)
        {
            if (responders.Count == 0)
            {
                threadlinkBeatOneResponderId = null;
                threadlinkBeatTwoResponderId = null;
                return;
            }

            var first = FindThreadlinkResponder(responders, threadlinkBeatOneResponderId);
            if (!first.HasValue)
            {
                first = responders[0];
                threadlinkBeatOneResponderId = first.Value.Identity;
            }

            var second = FindThreadlinkResponder(responders, threadlinkBeatTwoResponderId);
            if (!second.HasValue ||
                (responders.Count > 1 &&
                 second.Value.Identity == first.Value.Identity))
            {
                second = GetNextThreadlinkResponder(
                    responders,
                    first.Value.Identity,
                    first.Value.Identity);
                threadlinkBeatTwoResponderId = second.HasValue
                    ? second.Value.Identity
                    : first.Value.Identity;
            }
        }

        private static ThreadlinkRallyResponder? FindThreadlinkResponder(
            List<ThreadlinkRallyResponder> responders,
            string identity)
        {
            if (string.IsNullOrEmpty(identity))
            {
                return null;
            }

            foreach (var responder in responders)
            {
                if (responder.Identity == identity)
                {
                    return responder;
                }
            }

            return null;
        }

        private static ThreadlinkRallyResponder? GetNextThreadlinkResponder(
            List<ThreadlinkRallyResponder> responders,
            string currentId,
            string blockedId)
        {
            if (responders.Count == 0)
            {
                return null;
            }

            var currentIndex = 0;
            for (var index = 0; index < responders.Count; index++)
            {
                if (responders[index].Identity == currentId)
                {
                    currentIndex = index;
                    break;
                }
            }

            for (var offset = 1; offset <= responders.Count; offset++)
            {
                var candidate = responders[(currentIndex + offset) % responders.Count];
                if (responders.Count == 1 || candidate.Identity != blockedId)
                {
                    return candidate;
                }
            }

            return responders[0];
        }

        private bool HasLiveThreadlinkTarget()
        {
            return threadlinkEnemy != null && !threadlinkEnemy.IsDefeated ||
                   threadlinkBoss != null && !threadlinkBoss.IsDefeated;
        }

        private bool IsThreadlinkTargetReachable()
        {
            if (PlayerTransform == null)
            {
                return false;
            }

            if (threadlinkEnemy != null && !threadlinkEnemy.IsDefeated)
            {
                return Vector3.Distance(PlayerTransform.position, threadlinkEnemy.transform.position) <= 25f &&
                       HasExpeditionLineOfSight(threadlinkEnemy.transform);
            }

            return threadlinkBoss != null &&
                   !threadlinkBoss.IsDefeated &&
                   Vector3.Distance(PlayerTransform.position, threadlinkBoss.transform.position) <= 30f &&
                   HasExpeditionLineOfSight(threadlinkBoss.transform);
        }

        public void OnExpeditionAllyStrike(Enemy enemy, string source, Color color)
        {
            OnExpeditionAllyStrike(enemy, null, source, color);
        }

        public void OnExpeditionAllyStrike(Enemy enemy, Transform ally, string source, Color color)
        {
            if (enemy == null)
            {
                return;
            }

            PlayExpeditionAllyStrikeFeedback(ally, enemy.transform, color, source);
            if (enemy.IsDefeated)
            {
                return;
            }

            if (IsMandateStrike(source))
            {
                RegisterOvertone(enemy, OvertonePitch.Bind, source);
            }

            if (enemy != threadlinkEnemy)
            {
                return;
            }

            RegisterThreadlinkContribution(enemy.transform.position, source, color);
        }

        public void OnExpeditionAllyStrike(DungeonBoss boss, string source, Color color)
        {
            OnExpeditionAllyStrike(boss, null, source, color);
        }

        public void OnExpeditionAllyStrike(DungeonBoss boss, Transform ally, string source, Color color)
        {
            if (boss == null)
            {
                return;
            }

            PlayExpeditionAllyStrikeFeedback(ally, boss.transform, color, source);
            if (boss.IsDefeated)
            {
                return;
            }

            if (IsMandateStrike(source))
            {
                RegisterOvertone(boss, OvertonePitch.Bind, source);
            }

            if (boss != threadlinkBoss)
            {
                return;
            }

            RegisterThreadlinkContribution(boss.transform.position, source, color);
        }

        public void OnThreadlinkRallyResponse(Enemy enemy, Transform ally, string source, Color color)
        {
            if (enemy == null)
            {
                return;
            }

            PlayExpeditionAllyStrikeFeedback(ally, enemy.transform, color, source);
            if (enemy.IsDefeated || enemy != threadlinkEnemy)
            {
                return;
            }

            RegisterThreadlinkContribution(enemy.transform.position, source, color, true);
        }

        public void OnThreadlinkRallyResponse(DungeonBoss boss, Transform ally, string source, Color color)
        {
            if (boss == null)
            {
                return;
            }

            PlayExpeditionAllyStrikeFeedback(ally, boss.transform, color, source);
            if (boss.IsDefeated || boss != threadlinkBoss)
            {
                return;
            }

            RegisterThreadlinkContribution(boss.transform.position, source, color, true);
        }

        private static bool IsMandateStrike(string source)
        {
            return !string.IsNullOrEmpty(source) &&
                   (source.Contains("tactical") ||
                    source.Contains("focus mark") ||
                    source.Contains("breach"));
        }

        public string GetExpeditionDirectiveText()
        {
            return expeditionDirective switch
            {
                ExpeditionDirective.Focus => "Order Focus",
                ExpeditionDirective.Bulwark => "Order Bulwark",
                _ => "Order Trail"
            };
        }

        public string GetThreadlinkStatusText()
        {
            if (!IsThreadlinkActive)
            {
                return "Link dormant · G Seal · V Rally";
            }

            if (threadlinkRallyResponders.Count > 0)
            {
                return $"{GetThreadlinkSealName()} {Mathf.CeilToInt(threadlinkEndsAt - Time.time)}s · Weave {Mathf.Min(2, threadlinkRallyBeatIndex + 1)}/2";
            }

            return $"{GetThreadlinkSealName()} {Mathf.CeilToInt(threadlinkEndsAt - Time.time)}s · {threadlinkContributions}/2 · T/Q steer · V Rally";
        }

        private void RegisterThreadlinkContribution(
            Vector3 position,
            string source,
            Color color,
            bool bypassCooldown = false)
        {
            if (!IsThreadlinkActive || (!bypassCooldown && Time.time < nextThreadlinkContributionAt))
            {
                return;
            }

            nextThreadlinkContributionAt = Time.time + (bypassCooldown ? 0.08f : 0.34f);
            threadlinkContributions++;
            TimedPulse.Spawn(
                PrimitiveType.Sphere,
                "Threadlink contribution",
                position + Vector3.up * 1.05f,
                Vector3.one * 0.18f,
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(color.r, color.g, color.b, 0.58f), 0.7f),
                0.22f,
                2.4f + threadlinkContributions * 0.7f);

            if (threadlinkContributions >= 2)
            {
                ResolveThreadlink(position, source);
            }
        }

        private void ResolveThreadlink(Vector3 position, string source)
        {
            var seal = threadlinkSeal;
            var color = GetSealColor(seal);
            var primaryDamage = 0;
            var pulseScale = 5.2f;

            switch (seal)
            {
                case ThreadlinkSeal.Sever:
                    primaryDamage = Mathf.Max(
                        1,
                        Mathf.RoundToInt((20 + Level * 3) * ThreadlinkPotencyMultiplier));
                    ResolveSever(position, primaryDamage);
                    break;
                case ThreadlinkSeal.Latch:
                    primaryDamage = Mathf.Max(
                        1,
                        Mathf.RoundToInt((15 + Level * 2) * ThreadlinkPotencyMultiplier));
                    ResolveLatch(position, primaryDamage);
                    pulseScale = 6.8f;
                    break;
                case ThreadlinkSeal.Aegis:
                    primaryDamage = Mathf.Max(
                        1,
                        Mathf.RoundToInt((12 + Level * 2) * ThreadlinkPotencyMultiplier));
                    ResolveAegis(position, primaryDamage);
                    pulseScale = 7.4f;
                    break;
            }

            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                $"{GetThreadlinkSealName()} threadburst",
                position + Vector3.up * 0.08f,
                new Vector3(0.36f, 0.035f, 0.36f),
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(color.r, color.g, color.b, 0.7f), 0.72f),
                0.3f,
                pulseScale);
            cameraRig?.AddImpulse(0.12f, 0.14f);
            Notify($"{GetThreadlinkSealName()} threadburst answered by {source}.");
            ClearThreadlink();
        }

        private void ResolveSever(Vector3 position, int damage)
        {
            if (threadlinkEnemy != null && !threadlinkEnemy.IsDefeated)
            {
                threadlinkEnemy.TakeDamage(
                    damage,
                    "Sever threadburst",
                    CombatImpactKind.Threadlink);
                threadlinkEnemy.ApplyStagger(1.15f);
            }

            if (threadlinkBoss != null && !threadlinkBoss.IsDefeated)
            {
                threadlinkBoss.TakeDamage(
                    damage,
                    "Sever threadburst",
                    CombatImpactKind.Threadlink);
                threadlinkBoss.ApplyStagger(0.56f);
            }
        }

        private void ResolveLatch(Vector3 position, int damage)
        {
            threadlinkAffectedEnemies.Clear();
            var hitCount = Physics.OverlapSphereNonAlloc(
                position,
                4.7f,
                threadlinkAreaHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = threadlinkAreaHitBuffer[hitIndex];
                if (hit == null)
                {
                    continue;
                }

                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDefeated || !threadlinkAffectedEnemies.Add(enemy))
                {
                    continue;
                }

                enemy.TakeDamage(
                    enemy == threadlinkEnemy
                        ? damage
                        : Mathf.RoundToInt(damage * 0.62f),
                    "Latch threadburst",
                    CombatImpactKind.Threadlink);
                enemy.ApplyExpose(6.2f);
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                threadlinkAreaHitBuffer[hitIndex] = null;
            }

            threadlinkAffectedEnemies.Clear();
            if (threadlinkBoss != null && !threadlinkBoss.IsDefeated)
            {
                threadlinkBoss.TakeDamage(
                    damage,
                    "Latch threadburst",
                    CombatImpactKind.Threadlink);
                threadlinkBoss.ApplyExpose(6.2f);
            }
        }

        private void ResolveAegis(Vector3 position, int damage)
        {
            if (threadlinkEnemy != null && !threadlinkEnemy.IsDefeated)
            {
                threadlinkEnemy.TakeDamage(
                    damage,
                    "Aegis threadburst",
                    CombatImpactKind.Threadlink);
                threadlinkEnemy.ApplyStagger(0.45f);
            }

            if (threadlinkBoss != null && !threadlinkBoss.IsDefeated)
            {
                threadlinkBoss.TakeDamage(
                    damage,
                    "Aegis threadburst",
                    CombatImpactKind.Threadlink);
                threadlinkBoss.ApplyStagger(0.22f);
            }

            HealPlayer(10 + Level);
            RestoreStamina(30f);
            aegisWardEndsAt = Time.time + 7.5f;
        }

        private int CountActiveExpeditionAllies()
        {
            var count = CountAvailableActivePactActors();
            foreach (var companion in party)
            {
                if (companion != null && companion.IsRecruited && !companion.IsReserve)
                {
                    count++;
                }
            }

            return count;
        }

        private void ClearThreadlink()
        {
            if (threadlinkEnemy != null)
            {
                ThreadlinkMark.Clear(threadlinkEnemy.transform);
            }

            if (threadlinkBoss != null)
            {
                ThreadlinkMark.Clear(threadlinkBoss.transform);
            }

            threadlinkEnemy = null;
            threadlinkBoss = null;
            threadlinkSeal = ThreadlinkSeal.None;
            threadlinkEndsAt = 0f;
            threadlinkContributions = 0;
            threadlinkRallyResponders.Clear();
            threadlinkRallyBeatIndex = 0;
            nextThreadlinkRallyBeatAt = 0f;
            threadlinkBeatOneResponderId = null;
            threadlinkBeatTwoResponderId = null;
        }

        private string GetThreadlinkSealName()
        {
            return threadlinkSeal switch
            {
                ThreadlinkSeal.Sever => "Sever",
                ThreadlinkSeal.Latch => "Latch",
                ThreadlinkSeal.Aegis => "Aegis",
                _ => "Threadlink"
            };
        }

        private Color GetDirectiveColor()
        {
            return expeditionDirective switch
            {
                ExpeditionDirective.Focus => new Color(0.98f, 0.58f, 0.24f),
                ExpeditionDirective.Bulwark => new Color(0.37f, 0.78f, 0.94f),
                _ => new Color(0.43f, 0.9f, 0.66f)
            };
        }

        private string GetExpeditionDirectiveDescription()
        {
            return expeditionDirective switch
            {
                ExpeditionDirective.Focus => "Focus order: allies prioritize your Threadglass target.",
                ExpeditionDirective.Bulwark => "Bulwark order: allies hold close and reinforce your guard.",
                _ => "Trail order: allies cover nearby threats while moving with you."
            };
        }

        private static Color GetSealColor(ThreadlinkSeal seal)
        {
            return seal switch
            {
                ThreadlinkSeal.Sever => new Color(0.96f, 0.38f, 0.24f),
                ThreadlinkSeal.Latch => new Color(0.48f, 0.78f, 0.98f),
                ThreadlinkSeal.Aegis => new Color(0.42f, 0.92f, 0.72f),
                _ => new Color(0.82f, 0.75f, 0.48f)
            };
        }

        private void WriteExpeditionCombatSave(TetherfallSaveData data)
        {
            data.expeditionDirective = (int)expeditionDirective;
        }

        private void RestoreExpeditionCombatState(TetherfallSaveData data)
        {
            expeditionDirective = data.expeditionDirective >= (int)ExpeditionDirective.Trail &&
                                  data.expeditionDirective <= (int)ExpeditionDirective.Bulwark
                ? (ExpeditionDirective)data.expeditionDirective
                : ExpeditionDirective.Trail;
        }

        private readonly struct ThreadlinkRallyResponder
        {
            private readonly PactCreature pact;
            private readonly CapturedPactActor capturedPact;
            private readonly HumanCompanion companion;

            public ThreadlinkRallyResponder(PactCreature source)
            {
                pact = source;
                capturedPact = null;
                companion = null;
            }

            public ThreadlinkRallyResponder(CapturedPactActor source)
            {
                pact = null;
                capturedPact = source;
                companion = null;
            }

            public ThreadlinkRallyResponder(HumanCompanion source)
            {
                pact = null;
                capturedPact = null;
                companion = source;
            }

            public string DisplayName => pact != null ? pact.CreatureName :
                capturedPact != null ? capturedPact.DisplayName :
                companion != null ? companion.CompanionName : "Expedition ally";
            public string Identity => pact != null
                ? !string.IsNullOrEmpty(pact.RosterInstanceId)
                    ? pact.RosterInstanceId
                    : pact.PactId
                : capturedPact != null
                    ? capturedPact.InstanceId
                    : companion != null
                        ? companion.CompanionName
                        : string.Empty;
            public string WeaveLabel => pact != null ? pact.TacticalAbilityShortLabel :
                capturedPact != null ? capturedPact.TacticalAbilityShortLabel :
                companion != null ? companion.TacticalAbilityShortLabel : "Weave";
            public Transform Origin => pact != null ? pact.transform :
                capturedPact != null ? capturedPact.transform :
                companion != null ? companion.transform : null;

            public bool TryRespond(TetherfallGame game, int beat)
            {
                if (game.threadlinkEnemy != null && !game.threadlinkEnemy.IsDefeated)
                {
                    return pact != null
                        ? pact.TriggerThreadlinkResponse(game.threadlinkEnemy, beat)
                        : capturedPact != null
                            ? capturedPact.TriggerThreadlinkResponse(
                                game.threadlinkEnemy,
                                beat)
                            : companion != null &&
                              companion.TriggerThreadlinkResponse(
                                  game.threadlinkEnemy,
                                  beat);
                }

                if (game.threadlinkBoss != null && !game.threadlinkBoss.IsDefeated)
                {
                    return pact != null
                        ? pact.TriggerThreadlinkResponse(game.threadlinkBoss, beat)
                        : capturedPact != null
                            ? capturedPact.TriggerThreadlinkResponse(
                                game.threadlinkBoss,
                                beat)
                            : companion != null &&
                              companion.TriggerThreadlinkResponse(
                                  game.threadlinkBoss,
                                  beat);
                }

                return false;
            }
        }
    }

    public sealed class ThreadlinkMark : MonoBehaviour
    {
        private TetherfallGame game;
        private Transform visualRoot;
        private float expiresAt;

        public static void Attach(TetherfallGame game, Transform target, ThreadlinkSeal seal, float newExpiresAt)
        {
            if (game == null || target == null)
            {
                return;
            }

            var mark = target.GetComponent<ThreadlinkMark>();
            if (mark == null)
            {
                mark = target.gameObject.AddComponent<ThreadlinkMark>();
            }

            mark.Initialize(game, seal, newExpiresAt);
        }

        public static void Clear(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var mark = target.GetComponent<ThreadlinkMark>();
            if (mark != null)
            {
                Object.Destroy(mark);
            }
        }

        private void Initialize(TetherfallGame owner, ThreadlinkSeal seal, float newExpiresAt)
        {
            game = owner;
            expiresAt = newExpiresAt;
            var color = seal switch
            {
                ThreadlinkSeal.Sever => new Color(0.96f, 0.38f, 0.24f),
                ThreadlinkSeal.Latch => new Color(0.48f, 0.78f, 0.98f),
                _ => new Color(0.42f, 0.92f, 0.72f)
            };

            if (visualRoot == null)
            {
                visualRoot = new GameObject("Threadlink seal visual").transform;
                visualRoot.SetParent(transform, false);

                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Threadlink seal ring";
                ring.transform.SetParent(visualRoot, false);
                ring.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                ring.transform.localScale = new Vector3(0.96f, 0.026f, 0.96f);
                Object.Destroy(ring.GetComponent<Collider>());

                for (var index = 0; index < 3; index++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = "Threadlink seal shard";
                    shard.transform.SetParent(visualRoot, false);
                    var angle = index * 120f * Mathf.Deg2Rad;
                    shard.transform.localPosition = new Vector3(
                        Mathf.Cos(angle) * 0.68f,
                        1.45f,
                        Mathf.Sin(angle) * 0.68f);
                    shard.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
                    shard.transform.localRotation = Quaternion.Euler(18f, index * 120f, 25f);
                    Object.Destroy(shard.GetComponent<Collider>());
                }
            }

            var renderers = visualRoot.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = game.CreateGlowMaterial(color, 1.5f, 0.64f);
            }
        }

        private void Update()
        {
            if (game == null || Time.time >= expiresAt || !game.IsThreadlinkActive)
            {
                Object.Destroy(this);
                return;
            }

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, Time.time * 125f, 0f);
                var pulse = 0.94f + Mathf.Sin(Time.time * 6.2f) * 0.12f;
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
    }
}
