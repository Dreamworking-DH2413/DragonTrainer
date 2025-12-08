using UnityEngine;

public class ToothlessHeadController : MonoBehaviour
{

    public GameObject VRCamera;
    public GameObject aimTarget;
    
    [Header("Rotation Limits")]
    public float maxVerticalAngle = 60f;  // Up/down limit
    public float maxHorizontalAngle = 80f; // Left/right limit
    
    [Header("Smoothing")]
    public float smoothSpeed = 5f; // Higher = faster response
    
    private Vector3 currentTargetPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (aimTarget != null)
        {
            currentTargetPosition = aimTarget.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (VRCamera != null && aimTarget != null)
        {
            Transform parent = aimTarget.transform.parent;
            
            if (parent != null)
            {
                // Calculate the offset direction (10 meters from parent)
                float offsetDistance = aimTarget.transform.localPosition.magnitude;
                
                // Get the camera's local rotation relative to parent
                Quaternion localCameraRotation = Quaternion.Inverse(parent.rotation) * VRCamera.transform.rotation;
                Vector3 cameraEuler = localCameraRotation.eulerAngles;
                
                // Normalize angles to -180 to 180 range
                float pitch = cameraEuler.x > 180 ? cameraEuler.x - 360 : cameraEuler.x;
                float yaw = cameraEuler.y > 180 ? cameraEuler.y - 360 : cameraEuler.y;
                
                // Clamp the angles
                pitch = Mathf.Clamp(pitch, -maxVerticalAngle, maxVerticalAngle);
                yaw = Mathf.Clamp(yaw, -maxHorizontalAngle, maxHorizontalAngle);
                
                // Create clamped rotation
                Quaternion clampedLocalRotation = Quaternion.Euler(pitch, yaw, 0);
                Vector3 worldDirection = parent.rotation * (clampedLocalRotation * Vector3.forward);
                
                // Calculate target position
                Vector3 targetPosition = parent.position + worldDirection * offsetDistance;
                
                // Smoothly interpolate to the target position
                currentTargetPosition = Vector3.Lerp(currentTargetPosition, targetPosition, smoothSpeed * Time.deltaTime);
                aimTarget.transform.position = currentTargetPosition;
            }
        }
    }
}
