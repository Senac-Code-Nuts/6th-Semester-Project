using UnityEngine;

namespace PiGame.Sound
{
    public class SceneOSTManager : MonoBehaviour
    {
        [SerializeField] private OSTList _sceneOst;

        private void Start()
        {
            PlaySceneOST(_sceneOst);
        }
        private void PlaySceneOST(OSTList sceneOST)
        {
            NetworkSoundManager.Instance.LocalOST(sceneOST);
        }

        public void PlayVictoryOST()
        {
            NetworkSoundManager.Instance.LocalOST(OSTList.Battle_Vitoria);
        }
    }
    
}
