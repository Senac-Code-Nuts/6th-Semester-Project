using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System;

namespace PiGame.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _jumpForce = 12f;

        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;

        private Rigidbody2D _rigidbody;
        private NetworkPlayerState _playerState;

        private Animator _playerAnimator;
        private SpriteRenderer _spriteRenderer;

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
            _moveAction.action.Disable();
            _jumpAction.action.Disable();
        }

        private void Update()
        {
            if(!IsOwner)
                return;

            if(_playerState != null && !_playerState.CanAct)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }
            
            HandleMovement();
            HandleJump();
        }

        private void HandleMovement()
        {
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
        }
    }
}

