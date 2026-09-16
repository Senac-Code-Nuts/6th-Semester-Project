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

        private MatchController _subscribedMatchController;

        private void Awake()
        {
            CreateMissingTexts();
            ConfigureHudLayout();
            EnsureHudVisible();
        }

        private void OnEnable()
        {
            BindMatchController();
            EnsureHudVisible();
        }

        private void OnDisable()
        {
            UnbindMatchController();
        }

        private void BindMatchController()
        {
            if (_matchController == null)
            {
                _matchController = FindFirstObjectByType<MatchController>();
            }

            if (_matchController == null || _subscribedMatchController == _matchController)
            {
                return;
            }

            UnbindMatchController();
            _subscribedMatchController = _matchController;
            _subscribedMatchController.ScoresChanged += RefreshScores;
            _subscribedMatchController.PhaseChanged += RefreshResult;
            RefreshScores();
            RefreshResult();
        }

        private void UnbindMatchController()
        {
            if (_subscribedMatchController == null)
            {
                return;
            }

            _subscribedMatchController.ScoresChanged -= RefreshScores;
            _subscribedMatchController.PhaseChanged -= RefreshResult;
            _subscribedMatchController = null;
        }

        private void Update()
        {
            if (_subscribedMatchController == null)
            {
                BindMatchController();
            }

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

                string playerColor = PlayerSlotColors.GetHtml(score.PlayerSlot);
                scoreBuilder.Append(
                    $"<color=#{playerColor}>P{score.PlayerSlot + 1}: "
                    + $"{score.Kills}/{_matchController.KillLimit}</color>");
            }

            _scoreText.text = scoreBuilder.ToString();
        }

        private void EnsureHudVisible()
        {
            if (_timerText == null)
            {
                return;
            }

            Canvas hudCanvas = _timerText.canvas;
            if (hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(true);
                hudCanvas.enabled = true;
                hudCanvas.transform.localScale = Vector3.one;
            }

            _timerText.gameObject.SetActive(true);
            if (_scoreText != null)
            {
                _scoreText.gameObject.SetActive(true);
            }
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

            int winningPlayerSlot = _matchController.WinningPlayerSlot;
            if (winningPlayerSlot < 0)
            {
                _resultText.text = "EMPATE!";
                return;
            }

            string playerColor = PlayerSlotColors.GetHtml(winningPlayerSlot);
            _resultText.text =
                $"<color=#{playerColor}>P{winningPlayerSlot + 1} VENCEU!</color>";
        }

        private void CreateMissingTexts()
        {
            if (_timerText == null)
            {
                return;
            }

            if (_scoreText == null)
            {
                _scoreText = CreateText("ScoreText", 28f);
            }

            if (_resultText == null)
            {
                _resultText = CreateText("ResultText", 56f);
                _resultText.gameObject.SetActive(false);
            }
        }

        private void ConfigureHudLayout()
        {
            ConfigureTextRect(
                _scoreText,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(1100f, 60f),
                TextAlignmentOptions.Center);

            ConfigureTextRect(
                _timerText,
                Vector2.zero,
                Vector2.zero,
                new Vector2(32f, 24f),
                new Vector2(260f, 60f),
                TextAlignmentOptions.Left);

            ConfigureTextRect(
                _resultText,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1400f, 180f),
                TextAlignmentOptions.Center);
        }

        private TMP_Text CreateText(string objectName, float fontSize)
        {
            TMP_Text text = Instantiate(_timerText, _timerText.transform.parent);
            text.name = objectName;
            text.text = string.Empty;
            text.enableAutoSizing = false;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;

            return text;
        }

        private static void ConfigureTextRect(
            TMP_Text text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            TextAlignmentOptions alignment)
        {
            if (text == null)
            {
                return;
            }

            text.alignment = alignment;
            text.margin = Vector4.zero;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = anchorMax;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }
    }
}
