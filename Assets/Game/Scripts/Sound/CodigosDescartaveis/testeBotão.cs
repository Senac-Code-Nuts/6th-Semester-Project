using PiGame.Sound;
using UnityEngine;

public class TesteBotãoNovo : MonoBehaviour
{
    public void AoClicarNoBotao()
    {
        NetworkSoundManager.PlaySFX(SFXList.SFX1);
    }
}