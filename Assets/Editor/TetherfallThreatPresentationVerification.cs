using System;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallThreatPresentationVerification
    {
        [MenuItem("Tetherfall/Verify Threat Presentation", false, 2215)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Threat presentation verification is edit-mode only.");
            }

            VerifyForFullGame();
            Debug.Log(
                "Threat presentation verification passed: read-only threat APIs, priority, " +
                "status semantics, accessibility reinforcement, layout contracts, pooled outlines, " +
                "and Commit & Counter coexistence are intact.");
        }

        public static void VerifyForFullGame()
        {
            var actors = ReadSource("Scripts", "TetherfallActors.cs");
            var adventure = ReadSource("Scripts", "TetherfallAdventure.cs");
            var combat = ReadSource("Scripts", "TetherfallCombatSystems.cs");
            var presentation =
                ReadSource("Scripts", "TetherfallThreatPresentation.cs");
            var game = ReadSource("Scripts", "TetherfallGame.cs");
            var roaming = ReadSource("Scripts", "TetherfallRoamingWorld.cs");
            var bounties = ReadSource("Scripts", "TetherfallBounties.cs");
            var fullGame =
                ReadSource("Editor", "TetherfallFullGameVerification.cs");
            var shader =
                ReadSource("Shaders", "TetherfallHostileOutline.shader");
            var shaderMeta =
                ReadSource(
                    "Shaders",
                    "TetherfallHostileOutline.shader.meta");
            var graphicsSettings =
                ReadProjectSource(
                    "ProjectSettings",
                    "GraphicsSettings.asset");

            VerifyReadOnlyApis(actors, adventure);
            VerifyPriorityAndCaps(presentation);
            VerifyStatusAndExposeSemantics(
                actors,
                adventure,
                combat,
                presentation);
            VerifyAccessibilityVariants(presentation);
            VerifyOutlinePoolAndDrawPath(
                actors,
                adventure,
                combat,
                presentation,
                game,
                roaming,
                bounties,
                shader,
                shaderMeta,
                graphicsSettings);
            VerifyLayoutContracts();
            VerifyFullGameChain(fullGame);
        }

        private static void VerifyReadOnlyApis(
            string actors,
            string adventure)
        {
            RequireReadOnlyProperty(
                typeof(Enemy),
                "AttackStyle",
                typeof(EnemyAttackStyle));
            RequireReadOnlyProperty(typeof(Enemy), "IsElite", typeof(bool));
            RequireReadOnlyProperty(typeof(Enemy), "ThreatBand", typeof(int));
            RequireReadOnlyProperty(
                typeof(Enemy),
                "LastDamagedAt",
                typeof(float));
            var copyMethod = typeof(Enemy).GetMethod(
                "CopyActiveEnemiesNonAlloc",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(Enemy[]) },
                null);
            Assert(
                copyMethod != null && copyMethod.ReturnType == typeof(int),
                "Enemy must expose bounded CopyActiveEnemiesNonAlloc(Enemy[]) returning a count.");
            Assert(
                Enemy.MaximumActiveEnemyCopyCount > 0 &&
                Enemy.MaximumActiveEnemyCopyCount <= 256,
                "Enemy active-copy capacity must remain positive and bounded at 256 or fewer.");
            Assert(
                Enemy.CopyActiveEnemiesNonAlloc(null) == 0 &&
                Enemy.CopyActiveEnemiesNonAlloc(Array.Empty<Enemy>()) == 0,
                "Enemy active-copy API must safely accept null and empty buffers.");
            var neutralScaling =
                TetherfallCombatQualityRules.GetEnemyScalingBand(
                    1,
                    EncounterScalingContext.Ambient,
                    false);
            var scaledEnemy =
                TetherfallCombatQualityRules.GetEnemyScalingBand(
                    12,
                    EncounterScalingContext.CampRepeat,
                    false);
            var scaledEnemyAgain =
                TetherfallCombatQualityRules.GetEnemyScalingBand(
                    12,
                    EncounterScalingContext.CampRepeat,
                    false);
            var scaledBoss =
                TetherfallCombatQualityRules.GetBossScalingBand(12, true);
            Assert(
                TetherfallCombatQualityRules.GetThreatBand(
                    neutralScaling,
                    false) == 1 &&
                TetherfallCombatQualityRules.GetThreatBand(
                    scaledEnemy,
                    false) >
                TetherfallCombatQualityRules.GetThreatBand(
                    neutralScaling,
                    false) &&
                TetherfallCombatQualityRules.GetThreatBand(
                    scaledEnemy,
                    false) ==
                TetherfallCombatQualityRules.GetThreatBand(
                    scaledEnemyAgain,
                    false) &&
                TetherfallCombatQualityRules.GetThreatBand(
                    scaledBoss,
                    true) >= 1,
                "Threat bands must be deterministic and increase from actual encounter scaling.");

            RequireReadOnlyProperty(
                typeof(DungeonBoss),
                "Phase",
                typeof(int));
            RequireReadOnlyProperty(
                typeof(DungeonBoss),
                "ThreatBand",
                typeof(int));

            var enemyType =
                ExtractTypeBlock(actors, "public sealed class Enemy");
            var copyBlock = ExtractBlock(
                enemyType,
                "public static int CopyActiveEnemiesNonAlloc");
            Assert(
                enemyType.Contains(
                    "TetherfallCombatQualityRules.GetThreatBand(scaling, false)") &&
                copyBlock.Contains("MaximumActiveEnemyCopyCount") &&
                copyBlock.Contains("destination[copied++]") &&
                !copyBlock.Contains("new List<") &&
                !copyBlock.Contains("OrderBy("),
                "Enemy threat band or bounded nonalloc copy no longer follows actual encounter scaling.");
            Assert(
                enemyType.Contains("lastDamagedAt = Time.time;"),
                "Enemy.LastDamagedAt is not copied from a successful damage application.");

            var bossType =
                ExtractTypeBlock(adventure, "public sealed class DungeonBoss");
            Assert(
                bossType.Contains(
                    "TetherfallCombatQualityRules.GetThreatBand(scaling, true)") &&
                bossType.Contains("public int Phase =>"),
                "DungeonBoss phase or threat band is not derived from live boss scaling.");
        }

        private static void VerifyPriorityAndCaps(string presentation)
        {
            Assert(
                TetherfallThreatPresentationRules.MaximumThreatPlates == 4,
                "Threat plate cap must remain exactly four.");
            Assert(
                TetherfallThreatPresentationRules.MaximumEdgePips == 3,
                "Offscreen threat pip cap must remain exactly three.");
            Assert(
                TetherfallThreatPresentationRules.MaximumOutlineHullRenderers == 4,
                "Outline hull renderer cap must remain exactly four.");
            Assert(
                Mathf.Approximately(
                    TetherfallThreatPresentationRules.RecentDamageSeconds,
                    4.5f),
                "Recently damaged priority window must remain 4.5 seconds.");
            Assert(
                Mathf.Approximately(
                    TetherfallThreatPresentationRules.NearbyThreatRange,
                    18f),
                "Nearby awake threat range must remain 18 metres.");

            var focus = new TetherfallThreatPriorityKey(
                true,
                false,
                0f,
                400f,
                "focus",
                3);
            var recent = new TetherfallThreatPriorityKey(
                false,
                true,
                9f,
                144f,
                "recent",
                2);
            var olderRecent = new TetherfallThreatPriorityKey(
                false,
                true,
                7f,
                64f,
                "older",
                1);
            var nearby = new TetherfallThreatPriorityKey(
                false,
                false,
                0f,
                9f,
                "nearby",
                0);
            Assert(
                TetherfallThreatPresentationRules.ComparePriority(
                    focus,
                    recent) < 0 &&
                TetherfallThreatPresentationRules.ComparePriority(
                    recent,
                    nearby) < 0 &&
                TetherfallThreatPresentationRules.ComparePriority(
                    recent,
                    olderRecent) < 0,
                "Threat ordering must remain focus, newest recent damage, then nearby awake threats.");

            var updateBlock =
                ExtractBlock(presentation, "private void Update()");
            var refreshBlock = ExtractBlock(
                presentation,
                "private void RefreshSelection");
            Assert(
                updateBlock.Contains("Time.frameCount >= nextNonFocusRefreshFrame") &&
                updateBlock.Contains("Time.frameCount + 2") &&
                refreshBlock.Contains("Enemy.CopyActiveEnemiesNonAlloc") &&
                refreshBlock.Contains("BindOutlineTargets()"),
                "The centralized director no longer refreshes non-focus selection every two frames.");
            Assert(
                presentation.Contains("enemy.isActiveAndEnabled") &&
                presentation.Contains("enemy.gameObject.activeInHierarchy") &&
                presentation.Contains("boss.isActiveAndEnabled") &&
                presentation.Contains("boss.gameObject.activeInHierarchy"),
                "Inactive or asleep combatants are not guarded before presentation.");
        }

        private static void VerifyStatusAndExposeSemantics(
            string actors,
            string adventure,
            string combat,
            string presentation)
        {
            Assert(
                TetherfallThreatPresentationRules.ResolveEnemyStatus(
                    1f,
                    1f,
                    1f) == TetherfallThreatStatusKind.Ashguard &&
                TetherfallThreatPresentationRules.ResolveEnemyStatus(
                    0f,
                    1f,
                    1f) == TetherfallThreatStatusKind.Staggered &&
                TetherfallThreatPresentationRules.ResolveEnemyStatus(
                    0f,
                    0f,
                    1f) == TetherfallThreatStatusKind.Exposed &&
                TetherfallThreatPresentationRules.ResolveBossStatus(
                    1f,
                    1f) == TetherfallThreatStatusKind.Staggered,
                "Threat status priority must remain ASHGUARD, STAGGERED, then EXPOSED.");
            Assert(
                Mathf.Approximately(
                    TetherfallCombatQualityRules.EnemyExposeDamageMultiplier,
                    1.28f) &&
                Mathf.Approximately(
                    TetherfallCombatQualityRules.BossExposeDamageMultiplier,
                    1.15f),
                "Enemy and boss Expose must remain +28% and +15% respectively.");
            Assert(
                actors.Contains("EnemyExposeDamageMultiplier") &&
                adventure.Contains("BossExposeDamageMultiplier") &&
                presentation.Contains("\"EXPOSED +28%") &&
                presentation.Contains("\"EXPOSED +15%") &&
                presentation.Contains("enemy.AshguardRemaining") &&
                presentation.Contains("enemy.StaggerRemaining") &&
                presentation.Contains("enemy.ExposeRemaining") &&
                presentation.Contains("boss.StaggerRemaining") &&
                presentation.Contains("boss.ExposeRemaining"),
                "Threat UI status copy no longer matches the Commit & Counter APIs or Expose semantics.");
            Assert(
                combat.Contains(
                    "public const float EnemyExposeDamageMultiplier = 1.28f;") &&
                combat.Contains(
                    "public const float BossExposeDamageMultiplier = 1.15f;"),
                "Expose multipliers are no longer explicit shared combat contracts.");
        }

        private static void VerifyAccessibilityVariants(string presentation)
        {
            var snapshot = TetherfallAccessibilityRuntime.GetSnapshot();
            try
            {
                var cvd = snapshot.Copy();
                cvd.colorCuesEnabled = true;
                cvd.colorCuePalette =
                    TetherfallAccessibilityColorCuePalette
                        .ColorVisionDeficiencyFriendly;
                cvd.reinforceColorCuesWithShapes = false;
                Assert(
                    TetherfallAccessibilityRuntime.Apply(cvd),
                    "CVD settings could not be applied for threat verification.");
                var cvdHostile =
                    TetherfallAccessibilityRuntime.ResolveColorCue(
                        Color.red,
                        TetherfallAccessibilityColorCue.Hostile);
                Assert(
                    cvdHostile.r > cvdHostile.g &&
                    cvdHostile.g > cvdHostile.b,
                    "CVD hostile cue no longer resolves to a distinct warm signal.");

                var grayscale = snapshot.Copy();
                grayscale.colorCuesEnabled = true;
                grayscale.colorCuePalette =
                    TetherfallAccessibilityColorCuePalette.Grayscale;
                grayscale.reinforceColorCuesWithShapes = false;
                Assert(
                    TetherfallAccessibilityRuntime.Apply(grayscale),
                    "Grayscale settings could not be applied for threat verification.");
                var grayHostile =
                    TetherfallAccessibilityRuntime.ResolveColorCue(
                        Color.red,
                        TetherfallAccessibilityColorCue.Hostile);
                Assert(
                    Mathf.Approximately(grayHostile.r, grayHostile.g) &&
                    Mathf.Approximately(grayHostile.g, grayHostile.b) &&
                    TetherfallAccessibilityRuntime.ShouldReinforceColorCues,
                    "Grayscale hostile cues must force shape reinforcement.");
            }
            finally
            {
                TetherfallAccessibilityRuntime.Apply(snapshot);
            }

            Assert(
                TetherfallThreatPresentationRules.GetOutlineWidth(
                    true,
                    false) >
                TetherfallThreatPresentationRules.GetOutlineWidth(
                    false,
                    false) &&
                TetherfallThreatPresentationRules.GetOutlineWidth(
                    false,
                    true) >
                TetherfallThreatPresentationRules.GetOutlineWidth(
                    false,
                    false) &&
                presentation.Contains("DrawArchetypeMark(") &&
                presentation.Contains("DrawRectOutline(") &&
                presentation.Contains(
                    "TetherfallAccessibilityColorCue.Hostile") &&
                presentation.Contains(
                    "TetherfallAccessibilityRuntime.ShouldReinforceColorCues"),
                "Focus, grayscale, and reinforced cues must remain readable through thickness and shape.");
        }

        private static void VerifyOutlinePoolAndDrawPath(
            string actors,
            string adventure,
            string combat,
            string presentation,
            string game,
            string roaming,
            string bounties,
            string shader,
            string shaderMeta,
            string graphicsSettings)
        {
            Assert(
                TetherfallThreatPresentationRules.ActualSharedMaterialCount <=
                TetherfallThreatPresentationRules.MaximumSharedMaterials &&
                TetherfallThreatPresentationRules.MaximumSharedMaterials <= 3,
                "Threat presentation exceeds the shared-material cap.");
            Assert(
                CountOccurrences(presentation, "new Material(") ==
                TetherfallThreatPresentationRules.ActualSharedMaterialCount &&
                presentation.Contains(
                    "new OutlineSlot[TetherfallThreatPresentationRules.MaximumOutlineHullRenderers]") &&
                presentation.Contains("filter.sharedMesh = outlineMesh;") &&
                presentation.Contains(
                    "hullRenderer.sharedMaterial = outlineMaterial;") &&
                presentation.Contains("new MaterialPropertyBlock()") &&
                presentation.Contains(
                    "slot.Renderer.SetPropertyBlock(slot.PropertyBlock);"),
                "Outline hulls must use one shared mesh, shared material, and per-slot property blocks.");
            Assert(
                !presentation.Contains("GameObject.CreatePrimitive") &&
                !presentation.Contains("AddComponent<Collider") &&
                !presentation.Contains("AddComponent<Light") &&
                !presentation.Contains("GetComponentsInChildren<Renderer>") &&
                !presentation.Contains("Physics.") &&
                !presentation.Contains(".material =") &&
                !presentation.Contains("FindObjects"),
                "Threat presentation introduced a full renderer copy, forbidden component, physics query, or material instance.");
            Assert(
                !presentation.Contains("\u2014") &&
                presentation.IndexOf(
                    "Palworld",
                    StringComparison.OrdinalIgnoreCase) < 0,
                "Threat presentation contains forbidden UI punctuation or copied product wording.");
            Assert(
                shader.Contains("Shader \"Tetherfall/Hostile Outline\"") &&
                shader.Contains("Cull Front") &&
                shader.Contains("ZWrite Off") &&
                shader.Contains("#pragma vertex Vert") &&
                !shader.Contains("Lighting"),
                "Hostile outline must remain a Built-in RP unlit Cull Front shader.");
            var shaderGuid = ReadMetaGuid(shaderMeta);
            Assert(
                !string.IsNullOrEmpty(shaderGuid) &&
                graphicsSettings.Contains(
                    $"{{fileID: 4800000, guid: {shaderGuid}, type: 3}}"),
                "Hostile outline shader is not retained in GraphicsSettings for player builds.");

            var directorType = ExtractTypeBlock(
                presentation,
                "public sealed class TetherfallThreatPresentation");
            Assert(
                CountOccurrences(directorType, "private void Update()") == 1 &&
                CountOccurrences(directorType, "private void OnGUI()") == 1,
                "Threat presentation must remain one director update and one batched OnGUI path.");
            var updateBlock =
                ExtractBlock(directorType, "private void Update()");
            Assert(
                !updateBlock.Contains("Find") &&
                !updateBlock.Contains("Physics.") &&
                !updateBlock.Contains("new "),
                "The director Update introduced scene searches, physics queries, or allocations.");

            var enemyType =
                ExtractTypeBlock(actors, "public sealed class Enemy");
            var bossType =
                ExtractTypeBlock(adventure, "public sealed class DungeonBoss");
            var indicatorType = ExtractTypeBlock(
                combat,
                "public sealed class CombatTargetIndicator");
            var roamingSpawn = ExtractBlock(
                roaming,
                "private bool SpawnHostileMember");
            var bountyActivation = ExtractBlock(
                bounties,
                "public bool Activate()");
            Assert(
                !enemyType.Contains("OnGUI(") &&
                !bossType.Contains("OnGUI("),
                "Enemy or DungeonBoss contains a per-combatant OnGUI path.");
            Assert(
                indicatorType.Contains("Combat focus ring") &&
                !indicatorType.Contains("statusLabel") &&
                !indicatorType.Contains("EXPOSED +") &&
                !indicatorType.Contains("STAGGERED"),
                "CombatTargetIndicator must keep its focus ring without duplicate floating status labels.");
            Assert(
                !enemyType.Contains("WorldVisuals.AddLabel") &&
                !bossType.Contains("WorldVisuals.AddLabel") &&
                !roamingSpawn.Contains("WorldVisuals.AddLabel") &&
                !bountyActivation.Contains("WorldVisuals.AddLabel") &&
                !game.Contains("DrawBossPanel("),
                "Persistent hostile names or the old boss panel still duplicate the threat director.");
        }

        private static void VerifyLayoutContracts()
        {
            Assert(
                Mathf.Approximately(
                    TetherfallThreatPresentationRules.MinimumLayoutWidth,
                    1280f) &&
                Mathf.Approximately(
                    TetherfallThreatPresentationRules.MinimumLayoutHeight,
                    720f) &&
                Mathf.Approximately(
                    TetherfallThreatPresentationRules.LargeLayoutWidth,
                    1920f) &&
                Mathf.Approximately(
                    TetherfallThreatPresentationRules.LargeLayoutHeight,
                    1080f),
                "Threat layout source contracts must explicitly retain 1280x720 and 1920x1080.");
            VerifyLayoutAt(1280f, 720f);
            VerifyLayoutAt(1920f, 1080f);
        }

        private static void VerifyLayoutAt(float width, float height)
        {
            var boss =
                TetherfallThreatPresentationRules.GetBossStripRect(
                    width,
                    height);
            Assert(
                TetherfallThreatPresentationRules.IsInsideViewport(
                    boss,
                    width,
                    height) &&
                boss.width >= 380f &&
                boss.height >= 86f,
                $"{width:0}x{height:0} boss strip is clipped or too small.");

            var plate =
                TetherfallThreatPresentationRules.GetPlateRect(
                    new Vector2(width - 1f, height - 1f),
                    width,
                    height,
                    boss.yMax + 8f);
            var oppositePlate =
                TetherfallThreatPresentationRules.GetPlateRect(
                    Vector2.one,
                    width,
                    height,
                    boss.yMax + 8f);
            Assert(
                TetherfallThreatPresentationRules.IsInsideViewport(
                    plate,
                    width,
                    height) &&
                TetherfallThreatPresentationRules.IsInsideViewport(
                    oppositePlate,
                    width,
                    height) &&
                plate.yMin >= boss.yMax &&
                oppositePlate.yMin >= boss.yMax,
                $"{width:0}x{height:0} threat plates escape the safe HUD region.");

            var pip = TetherfallThreatPresentationRules.ClampEdgePip(
                new Vector2(-1000f, height + 1000f),
                width,
                height);
            Assert(
                pip.x >= 0f &&
                pip.x <= width &&
                pip.y >= 0f &&
                pip.y <= height,
                $"{width:0}x{height:0} offscreen pip clamp escapes the viewport.");
        }

        private static void VerifyFullGameChain(string fullGame)
        {
            var commitIndex = fullGame.IndexOf(
                "TetherfallCommitCounterVerification.VerifyForFullGame",
                StringComparison.Ordinal);
            var threatIndex = fullGame.IndexOf(
                "TetherfallThreatPresentationVerification.VerifyForFullGame",
                StringComparison.Ordinal);
            Assert(
                commitIndex >= 0 &&
                threatIndex > commitIndex,
                "Full-game verification must run Commit & Counter before threat presentation.");
        }

        private static void RequireReadOnlyProperty(
            Type owner,
            string propertyName,
            Type propertyType)
        {
            var property = owner.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert(
                property != null &&
                property.PropertyType == propertyType &&
                property.CanRead &&
                property.SetMethod == null,
                $"{owner.Name}.{propertyName} must be a public read-only {propertyType.Name} property.");
        }

        private static string ReadSource(string folder, string fileName)
        {
            var path = Path.Combine(Application.dataPath, folder, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Threat presentation verification source is missing: {path}",
                    path);
            }

            return File.ReadAllText(path);
        }

        private static string ReadProjectSource(
            string folder,
            string fileName)
        {
            var projectRoot =
                Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var path = Path.Combine(projectRoot, folder, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Threat presentation project source is missing: {path}",
                    path);
            }

            return File.ReadAllText(path);
        }

        private static string ReadMetaGuid(string meta)
        {
            const string token = "guid:";
            var index = meta.IndexOf(token, StringComparison.Ordinal);
            if (index < 0)
            {
                return string.Empty;
            }

            var start = index + token.Length;
            var end = meta.IndexOfAny(
                new[] { '\r', '\n' },
                start);
            var value = end < 0
                ? meta.Substring(start)
                : meta.Substring(start, end - start);
            return value.Trim();
        }

        private static string ExtractTypeBlock(
            string source,
            string typeToken)
        {
            return ExtractBlock(source, typeToken);
        }

        private static string ExtractBlock(string source, string token)
        {
            var tokenIndex =
                source.IndexOf(token, StringComparison.Ordinal);
            Assert(
                tokenIndex >= 0,
                $"Required threat source token is missing: {token}");
            var braceIndex = source.IndexOf('{', tokenIndex);
            Assert(
                braceIndex >= 0,
                $"Required threat source block has no opening brace: {token}");
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
                        return source.Substring(
                            tokenIndex,
                            index - tokenIndex + 1);
                    }
                }
            }

            throw new InvalidOperationException(
                $"Required threat source block has no closing brace: {token}");
        }

        private static int CountOccurrences(
            string source,
            string token)
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
