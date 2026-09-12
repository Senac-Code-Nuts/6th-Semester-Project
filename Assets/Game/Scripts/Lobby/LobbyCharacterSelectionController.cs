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
            }
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _view.SetInteractionEnabled(isEnabled);
        }

        private void HandleBrowseRequested(int direction, LobbyInputDeviceKind inputDevice)
        {
            if (_lobbyStateContract == null
                || !_lobbyStateContract.TryGetPlayer(_lobbyStateContract.LocalClientId, out LobbyPlayerData player)
                || player.IsReady)
            {
                return;
            }

            _lobbyStateContract.RequestInputDevice(inputDevice);
            LobbyCharacterId nextCharacter = FindNextAvailableCharacter(player, direction);
            if (nextCharacter != LobbyCharacterId.None)
            {
                _lobbyStateContract.RequestCharacterSelection(nextCharacter);
            }
        }

        private void HandleSubmitRequested(LobbyInputDeviceKind inputDevice)
        {
            if (_lobbyStateContract == null
                || !_lobbyStateContract.TryGetPlayer(_lobbyStateContract.LocalClientId, out LobbyPlayerData player))
            {
                return;
            }

            _lobbyStateContract.RequestInputDevice(inputDevice);
            if (player.CharacterId != LobbyCharacterId.None)
            {
                _lobbyStateContract.RequestReadyState(!player.IsReady);
            }
        }

        private void HandleBackRequested()
        {
            DisconnectRequested?.Invoke();
        }

        private LobbyCharacterId FindNextAvailableCharacter(LobbyPlayerData player, int direction)
        {
            const int characterCount = 4;
            int step = direction >= 0 ? 1 : -1;
            int currentIndex = player.CharacterId == LobbyCharacterId.None
                ? (step > 0 ? -1 : 0)
                : (int)player.CharacterId;

            for (int offset = 1; offset <= characterCount; offset++)
            {
                int candidateIndex = (currentIndex + step * offset + characterCount * 2) % characterCount;
                LobbyCharacterId candidate = (LobbyCharacterId)candidateIndex;
                if (_lobbyStateContract.IsCharacterAvailable(candidate, player.ClientId))
                {
                    return candidate;
                }
            }

            return LobbyCharacterId.None;
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

            _view.Render(snapshot, _lobbyStateContract.LocalClientId);
        }
    }
}
