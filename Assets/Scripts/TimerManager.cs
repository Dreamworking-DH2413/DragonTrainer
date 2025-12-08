using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    
    [Header("Timer Settings")]
    public bool startOnAwake = false;
    public float startingTime = 15f; // Starting countdown time in seconds
    public float timeAddedPerRing = 10f; // Time added when passing through a ring
    
    private float remainingTime = 15f;
    private bool isRunning = false;

    public RingSystemManager ringSystemManager;
    
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
        if (isRunning)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerDisplay();
            
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                StopTimer();
                Debug.Log("Time's up!");
                ringSystemManager.CompleteCourse();
            }
        }
    }
    
    public void StartTimer()
    {
        isRunning = true;
        remainingTime = startingTime;
        if (timerText != null)
        {
            timerText.enabled = true; // Show timer when course starts
        }
        Debug.Log($"Timer started! Countdown from {startingTime} seconds.");
    }
    
    public void StopTimer()
    {
        isRunning = false;
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
        remainingTime = startingTime;
        UpdateTimerDisplay();
    }
    
    public void AddTime(float seconds)
    {
        remainingTime += seconds;
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