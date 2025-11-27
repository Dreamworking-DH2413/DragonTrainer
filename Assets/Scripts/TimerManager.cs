using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    
    [Header("Timer Settings")]
    public bool startOnAwake = false;
    
    private float elapsedTime = 0f;
    private bool isRunning = false;
    
    void Start()
    {
        if (startOnAwake)
        {
            StartTimer();
        }
        
        if (timerText != null)
        {
            UpdateTimerDisplay();
        }
    }
    
    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }
    
    public void StartTimer()
    {
        isRunning = true;
        elapsedTime = 0f;
        Debug.Log("Timer started!");
    }
    
    public void StopTimer()
    {
        isRunning = false;
        Debug.Log($"Timer stopped at: {GetFormattedTime()}");
    }
    
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerDisplay();
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        
        timerText.text = GetFormattedTime();
    }
    
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);
        
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
    
    public float GetElapsedTime()
    {
        return elapsedTime;
    }
    
    public bool IsRunning()
    {
        return isRunning;
    }
}