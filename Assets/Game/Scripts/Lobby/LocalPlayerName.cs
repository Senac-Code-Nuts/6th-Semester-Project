using System;
using UnityEngine;

namespace PiGame.Lobby
{
    public static class LocalPlayerName
    {
        public const int MaximumLength = 16;
        private const string PreferenceKey = "PiGame.PlayerName";

        public static bool HasSavedName => PlayerPrefs.HasKey(PreferenceKey);
        public static event Action Saved;

        public static string Load()
        {
            return Normalize(PlayerPrefs.GetString(PreferenceKey, "Jogador"));
        }

        public static string Save(string value)
        {
            string name = Normalize(value);
            PlayerPrefs.SetString(PreferenceKey, name);
            PlayerPrefs.Save();
            Saved?.Invoke();
            return name;
        }

        public static string Normalize(string value)
        {
            char[] characters = new char[MaximumLength];
            int length = 0;
            foreach (char character in (value ?? string.Empty).Trim())
            {
                if (char.IsLetterOrDigit(character) || character == ' '
                    || character == '-' || character == '_' || character == '.' || character == '\'')
                {
                    characters[length++] = character;
                    if (length == MaximumLength)
                        break;
                }
            }

            string name = new string(characters, 0, length).Trim();
            return name.Length == 0 ? "Jogador" : name;
        }
    }
}
