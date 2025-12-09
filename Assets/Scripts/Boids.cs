using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;


public class Boids : NetworkBehaviour
{
    
    // MOVEMENT
    private float maxSpeed=51.8f;
    public float restSpeed=15f;
    public float huntSpeed=20f;
    public float huntedAcc = 100.0f;        // Units per second along the ground (XZ)
    public float restAcc = 6.5f;        // Units per second along the ground (XZ)

    public float turnSpeed = 90f;       // Degrees per second we can rotate around Y
    public bool faceMoveDirection = true; // If true, rotate to face where we’re moving

    // SENSING NEIGHBOUR BOIDS
    public LayerMask boidMask;            // Layer that all sheep are on (e.g., "Boid")
    public int maxNeighbors = 64;         // Size of reusable hits buffer
    public float senseHz = 30f;    // Each sheep senses ~10×/sec
    float senseInterval;
    public float cohesionRadius = 4f;     // How far we "see" other sheep
    //public float regroupRadius = 4f;     // How far we "see" other sheep

    public float pastureRadius = 4f;     // How far we "see" other sheep
    public float sepRadius = 4f;     // How far we "see" other sheep
    public float predatorRadius = 10f;   // how far away we start caring about the player
    public float senseRadius = 200f;   // how far away we start caring about the player

    public float matchingRadius = 10f;   // how far away we start caring about the player

    public Transform player;             // set in Awake or inspector


    //BOIDS FORCES STUFF
    Dictionary<string, bool> status = new Dictionary<string, bool>()
    {
        {"beach", false },
        { "water", false },
        { "sensing", false },
        { "hunted", false }
    };
    float damping;
    public float pastureDamping = 0.5f;
    public int pastureSize = 6;
    public float normalDamping = 0.12f;


    public float cohesionStr = 0.1f;
    public float regroupStr = 0.1f;
    public float sepStr = 0.5f;
    public float sepMax = 3.5f;
    public float predatorStr = 1.5f;     // strength of the avoidance
    public float matchingStr = 0.8f;      // str of mathching vel of surrounding fleeing cheep
    public float waterStr = 700.0f;






    // --- Internals ---
    private Vector3 cohesionForce;
    private Vector3 regroupForce;
    private Vector3 sepForce;
    private Vector3 predatorForce;
    private Vector3 matchingForce;
    //private Vector3 waterForce;
    


    private Rigidbody rb;               // Cached reference to our Rigidbody
    private Vector3 heading;            // Our current movement direction on XZ (y = 0)
    private float nextSenseTime;          // When this sheep should sense next (not every frame)
    private Collider[] hits; //non-triggered. OverlapSphere func returns Colliders it finds in a radius. <- Why we need it.
    private Vector3 cohesionCenter;
    private Vector3 regroupPos;

    private Vector3 matchingVel;

    private Vector3 desiredVel;

    //==============================================
     void Awake()
    {
        rb = GetComponent<Rigidbody>();                       // Grab Rigidbody once
        rb.useGravity = true;                                 // Let physics keep us on the ground
        rb.interpolation = RigidbodyInterpolation.Interpolate;// Smooth visuals between physics steps
        //rb.collisionDetectionMode = CollisionDetectionMode.Continuous; //set to discrete in inspector inst for optimization.

        // Start with a random XZ heading so all sheep don’t move identically
        Vector2 rnd = Random.insideUnitCircle.normalized;     // 2D random on the plane //!
        heading = new Vector3(rnd.x, 0f, rnd.y);              // Set XZ (L/R), keep y (UP) = 0
        if (heading.sqrMagnitude < 1e-4f) heading = Vector3.forward;// Fallback just in case

        Vector2 r = Random.insideUnitCircle.normalized;
        //desiredVel = new Vector3(r.x, 0f, r.y) * 0.5f; // small nudge


        hits = new Collider[maxNeighbors];                              // Allocate once; reuse forever
        senseInterval = 1f / senseHz;   // e.g., 5 Hz -> 0.2 s

        nextSenseTime = Time.fixedTime + Random.value * senseInterval;        // Stagger first sensing so they dont all check simultaniously//Mayb can use 0 inst of Time.time (?)

        

        //lost sheep regroup pos
        if (transform.parent != null)
        {
            regroupPos = transform.parent.position;
            //// Debug.Log(regroupPos);
        }
        else{
            // Debug.Log("NO HERD PARENT");
        }
    }

    //runs after herd has passed all references (player will be set by herd correctly this way)
    void start()
    {
     //For predator position (player/dragon)
        // Auto-find player if not assigned in Inspector or passed by herd parent
        if (player == null)
        {
            // Debug.Log("FAIL!!! finding player");
            GameObject p = GameObject.Find("FallbackObjects"); // Use Find by name instead
            if (p != null) player = p.transform;
        }   
    }

    void Update()
    {

    }
    
