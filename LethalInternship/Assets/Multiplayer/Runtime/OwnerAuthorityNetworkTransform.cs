// Ensures NetworkTransform runs in owner-authoritative mode.
using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class OwnerAuthorityNetworkTransform : MonoBehaviour
{
    [SerializeField] private NetworkTransform networkTransform;

    private void Awake()
    {
        if (networkTransform == null)
        {
            networkTransform = GetComponent<NetworkTransform>();
        }

        if (networkTransform != null)
        {
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        }
    }
}
