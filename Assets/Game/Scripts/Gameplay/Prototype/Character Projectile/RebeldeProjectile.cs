using UnityEngine;
using Unity.Netcode;
using PiGame.Sound;

namespace PiGame.Gameplay 
{

    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider2D))]
    public class RebeldeProjectile : NetworkProjectile
    {
        [Header("Physical Settings")]
        [SerializeField] private float _wallBounceSpeedMultiplier = 0.75f;
        [SerializeField] private float _playerBounceSpeedMultiplier = 0.35f;
        [SerializeField] private float _maximumSpeed = 30f;
        [SerializeField] private float _minimumSpeed = 0.5f;

        [Header("Physics")]
        [SerializeField] private float _gravityScale = 1f;

        [Header("References")]
        [SerializeField] private Collider2D _solidCollider;

        private Rigidbody2D _rigidbody;
        private RebeldeSoundManager _ownerSound;

        private bool _isStopped;
        private bool _isFalling;

        public float MinimumSpeed => _minimumSpeed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            _useLifetime = false;
        }
         public void InitializeServer(
            ulong shooterClientId,
            Vector2 direction,
            ProjectileDefinition definition,
            float speed,
            RebeldeSoundManager ownerSound)
        {
            if (!IsServer || !IsSpawned)
                return;

            _shooterClientId = shooterClientId;
            _damage = definition.Damage;

            _direction = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;

            _speed = Mathf.Clamp(speed, 0f, _maximumSpeed);

            _ownerSound = ownerSound;
            _isStopped = false;
            _isFalling = false;

            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = _gravityScale;
            _rigidbody.linearVelocity = _direction * _speed;
            _rigidbody.angularVelocity = 0f;

            if (_solidCollider != null)
                _solidCollider.enabled = true;
        }

        // Impede a classe NetworkProjectile de mover o Transform
        // por conta própria. O Rigidbody2D controla o movimento.
        protected override void Update()
        {
        }

        private void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || _isStopped)
                return;

            Vector2 velocity = _rigidbody.linearVelocity;
            float speed = velocity.magnitude;

            if (speed > _maximumSpeed)
            {
                velocity = velocity.normalized * _maximumSpeed;
                _rigidbody.linearVelocity = velocity;
                speed = _maximumSpeed;
            }

            // Mantém os campos herdados coerentes com a física atual.
            _speed = speed;
            _direction = velocity.normalized;
        }
        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !IsSpawned)
                return;


            NetworkPlayerState playerState =
                other.GetComponentInParent<NetworkPlayerState>();

            if (playerState == null)
                return;

            // Apenas o dono recupera o projétil.
            if (playerState.OwnerClientId != _shooterClientId)
                return;

            RebeldeCombat combat =
                playerState.GetComponent<RebeldeCombat>();

            if (combat == null)
                return;

            combat.AddShotServer();
            NetworkObject.Despawn();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsServer || !IsSpawned || _isStopped)
                return;

            if (collision.collider.GetComponentInParent<NetworkProjectile>() != null)
                return;

            NetworkPlayerState player =
                collision.collider.GetComponentInParent<NetworkPlayerState>();

            if (player != null)
            {
                HandlePlayerCollision(player);
                return;
            }

            HandleSurfaceCollision();
        }
        private void HandleSurfaceCollision()
        {
            // Se já está caindo, o primeiro impacto com uma
            // superfície encerra o movimento.
            if (_isFalling)
            {
                StopProjectile();
                return;
            }

            ApplySpeedMultiplier(_wallBounceSpeedMultiplier);

            if (_isStopped)
                return;

            PlayBounceSound();

            // Só avalia a velocidade depois do impacto.
            if (_rigidbody.linearVelocity.magnitude <= _minimumSpeed)
                StartFalling();
        }
        private void HandlePlayerCollision(NetworkPlayerState player)
        {
            if (_isFalling)
                    return;

                if (player.OwnerClientId == _shooterClientId)
                    return;

                if (!player.IsInvulnerable)
                {
                    player.ApplyDamageServer(_damage, _shooterClientId);
                }

                ApplySpeedMultiplier(_playerBounceSpeedMultiplier);

                if (_isStopped)
                    return;

                PlayBounceSound();

                if (_rigidbody.linearVelocity.magnitude <= _minimumSpeed)
                    StartFalling();
        }
        private void ApplySpeedMultiplier(float multiplier)
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            velocity *= Mathf.Clamp01(multiplier);

            if (velocity.magnitude > _maximumSpeed)
                velocity = velocity.normalized * _maximumSpeed;

            _rigidbody.linearVelocity = velocity;
            _speed = velocity.magnitude;

            if (velocity.sqrMagnitude > 0f)
                _direction = velocity.normalized;
        }

        public override void ReflectServer(float minimumSpeed)
        {
            if (!IsServer || !IsSpawned)
                return;

            // Guarda a velocidade antes de atualizar os campos herdados.
            Vector2 velocity = _rigidbody.linearVelocity;

            base.ReflectServer(minimumSpeed);

            _isStopped = false;
            _isFalling = false;

            _rigidbody.gravityScale = _gravityScale;

            // A habilidade inverte a velocidade atual.
            // Se estiver parado ou lento demais, aplica a velocidade mínima.
            Vector2 reflectedVelocity = -velocity;

            if (reflectedVelocity.magnitude < minimumSpeed)
            {
                Vector2 direction = _direction.sqrMagnitude > 0f
                    ? _direction.normalized
                    : Vector2.right;

                reflectedVelocity = direction * minimumSpeed;
            }

            if (reflectedVelocity.magnitude > _maximumSpeed)
                reflectedVelocity = reflectedVelocity.normalized * _maximumSpeed;

            _rigidbody.linearVelocity = reflectedVelocity;
            _speed = reflectedVelocity.magnitude;
            _direction = reflectedVelocity.normalized;

            if (_solidCollider != null)
                _solidCollider.enabled = true;
        }

        private void StartFalling()
        {
            if (_isFalling || _isStopped)
                return;

            _isFalling = true;

            // Mantém a velocidade atual e deixa a gravidade
            // conduzir a bola até a próxima superfície.
            _rigidbody.gravityScale = _gravityScale;
        }

        private void StopProjectile()
        {
            _isFalling = false;
            _isStopped = true;

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.gravityScale = 0f;

            _speed = 0f;
        }


    private void PlayBounceSound()
    {
        if (_ownerSound != null)
            _ownerSound.PlayRebeldeBounce();
    }
    }
    
}
