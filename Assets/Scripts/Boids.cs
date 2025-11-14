using UnityEngine;
using System.Collections.Generic;


public class Boids : MonoBehaviour
{
    
    // MOVEMENT
    public float maxSpeed=51.8f;
    public float maxAcc = 50.0f;        // Units per second along the ground (XZ)
    public float turnSpeed = 90f;       // Degrees per second we can rotate around Y
    public bool faceMoveDirection = true; // If true, rotate to face where we’re moving

    // SENSING NEIGHBOUR BOIDS
    public LayerMask boidMask;            // Layer that all sheep are on (e.g., "Boid")
    public int maxNeighbors = 64;         // Size of reusable hits buffer
    public float senseHz = 0.05f;    // Each sheep senses ~10×/sec
    float senseInterval;
    public float cohesionRadius = 4f;     // How far we "see" other sheep
    public float pastureRadius = 4f;     // How far we "see" other sheep
    public float sepRadius = 4f;     // How far we "see" other sheep
    public float predatorRadius = 10f;   // how far away we start caring about the player
    public float matchingRadius = 10f;   // how far away we start caring about the player

    public Transform player;             // set in Awake or inspector


    //BOIDS FORCES STUFF
    Dictionary<string, bool> status = new Dictionary<string, bool>()
    {
        { "regrouping", false },
        { "pasture", false },
        { "hunted", false }
    };
    public float damping = 0.1f;
    public float cohesionStr = 0.1f;
    public float sepStr = 0.5f;
    public float sepMax = 3.5f;
    public float predatorStr = 1.5f;     // strength of the avoidance
    public float matchingStr = 0.8f;      // str of mathching vel of surrounding fleeing cheep






    // --- Internals ---
    private Vector3 cohesionForce;
    private Vector3 sepForce;
    private Vector3 predatorForce;
    private Vector3 matchingForce;


    private Rigidbody rb;               // Cached reference to our Rigidbody
    private Vector3 heading;            // Our current movement direction on XZ (y = 0)
    private float nextSenseTime;          // When this sheep should sense next (not every frame)
    private Collider[] hits; //non-triggered. OverlapSphere func returns Colliders it finds in a radius. <- Why we need it.
    private Vector3 cohesionCenter;
    private Vector3 matchingVel;

    private Vector3 desiredVel;

    //==============================================
     void Awake()
    {
        rb = GetComponent<Rigidbody>();                       // Grab Rigidbody once
        rb.useGravity = true;                                 // Let physics keep us on the ground
        rb.interpolation = RigidbodyInterpolation.Interpolate;// Smooth visuals between physics steps
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Start with a random XZ heading so all sheep don’t move identically
        Vector2 rnd = Random.insideUnitCircle.normalized;     // 2D random on the plane
        heading = new Vector3(rnd.x, 0f, rnd.y);              // Set XZ (L/R), keep y (UP) = 0
        if (heading.sqrMagnitude < 1e-4f) heading = Vector3.forward;// Fallback just in case

        Vector2 r = Random.insideUnitCircle.normalized;
        //desiredVel = new Vector3(r.x, 0f, r.y) * 0.5f; // small nudge


        hits = new Collider[maxNeighbors];                              // Allocate once; reuse forever
        senseInterval = 1f / senseHz;   // e.g., 5 Hz -> 0.2 s

        nextSenseTime = Time.fixedTime + Random.value * senseInterval;        // Stagger first sensing //Mayb can use 0 inst of Time.time (?)

            // Auto-find player if not assigned in Inspector
        if (player == null)
        {
            GameObject p = GameObject.Find("FallbackObjects"); // Use Find by name instead
            if (p != null) player = p.transform;
        }
    
    }

    void Update()
    {

    }
    
    void calcPredatorForce()
    {
        
        Vector3 dstToPredator = player.position - transform.position;
        dstToPredator.y = 0f;
        
        if (dstToPredator.sqrMagnitude <= predatorRadius * predatorRadius)// out of range-ignore (!) should prob not be squared???
        {
            predatorForce = -dstToPredator * ((predatorRadius*predatorRadius) / Mathf.Max(dstToPredator.sqrMagnitude, 0.001f));
            predatorForce *= predatorStr;
            status["hunted"]=true;

        }
        else
        {
            predatorForce = Vector3.zero;
            status["hunted"]=false;
        }
    }

    void calcBoidForces()
    {
        

        //reset boid forces:
        cohesionForce = Vector3.zero;
        cohesionCenter = Vector3.zero;
        sepForce = Vector3.zero;
        matchingForce=Vector3.zero;
        matchingVel = Vector3.zero;
        
        
        int found = SenseNeighborsNonAlloc(transform.position, cohesionRadius, hits, boidMask);
        int cohersionCount = 0;
        int matchingCount = 0;

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
            if ((sqrDst <= cohesionRadius * cohesionRadius) && (sqrDst >= pastureRadius * pastureRadius))    // out of range-ignore
            {
                cohersionCount++; //found a neighbour!
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
            if (status["hunted"] == true)
            {
                if (sqrDst <= matchingRadius * matchingRadius)
                {
                    matchingCount++; //found a neighbour!
                    matchingVel += otherRb.linearVelocity;
                }
            }
            
            

        }

        // BOID FORCES calcs
        if (cohersionCount > 0)
        {
            cohesionCenter = cohesionCenter / cohersionCount;
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


        sepForce = Vector3.ClampMagnitude(sepForce, sepMax); // clamp the total separation force (since we dont normalize it)
        sepForce *= sepStr;
        


        
    }

    //==============================================
    void FixedUpdate()
    {
        // --- Sense and reCalc forces (throttled) ---
        //if (Time.fixedTime >= nextSenseTime)
        //{
         //   nextSenseTime += senseInterval; //book next sense session
            calcBoidForces();
        //}
        calcPredatorForce(); //we afford do every update because its only 1 item we look for
        //---------------------------------------------------------------------------------
        //---------------------------UPDATE VEL VIA BOID FORCES ---------------------------------------------
        //------------------------------------------------------------------------------------------------------------
        desiredVel = desiredVel  + predatorForce +  matchingForce; //sepForce + cohesionForce;
        //speed limit
        if (desiredVel.sqrMagnitude > maxSpeed*maxSpeed)
        {
            desiredVel = desiredVel.normalized * maxSpeed;
        }
        desiredVel *= 1f - (damping * Time.fixedDeltaTime);  // damping ~ 0.1–0.3


        //STEER currVEL TO newVEL VIA desiredVEL
        Vector3 currVel = rb.linearVelocity; // includes gravity Y
        Vector3 newVelXZ = new Vector3(currVel.x, 0f, currVel.z);
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
        
       
        
    }
    
    // Uses Unity’s physics broadphase to collect nearby colliders into a reused buffer.
    // Returns how many valid entries were written.
    int SenseNeighborsNonAlloc(Vector3 center, float radius, Collider[] buffer, LayerMask mask)
    {
        // QueryTriggerInteraction.Collide lets us pick up triggers if agents are triggers
        // This call writes up to buffer.Length colliders into buffer=hits(!!!) and returns how many
        int found = Physics.OverlapSphereNonAlloc(center, radius, buffer, mask, QueryTriggerInteraction.Collide);

        // If found > buffer.Length, results are truncated. You can increase maxNeighbors if needed.
        return Mathf.Min(found, buffer.Length); //can max be buffer-length
    }
}
