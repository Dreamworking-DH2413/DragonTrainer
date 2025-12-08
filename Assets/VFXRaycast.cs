using TMPro;
using UnityEngine;

public class VFXRaycast : MonoBehaviour
{
    private int sheepCount = 0;
    public TextMeshProUGUI sheepCounterLabel;
    public float maxDistance = 1f;
    public LayerMask hitLayers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sheepCounterLabel.text = "0";
    }

    // Update is called once per frame
    void Update()
    {
        
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, hitLayers))
        {
            // Debug.Log("VFX is touching: " + hit.collider.name);
            Sheep sheep = hit.collider.GetComponent<Sheep>();
            if (sheep != null)
            {
                sheepCount++;
                sheepCounterLabel.text = sheepCount.ToString();
                sheep.tag = "Untagged";
                sheep.PlayHitSound();
                sheep.shouldBurn = true;
            }
            
        }
        else
        {
            // No hit
        }

        Debug.DrawRay(transform.position, transform.forward * maxDistance, Color.red);
    }
}
