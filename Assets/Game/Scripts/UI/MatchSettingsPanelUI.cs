using System;
using PiGame.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class MatchSettingsPanelUI : MonoBehaviour
    {
        private const int DefaultDurationMinutes = 3;
        private const LobbyMatchMode DefaultMode = LobbyMatchMode.Solo;
        private const int DefaultMinimumPlayers = 2;

        [Header("Panels")]
        [SerializeField] private Button _menuButton;
        [SerializeField] private GameObject _overlay;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _closeButton;

        [Header("Duration")]
        [SerializeField] private Button[] _durationButtons;

        [Header("Mode")]
        [SerializeField] private Button[] _modeButtons;

        [Header("Summary")]
        [SerializeField] private Text _summaryText;

        [Header("Selection visual")]
        [SerializeField] private Color _normalColor = new Color(0.82f, 0.76f, 0.88f, 1f);
        [SerializeField] private Color _selectedColor = new Color(0.45f, 0.22f, 0.63f, 1f);
        [SerializeField] private Color _normalTextColor = new Color(0.14f, 0.08f, 0.18f, 1f);
        [SerializeField] private Color _selectedTextColor = Color.white;
        [SerializeField] private Color _unavailableColor = new Color(0.32f, 0.3f, 0.36f, 1f);
        [SerializeField] private Color _unavailableTextColor = new Color(0.68f, 0.66f, 0.72f, 1f);
        [SerializeField] private float _selectedScale = 1.15f;

        public event Action Opened;
        public event Action Closed;
        public event Action<int, LobbyMatchMode> SettingsConfirmed;

        public int SelectedDurationMinutes { get; private set; } = DefaultDurationMinutes;
        public LobbyMatchMode SelectedMode { get; private set; } = DefaultMode;
        public bool IsOpen => _overlay != null && _overlay.activeSelf;

        private int _minimumPlayers = DefaultMinimumPlayers;
        private bool _requireUniqueCharacters;
        private LobbyMatchSettingsData _confirmedSettings;

        private void Awake()
        {
            _modeButtons[0].interactable = false;
            _confirmedSettings = new LobbyMatchSettingsData(
                DefaultDurationMinutes,
                DefaultMode,
                DefaultMinimumPlayers,
                false);
            RefreshSelectionVisuals();
            RefreshSummary();
        }

        private void OnEnable()
        {
            _menuButton.onClick.AddListener(Show);
            _confirmButton.onClick.AddListener(ConfirmSelection);
            _closeButton.onClick.AddListener(Hide);

            _durationButtons[0].onClick.AddListener(SelectTwoMinutes);
            _durationButtons[1].onClick.AddListener(SelectThreeMinutes);
            _durationButtons[2].onClick.AddListener(SelectFourMinutes);
            _durationButtons[3].onClick.AddListener(SelectFiveMinutes);

            _modeButtons[1].onClick.AddListener(SelectSoloMode);
        }

        private void OnDisable()
        {
            _menuButton.onClick.RemoveListener(Show);
            _confirmButton.onClick.RemoveListener(ConfirmSelection);
            _closeButton.onClick.RemoveListener(Hide);

            _durationButtons[0].onClick.RemoveListener(SelectTwoMinutes);
            _durationButtons[1].onClick.RemoveListener(SelectThreeMinutes);
            _durationButtons[2].onClick.RemoveListener(SelectFourMinutes);
            _durationButtons[3].onClick.RemoveListener(SelectFiveMinutes);

            _modeButtons[1].onClick.RemoveListener(SelectSoloMode);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            bool keyboardCanceled = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool gamepadCanceled = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

            if (keyboardCanceled || gamepadCanceled)
            {
                Hide();
            }
        }

        public void Show()
        {
            if (IsOpen || !_menuButton.gameObject.activeInHierarchy)
            {
                return;
            }

            _overlay.SetActive(true);
            _menuButton.interactable = false;
            LoadDraftFromConfirmedSettings();
            RefreshSelectionVisuals();
            SelectCurrentOption();
            Opened?.Invoke();
        }

        public void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            _overlay.SetActive(false);
            _menuButton.interactable = true;
            LoadDraftFromConfirmedSettings();
            RefreshSelectionVisuals();
            EventSystem.current?.SetSelectedGameObject(_menuButton.gameObject);
            Closed?.Invoke();
        }

        public void ResetView()
        {
            _overlay.SetActive(false);
            _menuButton.interactable = true;
            LoadDraftFromConfirmedSettings();
            RefreshSelectionVisuals();
            RefreshSummary();
        }

        public void Render(LobbyMatchSettingsData settings)
        {
            _confirmedSettings = settings;
            _minimumPlayers = settings.MinimumPlayers;
            _requireUniqueCharacters = settings.RequireUniqueCharacters;

            if (!IsOpen)
            {
                LoadDraftFromConfirmedSettings();
            }

            RefreshSelectionVisuals();
            RefreshSummary();
        }

        public void SetMenuVisible(bool isVisible)
        {
            if (!isVisible)
            {
                _overlay.SetActive(false);
            }

            _menuButton.gameObject.SetActive(isVisible);
        }

        private void SelectTwoMinutes()
        {
            SelectDuration(2);
        }

        private void SelectThreeMinutes()
        {
            SelectDuration(3);
        }

        private void SelectFourMinutes()
        {
            SelectDuration(4);
        }

        private void SelectFiveMinutes()
        {
            SelectDuration(5);
        }

        private void SelectSoloMode()
        {
            SelectMode(LobbyMatchMode.Solo);
        }

        private void SelectDuration(int durationMinutes)
        {
            SelectedDurationMinutes = durationMinutes;
            RefreshSelectionVisuals();
        }

        private void SelectMode(LobbyMatchMode mode)
        {
            SelectedMode = mode;
            RefreshSelectionVisuals();
        }

        private void ConfirmSelection()
        {
            SettingsConfirmed?.Invoke(SelectedDurationMinutes, SelectedMode);
            Hide();
        }

        private void RefreshSelectionVisuals()
        {
            for (int index = 0; index < _durationButtons.Length; index++)
            {
                bool isSelected = SelectedDurationMinutes == index + 2;
                ApplySelectionVisual(_durationButtons[index], isSelected);
            }

            for (int index = 0; index < _modeButtons.Length; index++)
            {
                if (index == 0)
                {
                    Button unavailableButton = _modeButtons[index];
                    unavailableButton.targetGraphic.color = _unavailableColor;
                    unavailableButton.transform.localScale = Vector3.one;
                    Text unavailableLabel = unavailableButton.GetComponentInChildren<Text>(true);
                    unavailableLabel.color = _unavailableTextColor;
                    continue;
                }

                bool isSelected = (int)SelectedMode == index;
                ApplySelectionVisual(_modeButtons[index], isSelected);
            }
        }

        private void ApplySelectionVisual(Button button, bool isSelected)
        {
            button.targetGraphic.color = isSelected ? _selectedColor : _normalColor;
            button.transform.localScale = isSelected ? Vector3.one * _selectedScale : Vector3.one;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = isSelected ? _selectedTextColor : _normalTextColor;
            }
        }

        private void SelectCurrentOption()
        {
            int durationIndex = Mathf.Clamp(SelectedDurationMinutes - 2, 0, _durationButtons.Length - 1);
            EventSystem.current?.SetSelectedGameObject(_durationButtons[durationIndex].gameObject);
        }

        private void RefreshSummary()
        {
            if (_summaryText == null)
            {
                return;
            }

            string modeName = _confirmedSettings.Mode == LobbyMatchMode.Team ? "EQUIPE" : "SOLO";
            string repeatedCharacters = _requireUniqueCharacters ? "NÃO" : "SIM";
            _summaryText.text =
                $"PARTIDA: {_confirmedSettings.DurationMinutes} MIN  |  MODO: {modeName}\n"
                + $"MÍNIMO: {_minimumPlayers}  |  REPETIDOS: {repeatedCharacters}";
        }

        private void LoadDraftFromConfirmedSettings()
        {
            SelectedDurationMinutes = _confirmedSettings.DurationMinutes;
            SelectedMode = _confirmedSettings.Mode;
        }
    }
}
