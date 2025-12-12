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
        // Only allow server to respawn the dragon
        if (!IsServer) return;
        
        // R key - lift dragon up
        if (Input.GetKeyDown(KeyCode.R))
        {
            LiftDragon();
        }
    }

    private void LiftDragon()
    {
        // Calculate new position
        Vector3 newPosition = transform.position + Vector3.up * respawnHeight;
        
        // Reset velocities first to prevent weird physics after respawn
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Use MovePosition for network rigidbody compatibility
            rb.MovePosition(newPosition);
        }
        else
        {
            // Fallback if no rigidbody
            transform.position = newPosition;
        }
        
        Debug.Log($"Dragon lifted to: {newPosition}");
    }
}