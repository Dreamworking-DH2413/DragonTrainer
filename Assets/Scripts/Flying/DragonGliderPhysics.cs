using UnityEngine;
using AztechGames;

[RequireComponent(typeof(Rigidbody))]
public class DragonGliderPhysics : MonoBehaviour
{
    [Header("Lift / Drag")]
    public float liftCoefficient = 0.05f;     // much smaller than before
    public float dragCoefficient = 0.01f;

    [Header("Control Authority")]
    public float pitchTorque = 5f;           // was 50f!
    public float rollTorque = 5f;
    public float yawTorque = 1f;           // not used yet

    [Header("Stability")]
    public float angularDamping = 2f;        // stabilizing torque

    [Header("Forward Thrust")]
    private float glideThrust = 0.0f;
    public float maxThrust = 10000f;
    public float minThrust = 100f;
    public float thrustMultiplier = 500f;
    

    [Header("VR Player Follow")]
    public Vector3 dragonHeadOffset = new Vector3(0, 2f, 1.5f);  // Offset for host (dragon's head/eyes position)
    public Vector3 riderOffset = new Vector3(0, 1.5f, 0);        // Offset for client (rider on dragon's back)
    public bool rotatePlayerWithDragon = true;                    // Whether player rotates with dragon

    Rigidbody rb;
    GliderSurface_Controller surfaces;
    private Transform playerTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        surfaces = GliderSurface_Controller.Instance;

        // Make sure physics isn't super wild
        rb.linearDamping = 0.1f;
        rb.angularDamping = 2f;   // helps stop spinning

        // Find the Player object in the scene
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            Debug.Log("DragonGliderPhysics: Found Player object");
        }
        else
        {
            Debug.LogWarning("DragonGliderPhysics: Player object not found in scene!");
        }
    }

    void FixedUpdate()
    {
        // 1) Update inputs (uses Horizontal/Vertical)
        surfaces.GetInputs();

        float pitchInput = surfaces.ElevatorAmount / surfaces.elevatorMaxAngle;  // -1..1
        float rollInput = surfaces.AileronAmount / surfaces.aileronMaxAngle;   // -1..1

        // 2) Control torques (small!)
        Vector3 controlTorque = Vector3.zero;
        controlTorque += -pitchInput * pitchTorque * Vector3.right;   // pitch
        controlTorque += -rollInput * rollTorque * Vector3.forward; // roll

        rb.AddRelativeTorque(controlTorque, ForceMode.Acceleration);

        // 3) Aerodynamic damping (resists spin)
        Vector3 localAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 dampingTorque = -localAngularVel * angularDamping;
        rb.AddRelativeTorque(dampingTorque, ForceMode.Acceleration);

        // 4) Constant forward thrust
        rb.AddForce(transform.forward * glideThrust, ForceMode.Force);

        // 5) Lift & drag
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        if (speed > 0.01f)
        {
            Vector3 forward = transform.forward;
            float forwardSpeed = Vector3.Dot(velocity, forward);

            // simple lift
            float lift = liftCoefficient * forwardSpeed * forwardSpeed;
            Vector3 liftDir = transform.up;
            rb.AddForce(liftDir * lift, ForceMode.Force);

            // drag
            Vector3 drag = -velocity.normalized * dragCoefficient * speed * speed;
            rb.AddForce(drag, ForceMode.Force);
        }

        // 6) Update player position to follow dragon
        UpdateVRPlayerPosition();
    }

    private void UpdateVRPlayerPosition()
    {
        if (playerTransform == null)
            return;

        // Determine which offset to use based on player type from GameManager
        bool isHost = GameManager.Instance != null && GameManager.Instance.IsHost;
        Vector3 playerOffset = isHost ? dragonHeadOffset : riderOffset;

        // Position Player relative to the dragon in LOCAL space
        // The player offset is applied in the dragon's local coordinate system
        Vector3 worldOffset = transform.TransformDirection(playerOffset);
        Vector3 targetPosition = transform.position + worldOffset;
        
        playerTransform.position = targetPosition;

        // Rotate the Player with the dragon if enabled
        if (rotatePlayerWithDragon)
        {
            // Match the dragon's rotation exactly so the player experiences all the rolls, pitches, and yaws
            playerTransform.rotation = transform.rotation;
        }
    }

    public void SetThrustByVelocity(float avgVelocityY)
    {
        float thrust = Mathf.Clamp(-avgVelocityY * thrustMultiplier, minThrust, maxThrust);
        glideThrust = thrust;
    }
}
