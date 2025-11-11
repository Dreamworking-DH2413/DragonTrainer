using UnityEngine;
using UnityEngine.InputSystem;

public class FlyControl : MonoBehaviour
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
    
    [Header("Rotation (Pitch, Yaw, Roll)")]
    [SerializeField] private float pitchSpeed = 60f;          // Up/down rotation (W/S)
    [SerializeField] private float yawSpeed = 60f;            // Left/right rotation (A/D)
    [SerializeField] private float rollSpeed = 90f;           // Roll rotation (Q/E)
    [SerializeField] private float maxPitchAngle = 120f;       // Limit pitch to prevent over-rotation
    
    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Camera Follow")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool useMainCamera = true;
    [SerializeField] private string cameraTag = "MainCamera";
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1, -3);

    private Rigidbody rb;
    private bool isGrounded = false;
    private float currentPitch = 0f;
    
    // Flapping state tracking
    private float flapTimer = 0f;           // Time remaining in current flap
    private float flapCooldownTimer = 0f;   // Time until next flap is allowed
    private bool isCurrentlyFlapping = false;
    private bool flapRequested = false;     // Track if flap was requested this frame

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find camera if not assigned
        if (cameraTransform == null)
        {
            if (useMainCamera)
            {
                // Use Camera.main (the camera tagged "MainCamera")
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    cameraTransform = mainCamera.transform;
            }
            else
            {
                // Find camera by tag
                GameObject cameraObj = GameObject.FindWithTag(cameraTag);
                if (cameraObj != null)
                    cameraTransform = cameraObj.transform;
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
    }

    void FixedUpdate()
    {
        ApplyRotation();
        HandleFlapInput();  // Check flap request AFTER rotation
        ApplyThrust();
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
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

        if (Keyboard.current.aKey.isPressed)
            yawInput = -1f;
        if (Keyboard.current.dKey.isPressed)
            yawInput = 1f;

        if (Keyboard.current.qKey.isPressed)
            rollInput = 1f;
        if (Keyboard.current.eKey.isPressed)
            rollInput = -1f;

        // Apply pitch with clamping
        if (pitchInput != 0f)
        {
            float newPitch = currentPitch + pitchInput * pitchSpeed * Time.fixedDeltaTime;
            newPitch = Mathf.Clamp(newPitch, -maxPitchAngle, maxPitchAngle);
            transform.Rotate(newPitch - currentPitch, 0, 0, Space.Self);
            currentPitch = newPitch;
        }

        // Apply yaw (left/right)
        if (yawInput != 0f)
        {
            transform.Rotate(0, yawInput * yawSpeed * Time.fixedDeltaTime, 0, Space.Self);
        }

        // Apply roll (barrel roll)
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
            rb.linearDamping = flapDrag;
        }
        else if (!isGrounded)
        {
            // Gliding: Redirect velocity to follow current forward direction
            // Get current horizontal speed (ignoring vertical component)
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;
            
            // If gliding and moving, redirect velocity to face forward direction
            if (currentSpeed > 0.1f)
            {
                Vector3 newHorizontalVelocity = transform.forward * currentSpeed;
                rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);
            }
            
            // Apply lower drag for longer glide
            rb.linearDamping = glideDrag;
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

    private void UpdateCameraPosition()
    {
        if (cameraTransform == null)
            return;

        // Position camera relative to the dragon in LOCAL space
        // The camera offset is applied in the dragon's local coordinate system
        Vector3 localOffset = cameraOffset;
        Vector3 worldOffset = transform.TransformDirection(localOffset);
        Vector3 targetPosition = transform.position + worldOffset;
        
        cameraTransform.position = targetPosition;

        // Make camera look at a point ahead of the dragon (in dragon's forward direction)
        Vector3 lookAtPoint = transform.position + transform.forward * 5f;
        cameraTransform.LookAt(lookAtPoint);
        
        // Apply the dragon's roll to the camera by rotating around the forward axis
        Vector3 forward = (lookAtPoint - targetPosition).normalized;
        cameraTransform.rotation = Quaternion.LookRotation(forward, transform.up);
    }
}
