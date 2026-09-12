using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class ConnectionPanelUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private Text _secondaryButtonLabel;
        [SerializeField] private Text _statusText;

        private Coroutine _selectionRoutine;
        private GameObject _selectionTarget;
        private Navigation _hostNavigation;
        private Navigation _clientNavigation;
        private Navigation _secondaryNavigation;
        private bool _isConnecting;

        public event Action HostRequested;
        public event Action ClientRequested;
        public event Action CancelConnectionRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            _hostNavigation = _hostButton.navigation;
            _clientNavigation = _clientButton.navigation;
            _secondaryNavigation = _secondaryButton.navigation;
        }

        private void OnEnable()
        {
            _hostButton.onClick.AddListener(HandleHostClicked);
            _clientButton.onClick.AddListener(HandleClientClicked);
            _secondaryButton.onClick.AddListener(HandleSecondaryClicked);
            FocusDefaultButton();
        }

        private void OnDisable()
        {
            _hostButton.onClick.RemoveListener(HandleHostClicked);
            _clientButton.onClick.RemoveListener(HandleClientClicked);
            _secondaryButton.onClick.RemoveListener(HandleSecondaryClicked);
            StopSelectionRoutine();
            RestoreNavigation();
        }

        public void ShowIdle()
        {
            _isConnecting = false;
            SetButtonsInteractable(true);
            RestoreNavigation();
            _secondaryButtonLabel.text = "SAIR";
            SetStatus("ESCOLHA HOST OU CLIENTE");
            FocusDefaultButton();
        }

        public void ShowConnecting(string message)
        {
            _isConnecting = true;
            SetButtonsInteractable(false);
            DisableNavigation();
            _secondaryButtonLabel.text = "CANCELAR";
            SetStatus(message);
            SelectButtonNextFrame(_secondaryButton.gameObject);
        }

        public void ShowError(string message)
        {
            _isConnecting = false;
            SetButtonsInteractable(true);
            RestoreNavigation();
            _secondaryButtonLabel.text = "SAIR";
            SetStatus(message);
            FocusDefaultButton();
        }

        public void FocusDefaultButton()
        {
            SelectButtonNextFrame(_hostButton.gameObject);
        }

        private void HandleHostClicked()
        {
            HostRequested?.Invoke();
        }

        private void HandleClientClicked()
        {
            ClientRequested?.Invoke();
        }

        private void HandleSecondaryClicked()
        {
            if (_isConnecting)
            {
                CancelConnectionRequested?.Invoke();
                return;
            }

            QuitRequested?.Invoke();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _hostButton.interactable = interactable;
            _clientButton.interactable = interactable;
        }

        private void DisableNavigation()
        {
            Navigation disabledNavigation = Navigation.defaultNavigation;
            disabledNavigation.mode = Navigation.Mode.None;
            _hostButton.navigation = disabledNavigation;
            _clientButton.navigation = disabledNavigation;
            _secondaryButton.navigation = disabledNavigation;
        }

        private void RestoreNavigation()
        {
            _hostButton.navigation = _hostNavigation;
            _clientButton.navigation = _clientNavigation;
            _secondaryButton.navigation = _secondaryNavigation;
        }

        private void SetStatus(string message)
        {
            _statusText.text = message;
        }

        private void SelectButtonNextFrame(GameObject target)
        {
            StopSelectionRoutine();
            _selectionTarget = target;
            _selectionRoutine = StartCoroutine(SelectButtonRoutine());
        }

        private IEnumerator SelectButtonRoutine()
        {
            yield return null;

            if (_selectionTarget != null && _selectionTarget.activeInHierarchy)
            {
                EventSystem.current?.SetSelectedGameObject(_selectionTarget);
            }

            _selectionTarget = null;
            _selectionRoutine = null;
        }

        private void StopSelectionRoutine()
        {
            if (_selectionRoutine == null)
            {
                return;
            }

            StopCoroutine(_selectionRoutine);
            _selectionRoutine = null;
            _selectionTarget = null;
        }
    }
}