    void calcPredatorForce()
    {
        // Check if player reference is assigned
        if (player == null)
        {
            predatorForce = Vector3.zero;
            status["hunted"] = false;
            return;
        }
        
        Vector3 dstToPredator = player.position - transform.position;
        //dstToPredator.y = 0f; dst also depend on height
        
        if (dstToPredator.sqrMagnitude <= predatorRadius * predatorRadius)// out of range-ignore (!) should prob not be squared???
        {
            predatorForce = -dstToPredator.normalized; //* ((predatorRadius*predatorRadius) / Mathf.Max(dstToPredator.sqrMagnitude, 0.001f));
            predatorForce *= predatorStr;
            status["hunted"]=true;

        }
        else
        {
            predatorForce = Vector3.zero;
            status["hunted"]=false;
        }
        if (dstToPredator.sqrMagnitude <= senseRadius * senseRadius)// out of range-ignore (!) should prob not be squared???
        {
            status["sensing"]=true;
        }
        else
        {
            status["sensing"]=false;
        }
    }

    void calcBoidForces()
    {
        

        //reset boid forces:
        regroupForce = Vector3.zero;
        cohesionForce = Vector3.zero;
        cohesionCenter = Vector3.zero;
        sepForce = Vector3.zero;
        matchingForce=Vector3.zero;
        matchingVel = Vector3.zero;

        
        //only calc boid forces
        if(status["sensing"]==true)
        {
            //see if inside cohesionRadius (biggest radius)
            //NOTICE ITS THE COHESIONRADIUS DECIDING HOW MUCH THEY SEE CURRENTLY. may need changing
            int found = SenseNeighborsNonAlloc(transform.position, cohesionRadius, hits, boidMask);
            int cohesionCount = 0;
            int matchingCount = 0;
            int pastureCount = 0;
            
            //if(found==0){// Debug.Log(found);}

            for (int i = 0; i < found; i++)
            {
                var col = hits[i];                                      // Candidate collider (OTHER SHEEP)
                if (!col) continue;                                     // Safety-ignore (no found)
                var otherRb = col.attachedRigidbody;                    // Prefer Rigidbody to identify self
                if (otherRb == rb) continue;                            // if self-ignore

                Vector3 dstToOther = col.transform.position - transform.position;
                dstToOther.y = 0f;
                float sqrDst = dstToOther.sqrMagnitude;

                // Accumulate cohesion force
                //if ((sqrDst <= cohesionRadius * cohesionRadius) && (sqrDst >= pastureRadius * pastureRadius))    // out of range-ignore
                if (status["hunted"]==true && (sqrDst <= cohesionRadius * cohesionRadius))
                {   
                    cohesionCount++; //found a neighbour!
                    cohesionCenter += col.transform.position;                   
                }
                
                // Accumulate sep force
                if (sqrDst <= sepRadius * sepRadius)// out of range-ignore (!) should prob not be squared???
                {
                    Vector3 sepAdd = (-dstToOther) * ((sepRadius*sepRadius) / Mathf.Max(dstToOther.sqrMagnitude, 0.1f));//neg because the other way. Falloff-Apply greater force if very close
                    //sepAdd = Vector3.ClampMagnitude(sepAdd, sepMax); // clamp the total separation force (since we dont normalize it)
                    sepForce += sepAdd;
                }
                // Accumulate matching force
                if (status["hunted"] == true && (sqrDst <= matchingRadius * matchingRadius))
                {
                
                    matchingCount++; //found a neighbour!
                    matchingVel += otherRb.linearVelocity;
                
                }
                
                // Accumulate Pasture herd staus
                if (status["hunted"] == false && (sqrDst <= pastureRadius * pastureRadius))
                {
                    pastureCount+=1;
                }
                
                

            }
            // BOID FORCES calcs
            if (found < pastureSize/2) //if sheep alone calc regoupForce. Dont apply if already found a subgroups (half size of pasture group)
            {
                Vector3 toRegroup = regroupPos - transform.position;
                toRegroup.y = 0f;

                // When close to regroupPos, start pasturing
                if (toRegroup.sqrMagnitude > pastureRadius * pastureRadius)
                {
                    regroupForce = toRegroup.normalized * regroupStr;
                }
            }
            else if (cohesionCount > 0)
            {
                regroupForce = Vector3.zero;
                cohesionCenter = cohesionCenter / cohesionCount;
                Vector3 centerDir = cohesionCenter - transform.position;
                centerDir.y = 0f;
                cohesionForce = centerDir.normalized;   // direction only; accel cap handles magnitude
                cohesionForce *= cohesionStr;    
            }
            if ((matchingCount > 0) && (status["hunted"]==true))
            {
                matchingVel = matchingVel / matchingCount;
                matchingVel = matchingVel - rb.linearVelocity;
                matchingVel.y = 0f;
                matchingForce = matchingVel.normalized;   // lets not normalize because we want them to affecteach others vel: Large velocity differences = huge forces, small differences = tiny forces.
                matchingForce *= matchingStr;
            }
            else
            {
                matchingForce=Vector3.zero;
            }
            if ((pastureCount > pastureSize) && (status["hunted"]==false) && (status["beach"]==false)) //At least pastureSize not hunted and not beach close sheep for pasture to take place
            {
                damping = pastureDamping;
            }
            else
            {
                damping = normalDamping;
            }
            
            
            sepForce = Vector3.ClampMagnitude(sepForce, sepMax); // clamp the total separation force (since we dont normalize it)
            sepForce *= sepStr;

            if (status["water"]==true) //re add regrouping force even if in a (sub/) group if in water (set to 0 otherwise)
            {
                Vector3 toRegroup = regroupPos - transform.position;
                toRegroup.y = 0f;

                // When close to regroupPos, start pasturing
                if (toRegroup.sqrMagnitude > (pastureRadius * pastureRadius)/2) //smaller radius because if spawn is close to water
                {
                    regroupForce = toRegroup.normalized * regroupStr;
                }
            }
        
        }

        
    }

