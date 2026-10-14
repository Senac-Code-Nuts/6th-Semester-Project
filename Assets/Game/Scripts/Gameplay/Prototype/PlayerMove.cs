using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace PiGame.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : NetworkBehaviour
    {
        [Header("Components")]
        [SerializeField] private PlayerMoveInput _input;
        [SerializeField] private PlayerAnimatorController _presentation;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _jumpForce = 12f;

        [Header("Ground")]
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;

        [Header("Platforms")]
        [SerializeField, Min(0.1f)] private float _dropThroughTimeoutSeconds = 1f;

        [Header("Jump")]
        [SerializeField] private float _jumpBufferTime;
        [SerializeField] private float _coyoteTimer;

        [Header("Wall")]
        [SerializeField] private Transform _wallCheck;
        [SerializeField] private float _wallCheckRadius = 0.15f;
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private float _wallStickTime = 0.5f;
        [SerializeField] private float _wallSlideSpeed = 3f;

        [Header("Wall Jump")]
        [SerializeField] private float _wallJumpHorizontalForce = 10f;
        [SerializeField] private float _wallJumpVerticalForce = 12f;
        [SerializeField] private float _wallJumpControlLockTime = 0.2f;

        [Header("Crouch")]
        [FormerlySerializedAs("_colliderShirnkOffset")]
        [SerializeField] private float _crouchColliderOffset;
        [FormerlySerializedAs("_colliderShirnkSize")]
        [SerializeField] private float _crouchColliderHeightMultiplier;

        private readonly NetworkVariable<bool> _crouching = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private Rigidbody2D _rigidbody;
        private BoxCollider2D _bodyCollider;
        private NetworkPlayerState _playerState;
        private bool _referencesConfigured;
        private Vector2 _originalColliderSize;
        private Vector2 _originalColliderOffset;
        private float _horizontalInput;
        private float _facingInput;
        private Vector2 _lastMovementDirection = Vector2.right;
        private bool _isGrounded;
        private bool _isJumping;
        private bool _isTouchingWall;
        private bool _isWallSliding;
        private bool _isDroppingFromWall;
        private int _wallDirection;
        private float _stickTimer;
        private float _wallJumpControlTimer;
        private float _jumpBufferCounter;
        private float _coyoteCounter;
        private bool _isInputBlocked;
        private bool _hasHorizontalMovementOverride;
        private float _horizontalMovementOverrideSpeed;
        private RaycastHit2D _groundHit;
        private Collider2D _ignoredPlatform;
        private float _dropThroughSurfaceY;
        private float _dropThroughEndsAt;

        public bool IsCrouching => _crouching.Value;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _bodyCollider = GetComponent<BoxCollider2D>();
            _playerState = GetComponent<NetworkPlayerState>();
            if (_rigidbody == null || _bodyCollider == null || _playerState == null
                || _input == null || _presentation == null
                || _groundCheck == null || _wallCheck == null)
            {
                Debug.LogError("Configure os componentes, Ground Check e Wall Check do PlayerMove no prefab.", this);
                enabled = false;
                return;
            }

            _originalColliderSize = _bodyCollider.size;
            _originalColliderOffset = _bodyCollider.offset;
            _referencesConfigured = true;
        }

        public override void OnNetworkSpawn()
        {
            if (!_referencesConfigured) return;
            _crouching.OnValueChanged += OnCrouchChanged;
            OnCrouchChanged(false, _crouching.Value);
        }

        public override void OnNetworkDespawn()
        {
            _crouching.OnValueChanged -= OnCrouchChanged;
            if (!_referencesConfigured) return;
            _isInputBlocked = false;
            _hasHorizontalMovementOverride = false;
            _lastMovementDirection = Vector2.right;
            _jumpBufferCounter = 0f;
            RestorePlatformCollision();
            if (IsOwner) ResetCrouch();
            OnCrouchChanged(false, false);
        }

        // PlayerMoveInput chama uma vez por Update, preservando o ciclo atual da física.
        public void ProcessInput(Vector2 direction, bool jumpPressed, bool jumpReleased,
            bool crouchRequested, bool dropRequested, bool actionHeld)
        {
            if (!isActiveAndEnabled || !IsSpawned || !IsOwner) return;
            _horizontalInput = direction.x;
            if (Mathf.Abs(_horizontalInput) > 0.01f)
                _lastMovementDirection = _horizontalInput < 0f ? Vector2.left : Vector2.right;
            _facingInput = 0f;
            UpdatePlatformCollision();
            ApplyMovement(jumpPressed, jumpReleased, crouchRequested, dropRequested, actionHeld);
            _presentation.UpdateMovement(_rigidbody.linearVelocity, _facingInput,
                _isJumping, IsCrouching, _isWallSliding);
        }

        private void ApplyMovement(bool jumpPressed, bool jumpReleased,
            bool crouchRequested, bool dropRequested, bool actionHeld)
        {
            if (_isInputBlocked || !_playerState.CanAct)
            {
                ResetCrouch();
                _jumpBufferCounter = 0f;
                if (!_playerState.CanAct) RestorePlatformCollision();
                _rigidbody.linearVelocity = new Vector2(0f,
                    _playerState.CanAct ? _rigidbody.linearVelocity.y : 0f);
                return;
            }

            _wallJumpControlTimer = Mathf.Max(0f, _wallJumpControlTimer - Time.deltaTime);
            CheckGround();
            CheckWall();
            if (_hasHorizontalMovementOverride)
            {
                ResetCrouch();
                _rigidbody.linearVelocity = new Vector2(
                    _horizontalMovementOverrideSpeed, _rigidbody.linearVelocity.y);
                _facingInput = _horizontalMovementOverrideSpeed;
                return;
            }

            UpdateCrouch(crouchRequested, actionHeld);
            if (dropRequested) TryDropThroughPlatform();
            HandleMovement();
            HandleWallSlide();
            HandleJump(jumpPressed, jumpReleased);
        }

        public void SetGameplayInputBlocked(bool isBlocked)
        {
            if (!IsOwner) return;
            _isInputBlocked = isBlocked;
            if (!isBlocked) return;
            _jumpBufferCounter = 0f;
            ResetCrouch();
            _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
        }

        public Vector2 ReadMovementDirection()
        {
            float horizontal = _input.Direction.x;
            return Mathf.Abs(horizontal) <= 0.01f
                ? _lastMovementDirection
                : (horizontal < 0f ? Vector2.left : Vector2.right);
        }

        public bool CanBeginWallRestrictedAbility()
        {
            return !_bodyCollider.IsTouchingLayers(_wallLayer) && _wallJumpControlTimer <= 0f;
        }

        public void BeginHorizontalMovementOverride(float speed)
        {
            if (!IsOwner) return;
            _horizontalMovementOverrideSpeed = speed;
            _hasHorizontalMovementOverride = true;
            ResetCrouch();
        }

        public void EndHorizontalMovementOverride()
        {
            if (IsOwner) _hasHorizontalMovementOverride = false;
        }

        private void HandleMovement()
        {
            if (_wallJumpControlTimer > 0f) return;
            float speed = IsCrouching ? 0f : _horizontalInput * _moveSpeed;
            _rigidbody.linearVelocity = new Vector2(speed, _rigidbody.linearVelocity.y);
            _facingInput = _horizontalInput;
            UpdateWallCheckPosition();
        }

        private void HandleJump(bool pressed, bool released)
        {
            if (!IsCrouching && pressed) _jumpBufferCounter = _jumpBufferTime;
            if (released)
            {
                _jumpBufferCounter = 0f;
                if (_rigidbody.linearVelocity.y > 0f)
                    _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, 1f);
            }

            if (_jumpBufferCounter <= 0f) return;
            _jumpBufferCounter -= Time.deltaTime;
            if (_isWallSliding)
            {
                _rigidbody.linearVelocity = new Vector2(
                    -_wallDirection * _wallJumpHorizontalForce, _wallJumpVerticalForce);
                _wallJumpControlTimer = _wallJumpControlLockTime;
                _stickTimer = 0f;
            }
            else if (_coyoteCounter > 0f)
            {
                _coyoteCounter = 0f;
                _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, _jumpForce);
                _isGrounded = false;
            }
            else return;

            _isJumping = true;
            _jumpBufferCounter = 0f;
        }

        private void CheckGround()
        {
            Bounds bounds = _bodyCollider.bounds;
            Vector2 origin = new(_groundCheck.position.x, bounds.min.y + Physics2D.defaultContactOffset);
            Vector2 size = new(Mathf.Max(0.05f, bounds.size.x * 0.75f), Physics2D.defaultContactOffset * 2f);
            _groundHit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, _groundCheckDistance, _groundLayer);
            _isGrounded = _rigidbody.linearVelocity.y <= 0.05f
                && _groundHit.collider != null && _groundHit.collider != _ignoredPlatform
                && _groundHit.normal.y >= 0.5f;
            if (_isGrounded)
            {
                _isJumping = false;
                _coyoteCounter = _coyoteTimer;
            }
            else _coyoteCounter = Mathf.Max(0f, _coyoteCounter - Time.deltaTime);
        }

        private void CheckWall()
        {
            if (_isGrounded)
            {
                _isWallSliding = false;
                _stickTimer = _wallStickTime;
                return;
            }

            UpdateWallCheckPosition();
            Collider2D wall = Physics2D.OverlapCircle(_wallCheck.position, _wallCheckRadius, _wallLayer);
            _isTouchingWall = wall != null && wall != _ignoredPlatform;
            _wallDirection = wall == null ? 0 : (wall.bounds.center.x >= transform.position.x ? 1 : -1);
        }

        private void UpdateWallCheckPosition()
        {
            if (Mathf.Abs(_horizontalInput) > 0.01f)
                _wallCheck.position = new Vector2(
                    transform.position.x + 0.25f * Mathf.Sign(_horizontalInput), transform.position.y);
        }

        private void HandleWallSlide()
        {
            if (_wallJumpControlTimer > 0f) return;
            bool pressingWall = _wallDirection * _horizontalInput > 0f;
            if (_isDroppingFromWall || _isGrounded || !_isTouchingWall || !pressingWall)
            {
                _isWallSliding = false;
                _stickTimer = 0f;
                return;
            }

            _isWallSliding = true;
            _stickTimer += Time.deltaTime;
            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x,
                _stickTimer < _wallStickTime ? 0f : -_wallSlideSpeed);
        }

        private void UpdateCrouch(bool requested, bool actionHeld)
        {
            SetCrouching(requested && _isGrounded && !actionHeld);
            if (requested && !_isGrounded && _isTouchingWall)
            {
                _isDroppingFromWall = true;
                _isWallSliding = false;
                _stickTimer = 0f;
            }
            else if (!requested || _isGrounded) _isDroppingFromWall = false;
        }

        private void ResetCrouch()
        {
            SetCrouching(false);
            _isDroppingFromWall = false;
        }

        private void SetCrouching(bool crouching)
        {
            if (_crouching.Value != crouching) _crouching.Value = crouching;
        }

        private void OnCrouchChanged(bool previousValue, bool crouching)
        {
            _bodyCollider.size = crouching
                ? new Vector2(_originalColliderSize.x, _originalColliderSize.y * _crouchColliderHeightMultiplier)
                : _originalColliderSize;
            _bodyCollider.offset = crouching
                ? new Vector2(_originalColliderOffset.x, _crouchColliderOffset)
                : _originalColliderOffset;
            _presentation.SetCrouching(crouching);
        }

        private void TryDropThroughPlatform()
        {
            if (!_isGrounded || _ignoredPlatform != null) return;
            Collider2D platform = _groundHit.collider;
            if (!platform.usedByEffector || !platform.TryGetComponent(out PlatformEffector2D effector)
                || !effector.isActiveAndEnabled || !effector.useOneWay) return;

            ResetCrouch();
            _ignoredPlatform = platform;
            _dropThroughSurfaceY = _groundHit.point.y;
            _dropThroughEndsAt = Time.time + _dropThroughTimeoutSeconds;
            Physics2D.IgnoreCollision(_bodyCollider, platform, true);
            _isGrounded = false;
            _coyoteCounter = 0f;
            _jumpBufferCounter = 0f;
            _isTouchingWall = false;
            _isWallSliding = false;
            _stickTimer = 0f;
            _rigidbody.linearVelocity = new Vector2(
                _rigidbody.linearVelocity.x, Mathf.Min(_rigidbody.linearVelocity.y, -1f));
        }

        private void UpdatePlatformCollision()
        {
            if (_ignoredPlatform != null
                && (_bodyCollider.bounds.max.y < _dropThroughSurfaceY - Physics2D.defaultContactOffset
                    || Time.time >= _dropThroughEndsAt)) RestorePlatformCollision();
        }

        private void RestorePlatformCollision()
        {
            if (_ignoredPlatform != null) Physics2D.IgnoreCollision(_bodyCollider, _ignoredPlatform, false);
            _ignoredPlatform = null;
        }

        private void OnDisable()
        {
            _jumpBufferCounter = 0f;
            RestorePlatformCollision();
        }
    }
}

