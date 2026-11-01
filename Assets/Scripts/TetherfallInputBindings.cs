using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Logical controls used by the playable build. Movement and look are split
    /// into one-dimensional actions so keyboard keys and legacy joystick axes
    /// can be remapped through the same data model.
    /// </summary>
    public enum TetherfallInputAction
    {
        MoveHorizontal,
        MoveVertical,
        LookHorizontal,
        LookVertical,
        Sprint,
        JumpGlideClimb,
        DodgeDescendDrop,
        Guard,
        Attack,
        ResonanceAbility,
        CombatFocus,
        TacticalCommand,
        CycleExpeditionOrder,
        UseProvision,
        EquipPathblade,
        EquipRiftbow,
        EquipThreadstaff,
        Interact,
        OpenEquipmentAndStores,
        OpenQuestLedger,
        OpenCraftingOrCampServices,
        RotateCompanionPairOrRallyBeat2,
        RotateSupportPactOrRallyBeat1,
        BuildOrRestAtCamp,
        MountOrDismount,
        OpenFieldLog,
        ToggleGuidance,
        SkipGuidance,
        ToggleControls,
        CancelOrReleaseCursor,
        BeginTutorial,
        OpenSkillTree,
        OpenWorldMap,
        OpenSettings,
        OpenSettlementPlanning
    }

    public enum TetherfallInputBindingSource
    {
        KeyOrJoystickButton,
        MouseButton,
        LegacyAxis
    }

    /// <summary>
    /// Selects which half of a legacy axis contributes to an action. Positive
    /// and Negative both produce a positive value before scale is applied.
    /// </summary>
    public enum TetherfallInputAxisMode
    {
        Full,
        Positive,
        Negative
    }

    public enum TetherfallInputConflictResolution
    {
        Reject,
        ReplaceExisting,
        Allow
    }

    /// <summary>
    /// JsonUtility-safe physical binding data. KeyCode also represents legacy
    /// joystick buttons (for example, KeyCode.JoystickButton0).
    /// </summary>
    [Serializable]
    public sealed class TetherfallInputBinding
    {
        public TetherfallInputBindingSource source = TetherfallInputBindingSource.KeyOrJoystickButton;
        public KeyCode keyCode = KeyCode.None;
        public int mouseButton = -1;
        public string axisName = string.Empty;
        public TetherfallInputAxisMode axisMode = TetherfallInputAxisMode.Full;
        public float scale = 1f;
        public float deadZone = 0.001f;
        public float pressPoint = 0.5f;
        public bool useRawAxis = true;

        public static TetherfallInputBinding Key(KeyCode key, float valueScale = 1f)
        {
            return new TetherfallInputBinding
            {
                source = TetherfallInputBindingSource.KeyOrJoystickButton,
                keyCode = key,
                mouseButton = -1,
                axisName = string.Empty,
                axisMode = TetherfallInputAxisMode.Full,
                scale = valueScale,
                deadZone = 0.001f,
                pressPoint = 0.5f,
                useRawAxis = true
            };
        }

        public static TetherfallInputBinding Mouse(int button, float valueScale = 1f)
        {
            return new TetherfallInputBinding
            {
                source = TetherfallInputBindingSource.MouseButton,
                keyCode = KeyCode.None,
                mouseButton = button,
                axisName = string.Empty,
                axisMode = TetherfallInputAxisMode.Full,
                scale = valueScale,
                deadZone = 0.001f,
                pressPoint = 0.5f,
                useRawAxis = true
            };
        }

        /// <remarks>
        /// The named axis must exist in ProjectSettings/InputManager.asset.
        /// This keeps controller support compatible with UnityEngine.Input
        /// without taking a dependency on the newer Input System package.
        /// </remarks>
        public static TetherfallInputBinding LegacyAxis(
            string name,
            TetherfallInputAxisMode mode = TetherfallInputAxisMode.Full,
            float valueScale = 1f,
            float valueDeadZone = 0.15f,
            float valuePressPoint = 0.5f,
            bool raw = true)
        {
            return new TetherfallInputBinding
            {
                source = TetherfallInputBindingSource.LegacyAxis,
                keyCode = KeyCode.None,
                mouseButton = -1,
                axisName = name ?? string.Empty,
                axisMode = mode,
                scale = valueScale,
                deadZone = valueDeadZone,
                pressPoint = valuePressPoint,
                useRawAxis = raw
            };
        }

        public TetherfallInputBinding Clone()
        {
            return new TetherfallInputBinding
            {
                source = source,
                keyCode = keyCode,
                mouseButton = mouseButton,
                axisName = axisName,
                axisMode = axisMode,
                scale = scale,
                deadZone = deadZone,
                pressPoint = pressPoint,
                useRawAxis = useRawAxis
            };
        }
    }

    [Serializable]
    public sealed class TetherfallActionBindingData
    {
        public TetherfallInputAction action;
        public List<TetherfallInputBinding> bindings = new List<TetherfallInputBinding>();

        public TetherfallActionBindingData()
        {
        }

        public TetherfallActionBindingData(
            TetherfallInputAction inputAction,
            params TetherfallInputBinding[] inputBindings)
        {
            action = inputAction;
            bindings = new List<TetherfallInputBinding>();
            if (inputBindings == null)
            {
                return;
            }

            for (var index = 0; index < inputBindings.Length; index++)
            {
                if (inputBindings[index] != null)
                {
                    bindings.Add(inputBindings[index]);
                }
            }
        }
    }

    /// <summary>
    /// Top-level JsonUtility payload. An action present with an empty bindings
    /// list is intentionally unbound; actions absent from loaded data receive
    /// their defaults for forward compatibility.
    /// </summary>
    [Serializable]
    public sealed class TetherfallInputBindingData
    {
        public int version = TetherfallInputBindings.CurrentSchemaVersion;
        public List<TetherfallActionBindingData> actions = new List<TetherfallActionBindingData>();
    }

    [Serializable]
    public sealed class TetherfallInputConflict
    {
        public TetherfallInputAction firstAction;
        public int firstBindingIndex;
        public TetherfallInputAction secondAction;
        public int secondBindingIndex;
        public string controlLabel;

        public TetherfallInputConflict()
        {
        }

        internal TetherfallInputConflict(
            TetherfallInputAction first,
            int firstIndex,
            TetherfallInputAction second,
            int secondIndex,
            string label)
        {
            firstAction = first;
            firstBindingIndex = firstIndex;
            secondAction = second;
            secondBindingIndex = secondIndex;
            controlLabel = label;
        }
    }

    /// <summary>
    /// Legacy-Input-compatible action map. The class is intentionally not a
    /// MonoBehaviour: callers may use Shared or own an independent instance,
    /// serialize its data with JsonUtility, and query it from Update/LateUpdate.
    /// </summary>
    public sealed class TetherfallInputBindings
    {
        public const int CurrentSchemaVersion = 2;
        public const int MaximumLegacyMouseButton = 6;

        private enum ActionValueKind
        {
            Button,
            NormalizedAxis,
            DeltaAxis
        }

        private sealed class ActionDefinition
        {
            public readonly TetherfallInputAction action;
            public readonly string displayLabel;
            public readonly string description;
            public readonly ActionValueKind valueKind;

            public ActionDefinition(
                TetherfallInputAction inputAction,
                string label,
                string actionDescription,
                ActionValueKind kind)
            {
                action = inputAction;
                displayLabel = label;
                description = actionDescription;
                valueKind = kind;
            }
        }

        private static readonly ActionDefinition[] Definitions =
        {
            Define(TetherfallInputAction.MoveHorizontal, "Move Left / Right", "Move left or right.", ActionValueKind.NormalizedAxis),
            Define(TetherfallInputAction.MoveVertical, "Move Forward / Back", "Move forward or backward.", ActionValueKind.NormalizedAxis),
            Define(TetherfallInputAction.LookHorizontal, "Look Left / Right", "Turn the camera left or right.", ActionValueKind.DeltaAxis),
            Define(TetherfallInputAction.LookVertical, "Look Up / Down", "Turn the camera up or down.", ActionValueKind.DeltaAxis),
            Define(TetherfallInputAction.Sprint, "Sprint", "Sprint while held.", ActionValueKind.Button),
            Define(TetherfallInputAction.JumpGlideClimb, "Jump / Glide / Climb", "Jump, hold to glide, swim up, or catch a marked rootface.", ActionValueKind.Button),
            Define(TetherfallInputAction.DodgeDescendDrop, "Dodge / Descend / Drop", "Dodge, swim down, or drop from a climb.", ActionValueKind.Button),
            Define(TetherfallInputAction.Guard, "Guard", "Guard while held; timing the press can perfect guard.", ActionValueKind.Button),
            Define(TetherfallInputAction.Attack, "Attack", "Perform the active weapon's basic attack.", ActionValueKind.Button),
            Define(TetherfallInputAction.ResonanceAbility, "Resonance Ability", "Use the active weapon's resonance ability.", ActionValueKind.Button),
            Define(TetherfallInputAction.CombatFocus, "Combat Focus", "Bind or release Threadglass focus on a hostile.", ActionValueKind.Button),
            Define(TetherfallInputAction.TacticalCommand, "Tactical Command / Rally", "Command allies or launch a Threadlink Rally.", ActionValueKind.Button),
            Define(TetherfallInputAction.CycleExpeditionOrder, "Cycle Expedition Order", "Cycle Trail, Focus, and Bulwark orders.", ActionValueKind.Button),
            Define(TetherfallInputAction.UseProvision, "Use Provision", "Consume the selected prepared provision.", ActionValueKind.Button),
            Define(TetherfallInputAction.EquipPathblade, "Equip Pathblade", "Switch to the crafted Pathblade build.", ActionValueKind.Button),
            Define(TetherfallInputAction.EquipRiftbow, "Equip Riftbow", "Switch to the crafted Riftbow build.", ActionValueKind.Button),
            Define(TetherfallInputAction.EquipThreadstaff, "Equip Threadstaff", "Switch to the crafted Threadstaff build.", ActionValueKind.Button),
            Define(TetherfallInputAction.Interact, "Interact", "Interact, gather, Pact, recruit, rest, or harvest.", ActionValueKind.Button),
            Define(TetherfallInputAction.OpenEquipmentAndStores, "Equipment / Field Stores", "Open equipment and field stores.", ActionValueKind.Button),
            Define(TetherfallInputAction.OpenQuestLedger, "Quest Ledger", "Open the quest ledger.", ActionValueKind.Button),
            Define(TetherfallInputAction.OpenCraftingOrCampServices, "Crafting / Camp Services", "Open crafting or nearby camp services.", ActionValueKind.Button),
            Define(TetherfallInputAction.RotateCompanionPairOrRallyBeat2, "Companion Pair / Rally Beat 2", "Rotate the human companion pair or Rally Beat 2.", ActionValueKind.Button),
            Define(TetherfallInputAction.RotateSupportPactOrRallyBeat1, "Support Pact / Rally Beat 1", "Rotate the support Pact or Rally Beat 1.", ActionValueKind.Button),
            Define(TetherfallInputAction.BuildOrRestAtCamp, "Build / Rest at Camp", "Build or rest at the portable camp.", ActionValueKind.Button),
            Define(
                TetherfallInputAction.MountOrDismount,
                "Ride / Pact lift",
                "Mount or dismount with Slateback, or request Suncrest's one-per-airtime lift while gliding.",
                ActionValueKind.Button),
            Define(TetherfallInputAction.OpenFieldLog, "Field Log", "Open or close the field log.", ActionValueKind.Button),
            Define(TetherfallInputAction.ToggleGuidance, "Toggle Guidance", "Hide or show first-expedition guidance.", ActionValueKind.Button),
            Define(TetherfallInputAction.SkipGuidance, "Skip Guidance", "Skip first-expedition guidance.", ActionValueKind.Button),
            Define(TetherfallInputAction.ToggleControls, "Toggle Controls", "Hide or show the controls reference.", ActionValueKind.Button),
            Define(TetherfallInputAction.CancelOrReleaseCursor, "Cancel / Release Cursor", "Close a panel or release the mouse cursor.", ActionValueKind.Button),
            Define(TetherfallInputAction.BeginTutorial, "Begin Tutorial", "Begin first-expedition field guidance.", ActionValueKind.Button),
            Define(TetherfallInputAction.OpenSkillTree, "Wayfinder Disciplines", "Open the skill tree.", ActionValueKind.Button),
            Define(TetherfallInputAction.OpenWorldMap, "World Map", "Open the Threadway world map.", ActionValueKind.Button),
            Define(TetherfallInputAction.OpenSettings, "Settings", "Open controls and accessibility settings.", ActionValueKind.Button),
            Define(
                TetherfallInputAction.OpenSettlementPlanning,
                "Settlement Planning",
                "Open or close settlement planning; an existing Threadheart must be nearby.",
                ActionValueKind.Button)
        };

        private static TetherfallInputBindings shared;

        private TetherfallInputBindingData data;
        private readonly float[] sampledValues = new float[Definitions.Length];
        private readonly bool[] sampledHeld = new bool[Definitions.Length];
        private readonly bool[] sampledPressed = new bool[Definitions.Length];
        private readonly bool[] sampledReleased = new bool[Definitions.Length];
        private int sampledFrame = int.MinValue;

        public static TetherfallInputBindings Shared
        {
            get
            {
                if (shared == null)
                {
                    shared = new TetherfallInputBindings();
                }

                return shared;
            }
        }

        public TetherfallInputBindings()
        {
            ResetToDefaults();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetShared()
        {
            shared = null;
        }

        public static TetherfallInputAction[] GetAllActions()
        {
            var actions = new TetherfallInputAction[Definitions.Length];
            for (var index = 0; index < Definitions.Length; index++)
            {
                actions[index] = Definitions[index].action;
            }

            return actions;
        }

        public float GetAxis(TetherfallInputAction action)
        {
            EnsureSampled();
            var index = GetActionIndex(action);
            return index >= 0 ? sampledValues[index] : 0f;
        }

        public Vector2 GetMove()
        {
            EnsureSampled();
            return new Vector2(
                GetAxis(TetherfallInputAction.MoveHorizontal),
                GetAxis(TetherfallInputAction.MoveVertical));
        }

        public Vector2 GetLook()
        {
            EnsureSampled();
            return new Vector2(
                GetAxis(TetherfallInputAction.LookHorizontal),
                GetAxis(TetherfallInputAction.LookVertical));
        }

        public bool IsHeld(TetherfallInputAction action)
        {
            EnsureSampled();
            var index = GetActionIndex(action);
            return index >= 0 && sampledHeld[index];
        }

        public bool WasPressedThisFrame(TetherfallInputAction action)
        {
            EnsureSampled();
            var index = GetActionIndex(action);
            return index >= 0 && sampledPressed[index];
        }

        public bool WasReleasedThisFrame(TetherfallInputAction action)
        {
            EnsureSampled();
            var index = GetActionIndex(action);
            return index >= 0 && sampledReleased[index];
        }

        // Legacy Input-style aliases make existing Input.Get* call sites easy to replace.
        public bool GetButton(TetherfallInputAction action)
        {
            return IsHeld(action);
        }

        public bool GetButtonDown(TetherfallInputAction action)
        {
            return WasPressedThisFrame(action);
        }

        public bool GetButtonUp(TetherfallInputAction action)
        {
            return WasReleasedThisFrame(action);
        }

        /// <summary>
        /// Forces all action values to be sampled now if they have not already
        /// been sampled this frame. Calling this once near the start of Update
        /// is optional; every query method also samples lazily.
        /// </summary>
        public void SampleCurrentFrame()
        {
            EnsureSampled();
        }

        public List<TetherfallInputBinding> GetBindings(TetherfallInputAction action)
        {
            var result = new List<TetherfallInputBinding>();
            var actionData = FindActionData(data, action);
            if (actionData == null || actionData.bindings == null)
            {
                return result;
            }

            for (var index = 0; index < actionData.bindings.Count; index++)
            {
                var binding = actionData.bindings[index];
                if (binding != null)
                {
                    result.Add(binding.Clone());
                }
            }

            return result;
        }

        public string GetBindingSummary(TetherfallInputAction action, string separator = " / ")
        {
            var actionData = FindActionData(data, action);
            if (actionData == null || actionData.bindings == null || actionData.bindings.Count == 0)
            {
                return "Unbound";
            }

            var builder = new StringBuilder();
            for (var index = 0; index < actionData.bindings.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(separator ?? " / ");
                }

                builder.Append(GetBindingDisplayLabel(actionData.bindings[index]));
            }

            return builder.ToString();
        }

        public static string GetActionDisplayLabel(TetherfallInputAction action)
        {
            var definition = FindDefinition(action);
            return definition != null ? definition.displayLabel : Humanize(action.ToString());
        }

        public static string GetActionDescription(TetherfallInputAction action)
        {
            var definition = FindDefinition(action);
            return definition != null ? definition.description : string.Empty;
        }

        public static string GetBindingDisplayLabel(TetherfallInputBinding binding)
        {
            if (binding == null)
            {
                return "Unbound";
            }

            int mouseIndex;
            if (TryGetMouseButtonIndex(binding, out mouseIndex))
            {
                return GetMouseButtonLabel(mouseIndex);
            }

            if (binding.source == TetherfallInputBindingSource.KeyOrJoystickButton)
            {
                return GetKeyDisplayLabel(binding.keyCode);
            }

            if (binding.source == TetherfallInputBindingSource.LegacyAxis)
            {
                var label = string.IsNullOrWhiteSpace(binding.axisName)
                    ? "Unnamed Axis"
                    : binding.axisName.Trim();
                if (binding.axisMode == TetherfallInputAxisMode.Positive)
                {
                    label += " (+)";
                }
                else if (binding.axisMode == TetherfallInputAxisMode.Negative)
                {
                    label += " (-)";
                }

                if (binding.scale < 0f)
                {
                    label += " (inverted)";
                }

                return label;
            }

            return "Unknown Binding";
        }

        public void ResetToDefaults()
        {
            data = CreateDefaultData();
            InvalidateSamples();
        }

        public bool ResetActionToDefault(TetherfallInputAction action)
        {
            var defaultAction = FindActionData(CreateDefaultData(), action);
            var currentAction = FindActionData(data, action);
            if (defaultAction == null || currentAction == null)
            {
                return false;
            }

            currentAction.bindings = CloneBindings(defaultAction.bindings);
            InvalidateSamples();
            return true;
        }

        public TetherfallInputBindingData ExportData()
        {
            return CloneData(data);
        }

        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(ExportData(), prettyPrint);
        }

        public bool TryLoadJson(string json, out string error)
        {
            return TryLoadJson(json, false, out error);
        }

        public bool TryLoadJson(string json, bool rejectConflicts, out string error)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Binding JSON is empty.";
                return false;
            }

            TetherfallInputBindingData loaded;
            try
            {
                loaded = JsonUtility.FromJson<TetherfallInputBindingData>(json);
            }
            catch (Exception exception)
            {
                error = "Binding JSON could not be read: " + exception.Message;
                return false;
            }

            return TryApplyData(loaded, rejectConflicts, out error);
        }

        public bool TryApplyData(
            TetherfallInputBindingData sourceData,
            bool rejectConflicts,
            out string error)
        {
            TetherfallInputBindingData prepared;
            if (!TryPrepareData(sourceData, out prepared, out error))
            {
                return false;
            }

            if (rejectConflicts)
            {
                var conflicts = FindAllConflicts(prepared);
                if (conflicts.Count > 0)
                {
                    var conflict = conflicts[0];
                    error =
                        conflict.controlLabel + " is assigned to both " +
                        GetActionDisplayLabel(conflict.firstAction) + " and " +
                        GetActionDisplayLabel(conflict.secondAction) + ".";
                    return false;
                }
            }

            data = prepared;
            InvalidateSamples();
            error = string.Empty;
            return true;
        }

        public bool TrySetBinding(
            TetherfallInputAction action,
            int bindingIndex,
            TetherfallInputBinding binding,
            TetherfallInputConflictResolution resolution,
            out List<TetherfallInputConflict> conflicts,
            out string error)
        {
            conflicts = new List<TetherfallInputConflict>();
            if (GetActionIndex(action) < 0)
            {
                error = "Unknown input action: " + action + ".";
                return false;
            }

            if (!Enum.IsDefined(typeof(TetherfallInputConflictResolution), resolution))
            {
                error = "Unknown conflict resolution: " + resolution + ".";
                return false;
            }

            if (!TryValidateBinding(binding, out error))
            {
                return false;
            }

            var actionData = FindActionData(data, action);
            if (actionData == null)
            {
                error = "Binding data does not contain " + GetActionDisplayLabel(action) + ".";
                return false;
            }

            if (actionData.bindings == null)
            {
                actionData.bindings = new List<TetherfallInputBinding>();
            }

            if (bindingIndex < 0 || bindingIndex > actionData.bindings.Count)
            {
                error = "Binding index is outside the action's binding list.";
                return false;
            }

            conflicts = FindConflicts(action, bindingIndex, binding);
            if (resolution == TetherfallInputConflictResolution.Reject && conflicts.Count > 0)
            {
                var conflict = conflicts[0];
                error =
                    conflict.controlLabel + " is already assigned to " +
                    GetActionDisplayLabel(conflict.secondAction) + ".";
                return false;
            }

            var replacement = binding.Clone();
            if (replacement.source == TetherfallInputBindingSource.LegacyAxis)
            {
                replacement.axisName = replacement.axisName.Trim();
            }

            if (bindingIndex == actionData.bindings.Count)
            {
                actionData.bindings.Add(replacement);
            }
            else
            {
                actionData.bindings[bindingIndex] = replacement;
            }

            if (resolution == TetherfallInputConflictResolution.ReplaceExisting)
            {
                for (var actionIndex = 0; actionIndex < data.actions.Count; actionIndex++)
                {
                    var existingAction = data.actions[actionIndex];
                    if (existingAction == null || existingAction.bindings == null)
                    {
                        continue;
                    }

                    for (var index = existingAction.bindings.Count - 1; index >= 0; index--)
                    {
                        var existing = existingAction.bindings[index];
                        if (!ReferenceEquals(existing, replacement) &&
                            BindingsConflict(replacement, existing))
                        {
                            existingAction.bindings.RemoveAt(index);
                        }
                    }
                }
            }

            InvalidateSamples();
            error = string.Empty;
            return true;
        }

        public bool TryAddBinding(
            TetherfallInputAction action,
            TetherfallInputBinding binding,
            TetherfallInputConflictResolution resolution,
            out List<TetherfallInputConflict> conflicts,
            out string error)
        {
            var actionData = FindActionData(data, action);
            var index = actionData != null && actionData.bindings != null
                ? actionData.bindings.Count
                : 0;
            return TrySetBinding(action, index, binding, resolution, out conflicts, out error);
        }

        public bool TryRemoveBinding(
            TetherfallInputAction action,
            int bindingIndex,
            out string error)
        {
            var actionData = FindActionData(data, action);
            if (actionData == null || actionData.bindings == null)
            {
                error = "Unknown input action: " + action + ".";
                return false;
            }

            if (bindingIndex < 0 || bindingIndex >= actionData.bindings.Count)
            {
                error = "Binding index is outside the action's binding list.";
                return false;
            }

            actionData.bindings.RemoveAt(bindingIndex);
            InvalidateSamples();
            error = string.Empty;
            return true;
        }

        public List<TetherfallInputConflict> FindConflicts(
            TetherfallInputAction action,
            TetherfallInputBinding candidate)
        {
            return FindConflicts(action, -1, candidate);
        }

        public List<TetherfallInputConflict> FindAllConflicts()
        {
            return FindAllConflicts(data);
        }

        public static bool TryValidateBinding(TetherfallInputBinding binding, out string error)
        {
            if (binding == null)
            {
                error = "Binding is null.";
                return false;
            }

            if (!Enum.IsDefined(typeof(TetherfallInputBindingSource), binding.source))
            {
                error = "Binding source is invalid.";
                return false;
            }

            if (!IsFinite(binding.scale) || Mathf.Approximately(binding.scale, 0f))
            {
                error = "Binding scale must be a finite, non-zero number.";
                return false;
            }

            if (!IsFinite(binding.deadZone) || binding.deadZone < 0f)
            {
                error = "Binding dead zone must be a finite, non-negative number.";
                return false;
            }

            if (!IsFinite(binding.pressPoint) || binding.pressPoint <= 0f)
            {
                error = "Binding press point must be a finite number greater than zero.";
                return false;
            }

            switch (binding.source)
            {
                case TetherfallInputBindingSource.KeyOrJoystickButton:
                    if (binding.keyCode == KeyCode.None)
                    {
                        error = "Key binding cannot use KeyCode.None.";
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(KeyCode), binding.keyCode))
                    {
                        error = "Key binding contains an unknown KeyCode value.";
                        return false;
                    }

                    break;

                case TetherfallInputBindingSource.MouseButton:
                    if (binding.mouseButton < 0 || binding.mouseButton > MaximumLegacyMouseButton)
                    {
                        error =
                            "Legacy mouse button must be between 0 and " +
                            MaximumLegacyMouseButton + ".";
                        return false;
                    }

                    break;

                case TetherfallInputBindingSource.LegacyAxis:
                    if (string.IsNullOrWhiteSpace(binding.axisName))
                    {
                        error = "Legacy axis binding must have an Input Manager axis name.";
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(TetherfallInputAxisMode), binding.axisMode))
                    {
                        error = "Legacy axis mode is invalid.";
                        return false;
                    }

                    break;
            }

            error = string.Empty;
            return true;
        }

        private static ActionDefinition Define(
            TetherfallInputAction action,
            string label,
            string description,
            ActionValueKind kind)
        {
            return new ActionDefinition(action, label, description, kind);
        }

        private static TetherfallInputBindingData CreateDefaultData()
        {
            var defaults = new TetherfallInputBindingData
            {
                version = CurrentSchemaVersion,
                actions = new List<TetherfallActionBindingData>()
            };

            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.MoveHorizontal,
                TetherfallInputBinding.Key(KeyCode.A, -1f),
                TetherfallInputBinding.Key(KeyCode.D)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.MoveVertical,
                TetherfallInputBinding.Key(KeyCode.W),
                TetherfallInputBinding.Key(KeyCode.S, -1f)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.LookHorizontal,
                TetherfallInputBinding.LegacyAxis("Mouse X", TetherfallInputAxisMode.Full, 1f, 0f, 0.5f, false)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.LookVertical,
                TetherfallInputBinding.LegacyAxis("Mouse Y", TetherfallInputAxisMode.Full, 1f, 0f, 0.5f, false)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.Sprint,
                TetherfallInputBinding.Key(KeyCode.LeftShift),
                TetherfallInputBinding.Key(KeyCode.RightShift)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.JumpGlideClimb,
                TetherfallInputBinding.Key(KeyCode.Space)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.DodgeDescendDrop,
                TetherfallInputBinding.Key(KeyCode.LeftControl)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.Guard,
                TetherfallInputBinding.Mouse(1)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.Attack,
                TetherfallInputBinding.Key(KeyCode.F),
                TetherfallInputBinding.Mouse(0)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.ResonanceAbility,
                TetherfallInputBinding.Key(KeyCode.G)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.CombatFocus,
                TetherfallInputBinding.Key(KeyCode.L),
                TetherfallInputBinding.Mouse(2)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.TacticalCommand,
                TetherfallInputBinding.Key(KeyCode.V)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.CycleExpeditionOrder,
                TetherfallInputBinding.Key(KeyCode.Z)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.UseProvision,
                TetherfallInputBinding.Key(KeyCode.X)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.EquipPathblade,
                TetherfallInputBinding.Key(KeyCode.Alpha1)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.EquipRiftbow,
                TetherfallInputBinding.Key(KeyCode.Alpha2)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.EquipThreadstaff,
                TetherfallInputBinding.Key(KeyCode.Alpha3)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.Interact,
                TetherfallInputBinding.Key(KeyCode.E)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenEquipmentAndStores,
                TetherfallInputBinding.Key(KeyCode.I)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenQuestLedger,
                TetherfallInputBinding.Key(KeyCode.J)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenCraftingOrCampServices,
                TetherfallInputBinding.Key(KeyCode.B)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.RotateCompanionPairOrRallyBeat2,
                TetherfallInputBinding.Key(KeyCode.Q)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.RotateSupportPactOrRallyBeat1,
                TetherfallInputBinding.Key(KeyCode.T)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.BuildOrRestAtCamp,
                TetherfallInputBinding.Key(KeyCode.C)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.MountOrDismount,
                TetherfallInputBinding.Key(KeyCode.R)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenFieldLog,
                TetherfallInputBinding.Key(KeyCode.Tab)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.ToggleGuidance,
                TetherfallInputBinding.Key(KeyCode.F1)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.SkipGuidance,
                TetherfallInputBinding.Key(KeyCode.K)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.ToggleControls,
                TetherfallInputBinding.Key(KeyCode.H)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.CancelOrReleaseCursor,
                TetherfallInputBinding.Key(KeyCode.Escape)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.BeginTutorial,
                TetherfallInputBinding.Key(KeyCode.Return)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenSkillTree,
                TetherfallInputBinding.Key(KeyCode.U)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenWorldMap,
                TetherfallInputBinding.Key(KeyCode.M)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenSettings,
                TetherfallInputBinding.Key(KeyCode.F10)));
            defaults.actions.Add(new TetherfallActionBindingData(
                TetherfallInputAction.OpenSettlementPlanning,
                TetherfallInputBinding.Key(KeyCode.N)));

            return defaults;
        }

        private void EnsureSampled()
        {
            var frame = Time.frameCount;
            if (sampledFrame == frame)
            {
                return;
            }

            var previousFrameWasSampled = sampledFrame == frame - 1;
            for (var index = 0; index < Definitions.Length; index++)
            {
                var action = Definitions[index].action;
                var wasHeld = previousFrameWasSampled && sampledHeld[index];
                var isHeld = EvaluateActionHeld(action);
                sampledValues[index] = EvaluateActionValue(Definitions[index]);
                sampledPressed[index] =
                    EvaluateDigitalDown(action) ||
                    (!wasHeld && isHeld);
                sampledReleased[index] =
                    EvaluateDigitalUp(action) ||
                    (wasHeld && !isHeld);
                sampledHeld[index] = isHeld;
            }

            sampledFrame = frame;
        }

        private float EvaluateActionValue(ActionDefinition definition)
        {
            var actionData = FindActionData(data, definition.action);
            if (actionData == null || actionData.bindings == null)
            {
                return 0f;
            }

            if (definition.valueKind == ActionValueKind.Button)
            {
                var strongest = 0f;
                for (var index = 0; index < actionData.bindings.Count; index++)
                {
                    var value = ReadBindingValue(actionData.bindings[index]);
                    if (Mathf.Abs(value) > Mathf.Abs(strongest))
                    {
                        strongest = value;
                    }
                }

                return strongest;
            }

            var total = 0f;
            for (var index = 0; index < actionData.bindings.Count; index++)
            {
                total += ReadBindingValue(actionData.bindings[index]);
            }

            return definition.valueKind == ActionValueKind.NormalizedAxis
                ? Mathf.Clamp(total, -1f, 1f)
                : total;
        }

        private bool EvaluateActionHeld(TetherfallInputAction action)
        {
            var actionData = FindActionData(data, action);
            if (actionData == null || actionData.bindings == null)
            {
                return false;
            }

            for (var index = 0; index < actionData.bindings.Count; index++)
            {
                var binding = actionData.bindings[index];
                if (binding != null &&
                    Mathf.Abs(ReadBindingValue(binding)) >= binding.pressPoint)
                {
                    return true;
                }
            }

            return false;
        }

        private bool EvaluateDigitalDown(TetherfallInputAction action)
        {
            var actionData = FindActionData(data, action);
            if (actionData == null || actionData.bindings == null)
            {
                return false;
            }

            for (var index = 0; index < actionData.bindings.Count; index++)
            {
                var binding = actionData.bindings[index];
                if (binding == null)
                {
                    continue;
                }

                if (binding.source == TetherfallInputBindingSource.KeyOrJoystickButton &&
                    Input.GetKeyDown(binding.keyCode))
                {
                    return true;
                }

                if (binding.source == TetherfallInputBindingSource.MouseButton &&
                    Input.GetMouseButtonDown(binding.mouseButton))
                {
                    return true;
                }
            }

            return false;
        }

        private bool EvaluateDigitalUp(TetherfallInputAction action)
        {
            var actionData = FindActionData(data, action);
            if (actionData == null || actionData.bindings == null)
            {
                return false;
            }

            for (var index = 0; index < actionData.bindings.Count; index++)
            {
                var binding = actionData.bindings[index];
                if (binding == null)
                {
                    continue;
                }

                if (binding.source == TetherfallInputBindingSource.KeyOrJoystickButton &&
                    Input.GetKeyUp(binding.keyCode))
                {
                    return true;
                }

                if (binding.source == TetherfallInputBindingSource.MouseButton &&
                    Input.GetMouseButtonUp(binding.mouseButton))
                {
                    return true;
                }
            }

            return false;
        }

        private static float ReadBindingValue(TetherfallInputBinding binding)
        {
            if (binding == null)
            {
                return 0f;
            }

            switch (binding.source)
            {
                case TetherfallInputBindingSource.KeyOrJoystickButton:
                    return Input.GetKey(binding.keyCode) ? binding.scale : 0f;

                case TetherfallInputBindingSource.MouseButton:
                    return Input.GetMouseButton(binding.mouseButton) ? binding.scale : 0f;

                case TetherfallInputBindingSource.LegacyAxis:
                    float value;
                    try
                    {
                        value = binding.useRawAxis
                            ? Input.GetAxisRaw(binding.axisName)
                            : Input.GetAxis(binding.axisName);
                    }
                    catch (ArgumentException)
                    {
                        return 0f;
                    }
                    catch (UnityException)
                    {
                        return 0f;
                    }

                    if (Mathf.Abs(value) <= binding.deadZone)
                    {
                        return 0f;
                    }

                    if (binding.axisMode == TetherfallInputAxisMode.Positive)
                    {
                        value = Mathf.Max(0f, value);
                    }
                    else if (binding.axisMode == TetherfallInputAxisMode.Negative)
                    {
                        value = Mathf.Max(0f, -value);
                    }

                    return value * binding.scale;
            }

            return 0f;
        }

        private static List<TetherfallInputConflict> FindAllConflicts(
            TetherfallInputBindingData sourceData)
        {
            var conflicts = new List<TetherfallInputConflict>();
            if (sourceData == null || sourceData.actions == null)
            {
                return conflicts;
            }

            for (var firstActionIndex = 0; firstActionIndex < sourceData.actions.Count; firstActionIndex++)
            {
                var firstAction = sourceData.actions[firstActionIndex];
                if (firstAction == null || firstAction.bindings == null)
                {
                    continue;
                }

                for (var firstBindingIndex = 0; firstBindingIndex < firstAction.bindings.Count; firstBindingIndex++)
                {
                    var firstBinding = firstAction.bindings[firstBindingIndex];
                    for (var secondActionIndex = firstActionIndex; secondActionIndex < sourceData.actions.Count; secondActionIndex++)
                    {
                        var secondAction = sourceData.actions[secondActionIndex];
                        if (secondAction == null || secondAction.bindings == null)
                        {
                            continue;
                        }

                        var secondBindingStart = secondActionIndex == firstActionIndex
                            ? firstBindingIndex + 1
                            : 0;
                        for (var secondBindingIndex = secondBindingStart;
                             secondBindingIndex < secondAction.bindings.Count;
                             secondBindingIndex++)
                        {
                            var secondBinding = secondAction.bindings[secondBindingIndex];
                            if (BindingsConflict(firstBinding, secondBinding))
                            {
                                conflicts.Add(new TetherfallInputConflict(
                                    firstAction.action,
                                    firstBindingIndex,
                                    secondAction.action,
                                    secondBindingIndex,
                                    GetBindingDisplayLabel(firstBinding)));
                            }
                        }
                    }
                }
            }

            return conflicts;
        }

        private List<TetherfallInputConflict> FindConflicts(
            TetherfallInputAction action,
            int ignoredBindingIndex,
            TetherfallInputBinding candidate)
        {
            var conflicts = new List<TetherfallInputConflict>();
            if (candidate == null || data == null || data.actions == null)
            {
                return conflicts;
            }

            for (var actionIndex = 0; actionIndex < data.actions.Count; actionIndex++)
            {
                var existingAction = data.actions[actionIndex];
                if (existingAction == null || existingAction.bindings == null)
                {
                    continue;
                }

                for (var bindingIndex = 0; bindingIndex < existingAction.bindings.Count; bindingIndex++)
                {
                    if (existingAction.action == action && bindingIndex == ignoredBindingIndex)
                    {
                        continue;
                    }

                    if (BindingsConflict(candidate, existingAction.bindings[bindingIndex]))
                    {
                        conflicts.Add(new TetherfallInputConflict(
                            action,
                            ignoredBindingIndex,
                            existingAction.action,
                            bindingIndex,
                            GetBindingDisplayLabel(candidate)));
                    }
                }
            }

            return conflicts;
        }

        private static bool BindingsConflict(
            TetherfallInputBinding first,
            TetherfallInputBinding second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            int firstMouse;
            int secondMouse;
            var firstIsMouse = TryGetMouseButtonIndex(first, out firstMouse);
            var secondIsMouse = TryGetMouseButtonIndex(second, out secondMouse);
            if (firstIsMouse || secondIsMouse)
            {
                return firstIsMouse && secondIsMouse && firstMouse == secondMouse;
            }

            if (first.source == TetherfallInputBindingSource.KeyOrJoystickButton &&
                second.source == TetherfallInputBindingSource.KeyOrJoystickButton)
            {
                int firstJoystick;
                int firstButton;
                int secondJoystick;
                int secondButton;
                var firstIsJoystick = TryParseJoystickButton(
                    first.keyCode,
                    out firstJoystick,
                    out firstButton);
                var secondIsJoystick = TryParseJoystickButton(
                    second.keyCode,
                    out secondJoystick,
                    out secondButton);
                if (firstIsJoystick || secondIsJoystick)
                {
                    return firstIsJoystick &&
                           secondIsJoystick &&
                           firstButton == secondButton &&
                           (firstJoystick == 0 ||
                            secondJoystick == 0 ||
                            firstJoystick == secondJoystick);
                }

                return first.keyCode == second.keyCode;
            }

            if (first.source == TetherfallInputBindingSource.LegacyAxis &&
                second.source == TetherfallInputBindingSource.LegacyAxis &&
                string.Equals(
                    first.axisName == null ? string.Empty : first.axisName.Trim(),
                    second.axisName == null ? string.Empty : second.axisName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return first.axisMode == TetherfallInputAxisMode.Full ||
                       second.axisMode == TetherfallInputAxisMode.Full ||
                       first.axisMode == second.axisMode;
            }

            return false;
        }

        private static bool TryPrepareData(
            TetherfallInputBindingData sourceData,
            out TetherfallInputBindingData prepared,
            out string error)
        {
            prepared = null;
            if (sourceData == null)
            {
                error = "Binding data is null.";
                return false;
            }

            if (sourceData.version > CurrentSchemaVersion)
            {
                error =
                    "Binding data version " + sourceData.version +
                    " is newer than supported version " + CurrentSchemaVersion + ".";
                return false;
            }

            var supplied = new Dictionary<TetherfallInputAction, TetherfallActionBindingData>();
            if (sourceData.actions != null)
            {
                for (var actionIndex = 0; actionIndex < sourceData.actions.Count; actionIndex++)
                {
                    var actionData = sourceData.actions[actionIndex];
                    if (actionData == null)
                    {
                        error = "Binding data contains a null action entry.";
                        return false;
                    }

                    if (GetActionIndex(actionData.action) < 0)
                    {
                        error = "Binding data contains unknown action " + actionData.action + ".";
                        return false;
                    }

                    if (supplied.ContainsKey(actionData.action))
                    {
                        error = "Binding data contains duplicate action " + actionData.action + ".";
                        return false;
                    }

                    var clonedAction = new TetherfallActionBindingData
                    {
                        action = actionData.action,
                        bindings = new List<TetherfallInputBinding>()
                    };
                    if (actionData.bindings != null)
                    {
                        for (var bindingIndex = 0; bindingIndex < actionData.bindings.Count; bindingIndex++)
                        {
                            var binding = actionData.bindings[bindingIndex];
                            if (!TryValidateBinding(binding, out error))
                            {
                                error =
                                    GetActionDisplayLabel(actionData.action) +
                                    " binding " + bindingIndex + ": " + error;
                                return false;
                            }

                            var clonedBinding = binding.Clone();
                            if (clonedBinding.source == TetherfallInputBindingSource.LegacyAxis)
                            {
                                clonedBinding.axisName = clonedBinding.axisName.Trim();
                            }

                            clonedAction.bindings.Add(clonedBinding);
                        }
                    }

                    supplied.Add(clonedAction.action, clonedAction);
                }
            }

            var defaults = CreateDefaultData();
            prepared = new TetherfallInputBindingData
            {
                version = CurrentSchemaVersion,
                actions = new List<TetherfallActionBindingData>()
            };
            for (var definitionIndex = 0; definitionIndex < Definitions.Length; definitionIndex++)
            {
                var action = Definitions[definitionIndex].action;
                TetherfallActionBindingData actionData;
                if (supplied.TryGetValue(action, out actionData))
                {
                    prepared.actions.Add(actionData);
                }
                else
                {
                    var defaultAction = FindActionData(defaults, action);
                    prepared.actions.Add(CloneActionData(defaultAction));
                }
            }

            error = string.Empty;
            return true;
        }

        private static TetherfallInputBindingData CloneData(TetherfallInputBindingData source)
        {
            var clone = new TetherfallInputBindingData
            {
                version = source != null ? source.version : CurrentSchemaVersion,
                actions = new List<TetherfallActionBindingData>()
            };
            if (source == null || source.actions == null)
            {
                return clone;
            }

            for (var index = 0; index < source.actions.Count; index++)
            {
                clone.actions.Add(CloneActionData(source.actions[index]));
            }

            return clone;
        }

        private static TetherfallActionBindingData CloneActionData(
            TetherfallActionBindingData source)
        {
            if (source == null)
            {
                return null;
            }

            return new TetherfallActionBindingData
            {
                action = source.action,
                bindings = CloneBindings(source.bindings)
            };
        }

        private static List<TetherfallInputBinding> CloneBindings(
            List<TetherfallInputBinding> source)
        {
            var clone = new List<TetherfallInputBinding>();
            if (source == null)
            {
                return clone;
            }

            for (var index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                {
                    clone.Add(source[index].Clone());
                }
            }

            return clone;
        }

        private static TetherfallActionBindingData FindActionData(
            TetherfallInputBindingData source,
            TetherfallInputAction action)
        {
            if (source == null || source.actions == null)
            {
                return null;
            }

            for (var index = 0; index < source.actions.Count; index++)
            {
                var actionData = source.actions[index];
                if (actionData != null && actionData.action == action)
                {
                    return actionData;
                }
            }

            return null;
        }

        private static ActionDefinition FindDefinition(TetherfallInputAction action)
        {
            var index = GetActionIndex(action);
            return index >= 0 ? Definitions[index] : null;
        }

        private static int GetActionIndex(TetherfallInputAction action)
        {
            for (var index = 0; index < Definitions.Length; index++)
            {
                if (Definitions[index].action == action)
                {
                    return index;
                }
            }

            return -1;
        }

        private void InvalidateSamples()
        {
            sampledFrame = int.MinValue;
            Array.Clear(sampledValues, 0, sampledValues.Length);
            Array.Clear(sampledHeld, 0, sampledHeld.Length);
            Array.Clear(sampledPressed, 0, sampledPressed.Length);
            Array.Clear(sampledReleased, 0, sampledReleased.Length);
        }

        private static bool TryGetMouseButtonIndex(
            TetherfallInputBinding binding,
            out int button)
        {
            button = -1;
            if (binding == null)
            {
                return false;
            }

            if (binding.source == TetherfallInputBindingSource.MouseButton)
            {
                button = binding.mouseButton;
                return true;
            }

            if (binding.source != TetherfallInputBindingSource.KeyOrJoystickButton)
            {
                return false;
            }

            switch (binding.keyCode)
            {
                case KeyCode.Mouse0:
                    button = 0;
                    return true;
                case KeyCode.Mouse1:
                    button = 1;
                    return true;
                case KeyCode.Mouse2:
                    button = 2;
                    return true;
                case KeyCode.Mouse3:
                    button = 3;
                    return true;
                case KeyCode.Mouse4:
                    button = 4;
                    return true;
                case KeyCode.Mouse5:
                    button = 5;
                    return true;
                case KeyCode.Mouse6:
                    button = 6;
                    return true;
                default:
                    return false;
            }
        }

        private static string GetMouseButtonLabel(int button)
        {
            switch (button)
            {
                case 0:
                    return "Left Mouse";
                case 1:
                    return "Right Mouse";
                case 2:
                    return "Middle Mouse";
                default:
                    return "Mouse " + (button + 1);
            }
        }

        private static string GetKeyDisplayLabel(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None:
                    return "Unbound";
                case KeyCode.Alpha0:
                    return "0";
                case KeyCode.Alpha1:
                    return "1";
                case KeyCode.Alpha2:
                    return "2";
                case KeyCode.Alpha3:
                    return "3";
                case KeyCode.Alpha4:
                    return "4";
                case KeyCode.Alpha5:
                    return "5";
                case KeyCode.Alpha6:
                    return "6";
                case KeyCode.Alpha7:
                    return "7";
                case KeyCode.Alpha8:
                    return "8";
                case KeyCode.Alpha9:
                    return "9";
                case KeyCode.Return:
                    return "Enter";
                case KeyCode.Escape:
                    return "Esc";
                case KeyCode.LeftControl:
                    return "Left Ctrl";
                case KeyCode.RightControl:
                    return "Right Ctrl";
                case KeyCode.LeftShift:
                    return "Left Shift";
                case KeyCode.RightShift:
                    return "Right Shift";
                case KeyCode.LeftAlt:
                    return "Left Alt";
                case KeyCode.RightAlt:
                    return "Right Alt";
                case KeyCode.UpArrow:
                    return "Up Arrow";
                case KeyCode.DownArrow:
                    return "Down Arrow";
                case KeyCode.LeftArrow:
                    return "Left Arrow";
                case KeyCode.RightArrow:
                    return "Right Arrow";
                case KeyCode.PageUp:
                    return "Page Up";
                case KeyCode.PageDown:
                    return "Page Down";
                case KeyCode.CapsLock:
                    return "Caps Lock";
                case KeyCode.Numlock:
                    return "Num Lock";
                case KeyCode.BackQuote:
                    return "`";
                case KeyCode.Minus:
                    return "-";
                case KeyCode.Equals:
                    return "=";
                case KeyCode.LeftBracket:
                    return "[";
                case KeyCode.RightBracket:
                    return "]";
                case KeyCode.Backslash:
                    return "\\";
                case KeyCode.Semicolon:
                    return ";";
                case KeyCode.Quote:
                    return "'";
                case KeyCode.Comma:
                    return ",";
                case KeyCode.Period:
                    return ".";
                case KeyCode.Slash:
                    return "/";
            }

            int joystick;
            int button;
            if (TryParseJoystickButton(key, out joystick, out button))
            {
                return joystick == 0
                    ? "Gamepad Button " + button
                    : "Gamepad " + joystick + " Button " + button;
            }

            return Humanize(key.ToString());
        }

        /// <summary>
        /// joystick == 0 means Unity's any-joystick KeyCode.
        /// </summary>
        private static bool TryParseJoystickButton(
            KeyCode key,
            out int joystick,
            out int button)
        {
            joystick = -1;
            button = -1;
            var name = key.ToString();
            if (!name.StartsWith("Joystick", StringComparison.Ordinal))
            {
                return false;
            }

            var buttonMarker = name.IndexOf("Button", StringComparison.Ordinal);
            if (buttonMarker < 0)
            {
                return false;
            }

            var joystickText = name.Substring("Joystick".Length, buttonMarker - "Joystick".Length);
            var buttonText = name.Substring(buttonMarker + "Button".Length);
            if (joystickText.Length == 0)
            {
                joystick = 0;
            }
            else if (!int.TryParse(joystickText, out joystick))
            {
                return false;
            }

            return int.TryParse(buttonText, out button);
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length + 8);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (index > 0 &&
                    char.IsUpper(character) &&
                    (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1])))
                {
                    builder.Append(' ');
                }

                builder.Append(character);
            }

            return builder.ToString();
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
