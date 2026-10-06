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
                rebeldeProjectile.InitializeServer(OwnerClientId, shotDirection, _projectile, 1,_projectile.Speed);
                _hasShot.Value = false;
            }
        }
        public void AddShotServer()
        {
            if (!IsServer)
                return;

            _hasShot.Value = true;
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
    }

}
