using UnityEngine;

namespace Tetherfall
{
    public enum TutorialStep
    {
        // These values are persisted. Append new steps instead of reordering them.
        Welcome = 0,
        MoveAndLook = 1,
        CombatBasics = 2,
        FieldInteraction = 3,
        FieldKit = 4,
        FirstPact = 5,
        Complete = 6,
        TraversalBasics = 7,
        PartyBasics = 8
    }

    public enum TutorialEvent
    {
        Move = 0,
        Look = 1,
        Attack = 2,
        Guard = 3,
        Dodge = 4,
        Interact = 5,
        OpenFieldKit = 6,
        FormPact = 7,
        Sprint = 8,
        Jump = 9,
        ReviewParty = 10,
        Glide = 11
    }

    public sealed partial class TetherfallGame
    {
        private TutorialStep tutorialStep;
        private bool tutorialVisible = true;
        private bool tutorialSkipped;
        private bool tutorialMoved;
        private bool tutorialLooked;
        private bool tutorialAttacked;
        private bool tutorialGuarded;
        private bool tutorialDodged;
        private bool tutorialInteracted;
        private bool tutorialOpenedFieldKit;
        private bool tutorialFormedPact;
        private bool tutorialSprinted;
        private bool tutorialJumped;
        private bool tutorialGlided;
        private bool tutorialReviewedParty;
        private bool tutorialStateDirtyAfterRestore;
        private bool tutorialSpaceReleasedAfterJump;
        private bool tutorialGlideHoldCandidate;
        private bool tutorialPlayerWasGrounded;
        private float tutorialGlideHoldStartedAt;
        private CharacterController tutorialPlayerController;
        private GUIStyle tutorialHeadingStyle;
        private GUIStyle tutorialBodyStyle;
        private GUIStyle tutorialMutedStyle;
        private float cachedTutorialTextScale = -1f;

        private const int MovedFlag = 1;
        private const int LookedFlag = 2;
        private const int AttackedFlag = 4;
        private const int GuardedFlag = 8;
        private const int DodgedFlag = 16;
        private const int InteractedFlag = 32;
        private const int OpenedFieldKitFlag = 64;
        private const int FormedPactFlag = 128;
        private const int SprintedFlag = 256;
        private const int JumpedFlag = 512;
        private const int ReviewedPartyFlag = 1024;
        private const int GlidedFlag = 2048;
        private const int AddedTutorialFlags = SprintedFlag | JumpedFlag | ReviewedPartyFlag | GlidedFlag;
        private const float TutorialGlideHoldTime = 0.12f;

        public bool GameplayPaused =>
            CodexOpen ||
            activePanel != GamePanel.None ||
            IsPlayerSettlementInputCaptured ||
            TetherfallInputBindings.Shared.WasPressedThisFrame(
                TetherfallInputAction.OpenSettlementPlanning) ||
            Cursor.lockState != CursorLockMode.Locked ||
            !Application.isFocused ||
            CombatHitstopActive;
        public bool TutorialComplete => tutorialStep == TutorialStep.Complete;

        private void InitializeTutorial()
        {
            tutorialStep = TutorialStep.Welcome;
            tutorialVisible = true;
            tutorialSkipped = false;
            tutorialStateDirtyAfterRestore = false;
            ResetTutorialEventFlags();
        }

        private void UpdateTutorial()
        {
            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.ToggleGuidance))
            {
                tutorialVisible = !tutorialVisible;
            }

            if (tutorialStateDirtyAfterRestore)
            {
                tutorialStateDirtyAfterRestore = false;
                RequestSave();
            }

