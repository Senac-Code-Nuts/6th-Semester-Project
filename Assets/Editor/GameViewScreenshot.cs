#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PiGame.EditorTools
{
    public static class GameViewScreenshot
    {
        [MenuItem("Tools/Take Game View Screenshot %#k")]
        public static void CaptureScreenshot()
        {
            string folderPath = Path.Combine(Application.dataPath, "_Screenshots");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filename = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string fullPath = Path.Combine(folderPath, filename);

            ScreenCapture.CaptureScreenshot(fullPath);
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[Screenshot]</b></color> Saved to: {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }
    }
}
#endif
