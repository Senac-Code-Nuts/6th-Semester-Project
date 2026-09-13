using UnityEngine;
using Unity.Netcode;
using PiGame.Lobby;
using UnityEngine.SceneManagement;

namespace PiGame.Gameplay
{
    public class MatchController : NetworkBehaviour
    {
        private int _durationMinutes;
        private LobbyMatchMode _mode;

        private NetworkVariable<float> _remainingTime = new NetworkVariable<float>();
        public float RemainingTime => _remainingTime.Value;
        private bool _matchRunning;

        public void Initialize(LobbyMatchSettingsData settings)
        {
            if(!IsServer)
                return;

            _durationMinutes = settings.DurationMinutes;
            _mode = settings.Mode;

            _remainingTime.Value = _durationMinutes * 60;

            _matchRunning = true;
        }

        public void Update()
        {
            if(!IsServer || !_matchRunning)
                return;
            
            _remainingTime.Value -= Time.deltaTime;

            if(_remainingTime.Value <= 0f)
            {
                _remainingTime.Value = 0f;
                EndMatch();
            }
        }

        private void EndMatch()
        {
            _matchRunning = false;

            MatchSession.Instance.Clear();

            NetworkManager.SceneManager.LoadScene("scene_lobby",LoadSceneMode.Single);
        }
    }
}

