using PiGame.Gameplay;
using Unity.Netcode;
using UnityEngine;

public class ClankCombat : NetworkBehaviour, ICharacterCombat
{
    [Header("Projectile Settings")]
    [SerializeField] private ProjectileDefinition _projectile;
    [SerializeField, Min(1)] private int _maximumAmmo = 4;
    [SerializeField, Min(0f)] private float _shotCooldownSeconds = 0.35f;
    [SerializeField, Min(0f)] private float _projectileSpawnDistance = 0.5f;
    [SerializeField] private SpriteRenderer[] _ammoIndicators;

    [SerializeField, Min(0.1f)] private float _crouchReloadInterval = 0.6f;

    [Header("Special Settings")]
    [SerializeField, Min(1)] private int _maxAbilityCharges = 2;
    [SerializeField, Min(0.1f)] private float _abilityCooldownSeconds = 3.5f;
    [SerializeField, Min(1f)] private float _hammerLaunchForce = 14f;
    [SerializeField, Min(0.1f)] private float _knockbackRadius = 2.5f;
    [SerializeField, Min(1f)] private float _knockbackForce = 8f;
    [SerializeField] private LayerMask _playerLayer;

    [Header("Gizmo Settings")]
    [SerializeField] private Color _knockbackFillColor = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField] private Color _knockbackWireColor = Color.red;
    [SerializeField] private Color _projectileSpawnGizmoColor = Color.yellow;

    private readonly NetworkVariable<int> _ammo = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Owner);
    private readonly NetworkVariable<int> _abilityCharges = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Owner);

    private NetworkPlayerState _playerState;
    private PlayerMove _playerMove;
    private Rigidbody2D _rigidbody;

    private float _nextShotAt;
    private float _crouchTimer;
    private float _abilityCooldownTimer;

    private void Awake()
    {
        _playerState = GetComponent<NetworkPlayerState>();
        _playerMove = GetComponent<PlayerMove>();
        _rigidbody = GetComponent<Rigidbody2D>();

        if (_projectile == null || _projectile.Prefab == null
            || _ammoIndicators == null || _ammoIndicators.Length != _maximumAmmo)
        {
            Debug.LogError("Configure o projétil e os indicadores de munição no ClankCombat.", this);
            enabled = false;
            return;
        }

        foreach (SpriteRenderer indicator in _ammoIndicators)
        {
            if (indicator == null)
            {
                Debug.LogError("Todos os indicadores de munição devem estar atribuídos no ClankCombat.", this);
                enabled = false;
                return;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!enabled)
        {
            return;
        }

        _ammo.OnValueChanged += HandleAmmoChanged;
        _playerState.StateChanged += RefreshAmmoIndicators;

        if (IsServer)
        {
            _ammo.Value = _maximumAmmo;
            _abilityCharges.Value = _maxAbilityCharges;
        }

        foreach (SpriteRenderer indicator in _ammoIndicators)
        {
            indicator.gameObject.SetActive(IsOwner);
        }

        RefreshAmmoIndicators();
    }

    public override void OnNetworkDespawn()
    {
        _ammo.OnValueChanged -= HandleAmmoChanged;
        _playerState.StateChanged -= RefreshAmmoIndicators;
    }

    private void Update()
    {
        if (!IsServer || !enabled)
        {
            return;
        }

        if (!_playerState.CanAct)
        {
            _crouchTimer = 0f;
            return;
        }

        UpdateCrouchReloadServer();
        UpdateAbilityCooldownServer();
    }

    private void UpdateCrouchReloadServer()
    {
        if (_playerMove.IsCrouching && _ammo.Value < _maximumAmmo)
        {
            _crouchTimer += Time.deltaTime;
            if (_crouchTimer >= _crouchReloadInterval)
            {
                _ammo.Value = Mathf.Min(_maximumAmmo, _ammo.Value + 1);
                _crouchTimer = 0f;
            }
        }
        else
        {
            _crouchTimer = 0f;
        }
    }

    private void UpdateAbilityCooldownServer()
    {
        if (_abilityCharges.Value < _maxAbilityCharges)
        {
            _abilityCooldownTimer += Time.deltaTime;
            if (_abilityCooldownTimer >= _abilityCooldownSeconds)
            {
                _abilityCharges.Value++;
                _abilityCooldownTimer = 0f;
            }
        }
        else
        {
            _abilityCooldownTimer = 0f;
        }
    }

    #region Metodo Do ICharacterCombat
    public void ShootServer(Vector2 direction)
    {
        if (!IsServer || !enabled || !_playerState.CanAct || _ammo.Value <= 0 || Time.time < _nextShotAt)
        {
            return;
        }

        Vector2 shotDirection = direction.sqrMagnitude > 0.01f
            ? direction.normalized
            : Vector2.right;
        Vector3 spawnPosition = transform.position + (Vector3)(shotDirection * _projectileSpawnDistance);

        NetworkProjectile projectile = Instantiate(_projectile.Prefab, spawnPosition, Quaternion.identity);
        projectile.NetworkObject.Spawn(true);
        projectile.InitializeServer(OwnerClientId, shotDirection, _playerState.IndicatorColor, _projectile);

        _ammo.Value--;
        _nextShotAt = Time.time + _shotCooldownSeconds;
    }

    public void BeginAbilityServer(Vector2 aimDirection, Vector2 moveDirection)
    {
        if (!IsServer || !enabled || !_playerState.CanAct || _abilityCharges.Value <= 0)
        {
            return;
        }

        _abilityCharges.Value--;

        Vector2 aim = aimDirection.sqrMagnitude > 0.01f ? aimDirection.normalized : Vector2.down;
        Vector2 launchVelocity = -aim * _hammerLaunchForce;

        ApplyImpulseOwnerRpc(launchVelocity);

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _knockbackRadius, _playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            NetworkPlayerState targetState = hitCollider.GetComponent<NetworkPlayerState>();
            if (targetState != null && targetState != _playerState && targetState.CanAct)
            {
                Vector2 knockbackDir = ((Vector2)targetState.transform.position - (Vector2)transform.position).normalized;
                if (knockbackDir.sqrMagnitude < 0.01f)
                {
                    knockbackDir = Vector2.right;
                }

                ApplyImpulseClientRpc(
                    knockbackDir * _knockbackForce,
                    RpcTarget.Single(targetState.OwnerClientId, RpcTargetUse.Temp));
            }
        }
    }

    public void ShootReleaseServer()
    {
    }

    public void UpdateShootAimServer(Vector2 direction)
    {
    }

    public void EndAbilityServer(Vector2 aimDirection, Vector2 moveDirection)
    {
    }
    #endregion

    [Rpc(SendTo.Owner)]
    private void ApplyImpulseOwnerRpc(Vector2 velocity)
    {
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = velocity;
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ApplyImpulseClientRpc(Vector2 velocity, RpcParams rpcParams)
    {
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = velocity;
        }
    }

    private void HandleAmmoChanged(int previousValue, int currentValue)
    {
        RefreshAmmoIndicators();
    }

    private void RefreshAmmoIndicators()
    {
        for (int index = 0; index < _ammoIndicators.Length; index++)
        {
            _ammoIndicators[index].enabled = IsOwner && _playerState.CanAct
                && index < _ammo.Value;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _knockbackFillColor;
        Gizmos.DrawSphere(transform.position, _knockbackRadius);

        Gizmos.color = _knockbackWireColor;
        Gizmos.DrawWireSphere(transform.position, _knockbackRadius);

        Gizmos.color = _projectileSpawnGizmoColor;
        Gizmos.DrawWireSphere(transform.position, _projectileSpawnDistance);
    }
}