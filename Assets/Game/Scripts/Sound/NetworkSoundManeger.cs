using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PiGame.Sound
{
    public enum SFXList { WallJump, PlayerJump, Hit, DeathHit, DrawRoulette, VolumeScroll, LevelSelect, UiButton, DownPlatform, UpPlatform, WhirlPlatform,
    StuckNail, ShootNail, IrrigadorPull, IrrigadorSkill, DeserticoRoll, DeserticoShot, DeserticoEmpty, DeserticoReload, RebeldeBounce, RebeldeCatch, RebeldeEmpty, RebeldeSkill,
    RebeldeShot, FumaceiroEmpty, FumaceiroCatch, FumaceiroThrow, FumaceiroSkill, PlayerRespawn
        }
    public enum OSTList { Battle_Desertico, Battle_Fumaceiro, Battle_Irrigador, Battle_Rebelde, Battle_Vitoria, Menu_Titulo, Menu_Screens }

    public class NetworkSoundManager : NetworkBehaviour
    {
        public static NetworkSoundManager Instance { get; private set; }

        [System.Serializable]
        public struct SoundData
        {
            public SFXList sfxType;
            public AudioClip clip;

            [Min(0f)] public float fadeInSeconds;
            [Min(0f)] public float fadeOutSeconds;
        }

        [System.Serializable]
        public struct MusicData
        {
            public OSTList ostType;
            public AudioClip clip;

            [Min(0f)] public float fadeInSeconds;
            [Min(0f)] public float fadeOutSeconds;
        }

        [Header("Componentes de �udio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ostSource;

        [Header("Listas de �udio (Configure no Inspector)")]
        [SerializeField] private List<SoundData> sfxList = new();
        [SerializeField] private List<MusicData> ostList = new();

        private Dictionary<SFXList, SoundData> _sfxDict = new();
        private Dictionary<OSTList, MusicData> _ostDict = new();

        private Coroutine _sfxFadeRoutine;
        private Coroutine _ostFadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RegisterMixer();

            foreach (var item in sfxList)
            {
                if (!_sfxDict.ContainsKey(item.sfxType))
                    _sfxDict.Add(item.sfxType, item);
            }

            foreach (var item in ostList)
            {
                if (!_ostDict.ContainsKey(item.ostType))
                    _ostDict.Add(item.ostType, item);
            }
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            RegisterMixer();
        }

        private void RegisterMixer()
        {
            GameAudioSettings.UseMixer(ostSource != null
                ? ostSource.outputAudioMixerGroup?.audioMixer : null);
        }

        public void PlaySFX(SFXList sfx, float volume = 0.2f)
        {
            if (Instance == null) return;
            Instance.SendSFXServerRpc(sfx, Mathf.Clamp01(volume));
        }

        public void LocalSFX(SFXList sfx, float volume = 0.2f)
        {
            if(Instance == null) return;
            Instance.PlaySFXLocal(sfx, Mathf.Clamp01(volume));
        }

        public void PlayOST(OSTList ost, float volume = 0.3f)
        {
            if (Instance == null) return;
            Instance.SendOSTServerRpc(ost, Mathf.Clamp01(volume));
        }

        public void LocalOST(OSTList ost, float volume = 0.3f)
        {
            if(Instance == null) return;
            Instance.PlayOSTLocal(ost, Mathf.Clamp01(volume));
        }

        #region RPC do SFX
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SendSFXServerRpc(SFXList sfx, float volume)
        {
            SendSFXClientRpc(sfx, volume);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SendSFXClientRpc(SFXList sfx, float volume)
        {
            PlaySFXLocal(sfx, volume);
        }
        #endregion

        #region Rpc do OST
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SendOSTServerRpc(OSTList ost, float volume)
        {
            SendOSTClientRpc(ost, volume);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SendOSTClientRpc(OSTList ost, float volume)
        {
            PlayOSTLocal(ost, volume);
        }
        #endregion

        #region Execu��o Local
        private void PlaySFXLocal(SFXList sfx, float volume)
        {
            if (_sfxDict.TryGetValue(sfx, out SoundData sound))
            {
                if(_sfxFadeRoutine != null)
                {
                    StopCoroutine(_sfxFadeRoutine);
                }

                _sfxFadeRoutine = StartCoroutine(PlaySFXWithFade(sound, volume));
            }
            else
            {
                Debug.LogWarning($"O SFX '{sfx}' n�o foi configurado nas listas do SoundManager!");
            }
        }

        private void PlayOSTLocal(OSTList ost, float volume)
        {
            if (_ostDict.TryGetValue(ost, out MusicData music))
            {
                if(_ostFadeRoutine != null)
                {
                    StopCoroutine(_ostFadeRoutine);
                }

                _ostFadeRoutine = StartCoroutine(PlayOSTWithFade(music, volume));
            }
            else
            {
                Debug.LogWarning($"A OST '{ost}' n�o foi configurada nas listas do SoundManager!");
            }
        }

        private IEnumerator PlaySFXWithFade(SoundData sound, float targetVolume)
        {
            if (sound.clip == null)
            {
                _sfxFadeRoutine = null;
                yield break;
            }

            sfxSource.volume = 0f;
            sfxSource.PlayOneShot(sound.clip);

            yield return FadeVolume(sfxSource, 0f, targetVolume, sound.fadeInSeconds);

            float fadeOutStart = Mathf.Max(sound.fadeInSeconds, sound.clip.length - sound.fadeOutSeconds);
            float remainingTime = Mathf.Max(0f, fadeOutStart - sound.fadeInSeconds);

            yield return new WaitForSecondsRealtime(remainingTime);

            yield return FadeVolume(sfxSource, targetVolume, 0f, sound.fadeOutSeconds);

            sfxSource.volume = targetVolume;
            _sfxFadeRoutine = null;
        }

        private IEnumerator PlayOSTWithFade(MusicData music, float targetVolume)
        {
            if (music.clip == null)
            {
                _ostFadeRoutine = null;
                yield break;
            }

            if(ostSource.isPlaying)
            {
                yield return FadeVolume(ostSource, ostSource.volume, 0f, music.fadeOutSeconds);

                ostSource.Stop();
            }

            ostSource.clip = music.clip;
            ostSource.loop = true;
            ostSource.volume = 0f;
            ostSource.Play();

            yield return FadeVolume(ostSource, 0f, targetVolume, music.fadeInSeconds);

            _ostFadeRoutine = null;
        }

        private IEnumerator FadeVolume(AudioSource source, float startVolume, float targetVolume, float duration)
        {
            if(duration <= 0f)
            {
                source.volume = targetVolume;
                yield break;
            }

            float elapsed = 0f;
            source.volume = startVolume;

            while(elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float time = Mathf.Clamp01(elapsed / duration);

                source.volume = Mathf.Lerp(startVolume, targetVolume, time);

                yield return null;
            }
            
            source.volume = targetVolume;
        }

        #endregion
        
    }
}
