using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// A short-lived rescue interaction created only for a catalogued wild Echosworn.
    /// The interaction records the creature's offered Accord; it never changes existing
    /// enemy identities into roster-eligible identities.
    /// </summary>
    public sealed class ThreadbindOpportunity : MonoBehaviour, IWorldInteractable
    {
        public const float LifetimeSeconds = 24f;
        public const int MaximumRenderers = 8;

        private static readonly string[] CopySamples =
        {
            "Rescue the recovering Echosworn.",
            "Listen for its voluntary Threadbind Accord.",
            "The Echosworn is safe and chooses whether to join the roster."
        };

        private TetherfallGame game;
        private PactSpeciesDefinition species;
        private string originKey;
        private float expiresAt;
        private float baseHeight;
        private bool resolved;
        private Renderer coreRenderer;
        private Material surfaceMaterial;
        private Material resonanceMaterial;

        public string SpeciesId => species?.speciesId ?? string.Empty;
        public string EligibleEnemyId => species?.eligibleEnemyId ?? string.Empty;
        public string OriginKey => originKey ?? string.Empty;
        public float SecondsRemaining => Mathf.Max(0f, expiresAt - Time.time);
        public static IReadOnlyList<string> PlayerFacingCopySamples => CopySamples;

        public string InteractionPrompt
        {
            get
            {
                if (resolved || Time.time > expiresAt)
                {
                    return "The rescue opportunity has passed.";
                }

                return $"Rescue {species.displayName}; hear its voluntary Threadbind Accord";
            }
        }

        public static ThreadbindOpportunity Create(
            TetherfallGame game,
            PactSpeciesDefinition species,
            Vector3 position,
            string originKey)
        {
            if (game == null ||
                species == null ||
                !species.isWild ||
                !TetherfallAccordSpeciesCatalog.IsEligibleWildEnemyId(
                    species.eligibleEnemyId) ||
                !IsFinite(position))
            {
                return null;
            }

            var root = new GameObject($"Threadbind rescue · {species.displayName}");
            root.transform.position = position;
            var opportunity = root.AddComponent<ThreadbindOpportunity>();
            opportunity.game = game;
            opportunity.species = species;
            opportunity.originKey = originKey ?? string.Empty;
            opportunity.expiresAt = Time.time + LifetimeSeconds;
            opportunity.baseHeight = position.y;
            opportunity.BuildVisual();
            game.RegisterInteractable(opportunity);
            return opportunity;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !resolved &&
                   owner != null &&
                   owner == game &&
                   species != null &&
                   Time.time <= expiresAt;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                owner?.Notify("The recovering Echosworn has already found a safe path.");
                return;
            }

            if (!owner.AcceptThreadbindAccord(species, originKey, out _))
            {
                owner.Notify("The Threadbind Accord could not be recorded.");
                return;
            }

            resolved = true;
            owner.Notify(
                $"{species.displayName} is safe and freely chooses a Threadbind Accord.");
            Retire();
        }

        private void Update()
        {
            if (resolved)
            {
                return;
            }

            if (Time.time > expiresAt)
            {
                resolved = true;
                Retire();
                return;
            }

            var remaining = Mathf.Clamp01(SecondsRemaining / LifetimeSeconds);
            transform.position = new Vector3(
                transform.position.x,
                baseHeight + 0.22f + Mathf.Sin(Time.time * 2.4f) * 0.08f,
                transform.position.z);
            transform.Rotate(0f, 28f * Time.deltaTime, 0f, Space.World);
            if (coreRenderer != null)
            {
                var pulse = 0.84f + Mathf.Sin(Time.time * 4.2f) * 0.12f;
                coreRenderer.transform.localScale =
                    Vector3.one * Mathf.Max(0.08f, pulse * remaining);
            }
        }

        private void BuildVisual()
        {
            surfaceMaterial = game.CreateMaterial(
                Color.Lerp(species.art.surfaceColor, Color.white, 0.18f),
                0.08f,
                0.42f);
            resonanceMaterial = game.CreateGlowMaterial(
                species.art.resonanceColor,
                1.55f,
                0.66f);

            var cradle = CreatePart(
                transform,
                PrimitiveType.Sphere,
                "Resting resonance cradle",
                new Vector3(0f, 0.2f, 0f),
                new Vector3(0.48f, 0.18f, 0.48f),
                Quaternion.identity,
                surfaceMaterial);
            coreRenderer = cradle.GetComponent<Renderer>();
            for (var index = 0; index < 3; index++)
            {
                var angle = index * 120f;
                CreatePart(
                    transform,
                    PrimitiveType.Capsule,
                    $"Open Accord arc {index + 1}",
                    new Vector3(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * 0.42f,
                        0.52f,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * 0.42f),
                    new Vector3(0.055f, 0.35f, 0.055f),
                    Quaternion.Euler(18f, -angle, 24f),
                    resonanceMaterial);
            }

            var signal = CreatePart(
                transform,
                PrimitiveType.Sphere,
                "Voluntary Accord signal",
                new Vector3(0f, 0.72f, 0f),
                Vector3.one * 0.16f,
                Quaternion.identity,
                resonanceMaterial);
            coreRenderer = signal.GetComponent<Renderer>();
        }

        private static GameObject CreatePart(
            Transform parent,
            PrimitiveType primitive,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private void Retire()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void OnDestroy()
        {
            game?.OnThreadbindOpportunityClosed(originKey, this);
            DestroyOwnedMaterial(surfaceMaterial);
            DestroyOwnedMaterial(resonanceMaterial);
            surfaceMaterial = null;
            resonanceMaterial = null;
        }

        private static void DestroyOwnedMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// Collider-free field representation for one active roster record. Target queries are
    /// bounded and reusable; reserve records have no actor and advance through roster data.
    /// </summary>
    public sealed class CapturedPactActor : MonoBehaviour
    {
        public const int TargetQueryCapacity = 32;
        public const int MaximumAssistQueriesPerTick = 1;
        public const float SafeSettlementNoWarpDistance = 17f;

        private const float SafeSettlementNoWarpDistanceSquared =
            SafeSettlementNoWarpDistance * SafeSettlementNoWarpDistance;
        private const float MinimumAssistRetrySeconds = 0.62f;

        private readonly Collider[] assistTargetBuffer = new Collider[TargetQueryCapacity];
        private readonly ExpeditionFollowerMotor followerMotor = new ExpeditionFollowerMotor();

        private TetherfallGame game;
        private CapturedPactSaveData record;
        private PactSpeciesDefinition species;
        private CapturedPactArtRig artRig;
        private int fieldSlot;
        private float nextAssistAt;
        private float assistStagger;
        private string assistSource;
        private bool retiring;

        public string InstanceId => record?.instanceId ?? string.Empty;
        public string SpeciesId => species?.speciesId ?? string.Empty;
        public string DisplayName => PactRosterRules.GetDisplayName(record);
        public int FieldSlot => fieldSlot;
        public bool IsFieldActive =>
            !retiring &&
            record != null &&
            record.fieldSlotState == PactFieldSlotState.ActiveField &&
            isActiveAndEnabled;
        public bool UsesFlyingMotor => species?.movement?.usesFlyingMotor == true;
        public int TargetBufferCapacity => assistTargetBuffer.Length;
        public PactCombatRole CombatRole =>
            species?.combatRole ?? PactCombatRole.Vanguard;
        public PactResonanceAffinity ResonanceAffinity =>
            species?.resonanceAffinity ?? PactResonanceAffinity.Grove;
        public string TacticalAbilityShortLabel =>
            CombatRole switch
            {
                PactCombatRole.Mender => "Mend",
                PactCombatRole.Controller => "Snare",
                PactCombatRole.Warden => "Guard",
                PactCombatRole.Vanguard => "Break",
                PactCombatRole.Skirmisher => "Flank",
                _ => "Resonate"
            };

        public static CapturedPactActor Create(
            TetherfallGame game,
            CapturedPactSaveData record,
            PactSpeciesDefinition species,
            Vector3 position,
            int fieldSlot)
        {
            if (game == null || record == null || species == null)
            {
                return null;
            }

            var root = new GameObject($"Accord field actor · {species.displayName}");
            root.transform.position = position;
            var actor = root.AddComponent<CapturedPactActor>();
            actor.game = game;
            actor.record = record;
            actor.species = species;
            actor.fieldSlot = Mathf.Clamp(
                fieldSlot,
                0,
                PactRosterRules.AbsoluteMaximumFieldUnits - 1);
            actor.assistSource = $"{species.displayName} Accord assist";
            actor.assistStagger = StableStagger(record.instanceId);
            actor.nextAssistAt = Time.time + 0.45f + actor.assistStagger;
            actor.artRig = TetherfallCapturedPactArt.Build(game, root.transform, species);
            return actor;
        }

        public void RefreshRecord(
            CapturedPactSaveData currentRecord,
            PactSpeciesDefinition currentSpecies)
        {
            if (currentRecord == null || currentSpecies == null)
            {
                return;
            }

            record = currentRecord;
            if (ReferenceEquals(species, currentSpecies))
            {
                return;
            }

            species = currentSpecies;
            assistSource = $"{species.displayName} Accord assist";
        }

        public void SetFieldSlot(int slot)
        {
            fieldSlot = Mathf.Clamp(
                slot,
                0,
                PactRosterRules.AbsoluteMaximumFieldUnits - 1);
        }

        public void SetSimulationActive(bool active)
        {
            if (retiring || gameObject.activeSelf == active)
            {
                return;
            }

            gameObject.SetActive(active);
            if (!active)
            {
                followerMotor.ResetMotion();
            }
        }

        public bool TryRecallToFormation(Vector3 formationPosition)
        {
            return TryRecallToFormation(formationPosition, false);
        }

        public bool TryRecallToFormation(
            Vector3 formationPosition,
            bool immediate)
        {
            if (retiring ||
                game == null ||
                record == null ||
                record.IsSettlementAssigned ||
                species?.movement == null ||
                (!immediate && game.GameplayPaused))
            {
                return false;
            }

            if (immediate)
            {
                transform.position = formationPosition;
                followerMotor.ResetMotion();
                return true;
            }

            var profile = species.movement.usesFlyingMotor
                ? ExpeditionFollowerMotorProfile.FlyingPact
                : ExpeditionFollowerMotorProfile.GroundedPact;
            var result = followerMotor.Move(
                transform,
                formationPosition,
                game.PlayerTransform.position.y,
                0.1f,
                false,
                profile);
            return result.Moved || result.Warped;
        }

        public void Retire()
        {
            if (retiring)
            {
                return;
            }

            retiring = true;
            gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public bool CanAnswerTacticalCommand()
        {
            if (!IsFieldActive ||
                game == null ||
                game.Player == null ||
                record.stamina < 4f ||
                record.morale < 10f)
            {
                return false;
            }

            return CombatRole == PactCombatRole.Mender ||
                   game.HasTacticalFocusHostile(16f);
        }

        public bool TriggerTacticalCommand()
        {
            if (!CanAnswerTacticalCommand())
            {
                return false;
            }

            if (CombatRole == PactCombatRole.Mender)
            {
                var healing = ScaleCombatPotency(
                    14f + record.level * 1.4f,
                    true);
                game.HealPlayer(healing);
                game.RestoreStamina(
                    12f * game.PactConcordMultiplier);
                record.stamina = Mathf.Max(0f, record.stamina - 4f);
                return true;
            }

            var enemy = game.GetExpeditionEnemy(16f);
            if (enemy != null)
            {
                var damage = ScaleCombatPotency(
                    GetTacticalBasePower() *
                    game.ExpeditionDamageMultiplier,
                    true);
                enemy.TakeDamage(damage, $"{DisplayName} tactical response");
                ApplyTacticalRoleEffect(enemy);
                game.OnExpeditionAllyStrike(
                    enemy,
                    transform,
                    $"{DisplayName} tactical response",
                    GetAccentColor());
                record.stamina = Mathf.Max(0f, record.stamina - 4f);
                return true;
            }

            var boss = game.GetExpeditionBoss(18f);
            if (boss == null)
            {
                return false;
            }

            var bossDamage = ScaleCombatPotency(
                GetTacticalBasePower() *
                0.88f *
                game.ExpeditionDamageMultiplier,
                true);
            boss.TakeDamage(
                bossDamage,
                $"{DisplayName} tactical response");
            ApplyTacticalRoleEffect(boss);
            game.OnExpeditionAllyStrike(
                boss,
                transform,
                $"{DisplayName} tactical response",
                GetAccentColor());
            record.stamina = Mathf.Max(0f, record.stamina - 4f);
            return true;
        }

        public bool TriggerCrossweaveResponse(Enemy target)
        {
            if (!CanRespondToCoordination() ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            if (CombatRole == PactCombatRole.Mender)
            {
                game.HealPlayer(ScaleCombatPotency(6f + record.level, false));
                game.RestoreStamina(8f);
            }
            else
            {
                target.TakeDamage(
                    ScaleCombatPotency(
                        5f + GetBasePower() * 0.35f + record.level,
                        false),
                    $"{DisplayName} Crossweave response");
                ApplyCoordinationRoleEffect(target, false);
            }

            game.OnExpeditionAllyStrike(
                target,
                transform,
                $"{DisplayName} Crossweave response",
                GetAccentColor());
            return true;
        }

        public bool TriggerCrossweaveResponse(DungeonBoss target)
        {
            if (!CanRespondToCoordination() ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            if (CombatRole == PactCombatRole.Mender)
            {
                game.HealPlayer(ScaleCombatPotency(6f + record.level, false));
                game.RestoreStamina(8f);
            }
            else
            {
                target.TakeDamage(
                    ScaleCombatPotency(
                        5f + GetBasePower() * 0.3f + record.level,
                        false),
                    $"{DisplayName} Crossweave response");
                ApplyCoordinationRoleEffect(target, false);
            }

            game.OnExpeditionAllyStrike(
                target,
                transform,
                $"{DisplayName} Crossweave response",
                GetAccentColor());
            return true;
        }

        public bool TriggerThreadlinkResponse(Enemy target, int beat)
        {
            if (!CanRespondToCoordination() ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            if (CombatRole == PactCombatRole.Mender)
            {
                game.HealPlayer(ScaleCombatPotency(3f + record.level, false));
                game.RestoreStamina(5f);
            }
            else
            {
                target.TakeDamage(
                    ScaleCombatPotency(
                        3f + GetBasePower() * 0.24f + record.level,
                        false),
                    $"{DisplayName} Threadlink weave");
                ApplyCoordinationRoleEffect(target, true);
            }

            game.OnThreadlinkRallyResponse(
                target,
                transform,
                $"{DisplayName} Threadlink weave {beat}",
                GetAccentColor());
            return true;
        }

        public bool TriggerThreadlinkResponse(
            DungeonBoss target,
            int beat)
        {
            if (!CanRespondToCoordination() ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            if (CombatRole == PactCombatRole.Mender)
            {
                game.HealPlayer(ScaleCombatPotency(3f + record.level, false));
                game.RestoreStamina(5f);
            }
            else
            {
                target.TakeDamage(
                    ScaleCombatPotency(
                        3f + GetBasePower() * 0.2f + record.level,
                        false),
                    $"{DisplayName} Threadlink weave");
                ApplyCoordinationRoleEffect(target, true);
            }

            game.OnThreadlinkRallyResponse(
                target,
                transform,
                $"{DisplayName} Threadlink weave {beat}",
                GetAccentColor());
            return true;
        }

        private void Update()
        {
            if (retiring ||
                game == null ||
                game.Player == null ||
                record == null ||
                species?.movement == null ||
                record.IsSettlementAssigned)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                nextAssistAt += Time.deltaTime;
                return;
            }

            var formationPosition = game.GetPactFormationPosition(fieldSlot);
            var formationDelta = formationPosition - transform.position;
            if (game.IsInsidePactSafeSettlement(game.PlayerTransform.position) &&
                formationDelta.sqrMagnitude > SafeSettlementNoWarpDistanceSquared)
            {
                followerMotor.ResetMotion();
                artRig?.Tick(Time.time, Vector3.zero, game.IsPactCommandFocus(InstanceId));
                return;
            }

            var profile = species.movement.usesFlyingMotor
                ? ExpeditionFollowerMotorProfile.FlyingPact
                : ExpeditionFollowerMotorProfile.GroundedPact;
            followerMotor.Move(
                transform,
                formationPosition,
                game.PlayerTransform.position.y,
                Time.deltaTime,
                false,
                profile);
            var planarVelocity = followerMotor.Velocity;
            planarVelocity.y = 0f;
            if (planarVelocity.sqrMagnitude > 0.04f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(planarVelocity),
                    1f - Mathf.Exp(-7f * Time.deltaTime));
            }

            artRig?.Tick(
                Time.time,
                followerMotor.Velocity,
                game.IsPactCommandFocus(InstanceId));
            if (Time.time >= nextAssistAt)
            {
                TryAssist();
            }
        }

        private void TryAssist()
        {
            var cooldown = Mathf.Max(
                1.5f,
                species.baseStats != null
                    ? species.baseStats.assistCooldown
                    : 3f);
            nextAssistAt = Time.time + cooldown + assistStagger;
            if (record.stamina < 5f || record.morale < 10f)
            {
                return;
            }

            if (species.combatRole == PactCombatRole.Mender &&
                game.Health < game.MaxHealth - 12)
            {
                var healing = Mathf.Max(
                    4,
                    ScaleCombatPotency(
                        5f + record.level * 0.8f,
                        true));
                game.HealPlayer(healing);
                record.stamina = Mathf.Max(0f, record.stamina - 2f);
                return;
            }

            var target = FindBoundedAssistTarget();
            if (target == null)
            {
                var boss = game.GetExpeditionBoss(GetAssistRange());
                if (boss == null)
                {
                    nextAssistAt =
                        Time.time +
                        MinimumAssistRetrySeconds +
                        assistStagger;
                    return;
                }

                AssistBoss(boss);
                return;
            }

            var basePower = species.baseStats != null ? species.baseStats.power : 10;
            var focusMultiplier = game.IsPactCommandFocus(InstanceId) ? 1.14f : 1f;
            var damage = Mathf.Max(
                1,
                ScaleCombatPotency(
                    (basePower * 0.58f + record.level * 1.2f) *
                    game.ExpeditionDamageMultiplier *
                    focusMultiplier,
                    true));
            target.TakeDamage(
                damage,
                assistSource,
                species.combatRole == PactCombatRole.Controller
                    ? CombatImpactKind.Controller
                    : CombatImpactKind.PassiveAlly);

            game.OnExpeditionAllyStrike(
                target,
                transform,
                assistSource,
                species.art.resonanceColor);
            game.RegisterPactAllyOvertone(
                target,
                species.resonanceAffinity,
                assistSource);
            record.stamina = Mathf.Max(0f, record.stamina - 1.6f);
        }

        private void AssistBoss(DungeonBoss boss)
        {
            var basePower = species.baseStats != null
                ? species.baseStats.power
                : 10;
            var focusMultiplier = game.IsPactCommandFocus(InstanceId)
                ? 1.14f
                : 1f;
            var damage = Mathf.Max(
                1,
                ScaleCombatPotency(
                    (basePower * 0.52f + record.level) *
                    game.ExpeditionDamageMultiplier *
                    focusMultiplier,
                    true));
            boss.TakeDamage(
                damage,
                assistSource,
                species.combatRole == PactCombatRole.Controller
                    ? CombatImpactKind.Controller
                    : CombatImpactKind.PassiveAlly);

            game.OnExpeditionAllyStrike(
                boss,
                transform,
                assistSource,
                species.art.resonanceColor);
            game.RegisterPactAllyOvertone(
                boss,
                species.resonanceAffinity,
                assistSource);
            record.stamina = Mathf.Max(0f, record.stamina - 1.6f);
        }

        private Enemy FindBoundedAssistTarget()
        {
            var range = GetAssistRange();
            var hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                range,
                assistTargetBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var boundedCount = Mathf.Min(hitCount, assistTargetBuffer.Length);
            Enemy closest = null;
            var closestDistanceSquared = range * range;
            for (var index = 0; index < boundedCount; index++)
            {
                var collider = assistTargetBuffer[index];
                if (collider == null)
                {
                    continue;
                }

                var enemy = collider.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDefeated)
                {
                    continue;
                }

                var delta = enemy.transform.position - transform.position;
                var distanceSquared = delta.sqrMagnitude;
                if (distanceSquared <= closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closest = enemy;
                }
            }

            for (var index = 0; index < boundedCount; index++)
            {
                assistTargetBuffer[index] = null;
            }

            return closest;
        }

        private float GetAssistRange()
        {
            return species.baseStats != null
                ? Mathf.Clamp(species.baseStats.assistRange, 2f, 14f)
                : 8f;
        }

        private bool CanRespondToCoordination()
        {
            return IsFieldActive &&
                   game != null &&
                   record != null &&
                   species != null &&
                   record.morale >= 10f;
        }

        private int GetBasePower()
        {
            return species?.baseStats?.power ?? 10;
        }

        private float GetTacticalBasePower()
        {
            return GetBasePower() * 0.72f + record.level * 1.35f;
        }

        private int ScaleCombatPotency(
            float amount,
            bool applyConcord)
        {
            var concord = applyConcord && game != null
                ? game.PactConcordMultiplier
                : 1f;
            var skill = game != null
                ? game.PactAssistPotencyMultiplier
                : 1f;
            return Mathf.Max(
                1,
                Mathf.RoundToInt(amount * skill * concord));
        }

        private Color GetAccentColor()
        {
            return species?.art != null
                ? species.art.resonanceColor
                : new Color(0.42f, 0.88f, 0.72f);
        }

        private void ApplyTacticalRoleEffect(Enemy target)
        {
            var concord = game.PactConcordMultiplier;
            switch (CombatRole)
            {
                case PactCombatRole.Controller:
                    target.ApplyStagger(0.82f * concord);
                    target.ApplyExpose(2.4f * concord);
                    break;
                case PactCombatRole.Warden:
                    target.ApplyStagger(0.5f * concord);
                    break;
                case PactCombatRole.Vanguard:
                    target.ApplyStagger(0.66f * concord);
                    break;
                case PactCombatRole.Resonator:
                    game.RegisterPactAllyOvertone(
                        target,
                        ResonanceAffinity,
                        $"{DisplayName} tactical resonance");
                    break;
                default:
                    target.ApplyStagger(0.24f * concord);
                    break;
            }
        }

        private void ApplyTacticalRoleEffect(DungeonBoss target)
        {
            var concord = game.PactConcordMultiplier;
            switch (CombatRole)
            {
                case PactCombatRole.Controller:
                    target.ApplyStagger(0.38f * concord);
                    break;
                case PactCombatRole.Warden:
                    target.ApplyStagger(0.24f * concord);
                    break;
                case PactCombatRole.Vanguard:
                    target.ApplyStagger(0.32f * concord);
                    break;
                case PactCombatRole.Resonator:
                    game.RegisterPactAllyOvertone(
                        target,
                        ResonanceAffinity,
                        $"{DisplayName} tactical resonance");
                    break;
                default:
                    target.ApplyStagger(0.14f * concord);
                    break;
            }
        }

        private void ApplyCoordinationRoleEffect(
            Enemy target,
            bool threadlink)
        {
            if (CombatRole == PactCombatRole.Controller)
            {
                target.ApplyStagger(threadlink ? 0.28f : 0.46f);
            }
            else if (CombatRole == PactCombatRole.Warden ||
                     CombatRole == PactCombatRole.Vanguard)
            {
                target.ApplyStagger(threadlink ? 0.18f : 0.3f);
            }
            else if (CombatRole == PactCombatRole.Resonator &&
                     !threadlink)
            {
                target.ApplyExpose(2.2f);
            }
        }

        private void ApplyCoordinationRoleEffect(
            DungeonBoss target,
            bool threadlink)
        {
            if (CombatRole == PactCombatRole.Controller)
            {
                target.ApplyStagger(threadlink ? 0.12f : 0.2f);
            }
            else if (CombatRole == PactCombatRole.Warden ||
                     CombatRole == PactCombatRole.Vanguard)
            {
                target.ApplyStagger(threadlink ? 0.08f : 0.14f);
            }
            else if (CombatRole == PactCombatRole.Resonator)
            {
                target.ApplyExpose(threadlink ? 1.8f : 2.2f);
            }
        }

        private void OnDestroy()
        {
            artRig?.Dispose();
            artRig = null;
            for (var index = 0; index < assistTargetBuffer.Length; index++)
            {
                assistTargetBuffer[index] = null;
            }
        }

        private static float StableStagger(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                if (value != null)
                {
                    for (var index = 0; index < value.Length; index++)
                    {
                        hash ^= value[index];
                        hash *= 16777619u;
                    }
                }

                return (hash % 7u) * 0.09f;
            }
        }
    }

    public sealed partial class TetherfallGame
    {
        private readonly Dictionary<string, ThreadbindOpportunity> threadbindOpportunities =
            new Dictionary<string, ThreadbindOpportunity>(StringComparer.Ordinal);

        public ThreadbindOpportunity OnEligibleWildEchoswornDefeated(
            string enemyId,
            Vector3 position,
            string originId)
        {
            var species = TetherfallAccordSpeciesCatalog.FindByEligibleEnemyId(enemyId);
            if (species == null ||
                !TetherfallAccordSpeciesCatalog.IsEligibleWildEnemyId(enemyId))
            {
                return null;
            }

            var originKey = BuildThreadbindOriginKey(enemyId, position, originId);
            if (threadbindOpportunities.TryGetValue(originKey, out var existing) &&
                existing != null)
            {
                return existing;
            }

            var opportunity = ThreadbindOpportunity.Create(
                this,
                species,
                position,
                originKey);
            if (opportunity != null)
            {
                threadbindOpportunities[originKey] = opportunity;
            }

            return opportunity;
        }

        public IWorldInteractable SpawnThreadbindOpportunity(
            string eligibleEnemyId,
            Vector3 position,
            string originId)
        {
            return OnEligibleWildEchoswornDefeated(
                eligibleEnemyId,
                position,
                originId);
        }

        internal void OnThreadbindOpportunityClosed(
            string originKey,
            ThreadbindOpportunity opportunity)
        {
            if (string.IsNullOrEmpty(originKey) ||
                !threadbindOpportunities.TryGetValue(originKey, out var registered) ||
                registered != opportunity)
            {
                return;
            }

            threadbindOpportunities.Remove(originKey);
        }

        private static string BuildThreadbindOriginKey(
            string enemyId,
            Vector3 position,
            string originId)
        {
            if (!string.IsNullOrWhiteSpace(originId))
            {
                return originId.Trim();
            }

            return $"{enemyId}@{Mathf.RoundToInt(position.x)}:" +
                   $"{Mathf.RoundToInt(position.y)}:{Mathf.RoundToInt(position.z)}";
        }
    }
}
