using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private Button startGameButton;
    [SerializeField] private string gameSceneName = "MainScene";
    [SerializeField] private bool autoDisableWhenNotHost = true;
    [SerializeField] private TMP_Text lobbyStatusText;

    private void OnEnable()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
    }

    private void OnDisable()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }
    }

    private void Update()
    {
        if (!autoDisableWhenNotHost || startGameButton == null)
        {
            UpdateLobbyStatus();
            return;
        }

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        startGameButton.interactable = isHost;
        startGameButton.gameObject.SetActive(isHost);

        UpdateLobbyStatus();
    }

    private void StartGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("LobbyController: NetworkManager is missing.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("LobbyController: Only the host can start the game.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void UpdateLobbyStatus()
    {
        if (lobbyStatusText == null)
        {
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            lobbyStatusText.text = string.Empty;
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            int count = NetworkManager.Singleton.ConnectedClientsList.Count;
            string suffix = count == 1 ? "player" : "players";
            lobbyStatusText.text = $"Hosting, {count} {suffix} in the lobby ready";
        }
        else
        {
            lobbyStatusText.text = "Connected, waiting for host to start";
        }
    }
}
