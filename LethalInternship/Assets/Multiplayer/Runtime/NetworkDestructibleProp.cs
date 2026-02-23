// Server-authoritative destructible prop with network-synced HP and break visuals.
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkDestructibleProp : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private GameObject meshWhole;
    [SerializeField] private GameObject meshPieces;
    [SerializeField] private GameObject vfxSmoke;
    [SerializeField] private bool disableCollidersWhenDestroyed = true;
    [SerializeField] private bool disableRootPhysicsWhenDestroyed = true;
    [Header("Damage Visual")]
    [SerializeField] private bool driveOverlayFromHealth = true;
    [SerializeField] private string overlayPropertyName = "_OverlayStrength";
    [SerializeField] private Renderer[] overlayRenderers;
    [Header("Break Impulse")]
    [SerializeField] private bool applyImpulseOnDestroy = true;
    [SerializeField] private float explosionForce = 4f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private float explosionUpwardModifier = 0.2f;
    [SerializeField] private ForceMode explosionForceMode = ForceMode.Impulse;
    [SerializeField] private bool debugLogs = false;

    private readonly NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private MaterialPropertyBlock propertyBlock;
    private int overlayPropertyId;
    private Collider[] rootColliders;
    private Rigidbody[] rootRigidbodies;

    public int CurrentHealth => currentHealth.Value;
    public bool IsDestroyedState => isDestroyed.Value;

    private void Awake()
    {
        if (meshWhole == null)
        {
            Transform whole = transform.Find("Mesh_whole");
            if (whole != null)
            {
                meshWhole = whole.gameObject;
            }
        }

        if (meshPieces == null)
        {
            Transform pieces = transform.Find("Mesh_pieces");
            if (pieces != null)
            {
                meshPieces = pieces.gameObject;
            }
        }

        if (vfxSmoke == null)
        {
            Transform vfx = transform.Find("vfx_smoke");
            if (vfx != null)
            {
                vfxSmoke = vfx.gameObject;
            }
        }

        if (overlayRenderers == null || overlayRenderers.Length == 0)
        {
            GameObject source = meshWhole != null ? meshWhole : gameObject;
            overlayRenderers = source.GetComponentsInChildren<Renderer>(true);
        }

        rootColliders = GetComponents<Collider>();
        rootRigidbodies = GetComponents<Rigidbody>();

        propertyBlock = new MaterialPropertyBlock();
        overlayPropertyId = Shader.PropertyToID(overlayPropertyName);
    }

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
        isDestroyed.OnValueChanged += OnDestroyedChanged;

        if (IsServer)
        {
            currentHealth.Value = Mathf.Max(1, maxHealth);
            isDestroyed.Value = false;
            if (debugLogs)
            {
                Debug.Log($"NetworkDestructibleProp: spawned {name} with HP={currentHealth.Value}.", this);
            }
        }

        ApplyVisualState(isDestroyed.Value);
        ApplyDamageOverlay(currentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isDestroyed.OnValueChanged -= OnDestroyedChanged;
    }

    public void ApplyDamageServer(int amount)
    {
        if (!IsServer || isDestroyed.Value || amount <= 0)
        {
            return;
        }

        int newHealth = Mathf.Max(0, currentHealth.Value - amount);
        currentHealth.Value = newHealth;
        if (debugLogs)
        {
            Debug.Log($"NetworkDestructibleProp: {name} took {amount} damage, HP={newHealth}.", this);
        }

        if (newHealth <= 0)
        {
            isDestroyed.Value = true;
            if (debugLogs)
            {
                Debug.Log($"NetworkDestructibleProp: {name} destroyed.", this);
            }
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        ApplyDamageOverlay(newValue);

        if (newValue <= 0)
        {
            ApplyVisualState(true);
        }
    }

    private void OnDestroyedChanged(bool previousValue, bool newValue)
    {
        ApplyVisualState(newValue);

        if (!previousValue && newValue)
        {
            ApplyBreakImpulse();
        }
    }

    private void ApplyVisualState(bool destroyed)
    {
        if (meshWhole != null)
        {
            meshWhole.SetActive(!destroyed);
        }

        if (meshPieces != null)
        {
            meshPieces.SetActive(destroyed);
        }

        if (vfxSmoke != null)
        {
            vfxSmoke.SetActive(destroyed);
        }

        if (disableCollidersWhenDestroyed && destroyed && meshWhole != null)
        {
            foreach (Collider collider in meshWhole.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        if (disableRootPhysicsWhenDestroyed)
        {
            bool enabled = !destroyed;
            if (rootColliders != null)
            {
                foreach (Collider collider in rootColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = enabled;
                    }
                }
            }

            if (rootRigidbodies != null)
            {
                foreach (Rigidbody body in rootRigidbodies)
                {
                    if (body != null)
                    {
                        body.isKinematic = !enabled;
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }
                }
            }
        }
    }

    private void ApplyDamageOverlay(int health)
    {
        if (!driveOverlayFromHealth || overlayRenderers == null || overlayRenderers.Length == 0)
        {
            return;
        }

        int safeMax = Mathf.Max(1, maxHealth);
        float normalized = 1f - Mathf.Clamp01((float)health / safeMax);

        foreach (Renderer renderer in overlayRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(overlayPropertyId, normalized);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ApplyBreakImpulse()
    {
        if (!applyImpulseOnDestroy || meshPieces == null)
        {
            return;
        }

        Rigidbody[] bodies = meshPieces.GetComponentsInChildren<Rigidbody>(true);
        if (bodies == null || bodies.Length == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"NetworkDestructibleProp: {name} has no rigidbodies under Mesh_pieces for impulse.", this);
            }
            return;
        }

        Vector3 center = GetPiecesCenter();
        foreach (Rigidbody body in bodies)
        {
            if (body == null)
            {
                continue;
            }

            body.AddExplosionForce(explosionForce, center, explosionRadius, explosionUpwardModifier, explosionForceMode);
        }

        if (debugLogs)
        {
            Debug.Log($"NetworkDestructibleProp: applied break impulse to {bodies.Length} rigidbody(ies).", this);
        }
    }

    private Vector3 GetPiecesCenter()
    {
        Renderer[] renderers = meshPieces.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.center;
        }

        return meshPieces.transform.position;
    }
}
