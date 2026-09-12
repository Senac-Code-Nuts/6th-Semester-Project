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
        TransportFailure
    }

    public interface ILobbyConnectionService
    {
        event Action Connected;
        event Action Disconnected;
        event Action<LobbyConnectionFailure> ConnectionFailed;

        bool IsConnected { get; }
        bool IsHost { get; }

        void StartHost();
        void StartClient();
        void Shutdown();
    }
}
