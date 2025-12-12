using System;
using UnityEngine;
using Unity.Netcode;

public class VRRig : NetworkBehaviour
{
    [Header("VR Transforms")]
    public Transform head;
    public Transform player;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Tracker Transforms")]
    public Transform tracker1;
    public Transform tracker2;

    [Header("Dragon Transforms")]
    public Transform dragon;
    public Transform targetsParent;
    public Transform leftWingTarget;
    public Transform rightWingTarget;

    [Header("VFX")]
    public GameObject vfxDragonBreath;

    [Header("Movement Multipliers")]
    public float wingSpanCompensation = 5.0f;
    public float headToBodyCompensation = 3.0f;
    public float wingMovementMultiplier = 3.0f;
    public float followSpeed = 20f;
    public float trackerYMultiplier = 25f;
    public float trackerYOffset = -1.5f;
    
    [Header("Flap Detection")]
    [Tooltip("Minimum downward velocity (in local space) to trigger a flap")]
    public float flapVelocityThreshold = 1.5f;
    
    [Tooltip("Enable debug logging for flap detection")]
    public bool debugFlapDetection = false;

    [Header("Target Position Constraints")]
    public float minXOutward = -8f;  // How far out each wing can go (negative for left, positive for right)
    public float maxXInward = -0.5f; // How close to centerline (prevents crossing body)
    
    [Header("Y Bounds (interpolated by X position)")]
    public Vector2 yBoundsAtOutward = new Vector2(-2f, 3f);  // Y bounds when wing is fully extended (at minXOutward)
    public Vector2 yBoundsAtInward = new Vector2(-1f, 2f);   // Y bounds when wing is close to body (at maxXInward)
    
    [Header("Z Bounds (interpolated by X position)")]
    public Vector2 zBoundsAtOutward = new Vector2(-5f, 2f);  // Z bounds when wing is fully extended (at minXOutward)
    public Vector2 zBoundsAtInward = new Vector2(-3f, 1f);   // Z bounds when wing is close to body (at maxXInward)

    [Header("Internal State")]
    private Vector3 startLeftOuterWingLocal;
    private Vector3 startRightOuterWingLocal;
    private Vector3 startLeftTrackerLocalPosition;
    private Vector3 startRightTrackerLocalPosition;
    
    [Header("Smoothing")]
    private Vector3 smoothedLeftTargetLocalPos;
    private Vector3 smoothedRightTargetLocalPos;
    private bool isInitialized = false;
    private bool vfxInitialized = false;
    private bool velocityInitialized = false;

    void Start()
    {
        Vector3 dragonForward = dragon.forward;
        Vector3 dragonRight = dragon.right;
        Vector3 dragonUp = dragon.up;

        Vector3 toLeftWing = leftWingTarget.position - leftWingTarget.position;
        Vector3 toRightWing = rightWingTarget.position - rightWingTarget.position;

        // Project each offset onto the dragon's local axes
        startLeftOuterWingLocal = new Vector3(
            Vector3.Dot(toLeftWing, dragonRight),
            Vector3.Dot(toLeftWing, dragonUp),
            Vector3.Dot(toLeftWing, dragonForward)
        );

        startRightOuterWingLocal = new Vector3(
            Vector3.Dot(toRightWing, dragonRight),
            Vector3.Dot(toRightWing, dragonUp),
            Vector3.Dot(toRightWing, dragonForward)
        );

        if (tracker1)
        {
            startLeftTrackerLocalPosition = transform.InverseTransformPoint(tracker1.position);
        }

        if (tracker2)
        {
            startRightTrackerLocalPosition = transform.InverseTransformPoint(tracker2.position);
        }
    }

