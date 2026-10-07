    using UnityEngine;
    using Unity.Netcode;


namespace PiGame.Gameplay
    {
        [RequireComponent(typeof(NetworkPlayerState))]
        
        public class RebeldeCombat : NetworkBehaviour, ICharacterCombat
        {
            [Header("Combat Settings")]
            [SerializeField] private ProjectileDefinition _projectile;
            [SerializeField, Min(0f)] private float _shotCooldownSeconds = 0.35f;
            [SerializeField, Min(0f)] private float _projectileSpawnDistance = 0.85f;

            [SerializeField] private float[] _projectileVelocityStack = {5f, 10f, 15f};

            [Header("Ability Settings")]
            [SerializeField] private float _coneDistance = 4f;
            [SerializeField] private float _coneAngle = 60f;
            [SerializeField] private float _projectileReflectMinimumSpeed = 3f;
            private float _projectileVelocity;
            private int _projectileVelocityPointer = 0;

            
            private NetworkPlayerState _playerState;
            private NetworkVariable<bool> _hasShot = new NetworkVariable<bool>(true);
            public bool HasShot => _hasShot.Value;
            private float _nextShotTime;

            public void Awake()
            {
                _playerState = GetComponent<NetworkPlayerState>();
                if (_projectile == null || _projectile.Prefab == null)
                {
                    Debug.LogError("Configure uma definição com prefab de projétil no BasicCharacterCombat.", this);
                }
                _projectileVelocity = _projectileVelocityStack[0];
            }
            public void ShootServer(Vector2 direction)
            {
                if (!IsServer || !_playerState.CanAct || _projectile == null || !_hasShot.Value
                    || _projectile.Prefab == null || Time.time < _nextShotTime)
                {
                    return;
                }

                _nextShotTime = Time.time + _shotCooldownSeconds;

                Vector2 shotDirection = direction.sqrMagnitude > 0.01f
                    ? direction.normalized
                    : Vector2.right;
                    
                Vector3 spawnPosition = transform.position
                    + (Vector3)(shotDirection * _projectileSpawnDistance);

                NetworkProjectile projectile = Instantiate(
                    _projectile.Prefab, spawnPosition, Quaternion.identity);

                projectile.NetworkObject.Spawn(true);

                RebeldeProjectile rebeldeProjectile = projectile.GetComponent<RebeldeProjectile>();
                if(rebeldeProjectile != null)
                {
                    rebeldeProjectile.InitializeServer(OwnerClientId, shotDirection, _projectile,_projectileVelocity);
                    _hasShot.Value = false;
                }
                GetNextShot();
            }
            public void AddShotServer()
            {
                if (!IsServer)
                    return;

                _hasShot.Value = true;
            }
            private void GetNextShot()
            {
                _projectileVelocityPointer++;
                if(_projectileVelocityPointer >= _projectileVelocityStack.Length)
                {
                    _projectileVelocityPointer = 0;
                }

                _projectileVelocity = _projectileVelocityStack[_projectileVelocityPointer];
            }
            public void ShootReleaseServer()
            {

            }
            public void UpdateShootAimServer(Vector2 direction)
            {

            }


            public void BeginAbilityServer(Vector2 aimDirection, Vector2 moveDirection)
            {

            }
            public void EndAbilityServer(Vector2 aimDirection, Vector2 moveDirection)
            {

            }

            private bool IsInsideAbilityCone(Vector2 targetPosition, Vector2 forward)
            {
                Vector2 toTarget = targetPosition - (Vector2)transform.position;

                float distance = toTarget.magnitude;

                if(distance > _coneDistance)
                {
                    return false;
                }

                if(distance <= 0.001f)
                {
                    return true;
                }

                float angle =
                    Vector2.Angle(
                        forward,
                        toTarget.normalized);

                return angle <= _coneAngle;
            }

            private void ReflectProjectilesInCone(Vector2 forward)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _coneDistance);

                foreach(Collider2D hit in hits)
                {
                    NetworkProjectile projectile = hit.GetComponentInParent<NetworkProjectile>();

                    if(projectile == null)
                    {
                        continue;
                    }
                    if(projectile.ShooterClientId == OwnerClientId)
                    {
                        continue;
                    }

                    if(!IsInsideAbilityCone(projectile.transform.position, forward))
                    {
                        continue;
                    }

                    projectile.ReflectServer(_projectileReflectMinimumSpeed);
                }
            }
        }

    }
