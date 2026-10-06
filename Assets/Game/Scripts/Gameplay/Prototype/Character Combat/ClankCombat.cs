using PiGame.Gameplay;
using Unity.Netcode;
using UnityEngine;

public class ClankCombat : NetworkBehaviour, ICharacterCombat
{
    [Header("Projectile Settings")]
    [SerializeField] private ProjectileDefinition _clankProjectile; 
    [SerializeField, Min(1)] private int _maxAmmo = 4;
    [SerializeField] private float _timeToRecharge = 0.6f;

    private int _currentAmmo;
    private PlayerMove _playerMove;
    private void Awake()
    {
        _currentAmmo = _maxAmmo;
        _playerMove = GetComponent<PlayerMove>();
    }

    private void Update()
    {
        if (_playerMove.IsCrouching)
        {
            RechargeBullet(_timeToRecharge);
        }
    }

    void RechargeBullet(float time)
    {
        float claudio = time;
        claudio -= Time.deltaTime;
        if (claudio < 0) 
        {
            Debug.Log("Carlos");
        }
    }

    public void BeginAbilityServer(Vector2 aimDirection, Vector2 moveDirection)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateShootAimServer(Vector2 direction)
    {
        throw new System.NotImplementedException();
    }

    public void ShootReleaseServer()
    {
        throw new System.NotImplementedException();
    }

    public void ShootServer(Vector2 direction)
    {
        throw new System.NotImplementedException();
    }

    public void EndAbilityServer(Vector2 aimDirection, Vector2 moveDirection)
    {
        throw new System.NotImplementedException();
    }
}
