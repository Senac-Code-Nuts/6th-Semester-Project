using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System;
using PiGame.Input;

namespace PiGame.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : NetworkBehaviour, IGameplayInputBlocker
    {
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _jumpForce = 12f;

        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _crouchAction;
        [SerializeField] private InputActionReference _aimFireAction;
        [SerializeField] private InputActionReference _abilityAction;

        private Rigidbody2D _rigidbody;
        private BoxCollider2D _boxCollider2D;
        private NetworkPlayerState _playerState;

        private Animator _playerAnimator;
        private SpriteRenderer _spriteRenderer;

        [Header("Ground")]
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;

        [Header("Jump")]
        [SerializeField] private float _jumpBufferTime;
        [SerializeField] private float _coyoteTimer;
        private float _jumpBufferCounter;
        private float _coyoteCounter;


        [Header("Wall")]
        [SerializeField] private Transform _wallCheck;
        [SerializeField] private float _wallCheckRadius = 0.15f;
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private float _wallStickTime = 0.5f;
        [SerializeField] private float _wallSlideSpeed = 3f;
        [SerializeField] private float _wallCheckXPos;

        [Header("WallJump")]
        [SerializeField] private float _wallJumpHorizontalForce = 10f;
        [SerializeField] private float _wallJumpVerticalForce = 12f;
        [SerializeField] private float _wallJumpControlLockTime = 0.2f;

        [Header("Crouch")]
        [SerializeField] private float _colliderShirnkOffset;
        [SerializeField] private float _colliderShirnkSize;
        private Vector2 _originalColliderSize;
        private Vector2 _originalColliderOffset;

        private bool _isGrounded;
        private bool _isTouchingWall;
        private bool _isWallSliding;
        private NetworkVariable<bool> _isCrounching = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private bool _isCrouching;
        private bool _isDroppingFromWall;

        private float _stickTimer;

        private int _wallDirection;
        private float _wallJumpControlTimer;
        private bool _isGameplayInputBlocked;
        private bool _waitForJumpRelease;
        private bool _hasHorizontalMovementOverride;
        private float _horizontalMovementOverrideSpeed;
        private InputActionMap _playerActions;

        public bool IsCrouching => _isCrouching;

        public bool CanBeginWallRestrictedAbility()
        {
            return !_boxCollider2D.IsTouchingLayers(_wallLayer)
                && _wallJumpControlTimer <= 0f;
        }

        public Vector2 ReadMovementDirection()
        {
            float horizontal = _moveAction.action.ReadValue<Vector2>().x;
            if (Mathf.Abs(horizontal) <= 0.01f)
            {
                horizontal = _spriteRenderer.flipX ? -1f : 1f;
            }

            return horizontal < 0f ? Vector2.left : Vector2.right;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _playerState = GetComponent<NetworkPlayerState>();
            _playerAnimator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _boxCollider2D = GetComponent<BoxCollider2D>();

            _originalColliderSize = _boxCollider2D.size;
            _originalColliderOffset = _boxCollider2D.offset;
        }

        public override void OnNetworkSpawn()
        {
            _isCrounching.OnValueChanged += OnCrouchChanged;
            OnCrouchChanged(false, _isCrounching.Value);
            if (!IsOwner)
                return;

            if (!ConfigureInput())
            {
                enabled = false;
                return;
            }

            _playerActions.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                _isCrounching.OnValueChanged -= OnCrouchChanged;
                OnCrouchChanged(false, false);
                return;
            }

            _isGameplayInputBlocked = false;
            _waitForJumpRelease = false;
            _hasHorizontalMovementOverride = false;
            ResetCrouch();
            _playerActions?.Disable();
            _isCrounching.OnValueChanged -= OnCrouchChanged;
            OnCrouchChanged(false, false);
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            if (_isGameplayInputBlocked)
            {
                ResetCrouch();
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
                return;
            }

            if (_playerState != null && !_playerState.CanAct)
            {
                ResetCrouch();
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            if (_wallJumpControlTimer > 0f)
            {
                _wallJumpControlTimer -= Time.deltaTime;
            }

            CheckGround();
            CheckWall();
            if (_hasHorizontalMovementOverride)
            {
                ResetCrouch();
                _rigidbody.linearVelocity = new Vector2(
                    _horizontalMovementOverrideSpeed, _rigidbody.linearVelocity.y);
                _playerAnimator.SetFloat("xVelocity", Mathf.Abs(_horizontalMovementOverrideSpeed));
                _playerAnimator.SetFloat("yVelocity", _rigidbody.linearVelocity.y);
                _spriteRenderer.flipX = _horizontalMovementOverrideSpeed < 0f;
                return;
            }

            UpdateCrouch();
            HandleMovement();
            HandleWallSlide();
            HandleJump();


        }

        public void SetGameplayInputBlocked(bool isBlocked)
        {
            if (!IsOwner)
            {
                return;
            }

            _isGameplayInputBlocked = isBlocked;
            if (isBlocked)
                _waitForJumpRelease = true;

            if (isBlocked && _rigidbody != null)
            {
                ResetCrouch();
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
            }
        }

        public void BeginHorizontalMovementOverride(float speed)
        {
            if (!IsOwner)
            {
                return;
            }

            _horizontalMovementOverrideSpeed = speed;
            _hasHorizontalMovementOverride = true;
            ResetCrouch();
        }

        public void EndHorizontalMovementOverride()
        {
            if (IsOwner)
            {
                _hasHorizontalMovementOverride = false;
            }
        }

        private void HandleMovement()
        {
            if (_wallJumpControlTimer > 0f) return;

            Vector2 input = _moveAction.action.ReadValue<Vector2>();
            if (_isCrouching)
            {
                input.x = 0f;
            }

            _rigidbody.linearVelocity = new Vector2(input.x * _moveSpeed, _rigidbody.linearVelocity.y);

            _playerAnimator.SetFloat("xVelocity", Math.Abs(_rigidbody.linearVelocity.x));
            _playerAnimator.SetFloat("yVelocity", _rigidbody.linearVelocity.y);

            _wallDirection = (int)input.x;

            _wallCheck.position = new Vector2(transform.position.x + 0.25f * Mathf.Sign(input.x), transform.position.y);

            _spriteRenderer.flipX = Mathf.Sign(input.x) > 0.01f ? false : true;

        }

        private void HandleJump()
        {
            if (_waitForJumpRelease)
            {
                if (!_jumpAction.action.IsPressed())
                    _waitForJumpRelease = false;
                return;
            }

            if (!_isCrouching && _jumpAction.action.WasPressedThisFrame())
            {
                _jumpBufferCounter = _jumpBufferTime;
            }


            if (_jumpAction.action.WasReleasedThisFrame())
            {
                _jumpBufferCounter = 0;

                if (_rigidbody.linearVelocity.y > 0f)
                {
                    _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, 1);

                }

            }


            if (_jumpBufferCounter > 0f)
            {

                _jumpBufferCounter -= Time.deltaTime;

                if (_isWallSliding)
                {
                    float jumpDirection = -_wallDirection;

                    _rigidbody.linearVelocity = new Vector2(jumpDirection * _wallJumpHorizontalForce, _wallJumpVerticalForce);
                    _playerAnimator.SetBool("isJumping", true);

                    _wallJumpControlTimer = _wallJumpControlLockTime;

                    _stickTimer = 0f;
                    _jumpBufferCounter = 0f;

                    return;
                }

                if (_coyoteCounter > 0f)
                {
                    _coyoteCounter = 0f;
                    _jumpBufferCounter = 0f;

                    _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, _jumpForce);

                    _playerAnimator.SetBool("isJumping", true);
                    _isGrounded = false;
                    return;
                }

            }

        }

        private void CheckGround()
        {
            if (_boxCollider2D == null || _groundCheck == null)
            {
                _isGrounded = false;
                return;
            }

            Bounds bounds = _boxCollider2D.bounds;
            Vector2 probeOrigin = new Vector2(
                _groundCheck.position.x,
                bounds.min.y + Physics2D.defaultContactOffset);
            Vector2 probeSize = new Vector2(
                Mathf.Max(0.05f, bounds.size.x * 0.75f),
                Physics2D.defaultContactOffset * 2f);

            RaycastHit2D hit = Physics2D.BoxCast(
                probeOrigin,
                probeSize,
                0f,
                Vector2.down,
                _groundCheckDistance,
                _groundLayer);

            bool isFallingOrStill = _rigidbody.linearVelocity.y <= 0.05f;
            bool foundFloor = hit.collider != null && hit.normal.y >= 0.5f;
            _isGrounded = isFallingOrStill && foundFloor;

            if (_isGrounded)
            {
                _playerAnimator.SetBool("isJumping", false);
                _coyoteCounter = _coyoteTimer;
            }
            else
            {
                if (_coyoteCounter > 0)
                {
                    _coyoteCounter -= Time.deltaTime;
                }
            }
        }

        private void CheckWall()
        {
            if (_isGrounded)
            {
                _isWallSliding = false;
                _stickTimer = _wallStickTime;
                return;
            }

            _isTouchingWall = Physics2D.OverlapCircle(_wallCheck.position, _wallCheckRadius, _groundLayer);

        }

        private bool IsPressingAgainstWall()
        {
            Vector2 input = _moveAction.action.ReadValue<Vector2>();

            if (_wallDirection != 0)
            {

                return (_wallDirection * input.x) > 0;

            }

            return false;
        }

        private bool ConfigureInput()
        {
            InputAction move = _moveAction != null ? _moveAction.action : null;
            InputAction jump = _jumpAction != null ? _jumpAction.action : null;
            InputAction crouch = _crouchAction != null ? _crouchAction.action : null;
            InputAction aimFire = _aimFireAction != null ? _aimFireAction.action : null;
            InputAction ability = _abilityAction != null ? _abilityAction.action : null;
            if (move == null || jump == null || crouch == null || aimFire == null || ability == null
                || move.actionMap != jump.actionMap
                || move.actionMap != crouch.actionMap
                || move.actionMap != aimFire.actionMap
                || move.actionMap != ability.actionMap)
            {
                Debug.LogError("Configure Move, Jump, Crouch, AimFire e Ability do mesmo Action Map no PlayerMove.", this);
                return false;
            }

            _playerActions = move.actionMap;
            new InputBindingService(_playerActions.asset).Load();
            return true;
        }

        private void UpdateCrouch()
        {
            bool wantsToCrouch = _crouchAction.action.IsPressed();
            bool isAimingOrUsingAbility = _aimFireAction.action.IsPressed()
                || _abilityAction.action.IsPressed();
            _isCrouching = wantsToCrouch && _isGrounded && !isAimingOrUsingAbility;
            SyncCrouch();

            if (wantsToCrouch && !_isGrounded && _isTouchingWall)
            {
                _isDroppingFromWall = true;
                _isWallSliding = false;
                _stickTimer = 0f;
            }
            else if (!wantsToCrouch || _isGrounded)
            {
                _isDroppingFromWall = false;
            }
        }

        private void ResetCrouch()
        {
            _isCrouching = false;
            _isDroppingFromWall = false;
            SyncCrouch();
        }

        private void SyncCrouch()
        {
            if (_isCrounching.Value != _isCrouching)
                _isCrounching.Value = _isCrouching;
        }

        private void HandleWallSlide()
        {
            if (_wallJumpControlTimer > 0f) return;

            if (_isDroppingFromWall)
            {
                _isWallSliding = false;
                _stickTimer = 0f;
                return;
            }

            if (_isGrounded)
            {
                _isWallSliding = false;
                _stickTimer = 0f;
                return;
            }

            if (!_isTouchingWall || !IsPressingAgainstWall())
            {
                _isWallSliding = false;
                _stickTimer = 0f;
                return;
            }

            _isWallSliding = true;

            _stickTimer += Time.deltaTime;

            if (_stickTimer < _wallStickTime)
            {
                _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, 0f);
            }
            else
            {
                _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, -_wallSlideSpeed);
            }
        }

        private void OnCrouchChanged(bool previousValue,bool newValue)
        {
            if(newValue)
            {
                _boxCollider2D.size = new Vector2(
                _originalColliderSize.x,
                _originalColliderSize.y * _colliderShirnkSize
                );

                _boxCollider2D.offset = new Vector2(
                    _originalColliderOffset.x,
                    _colliderShirnkOffset
                );
            }
            else
            {
                _boxCollider2D.size = _originalColliderSize;
                _boxCollider2D.offset = _originalColliderOffset;
            }
        }
    }
}

