using System.Threading.Tasks;

namespace PiGame.UI
{
    public interface IPauseContext
    {
        bool CanOpenPause { get; }

        void EnterPause();
        void ExitPause();
        Task ExitContextAsync();
    }
}
