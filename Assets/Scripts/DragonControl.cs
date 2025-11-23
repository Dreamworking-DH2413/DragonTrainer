using UnityEngine;
using UnityEngine.InputSystem;

public class DragonControl : MonoBehaviour
{
    [Header("Flight Mechanics")]
    [SerializeField] private float flapForce = 50f;           // Forward impulse when flapping
    [SerializeField] private float flapUpForce = 50f;         // Upward impulse when flapping
    [SerializeField] private float flapDuration = 1f;         // Duration of flap animation/state (not force application)
    [SerializeField] private float flapCooldown = 0.5f;       // Minimum time between flaps
    [SerializeField] private float flapDrag = 0.2f;           // Air resistance while flapping (very low to maintain momentum)
    [SerializeField] private float glideDrag = 0.1f;          // Air resistance while gliding (very low for efficient gliding)
    [SerializeField] private float groundDrag = 5f;           // Drag when on ground to stop movement
    [SerializeField] private float gravityScale = 0.5f;       // Reduce gravity effect (0.5 = half gravity, 0.3 = very light)
    
    [Header("Wing Mechanics")]
    [SerializeField] private float expandedLiftMultiplier = 1.5f;    // Extra lift when wings expanded
    [SerializeField] private float expandedDragMultiplier = 1.3f;    // Extra drag when wings expanded
    [SerializeField] private float retractedSpeedMultiplier = 1.4f;  // Speed bonus when wings retracted
    [SerializeField] private float retractedLiftMultiplier = 0.6f;   // Reduced lift when wings retracted
    [SerializeField] private float glidingLiftForce = 5f;            // Upward force while gliding straight with expanded wings
    [SerializeField] private float velocityRedirectSpeed = 3f;       // How fast velocity aligns with forward direction (higher = tighter turns)
    [SerializeField] private float turnDragFactor = 0.98f;           // Speed retained per degree of turn (0.98 = 2% loss per degree)
    [SerializeField] private float minSpeedForLift = 2f;             // Minimum speed to generate lift while gliding
    
