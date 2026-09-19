using PiGame.Sound;
using UnityEngine;

public class TesteBotãoNovo : MonoBehaviour
{
    public void AoClicarNoBotao()
    {
        NetworkSoundManager.PlaySFX(SFXList.SFX1);
    }
    public void AoClicarNoBotao2()
    {
        NetworkSoundManager.PlayOST(OSTList.OST1);
    }
}