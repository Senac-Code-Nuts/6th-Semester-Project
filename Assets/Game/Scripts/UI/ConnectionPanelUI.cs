using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class ConnectionPanelUI : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private Text _secondaryButtonLabel;
        [SerializeField] private Text _statusText;

        private InputField _addressInput;
        private Text _clientButtonLabel;
        private Coroutine _selectionRoutine;
        private GameObject _selectionTarget;
        private Navigation _hostNavigation;
        private Navigation _clientNavigation;
        private Navigation _secondaryNavigation;
        private bool _navigationCached;
        private bool _initializationErrorReported;
        private bool _isConnecting;
        private bool _isEnteringAddress;

        public event Action HostRequested;
        public event Action<string> ClientRequested;
        public event Action CancelConnectionRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _hostButton.onClick.RemoveListener(HandleHostClicked);
            _clientButton.onClick.RemoveListener(HandleClientClicked);
            _secondaryButton.onClick.RemoveListener(HandleSecondaryClicked);
            _addressInput.onValueChanged.RemoveListener(HandleAddressChanged);
            _hostButton.onClick.AddListener(HandleHostClicked);
            _clientButton.onClick.AddListener(HandleClientClicked);
            _secondaryButton.onClick.AddListener(HandleSecondaryClicked);
            _addressInput.onValueChanged.AddListener(HandleAddressChanged);
            FocusDefaultButton();
        }

        private void OnDisable()
        {
            if (_hostButton != null)
            {
                _hostButton.onClick.RemoveListener(HandleHostClicked);
            }

            if (_clientButton != null)
            {
                _clientButton.onClick.RemoveListener(HandleClientClicked);
            }

            if (_secondaryButton != null)
            {
                _secondaryButton.onClick.RemoveListener(HandleSecondaryClicked);
            }

            if (_addressInput != null)
            {
                _addressInput.onValueChanged.RemoveListener(HandleAddressChanged);
            }

            StopSelectionRoutine();
            if (_navigationCached)
            {
                RestoreNavigation();
            }
        }

        public void ShowIdle()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _isConnecting = false;
            _isEnteringAddress = false;
            _hostButton.gameObject.SetActive(true);
            _clientButton.gameObject.SetActive(true);
            _addressInput.gameObject.SetActive(false);
            _addressInput.interactable = true;
            SetButtonsInteractable(true);
            RestoreNavigation();
            SetClientButtonLabel("CLIENT");
            _secondaryButtonLabel.text = "SAIR";
            SetStatus("ESCOLHA HOST OU CLIENTE");
            FocusDefaultButton();
        }

        public void ShowConnecting(string message)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _isConnecting = true;
            SetButtonsInteractable(false);
            _addressInput.interactable = false;
            DisableNavigation();
            _secondaryButtonLabel.text = "CANCELAR";
            SetStatus(message);
            SelectButtonNextFrame(_secondaryButton.gameObject);
        }

        public void ShowError(string message)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _isConnecting = false;
            if (_isEnteringAddress)
            {
                _hostButton.gameObject.SetActive(false);
                _clientButton.gameObject.SetActive(true);
                _addressInput.gameObject.SetActive(true);
                _addressInput.interactable = true;
                _clientButton.interactable = true;
                _secondaryButton.interactable = true;
                SetClientButtonLabel("CONECTAR");
                _secondaryButtonLabel.text = "CANCELAR";
                ConfigureAddressNavigation();
                SetStatus(message);
                FocusAddressInput();
                return;
            }

            SetButtonsInteractable(true);
            RestoreNavigation();
            _secondaryButtonLabel.text = "SAIR";
            SetStatus(message);
            FocusDefaultButton();
        }

        public void FocusDefaultButton()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            SelectButtonNextFrame(_hostButton.gameObject);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (!enabled)
            {
                _hostButton.interactable = false;
                _clientButton.interactable = false;
                _secondaryButton.interactable = false;
                return;
            }

            _hostButton.interactable = !_isConnecting;
            _clientButton.interactable = !_isConnecting;
            _addressInput.interactable = _isEnteringAddress && !_isConnecting;
            _secondaryButton.interactable = true;
        }

        public void OnCancel(BaseEventData eventData)
        {
            HandleSecondaryClicked();
        }

        private void HandleHostClicked()
        {
            HostRequested?.Invoke();
        }

        private void HandleClientClicked()
        {
            if (!_isEnteringAddress)
            {
                ShowAddressEntry();
                return;
            }

            if (!TryNormalizeIpv4(_addressInput.text, out string normalizedAddress))
            {
                SetStatus("IP INVALIDO");
                FocusAddressInput();
                return;
            }

            _addressInput.SetTextWithoutNotify(normalizedAddress);
            ClientRequested?.Invoke(normalizedAddress);
        }

        private void HandleSecondaryClicked()
        {
            if (_isConnecting)
            {
                CancelConnectionRequested?.Invoke();
                return;
            }

            if (_isEnteringAddress)
            {
                ShowIdle();
                return;
            }

            QuitRequested?.Invoke();
        }

        private void ShowAddressEntry()
        {
            _isEnteringAddress = true;
            _hostButton.gameObject.SetActive(false);
            _clientButton.gameObject.SetActive(true);
            _addressInput.gameObject.SetActive(true);
            _addressInput.interactable = true;
            SetClientButtonLabel("CONECTAR");
            _secondaryButtonLabel.text = "CANCELAR";
            SetStatus("DIGITE O IP DO HOST");
            ConfigureAddressNavigation();
            FocusAddressInput();
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

        private void ConfigureAddressNavigation()
        {
            Navigation inputNavigation = Navigation.defaultNavigation;
            inputNavigation.mode = Navigation.Mode.Explicit;
            inputNavigation.selectOnUp = _secondaryButton;
            inputNavigation.selectOnDown = _clientButton;
            _addressInput.navigation = inputNavigation;

            Navigation clientNavigation = Navigation.defaultNavigation;
            clientNavigation.mode = Navigation.Mode.Explicit;
            clientNavigation.selectOnUp = _addressInput;
            clientNavigation.selectOnDown = _secondaryButton;
            _clientButton.navigation = clientNavigation;

            Navigation secondaryNavigation = Navigation.defaultNavigation;
            secondaryNavigation.mode = Navigation.Mode.Explicit;
            secondaryNavigation.selectOnUp = _clientButton;
            secondaryNavigation.selectOnDown = _addressInput;
            _secondaryButton.navigation = secondaryNavigation;
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
                if (_selectionTarget == _addressInput.gameObject)
                {
                    _addressInput.ActivateInputField();
                }
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

        private void FocusAddressInput()
        {
            SelectButtonNextFrame(_addressInput.gameObject);
        }

        private void SetClientButtonLabel(string label)
        {
            if (_clientButtonLabel != null)
            {
                _clientButtonLabel.text = label;
            }
        }

        private void HandleAddressChanged(string value)
        {
            char[] filteredCharacters = new char[value.Length];
            int filteredLength = 0;
            int dotCount = 0;

            foreach (char character in value)
            {
                if (char.IsDigit(character))
                {
                    filteredCharacters[filteredLength++] = character;
                }
                else if (character == '.' && dotCount < 3)
                {
                    filteredCharacters[filteredLength++] = character;
                    dotCount++;
                }
            }

            string filteredValue = new string(filteredCharacters, 0, filteredLength);
            if (filteredValue != value)
            {
                _addressInput.SetTextWithoutNotify(filteredValue);
            }
        }

        private static bool TryNormalizeIpv4(string address, out string normalizedAddress)
        {
            normalizedAddress = string.Empty;
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string[] parts = address.Trim().Split('.');
            if (parts.Length != 4)
            {
                return false;
            }

            int[] octets = new int[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!byte.TryParse(parts[i], out byte octet))
                {
                    return false;
                }

                octets[i] = octet;
            }

            normalizedAddress =
                $"{octets[0]}.{octets[1]}.{octets[2]}.{octets[3]}";
            return true;
        }

        private bool EnsureInitialized()
        {
            if (_hostButton == null
                || _clientButton == null
                || _secondaryButton == null
                || _secondaryButtonLabel == null
                || _statusText == null)
            {
                if (!_initializationErrorReported)
                {
                    Debug.LogError(
                        "ConnectionPanelUI possui referencias obrigatorias ausentes.",
                        this);
                    _initializationErrorReported = true;
                }

                return false;
            }

            _clientButtonLabel ??= _clientButton.GetComponentInChildren<Text>();

            if (_addressInput == null)
            {
                Transform inputParent = _hostButton.transform.parent;
                Transform existingInput = inputParent.Find("ClientAddressInput");
                if (existingInput != null)
                {
                    _addressInput = existingInput.GetComponent<InputField>();
                }

                if (_addressInput == null)
                {
                    CreateAddressInput();
                }
            }

            if (!_navigationCached)
            {
                _hostNavigation = _hostButton.navigation;
                _clientNavigation = _clientButton.navigation;
                _secondaryNavigation = _secondaryButton.navigation;
                _navigationCached = true;
            }

            return _addressInput != null;
        }

        private void CreateAddressInput()
        {
            GameObject inputObject = new GameObject(
                "ClientAddressInput",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(InputField));
            inputObject.layer = gameObject.layer;
            inputObject.transform.SetParent(_hostButton.transform.parent, false);

            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0f, 90f);
            inputRect.sizeDelta = new Vector2(350f, 58f);

            Image inputBackground = inputObject.GetComponent<Image>();
            inputBackground.color = new Color(0.09f, 0.08f, 0.16f, 0.9f);

            Text inputText = CreateInputText(inputObject.transform, "Text", Color.white);
            Text placeholderText = CreateInputText(
                inputObject.transform,
                "Placeholder",
                new Color(1f, 1f, 1f, 0.35f));
            placeholderText.text = "192.168.0.10";

            _addressInput = inputObject.GetComponent<InputField>();
            _addressInput.targetGraphic = inputBackground;
            _addressInput.textComponent = inputText;
            _addressInput.placeholder = placeholderText;
            _addressInput.characterLimit = 15;
            _addressInput.lineType = InputField.LineType.SingleLine;
            _addressInput.contentType = InputField.ContentType.Standard;
            _addressInput.caretColor = Color.white;
            _addressInput.selectionColor = new Color(0.32f, 0.86f, 0.78f, 0.45f);
            inputObject.SetActive(false);
        }

        private Text CreateInputText(
            Transform parent,
            string objectName,
            Color color)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(parent, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 6f);
            textRect.offsetMax = new Vector2(-14f, -6f);

            Text text = textObject.GetComponent<Text>();
            text.font = _statusText.font;
            text.fontSize = 21;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
