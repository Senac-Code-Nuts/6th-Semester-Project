using System;
using System.Collections.Generic;
using PiGame.UI;
using UnityEngine;

namespace PiGame.Lobby
{
    public class LobbyCharacterSelectionController : MonoBehaviour
    {
        [SerializeField] private CharacterSelectionPanelUI _view;
        [SerializeField] private NetworkLobbyState _lobbyState;

        public event Action DisconnectRequested;

        private ILobbyState _lobbyStateContract;

        private void Awake()
        {
            _lobbyStateContract = _lobbyState;
        }

        private void OnEnable()
        {
            _view.BrowseRequested += HandleBrowseRequested;
            _view.SubmitRequested += HandleSubmitRequested;
            _view.BackRequested += HandleBackRequested;

            if (_lobbyStateContract != null)
            {
                _lobbyStateContract.PlayersChanged += Refresh;
                _lobbyStateContract.CharacterReadyRejected += HandleCharacterReadyRejected;
                _lobbyStateContract.StageChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            _view.BrowseRequested -= HandleBrowseRequested;
            _view.SubmitRequested -= HandleSubmitRequested;
            _view.BackRequested -= HandleBackRequested;

            if (_lobbyStateContract != null)
            {
                _lobbyStateContract.PlayersChanged -= Refresh;
                _lobbyStateContract.CharacterReadyRejected -= HandleCharacterReadyRejected;
                _lobbyStateContract.StageChanged -= Refresh;
            }
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _view.SetInteractionEnabled(isEnabled);
        }

        private void HandleBrowseRequested(int direction, LobbyInputDeviceKind inputDevice)
        {
            if (_lobbyStateContract == null
                || _lobbyStateContract.Stage != LobbyStage.CharacterSelection
                || !_lobbyStateContract.TryGetPlayer(_lobbyStateContract.LocalClientId, out LobbyPlayerData player)
                || player.IsReady)
            {
                return;
            }

            _lobbyStateContract.RequestInputDevice(inputDevice);
            _view.ClearCharacterBlockedFeedback();
            _lobbyStateContract.RequestCharacterSelection(FindNextCharacter(player, direction));
        }

        private void HandleSubmitRequested(LobbyInputDeviceKind inputDevice)
        {
            if (_lobbyStateContract == null
                || _lobbyStateContract.Stage != LobbyStage.CharacterSelection
                || !_lobbyStateContract.TryGetPlayer(_lobbyStateContract.LocalClientId, out LobbyPlayerData player))
            {
                return;
            }

            _lobbyStateContract.RequestInputDevice(inputDevice);
            if (player.IsReady)
            {
                _lobbyStateContract.RequestReadyState(false);
                return;
            }

            if (player.CharacterId == LobbyCharacterId.None)
            {
                return;
            }

            if (_lobbyStateContract.TryGetCharacterLock(
                    player.CharacterId,
                    player.ClientId,
                    out int lockingPlayerSlot))
            {
                _view.ShowCharacterBlocked(lockingPlayerSlot);
                return;
            }

            _lobbyStateContract.RequestReadyState(true);
        }

        private void HandleBackRequested()
        {
            if (_lobbyStateContract == null
                || _lobbyStateContract.Stage != LobbyStage.CharacterSelection)
            {
                return;
            }

            DisconnectRequested?.Invoke();
        }

        private static LobbyCharacterId FindNextCharacter(LobbyPlayerData player, int direction)
        {
            const int characterCount = 4;
            int step = direction >= 0 ? 1 : -1;
            int currentIndex = player.CharacterId == LobbyCharacterId.None
                ? (step > 0 ? -1 : 0)
                : (int)player.CharacterId;
            int candidateIndex = (currentIndex + step + characterCount) % characterCount;
            return (LobbyCharacterId)candidateIndex;
        }

        private void HandleCharacterReadyRejected(int lockingPlayerSlot)
        {
            _view.ShowCharacterBlocked(lockingPlayerSlot);
        }

        private void Refresh()
        {
            if (_lobbyStateContract == null)
            {
                return;
            }

            List<LobbyPlayerData> snapshot = new(_lobbyStateContract.PlayerCount);
            for (int i = 0; i < _lobbyStateContract.PlayerCount; i++)
            {
                snapshot.Add(_lobbyStateContract.GetPlayer(i));
            }

            _view.Render(
                snapshot,
                _lobbyStateContract.LocalClientId,
                _lobbyStateContract.RequireUniqueCharacters);
        }
    }
}
