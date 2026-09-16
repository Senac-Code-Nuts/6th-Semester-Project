using UnityEngine;

namespace PiGame.Lobby
{
    [CreateAssetMenu(fileName = "data_map_", menuName = "Pi Game/Lobby/Map")]
    public class LobbyMapDefinition : ScriptableObject
    {
        [SerializeField] private LobbyMapId _id = LobbyMapId.CidadeSemNome1;
        [SerializeField] private string _displayName = "MAPA 01";
        [SerializeField] private Sprite _preview;
        [SerializeField] private string _sceneName;

        public LobbyMapId Id => _id;
        public string DisplayName => _displayName;
        public Sprite Preview => _preview;
        public string SceneName => _sceneName;
    }
}
