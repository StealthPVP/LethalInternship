// Assigns the local player's transform as the camera follow target.
using Unity.Netcode;
using UnityEngine;

public class LocalPlayerCameraBinder : NetworkBehaviour
{
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform followTarget;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            return;
        }

        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
        }

        if (cameraController != null)
        {
            cameraController.SetTarget(followTarget != null ? followTarget : transform);
        }
        else
        {
            Debug.LogWarning("LocalPlayerCameraBinder: No CameraController found in scene.");
        }
    }
}
