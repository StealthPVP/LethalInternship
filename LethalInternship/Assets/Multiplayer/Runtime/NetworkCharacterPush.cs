// Lets owner-controlled CharacterController players push networked rigidbodies via server RPC.
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkCharacterPush : NetworkBehaviour
{
    [SerializeField] private LayerMask pushableLayers = ~0;
    [SerializeField] private float pushForce = 2.5f;
    [SerializeField] private float upwardPush = 0.05f;
    [SerializeField] private float pushCooldown = 0.06f;
    [SerializeField] private bool debugLogs = false;

    private readonly Dictionary<ulong, float> lastPushTimes = new Dictionary<ulong, float>();

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsSpawned || !IsOwner || hit.collider == null)
        {
            return;
        }

        if (((1 << hit.collider.gameObject.layer) & pushableLayers.value) == 0)
        {
            return;
        }

        Rigidbody rb = hit.rigidbody;
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        NetworkObject targetNetworkObject = rb.GetComponentInParent<NetworkObject>();
        if (targetNetworkObject == null || !targetNetworkObject.IsSpawned || targetNetworkObject.NetworkObjectId == NetworkObjectId)
        {
            return;
        }

        if (lastPushTimes.TryGetValue(targetNetworkObject.NetworkObjectId, out float lastTime) &&
            Time.time - lastTime < pushCooldown)
        {
            return;
        }

        lastPushTimes[targetNetworkObject.NetworkObjectId] = Time.time;

        Vector3 direction = hit.moveDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = (targetNetworkObject.transform.position - transform.position);
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        direction.Normalize();
        Vector3 impulse = direction * pushForce + Vector3.up * upwardPush;

        if (debugLogs)
        {
            Debug.Log($"NetworkCharacterPush: request push {targetNetworkObject.name} with impulse {impulse}.", this);
        }

        PushObjectServerRpc(targetNetworkObject.NetworkObjectId, hit.point, impulse);
    }

    [ServerRpc]
    private void PushObjectServerRpc(ulong targetNetworkObjectId, Vector3 hitPoint, Vector3 impulse)
    {
        if (NetworkManager == null ||
            !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject target))
        {
            return;
        }

        Rigidbody rb = FindClosestRigidbody(target.transform, hitPoint);
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        rb.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);

        if (debugLogs)
        {
            Debug.Log($"NetworkCharacterPush(Server): pushed {target.name}.", target);
        }
    }

    private static Rigidbody FindClosestRigidbody(Transform root, Vector3 worldPoint)
    {
        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        Rigidbody best = null;
        float bestDistance = float.MaxValue;

        foreach (Rigidbody rb in bodies)
        {
            if (rb == null || rb.isKinematic)
            {
                continue;
            }

            float distance = (rb.worldCenterOfMass - worldPoint).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = rb;
            }
        }

        return best;
    }
}
