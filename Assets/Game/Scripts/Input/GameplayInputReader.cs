using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PiGame.Input
{
    public class GameplayInputReader : IGameplayInputSource
    {
        public const string ActionMapName = "Player";
        public const string KeyboardMouseGroup = "Keyboard&Mouse";
        public const string GamepadGroup = "Gamepad";

        private readonly InputActionMap _actionMap;
        private readonly InputAction _moveAction;
        private readonly InputAction _aimDirectionAction;
        private readonly InputAction _aimPointerAction;
        private readonly InputAction _aimFireAction;
        private readonly InputAction _cancelAimAction;
        private readonly InputAction _crouchAction;
        private readonly InputAction _jumpAction;
        private readonly InputAction _abilityAction;

        public GameplayInputReader(InputActionAsset actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            InputBindingService bindingService = new InputBindingService(actions);
            bindingService.Load();

            _actionMap = actions.FindActionMap(ActionMapName, true);
            _moveAction = FindRequiredAction("Move");
            _aimDirectionAction = FindRequiredAction("AimDirection");
            _aimPointerAction = FindRequiredAction("AimPointer");
            _aimFireAction = FindRequiredAction("AimFire");
            _cancelAimAction = FindRequiredAction("CancelAim");
            _crouchAction = FindRequiredAction("Crouch");
            _jumpAction = FindRequiredAction("Jump");
            _abilityAction = FindRequiredAction("Ability");
        }

        public Vector2 Move => _moveAction.ReadValue<Vector2>();
        public Vector2 AimDirection => _aimDirectionAction.ReadValue<Vector2>();
        public Vector2 PointerPosition => _aimPointerAction.ReadValue<Vector2>();
        public bool IsAimFirePressed => _aimFireAction.IsPressed();
        public bool IsCrouchPressed => _crouchAction.IsPressed();

        public void Enable()
        {
            _actionMap.Enable();
        }

        public void Disable()
        {
            _actionMap.Disable();
        }

        public bool WasJumpPressedThisFrame()
        {
            return _jumpAction.WasPressedThisFrame();
        }

        public bool WasCancelAimPressedThisFrame()
        {
            return _cancelAimAction.WasPressedThisFrame();
        }

        public bool WasAbilityPressedThisFrame()
        {
            return _abilityAction.WasPressedThisFrame();
        }

        private InputAction FindRequiredAction(string actionName)
        {
            return _actionMap.FindAction(actionName, true);
        }
    }
}
