// Applies weapon damage from an animation event and routes damage through the server.
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class WeaponDamageOnEvent : NetworkBehaviour
{
    [SerializeField] private Collider weaponCollider;
    [SerializeField] private int damagePerHit = 1;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField] private int maxHitsPerEvent = 24;
    [SerializeField] private float hitRadiusMultiplier = 1.2f;
    [SerializeField] private bool debugLogs = false;

    private Collider[] overlapBuffer = new Collider[24];
    private readonly HashSet<ulong> uniqueTargetIds = new HashSet<ulong>();
    private readonly List<ulong> targetIds = new List<ulong>();

    private void Awake()
    {
        if (weaponCollider == null)
        {
            weaponCollider = FindWeaponCollider();
        }

        ResizeBufferIfNeeded();

        if (debugLogs && weaponCollider == null)
        {
            Debug.LogWarning($"WeaponDamageOnEvent: No weapon collider assigned/found on {name}.", this);
        }
    }

    // Hook this in the attack animation event to apply damage on the hit frame.
    public void AnimationEvent_ApplyWeaponDamage()
    {
        if (!IsSpawned || !IsOwner || weaponCollider == null || !weaponCollider.gameObject.activeInHierarchy)
        {
            if (debugLogs)
            {
                Debug.Log($"WeaponDamageOnEvent: skipped (spawned:{IsSpawned} owner:{IsOwner} collider:{weaponCollider != null} active:{weaponCollider != null && weaponCollider.gameObject.activeInHierarchy})", this);
            }
            return;
        }

        CollectTargets();
        if (targetIds.Count == 0)
        {
            if (debugLogs)
            {
                Debug.Log("WeaponDamageOnEvent: no destructible targets found on this hit frame.", this);
            }
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"WeaponDamageOnEvent: hit {targetIds.Count} destructible target(s), sending damage to server.", this);
        }
        ApplyDamageServerRpc(targetIds.ToArray());
    }

    // Alternate event name convenience.
    public void ApplyWeaponDamage()
    {
        Debug.Log("Weapon event hit ok");
        AnimationEvent_ApplyWeaponDamage();
    }

    [ServerRpc]
    private void ApplyDamageServerRpc(ulong[] networkObjectIds)
    {
        if (networkObjectIds == null || networkObjectIds.Length == 0)
        {
            return;
        }

        foreach (ulong id in networkObjectIds)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject obj))
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"WeaponDamageOnEvent(Server): target NetworkObjectId {id} is not spawned.");
                }
                continue;
            }

            var destructible = obj.GetComponent<NetworkDestructibleProp>();
            if (destructible != null)
            {
                destructible.ApplyDamageServer(damagePerHit);
                if (debugLogs)
                {
                    Debug.Log($"WeaponDamageOnEvent(Server): applied {damagePerHit} damage to {destructible.name}.");
                }
            }
            else if (debugLogs)
            {
                Debug.LogWarning($"WeaponDamageOnEvent(Server): target {obj.name} has no NetworkDestructibleProp.");
            }
        }
    }

    private void CollectTargets()
    {
        uniqueTargetIds.Clear();
        targetIds.Clear();

        ResizeBufferIfNeeded();

        Bounds bounds = weaponCollider.bounds;
        float radius = Mathf.Max(0.01f, bounds.extents.magnitude * Mathf.Max(0.1f, hitRadiusMultiplier));
        int hitCount = Physics.OverlapSphereNonAlloc(
            bounds.center,
            radius,
            overlapBuffer,
            hitLayers,
            triggerInteraction);

        if (debugLogs)
        {
            Debug.Log($"WeaponDamageOnEvent: overlap hitCount={hitCount} layerMask={hitLayers.value} radius={radius:F3}.", this);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (debugLogs)
            {
                Debug.Log($"WeaponDamageOnEvent: overlap[{i}] = {hit.name} (layer {hit.gameObject.layer}).", hit);
            }

            if (IsSelfCollider(hit))
            {
                if (debugLogs)
                {
                    Debug.Log($"WeaponDamageOnEvent: ignored self collider {hit.name}.", hit);
                }
                continue;
            }

            NetworkDestructibleProp destructible = FindDestructible(hit);
            if (destructible == null)
            {
                if (debugLogs)
                {
                    Debug.Log($"WeaponDamageOnEvent: collider {hit.name} has no NetworkDestructibleProp in parents.", hit);
                }
                continue;
            }

            if (destructible.NetworkObject == null)
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"WeaponDamageOnEvent: destructible {destructible.name} has no NetworkObject.", destructible);
                }
                continue;
            }

            if (!destructible.IsSpawned)
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"WeaponDamageOnEvent: destructible {destructible.name} is not spawned as a network object.", destructible);
                }
                continue;
            }

            if (destructible.IsDestroyedState)
            {
                continue;
            }

            ulong id = destructible.NetworkObject.NetworkObjectId;
            if (uniqueTargetIds.Add(id))
            {
                targetIds.Add(id);
            }
        }
    }

    private Collider FindWeaponCollider()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col != null && col.CompareTag("WeaponStun"))
            {
                return col;
            }
        }

        return null;
    }

    private bool IsSelfCollider(Collider hit)
    {
        return hit == weaponCollider || hit.transform.IsChildOf(transform);
    }

    private static NetworkDestructibleProp FindDestructible(Collider hit)
    {
        NetworkDestructibleProp destructible = hit.GetComponentInParent<NetworkDestructibleProp>();
        if (destructible != null)
        {
            return destructible;
        }

        NetworkObject netObj = hit.GetComponentInParent<NetworkObject>();
        if (netObj != null)
        {
            return netObj.GetComponent<NetworkDestructibleProp>();
        }

        return null;
    }

    private void ResizeBufferIfNeeded()
    {
        if (maxHitsPerEvent < 1)
        {
            maxHitsPerEvent = 1;
        }

        if (overlapBuffer == null || overlapBuffer.Length != maxHitsPerEvent)
        {
            overlapBuffer = new Collider[maxHitsPerEvent];
        }
    }
}
