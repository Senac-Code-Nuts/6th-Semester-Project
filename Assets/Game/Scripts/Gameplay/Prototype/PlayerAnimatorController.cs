using Unity.Netcode;
using UnityEngine;

namespace PiGame.Gameplay
{
    public class PlayerAnimatorController : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private bool _useAnimations = true;
        [SerializeField] private Animator _animator;

        private static readonly int XVelocity = Animator.StringToHash("xVelocity");
        private static readonly int YVelocity = Animator.StringToHash("yVelocity");
        private static readonly int IsJumping = Animator.StringToHash("isJumping");
        private static readonly int IsCrouching = Animator.StringToHash("isCrouching");
        private static readonly int IsWallSliding = Animator.StringToHash("isWallSliding");

        private readonly NetworkVariable<bool> _facingLeft = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private float _movementFacingInput;
        private float _aimFacingInput;
        private bool _isAiming;

        private void Awake()
        {
            if (_spriteRenderer == null
                || (_useAnimations && (_animator == null || _animator.runtimeAnimatorController == null)))
            {
                Debug.LogError("Configure SpriteRenderer e Animator no PlayerAnimatorController. Desative Use Animations somente para prefabs sem animações.", this);
                enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!enabled) return;
            _facingLeft.OnValueChanged += OnFacingChanged;
            OnFacingChanged(false, _facingLeft.Value);
        }

        public override void OnNetworkDespawn()
        {
            _facingLeft.OnValueChanged -= OnFacingChanged;
            _isAiming = false;
            _movementFacingInput = 0f;
            _aimFacingInput = 0f;
        }

        public void SetAimFacing(Vector2 direction, bool isAiming)
        {
            if (!enabled || !IsSpawned || !IsOwner) return;
            _isAiming = isAiming;
            _aimFacingInput = direction.x;
            UpdateFacing();
        }

        public void UpdateMovement(Vector2 velocity, float facingInput,
            bool isJumping, bool isCrouching, bool isWallSliding)
        {
            if (!enabled || !IsSpawned || !IsOwner) return;
            _movementFacingInput = facingInput;
            UpdateFacing();
            if (!_useAnimations) return;
            _animator.SetFloat(XVelocity, Mathf.Abs(velocity.x));
            _animator.SetFloat(YVelocity, velocity.y);
            _animator.SetBool(IsJumping, isJumping);
            _animator.SetBool(IsCrouching, isCrouching);
            _animator.SetBool(IsWallSliding, isWallSliding);
        }

        public void SetCrouching(bool isCrouching)
        {
            if (enabled && _useAnimations)
                _animator.SetBool(IsCrouching, isCrouching);
        }

        private void UpdateFacing()
        {
            float direction = _isAiming ? _aimFacingInput : _movementFacingInput;
            // Mira vertical e personagem parado mantêm o último lado visível.
            if (Mathf.Abs(direction) <= 0.01f) return;
            bool facingLeft = direction < 0f;
            if (_facingLeft.Value != facingLeft) _facingLeft.Value = facingLeft;
        }

        private void OnFacingChanged(bool previousValue, bool facingLeft)
        {
            _spriteRenderer.flipX = facingLeft;
        }
    }
}
