using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private GameObject buttonPanel;

    [Header("Network Settings")]
    [SerializeField] private string hostIP = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        // Subscribe to connection events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnHostClicked()
    {
        Debug.Log("Starting as Host...");
        StartConnection(true);
    }

    private void OnJoinClicked()
    {
        Debug.Log($"Starting as Client, connecting to {hostIP}:{port}...");
        StartConnection(false);
    }

    private void StartConnection(bool asHost)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(hostIP, port);
        
        if (asHost)
        {
            NetworkManager.Singleton.StartHost();
            // Update GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerType(true);
            }
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
        
        HideButtons();
    }

    private void OnServerStarted()
    {
        Debug.Log("✓ Host started successfully! Waiting for client...");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"✓ Client {clientId} connected to host");
        }
        else if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("✓ Successfully connected to host!");
            // Update GameManager for client
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerType(false);
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"✗ Client {clientId} disconnected");
    }

    private void HideButtons()
    {
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (hostButton != null) hostButton.onClick.RemoveListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.RemoveListener(OnJoinClicked);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}