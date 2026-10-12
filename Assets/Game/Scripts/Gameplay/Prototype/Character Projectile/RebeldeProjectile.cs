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

        [Header("Referencer")]
        [SerializeField] private Collider2D _solidCollider;
        private Collider2D _projectileCollider;
        private Rigidbody2D _rigidbody;

        private bool _isStopped;
        private bool _falling;
        private Vector2 _fallVelocity;


        public float MinimumSpeed => _minimumSpeed;

        private void Awake()
        {

            _projectileCollider = GetComponent<Collider2D>();
            _rigidbody = GetComponent<Rigidbody2D>();

            _useLifetime = false;
            _solidCollider.enabled = false;

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.gravityScale = 0f;
        }

        public void InitializeServer(ulong shooterClientId, Vector2 direction, ProjectileDefinition definition, float speed)
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
            _falling = false;
            _fallVelocity = Vector2.zero;            

        }

        protected override void Update()
        {
            //Deixando Update vazio para sobrescrever a movimentação no Update da classe pai
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

        private void StartFalling()
        {
            if (_falling)
                return;

            _falling = true;
            _speed = 0f;
            _solidCollider.enabled = true;

            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = 1f;
            _rigidbody.linearVelocity = Vector2.down * 2f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsServer || !_falling || _isStopped)
                return;

           /* _isStopped = true;

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;*/
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

        public override void ReflectServer(float minimumSpeed)
        {
            base.ReflectServer(minimumSpeed);

            _falling = false;
            _fallVelocity = Vector2.zero;
            _isStopped = false;
        }

    }
    
}
