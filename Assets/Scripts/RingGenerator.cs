using UnityEngine;
using System.Collections.Generic;

public class RingSystemManager : MonoBehaviour
{
    [Header("Ring Settings")]
    public GameObject ringPrefab;
    public int initialRingsAhead = 10; // How many rings to keep ahead of player
    public float ringSpacing = 50f;
    public float pathWidth = 20f;
    public float pathHeight = 15f;
    
    [Header("Visual Feedback")]
    public Color activeRingColor = Color.green;
    public Color inactiveRingColor = Color.white;
    public float emissionIntensity = 2f;
    
    private Queue<Ring> activeRings = new Queue<Ring>();
    private Vector3 nextRingPosition;
    private int totalRingsCreated = 0;
    
    void Start()
    {
        nextRingPosition = transform.position;
        
        // Generate initial rings
        for (int i = 0; i < initialRingsAhead; i++)
        {
            GenerateNextRing();
        }
        
        // Make sure first ring is active
        if (activeRings.Count > 0)
        {
            activeRings.Peek().SetActive(true);
        }
    }
    
    void GenerateNextRing()
    {
        // Calculate next ring position with some randomization
        nextRingPosition += Vector3.forward * ringSpacing;
        nextRingPosition.x += Random.Range(-pathWidth, pathWidth);
        nextRingPosition.y += Random.Range(-pathHeight, pathHeight);
        
        // Random rotation for variety
        Quaternion rotation = Quaternion.Euler(
            Random.Range(0, 0),
            Random.Range(0, 0),
            Random.Range(0, 0)
        );
        
        GameObject ringObj = Instantiate(ringPrefab, nextRingPosition, rotation);
        Ring ring = ringObj.GetComponent<Ring>();
        
        if (ring == null)
        {
            ring = ringObj.AddComponent<Ring>();
        }
        
        ring.Initialize(this, totalRingsCreated);
        activeRings.Enqueue(ring);
        totalRingsCreated++;
    }
    
    public void OnRingPassed(int ringIndex)
    {
        if (activeRings.Count > 0)
        {
            Ring passedRing = activeRings.Dequeue();
            
            // Destroy the ring that was passed through
            Destroy(passedRing.gameObject);
            
            // Generate a new ring ahead
            GenerateNextRing();
            
            // Activate the next ring in queue
            if (activeRings.Count > 0)
            {
                activeRings.Peek().SetActive(true);
            }
            
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
    
    public float GetEmissionIntensity()
    {
        return emissionIntensity;
    }
}