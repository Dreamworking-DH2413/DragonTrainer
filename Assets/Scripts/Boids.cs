using UnityEngine;

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
    public float senseInterval = 0.1f;    // Each sheep senses ~10×/sec
    public float cohesionRadius = 4f;     // How far we "see" other sheep
    public float sepRadius = 4f;     // How far we "see" other sheep

    //BOIDS FORCES STUFF
    public float damping = 0.1f;
    public float cohesionStr = 0.1f;
    public float sepStr = 0.5f;





    // --- Internals ---
    private Vector3 cohesionForce;
    private Vector3 sepForce;
    private Rigidbody rb;               // Cached reference to our Rigidbody
    private Vector3 heading;            // Our current movement direction on XZ (y = 0)
    private float nextSenseTime;          // When this sheep should sense next (not every frame)
    private Collider[] hits; //non-triggered. OverlapSphere func returns Colliders it finds in a radius. <- Why we need it.
    private Vector3 cohesionCenter;
    private Vector3 vel;
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
    
        hits = new Collider[maxNeighbors];                              // Allocate once; reuse forever
        nextSenseTime = 0f + Random.value * senseInterval;       // Stagger first sensing //Mayb can use 0 inst of Time.time (?)

    
    }

    void Update()
    {
        /*
        //update heading dir using BOID rules
        //forces should be normalized and have y val of 0
        if (cohesionForce.sqrMagnitude > 1e-6f)
        {
            heading = Vector3.Slerp(heading, cohesionForce, Mathf.Clamp01(cohesionStr)); //Gently rotate the current heading toward the desired steerDir.
            heading.y = 0f; //safe
            if (heading.sqrMagnitude > 1e-6f) heading.Normalize(); //safe

        }*/

        // --- Move using physics-friendly API so collisions behave nicely ---
        /*
        Vector3 targetVelXZ  = heading * maxSpeed; //* Time.fixedDeltaTime; // How far to go this physics tick (XZ)
        Vector3 vel = rb.linearVelocity; // includes current Y from gravity
        Vector3 curXZ = new Vector3(vel.x, 0f, vel.z); //exclude Y
        curXZ = Vector3.MoveTowards(curXZ, targetVelXZ, maxAcc * Time.fixedDeltaTime); //move by at most acc*dt=maxSpeed
        rb.linearVelocity = new Vector3(curXZ.x, vel.y, curXZ.z); // Reassemble final velocity: keep gravity-driven Y
        */
        //Vector3 target = rb.position + new Vector3(step.x, 0f, step.z); // Do not drive Y; gravity handles it
        //rb.MovePosition(target); // Ask the physics engine to move us to 'target'




        // --- Sense neighbors on a staggered schedule (not every frame) ---
        if (Time.time >= nextSenseTime)
        //if(true)
        {
            //reset boid forces:
            cohesionForce = Vector3.zero;
            cohesionCenter = Vector3.zero;
            sepForce = Vector3.zero;

            nextSenseTime += senseInterval;                             // Book next sensing time
            int found = SenseNeighborsNonAlloc(transform.position, cohesionRadius, hits, boidMask);
            int neighborCount = 0;

            for (int i = 0; i < found; i++)
            {
                var col = hits[i];                                      // Candidate collider
                if (!col) continue;                                     // Safety-ignore (no found)
                var otherRb = col.attachedRigidbody;                    // Prefer Rigidbody to identify self
                if (otherRb == rb) continue;                            // if self-ignore

                Vector3 dstToOther = col.transform.position - transform.position;
                dstToOther.y = 0f;
                float sqr = dstToOther.sqrMagnitude;

                // Accumulate cohesion force
                if (sqr > cohesionRadius * cohesionRadius) continue;    // out of range-ignore
                neighborCount++; //found a neighbour!
                cohesionCenter += col.transform.position;

                // Accumulate sep force
                if (sqr > sepRadius * sepRadius) continue;
                sepForce += (-dstToOther) * (sepRadius / Mathf.Min(dstToOther.sqrMagnitude, 0.1f)); //neg because the other way. Apply greater force if closer
            }

            // BOID FORCES calcs
            if (neighborCount > 0)
            {
                cohesionCenter = cohesionCenter / neighborCount;
                Vector3 centerDir = cohesionCenter - transform.position;
                centerDir.y = 0f;
                cohesionForce = centerDir.normalized;   // direction only; accel cap handles magnitude
                cohesionForce *= cohesionStr;
                
                sepForce = sepForce.normalized;
                sepForce *= sepStr;
            }


        }

        Vector3 oldVel = vel;
        //UPDATE POS/VEL VIA BOID FORCES
        vel = vel + sepForce + cohesionForce;
        //vel.y=0f; //safe

        //speed limit
        if (vel.sqrMagnitude > maxSpeed)
        {
            vel = vel.normalized * maxSpeed;
        }
        Vector3 slerpVel= Vector3.zero;
        if (vel.sqrMagnitude > 1e-1f) //speed minLimit
        {
            slerpVel = Vector3.Slerp(oldVel, vel, 0.5f); //Gently rotate the current heading toward the desired steerDir.

        }
        slerpVel*= (1f - damping * Time.deltaTime);  // damping ~ 0.1–0.3
        //Debug.Log("\n" + vel.sqrMagnitude);
        transform.position += slerpVel * Time.deltaTime;

        

        
        // Optionally rotate the body to face the movement direction (yaw only)
          // --- Face movement direction (yaw only) ---
        /*
        if (faceMoveDirection && vel.sqrMagnitude > 1e-6f)
        {
            Quaternion want = Quaternion.LookRotation(vel, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(rb.rotation, want, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(next);
        }
        */


    }

    //==============================================
    void FixedUpdate()
    {
        /*
// --- Move using physics-friendly API so collisions behave nicely ---
        
        Vector3 targetVelXZ  = heading * maxSpeed; //* Time.fixedDeltaTime; // How far to go this physics tick (XZ)
        Vector3 vel = rb.linearVelocity; // includes current Y from gravity
        Vector3 curXZ = new Vector3(vel.x, 0f, vel.z); //exclude Y
        curXZ = Vector3.MoveTowards(curXZ, targetVelXZ, maxAcc * Time.fixedDeltaTime); //move by at most acc*dt=maxSpeed
        rb.linearVelocity = new Vector3(curXZ.x, vel.y, curXZ.z); // Reassemble final velocity: keep gravity-driven Y
        
        //Vector3 target = rb.position + new Vector3(step.x, 0f, step.z); // Do not drive Y; gravity handles it
        //rb.MovePosition(target); // Ask the physics engine to move us to 'target'
        //vel = vel + cohesionForce;
        //transform.position += vel * Time.fixedDeltaTime;


        // Optionally rotate the body to face the movement direction (yaw only)
          // --- Face movement direction (yaw only) ---
        if (faceMoveDirection && curXZ.sqrMagnitude > 1e-6f)
        {
            Quaternion want = Quaternion.LookRotation(curXZ, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(rb.rotation, want, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(next);
        }

        // --- Sense neighbors on a staggered schedule (not every frame) ---
        if (Time.fixedTime >= nextSenseTime)
        {
            //reset boid forces:
            cohesionForce = Vector3.zero;
            cohesionCenter = Vector3.zero;
            
            nextSenseTime += senseInterval;                             // Book next sensing time
            int found = SenseNeighborsNonAlloc(transform.position, cohesionRadius, hits, boidMask);
            int neighborCount = 0;

            // Example: iterate results (you’ll use this later for boid forces)
            for (int i = 0; i < found; i++)
            {
                var col = hits[i];                                      // Candidate collider
                if (!col) continue;                                     // Safety-ignore (no found)
                var otherRb = col.attachedRigidbody;                    // Prefer Rigidbody to identify self
                if (otherRb == rb) continue;                            // if self-ignore

                // Optional precise filtering (e.g., 2D distance on XZ)
                Vector3 dstToOther = col.transform.position - transform.position;
                dstToOther.y = 0f;
                float sqr = dstToOther.sqrMagnitude;
                if (sqr > cohesionRadius * cohesionRadius) continue;    // out of range-ignore
                neighborCount++; //found a neighbour!

                // ACCUMULATE BOID FORCES
                // e.g., alignmentDir += other.forward; separation += -dstToOther.normalized, etc.
                cohesionCenter += col.transform.position;
            }

            //CREATE BOID FORCES
            if (neighborCount  > 0)
            {
                cohesionCenter = cohesionCenter / neighborCount;
                Vector3 centerDir = cohesionCenter - transform.position;
                centerDir.y = 0f;
                cohesionForce = centerDir.normalized;   // direction only; accel cap handles magnitude

            }
            
            
        }
        */
        
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
