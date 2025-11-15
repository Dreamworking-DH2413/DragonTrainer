using UnityEngine;

/// <summary>
/// Test harness for FireSimulation - attach to a GameObject
/// Visualizes the 3D density field using a simple slice viewer
/// </summary>
public class FireSimulationTester : MonoBehaviour
{
    [Header("Simulation Settings")]
    public ComputeShader fireComputeShader;
    public int gridX = 64;
    public int gridY = 32;
    public int gridZ = 64;
    
    [Header("Visualization")]
    public Material sliceVisualizerMaterial;
    [Range(0f, 1f)]
    public float sliceDepth = 0.5f;
    public bool showVelocity = false;
    
    private FireSimulation sim;
    private RenderTexture densityTexture;
    private RenderTexture velocityTexture;

    void Start()
    {
        // Initialize simulation
        sim = new FireSimulation();
        sim.gridX = gridX;
        sim.gridY = gridY;
        sim.gridZ = gridZ;
        sim.fireCS = fireComputeShader;
        
        sim.Initialize();
        
        // Run test kernel to fill with gradient
        RunFillTest();
    }

    void RunFillTest()
    {
        int kernel = fireComputeShader.FindKernel("Inject");
        
        // Set grid parameters
        fireComputeShader.SetInt("_GridX", gridX);
        fireComputeShader.SetInt("_GridY", gridY);
        fireComputeShader.SetInt("_GridZ", gridZ);
        
        // Bind textures - get them from simulation
        fireComputeShader.SetTexture(kernel, "DensityNext", sim.GetDensityTexture());
        
        // Dispatch
        int tx = Mathf.CeilToInt(gridX / 8f);
        int ty = Mathf.CeilToInt(gridY / 8f);
        int tz = Mathf.CeilToInt(gridZ / 8f);
        
        fireComputeShader.Dispatch(kernel, tx, ty, tz);
        
        Debug.Log("FillTest dispatched successfully!");
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Fire Simulation Tester", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.Space(10);
        
        GUILayout.Label($"Grid: {gridX}x{gridY}x{gridZ}");
        GUILayout.Label($"Slice Depth: {sliceDepth:F2}");
        
        if (GUILayout.Button("Run FillTest Again"))
        {
            RunFillTest();
        }
        
        if (GUILayout.Button("Toggle Visualization"))
        {
            showVelocity = !showVelocity;
        }
        
        GUILayout.EndArea();
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // Visualize a 2D slice of the 3D texture
        if (sliceVisualizerMaterial != null && sim != null)
        {
            sliceVisualizerMaterial.SetTexture("_Volume", sim.GetDensityTexture());
            sliceVisualizerMaterial.SetFloat("_SliceDepth", sliceDepth);
            Graphics.Blit(src, dst, sliceVisualizerMaterial);
        }
        else
        {
            Graphics.Blit(src, dst);
        }
    }
}
