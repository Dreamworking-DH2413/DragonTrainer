using UnityEngine;

public class TerrainStartRingSpawner : MonoBehaviour
{
    [Header("Start Ring Settings")]
    public GameObject ringPrefab;
    [Range(0f, 1f)]
    public float spawnChance = 0.3f; // 30% chance per chunk
    public float heightAboveTerrain = 20f;
    public float randomOffsetRange = 10f;
    
    [Header("Manager Reference")]
    private RingSystemManager ringSystemManager;
    
    private Ring spawnedRing;
    
    void Start()
    {
        ringSystemManager = FindFirstObjectByType<RingSystemManager>();

        if (ringSystemManager == null)
        {
            Debug.LogError("TerrainStartRingSpawner: No RingSystemManager found in the scene.");
            return;
        }

        // Random chance to spawn a start ring on this chunk
        if (Random.value < spawnChance && ringSystemManager.courseStarted.Value == false)
        {
            SpawnStartRing();
        }
    }
    
    void SpawnStartRing()
    {
        // Get the center of this terrain chunk
        Vector3 spawnPos = transform.position;
        
        // Get terrain component to find the actual terrain height
        Terrain terrain = GetComponent<Terrain>();
        if (terrain != null)
        {
            float terrainHeight = terrain.SampleHeight(spawnPos);
            spawnPos.y = transform.position.y + terrainHeight + heightAboveTerrain;
        }
        else
        {
            spawnPos.y += heightAboveTerrain;
        }
        
        // Add random offset within the chunk
        spawnPos.x += Random.Range(-randomOffsetRange, randomOffsetRange);
        spawnPos.z += Random.Range(randomOffsetRange/2, randomOffsetRange);
        
        // Spawn the ring
        GameObject ringObj = Instantiate(ringPrefab, spawnPos, Quaternion.identity);
        spawnedRing = ringObj.GetComponent<Ring>();
        
        // Initialize as start ring
        spawnedRing.Initialize(ringSystemManager, -1, true);
        spawnedRing.SetActive(true);
        
        // Register with the manager
        ringSystemManager.RegisterStartRing(spawnedRing);
        
       // Debug.Log($"Start ring spawned at {spawnPos}");
    }
    
    void OnDestroy()
    {
        // Unregister and clean up the ring when this terrain chunk is destroyed
        if (spawnedRing != null && ringSystemManager != null)
        {
            ringSystemManager.UnregisterStartRing(spawnedRing);
            Destroy(spawnedRing.gameObject);
        }
    }
}