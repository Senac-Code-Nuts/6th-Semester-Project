using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PiGame.Sound
{
    public enum OSTList
    {
        OST1,
        OST2,
        OST3,
    }
    public enum SFXList
    {
        SFX1,
        SFX2,
        SFX3,
    }
    public class NetworkSoundManeger : NetworkBehaviour
    {
        public static NetworkSoundManeger Instance { get; set; }

        [System.Serializable]
        public struct OSTtype
        {
            public OSTList _OST;
            public AudioClip OSTAudio;
        }

        [System.Serializable]
        public struct SFXtype
        {
            public SFXList SFX;
            public AudioClip SFXAudio;
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _ostSource;
        
        [Header("Audio lists")]
        [SerializeField] private List<SFXtype> _sfxList = new();
        [SerializeField] private List<OSTtype> _ostList = new();
        private Dictionary<SFXList, AudioClip> _sfxDictionary = new();
        private Dictionary<OSTList, AudioClip> _osfDictionary = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            
            foreach (var sfx in _sfxList) 
            {
                if (!_sfxDictionary.ContainsKey(sfx.SFX))
                {
                    _sfxDictionary.Add(sfx.SFX, sfx.SFXAudio);
                }
                
            }

            foreach (var ost in _ostList)
            {
                if (!_osfDictionary.ContainsKey(ost._OST))
                {
                    _osfDictionary.Add(ost._OST, ost.OSTAudio);
                }
            }
        }

        private void PlaySFX(SFXList sfxType) 
        {
            if(_sfxDictionary.TryGetValue(sfxType,out AudioClip clip))
            {
                _sfxSource.PlayOneShot(clip);
            }
        }
        private void PlayOST(OSTList ostType)
        {
            if (_osfDictionary.TryGetValue(ostType, out AudioClip clip))
            {
                _ostSource.clip = clip;
                _ostSource.loop = true;
                _ostSource.Play();
            }
        }

        #region OST 
        [Rpc(SendTo.Server)]
        public void PlaySFXServerRPC(SFXList sfxList) => PlaySfxAllClientRPC(sfxList);

        [Rpc(SendTo.ClientsAndHost)]
        private void PlaySfxAllClientRPC(SFXList sfxList) 
        {
            PlaySFX(sfxList);
        }
        #endregion

        #region SFX 
        /// <summary>
        ///  A ideia é que o desenvolvedor apenas chame o PlayOstServerRPC em uma unica linha, assim o codigo faz o resto
        /// </summary>
        /// <param name="oSTList"></param>
        [Rpc(SendTo.Server)]
        public void PlayOSTServerRPC(OSTList oSTList) => PlayOstAllClientRPC(oSTList);

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOstAllClientRPC(OSTList oSTList)
        {
            PlayOST(oSTList);
        }
        #endregion
    }
}
