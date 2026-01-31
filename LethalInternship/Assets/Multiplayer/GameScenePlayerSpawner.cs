using Unity.Netcode;
using UnityEngine;

public class GameScenePlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefabOverride;
    [SerializeField] private bool destroyPlayerWithScene = true;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool randomizeSpawnPoints = false;

    private int nextSpawnIndex;

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void Start()
    {
        if (!IsServerActive())
        {
            return;
        }

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            TrySpawnPlayer(client.ClientId);
        }
    }

    private bool IsServerActive()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServerActive())
        {
            return;
        }

        TrySpawnPlayer(clientId);
    }

    private void TrySpawnPlayer(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
        {
            return;
        }

        GameObject prefab = playerPrefabOverride != null
            ? playerPrefabOverride
            : NetworkManager.Singleton.NetworkConfig.PlayerPrefab;

        if (prefab == null)
        {
            Debug.LogError("GameScenePlayerSpawner: Player prefab is not set.");
            return;
        }

        GetSpawnTransform(out var position, out var rotation);

        NetworkObject player = NetworkObject.InstantiateAndSpawn(
            prefab,
            NetworkManager.Singleton,
            ownerClientId: clientId,
            destroyWithScene: destroyPlayerWithScene,
            isPlayerObject: true,
            position: position,
            rotation: rotation);

        var appearance = player.GetComponent<PlayerAppearance>();
        if (appearance != null)
        {
            appearance.SetVariantServer((int)(clientId % (ulong)Mathf.Max(1, appearance.VariantCount)));
        }
    }

    private void GetSpawnTransform(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        int index = randomizeSpawnPoints
            ? Random.Range(0, spawnPoints.Length)
            : nextSpawnIndex++ % spawnPoints.Length;

        Transform spawn = spawnPoints[index];
        position = spawn.position;
        rotation = spawn.rotation;
    }
}
