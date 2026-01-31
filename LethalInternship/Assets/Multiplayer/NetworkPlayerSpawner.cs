using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSpawner : MonoBehaviour
{
    [SerializeField] private bool disableAutoCreatePlayer = true;

    private bool approvalRegistered;

    private void Awake()
    {
        if (!disableAutoCreatePlayer || NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectionApprovalCallback != null)
        {
            Debug.LogWarning("NetworkPlayerSpawner: ConnectionApprovalCallback already set. Auto player creation may still occur.");
            return;
        }

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        approvalRegistered = true;
    }

    private void OnDisable()
    {
        if (approvalRegistered && NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectionApprovalCallback == ApprovalCheck)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
            approvalRegistered = false;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
    }
}
