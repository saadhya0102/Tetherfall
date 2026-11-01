using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    public static class TetherfallCampPresentationVerification
    {
        private const string MenuPath = "Tetherfall/Verify Enemy Camp Presentation";

        private static readonly ArchetypeCase[] Cases =
        {
            new ArchetypeCase(
                "encounter.alderwake.roadblock",
                TetherfallCampArchetype.RaiderRoadblock,
                new[] { "enemy.hollow_raider", "enemy.hollow_scout" },
                "roadblock palisade", "raider shelter", "watch platform", "supply stack",
                "weapon rack", "cart", "cookfire", "banner"),
            new ArchetypeCase(
                "encounter.alderwake.forest_salvage",
                TetherfallCampArchetype.ForestSalvageCamp,
                new[] { "enemy.mossglass_stalker", "enemy.mossglass_stalker", "enemy.hollow_scout" },
                "salvage shelter", "salvage lookout", "salvage cart", "log pile",
                "supply", "weapon rack", "cookfire", "banner"),
            new ArchetypeCase(
                "encounter.morrowfen.fen_tithe",
                TetherfallCampArchetype.FenStiltCamp,
                new[] { "enemy.fen_spitter", "enemy.fen_siltcaller", "enemy.hollow_raider" },
                "stilt deck", "bridge", "fen shelter", "reed screen", "supply",
                "weapon rack", "cookfire", "banner"),
            new ArchetypeCase(
                "encounter.morrowfen.sunken_watch",
                TetherfallCampArchetype.RuinedWatchPost,
                new[] { "enemy.fen_spitter", "enemy.hollow_raider", "enemy.mossglass_stalker" },
                "ruined watch platform", "ruined north wall", "ruined watch arch",
                "scavenger shelter", "rubble", "supply", "weapon rack", "cookfire"),
            new ArchetypeCase(
                "encounter.cinderwold.ember_quarry",
                TetherfallCampArchetype.CinderwoldQuarryForgeCamp,
                new[] { "enemy.cinder_echo", "enemy.cinder_echo", "enemy.vault_wisp" },
                "quarry scaffold", "forge", "crew shelter", "retaining wall", "ore hoist",
                "ore cart", "ore pile", "weapon rack", "banner"),
            new ArchetypeCase(
                "encounter.cinderwold.elite_war",
                TetherfallCampArchetype.EliteWarCamp,
                new[] { "enemy.cinder_echo", "enemy.relay_breaker", "enemy.ashjaw_brute" },
                "command shelter", "elite north west palisade", "west watch platform",
                "east watch platform", "supply stack", "weapon rack", "baggage cart",
                "cookfire", "banner", "command table")
        };

        [MenuItem(MenuPath, false, 2210)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Enemy-camp presentation verification is edit-mode only.");
            }

            var fixtureRoot = new GameObject("Camp Presentation Verification");
            var gameObject = new GameObject("Camp Presentation Game Context");
            var game = gameObject.AddComponent<TetherfallGame>();
            var summaries = new List<string>();
            try
            {
                for (var index = 0; index < Cases.Length; index++)
                {
                    VerifyCase(game, fixtureRoot.transform, Cases[index], index, summaries);
                }

                VerifyIdentityOnlyOverload(game, fixtureRoot.transform);
                Debug.Log(
                    "Enemy-camp presentation verification passed for all six archetypes: " +
                    string.Join("; ", summaries) +
                    ". Duplicate builds, finite transforms, open approaches, collider cleanup, " +
                    "forbidden components, and hard budgets verified.");
            }
            finally
            {
                Object.DestroyImmediate(fixtureRoot);
                Object.DestroyImmediate(gameObject);
            }
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyCase(
            TetherfallGame game,
            Transform fixtureRoot,
            ArchetypeCase testCase,
            int index,
            List<string> summaries)
        {
            var campObject = new GameObject("Camp fixture - " + testCase.Id);
            campObject.transform.SetParent(fixtureRoot, false);
            campObject.transform.position = new Vector3(index * 42f, 0f, index % 2 == 0 ? 0f : 42f);
            var camp = campObject.AddComponent<EnemyCamp>();
            var definition = new EncounterDefinition
            {
                id = testCase.Id,
                displayName = testCase.Expected.ToString(),
                campColor = new Color(0.5f, 0.2f, 0.18f),
                enemyIds = testCase.Roster,
                waves = new[] { testCase.Roster }
            };
            var stableIdentity = testCase.Id + "@" +
                                 Mathf.RoundToInt(campObject.transform.position.x) + ":" +
                                 Mathf.RoundToInt(campObject.transform.position.z);

            var selected = TetherfallCampPresentation.SelectArchetype(
                definition,
                stableIdentity,
                campObject.transform.position);
            Assert(
                selected == testCase.Expected,
                testCase.Id + " selected " + selected + " instead of " + testCase.Expected + ".");

            var first = TetherfallCampPresentation.Build(game, camp, definition, stableIdentity);
            Assert(first != null, testCase.Id + " did not return a presentation root.");
            Assert(first.parent == camp.transform, testCase.Id + " presentation was not parented to its camp.");
            Assert(
                first.name == TetherfallCampPresentation.PresentationRootName,
                testCase.Id + " returned an unexpected root name.");
            var firstSnapshot = VerifyPresentation(first, testCase);

            var second = TetherfallCampPresentation.Build(game, camp, definition, stableIdentity);
            Assert(
                ReferenceEquals(first, second),
                testCase.Id + " returned a different root on its duplicate Build call.");
            var secondSnapshot = Capture(first);
            Assert(
                firstSnapshot.Equals(secondSnapshot),
                testCase.Id + " changed its hierarchy or budgets on a duplicate Build call. First: " +
                firstSnapshot.Describe() + "; second: " + secondSnapshot.Describe() + ".");

            var directPresentationRoots = 0;
            for (var childIndex = 0; childIndex < camp.transform.childCount; childIndex++)
            {
                if (camp.transform.GetChild(childIndex).name ==
                    TetherfallCampPresentation.PresentationRootName)
                {
                    directPresentationRoots++;
                }
            }

            Assert(
                directPresentationRoots == 1,
                testCase.Id + " has " + directPresentationRoots + " direct presentation roots.");
            summaries.Add(testCase.Expected + " [" + secondSnapshot.Describe() + "]");
        }

        private static Snapshot VerifyPresentation(Transform root, ArchetypeCase testCase)
        {
            Physics.SyncTransforms();
            var snapshot = Capture(root);
            Assert(snapshot.Renderers > 0, testCase.Id + " contains no renderers.");
            Assert(
                snapshot.Renderers <= TetherfallCampPresentation.MaxRenderersPerCamp,
                testCase.Id + " exceeded its renderer budget: " + snapshot.Renderers + "/" +
                TetherfallCampPresentation.MaxRenderersPerCamp + ".");
            Assert(
                snapshot.Colliders <= TetherfallCampPresentation.MaxCollidersPerCamp,
                testCase.Id + " exceeded its collider budget: " + snapshot.Colliders + "/" +
                TetherfallCampPresentation.MaxCollidersPerCamp + ".");
            Assert(
                snapshot.Materials <= TetherfallCampPresentation.MaxSharedMaterialsPerCamp,
                testCase.Id + " exceeded its material budget: " + snapshot.Materials + "/" +
                TetherfallCampPresentation.MaxSharedMaterialsPerCamp + ".");
            Assert(
                root.GetComponentsInChildren<Light>(true).Length == 0,
                testCase.Id + " contains a forbidden realtime Light.");

            foreach (var requiredToken in testCase.RequiredTokens)
            {
                Assert(
                    HierarchyContains(root, requiredToken),
                    testCase.Id + " is missing archetype inventory token '" + requiredToken + "'.");
            }

            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                Assert(IsFinite(transform.localPosition), testCase.Id + " has a non-finite local position at " + transform.name + ".");
                Assert(IsFinite(transform.localRotation), testCase.Id + " has a non-finite local rotation at " + transform.name + ".");
                Assert(IsFinite(transform.localScale), testCase.Id + " has a non-finite local scale at " + transform.name + ".");
                Assert(IsFinite(transform.position), testCase.Id + " has a non-finite world position at " + transform.name + ".");
            }

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                var type = behaviour.GetType();
                Assert(
                    !DeclaresFrameCallback(type, "Update") &&
                    !DeclaresFrameCallback(type, "LateUpdate") &&
                    !DeclaresFrameCallback(type, "FixedUpdate"),
                    testCase.Id + " contains a forbidden per-frame component: " + type.FullName + ".");
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                Assert(collider.enabled, testCase.Id + " retained a disabled collider at " + collider.name + ".");
                Assert(collider.gameObject.isStatic, testCase.Id + " collider is not static at " + collider.name + ".");
                var renderer = collider.GetComponent<Renderer>();
                Assert(
                    renderer != null && renderer.enabled,
                    testCase.Id + " has a collider without matching visible architecture at " + collider.name + ".");
                var laneDistance = HorizontalDistanceToBounds(root.position, collider.bounds);
                Assert(
                    laneDistance >= TetherfallCampPresentation.ClearCombatLaneRadius,
                    testCase.Id + " collider at " + collider.name + " enters the clear combat lane (" +
                    laneDistance.ToString("0.00") + "m).");
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                var normalizedName = collider.name.ToLowerInvariant();
                Assert(
                    !normalizedName.Contains("supply") &&
                    !normalizedName.Contains("cart") &&
                    !normalizedName.Contains("banner") &&
                    !normalizedName.Contains("weapon rack") &&
                    !normalizedName.Contains("cookfire") &&
                    !normalizedName.Contains("rubble") &&
                    !normalizedName.Contains("ore pile") &&
                    !normalizedName.Contains("log pile") &&
                    !normalizedName.Contains("reed bundle"),
                    testCase.Id + " left a collider on cosmetic clutter at " + collider.name + ".");
            }

            Assert(
                CountClearApproaches(root) >= 2,
                testCase.Id + " exposes fewer than two unobstructed approaches.");
            return snapshot;
        }

        private static void VerifyIdentityOnlyOverload(TetherfallGame game, Transform fixtureRoot)
        {
            var campObject = new GameObject("Identity-only camp fixture");
            campObject.transform.SetParent(fixtureRoot, false);
            campObject.transform.position = new Vector3(320f, 0f, -80f);
            var presentation = TetherfallCampPresentation.Build(
                game,
                campObject.transform,
                "encounter.morrowfen.identity_only@320:-80",
                campObject.transform.position);
            Assert(presentation != null, "Identity-only Build overload returned no presentation.");
            Assert(
                HierarchyContains(presentation, "Fen Stilt Camp"),
                "Identity-only Build overload did not derive its region/archetype from stable identity.");
            Assert(
                ReferenceEquals(
                    presentation,
                    TetherfallCampPresentation.Build(
                        game,
                        campObject.transform,
                        "encounter.morrowfen.identity_only@320:-80",
                        campObject.transform.position)),
                "Identity-only Build overload was not idempotent.");
        }

        private static Snapshot Capture(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var materials = new HashSet<Material>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            return new Snapshot(
                transforms.Length,
                renderers.Length,
                colliders.Length,
                materials.Count);
        }

        private static int CountClearApproaches(Transform root)
        {
            var directions = new[]
            {
                Vector3.forward,
                (Vector3.forward + Vector3.right).normalized,
                Vector3.right,
                (Vector3.back + Vector3.right).normalized,
                Vector3.back,
                (Vector3.back + Vector3.left).normalized,
                Vector3.left,
                (Vector3.forward + Vector3.left).normalized
            };
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var clear = 0;
            var origin = root.position + Vector3.up;
            foreach (var direction in directions)
            {
                var ray = new Ray(origin, direction);
                var blocked = false;
                foreach (var collider in colliders)
                {
                    if (collider.Raycast(ray, out _, 16f))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    clear++;
                }
            }

            return clear;
        }

        private static float HorizontalDistanceToBounds(Vector3 origin, Bounds bounds)
        {
            var x = Mathf.Max(Mathf.Abs(bounds.center.x - origin.x) - bounds.extents.x, 0f);
            var z = Mathf.Max(Mathf.Abs(bounds.center.z - origin.z) - bounds.extents.z, 0f);
            return Mathf.Sqrt(x * x + z * z);
        }

        private static bool HierarchyContains(Transform root, string token)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DeclaresFrameCallback(Type type, string callback)
        {
            return type.GetMethod(
                       callback,
                       BindingFlags.Instance |
                       BindingFlags.Public |
                       BindingFlags.NonPublic |
                       BindingFlags.DeclaredOnly) != null;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class ArchetypeCase
        {
            public readonly string Id;
            public readonly TetherfallCampArchetype Expected;
            public readonly string[] Roster;
            public readonly string[] RequiredTokens;

            public ArchetypeCase(
                string id,
                TetherfallCampArchetype expected,
                string[] roster,
                params string[] requiredTokens)
            {
                Id = id;
                Expected = expected;
                Roster = roster;
                RequiredTokens = requiredTokens;
            }
        }

        private readonly struct Snapshot : IEquatable<Snapshot>
        {
            public readonly int Transforms;
            public readonly int Renderers;
            public readonly int Colliders;
            public readonly int Materials;

            public Snapshot(int transforms, int renderers, int colliders, int materials)
            {
                Transforms = transforms;
                Renderers = renderers;
                Colliders = colliders;
                Materials = materials;
            }

            public bool Equals(Snapshot other)
            {
                return Transforms == other.Transforms &&
                       Renderers == other.Renderers &&
                       Colliders == other.Colliders &&
                       Materials == other.Materials;
            }

            public override bool Equals(object other)
            {
                return other is Snapshot snapshot && Equals(snapshot);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = Transforms;
                    hash = (hash * 397) ^ Renderers;
                    hash = (hash * 397) ^ Colliders;
                    hash = (hash * 397) ^ Materials;
                    return hash;
                }
            }

            public string Describe()
            {
                return Renderers + " renderers, " + Colliders + " colliders, " +
                       Materials + " materials";
            }
        }
    }
}
