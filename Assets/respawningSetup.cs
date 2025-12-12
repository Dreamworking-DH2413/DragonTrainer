using UnityEngine;
using Unity.Netcode;

public class respawningSetup : NetworkBehaviour
{
    public float respawnHeight = 20f;  // How much to lift the dragon
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Disable physics at start
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;       // Enable gravity

        }
    }

    void Update()
    {
        // Only allow host to respawn the dragon
        if (!IsServer && !IsHost) return;
        
        // R key - lift dragon up
        if (Input.GetKeyDown(KeyCode.R))
        {
            LiftDragonServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void LiftDragonServerRpc()
    {
        // Server has authority over position - this will sync to all clients
        transform.position += Vector3.up * respawnHeight;
        
        // Reset velocities to prevent weird physics after respawn
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log($"Dragon lifted to: {transform.position}");
    }
}