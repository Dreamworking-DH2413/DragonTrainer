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
    [SerializeField] private float connectionTimeout = 10f;

    private float connectionAttemptTime;
    private bool isAttemptingConnection = false;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        // Subscribe to connection events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        
        // Log network diagnostics
        LogNetworkDiagnostics();
    }

    private void Update()
    {
        // Check for connection timeout
        if (isAttemptingConnection && Time.time - connectionAttemptTime > connectionTimeout)
        {
            isAttemptingConnection = false;
            Debug.LogError($"[NETWORK] ⏱️ Connection timeout after {connectionTimeout} seconds");
            Debug.LogError("[NETWORK] Connection failed. Possible reasons:\n" +
                          "- Host is not running\n" +
                          "- Wrong IP address\n" +
                          "- Firewall blocking port " + port + "\n" +
                          "- Different network/subnet");
        }
    }

    private void LogNetworkDiagnostics()
    {
        Debug.Log("[NETWORK] === NETWORK DIAGNOSTICS ===");
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            Debug.Log($"[NETWORK] Transport found: UnityTransport");
            Debug.Log($"[NETWORK] Default Port: {port}");
        }
        else
        {
            Debug.LogError("[NETWORK] No UnityTransport found on NetworkManager!");
        }
        Debug.Log("[NETWORK] ==========================");
    }

    private void OnHostClicked()
    {
        Debug.Log("[NETWORK] Starting as Host...");
        StartConnection(true);
    }

    private void OnJoinClicked()
    {
        Debug.Log($"[NETWORK] Starting as Client, connecting to {hostIP}:{port}...");
        StartConnection(false);
    }

    private void StartConnection(bool asHost)
    {
        Debug.Log("[NETWORK] === STARTING CONNECTION ===");
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        if (transport == null)
        {
            Debug.LogError("[NETWORK] FATAL: No UnityTransport component found!");
            return;
        }
        
        if (asHost)
        {
            // Host should listen on all network interfaces (0.0.0.0)
            Debug.Log($"[NETWORK] 🖥️  HOST MODE");
            Debug.Log($"[NETWORK] 📡 Binding to: 0.0.0.0:{port}");
            Debug.Log($"[NETWORK] ✅ This allows connections from any IP on the network");
            Debug.Log($"[NETWORK] 💡 Host's local IP (for clients to connect to): Check your network settings");
            
            transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            
            try
            {
                NetworkManager.Singleton.StartHost();
                Debug.Log($"[NETWORK] ✓ StartHost() called successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NETWORK] ❌ StartHost() failed: {e.Message}\n{e.StackTrace}");
                return;
            }
            
            // Update GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerType(true);
            }
        }
        else
        {
            // Client connects to specific host IP
            Debug.Log($"[NETWORK] 💻 CLIENT MODE");
            Debug.Log($"[NETWORK] 🎯 Target Host: {hostIP}:{port}");
            Debug.Log($"[NETWORK] 📡 Attempting connection...");
            
            transport.SetConnectionData(hostIP, port);
            
            // Start connection timeout timer
            connectionAttemptTime = Time.time;
            isAttemptingConnection = true;
            
            try
            {
                NetworkManager.Singleton.StartClient();
                Debug.Log($"[NETWORK] ✓ StartClient() called successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NETWORK] ❌ StartClient() failed: {e.Message}\n{e.StackTrace}");
                isAttemptingConnection = false;
                return;
            }
        }
        
        Debug.Log("[NETWORK] ===========================");
        HideButtons();
    }

    private void OnServerStarted()
    {
        Debug.Log("[NETWORK] === SERVER STARTED ===");
        Debug.Log("[NETWORK] ✅ Host is now listening for connections");
        Debug.Log($"[NETWORK] 📡 Port: {port}");
        Debug.Log("[NETWORK] ⏳ Waiting for client to connect...");
        Debug.Log("[NETWORK] ======================");
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("[NETWORK] === CLIENT CONNECTED ===");
        Debug.Log($"[NETWORK] Client ID: {clientId}");
        
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[NETWORK] ✅ CLIENT {clientId} CONNECTED TO THIS HOST");
            Debug.Log($"[NETWORK] Total connected clients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
        }
        else if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[NETWORK] ✅ THIS CLIENT SUCCESSFULLY CONNECTED TO HOST!");
            isAttemptingConnection = false;
            
            // Update GameManager for client
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerType(false);
            }
        }
        Debug.Log("[NETWORK] ========================");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("[NETWORK] === CLIENT DISCONNECTED ===");
        Debug.Log($"[NETWORK] Client ID: {clientId}");
        
        isAttemptingConnection = false;
        
        // If we're the client that got disconnected, show error
        if (NetworkManager.Singleton != null && 
            NetworkManager.Singleton.IsClient && 
            !NetworkManager.Singleton.IsHost && 
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogError("[NETWORK] ❌ CONNECTION FAILED\n" +
                          "\nTroubleshooting checklist:\n" +
                          "1. ✓ Is host computer running the game as Host?\n" +
                          "2. ✓ Is the host IP correct? (Current: " + hostIP + ")\n" +
                          "3. ✓ Are both computers on the same local network?\n" +
                          "4. ✓ Is port " + port + " open in host's firewall?\n" +
                          "5. ✓ Try disabling antivirus/firewall temporarily\n" +
                          "6. ✓ Check host's IP with ipconfig/ifconfig\n" +
                          "\nTo open firewall port on host (Windows):\n" +
                          "  netsh advfirewall firewall add rule name=\"Unity Game\" dir=in action=allow protocol=TCP localport=" + port + "\n" +
                          "  netsh advfirewall firewall add rule name=\"Unity Game\" dir=in action=allow protocol=UDP localport=" + port);
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning($"[NETWORK] ⚠️ Client {clientId} disconnected from host");
        }
        
        Debug.Log("[NETWORK] ===========================");
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