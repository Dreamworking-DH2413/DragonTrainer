using UnityEngine;
using UnityEngine.VFX;

public class CameraToVFX : MonoBehaviour
{
    public VisualEffect vfx;
    public Camera cam;

    void Update()
    {
        vfx.SetVector3("CameraForward", cam.transform.forward);
        vfx.SetVector3("CameraPosition", cam.transform.position);
    }
}

