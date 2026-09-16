using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PiGame.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private PauseMenuUI _view;
        [SerializeField] private MonoBehaviour _contextSource;
        [SerializeField] private PauseMenuDefinition _definition;

        private IPauseContext _context;
        private PauseInputDevice _currentDevice = PauseInputDevice.KeyboardMouse;
        private bool _isOpen;
        private bool _isExiting;

        private void Awake()
        {
            _view ??= GetComponent<PauseMenuUI>();
            _contextSource ??= GetComponent<MonoBehaviour>();
            _context = _contextSource as IPauseContext;

            if (_context == null)
            {
                foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
                {
                    if (behaviour is IPauseContext pauseContext)
                    {
                        _context = pauseContext;
                        _contextSource = behaviour;
                        break;
                    }
                }
            }

            _view?.Initialize(_definition);
        }

        private void OnEnable()
        {
            if (_view == null)
            {
                return;
            }

            _view.ResumeRequested += ClosePause;
            _view.ExitRequested += ShowExitConfirmation;
            _view.ExitConfirmed += HandleExitConfirmed;
            _view.ExitCanceled += HideExitConfirmation;
        }

        private void Start()
        {
            if (_view == null || _context == null || _definition == null)
            {
                Debug.LogError("PauseMenuController não está configurado corretamente.", this);
                enabled = false;
                return;
            }

            _view.Initialize(_definition);
            _view.SetVisible(false);
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.ResumeRequested -= ClosePause;
                _view.ExitRequested -= ShowExitConfirmation;
                _view.ExitConfirmed -= HandleExitConfirmed;
                _view.ExitCanceled -= HideExitConfirmation;
            }

            if (_isOpen && !_isExiting)
            {
                _context?.ExitPause();
            }

            _isOpen = false;
        }

        private void Update()
        {
            if (_isExiting)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetInputDevice(PauseInputDevice.KeyboardMouse);
                HandleBackOrToggle();
                return;
            }

            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                SetInputDevice(PauseInputDevice.Gamepad);
                HandleBackOrToggle();
                return;
            }

            if (!_isOpen)
            {
                return;
            }

            RefreshInputDevice();

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                if (_view.IsConfirmationVisible)
                {
                    HideExitConfirmation();
                }
                else
                {
                    ClosePause();
                }
            }
        }

        private void HandleBackOrToggle()
        {
            if (!_isOpen)
            {
                OpenPause();
                return;
            }

            if (_view.IsConfirmationVisible)
            {
                HideExitConfirmation();
                return;
            }

            ClosePause();
        }

        private void OpenPause()
        {
            if (_isOpen || _context == null || !_context.CanOpenPause)
            {
                return;
            }

            _isOpen = true;
            _context.EnterPause();
            _view.SetInputDevice(_currentDevice);
            _view.SetVisible(true);
        }

        private void ClosePause()
        {
            if (!_isOpen || _isExiting)
            {
                return;
            }

            _isOpen = false;
            _view.SetVisible(false);
            _context.ExitPause();
        }

        private void ShowExitConfirmation()
        {
            if (_isOpen && !_isExiting)
            {
                _view.ShowConfirmation();
            }
        }

        private void HideExitConfirmation()
        {
            if (_isOpen && !_isExiting)
            {
                _view.HideConfirmation();
            }
        }

        private async void HandleExitConfirmed()
        {
            if (!_isOpen || _isExiting || _context == null)
            {
                return;
            }

            _isExiting = true;
            _view.SetBusy(true);

            try
            {
                await _context.ExitContextAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                _isExiting = false;
                _view.SetBusy(false);
                _view.HideConfirmation();
            }
        }

        private void RefreshInputDevice()
        {
            if (WasGamepadUsed())
            {
                SetInputDevice(PauseInputDevice.Gamepad);
                return;
            }

            if (WasKeyboardOrMouseUsed())
            {
                SetInputDevice(PauseInputDevice.KeyboardMouse);
            }
        }

        private void SetInputDevice(PauseInputDevice device)
        {
            if (_currentDevice == device)
            {
                return;
            }

            _currentDevice = device;
            if (_isOpen)
            {
                _view.SetInputDevice(device);
            }
        }

        private static bool WasKeyboardOrMouseUsed()
        {
            bool keyboardUsed = Keyboard.current != null
                && Keyboard.current.anyKey.wasPressedThisFrame;
            if (keyboardUsed || Mouse.current == null)
            {
                return keyboardUsed;
            }

            return Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame
                || Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f;
        }

        private static bool WasGamepadUsed()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return false;
            }

            return gamepad.buttonSouth.wasPressedThisFrame
                || gamepad.buttonNorth.wasPressedThisFrame
                || gamepad.buttonWest.wasPressedThisFrame
                || gamepad.buttonEast.wasPressedThisFrame
                || gamepad.leftShoulder.wasPressedThisFrame
                || gamepad.rightShoulder.wasPressedThisFrame
                || gamepad.dpad.IsPressed()
                || gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f
                || gamepad.rightStick.ReadValue().sqrMagnitude > 0.25f
                || gamepad.leftTrigger.ReadValue() > 0.25f
                || gamepad.rightTrigger.ReadValue() > 0.25f;
        }
    }
}
