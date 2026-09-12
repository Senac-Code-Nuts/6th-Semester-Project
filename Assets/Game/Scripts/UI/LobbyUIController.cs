using PiGame.Networking;
using UnityEngine;

namespace PiGame.UI
{
    public class LobbyUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private ConnectionPanelUI _connectionPanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private ConfirmationPanelUI _quitConfirmationPanel;

        [Header("Services")]
        [SerializeField] private NetcodeLobbyConnectionService _connectionService;

        private ILobbyConnectionService _connectionServiceContract;

        private void Awake()
        {
            _connectionServiceContract = _connectionService;
        }

        private void OnEnable()
        {
            _connectionPanel.HostRequested += HandleHostRequested;
            _connectionPanel.ClientRequested += HandleClientRequested;
            _connectionPanel.CancelConnectionRequested += HandleCancelConnectionRequested;
            _connectionPanel.QuitRequested += HandleQuitRequested;
            _quitConfirmationPanel.Confirmed += HandleQuitConfirmed;
            _quitConfirmationPanel.Canceled += HandleQuitCanceled;
            _quitConfirmationPanel.gameObject.SetActive(false);

            if (_connectionServiceContract == null)
            {
                ShowConnectionError("SERVICO DE REDE NAO CONFIGURADO");
                Debug.LogError("LobbyUIController requires a connection service reference.", this);
                return;
            }

            _connectionServiceContract.Connected += HandleConnected;
            _connectionServiceContract.Disconnected += HandleDisconnected;
            _connectionServiceContract.ConnectionFailed += HandleConnectionFailed;

            if (_connectionServiceContract.IsConnected)
            {
                ShowLobbyPanel();
                return;
            }

            ShowConnectionPanel();
            _connectionPanel.ShowIdle();
        }

        private void OnDisable()
        {
            _connectionPanel.HostRequested -= HandleHostRequested;
            _connectionPanel.ClientRequested -= HandleClientRequested;
            _connectionPanel.CancelConnectionRequested -= HandleCancelConnectionRequested;
            _connectionPanel.QuitRequested -= HandleQuitRequested;
            _quitConfirmationPanel.Confirmed -= HandleQuitConfirmed;
            _quitConfirmationPanel.Canceled -= HandleQuitCanceled;

            if (_connectionServiceContract == null)
            {
                return;
            }

            _connectionServiceContract.Connected -= HandleConnected;
            _connectionServiceContract.Disconnected -= HandleDisconnected;
            _connectionServiceContract.ConnectionFailed -= HandleConnectionFailed;
        }

        private void HandleHostRequested()
        {
            if (_connectionServiceContract == null)
            {
                ShowConnectionError("SERVICO DE REDE NAO CONFIGURADO");
                return;
            }

            _connectionPanel.ShowConnecting("INICIANDO HOST...");
            _connectionServiceContract.StartHost();
        }

        private void HandleClientRequested()
        {
            if (_connectionServiceContract == null)
            {
                ShowConnectionError("SERVICO DE REDE NAO CONFIGURADO");
                return;
            }

            _connectionPanel.ShowConnecting("CONECTANDO AO HOST...");
            _connectionServiceContract.StartClient();
        }

        private void HandleCancelConnectionRequested()
        {
            _connectionServiceContract?.Shutdown();
            _connectionPanel.ShowIdle();
        }

        private void HandleQuitRequested()
        {
            _quitConfirmationPanel.gameObject.SetActive(true);
        }

        private static void HandleQuitConfirmed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleQuitCanceled()
        {
            _quitConfirmationPanel.gameObject.SetActive(false);
            _connectionPanel.FocusDefaultButton();
        }

        private void HandleConnected()
        {
            ShowLobbyPanel();
        }

        private void HandleDisconnected()
        {
            ShowConnectionError("CONEXAO ENCERRADA");
        }

        private void HandleConnectionFailed(LobbyConnectionFailure failure)
        {
            string message = failure switch
            {
                LobbyConnectionFailure.NetworkManagerUnavailable => "NETWORK MANAGER NAO CONFIGURADO",
                LobbyConnectionFailure.AlreadyRunning => "UMA CONEXAO JA ESTA ATIVA",
                LobbyConnectionFailure.HostUnavailable => "HOST NAO ENCONTRADO",
                LobbyConnectionFailure.TimedOut => "TEMPO DE CONEXAO ESGOTADO",
                LobbyConnectionFailure.TransportFailure => "FALHA NO TRANSPORTE DE REDE",
                _ => "NAO FOI POSSIVEL INICIAR A CONEXAO"
            };

            ShowConnectionError(message);
        }

        private void ShowConnectionPanel()
        {
            _quitConfirmationPanel.gameObject.SetActive(false);
            _connectionPanel.gameObject.SetActive(true);
            _lobbyPanel.SetActive(false);
        }

        private void ShowLobbyPanel()
        {
            _connectionPanel.gameObject.SetActive(false);
            _lobbyPanel.SetActive(true);
        }

        private void ShowConnectionError(string message)
        {
            ShowConnectionPanel();
            _connectionPanel.ShowError(message);
        }
    }
}
