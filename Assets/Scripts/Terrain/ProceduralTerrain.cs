using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class ProceduralTerrain : MonoBehaviour
{
    [Header("oneInXChance spawns")]
    public GameObject Herd;
    public int oneInXSheep = 60; //1 in X chance to spawn a herd
    [Header("Terrain Settings")]
    public int heightmapResolution = 257;
    public float terrainSizeX = 256f;
    public float terrainSizeZ = 256f;
    public float maxHeight = 256f;

    [Header("Noise Settings")]
    public float noiseScale = 1f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;
    public Vector2 noiseOffset;

    [Header("Texturing")]
    public Texture2D sandTexture;
    public Texture2D grassTexture;
    public float grassHeightWorld = 100f;
    public float blendRange = 10f;
    public float textureTileSize = 10f;

    [Header("Water")]
    public Material waterMaterial;
    public float waterHeight = 100f;
    public float waterOverlapMargin = 0.001f;

    Terrain _terrain;
    TerrainData _data;

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

        _data.heightmapResolution = heightmapResolution;
        _data.size = new Vector3(terrainSizeX, maxHeight, terrainSizeZ);
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
        CreateOrUpdateWaterPlane();
        // Calculate center of terrain tile
        Vector3 center = transform.position + new Vector3(terrainSizeX * 0.5f, 0f, terrainSizeZ * 0.5f); //corner + half size
        float terrainY = _terrain.SampleHeight(center) + _terrain.transform.position.y; //local origin height + worldspace height
        int rng = Random.Range(0, oneInXSheep + 1);
        center.y = terrainY;
        if (rng >= oneInXSheep - 1 && terrainY > waterHeight+3.0f) //spawn only on land with some margin
        {
            //Debug.Log($"Spawning herd, rng={rng}, terrainY={terrainY}");
            Instantiate(Herd, center, Quaternion.identity, this.transform);
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
            return;

        TerrainData td = _terrain.terrainData;

        var sandLayer = new TerrainLayer();
        sandLayer.diffuseTexture = sandTexture;
        sandLayer.tileSize = new Vector2(textureTileSize, textureTileSize);

        var grassLayer = new TerrainLayer();
        grassLayer.diffuseTexture = grassTexture;
        grassLayer.tileSize = new Vector2(textureTileSize, textureTileSize);

        td.terrainLayers = new TerrainLayer[] { sandLayer, grassLayer };

        int alphaWidth = td.alphamapWidth;
        int alphaHeight = td.alphamapHeight;
        int layers = td.alphamapLayers;

        float[,,] alphas = new float[alphaHeight, alphaWidth, layers];

        for (int z = 0; z < alphaHeight; z++)
        {
            for (int x = 0; x < alphaWidth; x++)
            {
                float u = (float)x / (alphaWidth - 1);
                float v = (float)z / (alphaHeight - 1);
                float heightWorld = td.GetInterpolatedHeight(u, v);

                float t = Mathf.InverseLerp(
                    grassHeightWorld - blendRange,
                    grassHeightWorld + blendRange,
                    heightWorld
                );

                float sandWeight = 1f - t;
                float grassWeight = t;

                float sum = sandWeight + grassWeight;
                if (sum > 0f)
                {
                    sandWeight /= sum;
                    grassWeight /= sum;
                }

                alphas[z, x, 0] = sandWeight;
                alphas[z, x, 1] = grassWeight;
            }
        }

        td.SetAlphamaps(0, 0, alphas);
    }

    void CreateOrUpdateWaterPlane()
    {
        Transform existing = transform.Find("Water");

        GameObject water;
        if (existing == null)
        {
            water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.transform.SetParent(transform, false);
        }
        else
        {
            water = existing.gameObject;
        }

        float worldWidthX = terrainSizeX + waterOverlapMargin * 2f;
        float worldWidthZ = terrainSizeZ + waterOverlapMargin * 2f;

        float scaleX = worldWidthX / 10f;
        float scaleZ = worldWidthZ / 10f;

        water.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        Vector3 pos = water.transform.localPosition;
        pos.x = terrainSizeX * 0.5f;
        pos.z = terrainSizeZ * 0.5f;
        pos.y = waterHeight - transform.position.y;
        water.transform.localPosition = pos;

        if (waterMaterial != null)
        {
            var renderer = water.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = waterMaterial;
        }
    }
}