            if (TutorialComplete)
            {
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.SkipGuidance))
            {
                tutorialSkipped = true;
                tutorialStep = TutorialStep.Complete;
                tutorialVisible = false;
                Notify("Guidance skipped. H shows controls; Tab opens the field log.");
                RequestSave();
                return;
            }

            EvaluateTutorialProgress();
            if (TutorialComplete || GameplayPaused)
            {
                return;
            }

            var stepAtInputStart = tutorialStep;
            var moveInput = TetherfallInputBindings.Shared.GetMove();
            var horizontal = moveInput.x;
            var vertical = moveInput.y;
            var isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
            var inputChanged = false;

            if (isMoving)
            {
                inputChanged |= SetTutorialEventFlag(TutorialEvent.Move);
                if (TetherfallInputBindings.Shared.IsHeld(TetherfallInputAction.Sprint))
                {
                    inputChanged |= SetTutorialEventFlag(TutorialEvent.Sprint);
                }
            }

            var lookInput = TetherfallInputBindings.Shared.GetLook();
            if (Mathf.Abs(lookInput.x) > 0.05f ||
                Mathf.Abs(lookInput.y) > 0.05f)
            {
                inputChanged |= SetTutorialEventFlag(TutorialEvent.Look);
            }

            inputChanged |= UpdateTutorialTraversalInput();

            if (ActiveCombatPactCount > 0 &&
                (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.TacticalCommand) ||
                 TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.RotateCompanionPairOrRallyBeat2) ||
                 TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.RotateSupportPactOrRallyBeat1)))
            {
                inputChanged |= SetTutorialEventFlag(TutorialEvent.ReviewParty);
            }

            if (stepAtInputStart == TutorialStep.PartyBasics &&
                TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.BeginTutorial))
            {
                inputChanged |= SetTutorialEventFlag(TutorialEvent.ReviewParty);
            }

            if (inputChanged)
            {
                RequestSave();
            }

            if (stepAtInputStart == TutorialStep.Welcome &&
                TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.BeginTutorial))
            {
                AdvanceTutorial();
                return;
            }

            EvaluateTutorialProgress();
        }

        public void RecordTutorialEvent(TutorialEvent tutorialEvent)
        {
            if (TutorialComplete)
            {
                return;
            }

            if (!SetTutorialEventFlag(tutorialEvent))
            {
                return;
            }

            RequestSave();
            EvaluateTutorialProgress();
        }

        private bool SetTutorialEventFlag(TutorialEvent tutorialEvent)
        {
            switch (tutorialEvent)
            {
                case TutorialEvent.Move:
                    if (tutorialMoved) return false;
                    tutorialMoved = true;
                    return true;
                case TutorialEvent.Look:
                    if (tutorialLooked) return false;
                    tutorialLooked = true;
                    return true;
                case TutorialEvent.Attack:
                    if (tutorialAttacked) return false;
                    tutorialAttacked = true;
                    return true;
                case TutorialEvent.Guard:
                    if (tutorialGuarded) return false;
                    tutorialGuarded = true;
                    return true;
                case TutorialEvent.Dodge:
                    if (tutorialDodged) return false;
                    tutorialDodged = true;
                    return true;
                case TutorialEvent.Interact:
                    if (tutorialInteracted) return false;
                    tutorialInteracted = true;
                    return true;
                case TutorialEvent.OpenFieldKit:
                    if (tutorialOpenedFieldKit) return false;
                    tutorialOpenedFieldKit = true;
                    return true;
                case TutorialEvent.FormPact:
                    if (tutorialFormedPact) return false;
                    tutorialFormedPact = true;
                    return true;
                case TutorialEvent.Sprint:
                    if (tutorialSprinted) return false;
                    tutorialSprinted = true;
                    return true;
                case TutorialEvent.Jump:
                    if (tutorialJumped) return false;
                    tutorialJumped = true;
                    return true;
                case TutorialEvent.Glide:
                    if (tutorialGlided) return false;
                    tutorialGlided = true;
                    return true;
                case TutorialEvent.ReviewParty:
                    if (tutorialReviewedParty) return false;
                    tutorialReviewedParty = true;
                    return true;
                default:
                    return false;
            }
        }

        private bool UpdateTutorialTraversalInput()
        {
            if (tutorialPlayerController == null && Player != null)
            {
                tutorialPlayerController = Player.GetComponent<CharacterController>();
            }

            if (tutorialPlayerController == null)
            {
                return false;
            }

            var grounded = tutorialPlayerController.isGrounded;
            var spacePressed =
                TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.JumpGlideClimb);
            var spaceReleased =
                TetherfallInputBindings.Shared.WasReleasedThisFrame(TetherfallInputAction.JumpGlideClimb);
            var canUseWayfinderTraversal = Player != null && !Player.IsMounted;
            var changed = false;

            if (canUseWayfinderTraversal && Player.IsGliding)
            {
                changed |= SetTutorialEventFlag(TutorialEvent.Glide);
                tutorialGlideHoldCandidate = false;
            }

            if (grounded)
            {
                tutorialSpaceReleasedAfterJump = false;
                tutorialGlideHoldCandidate = false;
            }

            if (spacePressed && canUseWayfinderTraversal)
            {
                if (tutorialJumped && tutorialSpaceReleasedAfterJump && !grounded)
                {
                    tutorialGlideHoldCandidate = true;
                    tutorialGlideHoldStartedAt = Time.time;
                }
                else if (grounded || tutorialPlayerWasGrounded)
                {
                    changed |= SetTutorialEventFlag(TutorialEvent.Jump);
                    tutorialSpaceReleasedAfterJump = false;
                    tutorialGlideHoldCandidate = false;
                }
            }

            if (spaceReleased)
            {
                tutorialGlideHoldCandidate = false;
                if (tutorialJumped && canUseWayfinderTraversal && !grounded)
                {
                    tutorialSpaceReleasedAfterJump = true;
                }
            }

            if (tutorialGlideHoldCandidate)
            {
                if (!TetherfallInputBindings.Shared.IsHeld(TetherfallInputAction.JumpGlideClimb) ||
                    grounded ||
                    !canUseWayfinderTraversal)
                {
                    tutorialGlideHoldCandidate = false;
                }
                else if (Time.time - tutorialGlideHoldStartedAt >= TutorialGlideHoldTime)
                {
                    changed |= SetTutorialEventFlag(TutorialEvent.Glide);
                    tutorialGlideHoldCandidate = false;
                }
            }

            tutorialPlayerWasGrounded = grounded;
            return changed;
        }

        private void EvaluateTutorialProgress()
        {
            if (tutorialStep == TutorialStep.Welcome || TutorialComplete)
            {
                return;
            }

            var nextStep = GetFirstIncompleteTutorialStep();
            if (nextStep == tutorialStep)
            {
                return;
            }

            var previousStep = tutorialStep;
            tutorialStep = nextStep;
            ApplyTutorialTransition(previousStep);
        }

        private void AdvanceTutorial()
        {
            var previousStep = tutorialStep;
            tutorialStep = GetFirstIncompleteTutorialStep();
            ApplyTutorialTransition(previousStep);
        }

        private TutorialStep GetFirstIncompleteTutorialStep()
        {
            if (!tutorialMoved || !tutorialLooked)
            {
                return TutorialStep.MoveAndLook;
            }

            if (!tutorialSprinted || !tutorialJumped || !tutorialGlided)
            {
                return TutorialStep.TraversalBasics;
            }

            if (!tutorialInteracted)
            {
                return TutorialStep.FieldInteraction;
            }

            if (!tutorialFormedPact)
            {
                return TutorialStep.FirstPact;
            }

            if (!tutorialReviewedParty)
            {
                return TutorialStep.PartyBasics;
            }

            if (!tutorialAttacked || !tutorialGuarded || !tutorialDodged)
            {
                return TutorialStep.CombatBasics;
            }

            if (!tutorialOpenedFieldKit)
            {
                return TutorialStep.FieldKit;
            }

            return TutorialStep.Complete;
        }

        private void ApplyTutorialTransition(TutorialStep previousStep)
        {
            if (tutorialStep == previousStep)
            {
                return;
            }

            if (tutorialStep == TutorialStep.Complete)
            {
                tutorialVisible = false;
                Notify("Guidance complete. Follow the objective panel; H shows the full control list.");
            }
            else
            {
                Notify($"Field guidance: {TutorialHeading()}");
            }

            RequestSave();
        }

        internal bool IsTutorialPanelActive =>
            tutorialVisible &&
            !TutorialComplete &&
            !CodexOpen &&
            activePanel == GamePanel.None &&
            !IsPlayerSettlementInputCaptured;

        private void DrawTutorial(float width, float height)
        {
            if (!IsTutorialPanelActive)
            {
                return;
            }

            EnsureTutorialStyles();
            var textScale = TetherfallAccessibilityRuntime.SubtitleTutorialTextScale;
            var panelWidth = Mathf.Min(width - 80f, 540f + Mathf.Max(0f, textScale - 1f) * 120f);
            var headingHeight = 22f * textScale;
            var footerHeight = 18f * textScale;
            var panelHeight = 28f + headingHeight + 48f * textScale + footerHeight;
            var rect = new Rect(
                width * 0.5f - panelWidth * 0.5f,
                height - 134f - panelHeight,
                panelWidth,
                panelHeight);
            GUI.DrawTexture(rect, slateTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), copperTexture);
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 10f, rect.width - 36f, headingHeight),
                TutorialHeading().ToUpperInvariant(),
                tutorialHeadingStyle);
            var bodyY = rect.y + 14f + headingHeight;
            var footerY = rect.yMax - footerHeight - 8f;
            GUI.Label(
                new Rect(
                    rect.x + 18f,
                    bodyY,
                    rect.width - 36f,
                    Mathf.Max(24f, footerY - bodyY - 4f)),
                TutorialInstruction(),
                tutorialBodyStyle);
            GUI.Label(
                new Rect(rect.x + 18f, footerY, rect.width - 36f, footerHeight),
                $"{TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.SkipGuidance)} Skip   " +
                $"{TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.ToggleGuidance)} Hide",
                tutorialMutedStyle);
        }

        private void EnsureTutorialStyles()
        {
            var scale = TetherfallAccessibilityRuntime.SubtitleTutorialTextScale;
            if (tutorialHeadingStyle != null && Mathf.Abs(cachedTutorialTextScale - scale) < 0.001f)
            {
                return;
            }

            cachedTutorialTextScale = scale;
            tutorialHeadingStyle = new GUIStyle(headingStyle)
            {
                fontSize = TetherfallAccessibilityRuntime.ScaleSubtitleTutorialFontSize(headingStyle.fontSize)
            };
            tutorialBodyStyle = new GUIStyle(bodyStyle)
            {
                fontSize = TetherfallAccessibilityRuntime.ScaleSubtitleTutorialFontSize(bodyStyle.fontSize)
            };
            tutorialMutedStyle = new GUIStyle(mutedStyle)
            {
                fontSize = TetherfallAccessibilityRuntime.ScaleSubtitleTutorialFontSize(mutedStyle.fontSize)
            };
        }

        private string TutorialHeading()
        {
            return tutorialStep switch
            {
                TutorialStep.Welcome => "Wayfinder field guidance",
                TutorialStep.MoveAndLook => !tutorialMoved ? "Find your footing" : "Read the ground",
                TutorialStep.TraversalBasics => !tutorialSprinted
                    ? "Move with purpose"
                    : !tutorialJumped
                        ? "Leave the ground"
                        : "Open the glider",
                TutorialStep.FieldInteraction => "Follow the Threadglass",
                TutorialStep.FirstPact => "An offer, not a capture",
                TutorialStep.PartyBasics => "Fight as an expedition",
                TutorialStep.CombatBasics => !tutorialAttacked
                    ? "Start the exchange"
                    : !tutorialGuarded
                        ? "Hold your ground"
                        : "Leave the danger",
                TutorialStep.FieldKit => "Prepare your build",
                _ => "Guidance complete"
            };
        }

        private string TutorialInstruction()
        {
            return tutorialStep switch
            {
                TutorialStep.Welcome =>
                    $"Press {TutorialBinding(TetherfallInputAction.BeginTutorial)} to begin. Guidance follows your actions and never blocks progress.",
                TutorialStep.MoveAndLook => !tutorialMoved
                    ? $"Move with {TutorialBinding(TetherfallInputAction.MoveVertical)} and {TutorialBinding(TetherfallInputAction.MoveHorizontal)}."
                    : $"Look around with {TutorialBinding(TetherfallInputAction.LookHorizontal)} and {TutorialBinding(TetherfallInputAction.LookVertical)}.",
                TutorialStep.TraversalBasics => TutorialTraversalInstruction(),
                TutorialStep.FieldInteraction => TutorialInteractionInstruction(),
                TutorialStep.FirstPact => TutorialPactInstruction(),
                TutorialStep.PartyBasics =>
                    $"Allies fight automatically. {TutorialBinding(TetherfallInputAction.TacticalCommand)} calls a ready tactic; " +
                    $"{TutorialBinding(TetherfallInputAction.BeginTutorial)} continues. Party rotation is remappable in Settings.",
                TutorialStep.CombatBasics => !tutorialAttacked
                    ? $"Press {TutorialBinding(TetherfallInputAction.Attack)} to attack."
                    : !tutorialGuarded
                        ? $"Use {TutorialBinding(TetherfallInputAction.Guard)} to guard. Start just before impact for a perfect guard."
                        : $"Press {TutorialBinding(TetherfallInputAction.DodgeDescendDrop)} while moving to dodge through danger.",
                TutorialStep.FieldKit =>
                    $"Press {TutorialBinding(TetherfallInputAction.OpenEquipmentAndStores)} to inspect weapons, equipment, and carried materials. " +
                    $"{TutorialBinding(TetherfallInputAction.OpenSettlementPlanning)} opens settlement planning when field panels are closed.",
                _ => string.Empty
            };
        }

        private string TutorialInteractionInstruction()
        {
            var prompt = GetInteractionPrompt();
            return string.IsNullOrEmpty(prompt)
                ? $"Follow the objective marker. Press {TutorialBinding(TetherfallInputAction.Interact)} when a world prompt appears."
                : $"Press {TutorialBinding(TetherfallInputAction.Interact)}: {prompt}.";
        }

        private string TutorialTraversalInstruction()
        {
            if (!tutorialSprinted)
            {
                var verb =
                    TetherfallAccessibilityRuntime.GetInputPreference(TetherfallAccessibilityInputAction.Sprint) ==
                    TetherfallAccessibilityInputPreference.Toggle
                        ? "Press"
                        : "Hold";
                return $"{verb} {TutorialBinding(TetherfallInputAction.Sprint)} while moving to sprint.";
            }

            if (!tutorialJumped)
            {
                return
                    $"Press {TutorialBinding(TetherfallInputAction.JumpGlideClimb)} once to jump. " +
                    $"Hold it near marked roots to climb.";
            }

            if (tutorialGlideHoldCandidate)
            {
                return "Keep holding the traversal input to open the glider.";
            }

            var toggleGlide =
                TetherfallAccessibilityRuntime.GetInputPreference(TetherfallAccessibilityInputAction.Glide) ==
                TetherfallAccessibilityInputPreference.Toggle;
            return tutorialSpaceReleasedAfterJump
                ? $"While airborne, press {TutorialBinding(TetherfallInputAction.JumpGlideClimb)} again" +
                  (toggleGlide ? " to toggle the glider." : " and hold to glide.")
                : $"Jump, release {TutorialBinding(TetherfallInputAction.JumpGlideClimb)}, then press it again while airborne" +
                  (toggleGlide ? "." : " and hold.");
        }

        private string TutorialPactInstruction()
        {
            var prompt = GetInteractionPrompt();
            return !string.IsNullOrEmpty(prompt) && prompt.Contains("Bramblet")
                ? $"Press {TutorialBinding(TetherfallInputAction.Interact)}: {prompt}."
                : "Follow the objective marker to Bramblet by the Seedspindle.";
        }

        private static string TutorialBinding(TetherfallInputAction action)
        {
            return TetherfallInputBindings.Shared.GetBindingSummary(action);
        }

        private void WriteTutorialSave(TetherfallSaveData data)
        {
            data.tutorialStep = (int)tutorialStep;
            data.tutorialEventFlags = GetTutorialEventFlags();
            data.tutorialComplete = TutorialComplete;
            data.tutorialSkipped = tutorialSkipped;
        }

        private void RestoreTutorialState(TetherfallSaveData data)
        {
            ResetTutorialEventFlags();
            RestoreTutorialEventFlags(data.tutorialEventFlags);
            tutorialSkipped = data.tutorialSkipped;
            var legacyProgress = data.tutorialStep == 0 &&
                                 !data.tutorialComplete &&
                                 data.tutorialEventFlags == 0 &&
                                 (data.gatheredNodes > 0 ||
                                  data.level > 1 ||
                                  (data.pactNames != null && data.pactNames.Count > 0));
            var savedCompleteStep = data.tutorialStep == (int)TutorialStep.Complete;
            if (data.tutorialComplete || data.tutorialSkipped || savedCompleteStep || legacyProgress)
            {
                tutorialStep = TutorialStep.Complete;
                tutorialVisible = false;
                tutorialStateDirtyAfterRestore =
                    legacyProgress ||
                    !data.tutorialComplete ||
                    !savedCompleteStep;
                return;
            }

            var hasValidStep = IsValidTutorialStep(data.tutorialStep);
            tutorialStep = hasValidStep
                ? (TutorialStep)data.tutorialStep
                : TutorialStep.Welcome;

            if ((data.tutorialEventFlags & AddedTutorialFlags) == 0)
            {
                ReconcileLegacyTutorialStep(tutorialStep);
            }

            ReconcileTutorialFlagsWithExpedition(data);
            tutorialVisible = true;
            tutorialStateDirtyAfterRestore =
                !hasValidStep ||
                GetTutorialEventFlags() != data.tutorialEventFlags;
        }

        private static bool IsValidTutorialStep(int value)
        {
            return value == (int)TutorialStep.Welcome ||
                   value == (int)TutorialStep.MoveAndLook ||
                   value == (int)TutorialStep.TraversalBasics ||
                   value == (int)TutorialStep.FieldInteraction ||
                   value == (int)TutorialStep.FirstPact ||
                   value == (int)TutorialStep.PartyBasics ||
                   value == (int)TutorialStep.CombatBasics ||
                   value == (int)TutorialStep.FieldKit ||
                   value == (int)TutorialStep.Complete;
        }

        private void ReconcileLegacyTutorialStep(TutorialStep restoredStep)
        {
            if (restoredStep == TutorialStep.Welcome ||
                restoredStep == TutorialStep.MoveAndLook ||
                restoredStep == TutorialStep.TraversalBasics)
            {
                return;
            }

            tutorialMoved = true;
            tutorialLooked = true;
            tutorialSprinted = true;
            tutorialJumped = true;
            tutorialGlided = true;

            if (restoredStep == TutorialStep.FieldInteraction ||
                restoredStep == TutorialStep.FieldKit ||
                restoredStep == TutorialStep.FirstPact)
            {
                tutorialAttacked = true;
                tutorialGuarded = true;
                tutorialDodged = true;
            }

            if (restoredStep == TutorialStep.FieldKit || restoredStep == TutorialStep.FirstPact)
            {
                tutorialInteracted = true;
            }

            if (restoredStep == TutorialStep.FirstPact)
            {
                tutorialOpenedFieldKit = true;
            }
        }

        private void ReconcileTutorialFlagsWithExpedition(TetherfallSaveData data)
        {
            if (data.gatheredNodes > 0 ||
                (data.recruitNames != null && data.recruitNames.Count > 0))
            {
                tutorialInteracted = true;
            }

            if ((data.pactNames != null && data.pactNames.Count > 0) ||
                !string.IsNullOrEmpty(data.activePactName) ||
                !string.IsNullOrEmpty(data.mountPactName))
            {
                tutorialInteracted = true;
                tutorialFormedPact = true;
            }
        }

        private int GetTutorialEventFlags()
        {
            var flags = 0;
            if (tutorialMoved) flags |= MovedFlag;
            if (tutorialLooked) flags |= LookedFlag;
            if (tutorialAttacked) flags |= AttackedFlag;
            if (tutorialGuarded) flags |= GuardedFlag;
            if (tutorialDodged) flags |= DodgedFlag;
            if (tutorialInteracted) flags |= InteractedFlag;
            if (tutorialOpenedFieldKit) flags |= OpenedFieldKitFlag;
            if (tutorialFormedPact) flags |= FormedPactFlag;
            if (tutorialSprinted) flags |= SprintedFlag;
            if (tutorialJumped) flags |= JumpedFlag;
            if (tutorialReviewedParty) flags |= ReviewedPartyFlag;
            if (tutorialGlided) flags |= GlidedFlag;
            return flags;
        }

        private void RestoreTutorialEventFlags(int flags)
        {
            tutorialMoved = (flags & MovedFlag) != 0;
            tutorialLooked = (flags & LookedFlag) != 0;
            tutorialAttacked = (flags & AttackedFlag) != 0;
            tutorialGuarded = (flags & GuardedFlag) != 0;
            tutorialDodged = (flags & DodgedFlag) != 0;
            tutorialInteracted = (flags & InteractedFlag) != 0;
            tutorialOpenedFieldKit = (flags & OpenedFieldKitFlag) != 0;
            tutorialFormedPact = (flags & FormedPactFlag) != 0;
            tutorialSprinted = (flags & SprintedFlag) != 0;
            tutorialJumped = (flags & JumpedFlag) != 0;
            tutorialReviewedParty = (flags & ReviewedPartyFlag) != 0;
            tutorialGlided = (flags & GlidedFlag) != 0;
        }

        private void ResetTutorialEventFlags()
        {
            tutorialMoved = false;
            tutorialLooked = false;
            tutorialAttacked = false;
            tutorialGuarded = false;
            tutorialDodged = false;
            tutorialInteracted = false;
            tutorialOpenedFieldKit = false;
            tutorialFormedPact = false;
            tutorialSprinted = false;
            tutorialJumped = false;
            tutorialGlided = false;
            tutorialReviewedParty = false;
            tutorialSpaceReleasedAfterJump = false;
            tutorialGlideHoldCandidate = false;
            tutorialGlideHoldStartedAt = 0f;
            tutorialPlayerController = Player != null ? Player.GetComponent<CharacterController>() : null;
            tutorialPlayerWasGrounded =
                tutorialPlayerController != null && tutorialPlayerController.isGrounded;
        }
    }
}