    void checkWater()
    {
        //simple water avoidance at y=0 plane by changing direction of vel
        if (transform.position.y < 101.0f) //water level + some margin //may need +-100
        {
            //// Debug.Log("Afraid of water!");
            status["water"] = true;
            //// Debug.Log(transform.position.y);
            
        }
        else if
        (transform.position.y < 105.0f) //close to water level + some margin //may need +-100
        {
            //// Debug.Log("B E A C H vibes!");
            status["water"] = false;

            status["beach"] = true;
            //// Debug.Log(transform.position.y);
            
        }else
        {
            status["water"] = false;
            status["beach"] = false;

        }
    }

    //==============================================
    void FixedUpdate()
    {
        // Only the server controls the movement
        if (!IsServer) return;
        
        // --- Sense and reCalc forces (throttled) ---
        //if (Time.fixedTime >= nextSenseTime)
        //{
        //nextSenseTime += senseInterval; //book next sense session
        calcBoidForces();
        
        //}
        calcPredatorForce(); //we afford do every update because its only 1 item we look for
        checkWater();
        //---------------------------------------------------------------------------------
        //---------------------------UPDATE VEL VIA BOID FORCES ---------------------------------------------
        //------------------------------------------------------------------------------------------------------------
        if(status["water"]==false){
            desiredVel = desiredVel  + predatorForce +regroupForce + matchingForce + sepForce  + cohesionForce;

        }
        else
        {
            desiredVel =  desiredVel + regroupForce*waterStr; //go back towards regroup pos if in water
            if (desiredVel.sqrMagnitude<1.0f) desiredVel= desiredVel.normalized; //small nudge to avoid getting stuck

        }
        
        if(status["hunted"]==true){
            maxSpeed=huntSpeed;
        }
        else{
            maxSpeed=restSpeed;
        }
        //speed limit
        if (desiredVel.sqrMagnitude > maxSpeed*maxSpeed)
        {
            desiredVel = desiredVel.normalized * maxSpeed;
        }
        desiredVel *= 1f - (damping * Time.fixedDeltaTime);  // damping ~ 0.1–0.3


        //STEER currVEL TO newVEL VIA desiredVEL
        Vector3 currVel = rb.linearVelocity; // includes gravity Y
        Vector3 newVelXZ = new Vector3(currVel.x, 0f, currVel.z);
        float maxAcc=0f;
        if (status["hunted"] == true || status["water"] == true)  maxAcc=huntedAcc;
        else{maxAcc=restAcc;}

        newVelXZ = Vector3.MoveTowards(newVelXZ, desiredVel, maxAcc * Time.fixedDeltaTime);

        //UPDATE POS VIA RB-VEL
        //if (desiredVel.sqrMagnitude > 1e-1f) //speed minLimit
        if(true)
        {

            rb.linearVelocity = new Vector3(newVelXZ.x, currVel.y, newVelXZ.z);
            
            /*
            Quaternion want = Quaternion.LookRotation(newVelXZ, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(rb.rotation, want, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(next);
            */

        }
        
       
        
        //}
    }
    
    // Uses Unity’s physics broadphase to collect nearby colliders into a reused buffer.
    // Returns how many valid entries were written.
    int SenseNeighborsNonAlloc(Vector3 center, float radius, Collider[] buffer, LayerMask mask)
    {
        // Only check siblings (other sheep in the same herd)
        int found = 0;
        if (transform.parent == null)
            return 0;
        foreach (Transform child in transform.parent)
        {
            if (child == transform) continue; // skip self
            Collider col = child.GetComponent<Collider>();
            if (col == null) continue;
            // Optionally check layer mask
            if (((1 << col.gameObject.layer) & mask) == 0) continue;
            if ((col.transform.position - center).sqrMagnitude <= radius * radius)
            {
                if (found < buffer.Length)
                {
                    buffer[found] = col;
                    found++;
                }
            }
        }
        return found;
    }
}
