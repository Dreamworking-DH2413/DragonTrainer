using UnityEngine;

public class FireSimulation
{
    public int gridX = 64;
    public int gridY = 32;
    public int gridZ = 64;
    
    // Swap A and B after each step
    RenderTexture velocityA;
    RenderTexture velocityB;
    RenderTexture densityA;
    RenderTexture densityB;

    public float timeStep = 0.016f;

    RenderTexture Create3DTexture(int x, int y, int z, RenderTextureFormat format)
    {
        var rt = new RenderTexture(x, y, 0);
    }
}

// https://dl-acm-org.focus.lib.kth.se/doi/pdf/10.1145/1275808.1276435
// Curl-noise for procedural fluid
