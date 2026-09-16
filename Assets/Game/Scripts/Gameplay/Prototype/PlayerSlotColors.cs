using UnityEngine;

namespace PiGame.Gameplay
{
    public static class PlayerSlotColors
    {
        private static readonly Color[] Colors =
        {
            new Color32(70, 156, 255, 255),
            new Color32(255, 126, 55, 255),
            new Color32(92, 205, 110, 255),
            new Color32(196, 112, 255, 255)
        };

        public static Color Get(int playerSlot)
        {
            return playerSlot >= 0 && playerSlot < Colors.Length
                ? Colors[playerSlot]
                : Color.white;
        }

        public static string GetHtml(int playerSlot)
        {
            return ColorUtility.ToHtmlStringRGB(Get(playerSlot));
        }
    }
}
