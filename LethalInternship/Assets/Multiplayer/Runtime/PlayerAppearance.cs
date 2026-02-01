// Syncs a material variant index so all clients see the same player look.
using Unity.Netcode;
using UnityEngine;

public class PlayerAppearance : NetworkBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Material[] variants;
    [SerializeField] private bool autoFindRenderers = true;
    [SerializeField] private bool includeInactive = true;

    private readonly NetworkVariable<int> variantIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int VariantCount => variants != null ? variants.Length : 0;

    public override void OnNetworkSpawn()
    {
        AutoAssignRenderers();
        variantIndex.OnValueChanged += OnVariantChanged;
        ApplyVariant(variantIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        variantIndex.OnValueChanged -= OnVariantChanged;
    }

    public void SetVariantServer(int index)
    {
        if (!IsServer)
        {
            return;
        }

        variantIndex.Value = ClampIndex(index);
    }

    private void OnVariantChanged(int previousValue, int newValue)
    {
        ApplyVariant(newValue);
    }

    private void ApplyVariant(int index)
    {
        AutoAssignRenderers();
        if (variants == null || variants.Length == 0 || targetRenderers == null || targetRenderers.Length == 0)
        {
            return;
        }

        int clamped = ClampIndex(index);
        Material material = variants[clamped];

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.material = material;
        }
    }

    private int ClampIndex(int index)
    {
        if (variants == null || variants.Length == 0)
        {
            return 0;
        }

        if (index < 0)
        {
            return 0;
        }

        if (index >= variants.Length)
        {
            return variants.Length - 1;
        }

        return index;
    }

    private void AutoAssignRenderers()
    {
        if (!autoFindRenderers)
        {
            return;
        }

        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return;
        }

        targetRenderers = GetComponentsInChildren<Renderer>(includeInactive);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignRenderers();
    }
#endif
}
