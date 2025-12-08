using UnityEngine;

public class ThirdPersonSpectatorCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // The player root or HMD rig
    public Transform player;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(1f, 1f, -3f);  // Behind and up
    public float heightOffset = 1.5f;                  // offset from model centre
    public float angleOffset = 17f;
    [Header("Zoom")]
    public float zoomSpeed = 2f;
    public float minZoom = 0.1f;
    public float maxZoom = 10f;
    private float currentZoom = 1f;

    [Header("Preset Camera Angles")]
    public Vector3 presetBehindClose = new Vector3(30f, 0f, 3f);         // 1 - behind (0° relative, 15° up)
    public Vector3 presetBehindFar = new Vector3(15f, 0f, 6f);         // 3 - below (0° relative, 70° down
    public Vector3 presetAboveClose = new Vector3(85f, 0f, 5f);          // 2 - above (0° relative, 80° up
    public Vector3 presetAboveFar = new Vector3(90f, 0f, 10f);          // 2 - above (0° relative, 80° up
    public Vector3 presetFrontClose = new Vector3(0f, 180f,0.9f);        // 5 - front (180° relative, 15° up)
    public Vector3 presetFrontSide = new Vector3(0f, 160f,2f);        // 5 - front (180° relative, 15° up)


    [Header("Orbit")]
    public bool autoOrbit = false;
    public float horizontalOrbitSpeed = 30f;   // degrees / second
    public float verticalAngleSpeed = 0f;      // Set to > 0 for auto vertical rotation

    public float manualOrbitSpeed = 110f;

    [Header("Smoothing")]
    public float lookSmooth = 8f;

    
    private float currentAngleY = 0f;  // For hori rotation around player pos
    private float currentAngleX = 0f;  // For vertical rotation around player pos

    void Start()
    {
        if (target != null)
        {
            // Initialize angle based on starting position
            Vector3 toCam = transform.position - target.position;
            currentAngleY = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
            currentAngleX = Mathf.Asin(toCam.y / toCam.magnitude) * Mathf.Rad2Deg;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- Check for preset camera positions (keyboard hotkeys) ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetCameraPosition(presetBehindClose);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetCameraPosition(presetBehindFar);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetCameraPosition(presetAboveClose);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SetCameraPosition(presetAboveFar);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            SetCameraPosition(presetFrontClose);
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            SetCameraPosition(presetFrontSide);

        // --- Zoom control with mouse scroll ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }

        // --- Orbit logic ---
        if (autoOrbit)
        {
            currentAngleY += horizontalOrbitSpeed * Time.deltaTime;
            currentAngleX += verticalAngleSpeed * Time.deltaTime;
        }
        else if (Input.GetMouseButton(1))  // Right mouse button held down
        {
            // Let audience operator rotate with mouse or keys
            float inputX = Input.GetAxis("Horizontal");     // A/D or Left/Right
            float mouseX = Input.GetAxis("Mouse X");        // mouse drag
            float mouseY = Input.GetAxis("Mouse Y");        // mouse up/down for vertical
            currentAngleY += (inputX + mouseX) * manualOrbitSpeed * Time.deltaTime;
            currentAngleX += mouseY * manualOrbitSpeed * Time.deltaTime;
        }

        // Clamp vertical rotation to prevent gimbal lock
        currentAngleX = Mathf.Clamp(currentAngleX, -89f, 89f);

        // --- Calculate desired camera position ---
        Quaternion rot = Quaternion.Euler(-currentAngleX, currentAngleY, 0f);
        Vector3 desiredPos = target.position + rot * (offset * currentZoom);
        transform.position = desiredPos;

        // --- Smooth look at target (from cams new pos) ---
        Vector3 lookPoint = target.position + Vector3.up * heightOffset;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-lookSmooth * Time.deltaTime));
    }

    private void SetCameraPosition(Vector3 presetVec)
    {
        // Apply target's Y rotation (heading) to make presets relative to dragon's facing direction
        float targetHeading = target != null ? target.eulerAngles.y : 0f;
        currentAngleY = presetVec.x  + targetHeading+angleOffset;
        currentAngleX = -presetVec.y;
        currentZoom = presetVec.z;  // corr zoom for this view
    }
}