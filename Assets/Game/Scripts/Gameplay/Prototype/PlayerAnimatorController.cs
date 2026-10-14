using UnityEngine;

namespace PiGame.Gameplay
{
    public class PlayerAnimatorController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private bool _useAnimations = true;
        [SerializeField] private Animator _animator;

        private static readonly int XVelocity = Animator.StringToHash("xVelocity");
        private static readonly int YVelocity = Animator.StringToHash("yVelocity");
        private static readonly int IsJumping = Animator.StringToHash("isJumping");
        private static readonly int IsCrouching = Animator.StringToHash("isCrouching");
        private static readonly int IsWallSliding = Animator.StringToHash("isWallSliding");

        public Vector2 FacingDirection => _spriteRenderer.flipX ? Vector2.left : Vector2.right;

        private void Awake()
        {
            if (_spriteRenderer == null
                || (_useAnimations && (_animator == null || _animator.runtimeAnimatorController == null)))
            {
                Debug.LogError("Configure SpriteRenderer e Animator no PlayerAnimatorController. Desative Use Animations somente para prefabs sem animações.", this);
                enabled = false;
            }
        }

        public void UpdateMovement(Vector2 velocity, float facingInput,
            bool isJumping, bool isCrouching, bool isWallSliding)
        {
            if (!enabled) return;
            if (Mathf.Abs(facingInput) > 0.01f) _spriteRenderer.flipX = facingInput < 0f;
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
    }
}
