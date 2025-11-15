using System;
using UnityEngine;
using UnityEngine.Rendering;

public class FireSimulation
{
    public int gridX = 64;
    public int gridY = 32;
    public int gridZ = 64;
    
    // Swap A and B after each step
    // https://docs.unity3d.com/6000.2/Documentation/ScriptReference/RenderTexture.html
    RenderTexture velocityA; // Call Create3DTexture for all of these RTs
    RenderTexture velocityB;
    RenderTexture densityA;
    RenderTexture densityB;

    public float timeStep = 0.016f;

    // CS references
    public ComputeShader fireCS;
    int kernelInject;
    int kernelAdvect;
    int kernelCurl;
    
    

    RenderTexture Create3DTexture(int x, int y, int z, RenderTextureFormat format)
    {
        var rt = new RenderTexture(x, y, 0);
        rt.dimension = TextureDimension.Tex3D;
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
    
    void Update()
    {
        SimulateFire();
        
        // // Optional: give raymarch renderer the density texture
        // fireMaterial.SetTexture("_DensityTex", densityA);
    }

    void SimulateFire()
    {
        RunInjectKernel();
        SwapBuffers();
        // RUN ADVECT
        SwapBuffers();
        // RUN CURL
        SwapBuffers();
        // RUN Dissipate
    }
    
    void RunInjectKernel() 
    {
        int tx = Mathf.CeilToInt(gridX / 8f);
        int ty = Mathf.CeilToInt(gridY / 8f);
        int tz = Mathf.CeilToInt(gridZ / 8f);
        kernelInject = fireCS.FindKernel("Inject");
        fireCS.Dispatch(kernelInject, tx, ty, tz);
    }
}

// https://dl-acm-org.focus.lib.kth.se/doi/pdf/10.1145/1275808.1276435
// Curl-noise for procedural fluid
