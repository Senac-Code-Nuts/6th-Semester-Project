using System.Threading.Tasks;

namespace PiGame.Gameplay
{
    public interface IMatchDisconnectService
    {
        Task DisconnectAsync();
    }
}
