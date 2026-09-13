using UnityEngine;

namespace PiGame.Lobby
{
    public class MatchSnapshot
    {
        public LobbyMapId MapId;
        public LobbyMatchSettingsData MatchSettings;
        public LobbyPlayerData[] Players;

        public MatchSnapshot(
            LobbyMapId mapId,
            LobbyMatchSettingsData matchSettings,
            LobbyPlayerData[] players
        )
        {
            MapId = mapId;
            MatchSettings = matchSettings;
            Players = players;
        }
    }    
}

