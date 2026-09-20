using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private Canvas _targetCanvas;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Hierarquia")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _confirmationText;
        [SerializeField] private GameObject _actionsRoot;
        [SerializeField] private GameObject _confirmationRoot;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _confirmExitButton;
        [SerializeField] private Button _cancelExitButton;
        [SerializeField] private ControlsSettingsUI _controlsView;

        private PauseMenuDefinition _definition;
        private Coroutine _selectionRoutine;
        private bool _buttonsBound;

        public event Action ResumeRequested;
        public event Action ExitRequested;
        public event Action ExitConfirmed;
        public event Action ExitCanceled;
        public event Action ControlsClosed;

        public bool IsVisible => _root != null && _root.activeSelf;
        public bool IsConfirmationVisible =>
            _confirmationRoot != null && _confirmationRoot.activeSelf;
        public bool IsControlsVisible => _controlsView != null && _controlsView.IsVisible;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            BindButtons();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void OnDisable()
        {
            StopSelectionRoutine();
        }

        public void Initialize(PauseMenuDefinition definition)
        {
            _definition = definition;
            if (!ValidateReferences())
            {
                return;
            }

            RefreshStaticTexts();
            ConfigureControlsView(_inputActions);
        }

        public void Initialize(
            Canvas canvas,
            TMP_FontAsset font,
            InputActionAsset actions,
            string title,
            string resumeLabel,
            string exitLabel,
            string confirmationMessage)
        {
            _targetCanvas = canvas;
            _font = font;
            _inputActions = actions;
            if (!ValidateReferences())
            {
                return;
            }

            _titleText.text = title;
            _confirmationText.text = confirmationMessage;
            SetButtonLabel(_resumeButton, resumeLabel);
            SetButtonLabel(_controlsButton, "CONTROLES");
            SetButtonLabel(_exitButton, exitLabel);
            ConfigureActions(true);
            ConfigureControlsView(actions);
        }

        public void SetVisible(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            _root.SetActive(isVisible);
            if (!isVisible)
            {
                _controlsView?.Hide(false);
                StopSelectionRoutine();
                return;
            }

            HideConfirmation();
            QueueSelection(_resumeButton);
        }

        public void SetInputDevice(PauseInputDevice device)
        {
            // Mantido por compatibilidade. O pause não exibe mais legendas fixas.
        }

        public void ShowControls()
        {
            if (_controlsView == null)
            {
                return;
            }

            _actionsRoot.SetActive(false);
            _controlsView.Show();
            _root.SetActive(false);
        }

        public void HideControls()
        {
            if (_controlsView == null || !_controlsView.IsVisible)
            {
                return;
            }

            _controlsView.Hide(false);
            _root.SetActive(true);
            _actionsRoot.SetActive(true);
            QueueSelection(_controlsButton);
        }

        public bool HandleControlsBack()
        {
            return _controlsView != null
                && _controlsView.IsVisible
                && _controlsView.HandleBack();
        }

        public void ShowConfirmation()
        {
            if (_confirmationRoot == null)
            {
                return;
            }

            _confirmationRoot.SetActive(true);
            QueueSelection(_cancelExitButton);
        }

        public void HideConfirmation()
        {
            if (_confirmationRoot == null)
            {
                return;
            }

            _confirmationRoot.SetActive(false);
            if (IsVisible)
            {
                QueueSelection(_resumeButton);
            }
        }

        public void SetBusy(bool isBusy)
        {
            SetButtonInteractable(_resumeButton, !isBusy);
            SetButtonInteractable(_controlsButton, !isBusy);
            SetButtonInteractable(_exitButton, !isBusy);
            SetButtonInteractable(_confirmExitButton, !isBusy);
            SetButtonInteractable(_cancelExitButton, !isBusy);
        }

        private void BindButtons()
        {
            if (_buttonsBound || !ValidateReferences())
            {
                return;
            }

            _resumeButton.onClick.AddListener(HandleResumeClicked);
            _controlsButton.onClick.AddListener(ShowControls);
            _exitButton.onClick.AddListener(HandleExitClicked);
            _confirmExitButton.onClick.AddListener(HandleExitConfirmedClicked);
            _cancelExitButton.onClick.AddListener(HandleExitCanceledClicked);
            _buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!_buttonsBound)
            {
                return;
            }

            _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            _controlsButton.onClick.RemoveListener(ShowControls);
            _exitButton.onClick.RemoveListener(HandleExitClicked);
            _confirmExitButton.onClick.RemoveListener(HandleExitConfirmedClicked);
            _cancelExitButton.onClick.RemoveListener(HandleExitCanceledClicked);
            if (_controlsView != null)
            {
                _controlsView.Closed -= HandleControlsClosed;
            }

            _buttonsBound = false;
        }

        private void HandleResumeClicked() => ResumeRequested?.Invoke();
        private void HandleExitClicked() => ExitRequested?.Invoke();
        private void HandleExitConfirmedClicked() => ExitConfirmed?.Invoke();
        private void HandleExitCanceledClicked() => ExitCanceled?.Invoke();

        private void RefreshStaticTexts()
        {
            if (_definition == null)
            {
                return;
            }

            _titleText.text = _definition.Title;
            _confirmationText.text = _definition.ExitConfirmationMessage;
            SetButtonLabel(_resumeButton, _definition.ResumeLabel);
            SetButtonLabel(_controlsButton, _definition.ControlsLabel);
            SetButtonLabel(_exitButton, _definition.ExitLabel);
            ConfigureActions(_definition.ShowControlsButton);
        }

        private void ConfigureActions(bool showControls)
        {
            _controlsButton.gameObject.SetActive(showControls);
        }

        private void ConfigureControlsView(InputActionAsset actions)
        {
            if (_controlsView == null || !_controlsButton.gameObject.activeSelf)
            {
                return;
            }

            if (actions == null)
            {
                _controlsButton.interactable = false;
                return;
            }

            _controlsView.Initialize(_targetCanvas, _font, actions);
            _controlsView.Closed -= HandleControlsClosed;
            _controlsView.Closed += HandleControlsClosed;
        }

        private void HandleControlsClosed()
        {
            _root.SetActive(true);
            _actionsRoot.SetActive(true);
            QueueSelection(_controlsButton);
            ControlsClosed?.Invoke();
        }

        private bool ValidateReferences()
        {
            bool valid = _targetCanvas != null
                && _root != null
                && _titleText != null
                && _confirmationText != null
                && _actionsRoot != null
                && _confirmationRoot != null
                && _resumeButton != null
                && _controlsButton != null
                && _exitButton != null
                && _confirmExitButton != null
                && _cancelExitButton != null;
            if (!valid)
            {
                Debug.LogError(
                    "PauseMenuUI possui referências obrigatórias ausentes. Configure a hierarquia pela cena ou prefab.",
                    this);
            }

            return valid;
        }

        private void QueueSelection(Button button)
        {
            StopSelectionRoutine();
            if (button != null && gameObject.activeInHierarchy)
            {
                _selectionRoutine = StartCoroutine(SelectNextFrame(button));
            }
        }

        private IEnumerator SelectNextFrame(Button button)
        {
            yield return null;
            _selectionRoutine = null;
            if (UnityEngine.EventSystems.EventSystem.current != null
                && button != null
                && button.interactable)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private void StopSelectionRoutine()
        {
            if (_selectionRoutine == null)
            {
                return;
            }

            StopCoroutine(_selectionRoutine);
            _selectionRoutine = null;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>() : null;
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
