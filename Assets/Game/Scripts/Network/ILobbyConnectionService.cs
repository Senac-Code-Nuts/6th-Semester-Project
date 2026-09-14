using System;

namespace PiGame.Networking
{
    public enum LobbyConnectionFailure
    {
        NetworkManagerUnavailable,
        AlreadyRunning,
        StartFailed,
        HostUnavailable,
        TimedOut,
        TransportFailure,
        InvalidAddress
    }

    public interface ILobbyConnectionService
    {
        event Action Connected;
        event Action Disconnected;
        event Action<LobbyConnectionFailure> ConnectionFailed;

        bool IsConnected { get; }
        bool IsHost { get; }
        string LocalAddress { get; }

        void StartHost();
        void StartClient(string address);
        void Shutdown();
    }
}
