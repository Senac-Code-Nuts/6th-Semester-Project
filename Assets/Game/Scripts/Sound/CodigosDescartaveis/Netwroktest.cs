using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;

public class NetworkTest : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;

    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject roomUI;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    [Header("Prefabs de Rede")]
    [SerializeField] private GameObject soundManagerPrefab; // Arraste o Prefab do som aqui no Inspector

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (joinCodeText != null) joinCodeText.text = joinCode;

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

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

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                joinAllocation.Key
            );

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