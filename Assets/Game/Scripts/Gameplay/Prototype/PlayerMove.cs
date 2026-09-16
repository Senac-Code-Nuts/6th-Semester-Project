using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System;

namespace PiGame.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : NetworkBehaviour, IGameplayInputBlocker
    {
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _jumpForce = 12f;

        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;

        private Rigidbody2D _rigidbody;
        private NetworkPlayerState _playerState;

        private Animator _playerAnimator;
        private SpriteRenderer _spriteRenderer;
        [Header("Ground")]
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;

        [Header("Wall")]
        [SerializeField] private Transform _wallCheck;
        [SerializeField] private float _wallCheckDistance = 0.15f;
        [SerializeField] private LayerMask _wallLayer;

        [SerializeField] private float _wallStickTime = 0.5f;
        [SerializeField] private float _wallSlideSpeed = 3f;

        [Header("WallJump")]
        [SerializeField] private float _wallJumpHorizontalForce = 10f;
        [SerializeField] private float _wallJumpVerticalForce = 12f;
        [SerializeField] private float _wallJumpControlLockTime = 0.2f;

        private bool _isGrounded;
        private bool _isTouchingWall;
        private bool _isWallSliding;

        private float _stickTimer;

        private int _wallDirection;
        private float _wallJumpControlTimer ;
        private bool _isGameplayInputBlocked;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _playerState = GetComponent<NetworkPlayerState>();
            _playerAnimator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public override void OnNetworkSpawn()
        {
            if(!IsOwner)
                return;
            
            _moveAction.action.Enable();
            _jumpAction.action.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if(!IsOwner)
                return;
            _isGameplayInputBlocked = false;
            _moveAction.action.Disable();
            _jumpAction.action.Disable();
        }

        private void Update()
        {
            if(!IsOwner)
                return;

            if (_isGameplayInputBlocked)
            {
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
                return;
            }

            if(_playerState != null && !_playerState.CanAct)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            CheckGround();
            CheckWall();
            
            HandleMovement();
            HandleWallSlide();
            HandleJump();

            if (_wallJumpControlTimer  > 0f)
            {
                _wallJumpControlTimer  -= Time.deltaTime;
            }
        }

        public void SetGameplayInputBlocked(bool isBlocked)
        {
            if (!IsOwner)
            {
                return;
            }

            _isGameplayInputBlocked = isBlocked;
            if (isBlocked && _rigidbody != null)
            {
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
            }
        }

        private void HandleMovement()
        {
            if (_wallJumpControlTimer  > 0f)
                return;
            Vector2 input = _moveAction.action.ReadValue<Vector2>();
            _rigidbody.linearVelocity = new Vector2(input.x * _moveSpeed, _rigidbody.linearVelocity.y);
            _playerAnimator.SetFloat("xVelocity",Math.Abs(_rigidbody.linearVelocity.x));
            _playerAnimator.SetFloat("yVelocity",_rigidbody.linearVelocity.y);
            _spriteRenderer.flipX = _rigidbody.linearVelocity.x < 0 ? false : true; 
        }

        private void HandleJump()
        {
            if(!_jumpAction.action.WasPressedThisFrame())
                return;
            
            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, _jumpForce);
            _playerAnimator.SetBool("isJumping", true);
            if(_isWallSliding)
            {
                float jumpDirection = -_wallDirection;

                _rigidbody.linearVelocity = new Vector2(jumpDirection * _wallJumpHorizontalForce, _wallJumpVerticalForce);

                _wallJumpControlTimer  = _wallJumpControlLockTime;

                _isWallSliding = false;
                _stickTimer = 0f;

                return;
            }
            if(_isGrounded)
            {
                _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, _jumpForce);
            }
        }

        private void CheckGround()
        {
            RaycastHit2D hit = Physics2D.Raycast(_groundCheck.position, Vector2.down, _groundCheckDistance, _groundLayer);

            _isGrounded = hit.collider != null;
        }

        private void CheckWall()
        {
            RaycastHit2D rightHit = Physics2D.Raycast(_wallCheck.position, Vector2.right, _wallCheckDistance, _wallLayer);
            RaycastHit2D leftHit = Physics2D.Raycast(_wallCheck.position, Vector2.left, _wallCheckDistance, _wallLayer);

            if(rightHit.collider != null)
            {
                _isTouchingWall = true;
                _wallDirection = 1;
            }
            else if(leftHit.collider != null)
            {
                _isTouchingWall = true;
                _wallDirection = -1;
            }
            else
            {
                _isTouchingWall = false;
                _wallDirection = 0;
            }
        }

        private bool IsPressingAgainstWall()
        {
            Vector2 input = _moveAction.action.ReadValue<Vector2>();

            if(_wallDirection == 1)
            {
                return input.x > 0;
            }

            if(_wallDirection == -1)
            {
                return input.x < 0;
            }

            return false;
        }

        private void HandleWallSlide()
        {
            if(_isGrounded)
            {
                _isWallSliding = false;
                _stickTimer = 0f;
                return;
            }

            if(!_isTouchingWall || !IsPressingAgainstWall())
            {
                _isWallSliding = false;
                _stickTimer = 0f;
                return;
            }

            _isWallSliding = true;

            _stickTimer += Time.deltaTime;

            if(_stickTimer < _wallStickTime)
            {
                _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, 0f);
            }
            else
            {
               _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, -_wallSlideSpeed); 
            }
        }
    }
}

