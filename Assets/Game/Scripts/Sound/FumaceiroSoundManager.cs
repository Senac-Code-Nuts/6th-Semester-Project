namespace PiGame.Sound
{
    public class FumaceiroSoundManager : GeneralPlayerSoundManager
    {
        public void PlayFumaceiroEmpty()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.FumaceiroEmpty);
        }

        public void PlayFumaceiroCatch()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.FumaceiroCatch);
        }

        public void PlayFumaceiroThrow()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.FumaceiroThrow);
        }

        public void PlayFumaceiroSkill()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.FumaceiroSkill);
        }
    }
    
}
