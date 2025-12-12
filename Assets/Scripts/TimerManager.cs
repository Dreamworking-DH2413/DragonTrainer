using UnityEngine;
using TMPro;
using Unity.Netcode;

public class TimerManager : NetworkBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    
    [Header("Timer Settings")]
    public bool startOnAwake = false;
    public float startingTime = 15f; // Starting countdown time in seconds
    public float timeAddedPerRing = 10f; // Time added when passing through a ring
    
    // Network synchronized variables
    private NetworkVariable<float> networkRemainingTime = new NetworkVariable<float>(15f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsRunning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    private float remainingTime = 15f;
    private bool isRunning = false;

    public RingSystemManager ringSystemManager;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to network variable changes on clients
        if (!IsServer)
        {
            networkRemainingTime.OnValueChanged += OnRemainingTimeChanged;
            networkIsRunning.OnValueChanged += OnIsRunningChanged;
            
            // Apply initial values
            remainingTime = networkRemainingTime.Value;
            isRunning = networkIsRunning.Value;
            UpdateTimerDisplay();
            
            if (timerText != null)
            {
                timerText.enabled = isRunning;
            }
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (!IsServer)
        {
            networkRemainingTime.OnValueChanged -= OnRemainingTimeChanged;
            networkIsRunning.OnValueChanged -= OnIsRunningChanged;
        }
    }
    
    private void OnRemainingTimeChanged(float oldValue, float newValue)
    {
        remainingTime = newValue;
        UpdateTimerDisplay();
    }
    
    private void OnIsRunningChanged(bool oldValue, bool newValue)
    {
        isRunning = newValue;
        if (timerText != null)
        {
            timerText.enabled = newValue;
        }
    }
    
    void Start()
    {
        if (timerText != null)
        {
            timerText.enabled = false; // Hide timer initially
            UpdateTimerDisplay();
        }
        
        if (startOnAwake)
        {
            StartTimer();
        }
    }
    
    void Update()
    {
        if (IsServer && isRunning)
        {
            remainingTime -= Time.deltaTime;
            networkRemainingTime.Value = remainingTime; // Sync to clients
            UpdateTimerDisplay();
            
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                networkRemainingTime.Value = 0f;
                StopTimer();
                Debug.Log("Time's up!");
                ringSystemManager.CompleteCourse();
            }
        }
        else if (!IsServer)
        {
            // Clients just update display based on synced value
            UpdateTimerDisplay();
        }
    }
    
    public void StartTimer()
    {
        if (!IsServer) return; // Only server can start timer
        
        isRunning = true;
        remainingTime = startingTime;
        
        // Update network variables
        networkIsRunning.Value = true;
        networkRemainingTime.Value = startingTime;
        
        if (timerText != null)
        {
            timerText.enabled = true; // Show timer when course starts
        }
        Debug.Log($"Timer started! Countdown from {startingTime} seconds.");
    }
    
    public void StopTimer()
    {
        if (!IsServer) return; // Only server can stop timer
        
        isRunning = false;
        networkIsRunning.Value = false;
        Debug.Log($"Timer stopped at: {GetFormattedTime()}");
    }
    
    public void HideTimer()
    {
        if (timerText != null)
        {
            timerText.enabled = false;
        }
    }
    
    public void ResetTimer()
    {
        if (!IsServer) return; // Only server can reset timer
        
        remainingTime = startingTime;
        networkRemainingTime.Value = startingTime;
        UpdateTimerDisplay();
    }
    
    public void AddTime(float seconds)
    {
        if (!IsServer) return; // Only server can add time
        
        remainingTime += seconds;
        networkRemainingTime.Value = remainingTime;
        Debug.Log($"Added {seconds} seconds! Time remaining: {GetFormattedTime()}");
    }
    
    public void AddTimeForRing()
    {
        AddTime(timeAddedPerRing);
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        
        timerText.text = GetFormattedTime();
    }
    
    public string GetFormattedTime()
    {
        float timeToDisplay = Mathf.Max(0f, remainingTime);
        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
        int milliseconds = Mathf.FloorToInt((timeToDisplay * 100f) % 100f);
        
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
    
    public float GetRemainingTime()
    {
        return remainingTime;
    }
    
    public bool IsRunning()
    {
        return isRunning;
    }
}