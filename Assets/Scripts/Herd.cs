using UnityEngine;

public class Herd : MonoBehaviour
{
    public GameObject sheepPrefab;   // assign prefab that has Boids on root
    public int maxSheepAmount = 20;
    public int minSheepAmount = 12; //must match pasture size in Boids.cs
    private int sheepAmount;
    public float spawningRadius = 5f;
    //public float detectionRadius = 20f;
    //public bool playerDetected = false;
    public Transform player;             // set in Awake or inspector
    
    void Start()
    {
        //For predator position (player/dragon) calcs. Player reference passed to sheep obj to avoid sheep individually lookups (find)
        // Auto-find player if not assigned in Inspector
        if (player == null)
        {
            GameObject p = GameObject.Find("Player"); // Use Find by name instead
            if (p != null) player = p.transform;
            else
            {
                Debug.Log("M E G A F A I L");

            }
        }
        
        sheepAmount = Mathf.FloorToInt(Random.value*maxSheepAmount)+minSheepAmount;      
    //spawn sheep in random positions within spawningRadius
        for (int i = 0; i < sheepAmount; i++)
        { //y should be a bit above ground level dropping the sheep down to avoid spawning inside the terrain
            Vector3 pos = new Vector3(Random.Range(-spawningRadius, spawningRadius), 10.0f, Random.Range(-spawningRadius, spawningRadius));
            //sheep will be child of this herd object/thus be destroyed with the herd when tile is destroyed
            var go = Instantiate(sheepPrefab, this.transform.position + pos, Quaternion.identity, this.transform);
            Boids boid = go.GetComponent<Boids>();
            boid.player = player;   //pass player reference to sheep
        }
        
        //// Debug.Log("spawned Sheep Herd of size: " + sheepAmount);

        
    }

    // Update is called once per frame
    void Update()
    {
        /* Artifact.
        float distanceToPlayer = Vector3.Distance(player.position, this.transform.position);
        if(distanceToPlayer<detectionRadius)
        {
            if(!playerDetected)
            {
                playerDetected=true;
                // Debug.Log("Sheep Herd detected player at distance: " + distanceToPlayer);
            }
        }
        else
        {
            if(playerDetected)
            {
                playerDetected=false;
                // Debug.Log("Sheep Herd lost sight of player at distance: " + distanceToPlayer);
            }
        }*/
    }
}
