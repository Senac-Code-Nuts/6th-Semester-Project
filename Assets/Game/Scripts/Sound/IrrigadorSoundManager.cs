namespace PiGame.Sound
{
    public class IrrigadorSoundManager : GeneralPlayerSoundManager
    {
        public void PlayStuckNail()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.StuckNail);
        }

        public void PlayNailShoot()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.ShootNail);
        }

        public void PlayIrrigadorPull()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.IrrigadorPull);
        }

        public void PlayIrrigadorSkill()
        {
            NetworkSoundManager.Instance.PlaySFX(SFXList.IrrigadorSkill);
        }
    }
    
}
