using UnityEngine;

/// <summary>
/// Diagnostic tool to check if compute shader kernels are found
/// Attach to any GameObject temporarily
/// </summary>
public class ComputeShaderDiagnostic : MonoBehaviour
{
    public ComputeShader computeShader;
    
    void Start()
    {
        if (computeShader == null)
        {
            Debug.LogError("❌ No compute shader assigned!");
            return;
        }
        
        Debug.Log($"✓ Compute shader assigned: {computeShader.name}");
        
        // List of kernels to check
        string[] kernelNames = { "FillTest", "Inject", "Advect", "CurlNoise", "TestKernel" };
        
        Debug.Log("--- Checking Kernels ---");
        
        foreach (string kernelName in kernelNames)
        {
            if (HasKernel(computeShader, kernelName))
            {
                int kernelIndex = computeShader.FindKernel(kernelName);
                Debug.Log($"✓ Found kernel: {kernelName} (index: {kernelIndex})");
            }
            else
            {
                Debug.LogError($"❌ Kernel NOT found: {kernelName}");
            }
        }
        
        Debug.Log("--- Diagnostic Complete ---");
        
        // Check if this is actually a compute shader
        if (computeShader.GetType() != typeof(ComputeShader))
        {
            Debug.LogError("❌ Assigned object is not a ComputeShader!");
        }
    }
    
    bool HasKernel(ComputeShader cs, string kernelName)
    {
        try
        {
            cs.FindKernel(kernelName);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Exception checking kernel '{kernelName}': {e.Message}");
            return false;
        }
    }
}