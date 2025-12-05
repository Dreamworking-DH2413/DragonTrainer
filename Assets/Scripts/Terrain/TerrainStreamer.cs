using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TerrainStreamer : MonoBehaviour
{
    [Header("Streaming Setup")]
    public Transform target;
    public ProceduralTerrain terrainPrefab;
    public int viewRadius = 2;
    public float updateMoveThreshold = 32f;
    public Vector2 globalNoiseOffset;
    public int tilesPerFrame = 1;

    [Header("Vertical Offset")]
    public float heightOffset = 0f;

    [Header("Noise Domain")]
    public float noiseDomainShift = 10000f;

    [Header("Tree Generation")]
    public bool generateTrees = true;
    public bool generateTreesOnlyInPlayMode = true;

    readonly Dictionary<Vector2Int, ProceduralTerrain> tiles = new();
    readonly Queue<Vector2Int> _spawnQueue = new();
    readonly HashSet<Vector2Int> _pendingTiles = new();

    Vector3 lastUpdatePos;

#if UNITY_EDITOR
    bool _prevGenerateTrees;
    bool _prevGenerateTreesOnlyInPlayMode;
#endif

    void OnEnable()
    {
        if (!target || !terrainPrefab)
            return;

        if (!Application.isPlaying)
        {
            ClearAllTiles(true);
            lastUpdatePos = target.position;
            RefreshTiles();
            SpawnAllQueuedTilesImmediate();
            ApplyHeightOffsetToExisting();
        }
    }

    void Start()
    {
        if (!Application.isPlaying)
            return;

        if (!target || !terrainPrefab)
        {
            // Debug.LogError("[TerrainStreamer] Assign target and terrainPrefab.");
            enabled = false;
            return;
        }

        ClearAllTiles(false);

        lastUpdatePos = target.position - new Vector3(updateMoveThreshold * 2f, 0, updateMoveThreshold * 2f);
        RefreshTiles();
    }

    void Update()
    {
        if (!target || !terrainPrefab)
            return;

        if (Application.isPlaying)
        {
            int budget = tilesPerFrame;
            while (budget-- > 0 && _spawnQueue.Count > 0)
            {
                SpawnTile(_spawnQueue.Dequeue());
            }

            if ((target.position - lastUpdatePos).sqrMagnitude < updateMoveThreshold * updateMoveThreshold)
                return;

            lastUpdatePos = target.position;
            RefreshTiles();
        }
        else
        {
            RefreshTiles();
            SpawnAllQueuedTilesImmediate();
            ApplyHeightOffsetToExisting();
        }
    }

    void OnDisable()
    {
        if (!Application.isPlaying)
        {
            ClearAllTiles(true);
        }
    }

    void RefreshTiles()
    {
        var center = WorldToTile(target.position);
        var wanted = new HashSet<Vector2Int>();

        for (int dz = -viewRadius; dz <= viewRadius; dz++)
        {
            for (int dx = -viewRadius; dx <= viewRadius; dx++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dz);
                wanted.Add(c);

                if (!tiles.ContainsKey(c) && !_pendingTiles.Contains(c))
                {
                    _spawnQueue.Enqueue(c);
                    _pendingTiles.Add(c);
                }
            }
        }

        var toRemove = new List<Vector2Int>();
        foreach (var kv in tiles)
        {
            if (!wanted.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }

        foreach (var key in toRemove)
            DestroyTile(key);

        RefreshNeighbors();
    }

    Vector2Int WorldToTile(Vector3 pos)
    {
        float sx = terrainPrefab.terrainSizeX;
        float sz = terrainPrefab.terrainSizeZ;

        int tx = Mathf.FloorToInt(pos.x / sx);
        int tz = Mathf.FloorToInt(pos.z / sz);

        return new Vector2Int(tx, tz);
    }

    void SpawnTile(Vector2Int coord)
    {
        _pendingTiles.Remove(coord);

        var pt = Instantiate(terrainPrefab, transform);

        float baseX = coord.x * terrainPrefab.terrainSizeX;
        float baseZ = coord.y * terrainPrefab.terrainSizeZ;
        float baseY = transform.position.y + heightOffset;

        pt.transform.position = new Vector3(baseX, baseY, baseZ);

        float nxBase = (coord.x + noiseDomainShift) * terrainPrefab.noiseScale;
        float nzBase = (coord.y + noiseDomainShift) * terrainPrefab.noiseScale;

        pt.noiseOffset = globalNoiseOffset + new Vector2(nxBase, nzBase);

        // Set tree generation flag based on streamer settings
        bool shouldGenerateTrees = generateTrees && (!generateTreesOnlyInPlayMode || Application.isPlaying);
        
        // Temporarily disable tree generation if needed
        GameObject[] originalTreePrefabs = null;
        if (!shouldGenerateTrees && pt.treePrefabs != null && pt.treePrefabs.Length > 0)
        {
            originalTreePrefabs = pt.treePrefabs;
            pt.treePrefabs = new GameObject[0];
        }

        pt.Generate();
        
        // Restore tree prefabs
        if (originalTreePrefabs != null)
        {
            pt.treePrefabs = originalTreePrefabs;
        }

        tiles[coord] = pt;
    }

    void SpawnAllQueuedTilesImmediate()
    {
        while (_spawnQueue.Count > 0)
        {
            SpawnTile(_spawnQueue.Dequeue());
        }
    }

    void DestroyTile(Vector2Int coord)
    {
        if (tiles.TryGetValue(coord, out var pt))
        {
            if (pt)
            {
                if (Application.isPlaying)
                    Object.Destroy(pt.gameObject);
                else
                    Object.DestroyImmediate(pt.gameObject);
            }

            tiles.Remove(coord);
        }

        _pendingTiles.Remove(coord);
    }

    void ClearAllTiles(bool immediate)
    {
        var existing = GetComponentsInChildren<ProceduralTerrain>();
        foreach (var pt in existing)
        {
            if (!pt) continue;
            if (!pt.gameObject.scene.IsValid()) continue;

            if (immediate)
                Object.DestroyImmediate(pt.gameObject);
            else
                Object.Destroy(pt.gameObject);
        }

        tiles.Clear();
        _spawnQueue.Clear();
        _pendingTiles.Clear();
    }

    void RefreshNeighbors()
    {
        foreach (var kv in tiles)
        {
            var c = kv.Key;
            Terrain left = tiles.TryGetValue(new Vector2Int(c.x - 1, c.y), out var L) ? L.GetComponent<Terrain>() : null;
            Terrain right = tiles.TryGetValue(new Vector2Int(c.x + 1, c.y), out var R) ? R.GetComponent<Terrain>() : null;
            Terrain top = tiles.TryGetValue(new Vector2Int(c.x, c.y + 1), out var T) ? T.GetComponent<Terrain>() : null;
            Terrain bottom = tiles.TryGetValue(new Vector2Int(c.x, c.y - 1), out var B) ? B.GetComponent<Terrain>() : null;

            kv.Value.GetComponent<Terrain>().SetNeighbors(left, top, right, bottom);
        }
    }

    void ApplyHeightOffsetToExisting()
    {
        float baseY = transform.position.y + heightOffset;

        foreach (var kv in tiles)
        {
            var pt = kv.Value;
            if (!pt) continue;

            var pos = pt.transform.position;
            pos.y = baseY;
            pt.transform.position = pos;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Regenerate All Chunks")]
    void RegenerateAllChunks()
    {
        if (Application.isPlaying)
        {
            // Debug.LogWarning("Regenerate All Chunks only works in Edit Mode.");
            return;
        }

        ClearAllTiles(true);
        lastUpdatePos = target ? target.position : Vector3.zero;
        RefreshTiles();
        SpawnAllQueuedTilesImmediate();
        ApplyHeightOffsetToExisting();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            bool treeSettingsChanged = (_prevGenerateTrees != generateTrees) || 
                                      (_prevGenerateTreesOnlyInPlayMode != generateTreesOnlyInPlayMode);
            
            _prevGenerateTrees = generateTrees;
            _prevGenerateTreesOnlyInPlayMode = generateTreesOnlyInPlayMode;

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                
                if (treeSettingsChanged)
                {
                    RegenerateAllChunks();
                }
                else
                {
                    ApplyHeightOffsetToExisting();
                }
            };
        }
    }
#endif
}
