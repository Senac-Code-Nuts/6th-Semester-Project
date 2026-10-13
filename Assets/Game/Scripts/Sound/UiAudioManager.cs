using UnityEngine;
namespace PiGame.Sound
{

    public class UiAudioManager : MonoBehaviour
    {
        public void PlayDrawRoullete()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DrawRoulette);
        }

        public void PlayVolumeScroll()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.VolumeScroll);
        }

        public void PlayLevelSelect()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.LevelSelect);
        }

        public void PlayButtonSound()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.UiButton);
        }
    }
}
