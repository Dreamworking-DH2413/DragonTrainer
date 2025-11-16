using UnityEngine;

/// <summary>
/// Standalone test - attach to a Quad in your scene
/// This displays the volume texture directly on a mesh
/// </summary>
public class StandaloneVolumeTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material displayMaterial;
    
    [Range(0f, 1f)]
    public float sliceDepth = 0.5f;
    
    private RenderTexture volumeTexture;
    
    void Start()
    {
        // Create 3D texture
        volumeTexture = new RenderTexture(64, 32, 0, RenderTextureFormat.RFloat);
        volumeTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        volumeTexture.volumeDepth = 64;
        volumeTexture.enableRandomWrite = true;
        volumeTexture.wrapMode = TextureWrapMode.Clamp;
        volumeTexture.filterMode = FilterMode.Bilinear;
        volumeTexture.Create();
        
        // Fill it with test pattern
        int kernel = computeShader.FindKernel("FillTest");
        computeShader.SetInt("_GridX", 64);
        computeShader.SetInt("_GridY", 32);
        computeShader.SetInt("_GridZ", 64);
        computeShader.SetTexture(kernel, "DensityNext", volumeTexture);
        computeShader.SetTexture(kernel, "VelocityNext", volumeTexture); // Dummy
        
        computeShader.Dispatch(kernel, 8, 4, 8);
        
        Debug.Log("Volume filled with test pattern");
        
        // Apply to material on this object
        GetComponent<Renderer>().material = displayMaterial;
        displayMaterial.SetTexture("_Volume", volumeTexture);
    }
    
    void Update()
    {
        if (displayMaterial != null)
        {
            displayMaterial.SetFloat("_SliceDepth", sliceDepth);
        }
    }
    
    void OnDestroy()
    {
        volumeTexture?.Release();
    }
}