    [Header("Rotation (Pitch, Yaw, Roll)")]
    [SerializeField] private float pitchSpeed = 60f;          // Up/down rotation (W/S)
    [SerializeField] private float yawSpeed = 60f;            // Left/right rotation (A/D)
    [SerializeField] private float rollSpeed = 90f;           // Roll rotation (Q/E)
    
    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("VR Player Follow")]
    [SerializeField] private Transform vrPlayerTransform;      // The VR Player root object
    [SerializeField] private Vector3 playerOffset = new Vector3(0, 1, 0);  // Offset from dragon (player sits on dragon's back)
    [SerializeField] private bool rotatePlayerWithDragon = true;  // Whether player rotates with dragon

    private Rigidbody rb;
    private bool isGrounded = false;
    private Animator animator;              // Reference to animator (if present)
    
    // Flapping state tracking
    private float flapTimer = 0f;           // Time remaining in current flap
    private float flapCooldownTimer = 0f;   // Time until next flap is allowed
    private bool isCurrentlyFlapping = false;
    private bool flapRequested = false;     // Track if flap was requested this frame
    
    // Wing state tracking
    private bool wingsExpanded = true;      // Default to expanded wings
    private Vector3 previousVelocity;       // Track velocity to detect turns
    private Vector3 previousForward;        // Track forward direction to detect turns

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Get animator if present (for rigged models)
        animator = GetComponentInChildren<Animator>();
        
        // CRITICAL FIX FOR RIGGED MODELS:
        // Configure animator to not interfere with physics
        if (animator != null)
        {
            Debug.Log("Rigged dragon detected! Configuring animator for physics control...");
            
            // Disable root motion so animator doesn't override Rigidbody movement
            animator.applyRootMotion = false;
            
            // Set update mode to Fixed so animator updates in sync with physics (FixedUpdate)
            animator.updateMode = AnimatorUpdateMode.Fixed;
            
            Debug.Log("Animator configured: Root Motion = " + animator.applyRootMotion + 
                     ", Update Mode = " + animator.updateMode);
        }
        
        // CRITICAL FIX FOR RIGGED MODELS:
        // Ensure Rigidbody is on the root object with the script
        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on " + gameObject.name + "! Adding one automatically.");
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 10f;
            rb.linearDamping = glideDrag;
            rb.angularDamping = 0.5f;
        }
        
        // Configure Rigidbody constraints to prevent unwanted rotation interference
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        previousVelocity = rb.linearVelocity;
        previousForward = transform.forward;
        
        // Find VR Player if not assigned
        if (vrPlayerTransform == null)
        {
            // Try to find SteamVR Player by type
            var player = FindFirstObjectByType<Valve.VR.InteractionSystem.Player>();
            if (player != null)
            {
                vrPlayerTransform = player.transform;
                Debug.Log("Found SteamVR Player: " + vrPlayerTransform.name);
            }
            else
            {
                // Fallback: Try to find by name
                GameObject playerObj = GameObject.Find("Player");
                if (playerObj != null)
                {
                    vrPlayerTransform = playerObj.transform;
                    Debug.Log("Found Player object by name: " + vrPlayerTransform.name);
                }
                else
                {
                    Debug.LogWarning("VR Player not found! Please assign vrPlayerTransform in the inspector.");
                }
            }
        }
    }

    void Update()
    {
        CheckGrounded();
        
        // Detect SPACE key press in Update where wasPressedThisFrame is reliable
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            flapRequested = true;
        }
        
        // Toggle wing state with LEFT SHIFT key
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            wingsExpanded = !wingsExpanded;
            Debug.Log("Wings " + (wingsExpanded ? "EXPANDED" : "RETRACTED"));
        }
    }

    void FixedUpdate()
    {
        ApplyRotation();
        HandleFlapInput();  // Check flap request AFTER rotation
        ApplyThrust();
    }

    void LateUpdate()
    {
        //should be on if wanna test ride dragon
        //UpdateVRPlayerPosition();
    }

    private void HandleFlapInput()
    {
        // Process flap request from Update (after rotation has been applied this frame)
        if (flapRequested)
        {
            flapRequested = false;  // Reset flag
            Debug.Log("SPACE pressed! Cooldown: " + flapCooldownTimer + ", Grounded: " + isGrounded);
            
            // Only allow flap if cooldown has expired
            if (flapCooldownTimer <= 0f)
            {
                StartFlap();
            }
            else
            {
                Debug.Log("Flap blocked - cooldown still active: " + flapCooldownTimer);
            }
        }
    }

    private void StartFlap()
    {
        flapTimer = flapDuration;
        flapCooldownTimer = flapCooldown;
        isCurrentlyFlapping = true;
        
        // Apply flap impulse immediately (sudden acceleration)
        Vector3 forwardForce = transform.forward * flapForce;
        Vector3 upwardForce = transform.up * flapUpForce;
        Vector3 totalForce = forwardForce + upwardForce;
        
        Debug.Log("FLAP! Applying impulse: " + totalForce + " | Forward: " + forwardForce + " | Up: " + upwardForce);
        
        rb.AddForce(totalForce, ForceMode.Impulse);
    }

    private void UpdateFlapState()
    {
        // Decrease flap timer
        if (flapTimer > 0f)
        {
            flapTimer -= Time.fixedDeltaTime;
            if (flapTimer <= 0f)
            {
                isCurrentlyFlapping = false;
                flapTimer = 0f;
            }
        }

        // Decrease cooldown timer
        if (flapCooldownTimer > 0f)
        {
            flapCooldownTimer -= Time.fixedDeltaTime;
        }
    }

    private void ApplyRotation()
    {
        // Get input
        float pitchInput = 0f;
        float yawInput = 0f;
        float rollInput = 0f;

        if (Keyboard.current.wKey.isPressed)
            pitchInput = 1f;
        if (Keyboard.current.sKey.isPressed)
            pitchInput = -1f;

        if (Keyboard.current.qKey.isPressed)
            yawInput = -1f;
        if (Keyboard.current.eKey.isPressed)
            yawInput = 1f;

        if (Keyboard.current.aKey.isPressed)
            rollInput = 1f;
        if (Keyboard.current.dKey.isPressed)
            rollInput = -1f;

        // Apply rotations directly in local space - full freedom of movement
        // Pitch (X-axis): Up/down nose rotation
        if (pitchInput != 0f)
        {
            transform.Rotate(pitchInput * pitchSpeed * Time.fixedDeltaTime, 0, 0, Space.Self);
        }

        // Yaw (Y-axis): Left/right nose rotation
        if (yawInput != 0f)
        {
            transform.Rotate(0, yawInput * yawSpeed * Time.fixedDeltaTime, 0, Space.Self);
        }

        // Roll (Z-axis): Barrel roll rotation
        if (rollInput != 0f)
        {
            transform.Rotate(0, 0, rollInput * rollSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }

    private void ApplyThrust()
    {
        if (rb == null)
            return;

        // Update flap state (timers)
        UpdateFlapState();

        // Apply scaled gravity (reduces gravity effect)
        rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);

        if (isCurrentlyFlapping)
        {
            // During flap animation state: Apply drag for flapping state
            // (Actual impulse was applied instantly in StartFlap())
            float dragMultiplier = wingsExpanded ? expandedDragMultiplier : 1f;
            rb.linearDamping = flapDrag * dragMultiplier;
        }
        else if (!isGrounded)
        {
            // === GLIDING PHYSICS (Elytra-style) ===
            
            // Get current velocity and speed
            Vector3 currentVelocity = rb.linearVelocity;
            float currentSpeed = currentVelocity.magnitude;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;
            
            // Calculate turn angle (how much the dragon rotated this frame)
            float turnAngle = Vector3.Angle(previousForward, transform.forward);
            
            // Apply small speed loss based on turning (air resistance from turning)
            if (turnAngle > 0.1f && currentSpeed > 0.1f)
            {
                // Slight speed loss when turning (much less aggressive)
                float speedLossMultiplier = Mathf.Pow(turnDragFactor, turnAngle);
                currentSpeed *= speedLossMultiplier;
            }
            
            // Smoothly redirect velocity toward the forward direction (Elytra-style)
            // This gradually turns the velocity vector instead of snapping it
            if (currentSpeed > 0.1f)
            {
                Vector3 targetVelocity = transform.forward * currentSpeed;
                
                // Smoothly interpolate current velocity toward target direction
                // Higher velocityRedirectSpeed = tighter turns, lower = wider turns
                Vector3 newVelocity = Vector3.Lerp(
                    currentVelocity, 
                    targetVelocity, 
                    velocityRedirectSpeed * Time.fixedDeltaTime
                );
                
                // Preserve the speed magnitude (prevent speed gain/loss from lerp)
                newVelocity = newVelocity.normalized * currentSpeed;
                
                rb.linearVelocity = newVelocity;
            }
            
            // === WING STATE EFFECTS ===
            
            if (wingsExpanded)
            {
                // Expanded wings: Generate lift when moving forward above minimum speed
                if (horizontalSpeed >= minSpeedForLift)
                {
                    // More speed = more lift (quadratic relationship like real wings)
                    float liftAmount = glidingLiftForce * expandedLiftMultiplier * (horizontalSpeed / minSpeedForLift);
                    // Use transform.up so lift follows dragon's orientation (banking turns!)
                    rb.AddForce(transform.up * liftAmount, ForceMode.Force);
                }
                
                // Expanded wings have more drag
                rb.linearDamping = glideDrag * expandedDragMultiplier;
            }
            else
            {
                // Retracted wings: Less lift, less drag (faster but sink faster)
                if (horizontalSpeed >= minSpeedForLift)
                {
                    float liftAmount = glidingLiftForce * retractedLiftMultiplier * (horizontalSpeed / minSpeedForLift);
                    // Use transform.up so lift follows dragon's orientation (banking turns!)
                    rb.AddForce(transform.up * liftAmount, ForceMode.Force);
                }
                
                // Retracted wings have less drag = can go faster
                rb.linearDamping = glideDrag * (1f / retractedSpeedMultiplier);
            }
            
            // Store current state for next frame
            previousVelocity = rb.linearVelocity;
            previousForward = transform.forward;
        }
        else
        {
            // On ground: High drag to stop movement
            rb.linearDamping = groundDrag;
            rb.angularVelocity = Vector3.zero; // Stop spinning on ground
        }
    }

    private void CheckGrounded()
    {
        // Cast ray downward from dragon's position
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
        
        // Visual debug
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    private void UpdateVRPlayerPosition()
    {
        if (vrPlayerTransform == null)
            return;

        // Position VR player relative to the dragon in LOCAL space
        // The player offset is applied in the dragon's local coordinate system
        Vector3 worldOffset = transform.TransformDirection(playerOffset);
        Vector3 targetPosition = transform.position + worldOffset;
        
        vrPlayerTransform.position = targetPosition;

        // Rotate the VR player with the dragon if enabled
        if (rotatePlayerWithDragon)
        {
            // Match the dragon's rotation exactly so the player experiences all the rolls, pitches, and yaws
            vrPlayerTransform.rotation = transform.rotation;
        }
    }
}
