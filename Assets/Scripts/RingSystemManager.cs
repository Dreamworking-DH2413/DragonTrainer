using UnityEngine;
using System.Collections.Generic;

public class RingSystemManager : MonoBehaviour
{
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
    
    private Queue<Ring> activeRings = new Queue<Ring>();
    private List<Ring> allCourseRings = new List<Ring>();
    private List<Ring> allStartRings = new List<Ring>(); // Track all start rings
    private Vector3 nextRingPosition;
    private Quaternion currentPathRotation = Quaternion.identity;
    private int currentActiveRingIndex = 0;
    private int ringsPassedThrough = 0;
    public bool courseStarted = false;
    private bool courseCompleted = false;
    private Ring startRing;
    
    void Start()
    {
        nextRingPosition = transform.position;
        
        // Only generate start ring if not using terrain spawner
        if (spawnStartRingOnInit)
        {
            GenerateStartRing();
        }
        else
        {
            Debug.Log("RingSystemManager initialized. Waiting for terrain-spawned start ring...");
        }
    }
    
    void GenerateStartRing()
    {
        GameObject ringObj = Instantiate(ringPrefab, nextRingPosition, Quaternion.identity);
        startRing = ringObj.GetComponent<Ring>();
        
        if (startRing == null)
        {
            startRing = ringObj.AddComponent<Ring>();
        }
        
        startRing.Initialize(this, -1, true); // -1 index indicates start ring
        startRing.SetActive(true);
        RegisterStartRing(startRing);
        
        Debug.Log($"Start ring created at position: {nextRingPosition}");
    }
    
    public void OnStartRingPassed(Ring passedStartRing)
    {
        courseStarted = true;
        // Destroy all other start rings
        foreach (Ring sr in allStartRings)
        {
            if (sr != null && sr != startRing)
                Destroy(sr.gameObject);
        }
        nextRingPosition = passedStartRing.transform.position;

        allStartRings.Clear();
        
        // Start timer
        if (timerManager != null)
        {
            timerManager.StartTimer();
            Debug.Log($"Timer running: {timerManager.IsRunning()}");
        }
        else
        {
            Debug.LogError("TimerManager is NULL! Did you assign it in the Inspector?");
        }
        
        // Destroy start ring
        if (startRing != null)
        {
            Destroy(startRing.gameObject);
        }
        
        // Generate ALL course rings instantly
        GenerateEntireCourse();
        
        // Activate first ring
        if (allCourseRings.Count > 0)
        {
            allCourseRings[0].SetActive(true);
        }
    }

    void GenerateEntireCourse()
    {
        Debug.Log("GENERATING ENTIRE COURSE...");
        for (int i = 0; i < courseLength; i++)
        {
            // Apply incremental rotation changes to create a winding path
            Vector3 rotationDelta = new Vector3(
                Random.Range(-30f, 30f),  // Pitch variation (up/down)
                Random.Range(-50f, 50f),  // Yaw variation (horizontal turns)
                Random.Range(-25f, 25f)   // Roll variation
            );
            
            currentPathRotation *= Quaternion.Euler(rotationDelta);
            
            // Calculate forward direction based on current path rotation
            Vector3 forwardDirection = currentPathRotation * Vector3.forward;
            
            // Move forward in the rotated direction
            nextRingPosition += forwardDirection * ringSpacing;
            
            // Add some perpendicular variation for more organic paths
            Vector3 rightDirection = currentPathRotation * Vector3.right;
            Vector3 upDirection = currentPathRotation * Vector3.up;
            
            nextRingPosition += rightDirection * Random.Range(-pathWidth * 0.4f, pathWidth * 0.4f);
            nextRingPosition += upDirection * Random.Range(-pathHeight * 0.5f, pathHeight * 0.5f);
            
            // Ensure rings stay above a minimum height (adjust this value as needed)
            if (nextRingPosition.y < 10f)
            {
                nextRingPosition.y = 10f + Random.Range(0f, pathHeight);
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
            
            bool isLastRing = (i == courseLength - 1);
            ring.Initialize(this, i, false, isLastRing);
            allCourseRings.Add(ring);
        }
    }
    
    public void OnRingPassed(int ringIndex, bool isLastRing)
    {
        if (!courseStarted || courseCompleted) return;
        
        // Only count if it's the current active ring
        if (ringIndex == currentActiveRingIndex)
        {
            ringsPassedThrough++;
            currentActiveRingIndex++;
            
           //Debug.Log($"Ring {ringIndex + 1}/{courseLength} passed! Total hits: {ringsPassedThrough}");
            
            if (isLastRing)
            {
                CompleteCourse();
            }
            else
            {
                // Activate next ring
                if (currentActiveRingIndex < allCourseRings.Count)
                {
                    allCourseRings[currentActiveRingIndex].SetActive(true);
                }
            }
        }
    }
    
    void CompleteCourse()
    {
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
        
        Debug.Log($"=== COURSE COMPLETED ===");
        Debug.Log($"Time: {timerManager?.GetFormattedTime() ?? "N/A"}");
        Debug.Log($"Rings Hit: {ringsPassedThrough}/{courseLength}");
        Debug.Log($"Accuracy: {(float)ringsPassedThrough / courseLength * 100f:F1}%");
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