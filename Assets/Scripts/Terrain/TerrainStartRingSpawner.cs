using UnityEngine;

public class TerrainStartRingSpawner : MonoBehaviour
{
    [Header("Start Ring Settings")]
    public GameObject ringPrefab;
    [Range(0f, 1f)]
    public float spawnChance = 0.3f; // 30% chance per chunk
    public float heightAboveTerrain = 20f;
    public float randomOffsetRange = 10f;
    
    private Ring spawnedRing;
    
    void Start()
    {
        // Only spawn if we have a ring prefab and the RingSystemManager exists
        if (ringPrefab == null || RingSystemManager.Instance == null)
            return;
        
        // Random chance to spawn a start ring on this chunk
        if (Random.value < spawnChance)
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
        spawnPos.z += Random.Range(-randomOffsetRange, randomOffsetRange);
        
        // Spawn the ring
        GameObject ringObj = Instantiate(ringPrefab, spawnPos, Quaternion.identity);
        spawnedRing = ringObj.GetComponent<Ring>();
        
        if (spawnedRing == null)
        {
            spawnedRing = ringObj.AddComponent<Ring>();
        }
        
        // Initialize as start ring
        spawnedRing.Initialize(RingSystemManager.Instance, -1, true);
        spawnedRing.SetActive(true);
        
        // Register with the manager
        RingSystemManager.Instance.RegisterStartRing(spawnedRing);
        
        Debug.Log($"Start ring spawned at {spawnPos}");
    }
    
    void OnDestroy()
    {
        // Unregister the ring when this terrain chunk is destroyed
        if (spawnedRing != null && RingSystemManager.Instance != null)
        {
            RingSystemManager.Instance.UnregisterStartRing(spawnedRing);
            Destroy(spawnedRing.gameObject);
        }
    }
}