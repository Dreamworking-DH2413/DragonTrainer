using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;

public class RingSystemManager : NetworkBehaviour
{
    [Header("Ring UI")]
    public TextMeshProUGUI ringCounter;
    
    [Header("Ring Settings")]
    public GameObject ringPrefab;
    public int courseLength = 20; // Total rings in the course (excluding start ring)
    public bool spawnStartRingOnInit = false; // Set to false if using TerrainStartRingSpawner
    public float ringSpacing = 50f;
    public float pathWidth = 20f;
    public float pathHeight = 15f;
    
    [Header("Visual Feedback")]
    public Color activeRingColor = Color.green;
    public Color inactiveRingColor = Color.white;
    public Color startRingColor = Color.cyan;
    public float emissionIntensity = 2f;
    
    [Header("Timer Integration")]
    public TimerManager timerManager;

    public AudioClip ringPassSound;
    public AudioClip finalRingPassSound;
    private GameObject player;
    private Queue<Ring> activeRings = new Queue<Ring>();
    private List<Ring> allCourseRings = new List<Ring>();
    private List<Ring> allStartRings = new List<Ring>(); // Track all start rings
    private Vector3 nextRingPosition;
    private Quaternion currentPathRotation = Quaternion.identity;
    private Quaternion currentLookDirection = Quaternion.identity;
    private int currentActiveRingIndex = 0;
    private int ringsPassedThrough = 0;
    public bool courseStarted = false;
    private bool courseCompleted = false;
    private Ring startRing;
    private float minHeight = 200f; // Minimum height above ground
    private float maxHeight = 250f; // Maximum height to prevent going too high
            

    void Start()
    {
        AudioClip ringPassSound = Resources.Load<AudioClip>("Sound/SFX/FlyThroughRing");
        AudioClip finalRingPassSound = Resources.Load<AudioClip>("Sound/SFX/FinalRing");
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Debug.LogWarning("Player not found! Make sure player GameObject is tagged as 'Player'");
        }
        nextRingPosition = transform.position;
        
