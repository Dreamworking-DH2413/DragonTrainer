using TMPro;
using UnityEngine;
using Unity.Netcode;

public class VFXRaycast : NetworkBehaviour
{
    private NetworkVariable<int> sheepCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public TextMeshProUGUI sheepCounterLabel;
    public float maxDistance = 1f;
    public LayerMask hitLayers;
    public AudioClip[] sheepSounds;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject playerCamera;
    
    void Start()
    {
        sheepCounterLabel.text = "0";
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to sheep count changes to update UI on all clients
        sheepCount.OnValueChanged += OnSheepCountChanged;
        
        // Initialize UI with current value
        sheepCounterLabel.text = sheepCount.Value.ToString();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        sheepCount.OnValueChanged -= OnSheepCountChanged;
    }
    
    private void OnSheepCountChanged(int previousValue, int newValue)
    {
        sheepCounterLabel.text = newValue.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        // Only the host/server performs raycasting and game logic
        if (!IsServer)
        {
            // Clients just draw debug line for visualization
            Debug.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.up * maxDistance, Color.red);
            return;
        }
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, hitLayers))
        {
            // Debug.Log("VFX is touching: " + hit.collider.name);
            Sheep sheep = hit.collider.GetComponent<Sheep>();
            if (sheep != null)
            {
                sheepCount.Value++; // NetworkVariable automatically syncs to all clients
                sheep.tag = "Untagged";
                
                if (sheepSounds.Length > 0) {
                    Debug.Log(sheepSounds);
                    int randomIndex = Random.Range(0, sheepSounds.Length);
                    sheep.audioSource.clip = sheepSounds[randomIndex];
                    Debug.Log(sheepSounds[randomIndex]);
                }
                
                sheep.PlayHitSoundClientRpc();
                sheep.StartBurningServerRpc();
            }
            
        }
        else
        {
            // No hit
        }

        Debug.Log($"[HOST] Camera pos: {playerCamera.transform.position}, Up: {playerCamera.transform.up}, Distance: {maxDistance}");
        Debug.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.up * maxDistance, Color.red);
    }
}
