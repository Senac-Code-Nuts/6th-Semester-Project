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
            StartCoroutine(FocusNextFrame());
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
            if (!_interactionEnabled || Mathf.Abs(eventData.moveVector.x) < 0.5f)
            {
                return;
            }

            LobbyInputDeviceKind inputDevice = ResolveInputDevice();
            SetLastInputDevice(inputDevice);
            BrowseRequested?.Invoke(eventData.moveVector.x > 0f ? 1 : -1, inputDevice);
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
            for (int slot = 0; slot < _slotRoots.Length; slot++)
            {
                if (TryFindPlayerInSlot(players, slot, out LobbyPlayerData player))
                {
                    bool isLocalPlayer = player.ClientId == localClientId;
                    if (isLocalPlayer && player.InputDevice != LobbyInputDeviceKind.Unknown)
                    {
                        SetLastInputDevice(player.InputDevice);
                    }

                    RenderConnectedSlot(slot, player, isLocalPlayer);
                }
                else
                {
                    RenderEmptySlot(slot);
                }
            }

            Focus();
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _interactionEnabled = isEnabled;
            if (isEnabled)
            {
                Focus();
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

            Color slotColor = _slotColors[slot];
            slotColor.a = isLocalPlayer ? 1f : 0.88f;
            _slotBackgrounds[slot].color = slotColor;

            Image portrait = _characterPortraits[slot];
            portrait.gameObject.SetActive(true);
            portrait.sprite = character != null ? character.Portrait : null;
            portrait.color = character != null && character.Portrait == null
                ? character.Color
                : Color.white;

            string characterName = character != null ? character.DisplayName : "ESCOLHENDO";
            _slotLabels[slot].text = $"P{slot + 1}\n{characterName.ToUpperInvariant()}";

            Image deviceIcon = _deviceIcons[slot];
            Sprite deviceSprite = GetDeviceSprite(player.InputDevice);
            deviceIcon.gameObject.SetActive(deviceSprite != null);
            deviceIcon.sprite = deviceSprite;

            Text statusText = _statusTexts[slot];
            if (player.IsReady)
            {
                statusText.text = "PRONTO";
                statusText.fontSize = 28;
                statusText.color = new Color(0.42f, 1f, 0.58f, 1f);
            }
            else
            {
                statusText.text = "ESCOLHENDO...";
                statusText.fontSize = isLocalPlayer ? 16 : 14;
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
            _slotLabels[slot].text = $"P{slot + 1}\nAGUARDANDO";
            _statusTexts[slot].text = "AGUARDANDO JOGADOR";
            _statusTexts[slot].fontSize = 12;
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
                    || Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > 0.5f))
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
