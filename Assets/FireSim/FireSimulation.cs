using UnityEngine;

/// <summary>
/// Eulerian fluid simulation for fire effects
/// Based on Stable Fluids (Stam 1999) and Curl-Noise (Bridson et al. 2007)
/// </summary>
public class FireSimulation
{
    public int gridX = 64;
    public int gridY = 32;
    public int gridZ = 64;
    
    // Double-buffered textures for ping-pong rendering
    RenderTexture velocityA;
    RenderTexture velocityB;
    RenderTexture densityA;
    RenderTexture densityB;

    public float timeStep = 0.016f;

    // Compute shader references
    public ComputeShader fireCS;
    int kernelInject;
    int kernelAdvect;
    int kernelCurl;
    
    // Injection
    public Vector3 injectionPoint = new Vector3(32, 2, 32); // Center-bottom
    public float injectionRadius = 5.0f;
    public float injectionStrength = 2.0f;
    public float injectionVelocity = 8.0f;
    
    // Public access for visualization
    public RenderTexture GetDensityTexture() => densityA;
    public RenderTexture GetVelocityTexture() => velocityA;

    public void Initialize()
    {
        Debug.Log("Initializing FireSimulation");
        // Create 3D textures
        velocityA = Create3DTexture(gridX, gridY, gridZ, RenderTextureFormat.ARGBFloat);
        velocityB = Create3DTexture(gridX, gridY, gridZ, RenderTextureFormat.ARGBFloat);
        densityA = Create3DTexture(gridX, gridY, gridZ, RenderTextureFormat.RFloat);
        densityB = Create3DTexture(gridX, gridY, gridZ, RenderTextureFormat.RFloat);
        
        // Find kernel indices
        kernelInject = fireCS.FindKernel("Inject");
        kernelAdvect = fireCS.FindKernel("Advect");
        kernelCurl = fireCS.FindKernel("CurlNoise");
        
        Debug.Log("FireSimulation initialized successfully");
    }

    RenderTexture Create3DTexture(int x, int y, int z, RenderTextureFormat format)
    {
        var rt = new RenderTexture(x, y, 0, format);
        rt.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        rt.volumeDepth = z;
        rt.enableRandomWrite = true;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();
        return rt;
    }

    void Swap(ref RenderTexture a, ref RenderTexture b)
    {
        var temp = a;
        a = b;
        b = temp;
    }

    void SwapBuffers()
    {
        Swap(ref velocityA, ref velocityB);
        Swap(ref densityA, ref densityB);
    }
    
    public void Update()
    {
        Debug.Log("Updating FireSimulation");
        SimulateFire();
    }

    void SimulateFire()
    {
        // Pipeline based on Stable Fluids:
        // 1. Inject forces/density
        RunInjectKernel();
        SwapBuffers();
        
        // 2. Advect velocity and density
        RunAdvectKernel();
        SwapBuffers();
        
        // 3. Add curl noise for turbulence
        RunCurlKernel();
        SwapBuffers();
        
        // 4. Dissipate/decay
        // RunDissipateKernel();
    }
    
    void RunInjectKernel() 
    {
        // Set grid parameters
        fireCS.SetInt("_GridX", gridX);
        fireCS.SetInt("_GridY", gridY);
        fireCS.SetInt("_GridZ", gridZ);
        fireCS.SetFloat("_DeltaTime", timeStep);
        
        fireCS.SetVector("_InjectionPoint", injectionPoint);
        fireCS.SetFloat("_InjectionRadius", injectionRadius);
        fireCS.SetFloat("_InjectionStrength", injectionStrength);
        fireCS.SetFloat("_InjectionVelocity", injectionVelocity);
        
        // Bind textures
        fireCS.SetTexture(kernelInject, "VelocityPrev", velocityA);
        fireCS.SetTexture(kernelInject, "VelocityNext", velocityB);
        fireCS.SetTexture(kernelInject, "DensityPrev", densityA);
        fireCS.SetTexture(kernelInject, "DensityNext", densityB);
        
        // Dispatch
        int tx = Mathf.CeilToInt(gridX / 8f);
        int ty = Mathf.CeilToInt(gridY / 8f);
        int tz = Mathf.CeilToInt(gridZ / 8f);
        
        fireCS.Dispatch(kernelInject, tx, ty, tz);
    }

    void RunAdvectKernel()
    {
        // Set grid parameters (advection needs these too)
        fireCS.SetInt("_GridX", gridX);
        fireCS.SetInt("_GridY", gridY);
        fireCS.SetInt("_GridZ", gridZ);
        fireCS.SetFloat("_DeltaTime", timeStep);

        // Bind textures (read from A, write to B)
        fireCS.SetTexture(kernelAdvect, "VelocityPrev", velocityA);
        fireCS.SetTexture(kernelAdvect, "VelocityNext", velocityB);
        fireCS.SetTexture(kernelAdvect, "DensityPrev", densityA);
        fireCS.SetTexture(kernelAdvect, "DensityNext", densityB);

        // Dispatch with same thread-group sizing as the compute shader
        int tx = Mathf.CeilToInt(gridX / 8f);
        int ty = Mathf.CeilToInt(gridY / 8f);
        int tz = Mathf.CeilToInt(gridZ / 8f);

        fireCS.Dispatch(kernelAdvect, tx, ty, tz);
    }
    
    void RunCurlKernel()
    {
        fireCS.SetInt("_GridX", gridX);
        fireCS.SetInt("_GridY", gridY);
        fireCS.SetInt("_GridZ", gridZ);
        fireCS.SetFloat("_DeltaTime", timeStep);

        // Bind textures (read from A, write to B)
        fireCS.SetTexture(kernelCurl, "VelocityPrev", velocityA);
        fireCS.SetTexture(kernelCurl, "VelocityNext", velocityB);
        fireCS.SetTexture(kernelCurl, "DensityPrev", densityA);
        fireCS.SetTexture(kernelCurl, "DensityNext", densityB);

        // Dispatch with same thread-group sizing as the compute shader
        int tx = Mathf.CeilToInt(gridX / 8f);
        int ty = Mathf.CeilToInt(gridY / 8f);
        int tz = Mathf.CeilToInt(gridZ / 8f);

        fireCS.Dispatch(kernelCurl, tx, ty, tz);
    }
    
    public void Cleanup()
    {
        velocityA?.Release();
        velocityB?.Release();
        densityA?.Release();
        densityB?.Release();
    }
}