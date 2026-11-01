using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    /// <summary>
    /// Editor and command-line verification for the optional art-overhaul integration.
    /// The integration is discovered through reflection so this file can compile before it exists.
    /// </summary>
    public static class TetherfallArtOverhaulVerification
    {
        private const int MaxArtRootCount = 32;
        private const int MaxRendererCount = 320;
        private const int MaxUniqueMaterialCount = 96;
        private const int MaxLightCount = 8;

        private static readonly string[] PreferredBuilderTypeNames =
        {
            "Tetherfall.TetherfallArtOverhaulBuilder",
            "Tetherfall.ArtOverhaulBuilder",
            "Tetherfall.TetherfallArtOverhaul",
            "TetherfallArtOverhaulBuilder"
        };

        [MenuItem("Tetherfall/Verify Art Overhaul")]
        public static void VerifyArtOverhaul()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Art-overhaul verification must run in Edit Mode.");
            }

            var cleanup = VerificationCleanup.Capture();
            var resetFixtureStatics = false;
            try
            {
                var buildHook = FindBuildHook();
                if (buildHook == null)
                {
                    var existingRoots = FindArtRoots(null);
                    if (existingRoots.Count == 0)
                    {
                        Debug.LogWarning(
                            "Art-overhaul verification skipped: the optional Build integration " +
                            "and art-only overhaul roots are not present.");
                        return;
                    }

                    var existingSnapshot = VerifyCurrentState(existingRoots, "existing scene");
                    Debug.Log(
                        "Art-overhaul structural verification passed without an installed Build hook: " +
                        existingSnapshot.Describe() + ". Duplicate Build verification was unavailable.");
                    return;
                }

                var fixture = CreateFixture(buildHook);
                resetFixtureStatics = fixture.OwnsGameContext;
                var returnedRoots = new List<GameObject>();

                CollectReturnedRoots(InvokeBuild(buildHook, fixture), returnedRoots);
                var firstRoots = FindArtRoots(returnedRoots);
                Assert(
                    firstRoots.Count > 0,
                    buildHook.DisplayName + " did not create a discoverable art-only overhaul root.");
                var firstSnapshot = VerifyCurrentState(firstRoots, "first Build call");

                CollectReturnedRoots(InvokeBuild(buildHook, fixture), returnedRoots);
                var secondRoots = FindArtRoots(returnedRoots);
                Assert(
                    secondRoots.Count > 0,
                    buildHook.DisplayName + " removed every art-only overhaul root on its second call.");
                var secondSnapshot = VerifyCurrentState(secondRoots, "second Build call");

                Assert(
                    secondSnapshot.RootCount <= firstSnapshot.RootCount,
                    "A duplicate Build call increased the art-root count from " +
                    firstSnapshot.RootCount + " to " + secondSnapshot.RootCount + ".");
                Assert(
                    secondSnapshot.RendererCount <= firstSnapshot.RendererCount,
                    "A duplicate Build call increased the renderer count from " +
                    firstSnapshot.RendererCount + " to " + secondSnapshot.RendererCount + ".");
                Assert(
                    secondSnapshot.MaterialCount <= firstSnapshot.MaterialCount,
                    "A duplicate Build call increased the unique material count from " +
                    firstSnapshot.MaterialCount + " to " + secondSnapshot.MaterialCount + ".");
                Assert(
                    secondSnapshot.LightCount <= firstSnapshot.LightCount,
                    "A duplicate Build call increased the light count from " +
                    firstSnapshot.LightCount + " to " + secondSnapshot.LightCount + ".");

                Debug.Log(
                    "Art-overhaul verification passed via " + buildHook.DisplayName + ": " +
                    secondSnapshot.Describe() +
                    "; duplicate Build calls remained bounded and duplicate-free.");
            }
            finally
            {
                cleanup.Restore(resetFixtureStatics);
            }
        }

        private static VerificationSnapshot VerifyCurrentState(
            List<GameObject> artRoots,
            string phase)
        {
            Assert(
                artRoots.Count <= MaxArtRootCount,
                phase + " exceeded the art-root budget (" + artRoots.Count + "/" +
                MaxArtRootCount + ").");
            AssertNoDuplicateLogicalRoots(artRoots, phase);

            var renderers = new Dictionary<int, Renderer>();
            var materials = new Dictionary<int, Material>();
            var lights = new Dictionary<int, Light>();
            var colliders = new Dictionary<int, Component>();

            foreach (var root in artRoots)
            {
                Assert(root != null, phase + " contained a missing art root.");

                foreach (var itemCollider in root.GetComponentsInChildren<Collider>(true))
                {
                    if (itemCollider != null)
                    {
                        colliders[itemCollider.GetInstanceID()] = itemCollider;
                    }
                }

                foreach (var itemCollider in root.GetComponentsInChildren<Collider2D>(true))
                {
                    if (itemCollider != null)
                    {
                        colliders[itemCollider.GetInstanceID()] = itemCollider;
                    }
                }

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderers[renderer.GetInstanceID()] = renderer;
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials[material.GetInstanceID()] = material;
                        }
                    }
                }

                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light != null)
                    {
                        lights[light.GetInstanceID()] = light;
                    }
                }
            }

            Assert(
                colliders.Count == 0,
                phase + " placed " + colliders.Count +
                " gameplay collider(s) beneath art-only overhaul roots: " +
                DescribeComponents(colliders.Values) + ".");
            Assert(
                renderers.Count > 0,
                phase + " did not contain a renderer beneath any art-only overhaul root.");
            Assert(
                renderers.Count <= MaxRendererCount,
                phase + " exceeded the renderer budget (" + renderers.Count + "/" +
                MaxRendererCount + ").");
            Assert(
                materials.Count <= MaxUniqueMaterialCount,
                phase + " exceeded the unique material budget (" + materials.Count + "/" +
                MaxUniqueMaterialCount + ").");
            Assert(
                lights.Count <= MaxLightCount,
                phase + " exceeded the light budget (" + lights.Count + "/" +
                MaxLightCount + ").");

            VerifyActorArtRoots(artRoots, phase);
            return new VerificationSnapshot(
                artRoots.Count,
                renderers.Count,
                materials.Count,
                lights.Count);
        }

        private static void VerifyActorArtRoots(List<GameObject> artRoots, string phase)
        {
            VerifyActorArtRoot(
                artRoots,
                FindSceneComponents("Tetherfall.PlayerAvatar"),
                new[] { "player", "wayfinder" },
                "player",
                phase);
            VerifyActorArtRoot(
                artRoots,
                FindSceneComponents("Tetherfall.PactCreature"),
                new[] { "pact", "echosworn", "creature" },
                "Pact",
                phase);
            VerifyActorArtRoot(
                artRoots,
                FindSceneComponents("Tetherfall.Enemy"),
                new[] { "enemy", "hostile" },
                "enemy",
                phase);
        }

        private static void VerifyActorArtRoot(
            List<GameObject> artRoots,
            List<Component> actors,
            string[] categoryTokens,
            string categoryLabel,
            string phase)
        {
            if (actors.Count == 0)
            {
                return;
            }

            foreach (var root in artRoots)
            {
                foreach (var actor in actors)
                {
                    if (actor != null && root.transform.IsChildOf(actor.transform))
                    {
                        return;
                    }
                }

                if (HierarchyContainsAnyToken(root.transform, categoryTokens))
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                phase + " found " + actors.Count + " " + categoryLabel +
                " actor(s), but no corresponding art-only overhaul root.");
        }

        private static bool HierarchyContainsAnyToken(Transform root, string[] tokens)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                var normalizedName = Normalize(item.name);
                foreach (var token in tokens)
                {
                    if (normalizedName.Contains(Normalize(token)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AssertNoDuplicateLogicalRoots(
            List<GameObject> artRoots,
            string phase)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in artRoots)
            {
                var parentKey = root.transform.parent != null
                    ? root.transform.parent.GetInstanceID().ToString()
                    : "scene:" + root.scene.handle;
                var key = parentKey + "|" + Normalize(root.name);
                Assert(
                    keys.Add(key),
                    phase + " contained duplicate art roots named '" + root.name +
                    "' beneath the same parent.");
            }
        }

        private static List<GameObject> FindArtRoots(List<GameObject> returnedRoots)
        {
            var roots = new Dictionary<int, GameObject>();
            if (returnedRoots != null)
            {
                foreach (var returnedRoot in returnedRoots)
                {
                    if (IsSceneObject(returnedRoot))
                    {
                        roots[returnedRoot.GetInstanceID()] = returnedRoot;
                    }
                }
            }

            foreach (var gameObject in GetSceneGameObjects())
            {
                if (LooksLikeArtRoot(gameObject))
                {
                    roots[gameObject.GetInstanceID()] = gameObject;
                }
            }

            var result = new List<GameObject>(roots.Values);
            result.Sort((left, right) =>
                string.CompareOrdinal(GetHierarchyPath(left.transform), GetHierarchyPath(right.transform)));
            return result;
        }

        private static bool LooksLikeArtRoot(GameObject gameObject)
        {
            var normalizedName = Normalize(gameObject.name);
            if (normalizedName.Contains("artoverhaul") ||
                normalizedName.Contains("overhaulart") ||
                normalizedName.Contains("artonly") ||
                normalizedName.Contains("artroot") ||
                (normalizedName.Contains("overhaul") &&
                 (normalizedName.Contains("visual") ||
                  normalizedName.Contains("wayfinder") ||
                  normalizedName.Contains("pact") ||
                  normalizedName.Contains("enemy"))))
            {
                return true;
            }

            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                var typeName = Normalize(component.GetType().Name);
                if (typeName.Contains("artoverhaul") &&
                    (typeName.Contains("root") || typeName.Contains("marker")))
                {
                    return true;
                }
            }

            return false;
        }

        private static BuildHook FindBuildHook()
        {
            for (var index = 0; index < PreferredBuilderTypeNames.Length; index++)
            {
                var type = FindType(PreferredBuilderTypeNames[index]);
                var hook = FindBuildHook(type);
                if (hook != null)
                {
                    return hook;
                }
            }

            BuildHook best = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (type == null || type == typeof(TetherfallArtOverhaulVerification))
                    {
                        continue;
                    }

                    var normalizedName = Normalize(type.FullName ?? type.Name);
                    if (!normalizedName.Contains("art") || !normalizedName.Contains("overhaul"))
                    {
                        continue;
                    }

                    var hook = FindBuildHook(type);
                    if (hook != null && (best == null || hook.Score > best.Score))
                    {
                        best = hook;
                    }
                }
            }

            return best;
        }

        private static BuildHook FindBuildHook(Type type)
        {
            if (type == null)
            {
                return null;
            }

            BuildHook best = null;
            foreach (var method in type.GetMethods(
                         BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, "Build", StringComparison.Ordinal) ||
                    !CanInvokeBuild(method))
                {
                    continue;
                }

                var score = 0;
                if (type.Name.EndsWith("Builder", StringComparison.Ordinal))
                {
                    score += 20;
                }

                if (method.IsPublic)
                {
                    score += 10;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 1 &&
                    parameters[0].ParameterType.Name == "TetherfallGame")
                {
                    score += 10;
                }

                var hook = new BuildHook(type, method, score);
                if (best == null || hook.Score > best.Score)
                {
                    best = hook;
                }
            }

            return best;
        }

        private static bool CanInvokeBuild(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length > 1)
            {
                return false;
            }

            if (parameters.Length == 0)
            {
                return true;
            }

            var parameter = parameters[0];
            if (parameter.IsOut || parameter.ParameterType.IsByRef)
            {
                return false;
            }

            var type = parameter.ParameterType;
            return type == typeof(GameObject) ||
                   type == typeof(Transform) ||
                   type == typeof(object) ||
                   typeof(Component).IsAssignableFrom(type) ||
                   parameter.IsOptional;
        }

        private static VerificationFixture CreateFixture(BuildHook buildHook)
        {
            var fixtureRoot = new GameObject("Tetherfall Visual Verification Fixture");
            var fixture = new VerificationFixture(fixtureRoot);

            var gameType = FindType("Tetherfall.TetherfallGame");
            if (gameType != null && typeof(Component).IsAssignableFrom(gameType))
            {
                var existingGames = FindSceneComponents(gameType);
                if (existingGames.Count > 0)
                {
                    fixture.GameContext = existingGames[0];
                }
                else
                {
                    var gameObject = new GameObject("Verification Game Context");
                    gameObject.transform.SetParent(fixtureRoot.transform, false);
                    fixture.GameContext = gameObject.AddComponent(gameType);
                    fixture.OwnsGameContext = true;
                    CreateActorFixtures(fixture);
                }
            }

            fixture.BuildArgument = ResolveBuildArgument(buildHook.Method, fixture);
            return fixture;
        }

        private static object ResolveBuildArgument(
            MethodInfo buildMethod,
            VerificationFixture fixture)
        {
            var parameters = buildMethod.GetParameters();
            if (parameters.Length == 0)
            {
                return null;
            }

            var parameter = parameters[0];
            var type = parameter.ParameterType;
            if (type == typeof(GameObject))
            {
                return fixture.Root;
            }

            if (type == typeof(Transform))
            {
                return fixture.Root.transform;
            }

            if (fixture.GameContext != null && type.IsInstanceOfType(fixture.GameContext))
            {
                return fixture.GameContext;
            }

            if (type == typeof(object))
            {
                return fixture.GameContext != null ? (object)fixture.GameContext : fixture.Root;
            }

            if (typeof(Component).IsAssignableFrom(type) && !type.IsAbstract)
            {
                var existing = FindSceneComponents(type);
                if (existing.Count > 0)
                {
                    return existing[0];
                }

                var contextObject = new GameObject("Verification " + type.Name);
                contextObject.transform.SetParent(fixture.Root.transform, false);
                return contextObject.AddComponent(type);
            }

            if (parameter.IsOptional)
            {
                return parameter.DefaultValue;
            }

            throw new InvalidOperationException(
                "Cannot supply parameter '" + parameter.Name + "' for " +
                buildMethod.DeclaringType?.FullName + ".Build.");
        }

        private static void CreateActorFixtures(VerificationFixture fixture)
        {
            CreatePlayerFixture(fixture);
            CreatePactFixture(fixture);
            CreateEnemyFixture(fixture);
        }

        private static void CreatePlayerFixture(VerificationFixture fixture)
        {
            var playerType = FindType("Tetherfall.PlayerAvatar");
            if (playerType == null || !typeof(Component).IsAssignableFrom(playerType))
            {
                return;
            }

            var playerObject = new GameObject("Verification Wayfinder");
            playerObject.transform.SetParent(fixture.Root.transform, false);
            var player = playerObject.AddComponent(playerType);
            var initialize = FindSingleArgumentMethod(
                playerType,
                "Initialize",
                fixture.GameContext.GetType());
            Assert(initialize != null, "PlayerAvatar.Initialize was unavailable to the verifier.");
            InvokeReflected(initialize, player, new object[] { fixture.GameContext });

            var playerProperty = fixture.GameContext.GetType().GetProperty(
                "Player",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var setter = playerProperty?.GetSetMethod(true);
            if (setter != null && playerProperty.PropertyType.IsInstanceOfType(player))
            {
                InvokeReflected(setter, fixture.GameContext, new object[] { player });
            }
        }

        private static void CreatePactFixture(VerificationFixture fixture)
        {
            var pactType = FindType("Tetherfall.PactCreature");
            if (pactType == null)
            {
                return;
            }

            MethodInfo factory = null;
            foreach (var method in pactType.GetMethods(
                         BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, "Create", StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length >= 6 &&
                    parameters[0].ParameterType.IsInstanceOfType(fixture.GameContext) &&
                    HasParameter(parameters, typeof(Vector3)) &&
                    HasParameter(parameters, typeof(Color)))
                {
                    factory = method;
                    break;
                }
            }

            Assert(factory != null, "PactCreature.Create was unavailable to the verifier.");
            var result = InvokeReflected(
                factory,
                null,
                CreateFactoryArguments(
                    factory.GetParameters(),
                    fixture.GameContext,
                    "Verification Pact",
                    new Vector3(3f, 0f, 0f),
                    new Color(0.28f, 0.74f, 0.48f),
                    10));
            ParentReturnedObject(result, fixture.Root.transform);
        }

        private static void CreateEnemyFixture(VerificationFixture fixture)
        {
            var enemyType = FindType("Tetherfall.Enemy");
            if (enemyType == null)
            {
                return;
            }

            MethodInfo factory = null;
            foreach (var method in enemyType.GetMethods(
                         BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, "Create", StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length >= 6 &&
                    parameters[0].ParameterType.IsInstanceOfType(fixture.GameContext) &&
                    HasParameter(parameters, typeof(Vector3)) &&
                    HasParameter(parameters, typeof(Color)) &&
                    HasParameter(parameters, typeof(int)))
                {
                    factory = method;
                    break;
                }
            }

            Assert(factory != null, "Enemy.Create runtime overload was unavailable to the verifier.");
            var result = InvokeReflected(
                factory,
                null,
                CreateFactoryArguments(
                    factory.GetParameters(),
                    fixture.GameContext,
                    "Verification Enemy",
                    new Vector3(6f, 0f, 0f),
                    new Color(0.72f, 0.25f, 0.2f),
                    10));
            ParentReturnedObject(result, fixture.Root.transform);
        }

        private static object[] CreateFactoryArguments(
            ParameterInfo[] parameters,
            Component gameContext,
            string displayName,
            Vector3 position,
            Color color,
            int integerValue)
        {
            var arguments = new object[parameters.Length];
            var stringIndex = 0;
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                var type = parameter.ParameterType;
                if (type.IsInstanceOfType(gameContext))
                {
                    arguments[index] = gameContext;
                }
                else if (type == typeof(string))
                {
                    arguments[index] = stringIndex++ == 0
                        ? displayName
                        : "editor verification";
                }
                else if (type == typeof(Vector3))
                {
                    arguments[index] = position;
                }
                else if (type == typeof(Color))
                {
                    arguments[index] = color;
                }
                else if (type == typeof(int))
                {
                    arguments[index] = integerValue;
                }
                else if (type == typeof(bool))
                {
                    arguments[index] = false;
                }
                else if (!type.IsValueType)
                {
                    arguments[index] = null;
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[index] = parameter.DefaultValue;
                }
                else
                {
                    arguments[index] = Activator.CreateInstance(type);
                }
            }

            return arguments;
        }

        private static object InvokeBuild(BuildHook hook, VerificationFixture fixture)
        {
            var parameters = hook.Method.GetParameters();
            var arguments = parameters.Length == 0
                ? null
                : new[] { fixture.BuildArgument };
            return InvokeReflected(hook.Method, null, arguments);
        }

        private static object InvokeReflected(MethodInfo method, object target, object[] arguments)
        {
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException(
                    method.DeclaringType?.FullName + "." + method.Name +
                    " failed during art-overhaul verification.",
                    exception.InnerException ?? exception);
            }
        }

        private static void CollectReturnedRoots(object value, List<GameObject> roots)
        {
            if (value == null)
            {
                return;
            }

            if (value is GameObject gameObject)
            {
                AddReturnedRoot(gameObject, roots);
                return;
            }

            if (value is Component component)
            {
                AddReturnedRoot(component.gameObject, roots);
                return;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (var item in enumerable)
                {
                    CollectReturnedRoots(item, roots);
                }
            }
        }

        private static void AddReturnedRoot(GameObject root, List<GameObject> roots)
        {
            if (root == null)
            {
                return;
            }

            foreach (var existing in roots)
            {
                if (existing == root)
                {
                    return;
                }
            }

            roots.Add(root);
        }

        private static void ParentReturnedObject(object value, Transform parent)
        {
            if (value is Component component && component != null)
            {
                component.transform.SetParent(parent, true);
            }
            else if (value is GameObject gameObject && gameObject != null)
            {
                gameObject.transform.SetParent(parent, true);
            }
        }

        private static MethodInfo FindSingleArgumentMethod(
            Type type,
            string methodName,
            Type argumentType)
        {
            foreach (var method in type.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var parameters = method.GetParameters();
                if (string.Equals(method.Name, methodName, StringComparison.Ordinal) &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType.IsAssignableFrom(argumentType))
                {
                    return method;
                }
            }

            return null;
        }

        private static bool HasParameter(ParameterInfo[] parameters, Type type)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                var types = new List<Type>();
                foreach (var type in exception.Types)
                {
                    if (type != null)
                    {
                        types.Add(type);
                    }
                }

                return types;
            }
        }

        private static List<Component> FindSceneComponents(string fullTypeName)
        {
            var type = FindType(fullTypeName);
            return type == null ? new List<Component>() : FindSceneComponents(type);
        }

        private static List<Component> FindSceneComponents(Type type)
        {
            var result = new List<Component>();
            foreach (var gameObject in GetSceneGameObjects())
            {
                var component = gameObject.GetComponent(type);
                if (component != null)
                {
                    result.Add(component);
                }
            }

            return result;
        }

        private static List<GameObject> GetSceneGameObjects()
        {
            var result = new List<GameObject>();
            foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (IsSceneObject(gameObject))
                {
                    result.Add(gameObject);
                }
            }

            return result;
        }

        private static bool IsSceneObject(GameObject gameObject)
        {
            return gameObject != null &&
                   gameObject.scene.IsValid() &&
                   gameObject.scene.isLoaded &&
                   !EditorUtility.IsPersistent(gameObject);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var characters = new char[value.Length];
            var length = 0;
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    characters[length++] = char.ToLowerInvariant(character);
                }
            }

            return new string(characters, 0, length);
        }

        private static string DescribeComponents(IEnumerable<Component> components)
        {
            var descriptions = new List<string>();
            foreach (var component in components)
            {
                if (component == null)
                {
                    continue;
                }

                descriptions.Add(
                    component.GetType().Name + " at " + GetHierarchyPath(component.transform));
                if (descriptions.Count == 4)
                {
                    break;
                }
            }

            return descriptions.Count == 0 ? "unknown" : string.Join(", ", descriptions);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class BuildHook
        {
            public readonly MethodInfo Method;
            public readonly int Score;
            public readonly string DisplayName;

            public BuildHook(Type type, MethodInfo method, int score)
            {
                Method = method;
                Score = score;
                DisplayName = (type.FullName ?? type.Name) + "." + method.Name;
            }
        }

        private sealed class VerificationFixture
        {
            public readonly GameObject Root;
            public Component GameContext;
            public bool OwnsGameContext;
            public object BuildArgument;

            public VerificationFixture(GameObject root)
            {
                Root = root;
            }
        }

        private readonly struct VerificationSnapshot
        {
            public readonly int RootCount;
            public readonly int RendererCount;
            public readonly int MaterialCount;
            public readonly int LightCount;

            public VerificationSnapshot(
                int rootCount,
                int rendererCount,
                int materialCount,
                int lightCount)
            {
                RootCount = rootCount;
                RendererCount = rendererCount;
                MaterialCount = materialCount;
                LightCount = lightCount;
            }

            public string Describe()
            {
                return RootCount + " art root(s), " +
                       RendererCount + " renderer(s), " +
                       MaterialCount + " unique material(s), and " +
                       LightCount + " light(s)";
            }
        }

        private sealed class VerificationCleanup
        {
            private readonly HashSet<int> gameObjectIds;
            private readonly HashSet<int> componentIds;
            private readonly HashSet<int> materialIds;

            private VerificationCleanup(
                HashSet<int> existingGameObjectIds,
                HashSet<int> existingComponentIds,
                HashSet<int> existingMaterialIds)
            {
                gameObjectIds = existingGameObjectIds;
                componentIds = existingComponentIds;
                materialIds = existingMaterialIds;
            }

            public static VerificationCleanup Capture()
            {
                var objects = new HashSet<int>();
                var components = new HashSet<int>();
                foreach (var gameObject in GetSceneGameObjects())
                {
                    objects.Add(gameObject.GetInstanceID());
                    foreach (var component in gameObject.GetComponents<Component>())
                    {
                        if (component != null)
                        {
                            components.Add(component.GetInstanceID());
                        }
                    }
                }

                var materials = new HashSet<int>();
                foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
                {
                    if (material != null)
                    {
                        materials.Add(material.GetInstanceID());
                    }
                }

                return new VerificationCleanup(objects, components, materials);
            }

            public void Restore(bool resetRuntimeStatics)
            {
                var addedObjects = new List<GameObject>();
                foreach (var gameObject in GetSceneGameObjects())
                {
                    if (!gameObjectIds.Contains(gameObject.GetInstanceID()))
                    {
                        addedObjects.Add(gameObject);
                    }
                }

                addedObjects.Sort((left, right) =>
                    GetDepth(right.transform).CompareTo(GetDepth(left.transform)));
                foreach (var gameObject in addedObjects)
                {
                    if (gameObject != null)
                    {
                        Object.DestroyImmediate(gameObject);
                    }
                }

                foreach (var gameObject in GetSceneGameObjects())
                {
                    foreach (var component in gameObject.GetComponents<Component>())
                    {
                        if (component != null &&
                            !(component is Transform) &&
                            !componentIds.Contains(component.GetInstanceID()))
                        {
                            Object.DestroyImmediate(component);
                        }
                    }
                }

                foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
                {
                    if (material != null &&
                        !materialIds.Contains(material.GetInstanceID()) &&
                        !EditorUtility.IsPersistent(material))
                    {
                        Object.DestroyImmediate(material);
                    }
                }

                if (resetRuntimeStatics)
                {
                    ResetStatics("Tetherfall.Enemy");
                    ResetStatics("Tetherfall.TetherfallGame");
                }
            }

            private static int GetDepth(Transform transform)
            {
                var depth = 0;
                while (transform.parent != null)
                {
                    depth++;
                    transform = transform.parent;
                }

                return depth;
            }

            private static void ResetStatics(string typeName)
            {
                try
                {
                    var type = FindType(typeName);
                    var method = type?.GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null && method.GetParameters().Length == 0)
                    {
                        method.Invoke(null, null);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "Art-overhaul verification cleanup could not reset " + typeName +
                        ": " + exception.Message);
                }
            }
        }
    }
}
