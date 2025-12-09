using UnityEngine;
using Unity.Netcode;

public class Herd : NetworkBehaviour
{
    public GameObject sheepPrefab;   // assign prefab that has Boids on root
    public int maxSheepAmount = 40;
    public int minSheepAmount = 20; //must match pasture size in Boids.cs
    private int sheepAmount;
    public float spawningRadius = 10f;
    //public float detectionRadius = 20f;
    //public bool playerDetected = false;
    public Transform player;             // set in Awake or inspector
    public bool sheepalanche=true;
    public int oneInXSheepalanche = 7;
    public int sheepalancheAmount = 170;

    public int oneInXSheepBoss = 6;
    public float sheepBossScale = 80f;
    private bool sheepBoss=false;

    
    
    void Start()
    {
        // Only the server spawns the herd
        if (!IsServer) return;
        
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
        
            int rng = Random.Range(0, oneInXSheepalanche + 1); //! pick the biggest onInX var as rng
            if (rng >= oneInXSheepalanche - 1){

                sheepAmount = sheepalancheAmount;
                spawningRadius = 40f;

            } else if (rng >= oneInXSheepBoss - 1){
                sheepAmount = 1;
                spawningRadius = 50f;
                sheepBoss = true;
            }
            else
            {
                sheepAmount = Mathf.FloorToInt(Random.value*maxSheepAmount)+minSheepAmount;      
                spawningRadius = 20f;
            }
           
            
        
    //spawn sheep in random positions within spawningRadius
        for (int i = 0; i < sheepAmount; i++)
        { //y should be a bit above ground level dropping the sheep down to avoid spawning inside the terrain
            Vector3 pos = new Vector3(Random.Range(-spawningRadius, spawningRadius), 10.0f, Random.Range(-spawningRadius, spawningRadius));
            //sheep will be child of this herd object/thus be destroyed with the herd when tile is destroyed
            if (sheepBoss){
                pos += new Vector3(0f, 50.0f, 0f); //boss always spawns at center
            }
            var go = Instantiate(sheepPrefab, this.transform.position + pos, Quaternion.identity, this.transform);
            
            // Spawn the sheep on the network
            var networkObject = go.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn(true); // Spawn with ownership to server
            }
            
            Boids boid = go.GetComponent<Boids>();
            boid.player = player;   //pass player reference to sheep
            if (sheepBoss){
                boid.transform.localScale *= 70f;            
            }
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
