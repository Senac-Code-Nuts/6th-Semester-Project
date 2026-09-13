using UnityEngine;
using Unity.Netcode;
using PiGame.Lobby;
using System.Collections.Generic;

namespace PiGame.Gameplay
{
    public class MatchManager : NetworkBehaviour
    {
        private MatchSnapshot _snapshot;

        [SerializeField] private Transform[] _spawnPoints;

        [Header("Characters")]
        [SerializeField] private List<LobbyCharacterDefinition> _characterDefinitions;

        private int _durationMinutes;
        private LobbyMatchMode _mode;

        [SerializeField] private MatchController _matchController;

        public override void OnNetworkSpawn()
        {
            if(!IsServer)
                return;

            _snapshot = MatchSession.Instance.Snapshot;

            if(_snapshot == null)
            {
                Debug.LogError("MatchSnapshot não encontrado");
                return;
            }

            StartMatch();
        }

        private void StartMatch()
        {
            InitializeRules();
            SpawnPlayers();

            _matchController.Initialize(_snapshot.MatchSettings);
        }

        private void InitializeRules()
        {
            LobbyMatchSettingsData settings = _snapshot.MatchSettings;
            if(settings == null)
            {
                Debug.LogError("Configurações não encontradas");
                return;
            }

            _durationMinutes = settings.DurationMinutes;
            _mode = settings.Mode;
        }

        private LobbyCharacterDefinition FindCharacter(LobbyCharacterId lobbyCharacterId)
        {
            foreach(LobbyCharacterDefinition character in _characterDefinitions)
            {
                if(character.Id == lobbyCharacterId)
                {
                    return character;
                }
            }
            return null;
        }

        private void SpawnPlayers()
        {
            List<int> availableSpawnPoints = new List<int>();

            for(int i = 0; i < _spawnPoints.Length; i++)
            {
                availableSpawnPoints.Add(i);
            }

            foreach(LobbyPlayerData playerData in _snapshot.Players)
            {
                if(availableSpawnPoints.Count == 0)
                {
                    return;
                }

                int randomIndex = Random.Range(0,availableSpawnPoints.Count);

                int spawnPointIndex = availableSpawnPoints[randomIndex];

                availableSpawnPoints.RemoveAt(randomIndex);

                Transform spawnPoint = _spawnPoints[spawnPointIndex];

                LobbyCharacterDefinition character = FindCharacter(playerData.CharacterId);

                if(character == null)
                {
                    Debug.LogError($"Personagem {playerData.CharacterId} não encontrado");

                    continue;
                }

                NetworkObject player = Instantiate(character.PlayerPrefab, spawnPoint.position, spawnPoint.rotation);

                player.SpawnAsPlayerObject(playerData.ClientId);
            }
        }
    }  
}


