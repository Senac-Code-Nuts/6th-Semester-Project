using System;
using System.Collections.Generic;
using UnityEngine;

namespace PiGame.UI
{
    public enum PauseInputDevice
    {
        KeyboardMouse,
        Gamepad
    }

    [Serializable]
    public struct PauseControlPrompt
    {
        [SerializeField] private string _label;
        [SerializeField] private Sprite _primaryIcon;
        [SerializeField] private Sprite _secondaryIcon;

        public string Label => _label;
        public Sprite PrimaryIcon => _primaryIcon;
        public Sprite SecondaryIcon => _secondaryIcon;
    }

    [CreateAssetMenu(
        fileName = "data_pause_menu",
        menuName = "Pi Game/UI/Pause Menu Definition")]
    public class PauseMenuDefinition : ScriptableObject
    {
        [Header("Textos")]
        [SerializeField] private string _title = "PAUSE";
        [SerializeField] private string _resumeLabel = "VOLTAR";
        [SerializeField] private string _exitLabel = "SAIR";
        [SerializeField] private string _exitConfirmationMessage = "DESEJA SAIR?";
        [SerializeField] private string _keyboardTitle = "TECLADO E MOUSE";
        [SerializeField] private string _gamepadTitle = "CONTROLE";

        [Header("Controles")]
        [SerializeField] private PauseControlPrompt[] _keyboardPrompts =
            Array.Empty<PauseControlPrompt>();
        [SerializeField] private PauseControlPrompt[] _gamepadPrompts =
            Array.Empty<PauseControlPrompt>();

        public string Title => _title;
        public string ResumeLabel => _resumeLabel;
        public string ExitLabel => _exitLabel;
        public string ExitConfirmationMessage => _exitConfirmationMessage;

        public string GetDeviceTitle(PauseInputDevice device)
        {
            return device == PauseInputDevice.Gamepad
                ? _gamepadTitle
                : _keyboardTitle;
        }

        public IReadOnlyList<PauseControlPrompt> GetPrompts(PauseInputDevice device)
        {
            return device == PauseInputDevice.Gamepad
                ? _gamepadPrompts
                : _keyboardPrompts;
        }
    }
}
