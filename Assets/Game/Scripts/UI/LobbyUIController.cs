using PiGame.Lobby;
using PiGame.Networking;
using UnityEngine;

namespace PiGame.UI
{
    public class LobbyUIController : MonoBehaviour
    {
        private enum ConfirmationAction
        {
            None,
            Quit,
            Disconnect
        }

        [Header("Panels")]
        [SerializeField] private ConnectionPanelUI _connectionPanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private LobbyCharacterSelectionController _characterSelectionController;
        [SerializeField] private ConfirmationPanelUI _confirmationPanel;
        [SerializeField] private MatchSettingsPanelUI _matchSettingsPanel;

        [Header("Services")]
        [SerializeField] private NetcodeLobbyConnectionService _connectionService;

        private ILobbyConnectionService _connectionServiceContract;
        private ConfirmationAction _pendingConfirmation;

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
            _characterSelectionController.DisconnectRequested += HandleDisconnectRequested;
            _confirmationPanel.Confirmed += HandleConfirmationConfirmed;
            _confirmationPanel.Canceled += HandleConfirmationCanceled;
            _matchSettingsPanel.Opened += HandleMatchSettingsOpened;
            _matchSettingsPanel.Closed += HandleMatchSettingsClosed;
            _confirmationPanel.Hide();

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
            _characterSelectionController.DisconnectRequested -= HandleDisconnectRequested;
            _confirmationPanel.Confirmed -= HandleConfirmationConfirmed;
            _confirmationPanel.Canceled -= HandleConfirmationCanceled;
            _matchSettingsPanel.Opened -= HandleMatchSettingsOpened;
            _matchSettingsPanel.Closed -= HandleMatchSettingsClosed;

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
            OpenConfirmation(ConfirmationAction.Quit, "DESEJA REALMENTE SAIR?");
        }

        private void HandleDisconnectRequested()
        {
            string message = _connectionServiceContract != null && _connectionServiceContract.IsHost
                ? "ENCERRAR LOBBY?"
                : "DESCONECTAR DO LOBBY?";
            OpenConfirmation(ConfirmationAction.Disconnect, message);
        }

        private void HandleConfirmationConfirmed()
        {
            ConfirmationAction confirmedAction = _pendingConfirmation;
            _pendingConfirmation = ConfirmationAction.None;

            if (confirmedAction == ConfirmationAction.Quit)
            {
                QuitApplication();
                return;
            }

            if (confirmedAction != ConfirmationAction.Disconnect)
            {
                return;
            }

            _confirmationPanel.Hide();
            _connectionServiceContract?.Shutdown();
            ShowConnectionPanel();
            _connectionPanel.ShowIdle();
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleConfirmationCanceled()
        {
            ConfirmationAction canceledAction = _pendingConfirmation;
            _pendingConfirmation = ConfirmationAction.None;
            _confirmationPanel.Hide();

            if (canceledAction == ConfirmationAction.Disconnect)
            {
                _characterSelectionController.SetInteractionEnabled(true);
                return;
            }

            _connectionPanel.SetInteractionEnabled(true);
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
            _pendingConfirmation = ConfirmationAction.None;
            _confirmationPanel.Hide();
            _matchSettingsPanel.ResetView();
            _connectionPanel.gameObject.SetActive(true);
            _connectionPanel.SetInteractionEnabled(true);
            _lobbyPanel.SetActive(false);
        }

        private void ShowLobbyPanel()
        {
            _pendingConfirmation = ConfirmationAction.None;
            _confirmationPanel.Hide();
            _connectionPanel.gameObject.SetActive(false);
            _lobbyPanel.SetActive(true);
            _matchSettingsPanel.ResetView();
            _matchSettingsPanel.SetMenuVisible(_connectionServiceContract.IsHost);
            _characterSelectionController.SetInteractionEnabled(true);
        }

        private void HandleMatchSettingsOpened()
        {
            _characterSelectionController.SetInteractionEnabled(false);
        }

        private void HandleMatchSettingsClosed()
        {
            _characterSelectionController.SetInteractionEnabled(true);
        }

        private void ShowConnectionError(string message)
        {
            ShowConnectionPanel();
            _connectionPanel.ShowError(message);
        }

        private void OpenConfirmation(ConfirmationAction action, string message)
        {
            _pendingConfirmation = action;

            if (action == ConfirmationAction.Disconnect)
            {
                _characterSelectionController.SetInteractionEnabled(false);
            }
            else
            {
                _connectionPanel.SetInteractionEnabled(false);
            }

            _confirmationPanel.Show(message);
        }
    }
}
