using System;
using System.Collections;
using System.Collections.Generic;
using PiGame.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class ControlsSettingsUI : MonoBehaviour
    {
        private class BindingButton
        {
            public string ActionName;
            public string PartName;
            public string Group;
            public Button Button;
        }

        private readonly List<BindingButton> _bindingButtons = new List<BindingButton>();

        private InputActionAsset _actions;
        private InputBindingService _bindingService;
        [Header("Hierarquia")]
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _conflictRoot;
        [SerializeField] private TMP_Text _conflictText;
        [SerializeField] private Button _conflictConfirmButton;
        [SerializeField] private Button _conflictCancelButton;
        [SerializeField] private Button _resetKeyboardButton;
        [SerializeField] private Button _resetGamepadButton;
        [SerializeField] private Button _backButton;

        [Header("Bindings de teclado")]
        [SerializeField] private Button _moveLeftKeyboardButton;
        [SerializeField] private Button _moveRightKeyboardButton;
        [SerializeField] private Button _jumpKeyboardButton;
        [SerializeField] private Button _crouchKeyboardButton;
        [SerializeField] private Button _aimFireKeyboardButton;
        [SerializeField] private Button _cancelAimKeyboardButton;
        [SerializeField] private Button _abilityKeyboardButton;

        [Header("Bindings de controle")]
        [SerializeField] private Button _jumpGamepadButton;
        [SerializeField] private Button _crouchGamepadButton;
        [SerializeField] private Button _aimFireGamepadButton;
        [SerializeField] private Button _cancelAimGamepadButton;
        [SerializeField] private Button _abilityGamepadButton;
        private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
        private BindingButton _pendingTarget;
        private InputAction _pendingConflictAction;
        private int _pendingTargetBindingIndex = -1;
        private int _pendingConflictBindingIndex = -1;
        private string _pendingCandidatePath;
        private string _pendingOriginalPath;
        private bool _pendingActionWasEnabled;
        private Coroutine _selectionRoutine;
        private Coroutine _rebindStartRoutine;
        private bool _staticButtonsBound;
        // Evita que o mesmo B/bolinha conclua o rebind e volte no menu no mesmo frame.
        private int _lastRebindFinishedFrame = -1;

        public bool IsVisible => enabled && _root != null && _root.activeSelf;
        private bool IsCapturingBinding => _rebindOperation != null || _rebindStartRoutine != null;
        public bool BlocksBackShortcut => IsCapturingBinding || _lastRebindFinishedFrame == Time.frameCount;

        public event Action Closed;

        public bool Initialize(InputActionAsset actions)
        {
            _actions = actions;
            if (_actions == null)
            {
                Debug.LogError("ControlsSettingsUI precisa de um InputActionAsset.", this);
                enabled = false;
                return false;
            }

            if (!ValidateSceneReferences())
            {
                enabled = false;
                return false;
            }

            _bindingService = new InputBindingService(_actions);
            _bindingService.Load();
            ConfigureSceneBindings();
            BindStaticButtons();
            RefreshBindings();
            Hide(false);
            return true;
        }

        private void OnDisable()
        {
            CancelRebind();
            StopSelectionRoutine();
        }

        private void OnDestroy()
        {
            UnbindStaticButtons();
        }

        private void Update()
        {
            if (IsCapturingBinding
                && Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelRebind();
                return;
            }

            if (_rebindOperation != null && WasOppositeDeviceUsed())
            {
                CancelRebind();
            }
        }

        public void Show()
        {
            _root.SetActive(true);
            _conflictRoot.SetActive(false);
            RefreshBindings();
            QueueSelection(_moveLeftKeyboardButton);
        }

        public void Hide(bool notify = true)
        {
            CancelRebind();
            _root.SetActive(false);

            if (notify)
            {
                Closed?.Invoke();
            }
        }

        public bool HandleBack()
        {
            if (!IsVisible)
            {
                return false;
            }

            if (IsCapturingBinding)
            {
                CancelRebind();
                return true;
            }

            if (_conflictRoot.activeSelf)
            {
                CancelConflict();
                return true;
            }

            Hide();
            return true;
        }

        private void ConfigureSceneBindings()
        {
            if (_bindingButtons.Count > 0)
            {
                return;
            }

            RegisterBindingButton(_moveLeftKeyboardButton, "Move", "left", InputBindingService.KeyboardMouseGroup);
            RegisterBindingButton(_moveRightKeyboardButton, "Move", "right", InputBindingService.KeyboardMouseGroup);
            RegisterBindingButton(_jumpKeyboardButton, "Jump", null, InputBindingService.KeyboardMouseGroup);
            RegisterBindingButton(_crouchKeyboardButton, "Crouch", null, InputBindingService.KeyboardMouseGroup);
            RegisterBindingButton(_aimFireKeyboardButton, "AimFire", null, InputBindingService.KeyboardMouseGroup);
            RegisterBindingButton(_cancelAimKeyboardButton, "CancelAim", null, InputBindingService.KeyboardMouseGroup);
            RegisterBindingButton(_abilityKeyboardButton, "Ability", null, InputBindingService.KeyboardMouseGroup);
            RegisterBindingButton(_jumpGamepadButton, "Jump", null, InputBindingService.GamepadGroup);
            RegisterBindingButton(_crouchGamepadButton, "Crouch", null, InputBindingService.GamepadGroup);
            RegisterBindingButton(_aimFireGamepadButton, "AimFire", null, InputBindingService.GamepadGroup);
            RegisterBindingButton(_cancelAimGamepadButton, "CancelAim", null, InputBindingService.GamepadGroup);
            RegisterBindingButton(_abilityGamepadButton, "Ability", null, InputBindingService.GamepadGroup);
        }

        private void RegisterBindingButton(
            Button button,
            string actionName,
            string partName,
            string group)
        {
            BindingButton bindingButton = new BindingButton
            {
                ActionName = actionName,
                PartName = partName,
                Group = group,
                Button = button
            };
            button.onClick.AddListener(() => BeginRebind(bindingButton));
            _bindingButtons.Add(bindingButton);
        }

        private void BindStaticButtons()
        {
            if (_staticButtonsBound)
            {
                return;
            }

            _resetKeyboardButton.onClick.AddListener(ResetKeyboard);
            _resetGamepadButton.onClick.AddListener(ResetGamepad);
            _backButton.onClick.AddListener(HandleBackClicked);
            _conflictConfirmButton.onClick.AddListener(ConfirmConflictSwap);
            _conflictCancelButton.onClick.AddListener(CancelConflict);
            _staticButtonsBound = true;
        }

        private void UnbindStaticButtons()
        {
            if (!_staticButtonsBound)
            {
                return;
            }

            _resetKeyboardButton.onClick.RemoveListener(ResetKeyboard);
            _resetGamepadButton.onClick.RemoveListener(ResetGamepad);
            _backButton.onClick.RemoveListener(HandleBackClicked);
            _conflictConfirmButton.onClick.RemoveListener(ConfirmConflictSwap);
            _conflictCancelButton.onClick.RemoveListener(CancelConflict);
            _staticButtonsBound = false;
        }

        private void ResetKeyboard() => ResetGroup(InputBindingService.KeyboardMouseGroup);
        private void ResetGamepad() => ResetGroup(InputBindingService.GamepadGroup);
        private void HandleBackClicked() => Hide();

        private bool ValidateSceneReferences()
        {
            bool valid = _root != null
                && _conflictRoot != null
                && _conflictText != null
                && _conflictConfirmButton != null
                && _conflictCancelButton != null
                && _resetKeyboardButton != null
                && _resetGamepadButton != null
                && _backButton != null
                && _moveLeftKeyboardButton != null
                && _moveRightKeyboardButton != null
                && _jumpKeyboardButton != null
                && _crouchKeyboardButton != null
                && _aimFireKeyboardButton != null
                && _cancelAimKeyboardButton != null
                && _abilityKeyboardButton != null
                && _jumpGamepadButton != null
                && _crouchGamepadButton != null
                && _aimFireGamepadButton != null
                && _cancelAimGamepadButton != null
                && _abilityGamepadButton != null;
            if (!valid)
            {
                Debug.LogError(
                    "ControlsSettingsUI possui referências obrigatórias ausentes. Configure a hierarquia pela cena ou prefab.",
                    this);
            }

            return valid;
        }

        private void BeginRebind(BindingButton target)
        {
            if (IsCapturingBinding)
            {
                return;
            }

            InputAction action = _actions.FindActionMap(InputBindingService.ActionMapName, true)
                .FindAction(target.ActionName, true);
            int bindingIndex = FindBindingIndex(action, target.Group, target.PartName);
            if (bindingIndex < 0)
            {
                Debug.LogError(
                    $"Binding de {target.ActionName} ({target.Group}) não encontrado no InputActionAsset.",
                    this);
                return;
            }

            _pendingTarget = target;
            _pendingTargetBindingIndex = bindingIndex;
            _pendingOriginalPath = action.bindings[bindingIndex].effectivePath;
            _pendingActionWasEnabled = action.enabled;
            SetButtonLabel(target.Button, "PRESSIONE...");
            SetBindingButtonsInteractable(false);

            _rebindStartRoutine = StartCoroutine(StartRebindAfterSubmitRelease(action, bindingIndex));
        }

        private IEnumerator StartRebindAfterSubmitRelease(InputAction action, int bindingIndex)
        {
            while ((Mouse.current != null && Mouse.current.leftButton.isPressed)
                || (Keyboard.current != null
                    && (Keyboard.current.enterKey.isPressed
                        || Keyboard.current.numpadEnterKey.isPressed
                        || Keyboard.current.spaceKey.isPressed))
                || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed))
            {
                yield return null;
            }

            yield return null;
            _rebindStartRoutine = null;
            if (_pendingTarget == null)
            {
                yield break;
            }

            action.Disable();
            InputActionRebindingExtensions.RebindingOperation operation =
                action.PerformInteractiveRebinding(bindingIndex)
                    .WithMatchingEventsBeingSuppressed()
                    .OnCancel(HandleRebindCanceled)
                    .OnComplete(HandleRebindCompleted);

            if (_pendingTarget.Group == InputBindingService.GamepadGroup)
            {
                operation.WithControlsHavingToMatchPath("<Gamepad>")
                    .WithCancelingThrough("<Keyboard>/escape");
            }
            else
            {
                operation.WithControlsExcluding("<Gamepad>")
                    .WithControlsExcluding("<Touchscreen>")
                    .WithControlsExcluding("<Joystick>")
                    .WithControlsExcluding("<Keyboard>/escape")
                    .WithControlsExcluding("<Pointer>/position")
                    .WithControlsExcluding("<Pointer>/delta")
                    .WithCancelingThrough("<Keyboard>/escape");
            }

            _rebindOperation = operation;
            operation.Start();
        }

        private void HandleRebindCompleted(InputActionRebindingExtensions.RebindingOperation operation)
        {
            _lastRebindFinishedFrame = Time.frameCount;
            InputAction action = operation.action;
            int targetIndex = _pendingTargetBindingIndex;
            string candidatePath = action.bindings[targetIndex].effectivePath;
            operation.Dispose();
            _rebindOperation = null;
            if (_pendingActionWasEnabled)
            {
                action.Enable();
            }

            if (TryFindConflict(_pendingTarget, action, targetIndex, candidatePath, out InputAction conflictAction, out int conflictIndex))
            {
                action.ApplyBindingOverride(targetIndex, _pendingOriginalPath);
                _pendingCandidatePath = candidatePath;
                _pendingConflictAction = conflictAction;
                _pendingConflictBindingIndex = conflictIndex;
                _conflictText.text = $"{GetActionLabel(_pendingTarget.ActionName)} JÁ USA ESSE BOTÃO EM "
                    + $"{GetActionLabel(conflictAction.name)}. DESEJA TROCAR?";
                _conflictRoot.SetActive(true);
                QueueSelection(_conflictCancelButton);
                return;
            }

            _bindingService.Save();
            FinishRebind();
        }

        private void HandleRebindCanceled(InputActionRebindingExtensions.RebindingOperation operation)
        {
            _lastRebindFinishedFrame = Time.frameCount;
            InputAction action = operation.action;
            operation.Dispose();
            _rebindOperation = null;
            if (_pendingActionWasEnabled)
            {
                action.Enable();
            }
            FinishRebind();
        }

        private void ConfirmConflictSwap()
        {
            if (_pendingTarget == null || _pendingConflictAction == null)
            {
                CancelConflict();
                return;
            }

            InputAction targetAction = _actions.FindActionMap(InputBindingService.ActionMapName, true)
                .FindAction(_pendingTarget.ActionName, true);
            int targetIndex = FindBindingIndex(targetAction, _pendingTarget.Group, _pendingTarget.PartName);
            targetAction.ApplyBindingOverride(targetIndex, _pendingCandidatePath);
            _pendingConflictAction.ApplyBindingOverride(_pendingConflictBindingIndex, _pendingOriginalPath);
            _bindingService.Save();
            _conflictRoot.SetActive(false);
            FinishRebind();
        }

        private void CancelConflict()
        {
            _conflictRoot.SetActive(false);
            FinishRebind();
        }

        private void FinishRebind()
        {
            Button selection = _pendingTarget?.Button ?? _moveLeftKeyboardButton;
            _pendingTarget = null;
            _pendingConflictAction = null;
            _pendingConflictBindingIndex = -1;
            _pendingTargetBindingIndex = -1;
            _pendingCandidatePath = null;
            _pendingOriginalPath = null;
            _pendingActionWasEnabled = false;
            SetBindingButtonsInteractable(true);
            RefreshBindings();
            QueueSelection(selection);
        }

        private void CancelRebind()
        {
            if (_rebindStartRoutine != null)
            {
                StopCoroutine(_rebindStartRoutine);
                _rebindStartRoutine = null;
                _lastRebindFinishedFrame = Time.frameCount;
                FinishRebind();
                return;
            }

            if (_rebindOperation == null)
            {
                return;
            }

            _rebindOperation.Cancel();
        }

        private bool WasOppositeDeviceUsed()
        {
            if (_pendingTarget == null)
            {
                return false;
            }

            if (_pendingTarget.Group == InputBindingService.KeyboardMouseGroup)
            {
                return WasAnyGamepadButtonPressed();
            }

            bool keyboardPressed = Keyboard.current != null
                && Keyboard.current.anyKey.wasPressedThisFrame;
            bool mousePressed = Mouse.current != null
                && (Mouse.current.leftButton.wasPressedThisFrame
                    || Mouse.current.rightButton.wasPressedThisFrame
                    || Mouse.current.middleButton.wasPressedThisFrame
                    || Mouse.current.forwardButton.wasPressedThisFrame
                    || Mouse.current.backButton.wasPressedThisFrame);
            return keyboardPressed || mousePressed;
        }

        private static bool WasAnyGamepadButtonPressed()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return false;
            }

            foreach (InputControl control in gamepad.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindConflict(
            BindingButton target,
            InputAction targetAction,
            int targetIndex,
            string candidatePath,
            out InputAction conflictAction,
            out int conflictIndex)
        {
            InputActionMap playerMap = _actions.FindActionMap(InputBindingService.ActionMapName, true);
            foreach (InputAction action in playerMap.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    InputBinding binding = action.bindings[i];
                    if ((action == targetAction && i == targetIndex)
                        || binding.isComposite
                        || !BelongsToGroup(binding.groups, target.Group)
                        || !string.Equals(binding.effectivePath, candidatePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    conflictAction = action;
                    conflictIndex = i;
                    return true;
                }
            }

            conflictAction = null;
            conflictIndex = -1;
            return false;
        }

        private void ResetGroup(string group)
        {
            CancelRebind();
            _bindingService.ResetBindingGroup(group);
            RefreshBindings();
            QueueSelection(_moveLeftKeyboardButton);
        }

        private void RefreshBindings()
        {
            InputActionMap map = _actions.FindActionMap(InputBindingService.ActionMapName, true);
            foreach (BindingButton item in _bindingButtons)
            {
                InputAction action = map.FindAction(item.ActionName, true);
                int index = FindBindingIndex(action, item.Group, item.PartName);
                string display = index >= 0
                    ? InputControlPath.ToHumanReadableString(
                        action.bindings[index].effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice)
                    : "NÃO DEFINIDO";
                SetButtonLabel(item.Button, display.ToUpperInvariant());
            }
        }

        private static int FindBindingIndex(InputAction action, string group, string partName)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (!BelongsToGroup(binding.groups, group))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(partName))
                {
                    if (!binding.isComposite && !binding.isPartOfComposite)
                    {
                        return i;
                    }

                    continue;
                }

                if (binding.isPartOfComposite
                    && string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool BelongsToGroup(string groups, string group)
        {
            if (string.IsNullOrWhiteSpace(groups))
            {
                return false;
            }

            string[] values = groups.Split(';');
            foreach (string value in values)
            {
                if (string.Equals(value, group, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetBindingButtonsInteractable(bool interactable)
        {
            foreach (BindingButton bindingButton in _bindingButtons)
            {
                bindingButton.Button.interactable = interactable;
            }
        }

        private static string GetActionLabel(string actionName)
        {
            return actionName switch
            {
                "Move" => "MOVIMENTO",
                "Jump" => "PULAR",
                "Crouch" => "AGACHAR",
                "AimFire" => "MIRAR / ATIRAR",
                "CancelAim" => "CANCELAR MIRA",
                "Ability" => "HABILIDADE",
                _ => actionName.ToUpperInvariant()
            };
        }

        private void QueueSelection(Button button)
        {
            StopSelectionRoutine();
            if (button != null && gameObject.activeInHierarchy)
            {
                _selectionRoutine = StartCoroutine(SelectNextFrame(button));
            }
        }

        private IEnumerator SelectNextFrame(Button button)
        {
            yield return null;
            _selectionRoutine = null;
            if (EventSystem.current != null && button != null && button.interactable)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private void StopSelectionRoutine()
        {
            if (_selectionRoutine == null)
            {
                return;
            }

            StopCoroutine(_selectionRoutine);
            _selectionRoutine = null;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text == null)
            {
                Debug.LogError($"O botão {button.name} precisa de um TMP_Text filho.", button);
                return;
            }

            text.text = label;
        }

    }
}
