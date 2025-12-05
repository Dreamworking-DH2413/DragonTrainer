using System;
using UnityEngine;

public class VRRig : MonoBehaviour
{
    public Transform head;
    public Transform player;
    public Transform leftHand;
    public Transform rightHand;

    public Transform tracker1;
    public Transform tracker2;

    public Transform dragon;
    public Transform leftWingTarget;
    public Transform rightWingTarget;

    public Transform leftOuterArm;
    public Transform rightOuterArm;

    public float wingSpanCompensation = 5.0f;
    public float headToBodyCompensation = 3.0f;
    public float wingMovementMultiplier = 3.0f;
    public float followSpeed = 20f;

    private Vector3 startLeftOuterWingLocal;
    private Vector3 startRightOuterWingLocal;
    private Vector3 startLeftTrackerLocalPosition;
    private Vector3 startRightTrackerLocalPosition;

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
        if (tracker1 && leftWingTarget && dragon)
        {
            // Convert tracker world position to dragon's local space
            Vector3 trackerLocalPos = dragon.InverseTransformPoint(tracker1.position);
            
            // Apply offsets in local space with multiplied x-axis movement
            Vector3 targetLocalPos = new Vector3(
                (trackerLocalPos.x * wingMovementMultiplier) - wingSpanCompensation,
                trackerLocalPos.y * 10,
                trackerLocalPos.z - headToBodyCompensation
            );
            
            // Convert back to world space and apply
            leftWingTarget.position = dragon.TransformPoint(targetLocalPos);
        }

        if (tracker2 && rightWingTarget && dragon)
        {
            // Convert tracker world position to dragon's local space
            Vector3 trackerLocalPos = dragon.InverseTransformPoint(tracker2.position);
            
            // Apply offsets in local space with multiplied x-axis movement
            Vector3 targetLocalPos = new Vector3(
                (trackerLocalPos.x * wingMovementMultiplier) + wingSpanCompensation,
                trackerLocalPos.y * 10,
                trackerLocalPos.z - headToBodyCompensation
            );
            
            // Convert back to world space and apply
            rightWingTarget.position = dragon.TransformPoint(targetLocalPos);
        }
    }

    private void FixedUpdate()
    {
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

