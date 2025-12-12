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
    public NetworkVariable<bool> isStartRing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isLastRing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Find manager if not set (for clients)
        if (manager == null)
        {
            manager = FindFirstObjectByType<RingSystemManager>();
        }
        
        // Subscribe to active state changes
        isActive.OnValueChanged += OnActiveStateChanged;
        isStartRing.OnValueChanged += OnRingTypeChanged;
        isLastRing.OnValueChanged += OnRingTypeChanged;
        
        // Initial update
        UpdateVisuals();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isActive.OnValueChanged -= OnActiveStateChanged;
        isStartRing.OnValueChanged -= OnRingTypeChanged;
        isLastRing.OnValueChanged -= OnRingTypeChanged;
    }
    
    void OnActiveStateChanged(bool oldValue, bool newValue)
    {
        UpdateVisuals();
    }
    
    void OnRingTypeChanged(bool oldValue, bool newValue)
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
        
        // Only server sets these network variables
        if (IsServer)
        {
            isStartRing.Value = startRing;
            isLastRing.Value = lastRing;
            
            // Set initial inactive state (unless it's the start ring)
            if (!startRing)
            {
                SetActive(false);
            }
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
        
        if (isStartRing.Value)
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
        if (isActive.Value || isStartRing.Value)
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
            if (isStartRing.Value)
            {
                Debug.Log("Start ring passed! Course beginning...");
                manager.OnStartRingPassed(this);
            }
            else if (isActive.Value)
            {
                
                // Debug.Log($"Active ring {ringIndex} passed through!");
                manager.OnRingPassed(ringIndex, isLastRing.Value);
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
        if (isActive.Value || isStartRing.Value)
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