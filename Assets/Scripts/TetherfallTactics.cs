using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private const float TacticalCommandStaminaCost = 22f;
        private float nextTacticalCommandAt;

        public float TacticalCommandCooldown => Mathf.Max(0f, nextTacticalCommandAt - Time.time);

        public void TryTacticalCommand()
        {
            var plan = BuildTacticalCommandPlan();
            if (!plan.CanIssue)
            {
                if (IsThreadlinkActive &&
                    (!HasLiveThreadlinkTarget() || !IsThreadlinkTargetReachable()))
                {
                    TryStartThreadlinkRally();
                    return;
                }

                if (!string.IsNullOrEmpty(plan.Status))
                {
                    Notify(plan.Status);
                }

                return;
            }

            if (plan.IsThreadlinkRally)
            {
                TryStartThreadlinkRally();
                return;
            }

            if (!TrySpendStamina(TacticalCommandStaminaCost, true))
            {
                return;
            }

            if (CurrentExpeditionDirective == ExpeditionDirective.Focus)
            {
                var focusedEnemy = GetExpeditionEnemy(16f);
                if (focusedEnemy != null)
                {
                    focusedEnemy.ApplyExpose(3.6f);
                    TimedPulse.Spawn(
                        PrimitiveType.Sphere,
                        "Focus order mark",
                        focusedEnemy.transform.position + Vector3.up * 1.1f,
                        Vector3.one * 0.2f,
                        RuntimeEffectMaterialCache.GetTransparent(this, new Color(0.98f, 0.58f, 0.24f, 0.64f), 0.7f),
                        0.24f,
                        2.8f);
                }
                else
                {
                    var focusedBoss = GetExpeditionBoss(16f);
                    if (focusedBoss != null)
                    {
                        focusedBoss.ApplyExpose(3.6f);
                        TimedPulse.Spawn(
                            PrimitiveType.Sphere,
                            "Focus order mark",
                            focusedBoss.transform.position + Vector3.up * 1.5f,
                            Vector3.one * 0.2f,
                            RuntimeEffectMaterialCache.GetTransparent(
                                this,
                                new Color(0.98f, 0.58f, 0.24f, 0.64f),
                                0.7f),
                            0.24f,
                            3.4f);
                    }
                }
            }

            var commandsIssued = 0;
            for (var slot = 0; slot < ActiveCombatPactCount; slot++)
            {
                if (!TryGetActivePactActor(
                        slot,
                        out var record,
                        out var storyActor,
                        out var capturedActor))
                {
                    continue;
                }

                var commandAnswered =
                    storyActor != null
                        ? storyActor.TriggerTacticalCommand(
                            string.Equals(
                                record.instanceId,
                                PactCommandFocusInstanceId,
                                System.StringComparison.Ordinal))
                        : capturedActor != null &&
                          capturedActor.TriggerTacticalCommand();
                if (commandAnswered)
                {
                    commandsIssued++;
                }
            }

            foreach (var companion in party)
            {
                if (companion != null && !companion.IsReserve && companion.TriggerTacticalCommand())
                {
                    commandsIssued++;
                }
            }

            if (commandsIssued == 0)
            {
                RestoreStamina(TacticalCommandStaminaCost);
                Notify("The field order lost its opening. Stamina restored.");
                return;
            }

            var mandateEnemy = GetExpeditionEnemy(16f);
            if (mandateEnemy != null)
            {
                RegisterOvertone(mandateEnemy, OvertonePitch.Bind, "Expedition mandate");
            }
            else
            {
                var mandateBoss = GetExpeditionBoss(16f);
                if (mandateBoss != null)
                {
                    RegisterOvertone(mandateBoss, OvertonePitch.Bind, "Expedition mandate");
                }
            }

            nextTacticalCommandAt = Time.time + 13f;
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Expedition command pulse",
                PlayerTransform.position + Vector3.up * 0.06f,
                new Vector3(0.38f, 0.035f, 0.38f),
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(0.78f, 0.57f, 0.3f, 0.56f), 0.68f),
                0.28f,
                5.1f);
            Notify($"{GetExpeditionDirectiveText()} answered. Your expedition moves as one.");
        }

        public string GetTacticalCommandStatus()
        {
            return BuildTacticalCommandPlan().Status;
        }

        internal bool HasTacticalAreaHostile(Vector3 center, float enemyRange, float bossRange)
        {
            var enemy = Enemy.FindClosest(center, enemyRange);
            if (enemy != null && !enemy.IsDefeated)
            {
                return true;
            }

            var boss = ActiveDungeonBoss;
            return boss != null &&
                   !boss.IsDefeated &&
                   Vector3.Distance(center, boss.transform.position) <= bossRange;
        }

        internal bool HasTacticalFocusHostile(float range)
        {
            return GetExpeditionEnemy(range) != null || GetExpeditionBoss(range) != null;
        }

        private TacticalCommandPlan BuildTacticalCommandPlan()
        {
            if (GameplayPaused)
            {
                return TacticalCommandPlan.Blocked(string.Empty);
            }

            if (TacticalCommandCooldown > 0f)
            {
                return TacticalCommandPlan.Blocked(
                    $"V: Order returns {Mathf.CeilToInt(TacticalCommandCooldown)}s");
            }

            if (ActiveCombatPactCount == 0 && party.Count == 0)
            {
                return TacticalCommandPlan.Blocked("V: Recruit a field ally");
            }

            if (IsThreadlinkActive)
            {
                if (!HasLiveThreadlinkTarget() || !IsThreadlinkTargetReachable())
                {
                    return TacticalCommandPlan.Blocked("V: Threadlink target lost");
                }

                var responders = GetThreadlinkRallyResponders();
                if (responders.Count == 0)
                {
                    return TacticalCommandPlan.Blocked("V: No ally for Rally");
                }

                if (Stamina + 0.001f < TacticalCommandStaminaCost)
                {
                    return TacticalCommandPlan.Blocked("V: Need 22 stamina for Rally");
                }

                var firstName = responders[0].DisplayName;
                var secondName = responders.Count > 1 ? responders[1].DisplayName : firstName;
                return TacticalCommandPlan.Ready(
                    true,
                    $"V: Rally {firstName} → {secondName}");
            }

            if (Stamina + 0.001f < TacticalCommandStaminaCost)
            {
                return TacticalCommandPlan.Blocked("V: Need 22 stamina");
            }

            var actionSummary = string.Empty;
            var responderCount = 0;
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

                var canAnswer =
                    storyActor != null
                        ? storyActor.CanAnswerTacticalCommand()
                        : capturedActor != null &&
                          capturedActor.CanAnswerTacticalCommand();
                if (!canAnswer)
                {
                    continue;
                }

                AddTacticalAction(
                    ref actionSummary,
                    storyActor != null
                        ? storyActor.TacticalAbilityShortLabel
                        : capturedActor.TacticalAbilityShortLabel,
                    responderCount);
                responderCount++;
            }

            foreach (var companion in party)
            {
                if (companion != null &&
                    !companion.IsReserve &&
                    companion.CanAnswerTacticalCommand())
                {
                    AddTacticalAction(ref actionSummary, companion.TacticalAbilityShortLabel, responderCount);
                    responderCount++;
                }
            }

            if (responderCount == 0)
            {
                return TacticalCommandPlan.Blocked("V: Need a hostile in range");
            }

            return TacticalCommandPlan.Ready(false, $"V: {actionSummary} ready");
        }

        private static void AddTacticalAction(ref string summary, string action, int actionIndex)
        {
            if (actionIndex == 0)
            {
                summary = action;
            }
            else if (actionIndex == 1)
            {
                summary += $" + {action}";
            }
            else if (actionIndex == 2)
            {
                summary += " + more";
            }
        }

        private readonly struct TacticalCommandPlan
        {
            private TacticalCommandPlan(bool canIssue, bool isThreadlinkRally, string status)
            {
                CanIssue = canIssue;
                IsThreadlinkRally = isThreadlinkRally;
                Status = status;
            }

            public bool CanIssue { get; }
            public bool IsThreadlinkRally { get; }
            public string Status { get; }

            public static TacticalCommandPlan Ready(bool isThreadlinkRally, string status)
            {
                return new TacticalCommandPlan(true, isThreadlinkRally, status);
            }

            public static TacticalCommandPlan Blocked(string status)
            {
                return new TacticalCommandPlan(false, false, status);
            }
        }
    }
}
