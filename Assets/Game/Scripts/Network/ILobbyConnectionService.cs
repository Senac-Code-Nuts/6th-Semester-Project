using System;
using System.Threading.Tasks;

namespace PiGame.Networking
{
    public enum LobbyConnectionFailure
    {
        NetworkManagerUnavailable,
        AlreadyRunning,
        StartFailed,
        HostUnavailable,
        TransportFailure,
        ServicesUnavailable,
        AuthenticationFailed,
        InvalidJoinCode,
        SessionNotFound,
        SessionConflict
    }

    public interface ILobbyConnectionService
    {
        event Action Connected;
        event Action Disconnected;
        event Action<LobbyConnectionFailure> ConnectionFailed;

        bool IsConnected { get; }
        bool IsHost { get; }
        string JoinCode { get; }

        Task StartHostAsync();
        Task StartClientAsync(string joinCode);
        Task ShutdownAsync();
    }
}
