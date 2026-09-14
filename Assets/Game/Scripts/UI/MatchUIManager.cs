using System.Text;
using TMPro;
using UnityEngine;

namespace PiGame.Gameplay
{
    public class MatchUIManager : MonoBehaviour
    {
        [SerializeField] private MatchController _matchController;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _resultText;

        private void Awake()
        {
            CreateMissingTexts();
        }

        private void OnEnable()
        {
            if (_matchController == null)
            {
                return;
            }

            _matchController.ScoresChanged += RefreshScores;
            _matchController.PhaseChanged += RefreshResult;
            RefreshScores();
            RefreshResult();
        }

        private void OnDisable()
        {
            if (_matchController == null)
            {
                return;
            }

            _matchController.ScoresChanged -= RefreshScores;
            _matchController.PhaseChanged -= RefreshResult;
        }

        private void Update()
        {
            if (_matchController == null || _timerText == null)
            {
                return;
            }

            float time = _matchController.RemainingTime;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void RefreshScores()
        {
            if (_scoreText == null || _matchController == null)
            {
                return;
            }

            StringBuilder scoreBuilder = new StringBuilder();
            for (int i = 0; i < _matchController.ScoreCount; i++)
            {
                MatchPlayerScoreData score = _matchController.GetScore(i);
                if (scoreBuilder.Length > 0)
                {
                    scoreBuilder.Append("   ");
                }

                scoreBuilder.Append($"P{score.PlayerSlot + 1}: {score.Kills}/{_matchController.KillLimit}");
            }

            _scoreText.text = scoreBuilder.ToString();
        }

        private void RefreshResult()
        {
            if (_resultText == null || _matchController == null)
            {
                return;
            }

            bool showResult = _matchController.Phase == MatchPhase.ShowingResult;
            _resultText.gameObject.SetActive(showResult);
            if (!showResult)
            {
                return;
            }

            _resultText.text = _matchController.WinningPlayerSlot >= 0
                ? $"P{_matchController.WinningPlayerSlot + 1} VENCEU!"
                : "EMPATE!";
        }

        private void CreateMissingTexts()
        {
            if (_timerText == null)
            {
                return;
            }

            if (_scoreText == null)
            {
                _scoreText = CreateText("ScoreText", new Vector2(0f, 190f), 28f);
            }

            if (_resultText == null)
            {
                _resultText = CreateText("ResultText", Vector2.zero, 56f);
                _resultText.gameObject.SetActive(false);
            }
        }

        private TMP_Text CreateText(string objectName, Vector2 position, float fontSize)
        {
            TMP_Text text = Instantiate(_timerText, _timerText.transform.parent);
            text.name = objectName;
            text.text = string.Empty;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;

            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(900f, 100f);
            return text;
        }
    }
}
