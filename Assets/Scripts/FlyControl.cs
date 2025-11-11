using UnityEngine;
using UnityEngine.InputSystem;

public class FlyControl : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float sprintSpeed = 20f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Camera Follow")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool useMainCamera = true;
    [SerializeField] private string cameraTag = "MainCamera";
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -5);
    [SerializeField] private float cameraFollowSmoothness = 5f;

    private Rigidbody rb;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationInput = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // If no camera is assigned, try to find one in the scene
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
        HandleInput();
    }

    void FixedUpdate()
    {
        ApplyMovement();
        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        // Get input for movement using new Input System
        float vertical = 0f;
        float upDown = 0f;

        // W/S keys for forward/backward
        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;
        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        // A/D keys for rotation
        rotationInput = 0f;
        if (Keyboard.current.aKey.isPressed)
            rotationInput = -1f;
        if (Keyboard.current.dKey.isPressed)
            rotationInput = 1f;

        // E for up, Q for down
        if (Keyboard.current.eKey.isPressed)
            upDown = 1f;
        if (Keyboard.current.qKey.isPressed)
            upDown = -1f;

        // Create movement direction based on world axes (no horizontal/A/D movement)
        moveDirection = new Vector3(0, upDown, vertical).normalized;
    }

    private void ApplyMovement()
    {
        // Determine speed based on shift key using new Input System
        float currentSpeed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed
            ? sprintSpeed 
            : speed;

        // Apply velocity - forward/backward relative to cube's facing direction
        Vector3 moveDirectionRelativeToFacing = transform.TransformDirection(new Vector3(0, moveDirection.y, moveDirection.z));
        
        if (rb != null)
        {
            rb.linearVelocity = moveDirectionRelativeToFacing * currentSpeed;
        }
        else
        {
            transform.position += moveDirectionRelativeToFacing * currentSpeed * Time.fixedDeltaTime;
        }

        // Apply rotation
        if (rotationInput != 0f)
        {
            transform.Rotate(0, rotationInput * rotationSpeed * Time.fixedDeltaTime, 0);
        }
    }

    private void UpdateCameraPosition()
    {
        if (cameraTransform == null)
            return;

        // Rotate the camera offset based on the cube's rotation
        Vector3 rotatedOffset = transform.TransformDirection(cameraOffset);

        // Calculate target position relative to the cube
        Vector3 targetPosition = transform.position + rotatedOffset;

        // Smoothly move the camera to the target position
        cameraTransform.position = Vector3.Lerp(
            cameraTransform.position,
            targetPosition,
            cameraFollowSmoothness * Time.deltaTime
        );

        // Make camera look at the cube
        cameraTransform.LookAt(transform.position + Vector3.up * 0.5f);
    }
}
