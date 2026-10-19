using System;
using System.Collections;
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

        [Header("Entrada dos pilares")]
        [SerializeField, Min(0f)] private float _riseDuration = 2.6f;
        [SerializeField, Min(0f)] private float _riseDistance = 650f;
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Tremor em pixels da UI durante a subida. Zero desativa o efeito.")]
        [SerializeField, Range(0f, 10f)] private float _shakeAmplitude = 2.5f;
        [SerializeField, Min(0f)] private float _shakeFrequency = 18f;

        private Vector2 _restPosition;
        private Coroutine _rise;

        private void Awake()
        {
            if (!_podium || !_frame || !_place || !_playerTag || !_playerName
                || !_characterName || !_points || !_character || _characters == null
                || _characters.Length == 0 || Array.Exists(_characters, c => c == null || !c.Idle))
            {
                Debug.LogError("Configure os textos, pódio e animações do ResultPlayerView no prefab.", this);
                enabled = false;
                return;
            }
            _restPosition = _podium.anchoredPosition;
        }

        private void OnDisable()
        {
            if (_rise == null) return;
            StopCoroutine(_rise);
            _rise = null;
            _podium.anchoredPosition = _restPosition;
        }

        public void Show(MatchPlayerScoreData player, int place, bool winner, bool animate)
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
            // Atualizações do placar não reiniciam a entrada que já está acontecendo.
            if (!animate) return;
            if (_rise != null) StopCoroutine(_rise);
            _rise = null;
            _podium.anchoredPosition = _restPosition;
            if (_riseDuration > 0f) _rise = StartCoroutine(Rise(player.PlayerSlot));
        }

        private IEnumerator Rise(int playerSlot)
        {
            Vector2 start = _restPosition + Vector2.down * _riseDistance;
            float phase = playerSlot * 13.37f;
            for (float elapsed = 0f; elapsed < _riseDuration; elapsed += Time.unscaledDeltaTime)
            {
                float time = elapsed / _riseDuration;
                float progress = Mathf.Clamp01(_riseCurve.Evaluate(time));
                // Ruído suave por jogador: esforço durante a subida, sem deslocamento residual ao parar.
                float strength = _shakeAmplitude * Mathf.Sin(Mathf.PI * time);
                float noiseTime = elapsed * _shakeFrequency;
                Vector2 shake = new Vector2(
                    Mathf.PerlinNoise(noiseTime, phase) * 2f - 1f,
                    (Mathf.PerlinNoise(phase, noiseTime + 37f) * 2f - 1f) * 0.35f) * strength;
                _podium.anchoredPosition = Vector2.LerpUnclamped(start, _restPosition, progress) + shake;
                yield return null;
            }
            _podium.anchoredPosition = _restPosition;
            _rise = null;
        }
    }
}
