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

        private float _sfxBaseVolume;
        private float _ostBaseVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _sfxBaseVolume = sfxSource.volume;
            _ostBaseVolume = ostSource.volume;
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

        public void PlaySFX(SFXList sfx)
        {
            if (Instance == null) return;
            Instance.SendSFXServerRpc(sfx);
        }

        public void LocalSFX(SFXList sfx)
        {
            if(Instance == null) return;
            Instance.PlaySFXLocal(sfx);
        }

        public void PlayOST(OSTList ost)
        {
            if (Instance == null) return;
            Instance.SendOSTServerRpc(ost);
        }

        public void LocalOST(OSTList ost)
        {
            if(Instance == null) return;
            Instance.PlayOSTLocal(ost);
        }

        #region RPC do SFX
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SendSFXServerRpc(SFXList sfx)
        {
            SendSFXClientRpc(sfx);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SendSFXClientRpc(SFXList sfx)
        {
            PlaySFXLocal(sfx);
        }
        #endregion

        #region Rpc do OST
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SendOSTServerRpc(OSTList ost)
        {
            SendOSTClientRpc(ost);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SendOSTClientRpc(OSTList ost)
        {
            PlayOSTLocal(ost);
        }
        #endregion

        #region Execu��o Local
        private void PlaySFXLocal(SFXList sfx)
        {
            if (_sfxDict.TryGetValue(sfx, out SoundData clip))
            {
                //sfxSource.PlayOneShot(clip);
                if(_sfxFadeRoutine != null)
                {
                    StopCoroutine(_sfxFadeRoutine);
                }

                _sfxFadeRoutine = StartCoroutine(PlaySFXWithFade(clip));
            }
            else
            {
                Debug.LogWarning($"O SFX '{sfx}' n�o foi configurado nas listas do SoundManager!");
            }
        }

        private void PlayOSTLocal(OSTList ost)
        {
            if (_ostDict.TryGetValue(ost, out MusicData clip))
            {
                if(_ostFadeRoutine != null)
                {
                    StopCoroutine(_ostFadeRoutine);
                }

                _ostFadeRoutine = StartCoroutine(PlayOSTWithFade(clip));
            }
            else
            {
                Debug.LogWarning($"A OST '{ost}' n�o foi configurada nas listas do SoundManager!");
            }
        }

        private IEnumerator PlaySFXWithFade(SoundData sound)
        {
            sfxSource.volume = 0f;
            sfxSource.PlayOneShot(sound.clip);

            yield return FadeVolume(sfxSource, 0f,_sfxBaseVolume, sound.fadeInSeconds);

            float remainingTime = Mathf.Max(0f, sound.clip.length - sound.fadeInSeconds - sound.fadeOutSeconds);

            yield return new WaitForSeconds(remainingTime);

            yield return FadeVolume(sfxSource, _sfxBaseVolume, 0f, sound.fadeOutSeconds);

            sfxSource.volume = _sfxBaseVolume;
            _sfxFadeRoutine = null;
        }

        private IEnumerator PlayOSTWithFade(MusicData music)
        {
            if(ostSource.isPlaying)
            {
                yield return FadeVolume(ostSource, ostSource.volume, 0f, music.fadeOutSeconds);

                ostSource.Stop();
            }

            ostSource.clip = music.clip;
            ostSource.loop = true;
            ostSource.volume = 0f;
            ostSource.Play();

            yield return FadeVolume(ostSource, 0f, _ostBaseVolume, music.fadeInSeconds);

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
