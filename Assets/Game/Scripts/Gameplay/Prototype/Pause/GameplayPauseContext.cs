using System.Collections.Generic;
using System.Threading.Tasks;
using PiGame.UI;
using UnityEngine;

namespace PiGame.Gameplay
{
    public class GameplayPauseContext : MonoBehaviour, IPauseContext
    {
        [SerializeField] private MatchController _matchController;
        [SerializeField] private MonoBehaviour _disconnectServiceSource;

        private readonly List<IGameplayInputBlocker> _inputBlockers =
            new List<IGameplayInputBlocker>();
        private IMatchDisconnectService _disconnectService;
        private bool _isInputBlocked;

        public bool CanOpenPause =>
            _matchController == null || _matchController.Phase == MatchPhase.Playing;

        private void Awake()
        {
            _matchController ??= FindFirstObjectByType<MatchController>();
            _disconnectService = _disconnectServiceSource as IMatchDisconnectService;

            if (_disconnectService == null)
            {
                foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
                {
                    if (behaviour is IMatchDisconnectService disconnectService)
                    {
                        _disconnectService = disconnectService;
                        _disconnectServiceSource = behaviour;
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            SetInputBlocked(false);
        }

        public void EnterPause()
        {
            SetInputBlocked(true);
        }

        public void ExitPause()
        {
            SetInputBlocked(false);
        }

        public async Task ExitContextAsync()
        {
            SetInputBlocked(false);
            if (_disconnectService == null)
            {
                Debug.LogError("Serviço de desconexão da partida não configurado.", this);
                return;
            }

            await _disconnectService.DisconnectAsync();
        }

        private void SetInputBlocked(bool isBlocked)
        {
            if (_isInputBlocked == isBlocked)
            {
                return;
            }

            _isInputBlocked = isBlocked;
            RefreshLocalInputBlockers();
            foreach (IGameplayInputBlocker inputBlocker in _inputBlockers)
            {
                inputBlocker.SetGameplayInputBlocked(isBlocked);
            }
        }

        private void RefreshLocalInputBlockers()
        {
            _inputBlockers.Clear();
            NetworkPlayerState[] players =
                FindObjectsByType<NetworkPlayerState>(FindObjectsSortMode.None);

            foreach (NetworkPlayerState player in players)
            {
                if (player == null || !player.IsOwner)
                {
                    continue;
                }

                foreach (MonoBehaviour behaviour in player.GetComponents<MonoBehaviour>())
                {
                    if (behaviour is IGameplayInputBlocker inputBlocker)
                    {
                        _inputBlockers.Add(inputBlocker);
                    }
                }

                break;
            }
        }
    }
}
