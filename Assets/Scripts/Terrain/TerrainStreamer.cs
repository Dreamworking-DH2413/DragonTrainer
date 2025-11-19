using System.Collections.Generic;
using UnityEngine;

public class TerrainStreamer : MonoBehaviour
{
    [Header("Streaming Setup")]
    public Transform target;
    public ProceduralTerrain terrainPrefab;
    public int viewRadius = 2;

    [Tooltip("How far the target must move before we refresh visible tiles.")]
    public float updateMoveThreshold = 32f;

    [Tooltip("Global offset applied to all noise tiles.")]
    public Vector2 globalNoiseOffset;

    [Tooltip("How many tiles we are allowed to spawn per frame.")]
    public int tilesPerFrame = 1;

    // Active tiles in the world
    readonly Dictionary<Vector2Int, ProceduralTerrain> tiles = new();

    // Tiles waiting to be spawned
    readonly Queue<Vector2Int> _spawnQueue = new();

    // Track tiles already queued so we don't enqueue duplicates
    readonly HashSet<Vector2Int> _pendingTiles = new();

    Vector3 lastUpdatePos;

    void Start()
    {
        if (!target || !terrainPrefab)
        {
            Debug.LogError("[TerrainStreamer] Assign target and terrainPrefab.");
            enabled = false;
            return;
        }

        // Force an initial refresh by placing the "last" position far away
        lastUpdatePos = target.position - new Vector3(updateMoveThreshold * 2f, 0, updateMoveThreshold * 2f);
        RefreshTiles();
    }

    void Update()
    {
        // Spawn tiles within our per-frame budget
        int budget = tilesPerFrame;
        while (budget-- > 0 && _spawnQueue.Count > 0)
        {
            SpawnTile(_spawnQueue.Dequeue());
        }

        // Only refresh when target moves far enough
        if ((target.position - lastUpdatePos).sqrMagnitude < updateMoveThreshold * updateMoveThreshold)
            return;

        lastUpdatePos = target.position;
        RefreshTiles();
    }

    void RefreshTiles()
    {
        var center = WorldToTile(target.position);
        var wanted = new HashSet<Vector2Int>();

        // Determine which tiles we want within viewRadius
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

        // Find tiles to remove
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
        // We're now actively creating this tile, so it's no longer "pending"
        _pendingTiles.Remove(coord);

        var pt = Instantiate(terrainPrefab, transform);

        pt.transform.position = new Vector3(
            coord.x * terrainPrefab.terrainSizeX,
            0f,
            coord.y * terrainPrefab.terrainSizeZ
        );

        // Noise offset chosen so that tiles line up seamlessly in noise-space
        pt.noiseOffset = globalNoiseOffset + new Vector2(
            coord.x * terrainPrefab.noiseScale,
            coord.y * terrainPrefab.noiseScale
        );

        pt.Generate();

        tiles[coord] = pt;
    }

    void DestroyTile(Vector2Int coord)
    {
        if (tiles.TryGetValue(coord, out var pt))
        {
            if (pt)
                Destroy(pt.gameObject);
            tiles.Remove(coord);
        }

        // If it was still queued somehow, clean that too
        _pendingTiles.Remove(coord);
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
}
