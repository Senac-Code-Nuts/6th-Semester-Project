using System;

namespace PiGame.Lobby
{
    public interface ILobbyState
    {
        event Action PlayersChanged;

        int PlayerCount { get; }
        ulong LocalClientId { get; }
        bool AllPlayersReady { get; }
        bool CanStartMatch { get; }

        LobbyPlayerData GetPlayer(int index);
        bool TryGetPlayer(ulong clientId, out LobbyPlayerData player);
        bool IsCharacterAvailable(LobbyCharacterId characterId, ulong requestingClientId);
        void RequestCharacterSelection(LobbyCharacterId characterId);
        void RequestInputDevice(LobbyInputDeviceKind inputDevice);
        void RequestReadyState(bool isReady);
    }
}
