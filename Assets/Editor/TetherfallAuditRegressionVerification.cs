#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallAuditRegressionVerification
    {
        private static int assertions;

        [MenuItem("Tetherfall/Verify/Audit Regressions")]
        public static void Verify()
        {
            assertions = 0;
            var previousLockState = Cursor.lockState;
            var previousCursorVisibility = Cursor.visible;
            GameObject gameRoot = null;
            GameObject playerRoot = null;
            GameObject bossRoot = null;
            GameObject obstacle = null;
            GameObject unavailableRoot = null;
            GameObject verticalRoot = null;
            GameObject availableRoot = null;
            GameObject waterRootA = null;
            GameObject waterRootB = null;

            try
            {
                gameRoot = new GameObject("Audit regression game");
                var game = gameRoot.AddComponent<TetherfallGame>();
                playerRoot = new GameObject("Audit regression player");
                var player = playerRoot.AddComponent<PlayerAvatar>();
                player.Initialize(game);
                SetPlayer(game, player);

                VerifyCursorAndSaveSafety(game);
                VerifyFieldArchiveFiltering(game);
                VerifyThreadbindReward(game, player);
                VerifyResourceAcquisitionExperience(game);
                VerifyInteractionSelection(game, player, out unavailableRoot, out verticalRoot, out availableRoot);
                VerifyWaterOverlapAndDismount(player, out waterRootA, out waterRootB);
                VerifyExpandedRegionRouting();
                VerifyBossLineOfSightAndBuffers(
                    game,
                    player,
                    out bossRoot,
                    out obstacle);
                VerifySettlementIntegrationSource();
                VerifyHardeningIntegrationSource();
                VerifyNonAllocPhysicsSource();

                Debug.Log(
                    $"Tetherfall audit regression verification passed: {assertions} assertions cover " +
                    "cursor/input pause safety, relock suppression, coalesced/critical save retry, " +
                    "Field Archive filtering, Threadbind/resource rewards, vertical-aware " +
                    "interaction status, overlapping water, dismount restoration, boss line-of-sight, " +
                    "settlement modal/save ordering, and non-alloc gameplay query buffers.");
            }
            finally
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousCursorVisibility;
                DestroyImmediateSafe(obstacle);
                DestroyImmediateSafe(bossRoot);
                DestroyImmediateSafe(waterRootB);
                DestroyImmediateSafe(waterRootA);
                DestroyImmediateSafe(availableRoot);
                DestroyImmediateSafe(verticalRoot);
                DestroyImmediateSafe(unavailableRoot);
                DestroyImmediateSafe(playerRoot);
                DestroyImmediateSafe(gameRoot);
            }
        }

        public static void VerifyFromCommandLine()
        {
            try
            {
                Verify();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void VerifyCursorAndSaveSafety(TetherfallGame game)
        {
            Cursor.lockState = CursorLockMode.None;
            Assert(game.GameplayPaused, "An unlocked cursor must pause gameplay.");
            Assert(
                !(bool)GetProperty(game, "CanProcessExpansionGameplayInput"),
                "Expansion gameplay mutations must be disabled while the cursor is unlocked.");

            Cursor.lockState = CursorLockMode.Locked;
            SetProperty(game, "CodexOpen", true);
            Assert(
                !(bool)GetProperty(game, "CanProcessExpansionGameplayInput"),
                "Expansion gameplay mutations must be disabled while modal gameplay pause is active.");
            SetProperty(game, "CodexOpen", false);

            Cursor.lockState = CursorLockMode.None;
            Invoke(game, "AcquireGameplayCursor");
            Assert(
                ReadField<int>(game, "cursorLockAcquiredFrame") == Time.frameCount,
                "Cursor acquisition must mark the current frame.");
            Assert(
                (bool)GetProperty(game, "CursorLockAcquiredThisFrame"),
                "The current cursor acquisition frame must suppress attack processing.");

            Cursor.lockState = CursorLockMode.None;
            WriteField(game, "cursorLockAcquiredFrame", -1);
            SetProperty(game, "CodexOpen", true);
            Invoke(game, "AcquireGameplayCursor");
            Assert(
                ReadField<int>(game, "cursorLockAcquiredFrame") == -1,
                "UI clicks must not reacquire the gameplay cursor while a modal panel is open.");
            SetProperty(game, "CodexOpen", false);

            WriteField(game, "saveRequested", false);
            WriteField(game, "criticalSaveRequested", false);
            Invoke(game, "RequestSave");
            Assert(ReadField<bool>(game, "saveRequested"), "A normal state mutation must dirty the save.");
            var firstDeadline = ReadField<float>(game, "saveNotBefore");
            Assert(firstDeadline > Time.time, "Normal saves must be coalesced behind a short deadline.");

            Invoke(game, "RequestSave");
            Assert(
                Mathf.Approximately(firstDeadline, ReadField<float>(game, "saveNotBefore")),
                "Repeated mutations in one coalescing window must not continually move the deadline.");

            Invoke(game, "RequestCriticalSave");
            Assert(
                ReadField<bool>(game, "criticalSaveRequested"),
                "Critical transitions must bypass combat-save deferral.");
            Assert(
                ReadField<float>(game, "saveNotBefore") <= Time.time,
                "Critical saves must be eligible immediately.");
            Invoke(game, "RetainSaveRequestAfterFailure");
            Assert(
                ReadField<bool>(game, "saveRequested") &&
                ReadField<bool>(game, "criticalSaveRequested"),
                "A failed critical save must retain both dirty and urgent retry flags.");
            Assert(
                ReadField<float>(game, "saveNotBefore") > Time.time,
                "A failed save must schedule a bounded retry instead of spinning every frame.");

            Assert(
                ReadArrayLength<PlayerAvatar, Collider>(game.Player, "combatHitBuffer") >= 32,
                "Player melee must retain a reusable non-alloc overlap buffer.");
            Assert(
                ReadArrayLength<PlayerAvatar, RaycastHit>(game.Player, "combatLineHitBuffer") >= 16,
                "Player melee line-of-sight must retain a reusable non-alloc ray buffer.");
            Assert(
                ReadArrayLength<TetherfallGame, Collider>(game, "perfectGuardHitBuffer") >= 32,
                "Perfect guard must retain a reusable non-alloc overlap buffer.");
            Assert(
                ReadArrayLength<TetherfallGame, RaycastHit>(game, "expeditionLineHitBuffer") >= 16,
                "Expedition targeting must retain a reusable non-alloc line-of-sight buffer.");
            Assert(
                ReadArrayLength<TetherfallGame, RaycastHit>(game, "crossweaveLineHitBuffer") >= 16,
                "Crossweave targeting must retain a reusable non-alloc line-of-sight buffer.");
            Assert(
                ReadArrayLength<TetherfallGame, Collider>(game, "threadlinkAreaHitBuffer") >= 32,
                "Threadlink area effects must retain a reusable non-alloc overlap buffer.");
            var campQueryCapacity = ReadArrayLength<TetherfallGame, Collider>(
                game,
                "campPlacementOverlapBuffer");
            Assert(
                campQueryCapacity >= 16 && campQueryCapacity <= 64,
                "Camp placement must retain a reusable bounded non-alloc overlap buffer.");
        }

        private static void VerifyFieldArchiveFiltering(TetherfallGame game)
        {
            var recovered = ReadField<HashSet<string>>(
                game,
                "recoveredFieldCacheIds");
            Assert(recovered != null, "Recovered field-cache IDs must use a filterable set.");
            recovered.Clear();
            recovered.Add(FieldCacheCatalog.All[0].id);
            recovered.Add("region-pack-secret.audit-only");
            Assert(
                game.RecoveredFieldCaches == 1,
                "Region-pack secret IDs must not count toward Field Archives.");

            foreach (var definition in FieldCacheCatalog.All)
            {
                recovered.Add(definition.id);
            }

            recovered.Add("region-pack-secret.second-audit-only");
            Assert(
                game.RecoveredFieldCaches == FieldCacheCatalog.All.Count,
                "Field Archive recovery count must be capped to canonical survey IDs.");
            recovered.Clear();
        }

        private static void VerifyThreadbindReward(
            TetherfallGame game,
            PlayerAvatar player)
        {
            Invoke(game, "BuildQuestLog");
            var quests = ReadField<List<QuestRecord>>(game, "questLog");
            var accords = quests.Find(record => record.id == "accords");
            Assert(accords != null, "The voluntary Accord quest must exist.");
            var previousProgress = accords.current;
            var previousExperience = game.Experience;
            var previousRosterCount = game.PactRoster.Count;
            var accept = typeof(TetherfallGame).GetMethod(
                "AcceptThreadbindAccord",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(accept != null, "Threadbind Accord acceptance entry point must exist.");

            var arguments = new object[]
            {
                TetherfallAccordSpeciesCatalog.Wild[0],
                "origin.audit.threadbind",
                null
            };
            try
            {
                var accepted = (bool)accept.Invoke(game, arguments);
                Assert(accepted, "A valid voluntary wild Accord must enter the roster.");
                Assert(
                    arguments[2] is CapturedPactSaveData &&
                    game.PactRoster.Count == previousRosterCount + 1,
                    "Successful Threadbind acceptance must insert exactly one roster record.");
                Assert(
                    accords.current == previousProgress + 1,
                    "Successful Threadbind acceptance must advance the Accord quest exactly once.");
                Assert(
                    game.Experience ==
                    previousExperience + TetherfallGame.ThreadbindAccordExperienceReward,
                    "Successful Threadbind acceptance must grant its bounded XP reward exactly once.");

                var progressAfterSuccess = accords.current;
                var experienceAfterSuccess = game.Experience;
                var tryInsert = typeof(TetherfallGame).GetMethod(
                    "TryInsertThreadbindAccordRecord",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert(tryInsert != null, "Threadbind roster insertion guard must exist.");
                Assert(
                    !(bool)tryInsert.Invoke(game, new[] { arguments[2] }) &&
                    accords.current == progressAfterSuccess &&
                    game.Experience == experienceAfterSuccess,
                    "A duplicate/failed roster insertion must not grant quest progress or XP.");

                var invalidArguments = new object[] { null, "origin.audit.invalid", null };
                var invalidAccepted = (bool)accept.Invoke(game, invalidArguments);
                Assert(
                    !invalidAccepted &&
                    accords.current == progressAfterSuccess &&
                    game.Experience == experienceAfterSuccess,
                    "Invalid voluntary Accord input must not grant quest progress or XP.");
            }
            finally
            {
                Invoke(game, "DestroyAllPactFieldActors");
                Assert(
                    ReferenceEquals(game.Player, player),
                    "Threadbind verification must not replace the active player.");
            }
        }

        private static void VerifyResourceAcquisitionExperience(
            TetherfallGame game)
        {
            var calculator = typeof(TetherfallGame).GetMethod(
                "CalculateResourceExperience",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(calculator != null, "Resource XP calculator must exist.");
            int Calculate(int amount, ResourceAcquisitionSource source)
            {
                return (int)calculator.Invoke(null, new object[] { amount, source });
            }

            Assert(
                Calculate(1, ResourceAcquisitionSource.WorldGather) > 0,
                "A valid world gather must grant a small fixed XP award.");
            Assert(
                Calculate(9999, ResourceAcquisitionSource.WorldGather) ==
                TetherfallGame.MaximumWorldGatherExperience,
                "World-gather XP must remain capped regardless of yield size.");
            Assert(
                Calculate(5, ResourceAcquisitionSource.WorldGather) ==
                Calculate(9999, ResourceAcquisitionSource.WorldGather),
                "World-gather XP must use bounded base yield.");
            Assert(
                Calculate(10, ResourceAcquisitionSource.AuthoredReward) == 0 &&
                Calculate(10, ResourceAcquisitionSource.VendorTrade) == 0 &&
                Calculate(10, ResourceAcquisitionSource.SettlementTransfer) == 0,
                "Authored, vendor, and settlement resource transfers must not silently grant XP.");

            var experienceBeforeTransfers = game.Experience;
            game.AddResource(
                ResourceKind.Timber,
                4,
                ResourceAcquisitionSource.AuthoredReward);
            game.AddResource(
                ResourceKind.Stone,
                4,
                ResourceAcquisitionSource.VendorTrade);
            game.AddResource(
                ResourceKind.Herbs,
                4,
                ResourceAcquisitionSource.SettlementTransfer);
            game.AddResource(ResourceKind.Fiber, 4);
            Assert(
                game.Experience == experienceBeforeTransfers,
                "Authored/reward-bundle, vendor, and settlement AddResource calls must preserve XP exactly.");

            var timberBeforeGather = game.GetResource(ResourceKind.Timber);
            var gatherExperience = Calculate(
                3,
                ResourceAcquisitionSource.WorldGather);
            game.AddResource(
                ResourceKind.Timber,
                3,
                ResourceAcquisitionSource.WorldGather);
            Assert(
                game.GetResource(ResourceKind.Timber) >= timberBeforeGather + 3 &&
                game.Experience == experienceBeforeTransfers + gatherExperience,
                "World gathers must preserve resource yield while granting XP from base yield.");
        }

        private static void VerifyInteractionSelection(
            TetherfallGame game,
            PlayerAvatar player,
            out GameObject unavailableRoot,
            out GameObject verticalRoot,
            out GameObject availableRoot)
        {
            player.transform.position = Vector3.zero;

            unavailableRoot = new GameObject("Unavailable same-floor interaction");
            unavailableRoot.transform.position = new Vector3(1f, 0f, 0f);
            var unavailable = unavailableRoot.AddComponent<AuditInteractable>();
            unavailable.Configure(false, "Bridge resets in 5s");
            game.RegisterInteractable(unavailable);

            verticalRoot = new GameObject("Interaction on another floor");
            verticalRoot.transform.position = new Vector3(0.25f, 3.1f, 0f);
            var vertical = verticalRoot.AddComponent<AuditInteractable>();
            vertical.Configure(true, "Use rooftop console");
            game.RegisterInteractable(vertical);

            Assert(
                game.GetInteractionPrompt() == "Bridge resets in 5s",
                "Unavailable nearby interactions must expose useful status while other-floor targets are ignored.");

            availableRoot = new GameObject("Available same-floor interaction");
            availableRoot.transform.position = new Vector3(0.75f, 0f, 0f);
            var available = availableRoot.AddComponent<AuditInteractable>();
            available.Configure(true, "Open supply cache");
            game.RegisterInteractable(available);

            Assert(
                game.GetInteractionPrompt() == "Open supply cache",
                "Available same-floor interactions must outrank nearby unavailable targets.");

            var pactRoot = new GameObject("Recruited Pact prompt suppression");
            try
            {
                pactRoot.transform.position = new Vector3(0.1f, 0f, 0f);
                var pact = pactRoot.AddComponent<PactCreature>();
                SetProperty(pact, "IsPacted", true);
                game.RegisterInteractable(pact);
                Assert(
                    game.GetInteractionPrompt() == "Open supply cache",
                    "Following recruited actors must not steal interaction prompts from world objects.");
            }
            finally
            {
                DestroyImmediateSafe(pactRoot);
            }

            Assert(
                ReadField<bool>(game, "hasInteractionScanCache"),
                "Interaction prompting must retain a short-lived nearest-target cache.");
        }

        private static void VerifyWaterOverlapAndDismount(
            PlayerAvatar player,
            out GameObject waterRootA,
            out GameObject waterRootB)
        {
            waterRootA = new GameObject("Audit water A");
            waterRootB = new GameObject("Audit water B");
            var waterA = waterRootA.AddComponent<WaterVolume>();
            var waterB = waterRootB.AddComponent<WaterVolume>();

            player.EnterWaterVolume(waterA);
            player.EnterWaterVolume(waterB);
            Assert(player.IsSwimming, "Entering overlapping water volumes must enable swimming.");

            player.ExitWaterVolume(waterA);
            Assert(player.IsSwimming, "Exiting one overlapping volume must not disable swimming.");

            player.SetMounted(true, null);
            Assert(!player.IsSwimming, "Mounting must suspend swimming.");
            player.SetMounted(false, null);
            Assert(player.IsSwimming, "Dismounting inside tracked water must restore swimming.");

            player.ExitWaterVolume(waterB);
            Assert(!player.IsSwimming, "Leaving the final water volume must disable swimming.");

            player.EnterWaterVolume(waterB);
            player.Teleport(new Vector3(2f, 1f, 2f));
            Assert(!player.IsSwimming, "Teleporting must clear stale water-volume membership.");
            player.EnterWaterVolume(waterB);
            Assert(player.IsSwimming, "A post-teleport trigger stay must restore valid water membership.");
            player.ExitWaterVolume(waterB);
        }

        private static void VerifyExpandedRegionRouting()
        {
            var resolver = typeof(TetherfallGame).GetMethod(
                "GetRegionForPosition",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert(resolver != null, "TetherfallGame region resolver must exist.");
            foreach (var region in TetherfallRegionPacks.Playable)
            {
                var resolved = resolver.Invoke(null, new object[] { region.center }) as string;
                Assert(
                    string.Equals(resolved, region.displayName, StringComparison.Ordinal),
                    $"Runtime region routing did not resolve playable pack '{region.displayName}'.");
            }
        }

        private static void VerifyBossLineOfSightAndBuffers(
            TetherfallGame game,
            PlayerAvatar player,
            out GameObject bossRoot,
            out GameObject obstacle)
        {
            player.transform.position = new Vector3(0f, 0f, 6f);
            var boss = DungeonBoss.Create(
                game,
                null,
                BossCatalog.Get("boss.skyvault_warden"),
                Vector3.zero);
            bossRoot = boss.gameObject;

            Assert(
                (bool)Invoke(boss, "HasClearAttackPath"),
                "Boss melee path should be clear without world geometry between boss and player.");

            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Audit boss line-of-sight wall";
            obstacle.transform.position = new Vector3(0f, 1.2f, 3f);
            obstacle.transform.localScale = new Vector3(4f, 3f, 0.6f);
            Physics.SyncTransforms();

            Assert(
                !(bool)Invoke(boss, "HasClearAttackPath"),
                "Boss melee and charge damage must be blocked by intervening world geometry.");
            Assert(
                ReadArrayLength<DungeonBoss, RaycastHit>(boss, "movementHitBuffer") >= 24,
                "Boss movement must use a reusable non-alloc sphere-cast buffer.");
            Assert(
                ReadArrayLength<DungeonBoss, RaycastHit>(boss, "attackPathHitBuffer") >= 16,
                "Boss attack fairness must use a reusable non-alloc line-of-sight buffer.");

            var allyRoot = new GameObject("Audit ally buffer host");
            var companionRoot = new GameObject("Audit companion buffer host");
            try
            {
                var pact = allyRoot.AddComponent<PactCreature>();
                var companion = companionRoot.AddComponent<HumanCompanion>();
                Assert(
                    ReadArrayLength<PactCreature, Collider>(pact, "tacticalHitBuffer") >= 32,
                    "Pact tactical attacks must use a reusable non-alloc overlap buffer.");
                Assert(
                    ReadArrayLength<HumanCompanion, Collider>(companion, "tacticalHitBuffer") >= 32,
                    "Companion tactical attacks must use a reusable non-alloc overlap buffer.");
            }
            finally
            {
                DestroyImmediateSafe(companionRoot);
                DestroyImmediateSafe(allyRoot);
            }

            var enemyRoot = new GameObject("Audit enemy buffer host");
            try
            {
                var enemy = enemyRoot.AddComponent<Enemy>();
                Assert(
                    ReadArrayLength<Enemy, Collider>(enemy, "separationHitBuffer") >= 12,
                    "Enemy separation must use a bounded spatial-query buffer rather than all-pairs scans.");
                Assert(
                    ReadArrayLength<Enemy, RaycastHit>(enemy, "attackPathHitBuffer") >= 16,
                    "Enemy attack line-of-sight must use a reusable non-alloc buffer.");
            }
            finally
            {
                DestroyImmediateSafe(enemyRoot);
            }

            var animatedEnemy = Enemy.Create(
                game,
                null,
                "enemy.hollow_raider",
                new Vector3(18f, 0f, 0f));
            try
            {
                Assert(
                    ReadField<TetherfallHostileAnimation>(animatedEnemy, "hostileAnimation") != null,
                    "Runtime enemies must bind exact attack, hit, stagger, and death animation hooks.");
            }
            finally
            {
                DestroyImmediateSafe(animatedEnemy != null ? animatedEnemy.gameObject : null);
            }

            var projectileRoot = new GameObject("Audit projectile buffer host");
            try
            {
                var projectile = projectileRoot.AddComponent<EnemyProjectile>();
                Assert(
                    ReadArrayLength<EnemyProjectile, RaycastHit>(projectile, "hitBuffer") >= 12,
                    "Enemy projectiles must use reusable non-alloc sphere-cast storage.");
            }
            finally
            {
                DestroyImmediateSafe(projectileRoot);
            }

            var threadglassRoot = new GameObject("Audit Threadglass buffer host");
            try
            {
                var projectile = threadglassRoot.AddComponent<ThreadglassProjectile>();
                Assert(
                    ReadArrayLength<ThreadglassProjectile, RaycastHit>(projectile, "hitBuffer") >= 12,
                    "Threadglass projectiles must use reusable non-alloc sphere-cast storage.");
            }
            finally
            {
                DestroyImmediateSafe(threadglassRoot);
            }
        }

        private static void VerifySettlementIntegrationSource()
        {
            var scripts = Path.Combine(Application.dataPath, "Scripts");
            var gameSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallGame.cs"));
            var tutorialSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallTutorial.cs"));
            var settlementSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallPlayerSettlement.cs"));

            Assert(
                gameSource.Contains("internal bool IsModalUiOpen =>") &&
                gameSource.Contains("IsPlayerSettlementInputCaptured;") &&
                tutorialSource.Contains("IsPlayerSettlementInputCaptured"),
                "Settlement panel/placement must participate in both modal and gameplay-pause guards.");
            Assert(
                settlementSource.Contains(
                    "playerSettlementInputCapturedFrame = Time.frameCount;") &&
                settlementSource.Contains(
                    "playerSettlementInputCapturedFrame == Time.frameCount") &&
                !settlementSource.Contains(
                    "Cursor.lockState = CursorLockMode.Locked;") &&
                gameSource.Contains("togglePressed && !cancelPressed") &&
                gameSource.Contains(
                    "TetherfallInputAction.CancelOrReleaseCursor") &&
                !gameSource.Contains("Input.GetKeyDown(KeyCode.Escape)") &&
                !settlementSource.Contains("Input.GetKeyDown(KeyCode.Escape)"),
                "Settlement close/cancel frames must use the remappable action and remain captured without attack leakage.");

            var settlementInput =
                gameSource.IndexOf(
                    "HandlePlayerSettlementPlanningInput()",
                    StringComparison.Ordinal);
            var interaction =
                gameSource.IndexOf("TryInteract();", StringComparison.Ordinal);
            Assert(
                settlementInput >= 0 &&
                interaction > settlementInput,
                "Settlement input capture must run before ordinary field interaction.");

            var writePact =
                gameSource.IndexOf(
                    "WritePactRosterSave(data);",
                    StringComparison.Ordinal);
            var writeSettlement =
                gameSource.IndexOf(
                    "WritePlayerSettlementSave(data);",
                    StringComparison.Ordinal);
            var save =
                gameSource.IndexOf(
                    "TetherfallSaveSystem.Save(data)",
                    StringComparison.Ordinal);
            Assert(
                writePact >= 0 &&
                writeSettlement > writePact &&
                save > writeSettlement,
                "Settlement save capture must follow Pact roster capture and precede disk persistence.");

            var restorePact =
                gameSource.IndexOf(
                    "RestorePactRosterState(data);",
                    StringComparison.Ordinal);
            var restoreSettlement =
                gameSource.IndexOf(
                    "RestorePlayerSettlementState(data);",
                    StringComparison.Ordinal);
            var restoreFinished =
                gameSource.IndexOf(
                    "isRestoringSave = false;",
                    StringComparison.Ordinal);
            Assert(
                restorePact >= 0 &&
                restoreSettlement > restorePact &&
                restoreFinished > restoreSettlement,
                "Settlement restore must follow Pact roster restore before restore mode ends.");
            Assert(
                gameSource.Contains("private void OnApplicationFocus(bool hasFocus)") &&
                gameSource.Contains("private void OnApplicationPause(bool paused)") &&
                gameSource.Contains("SaveGame();"),
                "Application focus/pause save hooks must remain wired.");
        }

        private static void VerifyHardeningIntegrationSource()
        {
            var scripts = Path.Combine(Application.dataPath, "Scripts");
            var gameSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallGame.cs"));
            var expansionSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallExpansion.cs"));
            var fieldCacheSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallFieldCaches.cs"));
            var objectiveSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallObjectiveGuidance.cs"));
            var rosterSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallPactRoster.cs"));
            var worldBuilderSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallWorldBuilder.cs"));
            var transitionBiomeSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallTransitionBiomes.cs"));
            var roamingSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallRoamingWorld.cs"));
            var settingsSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallSettings.cs"));
            var tutorialSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallTutorial.cs"));
            var mapSource = File.ReadAllText(
                Path.Combine(scripts, "TetherfallWorldMap.cs"));

            var inputStart = expansionSource.IndexOf(
                "private void HandleExpansionInput()",
                StringComparison.Ordinal);
            var inputEnd = expansionSource.IndexOf(
                "private void DrawExpansionUi",
                inputStart,
                StringComparison.Ordinal);
            Assert(
                inputStart >= 0 && inputEnd > inputStart,
                "HandleExpansionInput source span must remain discoverable.");
            var inputSource = expansionSource.Substring(
                inputStart,
                inputEnd - inputStart);
            var pauseGuard = inputSource.IndexOf(
                "if (!CanProcessExpansionGameplayInput)",
                StringComparison.Ordinal);
            Assert(pauseGuard >= 0, "Expansion input must retain an explicit gameplay-mutation pause guard.");
            Assert(
                inputSource.IndexOf("OpenPanel(", StringComparison.Ordinal) <
                pauseGuard,
                "Expansion UI opening must remain usable before the gameplay-mutation pause guard.");
            var mutations = new[]
            {
                "TrySetWeapon(",
                "TryUseCookedMeal();",
                "CycleActivePact();",
                "CycleParty();",
                "Player.TryResonanceAbility();",
                "ToggleCombatTarget();",
                "TryTacticalCommand();",
                "CycleExpeditionDirective();"
            };
            foreach (var mutation in mutations)
            {
                Assert(
                    inputSource.IndexOf(mutation, StringComparison.Ordinal) >
                    pauseGuard,
                    $"Expansion mutation '{mutation}' must remain after the pause guard.");
            }

            Assert(
                fieldCacheSource.Contains(
                    "public int RecoveredFieldCaches => CountFieldArchiveRecoveries();") &&
                fieldCacheSource.Contains(
                    "IsFieldArchiveRecoveryId(id)") &&
                expansionSource.Contains(
                    "CountFieldArchiveRecoveries()") &&
                !objectiveSource.Contains(
                    "recoveredFieldCacheIds.Count"),
                "Field Archive restore, HUD, and guidance must use the canonical catalog filter.");

            var threadbindStart = rosterSource.IndexOf(
                "internal bool AcceptThreadbindAccord(",
                StringComparison.Ordinal);
            var threadbindEnd = rosterSource.IndexOf(
                "private bool TryInsertThreadbindAccordRecord(",
                threadbindStart,
                StringComparison.Ordinal);
            Assert(
                threadbindStart >= 0 && threadbindEnd > threadbindStart,
                "Threadbind acceptance source span must remain discoverable.");
            var threadbindSource = rosterSource.Substring(
                threadbindStart,
                threadbindEnd - threadbindStart);
            var insertion = threadbindSource.IndexOf(
                "TryInsertThreadbindAccordRecord(record)",
                StringComparison.Ordinal);
            var questReward = threadbindSource.IndexOf(
                "OnPactFormed();",
                StringComparison.Ordinal);
            var experienceReward = threadbindSource.IndexOf(
                "GainExperience(ThreadbindAccordExperienceReward);",
                StringComparison.Ordinal);
            Assert(
                insertion >= 0 &&
                questReward > insertion &&
                experienceReward > questReward &&
                threadbindSource.IndexOf(
                    "OnPactFormed();",
                    questReward + 1,
                    StringComparison.Ordinal) < 0,
                "Threadbind quest/XP rewards must occur once and only after successful insertion.");

            Assert(
                gameSource.Contains(
                    "AddResource(kind, amount, ResourceAcquisitionSource.AuthoredReward);") &&
                gameSource.Contains(
                    "source == ResourceAcquisitionSource.WorldGather") &&
                gameSource.Contains(
                    "ApplySkillResourceYield(baseAmount)") &&
                gameSource.Contains(
                    "CalculateResourceExperience(baseAmount, source)") &&
                worldBuilderSource.Contains(
                    "ResourceAcquisitionSource.WorldGather") &&
                transitionBiomeSource.Contains(
                    "ResourceAcquisitionSource.WorldGather") &&
                worldBuilderSource.Contains(
                    "ResourceAcquisitionSource.SettlementTransfer") &&
                roamingSource.Contains(
                    "ResourceAcquisitionSource.VendorTrade"),
                "Resource acquisition sources must explicitly separate world yield, vendor, settlement, and authored rewards.");

            var retainStart = gameSource.IndexOf(
                "private void RetainSaveRequestAfterFailure()",
                StringComparison.Ordinal);
            var restoreStart = gameSource.IndexOf(
                "private void ApplySavedExpedition(",
                retainStart,
                StringComparison.Ordinal);
            Assert(
                retainStart >= 0 && restoreStart > retainStart,
                "Failed-save retry source span must remain discoverable.");
            var retainSource = gameSource.Substring(
                retainStart,
                restoreStart - retainStart);
            Assert(
                gameSource.Contains(
                    "if (TetherfallSaveSystem.Save(data))") &&
                gameSource.Contains(
                    "RetainSaveRequestAfterFailure();") &&
                retainSource.Contains(
                    "saveRequested = true;") &&
                !retainSource.Contains(
                    "criticalSaveRequested = false;"),
                "Failed I/O must retain the dirty request without clearing critical-save urgency.");

            Assert(
                settingsSource.Contains(
                    "reinforceColorCuesWithShapes") &&
                mapSource.Contains(
                    "ShouldReinforceColorCues") &&
                mapSource.Contains(
                    "GetAccessibleWorldMapState") &&
                gameSource.Contains(
                    "GetAccessibleCombatStatusText") &&
                gameSource.Contains(
                    "CRITICAL VITALITY") &&
                gameSource.Contains(
                    "$\"HP {activeDungeonBoss.Health}/{activeDungeonBoss.MaxHealth}\""),
                "Accessibility settings, reinforced map/combat status, and numeric critical/boss vitality must remain wired.");
            Assert(
                gameSource.Contains(
                    "GetBindingSummary(") &&
                rosterSource.Contains(
                    "GetBindingSummary(") &&
                rosterSource.Contains(
                    "DrawPanel(rect, \"THREADBIND ACCORD ROSTER\")") &&
                gameSource.Contains(
                    "ApplyLowResolutionModalScale(") &&
                tutorialSource.Contains(
                    "internal bool IsTutorialPanelActive") &&
                gameSource.Contains(
                    "!IsTutorialPanelActive"),
                "Remap-aware Pact UI, shared modal scaling, field-panel styling, and tutorial-safe footer must remain wired.");

            var effectiveScale = TetherfallGame.GetEffectiveModalScale(
                1280f,
                720f,
                1.5f);
            Assert(
                1280f / effectiveScale + 0.1f >=
                TetherfallGame.MinimumModalCanvasWidth &&
                720f / effectiveScale + 0.1f >=
                TetherfallGame.MinimumModalCanvasHeight,
                "1280x720 at HUD scale 1.5 must retain the minimum usable modal canvas.");
        }

        private static void VerifyNonAllocPhysicsSource()
        {
            var scripts = Directory.GetFiles(
                Path.Combine(Application.dataPath, "Scripts"),
                "*.cs",
                SearchOption.AllDirectories);
            var combinedSource = string.Empty;
            foreach (var script in scripts)
            {
                combinedSource += File.ReadAllText(script);
            }

            Assert(
                !combinedSource.Contains("Physics.RaycastAll("),
                "Runtime scripts must not reintroduce allocating Physics.RaycastAll queries.");
            Assert(
                !combinedSource.Contains("Physics.SphereCastAll("),
                "Runtime scripts must not reintroduce allocating Physics.SphereCastAll queries.");
            Assert(
                !combinedSource.Contains("Physics.OverlapSphere("),
                "Runtime scripts must not reintroduce allocating Physics.OverlapSphere queries.");
            Assert(
                !combinedSource.Contains("Physics.OverlapBox("),
                "Runtime scripts must not reintroduce allocating Physics.OverlapBox queries.");
            Assert(
                combinedSource.Contains("Physics.OverlapBoxNonAlloc(") &&
                combinedSource.Contains("campPlacementOverlapBuffer"),
                "Camp placement must use its reusable OverlapBoxNonAlloc buffer.");
        }

        private static void SetPlayer(TetherfallGame game, PlayerAvatar player)
        {
            var property = typeof(TetherfallGame).GetProperty(
                "Player",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(property != null, "TetherfallGame.Player property must exist.");
            property.SetValue(game, player);
        }

        private static object Invoke(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(method != null, $"{target.GetType().Name}.{methodName} must exist.");
            return method.Invoke(target, null);
        }

        private static object GetProperty(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(property != null, $"{target.GetType().Name}.{propertyName} must exist.");
            return property.GetValue(target);
        }

        private static void SetProperty<T>(object target, string propertyName, T value)
        {
            var property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(property != null, $"{target.GetType().Name}.{propertyName} must exist.");
            property.SetValue(target, value);
        }

        private static T ReadField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(field != null, $"{target.GetType().Name}.{fieldName} must exist.");
            return (T)field.GetValue(target);
        }

        private static void WriteField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(field != null, $"{target.GetType().Name}.{fieldName} must exist.");
            field.SetValue(target, value);
        }

        private static int ReadArrayLength<TOwner, TElement>(TOwner target, string fieldName)
        {
            var field = typeof(TOwner).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(field != null, $"{typeof(TOwner).Name}.{fieldName} must exist.");
            var array = field.GetValue(target) as TElement[];
            Assert(array != null, $"{typeof(TOwner).Name}.{fieldName} must be a {typeof(TElement).Name} array.");
            return array.Length;
        }

        private static void DestroyImmediateSafe(GameObject target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    public sealed class AuditInteractable : MonoBehaviour, IWorldInteractable
    {
        private bool available;
        private string prompt;

        public string InteractionPrompt => prompt;

        public void Configure(bool canInteract, string interactionPrompt)
        {
            available = canInteract;
            prompt = interactionPrompt;
        }

        public bool CanInteract(TetherfallGame game)
        {
            return available;
        }

        public void Interact(TetherfallGame game)
        {
        }
    }
}
#endif
