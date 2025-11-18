using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Ring : MonoBehaviour
{
    private RingSystemManager manager;
    private int ringIndex;
    private Renderer ringRenderer;
    private MaterialPropertyBlock propBlock;
    private bool isActive = false;
    
    void Awake()
    {
        ringRenderer = GetComponent<Renderer>();
        if (ringRenderer == null)
        {
            ringRenderer = GetComponentInChildren<Renderer>();
        }
        
        propBlock = new MaterialPropertyBlock();
        
        // Ensure collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Enable emission keyword on material
        if (ringRenderer != null)
        {
            Material mat = ringRenderer.material;
            mat.EnableKeyword("_EMISSION");
        }
    }
    
    public void Initialize(RingSystemManager mgr, int index)
    {
        manager = mgr;
        ringIndex = index;
        
        // Set initial inactive state
        SetActive(false);
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateVisuals();
    }
    
    void UpdateVisuals()
    {
        if (ringRenderer == null || manager == null) return;
        
        Color targetColor = isActive ? manager.GetActiveColor() : manager.GetInactiveColor();
        
        ringRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", targetColor);
        propBlock.SetColor("_BaseColor", targetColor); // For URP
        
        // Add emission for glowing effect
        if (isActive)
        {
            Color emissionColor = targetColor * manager.GetEmissionIntensity();
            propBlock.SetColor("_EmissionColor", emissionColor);
        }
        else
        {
            propBlock.SetColor("_EmissionColor", Color.black);
        }
        
        ringRenderer.SetPropertyBlock(propBlock);

    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the dragon passed through
        if (other.CompareTag("Player") && isActive)
        {
            manager.OnRingPassed(ringIndex);
            // Optional: Add particle effect, sound, etc.
        }
    }
    
    // Optional: Add pulsing animation for active ring
    void Update()
    {
        if (isActive)
        {
            // Pulse effect
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            transform.localScale = Vector3.one * (1f + pulse * 0.1f);
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }
}