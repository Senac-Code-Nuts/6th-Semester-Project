using UnityEngine;
namespace PiGame.Sound
{
    public class EnviromentSoundManager : MonoBehaviour
    {
        public void PlayDownPlatform()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DownPlatform);
        }

        public void PlayUpPlatform()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.UpPlatform);
        }

        public void PlayWhirlPlatform()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.WhirlPlatform);
        }
    }
    
}
