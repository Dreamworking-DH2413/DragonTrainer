using UnityEngine;

public class ThirdPersonSpectatorCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Transform player;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(1f, 1f, -3f);
    public float heightOffset = 1.5f;
    public float angleOffset = 17f;
    
    [Header("Zoom")]
    public float zoomSpeed = 2f;
    public float minZoom = 0.1f;
    public float maxZoom = 10f;
    private float currentZoom = 1f;

    [Header("Preset Camera Angles")]
    public Vector3 presetBehindClose = new Vector3(30f, 0f, 3f);
    public Vector3 presetBehindFar = new Vector3(15f, 0f, 6f);
    public Vector3 presetAboveClose = new Vector3(85f, 0f, 5f);
    public Vector3 presetAboveFar = new Vector3(90f, 0f, 10f);
    public Vector3 presetFrontClose = new Vector3(0f, 180f, 0.9f);
    public Vector3 presetFrontSide = new Vector3(0f, 160f, 2f);

    [Header("Orbit")]
    public bool autoOrbit = false;
    public float horizontalOrbitSpeed = 30f;
    public float verticalAngleSpeed = 0f;
    public float manualOrbitSpeed = 110f;

    [Header("Smoothing")]
    public float lookSmooth = 8f;

    [Header("Sheep Focus Mode")]
    public Vector3 sheepCameraOffset = new Vector3(0f, 1.5f, -2f);
    //public float sheepLookSmooth = 5f;

    private bool sheepFocusMode = false;
    private Transform currentSheep = null;
    private Transform currentHerd = null;
    //private float lastSheepSearchTime = -999f;
    private float sheepSearchCooldown = 0.5f;

    private float currentAngleY = 0f;
    private float currentAngleX = 0f;

    void Start()
    {
        if (target != null)
        {
            Vector3 toCam = transform.position - target.position;
            currentAngleY = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
            currentAngleX = Mathf.Asin(toCam.y / toCam.magnitude) * Mathf.Rad2Deg;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Sheep focus toggle (button 0)
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            sheepFocusMode = !sheepFocusMode;
            if (sheepFocusMode)
                FindAndFocusSheep();
        }

        if (sheepFocusMode && currentSheep != null)
        {
            HandleSheepFocusCamera();
            return;
        }

        HandleDragonFocusCamera();
    }

    private void HandleDragonFocusCamera()
    {
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

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }

        if (autoOrbit)
        {
            currentAngleY += horizontalOrbitSpeed * Time.deltaTime;
            currentAngleX += verticalAngleSpeed * Time.deltaTime;
        }
        else if (Input.GetMouseButton(1))
        {
            float inputX = Input.GetAxis("Horizontal");
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            currentAngleY += (inputX + mouseX) * manualOrbitSpeed * Time.deltaTime;
            currentAngleX += mouseY * manualOrbitSpeed * Time.deltaTime;
        }

        currentAngleX = Mathf.Clamp(currentAngleX, -89f, 89f);

        Quaternion rot = Quaternion.Euler(-currentAngleX, currentAngleY, 0f);
        Vector3 desiredPos = target.position + rot * (offset * currentZoom);
        transform.position = desiredPos;

        Vector3 lookPoint = target.position + Vector3.up * heightOffset;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-lookSmooth * Time.deltaTime));
        
        
    
    }

    private void HandleSheepFocusCamera()
    {
        if (currentSheep == null)
        {
            sheepFocusMode = false;
            return;
        }

        //if (Time.time - lastSheepSearchTime > sheepSearchCooldown)
        //    FindAndFocusSheep();

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }

        if (Input.GetMouseButton(1))
        {
            float inputX = Input.GetAxis("Horizontal");
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            currentAngleY += (inputX + mouseX) * manualOrbitSpeed * Time.deltaTime;
            currentAngleX += mouseY * manualOrbitSpeed * Time.deltaTime;
        }

        currentAngleX = Mathf.Clamp(currentAngleX, -89f, 89f);

        Vector3 sheepPos = currentSheep.position;
        Quaternion rot = Quaternion.Euler(-currentAngleX, currentAngleY, 0f);
        Vector3 desiredSheepPos = sheepPos + rot * (sheepCameraOffset * currentZoom);
        transform.position = desiredSheepPos;

        Vector3 dragonLookPoint = target.position + Vector3.up * heightOffset;
        Quaternion desiredRot = Quaternion.LookRotation(dragonLookPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-lookSmooth * Time.deltaTime));
    }
    private void FindAndFocusSheep()
    {
        //lastSheepSearchTime = Time.time;

        Transform closestHerd = FindClosestHerd();
        if (closestHerd == null)
        {
            Debug.LogWarning("No herds found!");
            sheepFocusMode = false;
            return;
        }

        currentHerd = closestHerd;
        currentSheep = GetRandomSheepInHerd(closestHerd);
        if (currentSheep == null)
        {
            Debug.LogWarning("No sheep in herd!");
            sheepFocusMode = false;
            return;
        }

        Debug.Log($"Focusing on: {currentSheep.name} in {closestHerd.name}");
    }

    private Transform FindClosestHerd()
    {
        Herd[] allHerds = FindObjectsOfType<Herd>();
        if (allHerds.Length == 0) return null;

        Herd closestHerd = allHerds[0];
        float closestDist = Vector3.Distance(target.position, closestHerd.transform.position);

        for (int i = 1; i < allHerds.Length; i++)
        {
            float dist = Vector3.Distance(target.position, allHerds[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestHerd = allHerds[i];
            }
        }

        return closestHerd.transform;
    }

    private Transform GetRandomSheepInHerd(Transform herd)
    {
        Boids[] sheepInHerd = herd.GetComponentsInChildren<Boids>();
        if (sheepInHerd.Length == 0) return null;

        int randomIndex = Random.Range(0, sheepInHerd.Length);
        return sheepInHerd[randomIndex].transform;
    }

    private void SetCameraPosition(Vector3 presetVec)
    {
        float targetHeading = target != null ? target.eulerAngles.y : 0f;
        currentAngleY = presetVec.x + targetHeading + angleOffset;
        currentAngleX = -presetVec.y;
        currentZoom = presetVec.z;
    }
}