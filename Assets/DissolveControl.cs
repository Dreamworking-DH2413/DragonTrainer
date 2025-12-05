using UnityEngine;

public class DissolveControl : MonoBehaviour
{
    private Renderer rend;
    private MaterialPropertyBlock[] mpb;  // one block per material
    private int materialCount = 2;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        materialCount = rend.sharedMaterials.Length;

        mpb = new MaterialPropertyBlock[materialCount];

        for (int i = 0; i < materialCount; i++)
            mpb[i] = new MaterialPropertyBlock();
    }

    public void SetDissolve(int materialIndex, float value)
    {
        if (materialIndex < 0 || materialIndex >= materialCount)
            return;

        rend.GetPropertyBlock(mpb[materialIndex], materialIndex);
        mpb[materialIndex].SetFloat("_DissolveStrength", value);
        rend.SetPropertyBlock(mpb[materialIndex], materialIndex);
    }
    
    public void SetDissolveBoth(float value)
    {
        for (int i = 0; i < materialCount; i++)
            SetDissolve(i, value);
    }
}