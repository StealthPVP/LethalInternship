// Handles Relay host/join flow and updates lobby connection status text.
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;

    [Header("Relay")]
    [SerializeField] private int maxConnections = 4;
    [SerializeField] private string connectionType = "udp";

    private bool isBusy;

    private void OnEnable()
    {
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(StartHost);
        }

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(StartClient);
        }
    }

    private void OnDisable()
    {
        if (hostButton != null)
        {
            hostButton.onClick.RemoveListener(StartHost);
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(StartClient);
        }
    }

    public async void StartHost()
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        SetStatus("Hosting...");
        try
        {
            await EnsureServicesAsync();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            UnityTransport transport = GetTransport();
            if (transport == null)
            {
                Debug.LogError("RelayUI: UnityTransport not found on NetworkManager.");
                return;
            }

            transport.SetRelayServerData(allocation.ToRelayServerData(connectionType));
            NetworkManager.Singleton.StartHost();

            if (joinCodeInput != null)
            {
                joinCodeInput.text = joinCode;
            }

            SetStatus("Hosting");
            Debug.Log($"Relay Host started. Join code: {joinCode}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"RelayUI: Failed to start host. {ex.Message}");
            SetStatus("Host failed");
        }
        finally
        {
            isBusy = false;
        }
    }

    public async void StartClient()
    {
        if (isBusy)
        {
            return;
        }

        string joinCode = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("RelayUI: Join code is empty.");
            SetStatus("Join code required");
            return;
        }

        isBusy = true;
        SetStatus("Connecting...");
        try
        {
            await EnsureServicesAsync();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport = GetTransport();
            if (transport == null)
            {
                Debug.LogError("RelayUI: UnityTransport not found on NetworkManager.");
                return;
            }

            transport.SetRelayServerData(allocation.ToRelayServerData(connectionType));
            NetworkManager.Singleton.StartClient();

            SetStatus("Connected");
            Debug.Log("Relay Client started.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"RelayUI: Failed to start client. {ex.Message}");
            SetStatus("Join failed");
        }
        finally
        {
            isBusy = false;
        }
    }

    private static UnityTransport GetTransport()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("RelayUI: NetworkManager.Singleton is missing.");
            return null;
        }

        return NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    private static async Task EnsureServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
