using UnityEngine;

namespace PiGame.Lobby
{
    [CreateAssetMenu(
        fileName = "data_lobby_rules",
        menuName = "Pi Game/Lobby/Rules Definition")]
    public class LobbyRulesDefinition : ScriptableObject
    {
        [Header("Players")]
        [SerializeField, Range(1, 4)] private int _minimumPlayers = 2;
        [SerializeField, Range(1, 4)] private int _maximumPlayers = 4;

        [Header("Characters")]
        [SerializeField] private bool _requireUniqueCharacters = true;

        [Header("Match defaults")]
        [SerializeField, Range(2, 5)] private int _defaultDurationMinutes = 3;
        [SerializeField] private LobbyMatchMode _defaultMatchMode = LobbyMatchMode.Solo;

        public int MinimumPlayers => _minimumPlayers;
        public int MaximumPlayers => _maximumPlayers;
        public bool RequireUniqueCharacters => _requireUniqueCharacters;
        public int DefaultDurationMinutes => _defaultDurationMinutes;
        public LobbyMatchMode DefaultMatchMode => _defaultMatchMode;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _minimumPlayers = Mathf.Clamp(_minimumPlayers, 1, 4);
            _maximumPlayers = Mathf.Clamp(_maximumPlayers, _minimumPlayers, 4);
            _defaultDurationMinutes = Mathf.Clamp(_defaultDurationMinutes, 2, 5);

            if (_defaultMatchMode != LobbyMatchMode.Team
                && _defaultMatchMode != LobbyMatchMode.Solo)
            {
                _defaultMatchMode = LobbyMatchMode.Solo;
            }
        }
#endif
    }
}
