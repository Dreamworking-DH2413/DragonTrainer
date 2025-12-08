using System;
using UnityEngine;
using Unity.Netcode;

public class VRRig : NetworkBehaviour
{
    public Transform head;
    public Transform player;
    public Transform leftHand;
    public Transform rightHand;

    public Transform tracker1;
    public Transform tracker2;

    public Transform dragon;
    public Transform targetsParent;
    public Transform leftWingTarget;
    public Transform rightWingTarget;

    public Transform leftOuterArm;
    public Transform rightOuterArm;

    public float wingSpanCompensation = 5.0f;
    public float headToBodyCompensation = 3.0f;
    public float wingMovementMultiplier = 3.0f;
    public float followSpeed = 20f;

    [Header("Target Position Constraints")]
    public float minXOutward = -8f;  // How far out each wing can go (negative for left, positive for right)
    public float maxXInward = -0.5f; // How close to centerline (prevents crossing body)
    
    [Header("Y Bounds (interpolated by X position)")]
    public Vector2 yBoundsAtOutward = new Vector2(-2f, 3f);  // Y bounds when wing is fully extended (at minXOutward)
    public Vector2 yBoundsAtInward = new Vector2(-1f, 2f);   // Y bounds when wing is close to body (at maxXInward)
    
    [Header("Z Bounds (interpolated by X position)")]
    public Vector2 zBoundsAtOutward = new Vector2(-5f, 2f);  // Z bounds when wing is fully extended (at minXOutward)
    public Vector2 zBoundsAtInward = new Vector2(-3f, 1f);   // Z bounds when wing is close to body (at maxXInward)

    private Vector3 startLeftOuterWingLocal;
    private Vector3 startRightOuterWingLocal;
    private Vector3 startLeftTrackerLocalPosition;
    private Vector3 startRightTrackerLocalPosition;
    
    // Smoothed target positions in LOCAL space to reduce choppiness
    private Vector3 smoothedLeftTargetLocalPos;
    private Vector3 smoothedRightTargetLocalPos;
    private bool isInitialized = false;

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
                trackerLocalPos.y * 10 + 0.25f,
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
                trackerLocalPos.y * 10 + 0.25f,
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
            Debug.Log("[VRRig] Networked mode detected.");
            if (!IsHost)
            {
                Debug.Log("[VRRig] Not host, skipping FixedUpdate.");
                return; // Clients don't run this script
            }
        }
        
        // calculate the velocity of the trackers relative to this VRRig object
        if (tracker1 && tracker2)
        {
            // Convert current positions to VRRig's local space
            Vector3 leftTrackerLocalPos = transform.InverseTransformPoint(tracker1.position);
            Vector3 leftTrackerVelocity = (leftTrackerLocalPos - startLeftTrackerLocalPosition) / Time.fixedDeltaTime;
            startLeftTrackerLocalPosition = leftTrackerLocalPos;
            
            Vector3 rightTrackerLocalPos = transform.InverseTransformPoint(tracker2.position);
            Vector3 rightTrackerVelocity = (rightTrackerLocalPos - startRightTrackerLocalPosition) / Time.fixedDeltaTime;
            startRightTrackerLocalPosition = rightTrackerLocalPos;
            
            // if the Y velocity is greater than a threshold, we call a function to trigger the wing flap and move the dragon up
            if (leftTrackerVelocity.y < -0.0f || rightTrackerVelocity.y < -0.0f)
            {
                // Debug.Log("[VRRig] Left tracker velocity: " + leftTrackerVelocity.ToString("F3"));
                // Debug.Log("[VRRig] Tracker flap detected.");
                // avg velocity of both trackers
                float avgVelocityY = (leftTrackerVelocity.y + rightTrackerVelocity.y) / 2.0f;
            
                // Call function to trigger left wing flap
                dragon.GetComponent<DragonGliderPhysics>().SetThrustByVelocity(avgVelocityY);
            }
        }
    }
}

