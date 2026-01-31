using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class OwnerAuthorityNetworkAnimator : MonoBehaviour
{
    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (networkAnimator == null)
        {
            networkAnimator = GetComponent<NetworkAnimator>();
        }

        if (networkAnimator != null)
        {
            networkAnimator.AuthorityMode = NetworkAnimator.AuthorityModes.Owner;

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null && networkAnimator.Animator == null)
            {
                networkAnimator.Animator = animator;
            }
        }
    }
}
