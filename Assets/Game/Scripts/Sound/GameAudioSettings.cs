using UnityEngine;
using UnityEngine.Audio;

namespace PiGame.Sound
{
    // Preferências locais: os volumes não são enviados pela rede.
    public static class GameAudioSettings
    {
        private const string MASTER_KEY = "MasterVolume";
        private const string MUSIC_KEY = "MusicVolume";
        private const string SFX_KEY = "SfxVolume";

        private static AudioMixer _mixer;

        public static float Master => Mathf.Clamp01(PlayerPrefs.GetFloat(MASTER_KEY, 1f));
        public static float Music => Mathf.Clamp01(PlayerPrefs.GetFloat(MUSIC_KEY, 1f));
        public static float Sfx => Mathf.Clamp01(PlayerPrefs.GetFloat(SFX_KEY, 1f));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RestoreMasterVolume()
        {
            _mixer = null;
            AudioListener.volume = Master;
        }

        public static void UseMixer(AudioMixer mixer)
        {
            if (mixer == null) return;
            _mixer = mixer;
            ApplyVolumes();
        }

        public static void ApplyVolumes()
        {
            if (_mixer == null)
            {
                AudioListener.volume = Master;
                return;
            }

            bool masterApplied = _mixer.SetFloat("MasterVolume", ToDecibels(Master));
            bool musicApplied = _mixer.SetFloat("MusicVolume", ToDecibels(Music));
            bool sfxApplied = _mixer.SetFloat("SFXVolume", ToDecibels(Sfx));
            // Evita aplicar o volume mestre duas vezes quando o mixer está ativo.
            AudioListener.volume = masterApplied ? 1f : Master;
            if (!masterApplied || !musicApplied || !sfxApplied)
                Debug.LogWarning("AudioMixer: exponha MasterVolume, MusicVolume e SFXVolume.");
        }

        private static float ToDecibels(float value) =>
            Mathf.Max(-80f, 20f * Mathf.Log10(Mathf.Max(value, 0.0001f)));

        public static void SetMaster(float value)
        {
            PlayerPrefs.SetFloat(MASTER_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public static void SetMusic(float value)
        {
            PlayerPrefs.SetFloat(MUSIC_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public static void SetSfx(float value)
        {
            PlayerPrefs.SetFloat(SFX_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyVolumes();
        }
    }
}
