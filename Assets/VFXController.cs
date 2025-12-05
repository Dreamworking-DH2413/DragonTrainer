using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Controls VFX Graph fire effect based on mouth open/closed state
/// Attach this script to the GameObject with your VFX Graph component
/// </summary>
public class MouthControlledFire : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MouthDetectionReceiver mouthReceiver;
    [SerializeField] private VisualEffect fireVFX;
    
    [Header("Control Settings")]
    [SerializeField] private bool playOnMouthOpen = true;  // If false, plays when mouth is closed
    [SerializeField] private bool useSmoothing = true;     // Smooth transitions
    [SerializeField] private float smoothSpeed = 5f;       // How fast to fade in/out
    
    [Header("VFX Parameters (Optional)")]
    [SerializeField] private bool controlSpawnRate = true;
    [SerializeField] private string spawnRateProperty = "SpawnRate";
    [SerializeField] private float minSpawnRate = 0f;
    [SerializeField] private float maxSpawnRate = 100f;
    
    [SerializeField] private bool controlLifetime = false;
    [SerializeField] private string lifetimeProperty = "Lifetime";
    [SerializeField] private float minLifetime = 0.5f;
    [SerializeField] private float maxLifetime = 2f;
    
    private AudioSource audioSource;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private float currentSpawnRate = 0f;
    private float targetSpawnRate = 0f;
    private bool wasPlaying = false;
    
    
    void Start()
    {
        // Get Fire sound
        audioSource = GetComponent<AudioSource>();
        
        // Find MouthDetectionReceiver if not assigned
        if (mouthReceiver == null)
        {
            mouthReceiver = FindObjectOfType<MouthDetectionReceiver>();
            if (mouthReceiver == null)
            {
                // Debug.LogError("MouthDetectionReceiver not found! Please add it to the scene.");
                enabled = false;
                return;
            }
        }
        
        // Find VFX Graph if not assigned
        if (fireVFX == null)
        {
            fireVFX = GetComponent<VisualEffect>();
            if (fireVFX == null)
            {
                // Debug.LogError("VisualEffect component not found! Please attach this script to a GameObject with VFX Graph.");
                enabled = false;
                return;
            }
        }
        
        // Subscribe to mouth events
        mouthReceiver.OnMouthOpened.AddListener(OnMouthOpened);
        mouthReceiver.OnMouthClosed.AddListener(OnMouthClosed);
        
        // Initialize VFX state
        if (!playOnMouthOpen)
        {
            audioSource.Play();
            fireVFX.SetBool("ConstantSpawnrate", true);
            wasPlaying = true;
        }
        else
        {
            audioSource.Stop();
            fireVFX.SetBool("ConstantSpawnrate", false);
            wasPlaying = false;
        }
        
        if (showDebugLogs)
        {
            // Debug.Log($"MouthControlledFire initialized. Play on mouth open: {playOnMouthOpen}");
        }
    }
    
    void Update()
    {
        if (mouthReceiver == null || fireVFX == null) return;
        
        // Determine if fire should be playing based on mouth state
        bool shouldPlay = playOnMouthOpen ? mouthReceiver.IsMouthOpen : !mouthReceiver.IsMouthOpen;
        
        // Simple on/off control
        if (!useSmoothing)
        {
            if (shouldPlay && !wasPlaying)
            {
                fireVFX.SetBool("ConstantSpawnrate", true);
                audioSource.Play();
                wasPlaying = true;
            }
            else if (!shouldPlay && wasPlaying)
            {
                audioSource.Stop();
                fireVFX.SetBool("ConstantSpawnrate", false);
                wasPlaying = false;
            }
        }
        // Smooth control using spawn rate
        else
        {
            // Set target spawn rate
            targetSpawnRate = shouldPlay ? maxSpawnRate : minSpawnRate;
            
            // Smoothly interpolate
            currentSpawnRate = Mathf.Lerp(currentSpawnRate, targetSpawnRate, Time.deltaTime * smoothSpeed);
            
            // Update VFX parameters
            if (controlSpawnRate && fireVFX.HasFloat(spawnRateProperty))
            {
                fireVFX.SetFloat(spawnRateProperty, currentSpawnRate);
            }
            
            if (controlLifetime && fireVFX.HasFloat(lifetimeProperty))
            {
                float lifetimeValue = Mathf.Lerp(minLifetime, maxLifetime, currentSpawnRate / maxSpawnRate);
                fireVFX.SetFloat(lifetimeProperty, lifetimeValue);
            }
            
            // Make sure VFX is playing when using smooth mode
            if (!fireVFX.isActiveAndEnabled || !wasPlaying)
            {
                fireVFX.Play();
                wasPlaying = true;
            }
        }
    }
    
    void OnMouthOpened()
    {
        if (showDebugLogs)
        {
            // Debug.Log($"Mouth opened - Fire {(playOnMouthOpen ? "ON" : "OFF")}");
        }
    }
    
    void OnMouthClosed()
    {
        if (showDebugLogs)
        {
            // Debug.Log($"Mouth closed - Fire {(playOnMouthOpen ? "OFF" : "ON")}");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (mouthReceiver != null)
        {
            mouthReceiver.OnMouthOpened.RemoveListener(OnMouthOpened);
            mouthReceiver.OnMouthClosed.RemoveListener(OnMouthClosed);
        }
    }
    
    // Public methods for external control
    public void SetPlayOnMouthOpen(bool value)
    {
        playOnMouthOpen = value;
        if (showDebugLogs)
        {
            // Debug.Log($"Play on mouth open set to: {value}");
        }
    }
    
    public void TogglePlayMode()
    {
        playOnMouthOpen = !playOnMouthOpen;
        if (showDebugLogs)
        {
            // Debug.Log($"Toggled play mode. Now: {(playOnMouthOpen ? "play on open" : "play on close")}");
        }
    }
}