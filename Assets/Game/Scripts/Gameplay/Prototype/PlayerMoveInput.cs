using PiGame.Input;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PiGame.Gameplay
{
    public class PlayerMoveInput : NetworkBehaviour, IGameplayInputBlocker
    {
        [SerializeField] private PlayerMove _movement;
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _crouchAction;
        [SerializeField] private InputActionReference _dropThroughAction;
        [SerializeField] private InputActionReference _aimFireAction;
        [SerializeField] private InputActionReference _abilityAction;

        private NetworkPlayerState _playerState;
        private InputActionMap _playerActions;
        private bool _isInputBlocked;
        private bool _waitForJumpRelease;

        public Vector2 Direction => _moveAction.action.ReadValue<Vector2>();

        private void Awake()
        {
            _playerState = GetComponent<NetworkPlayerState>();
            if (_movement == null || _playerState == null)
            {
                Debug.LogError("Configure PlayerMove e NetworkPlayerState no prefab do PlayerMoveInput.", this);
                enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!enabled || !IsOwner) return;
            if (!ConfigureInput())
            {
                enabled = false;
                return;
            }

            new InputBindingService(_playerActions.asset).Load();
            _playerActions.Enable();
        }

        public override void OnNetworkDespawn()
        {
            _isInputBlocked = false;
            _waitForJumpRelease = false;
            // Somente o proprietário configura e habilita este mapa.
            _playerActions?.Disable();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            if (!_movement.isActiveAndEnabled)
            {
                // Knockback desabilita PlayerMove; não reaplicar o pulo ao recuperar o movimento.
                _waitForJumpRelease = true;
                return;
            }

            if (_isInputBlocked || !_playerState.CanAct)
            {
                _waitForJumpRelease = true;
                _movement.ProcessInput(Vector2.zero, jumpPressed: false, jumpReleased: false,
                    crouchRequested: false, dropRequested: false, actionHeld: false);
                return;
            }

            bool jumpPressed = false;
            bool jumpReleased = false;
            if (_waitForJumpRelease)
            {
                if (!_jumpAction.action.IsPressed()) _waitForJumpRelease = false;
            }
            else
            {
                jumpPressed = _jumpAction.action.WasPressedThisFrame();
                jumpReleased = _jumpAction.action.WasReleasedThisFrame();
            }

            _movement.ProcessInput(Direction, jumpPressed, jumpReleased,
                crouchRequested: _crouchAction.action.IsPressed(),
                dropRequested: _dropThroughAction.action.WasPerformedThisFrame(),
                actionHeld: _aimFireAction.action.IsPressed() || _abilityAction.action.IsPressed());
        }

        public void SetGameplayInputBlocked(bool isBlocked)
        {
            if (!IsSpawned || !IsOwner || !enabled) return;
            _isInputBlocked = isBlocked;
            _dropThroughAction.action.Reset();
            if (isBlocked) _waitForJumpRelease = true;
            _movement.SetGameplayInputBlocked(isBlocked);
        }

        private bool ConfigureInput()
        {
            InputAction move = _moveAction != null ? _moveAction.action : null;
            InputAction jump = _jumpAction != null ? _jumpAction.action : null;
            InputAction crouch = _crouchAction != null ? _crouchAction.action : null;
            InputAction drop = _dropThroughAction != null ? _dropThroughAction.action : null;
            InputAction aim = _aimFireAction != null ? _aimFireAction.action : null;
            InputAction ability = _abilityAction != null ? _abilityAction.action : null;
            if (move == null || jump == null || crouch == null || drop == null
                || aim == null || ability == null || move.actionMap == null
                || move.actionMap != jump.actionMap || move.actionMap != crouch.actionMap
                || move.actionMap != drop.actionMap || move.actionMap != aim.actionMap
                || move.actionMap != ability.actionMap)
            {
                Debug.LogError("Configure Move, Jump, Crouch, DropThrough, AimFire e Ability do mesmo Action Map no PlayerMoveInput.", this);
                return false;
            }

            _playerActions = move.actionMap;
            return true;
        }
    }
}
