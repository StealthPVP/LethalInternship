// Assigns the local player's transform as the camera follow target.
using Unity.Netcode;
using UnityEngine;

public class LocalPlayerCameraBinder : NetworkBehaviour
{
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform followTarget;
    [SerializeField] private CharacterMovement characterMovement;

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
            cameraController.SetYawAlignmentTarget(transform);

            if (characterMovement == null)
            {
                characterMovement = GetComponent<CharacterMovement>();
            }

            if (characterMovement != null)
            {
                characterMovement.SetCameraTransform(cameraController.CameraTransform);
            }
        }
        else
        {
            Debug.LogWarning("LocalPlayerCameraBinder: No CameraController found in scene.");
        }
    }
}
