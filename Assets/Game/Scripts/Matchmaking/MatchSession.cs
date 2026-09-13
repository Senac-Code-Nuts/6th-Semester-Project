using UnityEngine;

namespace PiGame.Lobby
{
    public class MatchSession : MonoBehaviour
    {
        public static MatchSession Instance {get; private set;}
        public MatchSnapshot Snapshot {get; private set;}

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetSnapshot(MatchSnapshot matchSnapshot)
        {
            Snapshot = matchSnapshot;
        }

        public void Clear()
        {
            Snapshot = null;
        }
    }   
}

