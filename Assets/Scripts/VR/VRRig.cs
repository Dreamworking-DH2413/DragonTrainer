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

        // same idea for trackers
    }
}
