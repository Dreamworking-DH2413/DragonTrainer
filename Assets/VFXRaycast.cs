using TMPro;
using UnityEngine;

public class VFXRaycast : MonoBehaviour
{
    private int sheepCount = 0;
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

    // Update is called once per frame
    void Update()
    {
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.up);
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

        Debug.Log($"Camera pos: {playerCamera.transform.position}, Up: {playerCamera.transform.up}, Distance: {maxDistance}");
        Debug.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.up * maxDistance, Color.red);
    }
}
