using UnityEngine;
using System.Collections.Generic;

public class RingSystemManager : MonoBehaviour
{
    [Header("Ring Settings")]
    public GameObject ringPrefab;
    public int courseLength = 20; // Total rings in the course (excluding start ring)
    public int ringsAheadVisible = 5; // How many rings to keep visible ahead
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
    
    [Header("Start Ring Management")]
    public static RingSystemManager Instance { get; private set; }
    
    private Queue<Ring> activeRings = new Queue<Ring>();
    private List<Ring> allCourseRings = new List<Ring>();
    private List<Ring> allStartRings = new List<Ring>(); // Track all start rings in the world
    private Vector3 nextRingPosition;
    private int currentActiveRingIndex = 0;
    private int ringsPassedThrough = 0;
    private bool courseStarted = false;
    private bool courseCompleted = false;
    private Ring startRing;
    
    void Start()
    {
        // Set up singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Don't generate a start ring here anymore - let terrain chunks do it
    }
    
    // Called by terrain chunks to register their start rings
    public void RegisterStartRing(Ring ring)
    {
        if (!allStartRings.Contains(ring))
        {
            allStartRings.Add(ring);
        }
    }
    
    // Called by terrain chunks when they're destroyed
    public void UnregisterStartRing(Ring ring)
    {
        allStartRings.Remove(ring);
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
        
        Debug.Log("Start ring created! Fly through it to begin the course.");
    }
    
    public void OnStartRingPassed()
    {
        if (courseStarted)
        {
            Debug.Log("Course already started, ignoring duplicate call");
            return;
        }
        
        courseStarted = true;
        courseCompleted = false;
        Debug.Log("=== START RING PASSED ===");
        Debug.Log("Course started! Generating all rings...");
        
        // Hide all other start rings
        HideAllStartRings();
        
        // Start timer
        if (timerManager != null)
        {
            Debug.Log("Starting timer...");
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
    
    void HideAllStartRings()
    {
        foreach (Ring ring in allStartRings)
        {
            if (ring != null)
            {
                ring.gameObject.SetActive(false);
            }
        }
    }
    
    void ShowAllStartRings()
    {
        foreach (Ring ring in allStartRings)
        {
            if (ring != null)
            {
                ring.gameObject.SetActive(true);
            }
        }
    }
    
    void GenerateEntireCourse()
    {
        // Start from the position of the start ring that was passed
        nextRingPosition = startRing != null ? startRing.transform.position : transform.position;
        
        for (int i = 0; i < courseLength; i++)
        {
            // Calculate next ring position with some randomization
            nextRingPosition += Vector3.forward * ringSpacing;
            nextRingPosition.x += Random.Range(-pathWidth, pathWidth);
            nextRingPosition.y += Random.Range(-pathHeight, pathHeight);
            
            // Random rotation for variety
            Quaternion rotation = Quaternion.Euler(
                Random.Range(-15f, 15f),
                Random.Range(-30f, 30f),
                Random.Range(-15f, 15f)
            );
            
            GameObject ringObj = Instantiate(ringPrefab, nextRingPosition, rotation);
            Ring ring = ringObj.GetComponent<Ring>();
            
            if (ring == null)
            {
                ring = ringObj.AddComponent<Ring>();
            }
            
            bool isLastRing = (i == courseLength - 1);
            ring.Initialize(this, i, false, isLastRing);
            allCourseRings.Add(ring);
        }
        
        Debug.Log($"Generated {courseLength} rings for the course!");
    }
    
    public void OnRingPassed(int ringIndex, bool isLastRing)
    {
        if (!courseStarted || courseCompleted) return;
        
        // Only count if it's the current active ring
        if (ringIndex == currentActiveRingIndex)
        {
            ringsPassedThrough++;
            currentActiveRingIndex++;
            
            Debug.Log($"Ring {ringIndex + 1}/{courseLength} passed! Total hits: {ringsPassedThrough}");
            
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
    
    public void OnRingMissed(int ringIndex)
    {
        if (!courseStarted || courseCompleted) return;
        
        // If the player flew past the active ring without hitting it
        if (ringIndex == currentActiveRingIndex)
        {
            Debug.Log($"Ring {ringIndex + 1} missed! Moving to next ring.");
            currentActiveRingIndex++;
            
            // Check if that was the last ring
            if (currentActiveRingIndex >= allCourseRings.Count)
            {
                CompleteCourse();
            }
            else
            {
                // Activate next ring
                allCourseRings[currentActiveRingIndex].SetActive(true);
            }
        }
    }
    
    void CompleteCourse()
    {
        courseCompleted = true;
        courseStarted = false; // Allow starting a new course
        
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
        currentActiveRingIndex = 0;
        ringsPassedThrough = 0;
        
        // Show all start rings again
        ShowAllStartRings();
        
        Debug.Log($"=== COURSE COMPLETED ===");
        Debug.Log($"Time: {timerManager?.GetFormattedTime() ?? "N/A"}");
        Debug.Log($"Rings Hit: {ringsPassedThrough}/{courseLength}");
        Debug.Log($"Accuracy: {(float)ringsPassedThrough / courseLength * 100f:F1}%");
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