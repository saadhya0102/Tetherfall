using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum OvertonePitch
    {
        None,
        Cut,
        Line,
        Ring,
        Bind,
        Pulse
    }

    public enum OvertoneOutcome
    {
        None,
        SeveredRoute,
        BrokenGuard,
        GlassSnare,
        AccordStrike,
        MeasuredVolley,
        WardEcho,
        ThreadstepCascade,
        TriadBinding,
        WardAccord,
        ResonanceEcho
    }

    public sealed partial class TetherfallGame
    {
        private const float OvertoneWindowSeconds = 2.8f;
        private const float OvertonePairResolveSeconds = 0.58f;
        private const float OvertoneRefractorySeconds = 1.1f;

        private readonly List<OvertonePitch> activeOvertones = new List<OvertonePitch>();
        private Enemy overtoneEnemy;
        private DungeonBoss overtoneBoss;
        private float overtoneWindowEndsAt;
        private float overtoneResolveAt;
        private float overtoneRefractoryUntil;

        public void RegisterOvertone(Enemy target, OvertonePitch pitch, string source)
        {
            if (target == null || target.IsDefeated)
            {
                return;
            }

            RegisterOvertoneInternal(target, target.transform, pitch, source);
        }

        public void RegisterOvertone(DungeonBoss target, OvertonePitch pitch, string source)
        {
            if (target == null || target.IsDefeated)
            {
                return;
            }

            RegisterOvertoneInternal(target, target.transform, pitch, source);
        }

        internal void RegisterPactAllyOvertone(
            Enemy target,
            PactResonanceAffinity affinity,
            string source)
        {
            RegisterOvertone(target, GetPactOvertonePitch(affinity), source);
        }

        internal void RegisterPactAllyOvertone(
            DungeonBoss target,
            PactResonanceAffinity affinity,
            string source)
        {
            RegisterOvertone(target, GetPactOvertonePitch(affinity), source);
        }

        private void RegisterOvertoneInternal(
            IExpeditionDamageable target,
            Transform targetTransform,
            OvertonePitch pitch,
            string source)
        {
            if (pitch == OvertonePitch.None ||
                target == null ||
                targetTransform == null ||
                CombatTarget != targetTransform ||
                Time.time < overtoneRefractoryUntil)
            {
                return;
            }

            var targetChanged = target != GetOvertoneTarget();
            if (targetChanged)
            {
                ClearOvertoneLattice();
                overtoneEnemy = target as Enemy;
                overtoneBoss = target as DungeonBoss;
            }

            if (!activeOvertones.Contains(pitch))
            {
                activeOvertones.Add(pitch);
            }

            overtoneWindowEndsAt = Time.time + OvertoneWindowSeconds;
            if (activeOvertones.Count >= 2)
            {
                overtoneResolveAt = Time.time + OvertonePairResolveSeconds;
            }

            CreateOvertoneCue(targetTransform.position, GetPitchColor(pitch), activeOvertones.Count);
            if (activeOvertones.Count >= 3)
            {
                ResolveOvertoneLattice();
            }
        }

        private void UpdateOvertoneLattice()
        {
            if (GameplayPaused)
            {
                overtoneWindowEndsAt += Time.deltaTime;
                overtoneResolveAt += Time.deltaTime;
                overtoneRefractoryUntil += Time.deltaTime;
                return;
            }

            var target = GetOvertoneTarget();
            if (target == null || target.IsDefeated || !IsOvertoneTargetFocused())
            {
                ClearOvertoneLattice();
                return;
            }

            if (activeOvertones.Count == 0)
            {
                return;
            }

            if (Time.time >= overtoneWindowEndsAt)
            {
                ClearOvertoneLattice();
                return;
            }

            if (activeOvertones.Count >= 2 && Time.time >= overtoneResolveAt)
            {
                ResolveOvertoneLattice();
            }
        }

        private void ResolveOvertoneLattice()
        {
            var target = GetOvertoneTarget();
            if (target == null || target.IsDefeated || activeOvertones.Count < 2)
            {
                ClearOvertoneLattice();
                return;
            }

            var outcome = GetOvertoneOutcome();
            ApplyOvertoneOutcome(target, outcome);
            activeOvertones.Clear();
            overtoneWindowEndsAt = 0f;
            overtoneResolveAt = 0f;
            overtoneRefractoryUntil = Time.time + OvertoneRefractorySeconds;
        }

        private OvertoneOutcome GetOvertoneOutcome()
        {
            if (HasOvertones(OvertonePitch.Cut, OvertonePitch.Line, OvertonePitch.Pulse))
            {
                return OvertoneOutcome.ThreadstepCascade;
            }

            if (HasOvertones(OvertonePitch.Line, OvertonePitch.Bind, OvertonePitch.Pulse))
            {
                return OvertoneOutcome.TriadBinding;
            }

            if (HasOvertones(OvertonePitch.Ring, OvertonePitch.Bind, OvertonePitch.Pulse))
            {
                return OvertoneOutcome.WardAccord;
            }

            if (HasOvertones(OvertonePitch.Cut, OvertonePitch.Line))
            {
                return OvertoneOutcome.SeveredRoute;
            }

            if (HasOvertones(OvertonePitch.Cut, OvertonePitch.Ring))
            {
                return OvertoneOutcome.BrokenGuard;
            }

            if (HasOvertones(OvertonePitch.Line, OvertonePitch.Ring))
            {
                return OvertoneOutcome.GlassSnare;
            }

            if (HasOvertones(OvertonePitch.Bind, OvertonePitch.Cut))
            {
                return OvertoneOutcome.AccordStrike;
            }

            if (HasOvertones(OvertonePitch.Bind, OvertonePitch.Line))
            {
                return OvertoneOutcome.MeasuredVolley;
            }

            if (HasOvertones(OvertonePitch.Bind, OvertonePitch.Ring))
            {
                return OvertoneOutcome.WardEcho;
            }

            return activeOvertones.Contains(OvertonePitch.Pulse)
                ? OvertoneOutcome.ResonanceEcho
                : OvertoneOutcome.None;
        }

        private void ApplyOvertoneOutcome(IExpeditionDamageable target, OvertoneOutcome outcome)
        {
            var damage = 14;
            var staggerSeconds = 0.22f;
            var exposeSeconds = 0f;
            var restoreStamina = 0f;
            var heal = 0;
            var color = new Color(0.54f, 0.9f, 0.78f);
            var displayName = "Resonance echo";

            switch (outcome)
            {
                case OvertoneOutcome.SeveredRoute:
                    damage = 20;
                    staggerSeconds = 0.35f;
                    color = new Color(0.96f, 0.64f, 0.27f);
                    displayName = "Severed Route";
                    break;
                case OvertoneOutcome.BrokenGuard:
                    damage = 16;
                    staggerSeconds = 0.72f;
                    color = new Color(0.92f, 0.48f, 0.29f);
                    displayName = "Broken Guard";
                    break;
                case OvertoneOutcome.GlassSnare:
                    damage = 12;
                    staggerSeconds = 0.18f;
                    exposeSeconds = 3.2f;
                    color = new Color(0.41f, 0.78f, 0.98f);
                    displayName = "Glass Snare";
                    break;
                case OvertoneOutcome.AccordStrike:
                    damage = 18;
                    staggerSeconds = 0.3f;
                    restoreStamina = 14f;
                    color = new Color(0.95f, 0.73f, 0.3f);
                    displayName = "Accord Strike";
                    break;
                case OvertoneOutcome.MeasuredVolley:
                    damage = 18;
                    staggerSeconds = 0.24f;
                    exposeSeconds = 2.2f;
                    color = new Color(0.35f, 0.9f, 0.84f);
                    displayName = "Measured Volley";
                    break;
                case OvertoneOutcome.WardEcho:
                    damage = 10;
                    staggerSeconds = 0.28f;
                    heal = 10;
                    color = new Color(0.54f, 0.7f, 1f);
                    displayName = "Ward Echo";
                    break;
                case OvertoneOutcome.ThreadstepCascade:
                    damage = 34;
                    staggerSeconds = 0.82f;
                    color = new Color(1f, 0.5f, 0.2f);
                    displayName = "Threadstep Cascade";
                    break;
                case OvertoneOutcome.TriadBinding:
                    damage = 28;
                    staggerSeconds = 0.46f;
                    exposeSeconds = 4.2f;
                    color = new Color(0.42f, 0.92f, 0.87f);
                    displayName = "Triad Binding";
                    break;
                case OvertoneOutcome.WardAccord:
                    damage = 20;
                    staggerSeconds = 0.56f;
                    restoreStamina = 10f;
                    heal = 14;
                    color = new Color(0.61f, 0.71f, 1f);
                    displayName = "Ward Accord";
                    break;
                case OvertoneOutcome.ResonanceEcho:
                    damage = 16;
                    staggerSeconds = 0.32f;
                    color = new Color(0.78f, 0.58f, 0.95f);
                    displayName = "Resonance Echo";
                    break;
                default:
                    return;
            }

            damage = Mathf.Max(1, Mathf.RoundToInt(damage * OvertonePotencyMultiplier));
            if (target is Enemy enemy)
            {
                enemy.TakeDamage(
                    damage,
                    displayName,
                    CombatImpactKind.Overtone);
                if (staggerSeconds > 0f)
                {
                    enemy.ApplyStagger(staggerSeconds);
                }

                if (exposeSeconds > 0f)
                {
                    enemy.ApplyExpose(exposeSeconds);
                }
            }
            else if (target is DungeonBoss boss)
            {
                boss.TakeDamage(
                    damage,
                    displayName,
                    CombatImpactKind.Overtone);
                if (staggerSeconds > 0f)
                {
                    boss.ApplyStagger(staggerSeconds * 0.58f);
                }

                if (exposeSeconds > 0f)
                {
                    boss.ApplyExpose(exposeSeconds);
                }
            }

            if (restoreStamina > 0f)
            {
                RestoreStamina(restoreStamina);
            }

            if (heal > 0)
            {
                HealPlayer(heal);
            }

            var position = CombatTarget != null ? CombatTarget.position : PlayerTransform.position;
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                displayName,
                position + Vector3.up * 0.08f,
                new Vector3(0.28f, 0.035f, 0.28f),
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(color.r, color.g, color.b, 0.68f), 0.72f),
                0.34f,
                outcome == OvertoneOutcome.ThreadstepCascade ? 7.2f : 5.1f);
            Notify($"{displayName} resolves on the Threadglass focus.");
        }

        private bool HasOvertones(params OvertonePitch[] required)
        {
            if (activeOvertones.Count < required.Length)
            {
                return false;
            }

            foreach (var pitch in required)
            {
                if (!activeOvertones.Contains(pitch))
                {
                    return false;
                }
            }

            return true;
        }

        private IExpeditionDamageable GetOvertoneTarget()
        {
            if (overtoneEnemy != null && !overtoneEnemy.IsDefeated)
            {
                return overtoneEnemy;
            }

            return overtoneBoss != null && !overtoneBoss.IsDefeated ? overtoneBoss : null;
        }

        private bool IsOvertoneTargetFocused()
        {
            var target = GetOvertoneTarget();
            if (target is Enemy enemy)
            {
                return CombatTarget == enemy.transform;
            }

            if (target is DungeonBoss boss)
            {
                return CombatTarget == boss.transform;
            }

            return false;
        }

        private void ClearOvertoneLattice()
        {
            activeOvertones.Clear();
            overtoneEnemy = null;
            overtoneBoss = null;
            overtoneWindowEndsAt = 0f;
            overtoneResolveAt = 0f;
        }

        public string GetOvertoneStatusText()
        {
            if (activeOvertones.Count == 0)
            {
                return "Overtones quiet";
            }

            var pitches = string.Join("·", activeOvertones);
            if (activeOvertones.Count == 1)
            {
                return $"Overtone {pitches} {Mathf.CeilToInt(Mathf.Max(0f, overtoneWindowEndsAt - Time.time))}s";
            }

            return $"Lattice {pitches} {Mathf.Max(0f, overtoneResolveAt - Time.time):0.0}s";
        }

        private void CreateOvertoneCue(Vector3 position, Color color, int count)
        {
            TimedPulse.Spawn(
                PrimitiveType.Sphere,
                "Overtone shard",
                position + Vector3.up * (1.15f + count * 0.18f),
                Vector3.one * 0.13f,
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(color.r, color.g, color.b, 0.62f), 0.7f),
                0.26f,
                2.5f + count * 0.35f);
        }

        private static Color GetPitchColor(OvertonePitch pitch)
        {
            return pitch switch
            {
                OvertonePitch.Cut => new Color(0.96f, 0.58f, 0.28f),
                OvertonePitch.Line => new Color(0.38f, 0.92f, 0.84f),
                OvertonePitch.Ring => new Color(0.54f, 0.68f, 1f),
                OvertonePitch.Bind => new Color(0.95f, 0.76f, 0.31f),
                OvertonePitch.Pulse => new Color(0.78f, 0.54f, 0.96f),
                _ => Color.white
            };
        }

        private static OvertonePitch GetPactOvertonePitch(
            PactResonanceAffinity affinity)
        {
            return affinity switch
            {
                PactResonanceAffinity.Grove => OvertonePitch.Bind,
                PactResonanceAffinity.Gale => OvertonePitch.Line,
                PactResonanceAffinity.Tide => OvertonePitch.Ring,
                PactResonanceAffinity.Gloam => OvertonePitch.Ring,
                PactResonanceAffinity.Ember => OvertonePitch.Cut,
                PactResonanceAffinity.Prism => OvertonePitch.Pulse,
                _ => OvertonePitch.Bind
            };
        }
    }
}
