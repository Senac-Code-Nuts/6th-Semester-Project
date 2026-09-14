using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PiGame.Networking
{
    public class NetcodeLobbyConnectionService : MonoBehaviour, ILobbyConnectionService
    {
        private enum SessionRole
        {
            None,
            Host,
            Client
        }

        [SerializeField] private NetworkManager _networkManager;
        [SerializeField, Min(1f)] private float _clientConnectionTimeoutSeconds = 8f;

        private bool _isDuplicate;
        private SessionRole _sessionRole;
        private Coroutine _connectionTimeoutRoutine;
        private bool _isStarting;
        private bool _wasConnected;
        private bool _terminationReported;
        private bool _callbacksRegistered;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<LobbyConnectionFailure> ConnectionFailed;

        public static NetcodeLobbyConnectionService Instance { get; private set; }
        public bool IsConnected => _networkManager != null && _networkManager.IsConnectedClient;
        public bool IsHost => _networkManager != null && _networkManager.IsHost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                _isDuplicate = true;
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _networkManager ??= GetComponent<NetworkManager>();
        }

        private void OnEnable()
        {
            if (_isDuplicate)
            {
                return;
            }

            RegisterCallbacks();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            StopConnectionTimeout();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartHost()
        {
            if (!TryBeginSession(SessionRole.Host))
            {
                return;
            }

            if (!_networkManager.StartHost())
            {
                ReportFailure(LobbyConnectionFailure.StartFailed, false);
            }
        }

        public void StartClient()
        {
            if (!TryBeginSession(SessionRole.Client))
            {
                return;
            }

            if (!_networkManager.StartClient())
            {
                ReportFailure(LobbyConnectionFailure.StartFailed, false);
                return;
            }

            _connectionTimeoutRoutine = StartCoroutine(ConnectionTimeoutRoutine());
        }

        public void Shutdown()
        {
            _terminationReported = true;
            _isStarting = false;
            _wasConnected = false;
            _sessionRole = SessionRole.None;
            StopConnectionTimeout();

            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }
        }

        private bool TryBeginSession(SessionRole role)
        {
            if (_networkManager == null)
            {
                ConnectionFailed?.Invoke(LobbyConnectionFailure.NetworkManagerUnavailable);
                return false;
            }

            RegisterCallbacks();

            if (_networkManager.IsListening || _isStarting)
            {
                ConnectionFailed?.Invoke(LobbyConnectionFailure.AlreadyRunning);
                return false;
            }

            StopConnectionTimeout();
            _sessionRole = role;
            _isStarting = true;
            _wasConnected = false;
            _terminationReported = false;
            return true;
        }

        private void RegisterCallbacks()
        {
            if (_networkManager == null || _callbacksRegistered)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback += HandleClientConnected;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            _networkManager.OnTransportFailure += HandleTransportFailure;
            _networkManager.OnServerStopped += HandleSessionStopped;
            _networkManager.OnClientStopped += HandleSessionStopped;
            _callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (_networkManager == null || !_callbacksRegistered)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback -= HandleClientConnected;
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            _networkManager.OnTransportFailure -= HandleTransportFailure;
            _networkManager.OnServerStopped -= HandleSessionStopped;
            _networkManager.OnClientStopped -= HandleSessionStopped;
            _callbacksRegistered = false;
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (_terminationReported || clientId != _networkManager.LocalClientId)
            {
                return;
            }

            StopConnectionTimeout();
            _isStarting = false;
            _wasConnected = true;
            Connected?.Invoke();
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_terminationReported || clientId != _networkManager.LocalClientId)
            {
                return;
            }

            if (_wasConnected)
            {
                ReportDisconnection();
                return;
            }

            LobbyConnectionFailure failure = _sessionRole == SessionRole.Client
                ? LobbyConnectionFailure.HostUnavailable
                : LobbyConnectionFailure.StartFailed;
            ReportFailure(failure, false);
        }

        private void HandleTransportFailure()
        {
            ReportFailure(LobbyConnectionFailure.TransportFailure, false);
        }

        private void HandleSessionStopped(bool isHost)
        {
            if (_terminationReported)
            {
                return;
            }

            if (_wasConnected)
            {
                ReportDisconnection();
                return;
            }

            if (_isStarting)
            {
                LobbyConnectionFailure failure = _sessionRole == SessionRole.Client
                    ? LobbyConnectionFailure.HostUnavailable
                    : LobbyConnectionFailure.StartFailed;
                ReportFailure(failure, false);
            }
        }

        private IEnumerator ConnectionTimeoutRoutine()
        {
            yield return new WaitForSecondsRealtime(_clientConnectionTimeoutSeconds);
            _connectionTimeoutRoutine = null;

            if (_isStarting && _sessionRole == SessionRole.Client)
            {
                ReportFailure(LobbyConnectionFailure.TimedOut, true);
            }
        }

        private void ReportDisconnection()
        {
            if (_terminationReported)
            {
                return;
            }

            _terminationReported = true;
            _isStarting = false;
            _wasConnected = false;
            _sessionRole = SessionRole.None;
            StopConnectionTimeout();
            Disconnected?.Invoke();
        }

        private void ReportFailure(LobbyConnectionFailure failure, bool shutdownNetwork)
        {
            if (_terminationReported)
            {
                return;
            }

            _terminationReported = true;
            _isStarting = false;
            _wasConnected = false;
            _sessionRole = SessionRole.None;
            StopConnectionTimeout();

            if (shutdownNetwork && _networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }

            ConnectionFailed?.Invoke(failure);
        }

        private void StopConnectionTimeout()
        {
            if (_connectionTimeoutRoutine == null)
            {
                return;
            }

            StopCoroutine(_connectionTimeoutRoutine);
            _connectionTimeoutRoutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _clientConnectionTimeoutSeconds = Mathf.Max(1f, _clientConnectionTimeoutSeconds);
        }
#endif
    }
}
