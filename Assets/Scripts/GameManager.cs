using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public bool IsHost { get; private set; }
    public bool IsClient { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SetPlayerType(bool isHost)
    {
        IsHost = isHost;
        IsClient = !isHost;
        string playerType = isHost ? "HOST (Dragon)" : "CLIENT (Rider)";
        Debug.Log($"GameManager: Player type set to {playerType}");
    }
}
