// Returns players to the lobby scene via host or local client action.
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackToLobbyButton : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private bool allowClientLocalReturn = true;

    private void OnEnable()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBackToLobby);
        }
    }

    private void OnDisable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackToLobby);
        }
    }

    private void HandleBackToLobby()
    {
        if (NetworkManager.Singleton == null)
        {
            SceneManager.LoadScene(lobbySceneName);
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
            return;
        }

        if (allowClientLocalReturn)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogWarning("BackToLobbyButton: Only the host can return to lobby.");
        }
    }
}
