using PiGame.Lobby;
using PiGame.Networking;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
        [SerializeField] private LobbyMapVotingController _mapVotingController;
        [SerializeField] private ConfirmationPanelUI _confirmationPanel;
        [SerializeField] private MatchSettingsPanelUI _matchSettingsPanel;
        [FormerlySerializedAs("_localNetworkAddressText")]
        [SerializeField] private Text _sessionCodeText;

        [Header("Services")]
        [SerializeField] private NetcodeLobbyConnectionService _connectionService;

        private ILobbyConnectionService _connectionServiceContract;
        private ConfirmationAction _pendingConfirmation;

        private void Awake()
        {
            if (NetcodeLobbyConnectionService.Instance != null)
            {
                _connectionService = NetcodeLobbyConnectionService.Instance;
            }

            _connectionServiceContract = _connectionService;
        }

        private void OnEnable()
        {
            _connectionPanel.HostRequested += HandleHostRequested;
            _connectionPanel.ClientRequested += HandleClientRequested;
            _connectionPanel.CancelConnectionRequested += HandleCancelConnectionRequested;
            _connectionPanel.QuitRequested += HandleQuitRequested;
            _characterSelectionController.DisconnectRequested += HandleDisconnectRequested;
            _mapVotingController.DisconnectRequested += HandleDisconnectRequested;
            _mapVotingController.VisibilityChanged += HandleMapVotingVisibilityChanged;
            _confirmationPanel.Confirmed += HandleConfirmationConfirmed;
            _confirmationPanel.Canceled += HandleConfirmationCanceled;
            _matchSettingsPanel.Opened += HandleMatchSettingsOpened;
            _matchSettingsPanel.Closed += HandleMatchSettingsClosed;
            _confirmationPanel.Hide();

            if (_connectionServiceContract == null)
            {
                ShowConnectionError("SERVIÇO DE REDE NÃO CONFIGURADO");
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
            _mapVotingController.DisconnectRequested -= HandleDisconnectRequested;
            _mapVotingController.VisibilityChanged -= HandleMapVotingVisibilityChanged;
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

        private async void HandleHostRequested()
        {
            if (_connectionServiceContract == null)
            {
                ShowConnectionError("SERVIÇO DE REDE NÃO CONFIGURADO");
                return;
            }

            _connectionPanel.ShowConnecting("CRIANDO SALA...");
            await _connectionServiceContract.StartHostAsync();
        }

        private async void HandleClientRequested(string joinCode)
        {
            if (_connectionServiceContract == null)
            {
                ShowConnectionError("SERVIÇO DE REDE NÃO CONFIGURADO");
                return;
            }

            _connectionPanel.ShowConnecting("ENTRANDO NA SALA...");
            await _connectionServiceContract.StartClientAsync(joinCode);
        }

        private async void HandleCancelConnectionRequested()
        {
            if (_connectionServiceContract != null)
            {
                await _connectionServiceContract.ShutdownAsync();
            }

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

        private async void HandleConfirmationConfirmed()
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
            ShowConnectionPanel();
            _connectionPanel.ShowConnecting("ENCERRANDO SALA...");
            if (_connectionServiceContract != null)
            {
                await _connectionServiceContract.ShutdownAsync();
            }

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
                SetLobbyInteractionEnabled(true);
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
            ShowConnectionError("CONEXÃO ENCERRADA");
        }

        private void HandleConnectionFailed(LobbyConnectionFailure failure)
        {
            string message = failure switch
            {
                LobbyConnectionFailure.NetworkManagerUnavailable => "NETWORK MANAGER NÃO CONFIGURADO",
                LobbyConnectionFailure.AlreadyRunning => "UMA CONEXÃO JÁ ESTÁ ATIVA",
                LobbyConnectionFailure.HostUnavailable => "HOST NÃO ENCONTRADO",
                LobbyConnectionFailure.TransportFailure => "FALHA NO TRANSPORTE DE REDE",
                LobbyConnectionFailure.ServicesUnavailable => "SERVIÇOS UNITY INDISPONÍVEIS",
                LobbyConnectionFailure.AuthenticationFailed => "FALHA NA AUTENTICAÇÃO",
                LobbyConnectionFailure.InvalidJoinCode => "CÓDIGO INVÁLIDO",
                LobbyConnectionFailure.SessionNotFound => "SALA NÃO ENCONTRADA",
                LobbyConnectionFailure.SessionConflict => "SESSÃO ANTERIOR AINDA ATIVA",
                _ => "NÃO FOI POSSÍVEL INICIAR A CONEXÃO"
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
            _matchSettingsPanel.SetMenuVisible(
                _connectionServiceContract.IsHost && !_mapVotingController.IsVisible);
            RefreshSessionCode();
            SetLobbyInteractionEnabled(true);
        }

        private void RefreshSessionCode()
        {
            if (_sessionCodeText == null || _connectionServiceContract == null)
            {
                return;
            }

            bool showCode = _connectionServiceContract.IsHost
                && !string.IsNullOrWhiteSpace(_connectionServiceContract.JoinCode);
            _sessionCodeText.gameObject.SetActive(showCode);
            if (showCode)
            {
                _sessionCodeText.text =
                    $"CÓDIGO DA SALA: <size=26>"
                    + $"{_connectionServiceContract.JoinCode}</size>";
            }
        }

        private void HandleMatchSettingsOpened()
        {
            SetLobbyInteractionEnabled(false);
        }

        private void HandleMatchSettingsClosed()
        {
            SetLobbyInteractionEnabled(true);
        }

        private void HandleMapVotingVisibilityChanged(bool isVisible)
        {
            _matchSettingsPanel.SetMenuVisible(
                !isVisible
                && _connectionServiceContract != null
                && _connectionServiceContract.IsHost);
            SetLobbyInteractionEnabled(true);
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
                SetLobbyInteractionEnabled(false);
            }
            else
            {
                _connectionPanel.SetInteractionEnabled(false);
            }

            _confirmationPanel.Show(message);
        }

        private void SetLobbyInteractionEnabled(bool isEnabled)
        {
            bool mapVotingIsVisible = _mapVotingController.IsVisible;
            _characterSelectionController.SetInteractionEnabled(isEnabled && !mapVotingIsVisible);
            _mapVotingController.SetInteractionEnabled(isEnabled && mapVotingIsVisible);
        }
    }
}
