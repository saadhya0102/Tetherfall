using System;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallCommitCounterVerification
    {
        [MenuItem("Tetherfall/Verify Commit & Counter Combat", false, 2214)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Commit & Counter verification is edit-mode only.");
            }

            VerifyForFullGame();
            Debug.Log(
                "Commit & Counter verification passed: poise, protected windups, " +
                "hitstop intent, boss Expose, relay tokens, target cycling, encounter " +
                "scaling, mounted braid, and nonalloc guards are intact.");
        }

        public static void VerifyForFullGame()
        {
            var actors = ReadSource("Scripts", "TetherfallActors.cs");
            var adventure = ReadSource("Scripts", "TetherfallAdventure.cs");
            var combatSystems = ReadSource("Scripts", "TetherfallCombatSystems.cs");
            var tactics = ReadSource("Scripts", "TetherfallTactics.cs");
            var expedition = ReadSource("Scripts", "TetherfallExpeditionCombat.cs");
            var overtones = ReadSource("Scripts", "TetherfallOvertoneLattice.cs");
            var crossweave = ReadSource("Scripts", "TetherfallCrossweave.cs");
            var accords = ReadSource("Scripts", "TetherfallThreadbindAccords.cs");
            var dungeons = ReadSource("Scripts", "TetherfallDungeons.cs");

            VerifyPoiseRules(actors, adventure, accords);
            VerifyHitstopIntent(combatSystems);
            VerifyBossExpose(adventure, tactics, expedition, overtones, actors);
            VerifyRelayAndMountedBraid(actors, crossweave);
            VerifyTargetCycling(combatSystems);
            VerifyScaling(actors, adventure, dungeons);
            VerifyNonAllocGuards(actors, combatSystems, accords);
        }

        private static void VerifyPoiseRules(
            string actors,
            string adventure,
            string accords)
        {
            var melee = TetherfallCombatQualityRules.GetEnemyPoiseProfile(
                EnemyAttackStyle.Melee,
                false,
                false);
            var heavy = TetherfallCombatQualityRules.GetEnemyPoiseProfile(
                EnemyAttackStyle.Heavy,
                false,
                true);
            var elite = TetherfallCombatQualityRules.GetEnemyPoiseProfile(
                EnemyAttackStyle.Heavy,
                true,
                true);
            Assert(melee.Threshold > 0f && melee.RecoveryPerSecond > 0f,
                "Melee poise must have positive bounded threshold and recovery.");
            Assert(heavy.Threshold > melee.Threshold,
                "Heavy archetypes must resist poise pressure better than melee archetypes.");
            Assert(elite.Threshold > heavy.Threshold && elite.Threshold <= 164f,
                "Elite poise resistance must be stronger but bounded.");

            var light = TetherfallCombatQualityRules.CalculatePoiseContribution(
                18,
                100,
                melee.Threshold,
                CombatImpactKind.Light,
                false,
                false);
            var passiveProtected =
                TetherfallCombatQualityRules.CalculatePoiseContribution(
                    18,
                    100,
                    melee.Threshold,
                    CombatImpactKind.PassiveAlly,
                    true,
                    false);
            var perfect = TetherfallCombatQualityRules.CalculatePoiseContribution(
                18,
                100,
                melee.Threshold,
                CombatImpactKind.PerfectGuard,
                true,
                false);
            Assert(light > 0f && light < melee.Threshold,
                "One ordinary light hit must not immediately exhaust melee poise.");
            Assert(passiveProtected > 0f &&
                   passiveProtected <= melee.Threshold * 0.16f + 0.001f,
                "Passive ally pressure must be capped during protected windups.");
            Assert(!TetherfallCombatQualityRules.CanBreakProtectedWindup(
                    CombatImpactKind.PassiveAlly),
                "Passive allies must not break protected windups.");
            Assert(TetherfallCombatQualityRules.CanBreakProtectedWindup(
                    CombatImpactKind.Heavy) &&
                   TetherfallCombatQualityRules.CanBreakProtectedWindup(
                       CombatImpactKind.Threadlink) &&
                   Mathf.Approximately(perfect, melee.Threshold),
                "Heavy, Threadlink, and perfect-guard impacts must retain strong poise authority.");

            var enemyType = ExtractTypeBlock(actors, "public sealed class Enemy");
            var enemyDamage = ExtractBlock(enemyType, "public void TakeDamage(int amount, string source)");
            var enemyPoise = ExtractBlock(enemyType, "public bool ApplyPoiseDamage");
            Assert(enemyDamage.Contains("CombatImpactKind.Light"),
                "Enemy.TakeDamage compatibility overload must route through light poise.");
            Assert(enemyType.Contains("ApplyPoiseDamage(amount, impactKind);") &&
                   !enemyType.Contains(
                       "ApplyStagger(definition.attackStyle == EnemyAttackStyle.Heavy"),
                "Enemy damage still applies an unconditional per-hit stagger.");
            Assert(enemyPoise.Contains("IsWindupPoiseProtected") &&
                   enemyPoise.Contains("CanBreakProtectedWindup") &&
                   enemyPoise.Contains("IsStaggered"),
                "Enemy poise does not protect elite/heavy telegraphs from passive pressure.");

            var bossType = ExtractTypeBlock(adventure, "public sealed class DungeonBoss");
            var bossPoise = ExtractBlock(
                bossType,
                "public bool ApplyPoiseDamage");
            Assert(bossType.Contains("ApplyPoiseDamage(amount, impactKind);") &&
                   bossType.Contains("GetBossPoiseProfile") &&
                   bossPoise.Contains("IsStaggered"),
                "DungeonBoss damage is missing the narrower boss poise path.");
            Assert(accords.Contains("CombatImpactKind.PassiveAlly") &&
                   accords.Contains("CombatImpactKind.Controller"),
                "Captured Pact assists do not distinguish passive and controller poise pressure.");
        }

        private static void VerifyHitstopIntent(string combatSystems)
        {
            Assert(TetherfallCombatQualityRules.ShouldRetainAttackIntent(
                    true,
                    true,
                    true),
                "Attack intent is not retained during combat hitstop.");
            Assert(!TetherfallCombatQualityRules.ShouldRetainAttackIntent(
                    true,
                    false,
                    true),
                "Attack intent incorrectly survives ordinary gameplay pauses.");
            Assert(!TetherfallCombatQualityRules.ShouldRetainAttackIntent(
                    true,
                    true,
                    false),
                "Hitstop intent must not bypass panels or invalid input contexts.");

            var buffer = ExtractBlock(
                combatSystems,
                "private void UpdateAttackInputBuffer()");
            var holdIndex = buffer.IndexOf(
                "if (CombatHitstopActive)",
                StringComparison.Ordinal);
            var dispatchIndex = buffer.IndexOf(
                "Player.TryAttack();",
                StringComparison.Ordinal);
            Assert(buffer.Contains("GetButtonDown(TetherfallInputAction.Attack)") &&
                   holdIndex >= 0 &&
                   dispatchIndex > holdIndex,
                "The hitstop buffer must capture input, hold it, then dispatch afterward.");
            Assert(buffer.Contains("attackInputSwingSequence != combatSwingSequence"),
                "Buffered attack intent is not cleared after a successful swing.");
        }

        private static void VerifyBossExpose(
            string adventure,
            string tactics,
            string expedition,
            string overtones,
            string actors)
        {
            Assert(typeof(DungeonBoss).GetProperty(
                       "IsExposed",
                       BindingFlags.Instance | BindingFlags.Public) != null &&
                   typeof(DungeonBoss).GetMethod(
                       "ApplyExpose",
                       BindingFlags.Instance | BindingFlags.Public) != null,
                "DungeonBoss must publicly expose ApplyExpose/IsExposed parity.");
            Assert(Mathf.Approximately(
                    TetherfallCombatQualityRules.BossExposeDamageMultiplier,
                    1.15f),
                "DungeonBoss Expose damage must remain at +15%.");
            Assert(Mathf.Approximately(
                    TetherfallCombatQualityRules.ClampBossExposeDuration(99f),
                    TetherfallCombatQualityRules.BossMaximumExposeSeconds),
                "DungeonBoss Expose duration is not bounded.");

            var bossType = ExtractTypeBlock(adventure, "public sealed class DungeonBoss");
            Assert(bossType.Contains("BossExposeDamageMultiplier") &&
                   bossType.Contains("GetExposeColor()") &&
                   bossType.Contains("ExposeRemaining"),
                "DungeonBoss Expose lacks damage or readable status treatment.");
            Assert(tactics.Contains("focusedBoss.ApplyExpose(3.6f);"),
                "Focus mandate still skips DungeonBoss Expose.");
            Assert(expedition.Contains("threadlinkBoss.ApplyExpose(6.2f);"),
                "Threadlink Latch still skips DungeonBoss Expose.");
            Assert(overtones.Contains("boss.ApplyExpose(exposeSeconds);"),
                "Glass Snare/Measured Volley Expose still skips DungeonBoss.");
            Assert(CountOccurrences(actors, "target.ApplyExpose(5.2f);") >= 2 &&
                   CountOccurrences(actors, "target.ApplyExpose(2.8f);") >= 2,
                "Scout Crossweave or Threadlink Expose parity is missing for bosses.");
        }

        private static void VerifyRelayAndMountedBraid(
            string actors,
            string crossweave)
        {
            var mountSafety = ExtractBlock(
                actors,
                "public void SetMounted(bool mounted, PactCreature creature)");
            var swimSafety = ExtractBlock(
                actors,
                "public void SetSwimming(bool isSwimming)");
            Assert(actors.Contains(
                       "game.TryResolveCrossweave(enemy, crossweaveRelayToken);") &&
                   actors.Contains(
                       "game.TryResolveCrossweave(boss, crossweaveRelayToken);"),
                "Melee Crossweave resolution does not propagate its captured relay token.");
            Assert(!actors.Contains("isMounted ? OvertonePitch.None") &&
                   actors.Contains("var attackPitch = GetWeaponOvertonePitch();"),
                "Mounted basics are disconnected from weapon Overtone identity.");
            Assert(actors.Contains(
                       "game.CaptureCrossweaveRelayToken(game.ActiveWeapon)") &&
                   actors.Contains("MountedWeaponAttackName()") &&
                   actors.Contains(
                       "if (game.ActiveWeapon == WeaponDiscipline.Riftbow)"),
                "Mounted basics do not preserve discipline and Crossweave paths.");
            Assert(mountSafety.Contains("swimming = false;") &&
                   swimSafety.Contains("isSwimming && !isMounted"),
                "Mounted braid changes no longer preserve swim or mount safety.");
            Assert(!crossweave.Contains("Player.IsMounted ||"),
                "Mounted weapon swaps still suppress Crossweave arming.");
        }

        private static void VerifyTargetCycling(string combatSystems)
        {
            var near = new CombatTargetCycleKey(
                4f,
                0.4f,
                "enemy.a",
                new Vector3(1f, 0f, 1f),
                0,
                10);
            var far = new CombatTargetCycleKey(
                9f,
                0.1f,
                "enemy.b",
                new Vector3(2f, 0f, 1f),
                0,
                11);
            var tieA = new CombatTargetCycleKey(
                4f,
                0.4f,
                "enemy.a",
                new Vector3(1f, 0f, 1f),
                0,
                10);
            Assert(TetherfallCombatQualityRules.CompareTargetCycleKeys(near, far) < 0,
                "Target cycle order must prefer the deterministic nearer key.");
            Assert(TetherfallCombatQualityRules.CompareTargetCycleKeys(near, tieA) == 0,
                "Equivalent target cycle keys do not compare deterministically.");

            var toggle = ExtractBlock(combatSystems, "public void ToggleCombatTarget()");
            Assert(toggle.Contains("nextIndex = currentIndex + 1;") &&
                   toggle.Contains("nextIndex >= candidateCount"),
                "Repeated focus input does not cycle candidates before clearing.");
            Assert(combatSystems.Contains("boss.BossId"),
                "Boss focus candidates do not use their stable encounter id.");
            Assert(combatSystems.Contains("CombatFocusReleaseRange = 48f") &&
                   combatSystems.Contains("ValidateCombatTarget()"),
                "Combat focus no longer retains its target to the release range.");
        }

        private static void VerifyScaling(
            string actors,
            string adventure,
            string dungeons)
        {
            var low = TetherfallCombatQualityRules.GetEnemyScalingBand(
                1,
                EncounterScalingContext.CampRepeat,
                true);
            var first = TetherfallCombatQualityRules.GetEnemyScalingBand(
                12,
                EncounterScalingContext.CampFirstClear,
                false);
            var repeat = TetherfallCombatQualityRules.GetEnemyScalingBand(
                12,
                EncounterScalingContext.CampRepeat,
                false);
            var repeatAgain = TetherfallCombatQualityRules.GetEnemyScalingBand(
                12,
                EncounterScalingContext.CampRepeat,
                false);
            var boss = TetherfallCombatQualityRules.GetBossScalingBand(12, true);

            Assert(Mathf.Approximately(low.HealthMultiplier, 1f) &&
                   Mathf.Approximately(low.DamageMultiplier, 1f),
                "Low-rank encounter scaling must remain neutral.");
            Assert(repeat.HealthMultiplier > first.HealthMultiplier &&
                   repeat.DamageMultiplier > first.DamageMultiplier,
                "High-rank repeat camps must scale above first clears.");
            Assert(repeat.HealthMultiplier <=
                       TetherfallCombatQualityRules.MaximumEnemyHealthScale &&
                   repeat.DamageMultiplier <=
                       TetherfallCombatQualityRules.MaximumEnemyDamageScale,
                "Enemy encounter scaling exceeds its clamps.");
            Assert(Mathf.Approximately(
                       repeat.HealthMultiplier,
                       repeatAgain.HealthMultiplier) &&
                   Mathf.Approximately(
                       repeat.DamageMultiplier,
                       repeatAgain.DamageMultiplier),
                "Encounter scaling is not deterministic for identical context.");
            Assert(boss.HealthMultiplier <=
                       TetherfallCombatQualityRules.MaximumBossHealthScale &&
                   boss.DamageMultiplier <=
                       TetherfallCombatQualityRules.MaximumBossDamageScale &&
                   boss.HealthMultiplier < repeat.HealthMultiplier,
                "Boss scaling is not separately and more narrowly clamped.");
            Assert(actors.Contains("ResolveScalingContext") &&
                   actors.Contains("AwardedFirstClear"),
                "Enemy creation does not derive scaling from deterministic camp context.");
            Assert(adventure.Contains("repeatEncounterScaling") &&
                   adventure.Contains("GetBossScalingBand"),
                "DungeonBoss creation does not apply its narrower repeat band.");
            Assert(dungeons.Contains("DungeonBoss.Create(") &&
                   dungeons.Contains("replay);"),
                "Dungeon replay context is not propagated into boss scaling.");
        }

        private static void VerifyNonAllocGuards(
            string actors,
            string combatSystems,
            string accords)
        {
            var collection = ExtractBlock(
                combatSystems,
                "private int CollectCombatFocusCandidates");
            Assert(collection.Contains("Physics.OverlapSphereNonAlloc(") &&
                   !collection.Contains("Physics.OverlapSphere(") &&
                   !collection.Contains("new List<") &&
                   !collection.Contains("OrderBy("),
                "Target cycling introduced an allocating combat query or sort.");
            Assert(combatSystems.Contains(
                       "private readonly Collider[] combatFocusHitBuffer") &&
                   combatSystems.Contains(
                       "private readonly CombatFocusCandidate[] combatFocusCandidates"),
                "Target cycling is missing reusable candidate buffers.");

            var enemyType = ExtractTypeBlock(actors, "public sealed class Enemy");
            Assert(enemyType.Contains("Physics.OverlapSphereNonAlloc(") &&
                   enemyType.Contains("Physics.RaycastNonAlloc(") &&
                   enemyType.Contains("separationHitBuffer"),
                "Enemy combat queries lost their reusable nonalloc guards.");
            Assert(accords.Contains("Physics.OverlapSphereNonAlloc(") &&
                   accords.Contains("assistTargetBuffer"),
                "Pact assist targeting lost its nonalloc source guard.");
        }

        private static string ReadSource(string folder, string fileName)
        {
            var path = Path.Combine(Application.dataPath, folder, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Commit & Counter verification source is missing: {path}",
                    path);
            }

            return File.ReadAllText(path);
        }

        private static string ExtractTypeBlock(string source, string typeToken)
        {
            return ExtractBlock(source, typeToken);
        }

        private static string ExtractBlock(string source, string token)
        {
            var tokenIndex = source.IndexOf(token, StringComparison.Ordinal);
            Assert(tokenIndex >= 0, $"Required source token is missing: {token}");
            var braceIndex = source.IndexOf('{', tokenIndex);
            Assert(braceIndex >= 0, $"Required source block has no opening brace: {token}");

            var depth = 0;
            for (var index = braceIndex; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(tokenIndex, index - tokenIndex + 1);
                    }
                }
            }

            throw new InvalidOperationException(
                $"Required source block has no closing brace: {token}");
        }

        private static int CountOccurrences(string source, string token)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(
                       token,
                       index,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
