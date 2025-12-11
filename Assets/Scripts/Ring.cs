using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class Ring : NetworkBehaviour
{
    private RingSystemManager manager;
    public int ringIndex;
    private Renderer ringRenderer;
    private MaterialPropertyBlock propBlock;
    public NetworkVariable<bool> isActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool isStartRing = false;
    private bool isLastRing = false;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to active state changes
        isActive.OnValueChanged += OnActiveStateChanged;
        
        // Initial update
        UpdateVisuals();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isActive.OnValueChanged -= OnActiveStateChanged;
    }
    
    void OnActiveStateChanged(bool oldValue, bool newValue)
    {
        UpdateVisuals();
    }
    
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
        if (!IsServer) return;
        
        isActive.Value = active;
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
            targetColor = isActive.Value ? manager.GetActiveColor() : manager.GetInactiveColor();
        }
        
        ringRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", targetColor);
        propBlock.SetColor("_BaseColor", targetColor); // For URP
        
        // Add emission for glowing effect
        if (isActive.Value || isStartRing)
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
        // Only server processes collisions
        if (!IsServer) return;
        
        if (other.CompareTag("Toothless"))
        {        
            if (isStartRing)
            {
                Debug.Log("Start ring passed! Course beginning...");
                manager.OnStartRingPassed(this);
            }
            else if (isActive.Value)
            {
                
                // Debug.Log($"Active ring {ringIndex} passed through!");
                manager.OnRingPassed(ringIndex, isLastRing);
            }
            else
            {
                // Debug.Log($"Inactive ring {ringIndex} hit - doesn't count as the active ring");
            }
            
            // Network despawn
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (isActive.Value || isStartRing)
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