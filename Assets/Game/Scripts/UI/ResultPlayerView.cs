using System;
using PiGame.Gameplay;
using PiGame.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class ResultPlayerView : MonoBehaviour
    {
        [Serializable]
        private class CharacterVisual
        {
            public LobbyCharacterId Id;
            public string Name;
            public RuntimeAnimatorController Idle;
        }

        [SerializeField] private RectTransform _podium;
        [SerializeField] private Image _frame;
        [SerializeField] private TMP_Text _place;
        [SerializeField] private TMP_Text _playerTag;
        [SerializeField] private TMP_Text _playerName;
        [SerializeField] private TMP_Text _characterName;
        [SerializeField] private TMP_Text _points;
        [SerializeField] private Animator _character;
        [SerializeField] private CharacterVisual[] _characters;
        [SerializeField] private Color _winnerColor = new Color32(231, 186, 113, 255);
        [SerializeField] private float _winnerHeight = 310f;
        [SerializeField] private float _otherHeight = 270f;
        [SerializeField] private float _lowerPlaceHeight = 240f;

        private void Awake()
        {
            if (!_podium || !_frame || !_place || !_playerTag || !_playerName
                || !_characterName || !_points || !_character || _characters == null
                || _characters.Length == 0 || Array.Exists(_characters, c => c == null || !c.Idle))
            {
                Debug.LogError("Configure os textos, pódio e animações do ResultPlayerView no prefab.", this);
                enabled = false;
            }
        }

        public void Show(MatchPlayerScoreData player, int place, bool winner)
        {
            gameObject.SetActive(true);
            if (!enabled) return;

            CharacterVisual visual = Array.Find(_characters, c => c.Id == player.CharacterId);
            if (visual == null)
            {
                Debug.LogError($"Personagem {player.CharacterId} sem prévia no ResultPlayerView.", this);
                enabled = false;
                return;
            }

            _place.text = $"{place}º";
            _place.color = winner ? _winnerColor : Color.white;
            _playerTag.text = $"P{player.PlayerSlot + 1}";
            _playerTag.color = PlayerSlotColors.Get(player.PlayerSlot);
            _playerName.text = player.PlayerName.ToString();
            _characterName.text = visual.Name;
            _points.text = player.Kills.ToString();
            _frame.color = winner ? _winnerColor : PlayerSlotColors.Get(player.PlayerSlot);
            _podium.sizeDelta = new Vector2(_podium.sizeDelta.x,
                winner ? _winnerHeight : place >= 3 ? _lowerPlaceHeight : _otherHeight);
            if (_character.runtimeAnimatorController != visual.Idle)
            {
                _character.runtimeAnimatorController = visual.Idle;
                _character.Rebind();
                _character.Update(0f);
            }
        }
    }
}
