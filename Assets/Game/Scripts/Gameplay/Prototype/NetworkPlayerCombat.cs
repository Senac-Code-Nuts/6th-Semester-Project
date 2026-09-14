using PiGame.Lobby;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PiGame.Gameplay
{
    [RequireComponent(typeof(NetworkPlayerState))]
    public class NetworkPlayerCombat : NetworkBehaviour
    {
        [SerializeField] private NetworkProjectile _projectilePrefab;
        [SerializeField] private float _shotCooldownSeconds = 0.35f;
        [SerializeField] private float _projectileSpawnDistance = 0.85f;
        [SerializeField] private float _aimIndicatorLength = 1.15f;

        private readonly NetworkVariable<Vector2> _aimDirection =
            new NetworkVariable<Vector2>(
                Vector2.right,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isAiming =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        private NetworkPlayerState _playerState;
        private LineRenderer _aimLine;
        private bool _localAimHeld;
        private float _nextServerShotTime;

        private void Awake()
        {
            _playerState = GetComponent<NetworkPlayerState>();
            CreateAimLine();
        }

        public override void OnNetworkSpawn()
        {
            _aimDirection.OnValueChanged += HandleAimChanged;
            _isAiming.OnValueChanged += HandleAimingChanged;
            RefreshAimLine();
        }

        public override void OnNetworkDespawn()
        {
            _aimDirection.OnValueChanged -= HandleAimChanged;
            _isAiming.OnValueChanged -= HandleAimingChanged;
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            if (!_playerState.CanAct)
            {
                _localAimHeld = false;
                _isAiming.Value = false;
                return;
            }

            Vector2 aim = ReadAimDirection();
            if (aim.sqrMagnitude >= 0.04f)
            {
                _aimDirection.Value = aim.normalized;
            }

            bool aimPressed = ReadAimPressed();
            bool cancelPressed = ReadCancelPressed();

            if (cancelPressed && _localAimHeld)
            {
                _localAimHeld = false;
                _isAiming.Value = false;
                return;
            }

            if (aimPressed)
            {
                _localAimHeld = true;
                _isAiming.Value = true;
                return;
            }

            if (!_localAimHeld)
            {
                return;
            }

            _localAimHeld = false;
            _isAiming.Value = false;
            FireRpc(_aimDirection.Value);
        }

        [Rpc(SendTo.Server)]
        private void FireRpc(Vector2 direction, RpcParams rpcParams = default)
        {
            if (!_playerState.CanAct
                || rpcParams.Receive.SenderClientId != OwnerClientId
                || _projectilePrefab == null
                || Time.time < _nextServerShotTime)
            {
                return;
            }

            _nextServerShotTime = Time.time + _shotCooldownSeconds;

            Vector2 normalizedDirection = direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : Vector2.right;
            Vector3 spawnPosition =
                transform.position + (Vector3)(normalizedDirection * _projectileSpawnDistance);

            NetworkProjectile projectile =
                Instantiate(_projectilePrefab, spawnPosition, Quaternion.identity);
            projectile.NetworkObject.Spawn(true);
            projectile.InitializeServer(
                OwnerClientId,
                normalizedDirection,
                _playerState.IndicatorColor);
        }

        private Vector2 ReadAimDirection()
        {
            if (_playerState.InputDevice == LobbyInputDeviceKind.Gamepad
                && Gamepad.current != null)
            {
                return Gamepad.current.rightStick.ReadValue();
            }

            if (Mouse.current != null && Camera.main != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mousePosition);
                return mouseWorld - transform.position;
            }

            return _aimDirection.Value;
        }

        private bool ReadAimPressed()
        {
            if (_playerState.InputDevice == LobbyInputDeviceKind.Gamepad
                && Gamepad.current != null)
            {
                return Gamepad.current.leftTrigger.isPressed;
            }

            return Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        private bool ReadCancelPressed()
        {
            if (_playerState.InputDevice == LobbyInputDeviceKind.Gamepad
                && Gamepad.current != null)
            {
                return Gamepad.current.leftShoulder.wasPressedThisFrame;
            }

            return Keyboard.current != null
                && (Keyboard.current.leftShiftKey.wasPressedThisFrame
                    || Keyboard.current.rightShiftKey.wasPressedThisFrame);
        }

        private void CreateAimLine()
        {
            GameObject lineObject = new GameObject("AimIndicator");
            lineObject.transform.SetParent(transform, false);

            _aimLine = lineObject.AddComponent<LineRenderer>();
            _aimLine.useWorldSpace = false;
            _aimLine.positionCount = 2;
            _aimLine.startWidth = 0.08f;
            _aimLine.endWidth = 0.03f;
            _aimLine.sortingOrder = 20;
            _aimLine.material = new Material(Shader.Find("Sprites/Default"));
            _aimLine.enabled = false;
        }

        private void HandleAimChanged(Vector2 previousValue, Vector2 currentValue)
        {
            RefreshAimLine();
        }

        private void HandleAimingChanged(bool previousValue, bool currentValue)
        {
            RefreshAimLine();
        }

        private void RefreshAimLine()
        {
            if (_aimLine == null)
            {
                return;
            }

            _aimLine.enabled = _isAiming.Value && _playerState.IsAlive;
            _aimLine.startColor = _playerState.IndicatorColor;
            _aimLine.endColor = _playerState.IndicatorColor;
            _aimLine.SetPosition(0, Vector3.zero);
            _aimLine.SetPosition(1, _aimDirection.Value * _aimIndicatorLength);
        }
    }
}
