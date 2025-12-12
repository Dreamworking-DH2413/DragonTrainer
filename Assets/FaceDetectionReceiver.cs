using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unity.Netcode;

/// <summary>
/// Receives mouth open/closed data from Python script via UDP
/// Attach this script to any GameObject in your Unity scene
/// Only runs on HOST - syncs mouth state to all clients via NetworkVariable
/// </summary>
public class MouthDetectionReceiver : NetworkBehaviour
{
    [Header("UDP Settings")]
    [SerializeField] private int port = 5065;
    
    [Header("Mouth State")]
    private NetworkVariable<bool> mouthOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private float marValue = 0f;
    
    [Header("Manual Testing (Host Only)")]
    [SerializeField] private bool manualControl = false;
    [SerializeField] private bool manualMouthState = false;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnMouthOpened;
    public UnityEngine.Events.UnityEvent OnMouthClosed;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;
    private bool previousMouthState = false;
    
    // Properties to access mouth state
    public bool IsMouthOpen => mouthOpen.Value;
    public float MARValue => marValue;
    
    void Start()
    {
        // Only the host receives UDP data from Python
        if (IsServer)
        {
            StartUDPListener();
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to mouth state changes on all clients
        mouthOpen.OnValueChanged += OnMouthStateChanged;
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        mouthOpen.OnValueChanged -= OnMouthStateChanged;
    }
    
    private void OnMouthStateChanged(bool previousValue, bool newValue)
    {
        // This fires on all clients when the value changes
        if (newValue)
        {
            Debug.Log("MOUTH OPEN EVENT (Network Synced)");
            OnMouthOpened?.Invoke();
        }
        else
        {
            Debug.Log("MOUTH CLOSE EVENT (Network Synced)");
            OnMouthClosed?.Invoke();
        }
    }
    
    void Update()
    {
        // Manual control for testing (host only)
        if (IsServer && manualControl)
        {
            if (mouthOpen.Value != manualMouthState)
            {
                mouthOpen.Value = manualMouthState;
                if (showDebugLogs)
                {
                    Debug.Log($"[MANUAL] Mouth state set to: {manualMouthState}");
                }
            }
        }
    }
    
    void StartUDPListener()
    {
        try
        {
            udpClient = new UdpClient(port);
            isRunning = true;
            
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            if (showDebugLogs)
            {
                Debug.Log($"UDP Listener started on port {port}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start UDP listener: {e.Message}");
        }
    }
    
    void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                
                // Parse JSON data
                ParseMouthData(message);
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogWarning($"Error receiving UDP data: {e.Message}");
                }
            }
        }
    }
    
    void ParseMouthData(string json)
    {
        try
        {
            // Simple JSON parsing (you can use JsonUtility for more complex data)
            MouthData data = JsonUtility.FromJson<MouthData>(json);
            
            // Update mouth state (only server can write to NetworkVariable)
            if (IsServer)
            {
                mouthOpen.Value = data.mouth_open;
                marValue = data.mar_value;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[HOST] Mouth state: {(mouthOpen.Value ? "OPEN" : "CLOSED")} (MAR: {marValue:F4})");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error parsing mouth data: {e.Message}");
        }
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        StopUDPListener();
    }
    
    void OnApplicationQuit()
    {
        StopUDPListener();
    }
    
    void StopUDPListener()
    {
        isRunning = false;
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        
        if (udpClient != null)
        {
            udpClient.Close();
        }
        
        if (showDebugLogs)
        {
            Debug.Log("UDP Listener stopped");
        }
    }
    
    // Optional: Display GUI for debugging
    // void OnGUI()
    // {
    //     if (showDebugLogs)
    //     {
    //         GUIStyle style = new GUIStyle(GUI.skin.label);
    //         style.fontSize = 24;
    //         style.normal.textColor = mouthOpen ? Color.red : Color.green;
    //         
    //         GUI.Label(new Rect(10, 10, 400, 50), 
    //             $"Mouth: {(mouthOpen ? "OPEN" : "CLOSED")}", style);
    //         
    //         GUIStyle smallStyle = new GUIStyle(GUI.skin.label);
    //         smallStyle.fontSize = 16;
    //         GUI.Label(new Rect(10, 60, 400, 30), 
    //             $"MAR Value: {marValue:F4}", smallStyle);
    //     }
    // }
}

/// <summary>
/// Data structure for JSON parsing
/// </summary>
[System.Serializable]
public class MouthData
{
    public bool mouth_open;
    public float mar_value;
    public long timestamp;
}