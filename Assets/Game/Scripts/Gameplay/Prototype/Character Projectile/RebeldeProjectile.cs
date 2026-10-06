using UnityEngine;

namespace PiGame.Gameplay {
    public class RebeldeProjectile : NetworkProjectile
    {
        [Header("Projectile Settings")]
        [SerializeField] private float _ballSpeed = 3f;
        private float _startSpeed;

        private Collider2D _projectileCollider;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _projectileCollider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _startSpeed = _ballSpeed;
        }





    }
    
}
