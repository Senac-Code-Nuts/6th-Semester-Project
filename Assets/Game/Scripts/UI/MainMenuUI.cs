using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject _visuals;
        [SerializeField] private CanvasGroup _home;
        [Header("Botões — On Click disponível no Inspector para áudio local")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _charactersButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private MenuCharactersPanelUI _characters;
        [SerializeField] private GameObject _credits;
        [SerializeField] private Button _creditsBackButton;

        private Button _returnButton;
        private Coroutine _focusRoutine;

        public bool IsHomeVisible => _home.gameObject.activeInHierarchy;
        public bool IsSubpanelVisible => _characters.IsVisible || _credits.activeInHierarchy;
        public event Action PlayRequested;
        public event Action OptionsRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            if (!_visuals || !_home || !_playButton || !_charactersButton || !_optionsButton
                || !_creditsButton || !_quitButton || !_characters || !_credits || !_creditsBackButton)
            {
                Debug.LogError("Configure as referências do MainMenuUI no prefab.", this);
                enabled = false;
                return;
            }

            // Áudio do menu: os Buttons já oferecem On Click no Inspector.
            // Seleção/hover: SelectablePointerFocus.OnPointerEnter é o ponto existente.
            // Tocar esses sons só localmente, nunca por RPC.
            _playButton.onClick.AddListener(Play);
            _optionsButton.onClick.AddListener(OpenOptions);
            _quitButton.onClick.AddListener(Quit);
            _charactersButton.onClick.AddListener(OpenCharacters);
            _creditsButton.onClick.AddListener(OpenCredits);
            _creditsBackButton.onClick.AddListener(ShowHome);
            _characters.Closed += ShowHome;
            _returnButton = _playButton;
        }

        public void ShowHome()
        {
            _visuals.SetActive(true);
            _characters.Hide();
            _credits.SetActive(false);
            _home.gameObject.SetActive(true);
            SetInteractionEnabled(true);
            FocusDefaultButton();
        }

        public void ShowConnection()
        {
            _visuals.SetActive(true);
            _home.gameObject.SetActive(false);
            _characters.Hide();
            _credits.SetActive(false);
        }

        public void Hide()
        {
            ShowConnection();
            _visuals.SetActive(false);
        }

        public void SetInteractionEnabled(bool value)
        {
            _home.interactable = value;
            _home.blocksRaycasts = value;
        }

        public void FocusDefaultButton()
        {
            if (_focusRoutine != null) StopCoroutine(_focusRoutine);
            _focusRoutine = StartCoroutine(FocusHome());
        }

        private IEnumerator FocusHome()
        {
            yield return null;
            if (IsHomeVisible && _home.interactable)
                EventSystem.current.SetSelectedGameObject(_returnButton.gameObject);
            _focusRoutine = null;
        }

        public void HandleBack()
        {
            if (IsSubpanelVisible) ShowHome();
        }

        private void Play()
        {
            _returnButton = _playButton;
            PlayRequested?.Invoke();
        }

        private void OpenOptions()
        {
            _returnButton = _optionsButton;
            OptionsRequested?.Invoke();
        }

        private void Quit()
        {
            _returnButton = _quitButton;
            QuitRequested?.Invoke();
        }

        private void OpenCharacters()
        {
            _returnButton = _charactersButton;
            _home.gameObject.SetActive(false);
            _characters.Show();
        }

        private void OpenCredits()
        {
            _returnButton = _creditsButton;
            _home.gameObject.SetActive(false);
            _credits.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_creditsBackButton.gameObject);
        }
    }
}
