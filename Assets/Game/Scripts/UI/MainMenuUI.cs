using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _connectionPanel;
        [SerializeField] private GameObject _lobbyPanel;

        [Header("Connection menu")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Text _statusText;

        private Coroutine _selectionRoutine;

        private void Awake()
        {
            _quitButton.onClick.AddListener(QuitGame);
        }

        private void OnEnable()
        {
            ShowConnectionPanel();
            _selectionRoutine = StartCoroutine(SelectHostNextFrame());
        }

        private void OnDisable()
        {
            if (_selectionRoutine != null)
            {
                StopCoroutine(_selectionRoutine);
                _selectionRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(QuitGame);
            }
        }

        public void ShowConnectionPanel()
        {
            _connectionPanel.SetActive(true);
            _lobbyPanel.SetActive(false);
            SetStatus("ESCOLHA HOST OU CLIENTE");
        }

        public void SetStatus(string message)
        {
            _statusText.text = message;
        }

        private IEnumerator SelectHostNextFrame()
        {
            yield return null;
            EventSystem.current?.SetSelectedGameObject(_hostButton.gameObject);
            _selectionRoutine = null;
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
