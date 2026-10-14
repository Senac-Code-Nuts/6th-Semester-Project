namespace PiGame.Sound
{
    public class CowboySoundManager : GeneralPlayerSoundManager
    {
        public void PlayCowboySkill()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DeserticoRoll);
        }

        public void PlayCowboyShoot()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DeserticoShot);
        }

        public void PlayCowboyEmpty()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DeserticoEmpty);
        }

        public void PlayCowboyReload()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.DeserticoReload);
        }
    }
    
}
