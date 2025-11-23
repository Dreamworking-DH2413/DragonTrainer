using UnityEngine;

public class Herd : MonoBehaviour
{
    public GameObject sheepPrefab;
    public int maxSheepAmount = 10;
    private int minSheepAmount = 6; //must match pasture size in Boids.cs
    private int sheepAmount;
    public float spawningRadius = 5f;
    //public int oneInXChanceToSpawn = 10; //1 in X chance to spawn a herd
    
    void Start()
    {
        
        sheepAmount = Mathf.FloorToInt(Random.value*maxSheepAmount)+minSheepAmount;      
    //spawn sheep in random positions within spawningRadius
        for (int i = 0; i < sheepAmount; i++)
        { //y should be a bit above ground level dropping the sheep down to avoid spawning inside the terrain
            Vector3 pos = new Vector3(Random.Range(-spawningRadius, spawningRadius), 650.0f, Random.Range(-spawningRadius, spawningRadius));
            Instantiate(sheepPrefab, this.transform.position + pos, Quaternion.identity);
        }
        Debug.Log("spawned Sheep Herd of size: " + sheepAmount);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
