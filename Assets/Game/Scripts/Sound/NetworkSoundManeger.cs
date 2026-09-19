using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace PiGame.Sound
{
    public enum SFXList { SFX1, SFX2, SFX3 }
    public enum OSTList { OST1, OST2, OST3 }
    public class NetworkSoundManager: NetworkBehaviour
    {
        public static NetworkSoundManager Instance { get; private set; }

        [System.Serializable]
        public struct SoundData
        {
            public SFXList sfxType;
            public AudioClip clip;
        }

        [System.Serializable]
        public struct MusicData
        {
            public OSTList ostType;
            public AudioClip clip;
        }

        [Header("Componentes de Áudio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ostSource;

        [Header("Listas de Áudio (Configure no Inspector)")]
        [SerializeField] private List<SoundData> sfxList = new();
        [SerializeField] private List<MusicData> ostList = new();

        private Dictionary<SFXList, AudioClip> _sfxDict = new();
        private Dictionary<OSTList, AudioClip> _ostDict = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var item in sfxList)
            {
                if (!_sfxDict.ContainsKey(item.sfxType))
                    _sfxDict.Add(item.sfxType, item.clip);
            }

            foreach (var item in ostList)
            {
                if (!_ostDict.ContainsKey(item.ostType))
                    _ostDict.Add(item.ostType, item.clip);
            }
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
        }

        public static void PlaySFX(SFXList sfx)
        {
            if (Instance == null)
            {
                Debug.LogError("SoundManager não encontrado na cena!");
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Instance.PlaySFXServerRpc(sfx);
            }
            else
            {
                Instance.PlaySFXLocal(sfx);
            }
        }

        public static void PlayOST(OSTList ost)
        {
            if (Instance == null) return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Instance.PlayOSTServerRpc(ost);
            }
            else
            {
                Instance.PlayOSTLocal(ost);
            }
        }

        [Rpc(SendTo.Server)]
        private void PlaySFXServerRpc(SFXList sfx)
        {
            PlaySFXClientRpc(sfx);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlaySFXClientRpc(SFXList sfx)
        {
            PlaySFXLocal(sfx);
        }

        [Rpc(SendTo.Server)]
        private void PlayOSTServerRpc(OSTList ost)
        {
            PlayOSTClientRpc(ost);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOSTClientRpc(OSTList ost)
        {
            PlayOSTLocal(ost);
        }

        private void PlaySFXLocal(SFXList sfx)
        {
            if (_sfxDict.TryGetValue(sfx, out AudioClip clip))
            {
                sfxSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"O SFX '{sfx}' não foi configurado nas listas do SoundManager!");
            }
        }

        private void PlayOSTLocal(OSTList ost)
        {
            if (_ostDict.TryGetValue(ost, out AudioClip clip))
            {
                ostSource.clip = clip;
                ostSource.loop = true;
                ostSource.Play();
            }
            else
            {
                Debug.LogWarning($"A OST '{ost}' não foi configurada nas listas do SoundManager!");
            }
        }
    }
}