using UnityEngine;

/// <summary>
/// Phase 1 Tester: Test fire injection
/// Attach to a Quad to visualize the fire growing over time
/// </summary>
public class FirePhase1Tester : MonoBehaviour
{
    [Header("Simulation")]
    public ComputeShader fireComputeShader;
    public Material sliceMaterial;
    
    [Header("Grid Settings")]
    public int gridX = 64;
    public int gridY = 32;
    public int gridZ = 64;
    
    [Header("Injection Settings")]
    [Tooltip("Position in grid coordinates (0 to gridSize)")]
    public Vector3 injectionPoint = new Vector3(32, 2, 32);
    
    [Range(1f, 20f)]
    [Tooltip("How wide the fire source is")]
    public float injectionRadius = 5.0f;
    
    [Range(0.1f, 5f)]
    [Tooltip("How much density to add per second")]
    public float injectionStrength = 2.0f;
    
    [Range(1f, 20f)]
    [Tooltip("Upward velocity of fire")]
    public float injectionVelocity = 8.0f;
    
    [Header("Visualization")]
    [Range(0f, 1f)]
    public float sliceDepth = 0.5f;
    
    [Tooltip("Which axis to slice: 0=YZ plane, 1=XZ plane, 2=XY plane")]
    [Range(0, 2)]
    public int sliceAxis = 1; // XZ plane (looking down from top)
    
    [Header("Dissipation")]
    [Range(0f, 1f)]
    public float dissipationRate = 0.8f;
    
    private FireSimulation sim;
    
    void Start()
    {
        // Initialize simulation
        sim = new FireSimulation();
        sim.gridX = gridX;
        sim.gridY = gridY;
        sim.gridZ = gridZ;
        sim.fireCS = fireComputeShader;
        
        // Set injection parameters
        sim.injectionPoint = injectionPoint;
        sim.injectionRadius = injectionRadius;
        sim.injectionStrength = injectionStrength;
        sim.injectionVelocity = injectionVelocity;
        
        sim.Initialize();
        
        // Apply material to this quad
        GetComponent<Renderer>().material = sliceMaterial;
        
        Debug.Log("Phase 1: Fire injection ready! Watch the density grow.");
    }
    
    void Update()
    {
        // Update injection parameters in real-time
        sim.injectionPoint = injectionPoint;
        sim.injectionRadius = injectionRadius;
        sim.injectionStrength = injectionStrength;
        sim.injectionVelocity = injectionVelocity;
        sim.dissipationRate = dissipationRate;
        
        // Run simulation step
        sim.Update();
        
        // Update visualization
        if (sliceMaterial != null)
        {
            sliceMaterial.SetTexture("_Volume", sim.GetDensityTexture());
            sliceMaterial.SetFloat("_SliceDepth", sliceDepth);
        }
    }
    
    void OnDestroy()
    {
        sim?.Cleanup();
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        
        GUILayout.Label("🔥 PHASE 1: FIRE INJECTION", new GUIStyle(GUI.skin.label) 
            { fontSize = 18, fontStyle = FontStyle.Bold });
        
        GUILayout.Space(10);
        
        GUILayout.Label($"Grid: {gridX}x{gridY}x{gridZ}");
        GUILayout.Label($"Injection Point: ({injectionPoint.x:F1}, {injectionPoint.y:F1}, {injectionPoint.z:F1})");
        GUILayout.Label($"Injection Radius: {injectionRadius:F1}");
        GUILayout.Label($"Injection Strength: {injectionStrength:F2}");
        GUILayout.Label($"Injection Velocity: {injectionVelocity:F1}");
        GUILayout.Label($"Slice Depth: {sliceDepth:F2}");
        
        GUILayout.Space(10);
        
        GUILayout.Label("WHAT TO OBSERVE:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        GUILayout.Label("• Bright spot should appear at injection point");
        GUILayout.Label("• Density should continuously grow");
        GUILayout.Label("• Without advection, it stays in place");
        GUILayout.Label("• Adjust 'Slice Depth' to see different layers");
        
        GUILayout.Space(10);
        
        GUILayout.Label("CONTROLS:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        GUILayout.Label("• Tweak parameters in Inspector in real-time");
        GUILayout.Label("• Try different slice depths (0-1)");
        GUILayout.Label("• Move injection point to see it move");
        
        GUILayout.EndArea();
    }
}