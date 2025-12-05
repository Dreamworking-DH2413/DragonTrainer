using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Ring : MonoBehaviour
{
    private RingSystemManager manager;
    private int ringIndex;
    private Renderer ringRenderer;
    private MaterialPropertyBlock propBlock;
    private bool isActive = false;
    private bool isStartRing = false;
    private bool isLastRing = false;
    
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
    
    public void Initialize(RingSystemManager mgr, int index, bool startRing = false, bool lastRing = false)
    {
        manager = mgr;
        ringIndex = index;
        isStartRing = startRing;
        isLastRing = lastRing;
        
        // Set initial inactive state (unless it's the start ring)
        if (!isStartRing)
        {
            SetActive(false);
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateVisuals();
    }
    
    void UpdateVisuals()
    {
        if (ringRenderer == null || manager == null) return;
        
        Color targetColor;
        
        if (isStartRing)
        {
            targetColor = manager.GetStartRingColor();
        }
        else
        {
            targetColor = isActive ? manager.GetActiveColor() : manager.GetInactiveColor();
        }
        
        ringRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", targetColor);
        propBlock.SetColor("_BaseColor", targetColor); // For URP
        
        // Add emission for glowing effect
        if (isActive || isStartRing)
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
        
        if (other.CompareTag("Player"))
        {        
            if (isStartRing)
            {
                // Debug.Log("Start ring passed! Course beginning...");
                manager.OnStartRingPassed(this);
            }
            else if (isActive)
            {
                
                // Debug.Log($"Active ring {ringIndex} passed through!");
                manager.OnRingPassed(ringIndex, isLastRing);
            }
            else
            {
                // Debug.Log($"Inactive ring {ringIndex} hit - doesn't count as the active ring");
            }
                Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (isActive || isStartRing)
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