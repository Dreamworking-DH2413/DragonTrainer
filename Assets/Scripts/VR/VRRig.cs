using UnityEngine;

public class VRRig : MonoBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    public Transform tracker1;
    public Transform tracker2;

    public Transform dragon;
    public Transform leftWingRoot;
    public Transform rightWingRoot;

    public Transform leftOuterArm;
    public Transform rightOuterArm;

    public float wingSpanScale = 1.0f;
    public float followSpeed = 20f;

    private Vector3 startLeftOuterWingLocal;
    private Vector3 startRightOuterWingLocal;
    private Vector3 startLeftTrackerPosition;
    private Vector3 startRightTrackerPosition;

    void Start()
    {
        Vector3 dragonForward = dragon.forward;
        Vector3 dragonRight = dragon.right;
        Vector3 dragonUp = dragon.up;

        Vector3 toLeftWing = leftWingRoot.position - leftWingRoot.position;
        Vector3 toRightWing = rightWingRoot.position - rightWingRoot.position;

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
            startLeftTrackerPosition = tracker1.position;
        }

        if (tracker2)
        {
            startRightTrackerPosition = tracker2.position;
        }
    }

    void Update()
    {
        if (tracker1 && leftOuterArm)
        {
            leftOuterArm.position = tracker1.position * wingSpanScale;
        }

        if (tracker2 && rightOuterArm && rightWingRoot && dragon)
        {
            Vector3 tracker2Relative = (tracker2.position - startRightTrackerPosition) * wingSpanScale;

            Vector3 dragonToWing = dragon.right * tracker2Relative.x +
                dragon.up * tracker2Relative.y + 
                dragon.forward * tracker2Relative.z;

            rightOuterArm.position = rightWingRoot.position +
                dragon.right * startRightOuterWingLocal.x + 
                dragon.up * startRightOuterWingLocal.y +
                dragon.forward * startRightOuterWingLocal.z +
                dragonToWing;
        }
    }
}

