using UnityEngine;
using UnityEngine.VFX;

public class CameraToVFX : MonoBehaviour
{
    public VisualEffect vfx;
    public Transform cam;
    public float xRotationOffset = 0f;

    void Update()
    {
        Vector3 forward = Quaternion.AngleAxis(xRotationOffset, cam.right) * cam.up;
        vfx.SetVector3("CameraForward", forward);
        vfx.SetVector3("CameraPosition", cam.position);
    }
}

