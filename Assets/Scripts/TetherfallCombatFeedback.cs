using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        public void PlayWeaponSwing(WeaponDiscipline weapon, Vector3 position, Vector3 direction, int comboStage)
        {
            combatSwingSequence++;
            TetherfallCombatEffectsV2.PlayWeaponSwing(
                this,
                weapon,
                position,
                direction,
                comboStage);
        }

        public void PlayEnemyHitFeedback(Enemy enemy, int damage)
        {
            if (enemy == null || damage <= 0)
            {
                return;
            }

            var color = enemy.EnemyId.Contains("cinder") || enemy.EnemyId.Contains("ashjaw")
                ? new Color(1f, 0.48f, 0.25f)
                : new Color(0.71f, 0.94f, 0.78f);
            TetherfallCombatEffectsV2.PlayEnemyHit(this, enemy, damage, color);
            RequestCombatHitstop(0.024f + Mathf.Min(0.018f, damage * 0.00045f));
            AddAccessibleCombatImpulse(
                0.055f + Mathf.Min(0.09f, damage * 0.0018f),
                0.1f);
        }

        public void PlayBossHitFeedback(Vector3 position, int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            var hitWeight = Mathf.Clamp01(damage / 100f);
            TetherfallCombatEffectsV2.PlayBossHit(this, position, damage);
            RequestCombatHitstop(0.036f + hitWeight * 0.014f);
            AddAccessibleCombatImpulse(0.1f + hitWeight * 0.045f, 0.14f);
        }

        public void PlayPlayerHitFeedback(int amount)
        {
            if (Player == null || amount <= 0)
            {
                return;
            }

            var hitWeight = Mathf.Clamp01(amount / 40f);
            var position = PlayerTransform.position + Vector3.up * 0.95f;
            TetherfallCombatEffectsV2.PlayPlayerHit(this, position, amount);
            RequestCombatHitstop(0.032f + hitWeight * 0.012f);
            AddAccessibleCombatImpulse(
                0.1f + Mathf.Min(0.11f, amount * 0.005f),
                0.18f);
        }

        public void PlayExpeditionAllyStrikeFeedback(
            Transform ally,
            Transform target,
            Color color,
            string source)
        {
            if (target == null)
            {
                return;
            }

            var sourcePosition = ally != null
                ? ally.position + Vector3.up * 0.8f
                : PlayerTransform.position + Vector3.up * 0.9f;
            CombatFeedback.CreateAllyStrike(
                this,
                sourcePosition,
                target.position + Vector3.up * 0.9f,
                color,
                source);

            var boss = target.GetComponentInParent<DungeonBoss>();
            if (boss != null)
            {
                TetherfallCombatEffectsV2.ObserveBossState(this, boss);
            }
        }

        private void AddAccessibleCombatImpulse(float magnitude, float duration)
        {
            if (magnitude > 0f && duration > 0f)
            {
                cameraRig?.AddImpulse(magnitude, duration);
            }
        }
    }

    public static class CombatFeedback
    {
        private const int MaximumPooledItemsPerEffect = 32;
        private const int MaximumTrackedLegacyTransients = 64;
        private const int MaximumActiveDamageNumbers = 18;
        private const string DamageNumberPoolKey = "damage-number";

        private static readonly Dictionary<string, Queue<GameObject>> TransientPools =
            new Dictionary<string, Queue<GameObject>>();
        private static readonly HashSet<GameObject> TrackedTransients = new HashSet<GameObject>();
        private static int damageNumberSequence;
        private static int activeDamageNumbers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCachedMaterials()
        {
            foreach (var item in TrackedTransients)
            {
                if (item != null)
                {
                    Object.Destroy(item);
                }
            }

            TrackedTransients.Clear();
            TransientPools.Clear();
            damageNumberSequence = 0;
            activeDamageNumbers = 0;
        }

        public static void CreateSwingArc(TetherfallGame game, Vector3 position, Vector3 direction, Color color, float scale)
        {
            TetherfallCombatEffectsV2.CreateLegacySwingArc(
                game,
                position,
                direction,
                color,
                scale);
        }

        public static void CreateImpact(TetherfallGame game, Vector3 position, Color color, float scale)
        {
            TetherfallCombatEffectsV2.CreateGenericImpact(game, position, color, scale);
        }

        public static void CreateDamageNumber(Vector3 position, int amount, Color color)
        {
            if (amount <= 0)
            {
                return;
            }

            var root = AcquireDamageNumber();
            if (root == null)
            {
                return;
            }

            root.name = "Combat damage number";
            root.transform.position = position + GetDamageNumberOffset();
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            var text = root.GetComponent<TextMesh>();
            text.text = amount.ToString();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.105f + Mathf.Clamp(amount, 0, 100) * 0.00042f;
            text.fontSize = 62;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            var damageText = root.GetComponent<CombatDamageText>() ?? root.AddComponent<CombatDamageText>();
            damageText.Initialize(0.68f, 1.1f, DamageNumberPoolKey);
        }

        public static void CreateAllyStrike(
            TetherfallGame game,
            Vector3 source,
            Vector3 target,
            Color color,
            string strikeSource)
        {
            TetherfallCombatEffectsV2.CreateAllyStrike(
                game,
                source,
                target,
                color,
                strikeSource);
        }

        private static Material GetGlowMaterial(TetherfallGame game, Color color)
        {
            return game != null
                ? RuntimeEffectMaterialCache.GetGlow(game, color, 1.5f, 0.65f)
                : null;
        }

        internal static void RecycleTransient(string poolKey, GameObject item)
        {
            if (string.IsNullOrEmpty(poolKey) || item == null || !item.activeSelf)
            {
                return;
            }

            if (poolKey == DamageNumberPoolKey)
            {
                activeDamageNumbers = Mathf.Max(0, activeDamageNumbers - 1);
                var text = item.GetComponent<TextMesh>();
                if (text != null)
                {
                    text.text = string.Empty;
                }
            }

            var renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
                renderer.enabled = false;
            }

            if (!TransientPools.TryGetValue(poolKey, out var pool))
            {
                pool = new Queue<GameObject>();
                TransientPools[poolKey] = pool;
            }

            if (pool.Count >= MaximumPooledItemsPerEffect)
            {
                TrackedTransients.Remove(item);
                DestroyTransient(item);
                return;
            }

            item.SetActive(false);
            pool.Enqueue(item);
        }

        private static GameObject AcquirePrimitive(PrimitiveType primitive, string poolKey, string itemName)
        {
            if (TransientPools.TryGetValue(poolKey, out var pool))
            {
                while (pool.Count > 0)
                {
                    var item = pool.Dequeue();
                    if (item == null)
                    {
                        continue;
                    }

                    item.name = itemName;
                    item.transform.SetParent(null, false);
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                    item.transform.localScale = Vector3.one;
                    item.layer = TetherfallCombatEffectsV2.IgnoreRaycastLayer;
                    item.SetActive(true);
                    var renderer = item.GetComponent<Renderer>();
                    if (renderer == null)
                    {
                        TrackedTransients.Remove(item);
                        DestroyTransient(item);
                        continue;
                    }

                    renderer.enabled = true;
                    renderer.SetPropertyBlock(null);
                    return item;
                }
            }

            if (TrackedTransients.Count >= MaximumTrackedLegacyTransients)
            {
                return null;
            }

            var created = GameObject.CreatePrimitive(primitive);
            created.name = itemName;
            created.layer = TetherfallCombatEffectsV2.IgnoreRaycastLayer;
            RemoveTransientCollider(created);
            TrackedTransients.Add(created);
            return created;
        }

        private static GameObject AcquireDamageNumber()
        {
            if (activeDamageNumbers >= MaximumActiveDamageNumbers)
            {
                return null;
            }

            if (TransientPools.TryGetValue(DamageNumberPoolKey, out var pool))
            {
                while (pool.Count > 0)
                {
                    var item = pool.Dequeue();
                    if (item == null)
                    {
                        continue;
                    }

                    var text = item.GetComponent<TextMesh>();
                    if (text == null)
                    {
                        TrackedTransients.Remove(item);
                        DestroyTransient(item);
                        continue;
                    }

                    item.transform.SetParent(null, false);
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                    item.transform.localScale = Vector3.one;
                    item.layer = TetherfallCombatEffectsV2.IgnoreRaycastLayer;
                    item.SetActive(true);
                    var renderer = item.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        renderer.SetPropertyBlock(null);
                    }

                    activeDamageNumbers++;
                    return item;
                }
            }

            if (TrackedTransients.Count >= MaximumTrackedLegacyTransients)
            {
                return null;
            }

            var created = new GameObject("Combat damage number");
            created.layer = TetherfallCombatEffectsV2.IgnoreRaycastLayer;
            created.AddComponent<TextMesh>();
            TrackedTransients.Add(created);
            activeDamageNumbers++;
            return created;
        }

        private static void RemoveTransientCollider(GameObject item)
        {
            var collider = item.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            collider.enabled = false;
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void DestroyTransient(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(item);
            }
            else
            {
                Object.DestroyImmediate(item);
            }
        }

        private static Vector3 GetDamageNumberOffset()
        {
            var slot = damageNumberSequence++ % 5;
            var camera = Camera.main;
            var lateral = camera != null ? camera.transform.right : Vector3.right;
            return lateral * ((slot - 2) * 0.075f) + Vector3.up * ((slot & 1) * 0.045f);
        }
    }

    public sealed class CombatTransient : MonoBehaviour
    {
        private float expiresAt;
        private Vector3 targetScale;
        private Vector3 velocity;
        private string poolKey;

        public void Initialize(
            float lifetime,
            Vector3 scaleMultiplier,
            Vector3 movement = default,
            string transientPoolKey = null)
        {
            if (float.IsNaN(lifetime) || float.IsInfinity(lifetime))
            {
                lifetime = 0.01f;
            }

            expiresAt = Time.time + Mathf.Max(0.01f, lifetime);
            targetScale = Vector3.Scale(transform.localScale, scaleMultiplier);
            velocity = movement;
            poolKey = transientPoolKey;
        }

        private void Update()
        {
            var game = TetherfallGame.Instance;
            if (game != null && game.GameplayPaused)
            {
                expiresAt += Time.deltaTime;
                return;
            }

            transform.position += velocity * Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 16f);
            if (Time.time >= expiresAt)
            {
                if (string.IsNullOrEmpty(poolKey))
                {
                    Destroy(gameObject);
                }
                else
                {
                    CombatFeedback.RecycleTransient(poolKey, gameObject);
                }
            }
        }
    }

    public sealed class CombatDamageText : MonoBehaviour
    {
        private float expiresAt;
        private float riseSpeed;
        private TextMesh text;
        private Color color;
        private string poolKey;
        private Transform cameraTransform;

        public void Initialize(float lifetime, float speed)
        {
            Initialize(lifetime, speed, null);
        }

        internal void Initialize(float lifetime, float speed, string transientPoolKey)
        {
            if (float.IsNaN(lifetime) || float.IsInfinity(lifetime))
            {
                lifetime = 0.01f;
            }

            expiresAt = Time.time + Mathf.Max(0.01f, lifetime);
            riseSpeed = float.IsNaN(speed) || float.IsInfinity(speed) ? 0f : speed;
            text = GetComponent<TextMesh>();
            color = text != null ? text.color : Color.white;
            poolKey = transientPoolKey;
            var camera = Camera.main;
            cameraTransform = camera != null ? camera.transform : null;
        }

        private void Update()
        {
            var game = TetherfallGame.Instance;
            if (game != null && game.GameplayPaused)
            {
                expiresAt += Time.deltaTime;
                return;
            }

            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            if (cameraTransform != null)
            {
                var toCamera = cameraTransform.position - transform.position;
                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(toCamera);
                }
            }

            if (text != null)
            {
                var alpha = Mathf.Clamp01((expiresAt - Time.time) * 2.7f);
                text.color = new Color(color.r, color.g, color.b, alpha);
            }

            if (Time.time >= expiresAt)
            {
                if (string.IsNullOrEmpty(poolKey))
                {
                    Destroy(gameObject);
                }
                else
                {
                    CombatFeedback.RecycleTransient(poolKey, gameObject);
                }
            }
        }
    }

    public static class CombatVisualBuilder
    {
        public static void DressWayfinder(TetherfallGame game, Transform root)
        {
            AddPrimitive(
                game,
                root,
                PrimitiveType.Sphere,
                "Wayfinder hood",
                new Vector3(0f, 1.66f, 0.02f),
                Vector3.one * 0.32f,
                new Color(0.11f, 0.25f, 0.29f));
            AddPrimitive(
                game,
                root,
                PrimitiveType.Cube,
                "Wayfinder mantle",
                new Vector3(0f, 1.08f, 0.19f),
                new Vector3(0.88f, 0.5f, 0.22f),
                new Color(0.1f, 0.34f, 0.37f),
                new Vector3(14f, 0f, 0f));
            AddPrimitive(
                game,
                root,
                PrimitiveType.Sphere,
                "Wayfinder Threadglass",
                new Vector3(0.3f, 1.32f, -0.22f),
                Vector3.one * 0.1f,
                new Color(0.34f, 0.92f, 0.78f),
                glow: true);
        }

        public static void DressWayfinderWeapon(
            TetherfallGame game,
            Transform root,
            WeaponDiscipline discipline)
        {
            switch (discipline)
            {
                case WeaponDiscipline.Riftbow:
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cube,
                        "Wayfinder Riftbow upper limb",
                        new Vector3(0.47f, 1.18f, -0.14f),
                        new Vector3(0.08f, 0.48f, 0.11f),
                        new Color(0.31f, 0.86f, 0.78f),
                        new Vector3(0f, 0f, -24f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cube,
                        "Wayfinder Riftbow lower limb",
                        new Vector3(0.47f, 0.78f, -0.14f),
                        new Vector3(0.08f, 0.48f, 0.11f),
                        new Color(0.31f, 0.86f, 0.78f),
                        new Vector3(0f, 0f, 24f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cylinder,
                        "Wayfinder Riftbow string",
                        new Vector3(0.47f, 0.98f, -0.14f),
                        new Vector3(0.018f, 0.5f, 0.018f),
                        new Color(0.78f, 0.95f, 0.88f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Sphere,
                        "Wayfinder Riftbow nock",
                        new Vector3(0.47f, 1.02f, -0.24f),
                        Vector3.one * 0.09f,
                        new Color(0.45f, 0.96f, 0.86f),
                        glow: true);
                    break;

                case WeaponDiscipline.Threadstaff:
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cylinder,
                        "Wayfinder Threadstaff",
                        new Vector3(0.48f, 1.1f, -0.12f),
                        new Vector3(0.05f, 0.82f, 0.05f),
                        new Color(0.54f, 0.68f, 0.97f),
                        new Vector3(18f, 0f, 16f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Sphere,
                        "Wayfinder Threadstaff focus",
                        new Vector3(0.28f, 1.78f, -0.12f),
                        Vector3.one * 0.13f,
                        new Color(0.54f, 0.76f, 1f),
                        glow: true);
                    break;

                default:
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cylinder,
                        "Wayfinder Pathblade",
                        new Vector3(0.46f, 0.98f, -0.14f),
                        new Vector3(0.06f, 0.58f, 0.06f),
                        new Color(0.79f, 0.52f, 0.25f),
                        new Vector3(32f, 8f, 24f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cube,
                        "Wayfinder Pathblade guard",
                        new Vector3(0.35f, 0.78f, -0.14f),
                        new Vector3(0.28f, 0.05f, 0.09f),
                        new Color(0.43f, 0.25f, 0.14f),
                        new Vector3(0f, 8f, 24f));
                    break;
            }
        }

        public static void DressWayfinderFieldGear(
            TetherfallGame game,
            Transform root,
            string coatId,
            string charmId,
            string toolId)
        {
            if (coatId == "coat.mosscoat")
            {
                AddPrimitive(
                    game,
                    root,
                    PrimitiveType.Cube,
                    "Mosscoat mantle",
                    new Vector3(0f, 1.06f, 0.28f),
                    new Vector3(1.02f, 0.56f, 0.28f),
                    new Color(0.2f, 0.52f, 0.3f),
                    new Vector3(14f, 0f, 0f));
                AddPrimitive(
                    game,
                    root,
                    PrimitiveType.Sphere,
                    "Mosscoat shoulder leaf",
                    new Vector3(-0.38f, 1.42f, 0.11f),
                    new Vector3(0.22f, 0.1f, 0.14f),
                    new Color(0.39f, 0.74f, 0.36f),
                    new Vector3(0f, 0f, -26f));
                AddPrimitive(
                    game,
                    root,
                    PrimitiveType.Sphere,
                    "Mosscoat shoulder leaf",
                    new Vector3(0.38f, 1.42f, 0.11f),
                    new Vector3(0.22f, 0.1f, 0.14f),
                    new Color(0.39f, 0.74f, 0.36f),
                    new Vector3(0f, 0f, 26f));
            }

            if (!string.IsNullOrEmpty(charmId))
            {
                var charmColor = GetCharmColor(charmId);
                AddPrimitive(
                    game,
                    root,
                    PrimitiveType.Sphere,
                    "Wayfinder equipped charm",
                    new Vector3(-0.21f, 1.31f, -0.3f),
                    Vector3.one * 0.115f,
                    charmColor,
                    glow: true);
                AddPrimitive(
                    game,
                    root,
                    PrimitiveType.Cylinder,
                    "Wayfinder charm fitting",
                    new Vector3(-0.21f, 1.25f, -0.3f),
                    new Vector3(0.06f, 0.08f, 0.06f),
                    new Color(0.7f, 0.46f, 0.22f));
            }

            switch (toolId)
            {
                case "tool.hookline":
                    for (var index = 0; index < 3; index++)
                    {
                        AddPrimitive(
                            game,
                            root,
                            PrimitiveType.Cylinder,
                            "Hookline hip coil",
                            new Vector3(-0.42f, 0.82f + index * 0.07f, 0.18f),
                            new Vector3(0.18f, 0.03f, 0.18f),
                            new Color(0.68f, 0.43f, 0.2f),
                            new Vector3(90f, 0f, 0f));
                    }
                    break;

                case "tool.ridgepack":
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cube,
                        "Ridgepack",
                        new Vector3(0f, 0.95f, 0.42f),
                        new Vector3(0.6f, 0.68f, 0.26f),
                        new Color(0.29f, 0.22f, 0.14f),
                        new Vector3(8f, 0f, 0f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cylinder,
                        "Ridgepack bedroll",
                        new Vector3(0f, 1.42f, 0.43f),
                        new Vector3(0.12f, 0.34f, 0.12f),
                        new Color(0.42f, 0.58f, 0.35f),
                        new Vector3(0f, 0f, 90f));
                    break;

                case "tool.ashkite":
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cube,
                        "Ashkite folded wing",
                        new Vector3(-0.42f, 1.06f, 0.42f),
                        new Vector3(0.66f, 0.06f, 0.26f),
                        new Color(0.82f, 0.36f, 0.16f),
                        new Vector3(0f, 0f, 24f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cube,
                        "Ashkite folded wing",
                        new Vector3(0.42f, 1.06f, 0.42f),
                        new Vector3(0.66f, 0.06f, 0.26f),
                        new Color(0.82f, 0.36f, 0.16f),
                        new Vector3(0f, 0f, -24f));
                    break;

                case "tool.thermal_reel":
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cylinder,
                        "Thermal reel",
                        new Vector3(-0.42f, 0.98f, 0.34f),
                        new Vector3(0.2f, 0.12f, 0.2f),
                        new Color(0.76f, 0.3f, 0.13f),
                        new Vector3(90f, 0f, 0f));
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Sphere,
                        "Thermal reel ember",
                        new Vector3(-0.42f, 0.98f, 0.49f),
                        Vector3.one * 0.08f,
                        new Color(1f, 0.36f, 0.12f),
                        glow: true);
                    break;
            }
        }

        private static Color GetCharmColor(string charmId)
        {
            return charmId switch
            {
                "charm.anchor" => new Color(0.94f, 0.69f, 0.27f),
                "charm.galespring" => new Color(0.38f, 0.9f, 0.82f),
                "charm.mirelight" => new Color(0.47f, 0.78f, 0.94f),
                "charm.sluiceglass" => new Color(0.31f, 0.86f, 0.8f),
                "charm.emberwatch" => new Color(1f, 0.36f, 0.15f),
                _ => new Color(0.75f, 0.82f, 0.92f)
            };
        }

        public static void DressCompanion(TetherfallGame game, Transform root, string role, Color color)
        {
            AddPrimitive(
                game,
                root,
                PrimitiveType.Sphere,
                "Companion head",
                new Vector3(0f, 1.58f, 0f),
                Vector3.one * 0.29f,
                Color.Lerp(color, new Color(0.86f, 0.68f, 0.5f), 0.48f));
            AddPrimitive(
                game,
                root,
                PrimitiveType.Cube,
                "Companion pack",
                new Vector3(0f, 0.88f, 0.32f),
                new Vector3(0.44f, 0.52f, 0.16f),
                Color.Lerp(color, Color.black, 0.33f),
                new Vector3(10f, 0f, 0f));
            var toolColor = role.Contains("medic")
                ? new Color(0.46f, 0.9f, 0.78f)
                : role.Contains("breaker")
                    ? new Color(0.92f, 0.42f, 0.24f)
                    : new Color(0.91f, 0.7f, 0.34f);
            AddPrimitive(
                game,
                root,
                PrimitiveType.Cylinder,
                "Companion field tool",
                new Vector3(0.42f, 1.02f, 0.02f),
                new Vector3(0.045f, 0.48f, 0.045f),
                toolColor,
                new Vector3(22f, 0f, 18f),
                role.Contains("medic"));
        }

        public static void DressPact(TetherfallGame game, Transform root, string creatureName, bool isMount, Color color)
        {
            if (isMount)
            {
                for (var index = 0; index < 3; index++)
                {
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Cube,
                        "Slateback shell plate",
                        new Vector3(0f, 0.58f, 0.45f - index * 0.42f),
                        new Vector3(1.04f - index * 0.12f, 0.24f, 0.38f),
                        Color.Lerp(color, new Color(0.26f, 0.63f, 0.66f), 0.35f),
                        new Vector3(10f + index * 6f, 0f, 0f));
                }

                return;
            }

            if (creatureName == "Cinderwing")
            {
                AddPrimitive(game, root, PrimitiveType.Cube, "Cinderwing left wing", new Vector3(-0.65f, 0.2f, 0f), new Vector3(0.92f, 0.05f, 0.35f), color, new Vector3(0f, 0f, 23f));
                AddPrimitive(game, root, PrimitiveType.Cube, "Cinderwing right wing", new Vector3(0.65f, 0.2f, 0f), new Vector3(0.92f, 0.05f, 0.35f), color, new Vector3(0f, 0f, -23f));
            }
            else if (creatureName == "Bramblet")
            {
                AddPrimitive(game, root, PrimitiveType.Sphere, "Bramblet leaf crown", new Vector3(-0.34f, 0.38f, 0f), new Vector3(0.34f, 0.13f, 0.22f), new Color(0.48f, 0.92f, 0.35f), new Vector3(0f, 0f, -28f));
                AddPrimitive(game, root, PrimitiveType.Sphere, "Bramblet leaf crown", new Vector3(0.34f, 0.38f, 0f), new Vector3(0.34f, 0.13f, 0.22f), new Color(0.48f, 0.92f, 0.35f), new Vector3(0f, 0f, 28f));
            }
            else
            {
                AddPrimitive(game, root, PrimitiveType.Sphere, "Mistling halo", new Vector3(0f, 0.5f, 0f), Vector3.one * 0.26f, new Color(0.43f, 0.88f, 0.92f), glow: true);
            }
        }

        public static void DressEnemy(TetherfallGame game, Transform root, EnemyDefinition definition)
        {
            var accent = definition.attackStyle switch
            {
                EnemyAttackStyle.Ranged => new Color(0.35f, 0.88f, 0.92f),
                EnemyAttackStyle.Heavy => new Color(0.97f, 0.43f, 0.23f),
                _ => new Color(0.86f, 0.68f, 0.4f)
            };
            AddPrimitive(
                game,
                root,
                PrimitiveType.Sphere,
                "Enemy mask",
                new Vector3(0f, definition.scale * 1.54f, definition.scale * 0.34f),
                Vector3.one * definition.scale * 0.27f,
                accent,
                glow: definition.attackStyle == EnemyAttackStyle.Ranged);
            if (definition.specialMove == EnemySpecialMove.GroundBurst)
            {
                AddPrimitive(
                    game,
                    root,
                    PrimitiveType.Cylinder,
                    "Ground-burst silt focus",
                    new Vector3(0f, definition.scale * 1.02f, definition.scale * 0.32f),
                    new Vector3(definition.scale * 0.24f, definition.scale * 0.1f, definition.scale * 0.24f),
                    new Color(0.28f, 0.92f, 0.87f),
                    new Vector3(0f, 0f, 0f),
                    true);
                for (var index = 0; index < 3; index++)
                {
                    var angle = index * 120f * Mathf.Deg2Rad;
                    AddPrimitive(
                        game,
                        root,
                        PrimitiveType.Sphere,
                        "Ground-burst mote",
                        new Vector3(
                            Mathf.Cos(angle) * definition.scale * 0.34f,
                            definition.scale * 1.22f,
                            Mathf.Sin(angle) * definition.scale * 0.34f),
                        Vector3.one * definition.scale * 0.08f,
                        new Color(0.32f, 0.88f, 0.82f),
                        glow: true);
                }
            }

            if (definition.attackStyle == EnemyAttackStyle.Heavy)
            {
                AddPrimitive(game, root, PrimitiveType.Cube, "Enemy left pauldron", new Vector3(-definition.scale * 0.52f, definition.scale * 1.16f, 0f), new Vector3(definition.scale * 0.46f, definition.scale * 0.23f, definition.scale * 0.35f), accent, new Vector3(0f, 0f, 18f));
                AddPrimitive(game, root, PrimitiveType.Cube, "Enemy right pauldron", new Vector3(definition.scale * 0.52f, definition.scale * 1.16f, 0f), new Vector3(definition.scale * 0.46f, definition.scale * 0.23f, definition.scale * 0.35f), accent, new Vector3(0f, 0f, -18f));
            }
            else
            {
                AddPrimitive(
                    game,
                    root,
                    PrimitiveType.Cylinder,
                    definition.attackStyle == EnemyAttackStyle.Ranged ? "Enemy channeling staff" : "Enemy blade",
                    new Vector3(definition.scale * 0.48f, definition.scale * 0.96f, 0f),
                    new Vector3(definition.scale * 0.05f, definition.scale * 0.5f, definition.scale * 0.05f),
                    accent,
                    new Vector3(24f, 0f, definition.attackStyle == EnemyAttackStyle.Ranged ? 12f : 28f),
                    definition.attackStyle == EnemyAttackStyle.Ranged);
            }
        }

        public static void DressBoss(TetherfallGame game, Transform root)
        {
            AddPrimitive(
                game,
                root,
                PrimitiveType.Sphere,
                "Warden core",
                new Vector3(0f, 1.1f, 0.48f),
                Vector3.one * 0.26f,
                new Color(0.82f, 0.34f, 0.69f),
                glow: true);
            AddPrimitive(
                game,
                root,
                PrimitiveType.Cylinder,
                "Warden crown",
                new Vector3(0f, 1.7f, 0f),
                new Vector3(0.45f, 0.12f, 0.45f),
                new Color(0.41f, 0.22f, 0.65f),
                new Vector3(0f, 0f, 0f),
                true);
            AddPrimitive(
                game,
                root,
                PrimitiveType.Cube,
                "Warden left prism",
                new Vector3(-0.62f, 1.25f, 0f),
                new Vector3(0.22f, 0.64f, 0.22f),
                new Color(0.63f, 0.29f, 0.72f),
                new Vector3(0f, 0f, 24f),
                true);
            AddPrimitive(
                game,
                root,
                PrimitiveType.Cube,
                "Warden right prism",
                new Vector3(0.62f, 1.25f, 0f),
                new Vector3(0.22f, 0.64f, 0.22f),
                new Color(0.63f, 0.29f, 0.72f),
                new Vector3(0f, 0f, -24f),
                true);
        }

        private static void AddPrimitive(
            TetherfallGame game,
            Transform parent,
            PrimitiveType primitive,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Vector3 localRotation = default,
            bool glow = false)
        {
            var item = GameObject.CreatePrimitive(primitive);
            item.name = objectName;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.transform.localRotation = Quaternion.Euler(localRotation);
            item.GetComponent<Renderer>().material = glow
                ? game.CreateGlowMaterial(color, 1.3f, 0.6f)
                : game.CreateMaterial(color, 0.04f, 0.38f);
            Object.Destroy(item.GetComponent<Collider>());
        }
    }
}