    void Update()
    {
        // Disable VFX dragon breath for clients once network is ready
        if (!vfxInitialized && vfxDragonBreath != null)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                // In network mode: only host has VFX enabled
                vfxDragonBreath.SetActive(IsHost);
                vfxInitialized = true;
            }
            else
            {
                // In single-player mode: keep VFX enabled (default state)
                vfxInitialized = true;
            }
        }

        // Only host controls the trackers and targets
        // In network mode: only run on host
        // In single-player: always run
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (!IsHost)
            {
                return; // Clients don't run this script
            }
        }
        
        if (tracker1 && leftWingTarget && targetsParent)
        {
            Vector3 trackerLocalPos = transform.InverseTransformPoint(tracker1.position);
            
            // Calculate desired position in local space with multiplied x-axis movement
            Vector3 targetLocalPos = new Vector3(
                (trackerLocalPos.x * wingMovementMultiplier) - wingSpanCompensation,
                trackerLocalPos.y * trackerYMultiplier + trackerYOffset,
                trackerLocalPos.z - headToBodyCompensation
            );
            
            // Clamp X first
            float clampedX = Mathf.Clamp(targetLocalPos.x, minXOutward, maxXInward);
            
            // Interpolate Y and Z bounds based on X position (0 = outward, 1 = inward)
            float xNormalized = Mathf.InverseLerp(minXOutward, maxXInward, clampedX);
            
            float yMin = Mathf.Lerp(yBoundsAtOutward.x, yBoundsAtInward.x, xNormalized);
            float yMax = Mathf.Lerp(yBoundsAtOutward.y, yBoundsAtInward.y, xNormalized);
            float clampedY = Mathf.Clamp(targetLocalPos.y, yMin, yMax);
            
            float zMin = Mathf.Lerp(zBoundsAtOutward.x, zBoundsAtInward.x, xNormalized);
            float zMax = Mathf.Lerp(zBoundsAtOutward.y, zBoundsAtInward.y, xNormalized);
            float clampedZ = Mathf.Clamp(targetLocalPos.z, zMin, zMax);
            
            targetLocalPos = new Vector3(clampedX, clampedY, clampedZ);
            
            // Initialize smoothed position on first frame
            if (!isInitialized)
            {
                smoothedLeftTargetLocalPos = targetLocalPos;
            }
            
            // Smoothly interpolate in LOCAL space to reduce choppiness
            smoothedLeftTargetLocalPos = Vector3.Lerp(smoothedLeftTargetLocalPos, targetLocalPos, Time.deltaTime * followSpeed);
            
            // Set the target's local position relative to targetsParent
            leftWingTarget.localPosition = smoothedLeftTargetLocalPos;
        }

        if (tracker2 && rightWingTarget && targetsParent)
        {
            Vector3 trackerLocalPos = transform.InverseTransformPoint(tracker2.position);
            
            // Calculate desired position in local space with multiplied x-axis movement
            Vector3 targetLocalPos = new Vector3(
                (trackerLocalPos.x * wingMovementMultiplier) + wingSpanCompensation,
                trackerLocalPos.y * trackerYMultiplier + trackerYOffset,
                trackerLocalPos.z - headToBodyCompensation
            );
            
            // Clamp X first
            float clampedX = Mathf.Clamp(targetLocalPos.x, -maxXInward, -minXOutward);
            
            // Interpolate Y and Z bounds based on X position (0 = outward, 1 = inward)
            float xNormalized = Mathf.InverseLerp(-minXOutward, -maxXInward, clampedX);
            
            float yMin = Mathf.Lerp(yBoundsAtOutward.x, yBoundsAtInward.x, xNormalized);
            float yMax = Mathf.Lerp(yBoundsAtOutward.y, yBoundsAtInward.y, xNormalized);
            float clampedY = Mathf.Clamp(targetLocalPos.y, yMin, yMax);
            
            float zMin = Mathf.Lerp(zBoundsAtOutward.x, zBoundsAtInward.x, xNormalized);
            float zMax = Mathf.Lerp(zBoundsAtOutward.y, zBoundsAtInward.y, xNormalized);
            float clampedZ = Mathf.Clamp(targetLocalPos.z, zMin, zMax);
            
            targetLocalPos = new Vector3(clampedX, clampedY, clampedZ);
            
            // Initialize smoothed position on first frame
            if (!isInitialized)
            {
                smoothedRightTargetLocalPos = targetLocalPos;
                isInitialized = true;
            }
            
            // Smoothly interpolate in LOCAL space to reduce choppiness
            smoothedRightTargetLocalPos = Vector3.Lerp(smoothedRightTargetLocalPos, targetLocalPos, Time.deltaTime * followSpeed);
            
            // Set the target's local position relative to targetsParent
            rightWingTarget.localPosition = smoothedRightTargetLocalPos;
        }
    }

    private void FixedUpdate()
    {
        // Only host controls the trackers and thrust
        // In network mode: only run on host
        // In single-player: always run
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // Debug.Log("[VRRig] Networked mode detected.");
            if (!IsHost)
            {
                // Debug.Log("[VRRig] Not host, skipping FixedUpdate.");
                return; // Clients don't run this script
            }
        }
        
        // calculate the velocity of the trackers relative to this VRRig object
        if (tracker1 && tracker2)
        {
            // Convert current positions to VRRig's local space
            Vector3 leftTrackerLocalPos = transform.InverseTransformPoint(tracker1.position);
            Vector3 rightTrackerLocalPos = transform.InverseTransformPoint(tracker2.position);
            
            // On first frame, just initialize positions without calculating velocity
            if (!velocityInitialized)
            {
                startLeftTrackerLocalPosition = leftTrackerLocalPos;
                startRightTrackerLocalPosition = rightTrackerLocalPos;
                velocityInitialized = true;
                return; // Skip velocity calculation on first frame
            }
            
            Vector3 leftTrackerVelocity = (leftTrackerLocalPos - startLeftTrackerLocalPosition) / Time.fixedDeltaTime;
            startLeftTrackerLocalPosition = leftTrackerLocalPos;
            
            Vector3 rightTrackerVelocity = (rightTrackerLocalPos - startRightTrackerLocalPosition) / Time.fixedDeltaTime;
            startRightTrackerLocalPosition = rightTrackerLocalPos;
            
            // Debug logging if enabled
            if (debugFlapDetection)
            {
                Debug.Log($"[VRRig] Tracker Velocities | Left Y: {leftTrackerVelocity.y:F3} | Right Y: {rightTrackerVelocity.y:F3} | Threshold: -{flapVelocityThreshold:F3}");
            }
            
            // Check if downward velocity exceeds threshold (negative Y = downward in local space)
            if (leftTrackerVelocity.y < -flapVelocityThreshold || rightTrackerVelocity.y < -flapVelocityThreshold)
            {
                // avg velocity of both trackers
                float avgVelocityY = (leftTrackerVelocity.y + rightTrackerVelocity.y) / 2.0f;
                
                if (debugFlapDetection)
                {
                    Debug.Log($"[VRRig] FLAP DETECTED! Avg Velocity Y: {avgVelocityY:F3}");
                }
            
                // Call function to trigger wing flap thrust
                dragon.GetComponent<DragonGliderPhysics>().SetThrustByVelocity(avgVelocityY);
            }
        }
    }
}

