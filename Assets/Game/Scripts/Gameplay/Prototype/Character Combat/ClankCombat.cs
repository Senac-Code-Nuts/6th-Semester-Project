using System.Collections;
using PiGame.Gameplay;
using Unity.Netcode;
using UnityEngine;

public class ClankCombat : NetworkBehaviour, ICharacterCombat
{
    [Header("Projectile Settings")]
    [SerializeField, Tooltip("Definição do projétil disparado pelo jogador.")]
    private ProjectileDefinition _projectile;

    [SerializeField, Min(1), Tooltip("Quantidade máxima de munição de projéteis.")]
    private int _maximumAmmo = 4;

    [SerializeField, Min(0f), Tooltip("Tempo de espera em segundos entre cada disparo.")]
    private float _shotCooldownSeconds = 0.35f;

    [SerializeField, Min(0f), Tooltip("Distância à frente do jogador onde o projétil é instanciado.")]
    private float _projectileSpawnDistance = 0.5f;

    [SerializeField, Tooltip("Indicadores visuais da munição na interface ou sobre o jogador.")]
    private SpriteRenderer[] _ammoIndicators;

    [SerializeField, Min(0.1f), Tooltip("Tempo agachado necessário para recarregar uma munição.")]
    private float _crouchReloadInterval = 0.6f;

    [Header("Special Settings")]
    [SerializeField, Min(1), Tooltip("Quantidade máxima de cargas da habilidade especial.")]
    private int _maxAbilityCharges = 2;

    [SerializeField, Min(0.1f), Tooltip("Tempo em segundos para recarregar uma carga da habilidade especial.")]
    private float _abilityCooldownSeconds = 3.5f;

    [SerializeField, Min(1f), Tooltip("Força do impulso aplicado ao próprio jogador ao usar o especial.")]
    private float _hammerLaunchForce = 14f;

    [SerializeField, Min(0.1f), Tooltip("Raio da área circular para detectar e empurrar jogadores próximos.")]
    private float _knockbackRadius = 2.5f;

    [SerializeField, Min(1f), Tooltip("Força de repulsão aplicada aos jogadores atingidos.")]
    private float _knockbackForce = 8f;

    [SerializeField, Min(0.01f), Tooltip("Distância do Raycast para verificar se o jogador está em contato com o chão.")]
    private float _groundCheckDistance = 0.2f;

    [SerializeField, Tooltip("Layer de colisão utilizada para identificar outros jogadores no alcance.")]
    private LayerMask _playerLayer;

    [SerializeField, Tooltip("Layer de colisão utilizada para identificar plataformas e o chão.")]
    private LayerMask _groundLayer;

    [Header("Gizmo Settings")]
    [SerializeField, Tooltip("Cor do contorno do Gizmo do raio de repulsão no Editor.")]
    private Color _knockbackWireColor = Color.red;

    private readonly NetworkVariable<int> _ammo = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _abilityCharges = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkPlayerState _playerState;
    private PlayerMove _playerMove;
    private Rigidbody2D _rigidbody;
    private Coroutine _knockbackDisableRoutine;

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
            indicator.gameObject.SetActive(true);
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

        Collider2D[] playerHits = Physics2D.OverlapCircleAll(transform.position, _knockbackRadius, _playerLayer);

        bool hitOtherPlayer = false;
        foreach (Collider2D col in playerHits)
        {
            NetworkPlayerState targetState = col.GetComponent<NetworkPlayerState>();
            if (targetState != null && targetState != _playerState && targetState.CanAct)
            {
                hitOtherPlayer = true;
                break;
            }
        }

        bool isGrounded = IsGroundedRaycast();

        if (!hitOtherPlayer && !isGrounded)
        {
            return;
        }

        _abilityCharges.Value--;

        Vector2 launchVelocity = Vector2.up * _hammerLaunchForce;

        ApplyImpulseOwnerRpc(launchVelocity);

        foreach (Collider2D hitCollider in playerHits)
        {
            NetworkPlayerState targetState = hitCollider.GetComponent<NetworkPlayerState>();
            if (targetState != null && targetState != _playerState && targetState.CanAct)
            {
                Vector2 knockbackDir = ((Vector2)targetState.transform.position - (Vector2)transform.position).normalized;
                if (knockbackDir.sqrMagnitude < 0.01f)
                {
                    knockbackDir = Vector2.right;
                }

                ClankCombat targetCombat = targetState.GetComponent<ClankCombat>();
                if (targetCombat != null)
                {
                    targetCombat.ApplyKnockbackOwnerRpc(knockbackDir * _knockbackForce);
                }
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

    private bool IsGroundedRaycast()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            return false;
        }

        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, _groundCheckDistance, _groundLayer);
        return hit.collider != null;
    }

    [Rpc(SendTo.Owner)]
    private void ApplyImpulseOwnerRpc(Vector2 velocity)
    {
        ApplyKnockbackImpulse(velocity, 0.15f);
    }

    [Rpc(SendTo.Owner)]
    private void ApplyKnockbackOwnerRpc(Vector2 velocity)
    {
        ApplyKnockbackImpulse(velocity, 0.25f);
    }

    private void ApplyKnockbackImpulse(Vector2 velocity, float disableDuration)
    {
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = velocity;
        }

        if (_playerMove != null)
        {
            if (_knockbackDisableRoutine != null)
            {
                StopCoroutine(_knockbackDisableRoutine);
            }
            _knockbackDisableRoutine = StartCoroutine(DisableMovementTemporarilyRoutine(disableDuration));
        }
    }

    private IEnumerator DisableMovementTemporarilyRoutine(float duration)
    {
        _playerMove.enabled = false;
        yield return new WaitForSeconds(duration);
        _playerMove.enabled = true;
        _knockbackDisableRoutine = null;
    }

    private void HandleAmmoChanged(int previousValue, int currentValue)
    {
        RefreshAmmoIndicators();
    }

    private void RefreshAmmoIndicators()
    {
        for (int index = 0; index < _ammoIndicators.Length; index++)
        {
            _ammoIndicators[index].enabled = _playerState != null && _playerState.CanAct
                && index < _ammo.Value;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _knockbackWireColor;
        Gizmos.DrawWireSphere(transform.position, _knockbackRadius);

        Collider2D col = GetComponent<Collider2D>();
        Vector3 origin = col != null ? new Vector3(col.bounds.center.x, col.bounds.min.y, 0f) : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + Vector3.down * _groundCheckDistance);
    }
}