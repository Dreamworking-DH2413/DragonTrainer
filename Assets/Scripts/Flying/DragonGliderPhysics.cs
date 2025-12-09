using UnityEngine;
using AztechGames;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class DragonGliderPhysics : NetworkBehaviour
{
    /* ----------------- ANGLE LIMITS (NO LOOPS / ROLLS) ----------------- */

    [Header("Angle Limits")]
    public float maxRollAngle = 60f;        // +/- max roll (no barrel rolls)
    public float maxPitchAngleUp = 30f;     // Nose up limit (relative to neutral)
    public float maxPitchAngleDown = 20f;   // Nose down limit (relative to neutral)

    [Header("Soft Limits (Anti-Jitter)")]
    [Tooltip("Degrees near roll limit where control authority fades out")]
    public float rollSoftZone = 5f;

    [Tooltip("Degrees near nose-up limit where control authority fades out")]
    public float pitchSoftZoneUp = 5f;

    [Tooltip("Degrees near nose-down limit where control authority fades out")]
    public float pitchSoftZoneDown = 5f;

    /* ----------------- CONTROL RESPONSE ----------------- */

    [Header("Control Response")]
    public float pitchTorque = 5f;
    public float rollTorque = 5f;
    public float yawTorque = 2f;

    [Tooltip("Speed at which controls reach full authority")]
    public float controlFullEffectSpeed = 25f;

    [Header("Turn Behaviour (A/D)")]
    [Tooltip("Extra nose-up when turning with A/D")]
    public float turnPitchUpAmount = 10f;

    [Tooltip("How much yaw from A/D directly (rudder)")]
    public float yawFromInputFactor = 1.0f;

    [Tooltip("How much yaw from actual bank angle (auto-coordination)")]
    public float autoYawFromBankFactor = 0.8f;

    [Header("Input")]
    public float inputDeadzone = 0.05f;

    /* ----------------- AZTECH SURFACES ----------------- */

    [Header("Aztech Surfaces")]
    [Tooltip("How fast aileron/elevator amounts move toward target / neutral")]
    public float surfaceCenterSpeed = 3f;   // units per second in [-1,1] range

    /* ----------------- STABILITY & AERODYNAMICS ----------------- */

    [Header("Stability & Aerodynamics")]
    public float angularDamping = 2f;

    public float liftCoefficient = 0.5f;
    public float dragCoefficient = 0.02f;
    public float aoaDragMultiplier = 3f;

    /* ----------------- THRUST ----------------- */

    [Header("Thrust")]
    [SerializeField] private float glideThrust = 0.0f;
    public float maxThrust = 10000f;
    public float minThrust = 100f;
    public float thrustMultiplier = 500f;

    [Header("Turn Thrust Boost")]
    [Tooltip("If true, add extra forward thrust when banked to avoid losing too much speed in tight turns")]
    public bool enableTurnThrustBoost = true;

    [Tooltip("Extra forward force at maximum bank (|roll| = maxRollAngle)")]
    public float maxBankThrustBoost = 5000f;

    /* ----------------- NEUTRAL ORIENTATION ----------------- */

    [Header("Neutral Orientation")]
    public bool useInitialRotationAsNeutral = true;
    Quaternion neutralRotation; // "level flight" orientation

    /* ----------------- VR PLAYER LINK ----------------- */

    [Header("VR Rider / Debug")]
    public bool freezeDragon = false;
    public float debugThrustBoost = 5000f;  // Extra thrust when pressing space
    public Vector3 dragonHeadOffset = new Vector3(0, 2f, 1.5f);
    public Vector3 riderOffset = new Vector3(0, 1.5f, 0);
    public bool rotatePlayerWithDragon = true;

    Rigidbody rb;
    GliderSurface_Controller surfaces;
    Transform playerTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        surfaces = GliderSurface_Controller.Instance;

        rb.useGravity = true;
        rb.linearDamping = 0.1f;
        rb.angularDamping = angularDamping;

        // Whatever rotation we start in is considered "level / neutral"
        neutralRotation = useInitialRotationAsNeutral ? transform.rotation : Quaternion.identity;

        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
            playerTransform = playerObject.transform;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // NetworkRigidbody + NetworkTransform handle all syncing
        // Server runs physics, NetworkRigidbody syncs to clients
    }

    void FixedUpdate()
    {
        // Only run physics simulation on server or in single-player
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (!isNetworked || IsServer)
        {
            RunPhysicsSimulation();
        }
    }

    void RunPhysicsSimulation()
    {
        /* ---------------- OPTIONAL FREEZE FOR DEBUG ---------------- */
        if (freezeDragon)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            UpdateVRPlayerPosition();
            return;
        }
        else
        {
            rb.useGravity = true;
        }

        /* ---------------- READ INPUT ---------------- */

        // Invert horizontal axis so A = left, D = right in our convention
        float turnInput = -Input.GetAxisRaw("Horizontal");   // A/D or stick left-right
        float pitchInputRaw = Input.GetAxisRaw("Vertical");  // W/S or stick up-down

        if (Mathf.Abs(turnInput) < inputDeadzone) turnInput = 0f;
        if (Mathf.Abs(pitchInputRaw) < inputDeadzone) pitchInputRaw = 0f;

        /* ---------------- VELOCITY & AOA ---------------- */

        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        Vector3 localVel = Vector3.zero;
        float forwardSpeed = 0f;
        float aoa = 0f;

        if (speed > 0.01f)
        {
            localVel = transform.InverseTransformDirection(velocity);
            forwardSpeed = Mathf.Max(0f, localVel.z);
            aoa = Mathf.Atan2(localVel.y, Mathf.Max(0.01f, forwardSpeed));
        }

        /* ---------------- CURRENT ATTITUDE RELATIVE TO NEUTRAL ---------------- */

        Quaternion relativeRot = Quaternion.Inverse(neutralRotation) * transform.rotation;
        Vector3 relEuler = relativeRot.eulerAngles;

        // Map 0..360 -> -180..180 for small-angle control
        float currentPitchRel = Mathf.DeltaAngle(0f, relEuler.x); // + = nose down
        float currentRollRel = Mathf.DeltaAngle(0f, relEuler.z);  // + = right wing down

        /* ---------------- TARGET ATTITUDE (CLAMPED) ---------------- */

        // Target pitch:
        float targetPitchRel = 0f;

        if (pitchInputRaw > 0f)
        {
            // Pitch down input (S) -> nose down
            targetPitchRel = pitchInputRaw * maxPitchAngleDown;
        }
        else if (pitchInputRaw < 0f)
        {
            // Pitch up input (W) -> nose up (negative rel)
            targetPitchRel = pitchInputRaw * maxPitchAngleUp;
        }

        // Extra pitch up when turning with A/D
        if (Mathf.Abs(turnInput) > 0f)
        {
            // Negative rel pitch = nose up, so subtract
            targetPitchRel -= Mathf.Abs(turnInput) * turnPitchUpAmount;
        }

        // Clamp pitch so we never loop
        targetPitchRel = Mathf.Clamp(targetPitchRel, -maxPitchAngleUp, maxPitchAngleDown);

        // Target roll directly from A/D input, clamped to prevent barrel rolls
        float targetRollRel = Mathf.Clamp(turnInput * maxRollAngle, -maxRollAngle, maxRollAngle);

        /* ---------------- ERROR & SOFT-LIMIT SCALING ---------------- */

        float rawPitchError = Mathf.DeltaAngle(currentPitchRel, targetPitchRel);
        float rawRollError = Mathf.DeltaAngle(currentRollRel, targetRollRel);

        float pitchLimitFactor = 1f;
        float rollLimitFactor = 1f;

        // --- Roll soft zone ---
        if (maxRollAngle > 0.1f && rollSoftZone > 0f && Mathf.Abs(turnInput) > 0f)
        {
            float absRoll = Mathf.Abs(currentRollRel);
            float rollToLimit = maxRollAngle - absRoll;

            // >0 if input is trying to push further into the current bank direction
            float rollPushDir = Mathf.Sign(turnInput) * Mathf.Sign(currentRollRel);

            if (rollPushDir > 0f && rollToLimit <= rollSoftZone)
            {
                // 1 at edge of soft zone, 0 exactly at limit
                float t = Mathf.Clamp01(rollToLimit / rollSoftZone);
                rollLimitFactor = t;
            }
        }

        // --- Pitch soft zones (separate up / down) ---
        if (pitchSoftZoneUp > 0f || pitchSoftZoneDown > 0f)
        {
            // Nose up region
            if (targetPitchRel < 0f && currentPitchRel < 0f && pitchInputRaw < 0f && pitchSoftZoneUp > 0f)
            {
                float distToUpLimit = Mathf.Abs(currentPitchRel + maxPitchAngleUp); // 0 at -maxPitchAngleUp
                if (distToUpLimit <= pitchSoftZoneUp)
                {
                    float t = Mathf.Clamp01(distToUpLimit / pitchSoftZoneUp);
                    pitchLimitFactor = t;
                }
            }
            // Nose down region
            else if (targetPitchRel > 0f && currentPitchRel > 0f && pitchInputRaw > 0f && pitchSoftZoneDown > 0f)
            {
                float distToDownLimit = Mathf.Abs(currentPitchRel - maxPitchAngleDown); // 0 at +maxPitchAngleDown
                if (distToDownLimit <= pitchSoftZoneDown)
                {
                    float t = Mathf.Clamp01(distToDownLimit / pitchSoftZoneDown);
                    pitchLimitFactor = t;
                }
            }
        }

        float pitchError = rawPitchError * pitchLimitFactor;
        float rollError = rawRollError * rollLimitFactor;

        float controlEffect = Mathf.Clamp01(speed / controlFullEffectSpeed);

        /* ---------------- YAW: INPUT + BANK-BASED ---------------- */

        float yawFromInput = turnInput * yawFromInputFactor;

        float bankRatio = (maxRollAngle > 0.1f) ? (currentRollRel / maxRollAngle) : 0f;
        float yawFromBank = bankRatio * -autoYawFromBankFactor;

        float yawInput = yawFromInput + yawFromBank;

        /* ---------------- APPLY TORQUE (CORE BEHAVIOUR) ---------------- */

        Vector3 torque = Vector3.zero;

        torque.x = pitchError * pitchTorque * controlEffect; // pitch toward target
        torque.z = rollError * rollTorque * controlEffect;   // roll toward target
        torque.y = yawInput * yawTorque * controlEffect;     // yaw for turn

        rb.AddRelativeTorque(torque, ForceMode.Acceleration);

        /* ---------------- EXTRA ANGULAR DAMPING ---------------- */

        Vector3 angLocal = transform.InverseTransformDirection(rb.angularVelocity);
        rb.AddRelativeTorque(-angLocal * angularDamping, ForceMode.Acceleration);

        /* ---------------- AZTECH SURFACES: FOLLOW TARGET + RESET ---------------- */

        if (surfaces != null)
        {
            float dt = Time.fixedDeltaTime;
            float maxDelta = surfaceCenterSpeed * dt;

            // Map target roll to aileron amount (-1..1)
            float aileronTarget = 0f;
            if (maxRollAngle > 0.1f)
                aileronTarget = Mathf.Clamp(targetRollRel / maxRollAngle, -1f, 1f);

            // Map target pitch to elevator amount (-1..1)
            float elevatorTarget = 0f;
            float maxPitchMagUp = Mathf.Max(1e-3f, maxPitchAngleUp);
            float maxPitchMagDown = Mathf.Max(1e-3f, maxPitchAngleDown);

            if (targetPitchRel < 0f)
            {
                // Negative rel pitch = nose up -> positive elevator
                float norm = -targetPitchRel / maxPitchMagUp;
                elevatorTarget = Mathf.Clamp(norm, -1f, 1f);
            }
            else if (targetPitchRel > 0f)
            {
                // Positive rel pitch = nose down -> negative elevator
                float norm = -targetPitchRel / maxPitchMagDown;
                elevatorTarget = Mathf.Clamp(norm, -1f, 1f);
            }

            // If no input at all, both targets go toward 0 (auto-level surfaces / trim reset)
            bool anyInput = Mathf.Abs(turnInput) > 0f || Mathf.Abs(pitchInputRaw) > 0f;
            if (!anyInput)
            {
                aileronTarget = 0f;
                elevatorTarget = 0f;
            }

            surfaces.AileronAmount = Mathf.MoveTowards(surfaces.AileronAmount, aileronTarget, maxDelta);
            surfaces.ElevatorAmount = Mathf.MoveTowards(surfaces.ElevatorAmount, elevatorTarget, maxDelta);
        }

        /* ---------------- THRUST ---------------- */

        // Base thrust (from glide / dive logic)
        float currentThrust = glideThrust;

        // Debug: Add extra thrust when space is pressed
        if (Input.GetKey(KeyCode.Space))
        {
            currentThrust += debugThrustBoost;
        }

        if (currentThrust != 0f)
        {
            rb.AddForce(transform.forward * currentThrust, ForceMode.Force);
        }

        // Extra thrust when banked hard, so sharp turns don't bleed all your speed
        if (enableTurnThrustBoost && maxRollAngle > 0.1f)
        {
            float bankAmount = Mathf.Clamp01(Mathf.Abs(currentRollRel) / maxRollAngle); // 0 (level) .. 1 (max bank)
            float extraThrust = maxBankThrustBoost * bankAmount;
            if (extraThrust > 0f)
            {
                rb.AddForce(transform.forward * extraThrust, ForceMode.Force);
            }
        }

        /* ---------------- LIFT & DRAG ---------------- */

        if (speed > 0.01f && forwardSpeed > 0.01f)
        {
            float lift = liftCoefficient * forwardSpeed * forwardSpeed * Mathf.Cos(aoa);
            rb.AddForce(transform.up * lift, ForceMode.Force);

            float aoaDrag = 1f + Mathf.Abs(aoa) * aoaDragMultiplier;
            rb.AddForce(-velocity.normalized * dragCoefficient * speed * speed * aoaDrag, ForceMode.Force);
        }
    }

    void Update()
    {
        // Update player position for clients
        UpdateVRPlayerPosition();
    }

    void UpdateVRPlayerPosition()
    {
        if (!playerTransform) return;

        bool isHost = GameManager.Instance != null && GameManager.Instance.IsHost;
        Vector3 offset = isHost ? dragonHeadOffset : riderOffset;

        playerTransform.position = transform.position + transform.TransformDirection(offset);

        if (rotatePlayerWithDragon)
            playerTransform.rotation = transform.rotation;
    }

    // External hook to drive thrust based on vertical speed
    public void SetThrustByVelocity(float avgVelocityY)
    {
        glideThrust = Mathf.Clamp(-avgVelocityY * thrustMultiplier, minThrust, maxThrust);
    }
}
