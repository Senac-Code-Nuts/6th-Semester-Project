using System;
using Unity.Netcode;
using UnityEngine;

namespace PiGame.Lobby
{
    public class NetworkLobbyState : NetworkBehaviour, ILobbyState
    {
        [SerializeField, Min(1)] private int _maximumPlayers = 4;

        private readonly NetworkList<LobbyPlayerData> _players = new();

        public event Action PlayersChanged;

        public int PlayerCount => _players.Count;
        public ulong LocalClientId => NetworkManager != null
            ? NetworkManager.LocalClientId
            : ulong.MaxValue;
        public bool CanStartMatch => _players.Count >= 2 && AllPlayersReady;

        public bool AllPlayersReady
        {
            get
            {
                if (_players.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < _players.Count; i++)
                {
                    LobbyPlayerData player = _players[i];
                    if (!player.IsReady || player.CharacterId == LobbyCharacterId.None)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override void OnNetworkSpawn()
        {
            _players.OnListChanged += HandlePlayersChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

                foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
                {
                    AddPlayer(clientId);
                }
            }

            PlayersChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _players.OnListChanged -= HandlePlayersChanged;

            if (NetworkManager != null && IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            PlayersChanged?.Invoke();
        }

        public LobbyPlayerData GetPlayer(int index)
        {
            if (index < 0 || index >= _players.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _players[index];
        }

        public bool TryGetPlayer(ulong clientId, out LobbyPlayerData player)
        {
            int playerIndex = FindPlayerIndex(clientId);
            if (playerIndex >= 0)
            {
                player = _players[playerIndex];
                return true;
            }

            player = default;
            return false;
        }

        public bool IsCharacterAvailable(LobbyCharacterId characterId, ulong requestingClientId)
        {
            if (!IsValidCharacter(characterId))
            {
                return false;
            }

            for (int i = 0; i < _players.Count; i++)
            {
                LobbyPlayerData player = _players[i];
                if (player.ClientId != requestingClientId && player.CharacterId == characterId)
                {
                    return false;
                }
            }

            return true;
        }

        public void RequestCharacterSelection(LobbyCharacterId characterId)
        {
            if (!IsSpawned || !IsValidCharacter(characterId))
            {
                return;
            }

            SelectCharacterRpc(characterId);
        }

        public void RequestReadyState(bool isReady)
        {
            if (!IsSpawned)
            {
                return;
            }

            SetReadyStateRpc(isReady);
        }

        public void RequestInputDevice(LobbyInputDeviceKind inputDevice)
        {
            if (!IsSpawned || inputDevice == LobbyInputDeviceKind.Unknown)
            {
                return;
            }

            SetInputDeviceRpc(inputDevice);
        }

        [Rpc(SendTo.Server)]
        private void SelectCharacterRpc(LobbyCharacterId characterId, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            int playerIndex = FindPlayerIndex(senderClientId);
            if (playerIndex < 0 || !IsCharacterAvailable(characterId, senderClientId))
            {
                return;
            }

            LobbyPlayerData player = _players[playerIndex];
            if (player.CharacterId == characterId && !player.IsReady)
            {
                return;
            }

            player.CharacterId = characterId;
            player.IsReady = false;
            _players[playerIndex] = player;
        }

        [Rpc(SendTo.Server)]
        private void SetReadyStateRpc(bool isReady, RpcParams rpcParams = default)
        {
            int playerIndex = FindPlayerIndex(rpcParams.Receive.SenderClientId);
            if (playerIndex < 0)
            {
                return;
            }

            LobbyPlayerData player = _players[playerIndex];
            if (isReady && player.CharacterId == LobbyCharacterId.None)
            {
                return;
            }

            if (player.IsReady == isReady)
            {
                return;
            }

            player.IsReady = isReady;
            _players[playerIndex] = player;
        }

        [Rpc(SendTo.Server)]
        private void SetInputDeviceRpc(
            LobbyInputDeviceKind inputDevice,
            RpcParams rpcParams = default)
        {
            if (inputDevice != LobbyInputDeviceKind.Keyboard
                && inputDevice != LobbyInputDeviceKind.Gamepad)
            {
                return;
            }

            int playerIndex = FindPlayerIndex(rpcParams.Receive.SenderClientId);
            if (playerIndex < 0)
            {
                return;
            }

            LobbyPlayerData player = _players[playerIndex];
            if (player.InputDevice == inputDevice)
            {
                return;
            }

            player.InputDevice = inputDevice;
            _players[playerIndex] = player;
        }

        private void HandlePlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
        {
            PlayersChanged?.Invoke();
        }

        private void HandleClientConnected(ulong clientId)
        {
            AddPlayer(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            int playerIndex = FindPlayerIndex(clientId);
            if (playerIndex >= 0)
            {
                _players.RemoveAt(playerIndex);
            }
        }

        private void AddPlayer(ulong clientId)
        {
            if (FindPlayerIndex(clientId) >= 0)
            {
                return;
            }

            int availableSlot = FindAvailableSlot();
            if (availableSlot < 0)
            {
                NetworkManager.DisconnectClient(clientId, "Lobby cheio.");
                return;
            }

            LobbyCharacterId initialCharacter = FindFirstAvailableCharacter(clientId);
            _players.Add(new LobbyPlayerData(clientId, availableSlot, initialCharacter));
        }

        private int FindPlayerIndex(ulong clientId)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].ClientId == clientId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindAvailableSlot()
        {
            for (int slot = 0; slot < _maximumPlayers; slot++)
            {
                bool isOccupied = false;
                for (int i = 0; i < _players.Count; i++)
                {
                    if (_players[i].PlayerSlot == slot)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    return slot;
                }
            }

            return -1;
        }

        private static bool IsValidCharacter(LobbyCharacterId characterId)
        {
            return characterId >= LobbyCharacterId.Cowboy
                && characterId <= LobbyCharacterId.Rebel;
        }

        private LobbyCharacterId FindFirstAvailableCharacter(ulong clientId)
        {
            for (int characterIndex = 0; characterIndex < 4; characterIndex++)
            {
                LobbyCharacterId characterId = (LobbyCharacterId)characterIndex;
                if (IsCharacterAvailable(characterId, clientId))
                {
                    return characterId;
                }
            }

            return LobbyCharacterId.None;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maximumPlayers = Mathf.Max(1, _maximumPlayers);
        }
#endif
    }
}
