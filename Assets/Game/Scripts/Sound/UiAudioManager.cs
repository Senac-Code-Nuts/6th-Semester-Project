using UnityEngine;
namespace PiGame.Sound
{

    public class UiAudioManager : MonoBehaviour
    {
        public void PlayDrawRoullete()
        {
            NetworkSoundManager.Instance.LocalSFX(SFXList.DrawRoulette);
        }

        public void PlayVolumeScroll()
        {
            NetworkSoundManager.Instance.LocalSFX(SFXList.VolumeScroll);
        }

        public void PlayLevelSelect()
        {
            NetworkSoundManager.Instance.LocalSFX(SFXList.LevelSelect);
        }

        public void PlayLocalButtonSound()
        {
           NetworkSoundManager.Instance.LocalSFX(SFXList.UiButton); 
        }
    }
}