        // Only generate start ring if not using terrain spawner
        if (spawnStartRingOnInit)
        {
            GenerateStartRing();
        }
        else
        {
            // Debug.Log("RingSystemManager initialized. Waiting for terrain-spawned start ring...");
        }
    }
    
    void GenerateStartRing()
    {
        if (!IsServer) return; // Only server spawns rings
        
        if (nextRingPosition.y < 200)
        {
                nextRingPosition.y = minHeight + Random.Range(5f, maxHeight - minHeight);
        }
        GameObject ringObj = Instantiate(ringPrefab, nextRingPosition, Quaternion.identity);

        startRing = ringObj.GetComponent<Ring>();
        
        if (startRing == null)
        {
            startRing = ringObj.AddComponent<Ring>();
        }
        
        // Spawn as NetworkObject
        NetworkObject netObj = ringObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        
        startRing.Initialize(this, -1, true); // -1 index indicates start ring
        startRing.SetActive(true);
        RegisterStartRing(startRing);
        
        // Debug.Log($"Start ring created at position: {nextRingPosition}");
    }
    
    public void OnStartRingPassed(Ring passedStartRing)
    {
        if (!IsServer) return; // Only server handles ring logic
       
        Debug.Log("[RingSystemManager] Start ring passed! Beginning course...");
        ringCounter.text = GetRingsPassedThrough().ToString();
        
        courseStarted = true;
        Vector3 soundPosition = player != null ? player.transform.position : passedStartRing.transform.position;
        AudioSource.PlayClipAtPoint(ringPassSound, soundPosition);

        // Destroy all other start rings
        foreach (Ring sr in allStartRings)
        {
            if (sr != null && sr != startRing)
                Destroy(sr.gameObject);
        }
    
        allStartRings.Clear();
        
        // Start timer
        if (timerManager != null)
        {
            timerManager.StartTimer();
            // Debug.Log($"[RingSystemManager] Timer running: {timerManager.IsRunning()}");
        }
        else
        {
            // Debug.LogError("[RingSystemManager] TimerManager is NULL! Did you assign it in the Inspector?");
        }
        
        // Destroy start ring
        if (startRing != null)
        {
            Destroy(startRing.gameObject);
        }
        
        nextRingPosition = passedStartRing.transform.position;
        
        // Set course direction based on player's forward direction
        Vector3 playerForward = player.transform.forward;
        currentLookDirection = Quaternion.LookRotation(playerForward);
        
        Debug.Log($"Course generating in player's forward direction: {playerForward}");

        // Generate all course rings
        GenerateEntireCourse(currentLookDirection);
        
        // Activate first ring
        if (allCourseRings.Count > 0)
        {
            Debug.Log("[RingSystemManager] Activating first ring of the course.");
            allCourseRings[0].SetActive(true);
        }
    }

    void GenerateEntireCourse(Quaternion currentLookDirection)
    {
        if (!IsServer) return; // Only server generates course
        
        // Initialize path rotation with the player's direction
        currentPathRotation = currentLookDirection;
        
        // Debug.Log("[RingSystemManager] GENERATING ENTIRE COURSE...");
        for (int i = 0; i < courseLength; i++)
        {
            // Get current pitch angle to prevent extreme diving or climbing
            Vector3 currentEuler = currentPathRotation.eulerAngles;
            float currentPitch = currentEuler.x;
            if (currentPitch > 180f) currentPitch -= 360f; // Normalize to -180 to 180
            
            // Limit pitch changes based on current pitch to prevent going too steep
            float pitchChange;
            if (currentPitch > 30f) // Too steep upward, bias downward
            {
                pitchChange = Random.Range(-30f, 10f);
            }
            else if (currentPitch < -30f) // Too steep downward, bias upward
            {
                pitchChange = Random.Range(-10f, 30f);
            }
            else // Normal range, allow full variation
            {
                pitchChange = Random.Range(-25f, 25f);
            }
            
            // Apply incremental rotation changes to create a winding path
            Vector3 rotationDelta = new Vector3(
                pitchChange,              // Pitch variation (up/down) with constraints
                Random.Range(-50f, 50f),  // Yaw variation (horizontal turns)
                Random.Range(-25f, 25f)   // Roll variation
            );
                        
            // Calculate forward direction based on current path rotation
            Vector3 forwardDirection = currentPathRotation * Vector3.forward;
            
            // Move forward in the rotated direction
            nextRingPosition += forwardDirection * ringSpacing;
            
            // Add some perpendicular variation for more organic paths
            Vector3 rightDirection = currentPathRotation * Vector3.right;
            Vector3 upDirection = currentPathRotation * Vector3.up;
            
            nextRingPosition += rightDirection * Random.Range(-pathWidth * 0.4f, pathWidth * 0.4f);
            nextRingPosition += upDirection * Random.Range(-pathHeight * 0.5f, pathHeight * 0.5f);
    
            if (nextRingPosition.y < 200)
            {
                nextRingPosition.y = minHeight + Random.Range(5f, maxHeight - minHeight);
                // Adjust path rotation to tilt upward if we're too low
                Vector3 correctedEuler = currentPathRotation.eulerAngles;
                correctedEuler.x = Mathf.Clamp(correctedEuler.x > 180f ? correctedEuler.x - 360f : correctedEuler.x, -20f, 45f);
                currentPathRotation = Quaternion.Euler(correctedEuler);
            }
            else if (nextRingPosition.y > maxHeight)
            {
                nextRingPosition.y = maxHeight - Random.Range(0f, pathHeight);
                // Adjust path rotation to tilt downward if we're too high
                Vector3 correctedEuler = currentPathRotation.eulerAngles;
                correctedEuler.x = Mathf.Clamp(correctedEuler.x > 180f ? correctedEuler.x - 360f : correctedEuler.x, -45f, 20f);
                currentPathRotation = Quaternion.Euler(correctedEuler);
            }
            
            // Apply additional random rotation to each ring for visual variety
            Quaternion ringRotation = currentPathRotation * Quaternion.Euler(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );
            
            GameObject ringObj = Instantiate(ringPrefab, nextRingPosition, ringRotation);
            Ring ring = ringObj.GetComponent<Ring>();
            
            if (ring == null)
            {
                ring = ringObj.AddComponent<Ring>();
            }
            
            // Spawn as NetworkObject
            NetworkObject netObj = ringObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            
            bool isLastRing = (i == courseLength - 1);
            ring.Initialize(this, i, false, isLastRing);
            allCourseRings.Add(ring);
        }
    }
    
    public void OnRingPassed(int ringIndex, bool isLastRing)
    {
        if (!IsServer) return; // Only server handles ring logic
        
        ringCounter.text = ringsPassedThrough.ToString();
        if (!courseStarted || courseCompleted) return;
        
        // Only count if it's the current active ring
        if (ringIndex == currentActiveRingIndex)
        {
            ringsPassedThrough++;
            currentActiveRingIndex++;
            ringCounter.text = GetRingsPassedThrough().ToString();
            
            
           //// Debug.Log($"Ring {ringIndex + 1}/{courseLength} passed! Total hits: {ringsPassedThrough}");
            
            Ring currentRing = allCourseRings[ringIndex];
            Vector3 soundPosition = player != null ? player.transform.position : currentRing.transform.position;
            
            // Add time for passing through ring
            if (timerManager != null)
            {
                timerManager.AddTimeForRing();
            }
            
            if (isLastRing)
            {
                AudioSource.PlayClipAtPoint(finalRingPassSound, soundPosition);
                CompleteCourse();
            }
            else
            {
                AudioSource.PlayClipAtPoint(ringPassSound, soundPosition);
                // Activate next ring
                if (currentActiveRingIndex < allCourseRings.Count)
                {
                    allCourseRings[currentActiveRingIndex].SetActive(true);
                }
            }
        }
    }
    
    public void CompleteCourse()
    {
        if (!IsServer) return; // Only server handles course completion
        
        courseCompleted = true;
        courseStarted = false;
        // Stop timer
        if (timerManager != null)
        {
            timerManager.StopTimer();
        }
        
        // Destroy all remaining rings
        foreach (Ring ring in allCourseRings)
        {
            if (ring != null)
            {
                Destroy(ring.gameObject);
            }
        }
        allCourseRings.Clear();
        
        // Debug.Log($"=== COURSE COMPLETED ===");
        // Debug.Log($"Time: {timerManager?.GetFormattedTime() ?? "N/A"}");
        // Debug.Log($"Rings Hit: {ringsPassedThrough}/{courseLength}");
        // Debug.Log($"Accuracy: {(float)ringsPassedThrough / courseLength * 100f:F1}%");
    }

    public void RegisterStartRing(Ring ring)
    {
        if (!allStartRings.Contains(ring))
        {
            allStartRings.Add(ring);
        }
    }

    public void UnregisterStartRing(Ring ring)
    {
        if (allStartRings.Contains(ring))
        {
            allStartRings.Remove(ring);
        }
    }
    
    public Color GetActiveColor()
    {
        return activeRingColor;
    }
    
    public Color GetInactiveColor()
    {
        return inactiveRingColor;
    }
    
    public Color GetStartRingColor()
    {
        return startRingColor;
    }
    
    public float GetEmissionIntensity()
    {
        return emissionIntensity;
    }
    
    public int GetRingsPassedThrough()
    {
        return ringsPassedThrough;
    }
    
    public int GetTotalRings()
    {
        return courseLength;
    }
}