using UnityEngine;
using Unity.Netcode;

namespace PiGame.Gameplay 
{

    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider2D))]
    public class RebeldeProjectile : NetworkProjectile
    {
        [Header("Projectile Settings")]
        [SerializeField] private float _minimumSpeed = 3f;
        [SerializeField] private float _wallBounceSpeedLoss = 0.25f;
        [SerializeField] private float _playerBounceSpeedLoss = 0.5f;
        [SerializeField] private float _fallSpeedThreshold = 3f;
        [SerializeField] private float _fallGravity = 20f;

        [Header("Collision")]
        [SerializeField] private LayerMask _collisionLayers;
        [SerializeField] private float _collisionOffset = 0.01f;

        private bool _isStopped;
        private bool _falling;
        private Vector2 _fallVelocity;

        private Collider2D _projectileCollider;

        public float MinimumSpeed => _minimumSpeed;

        private void Awake()
        {

            _projectileCollider = GetComponent<Collider2D>();

            _useLifetime = false;
        }

        public void InitializeServer(ulong shooterClientId, Vector2 direction, ProjectileDefinition definition, int stack, float speed)
        {
            if (!IsServer)
                return;

            _shooterClientId = shooterClientId;

            _damage = definition.Damage;

            _direction = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;

            _speed = speed;

            _isStopped = false;

        }

        protected override void Update()
        {
            
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !IsSpawned)
                return;

            NetworkPlayerState playerState =
                other.GetComponentInParent<NetworkPlayerState>();

            if (playerState == null)
                return;

            if (playerState.OwnerClientId != _shooterClientId)
                return;

            RebeldeCombat combat =
                playerState.GetComponent<RebeldeCombat>();

            if (combat == null)
                return;

            combat.AddShotServer();
            NetworkObject.Despawn();
        }

        private void FixedUpdate()
        {
            if(!IsServer || !IsSpawned)
                return;
            
            if(_isStopped)
                return;
            
            if(_falling)
            {
                UpdateFalling();
                return;
            }

            UpdateFlying();
        }

        private void UpdateFlying()
        {
            if(_speed <= _fallSpeedThreshold)
            {
                StartFalling();
                return;
            }

            float deltaTime = Time.fixedDeltaTime;

            Vector2 startPosition = transform.position;

            Vector2 movement =
                _direction * _speed * deltaTime;

            float distance = movement.magnitude;

            if (distance <= 0f)
                return;

            RaycastHit2D hit = Physics2D.Raycast(
                startPosition,
                movement.normalized,
                distance,
                _collisionLayers);

            if (hit.collider != null)
            {
                HandleCollision(hit);
                return;
            }

            transform.position += (Vector3)movement;
        }

        private void UpdateFalling()
        {
            _fallVelocity.y -=
                _fallGravity * Time.fixedDeltaTime;

            Vector2 movement =
                _fallVelocity * Time.fixedDeltaTime;

            float distance = movement.magnitude;

            if (distance <= 0f)
                return;

            RaycastHit2D[] hits = new RaycastHit2D[4];

            int hitCount = _projectileCollider.Cast(
                movement.normalized,
                hits,
                distance,
                true);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = hits[i];

                if (hit.collider == null)
                    continue;

                if (((1 << hit.collider.gameObject.layer) &
                    _collisionLayers) == 0)
                {
                    continue;
                }

                HandleFallCollision(hit);
                return;
            }

            transform.position += (Vector3)movement;
        }
        private void HandleFallCollision(RaycastHit2D hit)
        {
            transform.position =
                hit.point +
                hit.normal * _collisionOffset;

            _fallVelocity = Vector2.zero;

            _isStopped = true;
        }

        private void StartFalling()
        {
            _falling = true;

            _speed = 0f;

            _fallVelocity = Vector2.down * 2f;
        }


        private void HandleCollision(RaycastHit2D hit)
        {
            NetworkPlayerState player =
                hit.collider.GetComponentInParent<NetworkPlayerState>();

            if (player != null)
            {
                HandlePlayerCollision(player, hit);
                return;
            }

            HandleEnvironmentCollision(hit);
        }

        private void HandleEnvironmentCollision(RaycastHit2D hit)
        {
            transform.position =
                hit.point +
                hit.normal * _collisionOffset;

            Bounce(
                hit.normal,
                _wallBounceSpeedLoss);
        }

        private void HandlePlayerCollision(NetworkPlayerState player, RaycastHit2D hit)
        {
            if (player.OwnerClientId == _shooterClientId)
            {
                transform.position +=
                    (Vector3)(_direction * _speed * Time.fixedDeltaTime);

                return;
            }

            if (!player.IsInvulnerable)
            {
                player.ApplyDamageServer(
                    _damage,
                    _shooterClientId);
            }

            transform.position =
                hit.point +
                hit.normal * _collisionOffset;

            Bounce(
                hit.normal,
                _playerBounceSpeedLoss);    
        }

        private void Bounce(Vector2 normal, float speedLoss)
        {
            float currentSpeed =
                _speed - speedLoss;

            if (currentSpeed <= _minimumSpeed)
            {
                StartFalling();
                return;
            }

            _direction =
                Vector2.Reflect(
                    _direction,
                    normal).normalized;

            _speed = currentSpeed;
        }

        private void StopBall()
        {
            _speed = 0f;
            _isStopped = true;
        }



    }
    
}
