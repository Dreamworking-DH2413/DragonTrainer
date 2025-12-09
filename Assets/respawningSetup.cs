using UnityEngine;

public class respawningSetup : MonoBehaviour
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
        // R key - lift dragon up
        if (Input.GetKeyDown(KeyCode.R))
        {
            LiftDragon();
        }
    }

    private void LiftDragon()
    {
        transform.position += Vector3.up * respawnHeight;
        Debug.Log($"Dragon lifted to: {transform.position}");
    }
}