using UnityEngine;

public class VRRig : MonoBehaviour
{
    [Header("Main HMD and controllers")]
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Optional trackers")]
    public Transform tracker1;
    public Transform tracker2;

    [Header("Bones")]
    public float wingSpanScale = 8.0f;

    public Transform leftOuterArm;
    public Transform rightOuterArm;

    void Update()
    {
        if (head != null)
        {
            Vector3 hmdPos = head.position;
            Quaternion hmdRot = head.rotation;
            // Debug.Log($"HMD: {hmdPos}");
        }

        if (leftHand != null)
        {
            Vector3 leftPos = leftHand.position;
            Quaternion leftRot = leftHand.rotation;
        }

        if (rightHand != null)
        {
            Vector3 rightPos = rightHand.position;
            Quaternion rightRot = rightHand.rotation;
        }

        if (tracker1 != null)
        {
            Vector3 tracker1Pos = tracker1.position;
            Quaternion tracker1Rot = tracker1.rotation;

            if (leftOuterArm != null)
            {
                // leftOuterArm.position = tracker1Pos * wingSpanScale;
                //leftOuterArm.rotation = tracker1Rot;
            }
        }

        if (tracker2 != null)
        {
            Vector3 tracker2Pos = tracker2.position;
            Quaternion tracker2Rot = tracker2.rotation;

            if (rightOuterArm != null)
            {
                // rightOuterArm.position = tracker2Pos * wingSpanScale;
                //rightOuterArm.rotation = tracker2Rot;
            }
        }
    }
}
