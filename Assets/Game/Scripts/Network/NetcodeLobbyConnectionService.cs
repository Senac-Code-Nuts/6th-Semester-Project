using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
        private UnityTransport _transport;
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
        public string LocalAddress => FindLocalIpv4Address();

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
            _transport = _networkManager != null
                ? _networkManager.GetComponent<UnityTransport>()
                : null;
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

            if (_transport == null)
            {
                ReportFailure(LobbyConnectionFailure.NetworkManagerUnavailable, false);
                return;
            }

            ushort port = _transport.ConnectionData.Port;
            _transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");

            if (!_networkManager.StartHost())
            {
                ReportFailure(LobbyConnectionFailure.StartFailed, false);
            }
        }

        public void StartClient(string address)
        {
            if (!TryBeginSession(SessionRole.Client))
            {
                return;
            }

            if (_transport == null || !TryNormalizeIpv4(address, out string normalizedAddress))
            {
                ReportFailure(LobbyConnectionFailure.InvalidAddress, false);
                return;
            }

            _transport.SetConnectionData(
                normalizedAddress,
                _transport.ConnectionData.Port);

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

        private static bool TryNormalizeIpv4(string address, out string normalizedAddress)
        {
            normalizedAddress = string.Empty;
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string[] parts = address.Trim().Split('.');
            if (parts.Length != 4)
            {
                return false;
            }

            int[] octets = new int[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!byte.TryParse(parts[i], out byte octet))
                {
                    return false;
                }

                octets[i] = octet;
            }

            normalizedAddress =
                $"{octets[0]}.{octets[1]}.{octets[2]}.{octets[3]}";
            return true;
        }

        private static string FindLocalIpv4Address()
        {
            try
            {
                IPAddress fallbackAddress = null;
                foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (address.AddressFamily != AddressFamily.InterNetwork
                        || IPAddress.IsLoopback(address))
                    {
                        continue;
                    }

                    fallbackAddress ??= address;
                    byte[] bytes = address.GetAddressBytes();
                    bool isPrivate = bytes[0] == 10
                        || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        || (bytes[0] == 192 && bytes[1] == 168);
                    if (isPrivate)
                    {
                        return address.ToString();
                    }
                }

                return fallbackAddress != null
                    ? fallbackAddress.ToString()
                    : "NAO ENCONTRADO";
            }
            catch (SocketException)
            {
                return "NAO ENCONTRADO";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _clientConnectionTimeoutSeconds = Mathf.Max(1f, _clientConnectionTimeoutSeconds);
        }
#endif
    }
}
