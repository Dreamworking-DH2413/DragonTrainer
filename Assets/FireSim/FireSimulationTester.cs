using UnityEngine;

/// <summary>
/// Test harness for FireSimulation - attach to Main Camera
/// Visualizes the 3D density field using a simple slice viewer
/// </summary>
[RequireComponent(typeof(Camera))]
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
    
    [Header("Debug")]
    public bool runFillTestOnStart = true;
    
    private FireSimulation sim;
    private RenderTexture densityTexture;
    private RenderTexture velocityTexture;

    void Start()
    {
        // Validate compute shader
        if (fireComputeShader == null)
        {
            Debug.LogError("FireComputeShader is not assigned!");
            return;
        }
        
        // Check if FillTest kernel exists
        if (!HasKernel(fireComputeShader, "FillTest"))
        {
            Debug.LogError("FillTest kernel not found in compute shader! Check your .compute file.");
            return;
        }
        
        // Initialize simulation
        sim = new FireSimulation();
        sim.gridX = gridX;
        sim.gridY = gridY;
        sim.gridZ = gridZ;
        sim.fireCS = fireComputeShader;
        
        sim.Initialize();
        
        // Run test kernel to fill with gradient
        if (runFillTestOnStart)
        {
            RunFillTest();
        }
        
        Debug.Log("FireSimulationTester started successfully!");
    }
    
    bool HasKernel(ComputeShader cs, string kernelName)
    {
        try
        {
            cs.FindKernel(kernelName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    void RunFillTest()
    {
        if (fireComputeShader == null || sim == null)
        {
            Debug.LogError("Cannot run FillTest - compute shader or simulation not initialized!");
            return;
        }
        
        if (sim.GetDensityTexture() == null)
        {
            Debug.LogError("Density texture is null! Did Initialize() fail?");
            return;
        }
        
        try
        {
            int kernel = fireComputeShader.FindKernel("FillTest");
            
            // Set grid parameters
            fireComputeShader.SetInt("_GridX", gridX);
            fireComputeShader.SetInt("_GridY", gridY);
            fireComputeShader.SetInt("_GridZ", gridZ);
            
            // Bind textures - get them from simulation
            fireComputeShader.SetTexture(kernel, "DensityNext", sim.GetDensityTexture());
            fireComputeShader.SetTexture(kernel, "VelocityNext", sim.GetVelocityTexture());
            
            // Dispatch
            int tx = Mathf.CeilToInt(gridX / 8f);
            int ty = Mathf.CeilToInt(gridY / 8f);
            int tz = Mathf.CeilToInt(gridZ / 8f);
            
            fireComputeShader.Dispatch(kernel, tx, ty, tz);
            
            Debug.Log($"FillTest dispatched successfully! ({tx}x{ty}x{tz} thread groups)");
            Debug.Log("You should see a gradient from black (corner) to white (opposite corner)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error running FillTest: {e.Message}");
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        
        GUILayout.Label("Fire Simulation Tester", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.Space(10);
        
        GUILayout.Label($"Grid: {gridX}x{gridY}x{gridZ}");
        GUILayout.Label($"Slice Depth: {sliceDepth:F2}");
        GUILayout.Label($"Compute Shader: {(fireComputeShader != null ? "Assigned" : "MISSING!")}");
        GUILayout.Label($"Slice Material: {(sliceVisualizerMaterial != null ? "Assigned" : "MISSING!")}");
        
        GUILayout.Space(10);
        
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
        // Debug: Log what's happening
        if (sliceVisualizerMaterial == null)
        {
            Debug.LogWarning("sliceVisualizerMaterial is NULL!");
            Graphics.Blit(src, dst);
            return;
        }
        
        if (sim == null)
        {
            Debug.LogWarning("sim is NULL!");
            Graphics.Blit(src, dst);
            return;
        }
        
        RenderTexture densityTex = sim.GetDensityTexture();
        if (densityTex == null)
        {
            Debug.LogWarning("Density texture is NULL!");
            Graphics.Blit(src, dst);
            return;
        }
        
        // All good - apply visualization
        sliceVisualizerMaterial.SetTexture("_Volume", densityTex);
        sliceVisualizerMaterial.SetFloat("_SliceDepth", sliceDepth);
        Graphics.Blit(src, dst, sliceVisualizerMaterial);
        
        // Debug: Only log once
        if (Time.frameCount == 2)
        {
            Debug.Log("✓ OnRenderImage is working! Shader applied.");
        }
    }
    
    void OnDestroy()
    {
        sim?.Cleanup();
    }
}