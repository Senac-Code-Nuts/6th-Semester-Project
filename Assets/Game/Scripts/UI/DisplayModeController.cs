using UnityEngine;
using UnityEngine.InputSystem;

namespace PiGame.UI
{
    public class DisplayModeController : MonoBehaviour
    {
        private const int DefaultWindowWidth = 960;
        private const int DefaultWindowHeight = 540;

        private static DisplayModeController _instance;

        private Vector2Int _windowedResolution =
            new Vector2Int(DefaultWindowWidth, DefaultWindowHeight);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null)
            {
                return;
            }

            GameObject controllerObject = new GameObject(nameof(DisplayModeController));
            _instance = controllerObject.AddComponent<DisplayModeController>();
            DontDestroyOnLoad(controllerObject);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                _windowedResolution = new Vector2Int(Screen.width, Screen.height);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool pressedF11 = keyboard.f11Key.wasPressedThisFrame;
            bool pressedAltEnter = keyboard.enterKey.wasPressedThisFrame
                && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);

            if (pressedF11 || pressedAltEnter)
            {
                ToggleFullscreen();
            }
        }

        public void ToggleFullscreen()
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                _windowedResolution = new Vector2Int(Screen.width, Screen.height);
                Screen.SetResolution(
                    Display.main.systemWidth,
                    Display.main.systemHeight,
                    FullScreenMode.FullScreenWindow);
                return;
            }

            Screen.SetResolution(
                _windowedResolution.x,
                _windowedResolution.y,
                FullScreenMode.Windowed);
        }
    }
}
