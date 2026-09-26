using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay; 
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;
namespace PiGame.Sound.Tests
{
    public class NetworkTest : MonoBehaviour
    {
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TMP_Text joinCodeText;

        [SerializeField] private GameObject menuUI;
        [SerializeField] private GameObject roomUI;

        [Header("Prefabs de Rede")]
        [SerializeField] private GameObject soundManagerPrefab;

        private async void Start()
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        public async void CreateRelay()
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                if (joinCodeText != null) joinCodeText.text = joinCode;

                var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                NetworkManager.Singleton.StartHost();

                if (soundManagerPrefab != null)
                {
                    GameObject soundInstance = Instantiate(soundManagerPrefab);
                    soundInstance.GetComponent<NetworkObject>().Spawn();
                }

                SwitchUI();
            }
            catch (RelayServiceException e)
            {
                Debug.LogException(e);
            }
        }

        public async void JoinRelay()
        {
            try
            {
                string joinCode = joinCodeInput.text.Trim();
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                NetworkManager.Singleton.StartClient();
                SwitchUI();
            }
            catch (RelayServiceException e)
            {
                Debug.LogException(e);
            }
        }

        private void SwitchUI()
        {
            if (menuUI != null) menuUI.SetActive(false);
            if (roomUI != null) roomUI.SetActive(true);
        }

    }
}
