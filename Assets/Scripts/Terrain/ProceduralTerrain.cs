using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class ProceduralTerrain : MonoBehaviour

{
    //Sheep herd prefab
    public GameObject Herd;
    public int oneInXChanceToSpawn = 60; //1 in X chance to spawn a herd

    [Header("Terrain Settings")]
    [Tooltip("Must be 2^n + 1 (e.g. 257, 513, 1025)")]
    public int heightmapResolution = 257;

    [Tooltip("World size of this terrain tile in X")]
    public float terrainSizeX = 256f;

    [Tooltip("World size of this terrain tile in Z")]
    public float terrainSizeZ = 256f;

    [Tooltip("Maximum height of the terrain in world units")]
    public float maxHeight = 256f;

    [Tooltip("Water level")]
    public float waterLevel = 100f;

    [Header("Noise Settings")]
    [Tooltip("Noise span per tile in noise space (not world units)")]
    public float noiseScale = 1f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;
    public Vector2 noiseOffset;
    [Header("Texturing")]
    [Tooltip("Sand texture (used below grassHeightWorld).")]
    public Texture2D sandTexture;

    [Tooltip("Grass texture (used above grassHeightWorld).")]
    public Texture2D grassTexture;

    [Tooltip("World-space height where grass starts.")]
    public float grassHeightWorld = 100f;

    [Tooltip("World-space range over which sand blends into grass.")]
    public float blendRange = 10f;

    [Tooltip("Tiling size for terrain textures.")]
    public float textureTileSize = 10f;

    private Terrain _terrain;
    private TerrainData _data;
    

    void Awake()
    {
        _terrain = GetComponent<Terrain>();

        // Clone the TerrainData so each tile has its own independent heightmap.
        _data = Instantiate(_terrain.terrainData);
        _terrain.terrainData = _data;
        //make collider follow the data of the terrain
        var terrainCollider = GetComponent<TerrainCollider>();
        if (terrainCollider != null)
            terrainCollider.terrainData = _data;
    }

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (_terrain == null)
            _terrain = GetComponent<Terrain>();
        if (_data == null)
        {
            _data = Instantiate(_terrain.terrainData);
            _terrain.terrainData = _data;
        }

        // Configure terrain data for this tile
        _data.heightmapResolution = heightmapResolution;
        _data.size = new Vector3(terrainSizeX, maxHeight, terrainSizeZ);

        // (Optional) make alphamap reasonably detailed
        _data.alphamapResolution = heightmapResolution;

        int w = _data.heightmapResolution;
        int h = _data.heightmapResolution;

        float[,] heights = new float[h, w];

        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / (w - 1) * noiseScale + noiseOffset.x;
                float nz = (float)z / (h - 1) * noiseScale + noiseOffset.y;

                float sample = FractalNoise(nx, nz);
                heights[z, x] = Mathf.Clamp01(sample);
            }
        }

        _data.SetHeights(0, 0, heights);

        ApplyTextureSplatmap();
        
        //Spawn sheep herd with 1 in X chance
        int rng = Random.Range(0,oneInXChanceToSpawn+1);
        if(rng>=oneInXChanceToSpawn-1) //1 in X chance to spawn a herd at all
        {
            Debug.Log(rng);
            //Herd object will be child of this terrain tile/thus be destroyed with the tile
            Instantiate(Herd, this.transform.position, Quaternion.identity, this.transform);        
        }
    }

    float FractalNoise(float x, float y)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float value = 0f;
        float amplitudeSum = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float nx = x * frequency;
            float ny = y * frequency;

            float perlin = Mathf.PerlinNoise(nx, ny);
            value += perlin * amplitude;
            amplitudeSum += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return value / amplitudeSum;
    }

    void ApplyTextureSplatmap()
    {
        if (sandTexture == null || grassTexture == null)
        {
            Debug.LogWarning($"{name}: Sand/Grass textures not assigned on ProceduralTerrain.");
            return;
        }

        TerrainData td = _terrain.terrainData;

        // Create terrain layers for sand & grass
        var sandLayer = new TerrainLayer();
        sandLayer.diffuseTexture = sandTexture;
        sandLayer.tileSize = new Vector2(textureTileSize, textureTileSize);

        var grassLayer = new TerrainLayer();
        grassLayer.diffuseTexture = grassTexture;
        grassLayer.tileSize = new Vector2(textureTileSize, textureTileSize);

        // Order matters: index 0 = sand, index 1 = grass
        td.terrainLayers = new TerrainLayer[] { sandLayer, grassLayer };

        int alphaWidth = td.alphamapWidth;
        int alphaHeight = td.alphamapHeight;
        int layers = td.alphamapLayers; // should be 2 now

        float[,,] alphas = new float[alphaHeight, alphaWidth, layers];

        for (int z = 0; z < alphaHeight; z++)
        {
            for (int x = 0; x < alphaWidth; x++)
            {
                // Normalized coordinates across the terrain [0,1]
                float u = (float)x / (alphaWidth - 1);
                float v = (float)z / (alphaHeight - 1);

                // Sample world-space height from terrain
                float heightWorld = td.GetInterpolatedHeight(u, v);

                // t = 0 -> pure sand, t = 1 -> pure grass, smooth around grassHeightWorld
                float t = Mathf.InverseLerp(
                    grassHeightWorld - blendRange,
                    grassHeightWorld + blendRange,
                    heightWorld
                );

                float sandWeight = 1f - t;
                float grassWeight = t;

                // Ensure weights sum to 1
                float sum = sandWeight + grassWeight;
                if (sum > 0f)
                {
                    sandWeight /= sum;
                    grassWeight /= sum;
                }

                alphas[z, x, 0] = sandWeight;  // sand layer
                alphas[z, x, 1] = grassWeight; // grass layer
            }
        }

        td.SetAlphamaps(0, 0, alphas);
    }
}
