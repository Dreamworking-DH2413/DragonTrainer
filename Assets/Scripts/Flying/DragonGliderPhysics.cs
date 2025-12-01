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
    public float glideThrust = 15f;

    Rigidbody rb;
    GliderSurface_Controller surfaces;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        surfaces = GliderSurface_Controller.Instance;

        // Make sure physics isn't super wild
        rb.linearDamping = 0.1f;
        rb.angularDamping = 2f;   // helps stop spinning
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
    }
}
