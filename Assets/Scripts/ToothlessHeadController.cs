using UnityEngine;
using Unity.Netcode;

public class ToothlessHeadController : MonoBehaviour
{

    public GameObject VRCamera;
    public GameObject aimTarget;
    
    [Header("Rotation Limits")]
    public float maxVerticalAngle = 60f;  // Up/down limit
    public float maxHorizontalAngle = 80f; // Left/right limit
    
    [Header("Smoothing")]
    public float smoothSpeed = 5f; // Higher = faster response
    
    private float initialOffsetDistance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (aimTarget != null)
        {
            // Store the initial offset distance and never recalculate it
            initialOffsetDistance = aimTarget.transform.localPosition.magnitude;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only the host should control the head
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }
        
        if (VRCamera != null && aimTarget != null)
        {
            Transform parent = aimTarget.transform.parent;
            
            if (parent != null)
            {
                // Get camera's parent (VR rig)
                Transform cameraParent = VRCamera.transform.parent;
                
                if (cameraParent != null)
                {
                    // Get the camera's local rotation relative to its own parent (VR rig)
                    Vector3 cameraLocalEuler = VRCamera.transform.localEulerAngles;
                    
                    // Normalize angles to -180 to 180 range
                    float pitch = cameraLocalEuler.x > 180 ? cameraLocalEuler.x - 360 : cameraLocalEuler.x;
                    float yaw = cameraLocalEuler.y > 180 ? cameraLocalEuler.y - 360 : cameraLocalEuler.y;
                    
                    // Clamp the angles
                    pitch = Mathf.Clamp(pitch, -maxVerticalAngle, maxVerticalAngle);
                    yaw = Mathf.Clamp(yaw, -maxHorizontalAngle, maxHorizontalAngle);
                    
                    // Create clamped local rotation
                    Quaternion clampedLocalRotation = Quaternion.Euler(pitch, yaw, 0);
                    
                    // Calculate local direction vector from clamped rotation
                    Vector3 localDirection = clampedLocalRotation * Vector3.forward;
                    
                    // Set the aimTarget's local position directly using the fixed initial distance
                    Vector3 targetLocalPosition = localDirection * initialOffsetDistance;
                    
                    // Smoothly interpolate in local space
                    aimTarget.transform.localPosition = Vector3.Lerp(
                        aimTarget.transform.localPosition, 
                        targetLocalPosition, 
                        smoothSpeed * Time.deltaTime
                    );
                }
            }
        }
    }
}
