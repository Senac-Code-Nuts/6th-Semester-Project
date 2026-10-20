using UnityEngine;

namespace PiGame.Sound
{
    public class GeneralPlayerSoundManager : MonoBehaviour
    {
        public void PlayWallJump()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.WallJump);
        }

        public void PlayJump()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.PlayerJump);
        }

        public void PlayHit()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.Hit);
        }

        public void PlayDeathHit()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DeathHit);
        }

        public void PlayRespawn()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.PlayerRespawn);
        }

        public void PlayDownPlatform()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DownPlatform);
        }

        public void PlayUpPlatform()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.UpPlatform);
        }
    }
    
}
