using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Tile & Data")]
    [Tooltip("World meters per terrain tile (X/Z).")]
    public int tileSizeMeters = 512;
    [Tooltip("Must be 2^n + 1: 257, 513, 1025...")]
    public int heightmapResolution = 257;
    [Tooltip("Max height in meters (TerrainData.size.y).")]
    public float maxHeight = 60f;

    [Header("Streaming")]
    [Tooltip("How many tiles out from the center to keep (1 => 3x3, 2 => 5x5).")]
    public int viewRadius = 2;
    [Tooltip("Only update streaming if player moved this many meters.")]
    public float updateMoveThreshold = 32f;

    [Header("Noise")]
    public float noiseScale = 200f;
    public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2.0f;
    public int seed = 1337;
    public Vector2 globalOffset;

    [Header("Terrain Render Tweaks")]
    public bool drawInstanced = true;
    [Range(1f, 50f)] public float pixelError = 8f;
    public float baseMapDistance = 600f;
    public float detailDistance = 100f;
    public float treeDistance = 700f;

    struct Tile
    {
        public GameObject go;
        public Terrain terrain;
        public TerrainData data;
    }

    readonly Dictionary<Vector2Int, Tile> _tiles = new();
    Vector3 _lastUpdatePos;
    System.Random _rng;
    Vector2 _seedOffset;

    void Awake()
    {
        if (target == null)
        {
            Debug.LogError("[InfiniteTerrain] Assign a target Transform.");
            enabled = false;
            return;
        }
        _rng = new System.Random(seed);
        _seedOffset = new Vector2(_rng.Next(-100000, 100000), _rng.Next(-100000, 100000));
        _lastUpdatePos = target.position - new Vector3(updateMoveThreshold * 2f, 0, updateMoveThreshold * 2f);
        heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution - 1) + 1; // force 2^n+1
    }

    void Update()
    {
        // Update only when the player actually moved far enough (cheap early out)
        if ((target.position - _lastUpdatePos).sqrMagnitude < updateMoveThreshold * updateMoveThreshold)
            return;

        _lastUpdatePos = target.position;
        UpdateVisibleTiles();
    }

    void UpdateVisibleTiles()
    {
        var center = WorldToTile(target.position);
        var wanted = new HashSet<Vector2Int>();

        for (int dz = -viewRadius; dz <= viewRadius; dz++)
        {
            for (int dx = -viewRadius; dx <= viewRadius; dx++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dz);
                wanted.Add(c);
                if (!_tiles.ContainsKey(c))
                    CreateTile(c);
            }
        }

        // Destroy tiles we no longer want
        var toRemove = new List<Vector2Int>();
        foreach (var kv in _tiles)
            if (!wanted.Contains(kv.Key))
                toRemove.Add(kv.Key);

        foreach (var key in toRemove)
            DestroyTile(key);

        // Wire neighbors (do after additions/removals)
        RefreshNeighbors();
    }

    Vector2Int WorldToTile(Vector3 worldPos)
    {
        int tx = Mathf.FloorToInt(worldPos.x / tileSizeMeters);
        int tz = Mathf.FloorToInt(worldPos.z / tileSizeMeters);
        return new Vector2Int(tx, tz);
    }

    void CreateTile(Vector2Int coord)
    {
        var data = new TerrainData
        {
            heightmapResolution = heightmapResolution,
            size = new Vector3(tileSizeMeters, maxHeight, tileSizeMeters)
        };

        var layer = new TerrainLayer();
        layer.diffuseTexture = Texture2D.whiteTexture; // or your grass/dirt texture
        layer.tileSize = new Vector2(10, 10);
        data.terrainLayers = new TerrainLayer[] { layer };

        var heights = GenerateHeights(coord, data.heightmapResolution);
        data.SetHeights(0, 0, heights);

        var go = new GameObject($"Terrain_{coord.x}_{coord.y}");
        go.transform.position = new Vector3(coord.x * tileSizeMeters, 0f, coord.y * tileSizeMeters);

        var terrain = go.AddComponent<Terrain>();
        terrain.terrainData = data;
        terrain.drawInstanced = drawInstanced;
        terrain.heightmapPixelError = pixelError;
        terrain.basemapDistance = baseMapDistance;
        terrain.detailObjectDistance = detailDistance;
        terrain.treeDistance = treeDistance;

        var collider = go.AddComponent<TerrainCollider>();
        collider.terrainData = data;

        _tiles[coord] = new Tile { go = go, terrain = terrain, data = data };
    }

    void DestroyTile(Vector2Int coord)
    {
        if (_tiles.TryGetValue(coord, out var tile))
        {
            if (tile.go != null) Destroy(tile.go);
            _tiles.Remove(coord);
        }
    }

    void RefreshNeighbors()
    {
        foreach (var kv in _tiles)
        {
            var c = kv.Key;
            Terrain left = _tiles.TryGetValue(new Vector2Int(c.x - 1, c.y), out var L) ? L.terrain : null;
            Terrain right = _tiles.TryGetValue(new Vector2Int(c.x + 1, c.y), out var R) ? R.terrain : null;
            Terrain top = _tiles.TryGetValue(new Vector2Int(c.x, c.y + 1), out var T) ? T.terrain : null;
            Terrain bottom = _tiles.TryGetValue(new Vector2Int(c.x, c.y - 1), out var B) ? B.terrain : null;

            kv.Value.terrain.SetNeighbors(left, top, right, bottom);
        }
    }

    float[,] GenerateHeights(Vector2Int tileCoord, int res)
    {
        // res == heightmapResolution (e.g., 257). Unity expects [z,x] indexing.
        float[,] h = new float[res, res];

        // Convert each height sample into world coordinates for consistent noise across tile seams.
        // We sample exactly on the grid: (res-1) spans the tile width.
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                // Local 0..1 across tile
                float u = (float)x / (res - 1);
                float v = (float)z / (res - 1);

                // World meters of this sample
                float worldX = (tileCoord.x * tileSizeMeters) + u * tileSizeMeters;
                float worldZ = (tileCoord.y * tileSizeMeters) + v * tileSizeMeters;

                // Map world meters to noise space
                float nx = (worldX + globalOffset.x + _seedOffset.x) / Mathf.Max(1e-3f, noiseScale);
                float nz = (worldZ + globalOffset.y + _seedOffset.y) / Mathf.Max(1e-3f, noiseScale);

                h[z, x] = FractalNoise(nx, nz, octaves, persistence, lacunarity);
            }
        }
        return h;
    }

    static float FractalNoise(float x, float y, int oct, float pers, float lac)
    {
        float amp = 1f, freq = 1f;
        float value = 0f, norm = 0f;

        for (int i = 0; i < oct; i++)
        {
            float px = x * freq;
            float py = y * freq;

            float n = Mathf.PerlinNoise(px, py); // 0..1
            value += n * amp;
            norm += amp;

            amp *= pers;
            freq *= lac;
        }

        // Optionally shape it (e.g., pow) to taste:
        float h = value / Mathf.Max(1e-6f, norm); // 0..1
        // h = Mathf.Pow(h, 1.25f); // example shaping
        return Mathf.Clamp01(h);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        tileSizeMeters = Mathf.Max(32, tileSizeMeters);
        heightmapResolution = Mathf.Clamp(heightmapResolution, 33, 4097);
        // force valid 2^n+1
        heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution - 1) + 1;
        viewRadius = Mathf.Clamp(viewRadius, 0, 8);
        noiseScale = Mathf.Max(1f, noiseScale);
        lacunarity = Mathf.Max(1.01f, lacunarity);
        persistence = Mathf.Clamp01(persistence);
        pixelError = Mathf.Clamp(pixelError, 1f, 50f);
    }
#endif
}
