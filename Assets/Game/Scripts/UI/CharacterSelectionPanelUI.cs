using System;
using System.Collections;
using System.Collections.Generic;
using PiGame.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class CharacterSelectionPanelUI : MonoBehaviour,
        IMoveHandler,
        ISubmitHandler,
        ICancelHandler,
        IPointerEnterHandler
    {
        [Header("Characters")]
        [SerializeField] private LobbyCharacterDefinition[] _characters;

        [Header("Player slots")]
        [SerializeField] private RectTransform[] _slotRoots;
        [SerializeField] private Image[] _slotBackgrounds;
        [SerializeField] private Image[] _characterPortraits;
        [SerializeField] private Image[] _deviceIcons;
        [SerializeField] private Text[] _slotLabels;
        [SerializeField] private Text[] _statusTexts;

        [Header("Character navigation")]
        [SerializeField] private RectTransform _browseControlsRoot;
        [SerializeField] private Button _previousCharacterButton;
        [SerializeField] private Button _nextCharacterButton;

        [Header("Input icons")]
        [SerializeField] private Sprite _keyboardDeviceSprite;
        [SerializeField] private Sprite _gamepadDeviceSprite;
        [SerializeField] private Image _confirmLegendIcon;
        [SerializeField] private Image _backLegendIcon;
        [SerializeField] private Sprite _keyboardConfirmSprite;
        [SerializeField] private Sprite _keyboardBackSprite;
        [SerializeField] private Sprite _gamepadConfirmSprite;
        [SerializeField] private Sprite _gamepadBackSprite;
        [SerializeField] private Text _instructionText;

        public event Action<int, LobbyInputDeviceKind> BrowseRequested;
        public event Action<LobbyInputDeviceKind> SubmitRequested;
        public event Action BackRequested;

        private readonly Color[] _slotColors = new Color[4];
        private LobbyInputDeviceKind _lastInputDevice = LobbyInputDeviceKind.Keyboard;
        private bool _interactionEnabled = true;
        private int _localPlayerSlot = -1;
        private bool _localPlayerIsReady = true;

        private void Awake()
        {
            for (int i = 0; i < _slotColors.Length && i < _slotBackgrounds.Length; i++)
            {
                _slotColors[i] = _slotBackgrounds[i].color;
            }

            RefreshLegend();
        }

        private void OnEnable()
        {
            _previousCharacterButton.onClick.AddListener(HandlePreviousCharacterClicked);
            _nextCharacterButton.onClick.AddListener(HandleNextCharacterClicked);
            StartCoroutine(FocusNextFrame());
        }

        private void OnDisable()
        {
            _previousCharacterButton.onClick.RemoveListener(HandlePreviousCharacterClicked);
            _nextCharacterButton.onClick.RemoveListener(HandleNextCharacterClicked);
        }

        private void Update()
        {
            LobbyInputDeviceKind detectedDevice = DetectRecentlyUsedDevice();
            if (detectedDevice != LobbyInputDeviceKind.Unknown)
            {
                SetLastInputDevice(detectedDevice);
            }
        }

        public void OnMove(AxisEventData eventData)
        {
            if (!_interactionEnabled || Mathf.Abs(eventData.moveVector.y) < 0.5f)
            {
                return;
            }

            LobbyInputDeviceKind inputDevice = ResolveInputDevice();
            SetLastInputDevice(inputDevice);
            BrowseRequested?.Invoke(eventData.moveVector.y > 0f ? -1 : 1, inputDevice);
            eventData.Use();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!_interactionEnabled)
            {
                return;
            }

            LobbyInputDeviceKind inputDevice = ResolveInputDevice();
            SetLastInputDevice(inputDevice);
            SubmitRequested?.Invoke(inputDevice);
            eventData.Use();
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (!_interactionEnabled)
            {
                return;
            }

            SetLastInputDevice(ResolveInputDevice());
            BackRequested?.Invoke();
            eventData.Use();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Focus();
        }

        public void Render(IReadOnlyList<LobbyPlayerData> players, ulong localClientId)
        {
            _localPlayerSlot = -1;
            _localPlayerIsReady = true;

            for (int slot = 0; slot < _slotRoots.Length; slot++)
            {
                if (TryFindPlayerInSlot(players, slot, out LobbyPlayerData player))
                {
                    bool isLocalPlayer = player.ClientId == localClientId;
                    if (isLocalPlayer && player.InputDevice != LobbyInputDeviceKind.Unknown)
                    {
                        SetLastInputDevice(player.InputDevice);
                    }

                    if (isLocalPlayer)
                    {
                        _localPlayerSlot = slot;
                        _localPlayerIsReady = player.IsReady;
                    }

                    RenderConnectedSlot(slot, player, isLocalPlayer);
                }
                else
                {
                    RenderEmptySlot(slot);
                }
            }

            RefreshBrowseControls();
            Focus();
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _interactionEnabled = isEnabled;
            RefreshBrowseControls();
            if (isEnabled)
            {
                Focus();
            }
        }

        private void HandlePreviousCharacterClicked()
        {
            HandleBrowseButtonClicked(-1);
        }

        private void HandleNextCharacterClicked()
        {
            HandleBrowseButtonClicked(1);
        }

        private void HandleBrowseButtonClicked(int direction)
        {
            if (!_interactionEnabled)
            {
                return;
            }

            SetLastInputDevice(LobbyInputDeviceKind.Keyboard);
            BrowseRequested?.Invoke(direction, LobbyInputDeviceKind.Keyboard);
            Focus();
        }

        private void RefreshBrowseControls()
        {
            bool shouldShow = _interactionEnabled
                && _localPlayerSlot >= 0
                && !_localPlayerIsReady;

            _browseControlsRoot.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                _browseControlsRoot.anchoredPosition = _slotRoots[_localPlayerSlot].anchoredPosition;
            }
        }

        public void Focus()
        {
            if (_interactionEnabled && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        private void RenderConnectedSlot(int slot, LobbyPlayerData player, bool isLocalPlayer)
        {
            LobbyCharacterDefinition character = FindCharacter(player.CharacterId);
            _slotRoots[slot].localScale = isLocalPlayer && !player.IsReady
                ? Vector3.one * 1.08f
                : Vector3.one;

            Color slotColor = character != null ? character.Color : _slotColors[slot];
            slotColor.a = isLocalPlayer ? 1f : 0.88f;
            _slotBackgrounds[slot].color = slotColor;

            Image portrait = _characterPortraits[slot];
            portrait.gameObject.SetActive(true);
            portrait.sprite = character != null ? character.Portrait : null;
            portrait.color = character != null && character.Portrait == null
                ? Color.Lerp(character.Color, Color.black, 0.38f)
                : Color.white;

            string characterName = character != null ? character.DisplayName : "ESCOLHENDO";
            _slotLabels[slot].text = characterName.ToUpperInvariant();

            Image deviceIcon = _deviceIcons[slot];
            Sprite deviceSprite = GetDeviceSprite(player.InputDevice);
            deviceIcon.gameObject.SetActive(deviceSprite != null);
            deviceIcon.sprite = deviceSprite;

            Text statusText = _statusTexts[slot];
            if (player.IsReady)
            {
                statusText.text = $"<size=15>P{slot + 1}</size>\nPRONTO";
                statusText.fontSize = 28;
                statusText.color = new Color(0.42f, 1f, 0.58f, 1f);
            }
            else
            {
                statusText.text = $"<size=14>P{slot + 1}</size>\nESCOLHENDO...";
                statusText.fontSize = isLocalPlayer ? 18 : 16;
                statusText.color = isLocalPlayer
                    ? Color.white
                    : new Color(0.78f, 0.78f, 0.84f, 1f);
            }
        }

        private void RenderEmptySlot(int slot)
        {
            _slotRoots[slot].localScale = Vector3.one;
            Color emptyColor = _slotColors[slot];
            emptyColor.a = 0.28f;
            _slotBackgrounds[slot].color = emptyColor;
            _characterPortraits[slot].gameObject.SetActive(false);
            _deviceIcons[slot].gameObject.SetActive(false);
            _slotLabels[slot].text = "AGUARDANDO";
            _statusTexts[slot].text = $"<size=14>P{slot + 1}</size>\nAGUARDANDO";
            _statusTexts[slot].fontSize = 16;
            _statusTexts[slot].color = new Color(0.58f, 0.58f, 0.64f, 1f);
        }

        private void SetLastInputDevice(LobbyInputDeviceKind inputDevice)
        {
            if (inputDevice == LobbyInputDeviceKind.Unknown || _lastInputDevice == inputDevice)
            {
                return;
            }

            _lastInputDevice = inputDevice;
            RefreshLegend();
        }

        private void RefreshLegend()
        {
            bool usesGamepad = _lastInputDevice == LobbyInputDeviceKind.Gamepad;
            if (_confirmLegendIcon != null)
            {
                _confirmLegendIcon.sprite = usesGamepad ? _gamepadConfirmSprite : _keyboardConfirmSprite;
            }

            if (_backLegendIcon != null)
            {
                _backLegendIcon.sprite = usesGamepad ? _gamepadBackSprite : _keyboardBackSprite;
            }

            if (_instructionText != null)
            {
                _instructionText.text = "CONFIRMAR / PRONTO                 VOLTAR";
            }
        }

        private Sprite GetDeviceSprite(LobbyInputDeviceKind inputDevice)
        {
            return inputDevice switch
            {
                LobbyInputDeviceKind.Keyboard => _keyboardDeviceSprite,
                LobbyInputDeviceKind.Gamepad => _gamepadDeviceSprite,
                _ => null
            };
        }

        private LobbyCharacterDefinition FindCharacter(LobbyCharacterId characterId)
        {
            for (int i = 0; i < _characters.Length; i++)
            {
                if (_characters[i] != null && _characters[i].Id == characterId)
                {
                    return _characters[i];
                }
            }

            return null;
        }

        private static bool TryFindPlayerInSlot(
            IReadOnlyList<LobbyPlayerData> players,
            int slot,
            out LobbyPlayerData player)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerSlot == slot)
                {
                    player = players[i];
                    return true;
                }
            }

            player = default;
            return false;
        }

        private static LobbyInputDeviceKind ResolveInputDevice()
        {
            if (Gamepad.current != null
                && (Gamepad.current.buttonSouth.wasPressedThisFrame
                    || Gamepad.current.buttonEast.wasPressedThisFrame
                    || Gamepad.current.dpad.IsPressed()
                    || Mathf.Abs(Gamepad.current.leftStick.y.ReadValue()) > 0.5f))
            {
                return LobbyInputDeviceKind.Gamepad;
            }

            return LobbyInputDeviceKind.Keyboard;
        }

        private static LobbyInputDeviceKind DetectRecentlyUsedDevice()
        {
            if (Gamepad.current != null
                && (Gamepad.current.buttonSouth.wasPressedThisFrame
                    || Gamepad.current.buttonEast.wasPressedThisFrame
                    || Gamepad.current.dpad.IsPressed()
                    || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.36f))
            {
                return LobbyInputDeviceKind.Gamepad;
            }

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return LobbyInputDeviceKind.Keyboard;
            }

            return LobbyInputDeviceKind.Unknown;
        }

        private IEnumerator FocusNextFrame()
        {
            yield return null;
            Focus();
        }
    }
}
