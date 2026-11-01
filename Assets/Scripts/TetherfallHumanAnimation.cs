using System;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Selects the character carried by the persistent idle layer.
    /// </summary>
    public enum HumanIdleVariant : byte
    {
        Neutral,
        Relaxed,
        Alert,
        Working
    }

    /// <summary>
    /// Safe, idempotent entry point for generated human presentation.
    /// Attach only adds a driver to a collider-free visual hierarchy.
    /// </summary>
    public static class TetherfallHumanAnimation
    {
        public static HumanProceduralAnimator Attach(
            Transform visualRoot,
            TetherfallGame owner = null)
        {
            if (visualRoot == null)
            {
                return null;
            }

            var existing = FindAnimatorInChildren(visualRoot);
            if (existing == null)
            {
                existing = FindOwningAnimatorInParents(visualRoot);
            }

            if (existing != null)
            {
                if (!IsColliderFree(existing.transform))
                {
                    existing.ResetPose();
                    existing.enabled = false;
                    return null;
                }

                existing.SetOwner(owner);
                return existing;
            }

            var resolvedRoot = ResolveVisualRoot(visualRoot);
            if (resolvedRoot == null)
            {
                return null;
            }

            var animator = resolvedRoot.GetComponent<HumanProceduralAnimator>();
            if (animator == null)
            {
                animator = resolvedRoot.gameObject.AddComponent<HumanProceduralAnimator>();
            }

            animator.Configure(owner, resolvedRoot);
            return animator;
        }

        public static HumanProceduralAnimator Attach(
            TetherfallGame owner,
            Transform visualRoot)
        {
            return Attach(visualRoot, owner);
        }

        internal static bool IsColliderFree(Transform target)
        {
            return target != null &&
                   target.GetComponent<Collider>() == null &&
                   target.GetComponent<CharacterController>() == null &&
                   target.GetComponent<Rigidbody>() == null;
        }

        internal static bool ContainsIgnoreCase(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(fragment) &&
                   value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static HumanProceduralAnimator FindAnimatorInChildren(Transform root)
        {
            var animator = root.GetComponent<HumanProceduralAnimator>();
            if (animator != null)
            {
                return animator;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                animator = FindAnimatorInChildren(root.GetChild(index));
                if (animator != null)
                {
                    return animator;
                }
            }

            return null;
        }

        private static HumanProceduralAnimator FindOwningAnimatorInParents(Transform root)
        {
            var parent = root.parent;
            while (parent != null)
            {
                var animator = parent.GetComponent<HumanProceduralAnimator>();
                if (animator != null &&
                    animator.BindingRoot != null &&
                    IsDescendantOrSelf(root, animator.BindingRoot))
                {
                    return animator;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static bool IsDescendantOrSelf(Transform target, Transform possibleParent)
        {
            while (target != null)
            {
                if (target == possibleParent)
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }

        private static Transform ResolveVisualRoot(Transform requestedRoot)
        {
            if (IsColliderFree(requestedRoot) && ContainsAnimatableTarget(requestedRoot))
            {
                return requestedRoot;
            }

            Transform best = null;
            var bestScore = int.MinValue;
            FindBestVisualDescendant(requestedRoot, ref best, ref bestScore);
            return best;
        }

        private static void FindBestVisualDescendant(
            Transform root,
            ref Transform best,
            ref int bestScore)
        {
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (IsColliderFree(child) && ContainsAnimatableTarget(child))
                {
                    var score = ScoreVisualRoot(child);
                    if (score > bestScore)
                    {
                        best = child;
                        bestScore = score;
                    }
                }

                FindBestVisualDescendant(child, ref best, ref bestScore);
            }
        }

        private static int ScoreVisualRoot(Transform candidate)
        {
            var score = 0;
            var objectName = candidate.name;
            if (ContainsIgnoreCase(objectName, "human visual") ||
                ContainsIgnoreCase(objectName, "npc visual"))
            {
                score += 160;
            }
            else if (ContainsIgnoreCase(objectName, "visual") ||
                     ContainsIgnoreCase(objectName, "model") ||
                     ContainsIgnoreCase(objectName, "art") ||
                     ContainsIgnoreCase(objectName, "rig"))
            {
                score += 100;
            }
            else if (ContainsIgnoreCase(objectName, "body") ||
                     ContainsIgnoreCase(objectName, "torso"))
            {
                score += 60;
            }

            if (HasRenderableMesh(candidate))
            {
                score += IsAccessoryName(objectName) ? -80 : 30;
            }

            if (ContainsNamedRigTarget(candidate))
            {
                score += 80;
            }

            score -= HierarchyDepth(candidate);
            return score;
        }

        private static int HierarchyDepth(Transform target)
        {
            var depth = 0;
            while (target.parent != null)
            {
                depth++;
                target = target.parent;
            }

            return depth;
        }

        private static bool ContainsAnimatableTarget(Transform root)
        {
            if (IsColliderFree(root) &&
                ((HasRenderableMesh(root) && !IsAccessoryName(root.name)) ||
                 IsNamedRigTarget(root.name)))
            {
                return true;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                if (ContainsAnimatableTarget(root.GetChild(index)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsNamedRigTarget(Transform root)
        {
            if (IsColliderFree(root) && IsNamedRigTarget(root.name))
            {
                return true;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                if (ContainsNamedRigTarget(root.GetChild(index)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNamedRigTarget(string objectName)
        {
            return ContainsIgnoreCase(objectName, "rig root -") ||
                   ContainsIgnoreCase(objectName, "socket - pelvis") ||
                   ContainsIgnoreCase(objectName, "socket - chest") ||
                   ContainsIgnoreCase(objectName, "human body");
        }

        private static bool HasRenderableMesh(Transform target)
        {
            if (target.GetComponent<TextMesh>() != null ||
                target.GetComponent<ParticleSystem>() != null)
            {
                return false;
            }

            var renderer = target.GetComponent<Renderer>();
            return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
        }

        private static bool IsAccessoryName(string objectName)
        {
            return ContainsIgnoreCase(objectName, "label") ||
                   ContainsIgnoreCase(objectName, "nameplate") ||
                   ContainsIgnoreCase(objectName, "marker") ||
                   ContainsIgnoreCase(objectName, "sigil") ||
                   ContainsIgnoreCase(objectName, "mote") ||
                   ContainsIgnoreCase(objectName, "glow") ||
                   ContainsIgnoreCase(objectName, "weapon") ||
                   ContainsIgnoreCase(objectName, "equipment") ||
                   ContainsIgnoreCase(objectName, "tool") ||
                   ContainsIgnoreCase(objectName, "pack") ||
                   ContainsIgnoreCase(objectName, "sash") ||
                   ContainsIgnoreCase(objectName, "head") ||
                   ContainsIgnoreCase(objectName, "hand") ||
                   ContainsIgnoreCase(objectName, "foot");
        }
    }

    /// <summary>
    /// Allocation-free layered motion for human NPCs and companions.
    /// Hierarchy discovery is limited to Configure/RebindSockets; Update only uses cached poses.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(60)]
    public sealed class HumanProceduralAnimator : MonoBehaviour
    {
        private const float Tau = Mathf.PI * 2f;

        private static readonly string[] PelvisNames =
        {
            "Rig Root - Pelvis",
            "Socket - Pelvis",
            "Pelvis",
            "Hips",
            "mixamorig:Hips"
        };

        private static readonly string[] ChestNames =
        {
            "Rig Root - Chest",
            "Socket - Chest",
            "Chest",
            "UpperChest",
            "Spine2",
            "mixamorig:Spine2"
        };

        private static readonly string[] HeadNames =
        {
            "Rig Root - Head",
            "Socket - Head",
            "Head",
            "mixamorig:Head",
            "Companion head",
            "Resident head",
            "Vendor head"
        };

        private static readonly string[] LeftShoulderNames =
        {
            "Rig Root - Left Shoulder",
            "Socket - Left Shoulder",
            "Left Shoulder",
            "LeftShoulder",
            "mixamorig:LeftShoulder"
        };

        private static readonly string[] RightShoulderNames =
        {
            "Rig Root - Right Shoulder",
            "Socket - Right Shoulder",
            "Right Shoulder",
            "RightShoulder",
            "mixamorig:RightShoulder"
        };

        private static readonly string[] LeftElbowNames =
        {
            "Rig Root - Left Elbow",
            "Socket - Left Elbow",
            "Left Elbow",
            "LeftForeArm",
            "mixamorig:LeftForeArm"
        };

        private static readonly string[] RightElbowNames =
        {
            "Rig Root - Right Elbow",
            "Socket - Right Elbow",
            "Right Elbow",
            "RightForeArm",
            "mixamorig:RightForeArm"
        };

        private static readonly string[] LeftHandNames =
        {
            "Rig Root - Left Hand",
            "Socket - Left Hand",
            "Left Hand",
            "LeftHand",
            "mixamorig:LeftHand"
        };

        private static readonly string[] RightHandNames =
        {
            "Rig Root - Right Hand",
            "Socket - Right Hand",
            "Right Hand",
            "RightHand",
            "mixamorig:RightHand"
        };

        private static readonly string[] LeftHipNames =
        {
            "Rig Root - Left Hip",
            "Socket - Left Hip",
            "Left Hip",
            "LeftUpLeg",
            "mixamorig:LeftUpLeg"
        };

        private static readonly string[] RightHipNames =
        {
            "Rig Root - Right Hip",
            "Socket - Right Hip",
            "Right Hip",
            "RightUpLeg",
            "mixamorig:RightUpLeg"
        };

        private static readonly string[] LeftKneeNames =
        {
            "Rig Root - Left Knee",
            "Socket - Left Knee",
            "Left Knee",
            "LeftLeg",
            "mixamorig:LeftLeg"
        };

        private static readonly string[] RightKneeNames =
        {
            "Rig Root - Right Knee",
            "Socket - Right Knee",
            "Right Knee",
            "RightLeg",
            "mixamorig:RightLeg"
        };

        private static readonly string[] LeftFootNames =
        {
            "Rig Root - Left Foot",
            "Socket - Left Foot",
            "Left Foot",
            "LeftFoot",
            "mixamorig:LeftFoot"
        };

        private static readonly string[] RightFootNames =
        {
            "Rig Root - Right Foot",
            "Socket - Right Foot",
            "Right Foot",
            "RightFoot",
            "mixamorig:RightFoot"
        };

        private static readonly string[] PrimaryEquipmentNames =
        {
            "Socket - Weapon Motion",
            "Socket - Right Hand Grip",
            "Right Hand Grip",
            "Socket - Weapon",
            "Weapon Socket",
            "Socket - Back Equipment",
            "Equipment Socket"
        };

        private static readonly string[] SecondaryEquipmentNames =
        {
            "Socket - Left Hand Grip",
            "Left Hand Grip",
            "Socket - Offhand",
            "Offhand Socket",
            "Socket - Left Hip Utility",
            "Socket - Right Hip Utility"
        };

        private static readonly string[] FallbackBodyNames =
        {
            "Human Body",
            "NPC Body",
            "Companion Body",
            "Resident Body",
            "Vendor Body",
            "Body",
            "Torso",
            "Wayfinder Coat"
        };

        private enum GestureKind : byte
        {
            None,
            Talk,
            Vendor,
            QuestHandoff
        }

        private enum ActionKind : byte
        {
            None,
            Attack,
            Support,
            Hit
        }

        private enum SpectatorKind : byte
        {
            None,
            Cheer,
            Flinch
        }

        private struct HumanLocalPose
        {
            private Transform target;
            private Vector3 localPosition;
            private Quaternion localRotation;
            private Vector3 localScale;

            public Transform Target => target;
            public bool IsBound => target != null;

            public void Capture(Transform newTarget)
            {
                target = newTarget;
                if (target == null)
                {
                    localPosition = Vector3.zero;
                    localRotation = Quaternion.identity;
                    localScale = Vector3.one;
                    return;
                }

                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void Clear()
            {
                target = null;
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                localScale = Vector3.one;
            }

            public void Restore()
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
            }

            public void Apply(Vector3 positionOffset, Vector3 rotationOffset)
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = localPosition + positionOffset;
                target.localRotation = localRotation * Quaternion.Euler(rotationOffset);
                target.localScale = localScale;
            }
        }

        private TetherfallGame owner;
        private Transform bindingRoot;

        private HumanLocalPose pelvis;
        private HumanLocalPose chest;
        private HumanLocalPose head;
        private HumanLocalPose leftShoulder;
        private HumanLocalPose rightShoulder;
        private HumanLocalPose leftElbow;
        private HumanLocalPose rightElbow;
        private HumanLocalPose leftHand;
        private HumanLocalPose rightHand;
        private HumanLocalPose leftHip;
        private HumanLocalPose rightHip;
        private HumanLocalPose leftKnee;
        private HumanLocalPose rightKnee;
        private HumanLocalPose leftFoot;
        private HumanLocalPose rightFoot;
        private HumanLocalPose primaryEquipment;
        private HumanLocalPose secondaryEquipment;
        private HumanLocalPose fallbackBody;

        private HumanIdleVariant idleVariant;
        private float targetForward;
        private float targetStrafe;
        private float targetSpeed;
        private float forward;
        private float strafe;
        private float speed;
        private float gaitPhase;
        private float lifeTime;
        private float idlePhase;

        private GestureKind gestureKind;
        private float gestureElapsed;
        private float gestureDuration;
        private float gestureAnticipation;
        private float gestureRecovery;
        private float gestureStrength = 1f;

        private ActionKind actionKind;
        private float actionElapsed;
        private float actionDuration;
        private float actionAnticipation;
        private float actionRecovery;
        private float actionStrength = 1f;
        private float actionSide = 1f;

        private SpectatorKind spectatorKind;
        private float spectatorElapsed;
        private float spectatorDuration;
        private float spectatorAnticipation;
        private float spectatorRecovery;
        private float spectatorStrength = 1f;

        private int boundSocketCount;
        private bool usesFallbackBody;
        private bool hasBinding;

        public TetherfallGame Owner => owner;
        public Transform BindingRoot => bindingRoot;
        public HumanIdleVariant IdleVariant => idleVariant;
        public int BoundSocketCount => boundSocketCount;
        public bool IsBound => hasBinding;
        public bool UsesFallbackBody => usesFallbackBody;
        public bool IsTalking => gestureKind == GestureKind.Talk;
        public bool IsActing => actionKind != ActionKind.None;
        public bool IsSpectating => spectatorKind != SpectatorKind.None;

        public static HumanProceduralAnimator Attach(
            Transform visualRoot,
            TetherfallGame owner = null)
        {
            return TetherfallHumanAnimation.Attach(visualRoot, owner);
        }

        public static HumanProceduralAnimator Attach(
            TetherfallGame owner,
            Transform visualRoot)
        {
            return TetherfallHumanAnimation.Attach(visualRoot, owner);
        }

        public void SetOwner(TetherfallGame newOwner)
        {
            owner = newOwner;
        }

        /// <summary>
        /// Supplies normalized local direction and normalized movement speed.
        /// Forward/strafe are clamped to [-1, 1], speed to [0, 1].
        /// </summary>
        public void SetLocomotion(float forwardAmount, float strafeAmount, float speedAmount)
        {
            targetForward = Mathf.Clamp(forwardAmount, -1f, 1f);
            targetStrafe = Mathf.Clamp(strafeAmount, -1f, 1f);
            targetSpeed = Mathf.Clamp01(speedAmount);
        }

        public void StopLocomotion()
        {
            targetForward = 0f;
            targetStrafe = 0f;
            targetSpeed = 0f;
        }

        public void SetIdleVariant(HumanIdleVariant variant)
        {
            idleVariant = variant;
        }

        public void TriggerTalk(float strength = 1f)
        {
            StartGesture(GestureKind.Talk, 0.92f, 0.1f, 0.24f, strength);
        }

        public void TriggerVendorGesture(float strength = 1f)
        {
            StartGesture(GestureKind.Vendor, 1.18f, 0.16f, 0.32f, strength);
        }

        public void TriggerQuestHandoff(float strength = 1f)
        {
            StartGesture(GestureKind.QuestHandoff, 1.28f, 0.18f, 0.38f, strength);
        }

        /// <summary>
        /// Starts a complete wind-up, release, and recovery. Mirrored attacks lead left.
        /// </summary>
        public void TriggerAttack(bool mirrored = false, float strength = 1f)
        {
            StartAction(ActionKind.Attack, 0.72f, 0.14f, 0.32f, strength);
            actionSide = mirrored ? -1f : 1f;
        }

        public void TriggerSupport(float strength = 1f)
        {
            StartAction(ActionKind.Support, 1.04f, 0.16f, 0.3f, strength);
        }

        public void TriggerHitReact(float strength = 1f, bool mirrored = false)
        {
            StartAction(ActionKind.Hit, 0.46f, 0.035f, 0.25f, strength);
            actionSide = mirrored ? -1f : 1f;
        }

        public void TriggerSpectateCheer(float strength = 1f)
        {
            StartSpectator(SpectatorKind.Cheer, 1.16f, 0.14f, 0.34f, strength);
        }

        public void TriggerSpectateFlinch(float strength = 1f)
        {
            StartSpectator(SpectatorKind.Flinch, 0.56f, 0.045f, 0.28f, strength);
        }

        /// <summary>
        /// Re-scans a rebuilt visual hierarchy. Do not call this every frame.
        /// </summary>
        public void RebindSockets()
        {
            ResetPose();
            BindSockets();
        }

        /// <summary>
        /// Restores every captured local position, rotation, and scale.
        /// </summary>
        public void ResetPose()
        {
            pelvis.Restore();
            chest.Restore();
            head.Restore();
            leftShoulder.Restore();
            rightShoulder.Restore();
            leftElbow.Restore();
            rightElbow.Restore();
            leftHand.Restore();
            rightHand.Restore();
            leftHip.Restore();
            rightHip.Restore();
            leftKnee.Restore();
            rightKnee.Restore();
            leftFoot.Restore();
            rightFoot.Restore();
            primaryEquipment.Restore();
            secondaryEquipment.Restore();
            fallbackBody.Restore();
        }

        internal void Configure(TetherfallGame newOwner, Transform visualRoot)
        {
            owner = newOwner;
            if (visualRoot == null || !TetherfallHumanAnimation.IsColliderFree(visualRoot))
            {
                ResetPose();
                ClearBindings();
                bindingRoot = null;
                enabled = false;
                return;
            }

            enabled = true;
            if (bindingRoot != visualRoot)
            {
                ResetPose();
                bindingRoot = visualRoot;
                idlePhase = ComputeIdlePhase();
                BindSockets();
            }
            else if (!hasBinding)
            {
                BindSockets();
            }
        }

        private void OnEnable()
        {
            if (bindingRoot == null &&
                TetherfallHumanAnimation.IsColliderFree(transform))
            {
                bindingRoot = transform;
                idlePhase = ComputeIdlePhase();
                BindSockets();
            }
        }

        private float ComputeIdlePhase()
        {
            var worldPosition = transform.position;
            return Mathf.Repeat(
                Mathf.Abs(
                    worldPosition.x * 0.137f +
                    worldPosition.z * 0.071f +
                    transform.GetSiblingIndex() * 0.619f +
                    name.Length * 0.113f),
                Tau);
        }

        private void OnDisable()
        {
            ResetPose();
        }

        private void Update()
        {
            if (!hasBinding || (owner != null && owner.GameplayPaused))
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Tick(Mathf.Min(deltaTime, 0.1f));
        }

        private void Tick(float deltaTime)
        {
            lifeTime += deltaTime;

            var locomotionBlend = Damp(12f, deltaTime);
            forward = Mathf.Lerp(forward, targetForward, locomotionBlend);
            strafe = Mathf.Lerp(strafe, targetStrafe, locomotionBlend);
            speed = Mathf.Lerp(speed, targetSpeed, locomotionBlend);

            var directionMagnitude = Mathf.Clamp01(Mathf.Sqrt(
                forward * forward + strafe * strafe));
            var movement = directionMagnitude * speed;
            gaitPhase = Mathf.Repeat(
                gaitPhase + deltaTime * Mathf.Lerp(2.4f, 10.8f, movement),
                Tau);

            AdvanceLayers(deltaTime);

            // Every layer starts from captured rest transforms, so offsets never accumulate.
            ResetPose();

            var stride = Mathf.Sin(gaitPhase) * movement;
            var step = Mathf.Cos(gaitPhase) * movement;
            var breath = Mathf.Sin(lifeTime * 1.55f + idlePhase);
            var sway = Mathf.Sin(lifeTime * 0.72f + idlePhase * 0.63f);
            var glance = Mathf.Sin(lifeTime * 0.39f + idlePhase * 1.31f);

            var idlePitch = 0f;
            var idleYaw = sway * 0.8f;
            var idleRoll = sway * 0.55f;
            var idleArm = 0f;
            var idleHeadYaw = glance * 1.2f;
            switch (idleVariant)
            {
                case HumanIdleVariant.Relaxed:
                    idlePitch = -1.8f;
                    idleYaw *= 1.45f;
                    idleRoll += 1.2f;
                    idleArm = 5f;
                    idleHeadYaw *= 1.75f;
                    break;

                case HumanIdleVariant.Alert:
                    idlePitch = 2.4f;
                    idleYaw *= 0.45f;
                    idleRoll *= 0.35f;
                    idleArm = -3f;
                    idleHeadYaw *= 2.25f;
                    break;

                case HumanIdleVariant.Working:
                    idlePitch = -3.2f;
                    idleYaw *= 1.8f;
                    idleRoll *= 1.35f;
                    idleArm = 8f + Mathf.Sin(lifeTime * 2.1f + idlePhase) * 2f;
                    idleHeadYaw *= 0.7f;
                    break;
            }

            var gestureWeight = LayerEnvelope(
                gestureElapsed,
                gestureDuration,
                gestureAnticipation,
                gestureRecovery);
            var gestureProgress = gestureDuration > 0f
                ? Mathf.Clamp01(gestureElapsed / gestureDuration)
                : 0f;
            gestureWeight *= gestureStrength;

            var talk = 0f;
            var talkBeat = 0f;
            var vendor = 0f;
            var vendorWave = 0f;
            var handoff = 0f;
            if (gestureKind == GestureKind.Talk)
            {
                talk = gestureWeight;
                talkBeat = Mathf.Sin(gestureProgress * Mathf.PI * 5f) * talk;
            }
            else if (gestureKind == GestureKind.Vendor)
            {
                vendor = gestureWeight;
                vendorWave = Mathf.Sin(gestureProgress * Mathf.PI * 4f) * vendor;
            }
            else if (gestureKind == GestureKind.QuestHandoff)
            {
                handoff = gestureWeight;
            }

            var attackPose = 0f;
            var attackDrive = 0f;
            var support = 0f;
            var supportPulse = 0f;
            var hit = 0f;
            if (actionKind == ActionKind.Attack)
            {
                EvaluateAttack(out attackPose, out attackDrive);
            }
            else
            {
                var actionWeight = LayerEnvelope(
                                       actionElapsed,
                                       actionDuration,
                                       actionAnticipation,
                                       actionRecovery) *
                                   actionStrength;
                var actionProgress = actionDuration > 0f
                    ? Mathf.Clamp01(actionElapsed / actionDuration)
                    : 0f;
                if (actionKind == ActionKind.Support)
                {
                    support = actionWeight;
                    supportPulse =
                        Mathf.Sin(actionProgress * Mathf.PI * 3f) * support;
                }
                else if (actionKind == ActionKind.Hit)
                {
                    hit = actionWeight;
                }
            }

            var spectatorWeight = LayerEnvelope(
                                      spectatorElapsed,
                                      spectatorDuration,
                                      spectatorAnticipation,
                                      spectatorRecovery) *
                                  spectatorStrength;
            var spectatorProgress = spectatorDuration > 0f
                ? Mathf.Clamp01(spectatorElapsed / spectatorDuration)
                : 0f;
            var cheer = spectatorKind == SpectatorKind.Cheer
                ? spectatorWeight
                : 0f;
            var cheerBounce = spectatorKind == SpectatorKind.Cheer
                ? Mathf.Abs(Mathf.Sin(spectatorProgress * Mathf.PI * 3f)) * cheer
                : 0f;
            var flinch = spectatorKind == SpectatorKind.Flinch
                ? spectatorWeight
                : 0f;

            var bob =
                Mathf.Abs(step) * 0.038f +
                breath * 0.006f +
                cheerBounce * 0.055f -
                hit * 0.025f;
            var bodyPitch =
                idlePitch -
                forward * movement * 5.5f +
                breath * 0.55f -
                attackDrive * 8.5f -
                support * 3f +
                hit * 7.5f +
                flinch * 5f;
            var bodyYaw =
                idleYaw -
                stride * 4f -
                strafe * movement * 2.8f +
                attackPose * actionSide * 11f -
                hit * actionSide * 8f;
            var bodyRoll =
                idleRoll -
                strafe * movement * 5f -
                attackPose * actionSide * 2.4f +
                hit * actionSide * 7f;
            var armSwing = stride * 24f;
            var legSwing = stride * 28f;
            var leadingRight = actionSide > 0f ? 1f : 0f;
            var leadingLeft = 1f - leadingRight;

            var pelvisPosition = new Vector3(
                0f,
                bob,
                -attackDrive * 0.018f + hit * 0.012f);
            var pelvisRotation = new Vector3(
                -forward * movement * 1.5f,
                bodyYaw * 0.38f,
                bodyRoll * 0.55f);
            var chestRotation = new Vector3(
                bodyPitch,
                bodyYaw * 0.72f + talkBeat * 1.8f,
                bodyRoll);
            var headRotation = new Vector3(
                -bodyPitch * 0.32f + talkBeat * 1.6f + handoff * 3f,
                idleHeadYaw - bodyYaw * 0.28f + talkBeat * 2.2f,
                -bodyRoll * 0.32f - talkBeat * 0.8f);

            var leftShoulderRotation = new Vector3(
                -armSwing + idleArm -
                talk * 8f - talkBeat * 7f -
                vendor * 4f -
                handoff * 42f -
                attackPose * 46f * leadingLeft -
                attackDrive * 18f * leadingRight -
                support * 48f -
                hit * 20f -
                cheer * 112f +
                flinch * 28f,
                -attackPose * actionSide * 8f -
                handoff * 5f +
                flinch * 8f,
                -vendor * 6f -
                support * 15f -
                cheer * 18f +
                flinch * 18f);
            var rightShoulderRotation = new Vector3(
                armSwing + idleArm -
                talk * 26f + talkBeat * 9f -
                vendor * 54f + vendorWave * 13f -
                handoff * 42f -
                attackPose * 46f * leadingRight -
                attackDrive * 18f * leadingLeft -
                support * 48f -
                hit * 20f -
                cheer * 112f +
                flinch * 28f,
                attackPose * actionSide * 8f +
                vendor * 7f +
                handoff * 5f -
                flinch * 8f,
                vendor * 24f +
                support * 15f +
                cheer * 18f -
                flinch * 18f);

            var leftElbowRotation = new Vector3(
                -Mathf.Max(0f, armSwing) * 0.3f -
                talk * 18f -
                handoff * 22f -
                attackDrive * 24f * leadingLeft -
                support * 34f -
                hit * 30f -
                cheer * 38f +
                flinch * 44f,
                handoff * 5f,
                handoff * 4f);
            var rightElbowRotation = new Vector3(
                -Mathf.Max(0f, -armSwing) * 0.3f -
                talk * 38f - talkBeat * 8f -
                vendor * 54f - vendorWave * 16f -
                handoff * 22f -
                attackDrive * 24f * leadingRight -
                support * 34f -
                hit * 30f -
                cheer * 38f +
                flinch * 44f,
                -vendor * 8f - handoff * 5f,
                -vendor * 9f - handoff * 4f);

            var leftHandRotation = new Vector3(
                talkBeat * 3f +
                handoff * 10f +
                supportPulse * 8f +
                cheer * 8f,
                -handoff * 8f,
                talkBeat * 5f - support * 7f);
            var rightHandRotation = new Vector3(
                -talkBeat * 5f +
                vendorWave * 18f +
                handoff * 10f -
                attackPose * 15f * leadingRight +
                supportPulse * 8f +
                cheer * 8f,
                vendorWave * 10f + handoff * 8f,
                -talkBeat * 6f + vendorWave * 9f + support * 7f);

            var leftHipRotation = new Vector3(
                legSwing - cheerBounce * 12f + hit * 8f,
                0f,
                -strafe * movement * 2f);
            var rightHipRotation = new Vector3(
                -legSwing - cheerBounce * 12f + hit * 8f,
                0f,
                -strafe * movement * 2f);
            var leftKneeRotation = new Vector3(
                Mathf.Max(0f, -stride) * 34f +
                cheerBounce * 28f,
                0f,
                0f);
            var rightKneeRotation = new Vector3(
                Mathf.Max(0f, stride) * 34f +
                cheerBounce * 28f,
                0f,
                0f);
            var leftFootRotation = new Vector3(-step * 8f, 0f, 0f);
            var rightFootRotation = new Vector3(step * 8f, 0f, 0f);

            var primaryEquipmentPosition = new Vector3(
                attackDrive * actionSide * 0.035f,
                support * 0.018f,
                attackDrive * 0.055f + handoff * 0.02f);
            var primaryEquipmentRotation = new Vector3(
                -attackPose * 38f -
                attackDrive * 22f +
                vendorWave * 6f,
                attackPose * actionSide * 24f,
                attackPose * actionSide * 12f + vendorWave * 8f);
            var secondaryEquipmentRotation = new Vector3(
                -support * 12f + supportPulse * 4f,
                handoff * 8f,
                -handoff * 6f);

            pelvis.Apply(pelvisPosition, pelvisRotation);
            chest.Apply(Vector3.zero, chestRotation);
            head.Apply(Vector3.zero, headRotation);
            leftShoulder.Apply(Vector3.zero, leftShoulderRotation);
            rightShoulder.Apply(Vector3.zero, rightShoulderRotation);
            leftElbow.Apply(Vector3.zero, leftElbowRotation);
            rightElbow.Apply(Vector3.zero, rightElbowRotation);
            leftHand.Apply(Vector3.zero, leftHandRotation);
            rightHand.Apply(Vector3.zero, rightHandRotation);
            leftHip.Apply(Vector3.zero, leftHipRotation);
            rightHip.Apply(Vector3.zero, rightHipRotation);
            leftKnee.Apply(Vector3.zero, leftKneeRotation);
            rightKnee.Apply(Vector3.zero, rightKneeRotation);
            leftFoot.Apply(Vector3.zero, leftFootRotation);
            rightFoot.Apply(Vector3.zero, rightFootRotation);
            primaryEquipment.Apply(primaryEquipmentPosition, primaryEquipmentRotation);
            secondaryEquipment.Apply(Vector3.zero, secondaryEquipmentRotation);

            fallbackBody.Apply(
                pelvisPosition,
                new Vector3(
                    bodyPitch +
                    talkBeat * 1.2f -
                    vendorWave * 1.5f +
                    handoff * 2f -
                    cheerBounce * 3f,
                    bodyYaw + talkBeat * 1.5f + vendorWave * 2f,
                    bodyRoll +
                    vendorWave * 1.2f +
                    cheer * sway * 1.5f));
        }

        private void StartGesture(
            GestureKind kind,
            float duration,
            float anticipation,
            float recovery,
            float strength)
        {
            gestureKind = kind;
            gestureElapsed = 0f;
            gestureDuration = duration;
            gestureAnticipation = anticipation;
            gestureRecovery = recovery;
            gestureStrength = Mathf.Clamp(strength, 0.1f, 2f);
        }

        private void StartAction(
            ActionKind kind,
            float duration,
            float anticipation,
            float recovery,
            float strength)
        {
            actionKind = kind;
            actionElapsed = 0f;
            actionDuration = duration;
            actionAnticipation = anticipation;
            actionRecovery = recovery;
            actionStrength = Mathf.Clamp(strength, 0.1f, 2f);
        }

        private void StartSpectator(
            SpectatorKind kind,
            float duration,
            float anticipation,
            float recovery,
            float strength)
        {
            spectatorKind = kind;
            spectatorElapsed = 0f;
            spectatorDuration = duration;
            spectatorAnticipation = anticipation;
            spectatorRecovery = recovery;
            spectatorStrength = Mathf.Clamp(strength, 0.1f, 2f);
        }

        private void AdvanceLayers(float deltaTime)
        {
            if (gestureKind != GestureKind.None)
            {
                gestureElapsed += deltaTime;
                if (gestureElapsed >= gestureDuration)
                {
                    gestureKind = GestureKind.None;
                    gestureElapsed = 0f;
                }
            }

            if (actionKind != ActionKind.None)
            {
                actionElapsed += deltaTime;
                if (actionElapsed >= actionDuration)
                {
                    actionKind = ActionKind.None;
                    actionElapsed = 0f;
                }
            }

            if (spectatorKind != SpectatorKind.None)
            {
                spectatorElapsed += deltaTime;
                if (spectatorElapsed >= spectatorDuration)
                {
                    spectatorKind = SpectatorKind.None;
                    spectatorElapsed = 0f;
                }
            }
        }

        private void EvaluateAttack(out float pose, out float drive)
        {
            var releaseEnd = actionAnticipation + 0.16f;
            if (actionElapsed < actionAnticipation)
            {
                var anticipation = Smooth01(actionElapsed / actionAnticipation);
                pose = -anticipation;
                drive = -anticipation * 0.22f;
            }
            else if (actionElapsed < releaseEnd)
            {
                var release = Smooth01(
                    (actionElapsed - actionAnticipation) /
                    (releaseEnd - actionAnticipation));
                pose = Mathf.Lerp(-1f, 1f, release);
                drive = Mathf.Lerp(-0.22f, 1f, release);
            }
            else
            {
                var recovery = Smooth01(
                    (actionElapsed - releaseEnd) /
                    Mathf.Max(0.01f, actionDuration - releaseEnd));
                pose = Mathf.Lerp(1f, 0f, recovery);
                drive = 1f - recovery;
            }

            pose *= actionStrength;
            drive *= actionStrength;
        }

        private void BindSockets()
        {
            ClearBindings();
            if (bindingRoot == null ||
                !TetherfallHumanAnimation.IsColliderFree(bindingRoot))
            {
                return;
            }

            CaptureUnique(ref pelvis, FindNamedSafe(bindingRoot, PelvisNames));
            CaptureUnique(ref chest, FindNamedSafe(bindingRoot, ChestNames));
            CaptureUnique(ref head, FindNamedSafe(bindingRoot, HeadNames));
            CaptureUnique(
                ref leftShoulder,
                FindNamedSafe(bindingRoot, LeftShoulderNames));
            CaptureUnique(
                ref rightShoulder,
                FindNamedSafe(bindingRoot, RightShoulderNames));
            CaptureUnique(ref leftElbow, FindNamedSafe(bindingRoot, LeftElbowNames));
            CaptureUnique(ref rightElbow, FindNamedSafe(bindingRoot, RightElbowNames));
            CaptureUnique(ref leftHand, FindNamedSafe(bindingRoot, LeftHandNames));
            CaptureUnique(ref rightHand, FindNamedSafe(bindingRoot, RightHandNames));
            CaptureUnique(ref leftHip, FindNamedSafe(bindingRoot, LeftHipNames));
            CaptureUnique(ref rightHip, FindNamedSafe(bindingRoot, RightHipNames));
            CaptureUnique(ref leftKnee, FindNamedSafe(bindingRoot, LeftKneeNames));
            CaptureUnique(ref rightKnee, FindNamedSafe(bindingRoot, RightKneeNames));
            CaptureUnique(ref leftFoot, FindNamedSafe(bindingRoot, LeftFootNames));
            CaptureUnique(ref rightFoot, FindNamedSafe(bindingRoot, RightFootNames));
            CaptureUnique(
                ref primaryEquipment,
                FindNamedSafe(bindingRoot, PrimaryEquipmentNames));
            CaptureUnique(
                ref secondaryEquipment,
                FindNamedSafe(bindingRoot, SecondaryEquipmentNames));

            var hasCoreRig =
                pelvis.IsBound ||
                chest.IsBound ||
                leftShoulder.IsBound ||
                rightShoulder.IsBound ||
                leftHip.IsBound ||
                rightHip.IsBound;
            if (!hasCoreRig)
            {
                CaptureUnique(ref fallbackBody, FindFallbackBody());
                usesFallbackBody = fallbackBody.IsBound;
            }

            hasBinding = boundSocketCount > 0;
        }

        private void ClearBindings()
        {
            pelvis.Clear();
            chest.Clear();
            head.Clear();
            leftShoulder.Clear();
            rightShoulder.Clear();
            leftElbow.Clear();
            rightElbow.Clear();
            leftHand.Clear();
            rightHand.Clear();
            leftHip.Clear();
            rightHip.Clear();
            leftKnee.Clear();
            rightKnee.Clear();
            leftFoot.Clear();
            rightFoot.Clear();
            primaryEquipment.Clear();
            secondaryEquipment.Clear();
            fallbackBody.Clear();
            boundSocketCount = 0;
            usesFallbackBody = false;
            hasBinding = false;
        }

        private void CaptureUnique(ref HumanLocalPose pose, Transform target)
        {
            if (target == null ||
                !TetherfallHumanAnimation.IsColliderFree(target) ||
                IsTargetBound(target))
            {
                pose.Clear();
                return;
            }

            pose.Capture(target);
            boundSocketCount++;
        }

        private bool IsTargetBound(Transform target)
        {
            return pelvis.Target == target ||
                   chest.Target == target ||
                   head.Target == target ||
                   leftShoulder.Target == target ||
                   rightShoulder.Target == target ||
                   leftElbow.Target == target ||
                   rightElbow.Target == target ||
                   leftHand.Target == target ||
                   rightHand.Target == target ||
                   leftHip.Target == target ||
                   rightHip.Target == target ||
                   leftKnee.Target == target ||
                   rightKnee.Target == target ||
                   leftFoot.Target == target ||
                   rightFoot.Target == target ||
                   primaryEquipment.Target == target ||
                   secondaryEquipment.Target == target ||
                   fallbackBody.Target == target;
        }

        private Transform FindFallbackBody()
        {
            var named = FindNamedSafe(bindingRoot, FallbackBodyNames);
            if (IsFallbackCandidate(named))
            {
                return named;
            }

            if (IsFallbackCandidate(bindingRoot))
            {
                return bindingRoot;
            }

            Transform best = null;
            var bestScore = int.MinValue;
            FindBestFallback(bindingRoot, ref best, ref bestScore);
            return best;
        }

        private void FindBestFallback(
            Transform root,
            ref Transform best,
            ref int bestScore)
        {
            if (IsFallbackCandidate(root))
            {
                var score = ScoreFallbackName(root.name);
                if (score > bestScore)
                {
                    best = root;
                    bestScore = score;
                }
            }

            for (var index = 0; index < root.childCount; index++)
            {
                FindBestFallback(root.GetChild(index), ref best, ref bestScore);
            }
        }

        private bool IsFallbackCandidate(Transform target)
        {
            if (target == null ||
                IsTargetBound(target) ||
                !TetherfallHumanAnimation.IsColliderFree(target) ||
                target.GetComponent<TextMesh>() != null ||
                target.GetComponent<ParticleSystem>() != null)
            {
                return false;
            }

            var renderer = target.GetComponent<Renderer>();
            return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
        }

        private static int ScoreFallbackName(string objectName)
        {
            if (TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "human body") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "npc body"))
            {
                return 100;
            }

            if (TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "body") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "torso") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "coat"))
            {
                return 70;
            }

            if (TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "visual") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "model"))
            {
                return 50;
            }

            if (TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "head") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "hand") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "foot") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "weapon") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "equipment") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "tool") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "pack") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "label") ||
                TetherfallHumanAnimation.ContainsIgnoreCase(objectName, "marker"))
            {
                return -100;
            }

            return 0;
        }

        private static Transform FindNamedSafe(Transform root, string[] names)
        {
            if (root == null || names == null)
            {
                return null;
            }

            if (TetherfallHumanAnimation.IsColliderFree(root))
            {
                for (var index = 0; index < names.Length; index++)
                {
                    if (string.Equals(
                            root.name,
                            names[index],
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return root;
                    }
                }
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var found = FindNamedSafe(root.GetChild(index), names);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static float LayerEnvelope(
            float elapsed,
            float duration,
            float anticipation,
            float recovery)
        {
            if (duration <= 0f || elapsed < 0f || elapsed >= duration)
            {
                return 0f;
            }

            if (elapsed < anticipation)
            {
                return Smooth01(elapsed / Mathf.Max(0.001f, anticipation));
            }

            var recoveryStart = duration - recovery;
            if (elapsed > recoveryStart)
            {
                return 1f - Smooth01(
                    (elapsed - recoveryStart) / Mathf.Max(0.001f, recovery));
            }

            return 1f;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float Damp(float sharpness, float deltaTime)
        {
            return 1f - Mathf.Exp(-sharpness * deltaTime);
        }
    }
}
