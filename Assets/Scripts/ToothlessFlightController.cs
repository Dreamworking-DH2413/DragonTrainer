using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls tail and feet animations during dragon flight
/// Tail responds to rotation and has idle movement
/// Feet have simple idle sine wave motion
/// </summary>
public class ToothlessFlightController : MonoBehaviour
{
    [Header("Setup")]
    public Transform armatureRoot;
    public bool autoFindArmature = true;
    
    [Header("Tail Control")]
    [Tooltip("Enable tail animation")]
    public bool enableTailControl = true;
    
    [Tooltip("Root bone of the tail chain")]
    public Transform tailRoot;
    
    [Tooltip("Auto-find tail bones by name pattern (TailBone)")]
    public bool autoFindTailBones = true;
    
    [Tooltip("Maximum tail bend angle based on rotation (degrees)")]
    public float maxTailRotationResponse = 25f;
    
    [Tooltip("How quickly tail responds to rotation changes")]
    public float tailRotationSmoothSpeed = 8f;
    
    [Tooltip("Tail idle wave amplitude (degrees)")]
    public float tailIdleAmplitude = 5f;
    
    [Tooltip("Tail idle wave frequency (cycles per second)")]
    public float tailIdleFrequency = 0.5f;
    
    [Tooltip("How tail bend distributes down the chain (0=even, 1=more at tip)")]
    [Range(0f, 2f)]
    public float tailBendCurve = 1.2f;
    
    [Header("Tail Fin Control")]
    [Tooltip("Enable tail fin animation")]
    public bool enableTailFinControl = true;
    
    [Tooltip("Auto-find tail fin bones by name pattern")]
    public bool autoFindTailFinBones = true;
    
    [Tooltip("V-shape angle for tail fins (degrees upward)")]
    [Range(0f, 90f)]
    public float tailFinVAngle = 30f;
    
    [Tooltip("Enable wind shake effect on tail fin tips")]
    public bool enableTailFinShake = true;
    
    [Tooltip("Intensity of tail fin shake (degrees)")]
    [Range(0f, 10f)]
    public float tailFinShakeIntensity = 1.5f;
    
    [Tooltip("Speed of tail fin shake oscillation")]
    [Range(0.1f, 20f)]
    public float tailFinShakeSpeed = 6f;
    
    [Tooltip("Randomness variation in tail fin shake")]
    [Range(0f, 1f)]
    public float tailFinShakeRandomness = 0.4f;
    
    [Tooltip("Scale shake with dragon movement speed")]
    public bool scaleTailFinShakeWithSpeed = true;
    
    [Header("Leg Control")]
    [Tooltip("Enable leg animation")]
    public bool enableLegControl = true;
    
    [Header("Back Legs")]
    [Tooltip("Left back leg bone (Thigh.L)")]
    public Transform leftBackLeg;
    
    [Tooltip("Right back leg bone (Thigh.R)")]
    public Transform rightBackLeg;
    
    [Header("Front Legs")]
    [Tooltip("Left front shoulder bone")]
    public Transform leftFrontLeg;
    
    [Tooltip("Right front shoulder bone")]
    public Transform rightFrontLeg;
    
    [Tooltip("Auto-find leg bones by name pattern")]
    public bool autoFindLegBones = true;
    
    [Tooltip("Base backward lean angle for back legs during flight (degrees)")]
    public float backLegFlightLeanAngle = 25f;
    
    [Tooltip("Base backward lean angle for front legs during flight (degrees)")]
    public float frontLegFlightLeanAngle = 35f;
    
    [Tooltip("Knee bend angle for back legs (degrees)")]
    public float backKneeBendAngle = 30f;
    
    [Tooltip("Knee bend angle for front legs (degrees)")]
    public float frontKneeBendAngle = 40f;
    
    [Tooltip("Leg idle wave amplitude (degrees)")]
    public float legIdleAmplitude = 2f;
    
    [Tooltip("Leg idle wave frequency (cycles per second)")]
    public float legIdleFrequency = 0.6f;
    
    [Tooltip("Phase offset between left and right legs")]
    public float legPhaseOffset = 0.5f;
    
    [Tooltip("Phase offset between front and back legs")]
    public float frontBackPhaseOffset = 0.25f;
    
    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool showDebugInfo = false;

    // Tail bones
    private List<Transform> tailBones = new List<Transform>();
    private List<Quaternion> tailBaseRotations = new List<Quaternion>();
    
    // Tail fin data
    private List<Transform> leftTailFinBones = new List<Transform>();
    private List<Transform> rightTailFinBones = new List<Transform>();
    private List<Transform> leftMidFinBones = new List<Transform>();
    private List<Transform> rightMidFinBones = new List<Transform>();
    private Dictionary<Transform, Quaternion> tailFinBaseRotations = new Dictionary<Transform, Quaternion>();
    private List<Transform> leftTailFinTipBones = new List<Transform>();
    private List<Transform> rightTailFinTipBones = new List<Transform>();
    private Dictionary<Transform, Quaternion> tailFinTipBaseRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, float> tailFinShakeOffsets = new Dictionary<Transform, float>();
    private float tailFinShakeTimeOffset = 0f;
    
    // Rotation tracking
    private Quaternion lastRotation;
    private Vector3 currentRotationAxis = Vector3.zero;
    private float currentRotationSpeed = 0f;
    private Vector3 targetTailBendAxis = Vector3.zero;
    private Vector3 currentTailBendAxis = Vector3.zero;
    private Vector3 lastPosition;
    private float currentMovementSpeed = 0f;
    
    // Leg base rotations
    private Quaternion leftBackLegBaseRotation;
    private Quaternion rightBackLegBaseRotation;
    private Quaternion leftFrontLegBaseRotation;
    private Quaternion rightFrontLegBaseRotation;
    private bool legRotationsInitialized = false;
    
    // Knee bones
    private Transform leftBackKnee;
    private Transform rightBackKnee;
    private Transform leftFrontKnee;
    private Transform rightFrontKnee;
    
    // Knee base rotations
    private Quaternion leftBackKneeBaseRotation;
    private Quaternion rightBackKneeBaseRotation;
    private Quaternion leftFrontKneeBaseRotation;
    private Quaternion rightFrontKneeBaseRotation;
    
    // Time tracking for sine waves
    private float timeAccumulator = 0f;
    
    private bool isInitialized = false;
    private Dictionary<string, Transform> boneCache = new Dictionary<string, Transform>();

    void Start()
    {
        Initialize();
        
        // Initialize shake with random offset
        tailFinShakeTimeOffset = Random.Range(0f, 100f);
        
        // Initialize position tracking
        lastPosition = transform.position;
    }

    public void Initialize()
    {
        // Find armature
        if (autoFindArmature && armatureRoot == null)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                armatureRoot = animator.transform;
            }
            
            if (armatureRoot == null)
            {
                armatureRoot = transform.Find("Armature") ?? transform.Find("ArmatureT") ?? transform.Find("Root");
            }
        }

        if (armatureRoot == null)
        {
            Debug.LogWarning("ToothlessFlightController: Armature root not found!");
            return;
        }

        // Build bone cache
        boneCache.Clear();
        BuildBoneCache(armatureRoot);

        // Setup tail
        if (enableTailControl)
        {
            SetupTail();
        }

        // Setup legs
        if (enableLegControl)
        {
            SetupLegs();
        }
        
        // Setup tail fins
        if (enableTailFinControl)
        {
            SetupTailFins();
        }

        // Initialize rotation tracking
        lastRotation = transform.rotation;
        
        isInitialized = true;
        int legCount = (leftBackLeg != null ? 1 : 0) + (rightBackLeg != null ? 1 : 0) + (leftFrontLeg != null ? 1 : 0) + (rightFrontLeg != null ? 1 : 0);
        Debug.Log($"ToothlessFlightController initialized! Tail bones: {tailBones.Count}, Legs: {legCount}/4, Fins: {leftTailFinBones.Count + rightTailFinBones.Count}/16 (8 mid-fins + 10 tail fins)");
    }

    void BuildBoneCache(Transform root)
    {
        boneCache[root.name] = root;
        foreach (Transform child in root)
        {
            BuildBoneCache(child);
        }
    }

    void SetupTail()
    {
        tailBones.Clear();
        tailBaseRotations.Clear();

        // Auto-find tail root if needed
        if (autoFindTailBones && tailRoot == null)
        {
            // Try common tail bone names
            foreach (var kvp in boneCache)
            {
                if (kvp.Key.ToLower().Contains("tail") && kvp.Key.Contains("1"))
                {
                    tailRoot = kvp.Value;
                    Debug.Log($"Auto-found tail root: {kvp.Key}");
                    break;
                }
            }
        }

        if (tailRoot == null)
        {
            Debug.LogWarning("ToothlessFlightController: Tail root not found!");
            return;
        }

        // Recursively find all tail bones in the chain
        CollectTailBones(tailRoot);
        
        // Store base rotations
        foreach (var bone in tailBones)
        {
            tailBaseRotations.Add(bone.localRotation);
        }

        Debug.Log($"Setup {tailBones.Count} tail bones starting from {tailRoot.name}");
    }

    void CollectTailBones(Transform bone)
    {
        tailBones.Add(bone);
        
        // Continue down the chain - tail bones typically have one child
        if (bone.childCount > 0)
        {
            foreach (Transform child in bone)
            {
                // Only continue if this looks like a tail bone
                if (child.name.ToLower().Contains("tail"))
                {
                    CollectTailBones(child);
                    break; // Only follow one chain
                }
            }
        }
    }

    void SetupLegs()
    {
        // Auto-find legs if needed
        if (autoFindLegBones)
        {
            // Back legs - look for Thigh bones
            if (leftBackLeg == null && boneCache.ContainsKey("Thigh.L"))
            {
                leftBackLeg = boneCache["Thigh.L"];
                Debug.Log($"Auto-found left back leg: Thigh.L");
            }

            if (rightBackLeg == null && boneCache.ContainsKey("Thigh.R"))
            {
                rightBackLeg = boneCache["Thigh.R"];
                Debug.Log($"Auto-found right back leg: Thigh.R");
            }
            
            // Front legs - look for Shoulder bones
            if (leftFrontLeg == null && boneCache.ContainsKey("FShoulder.L"))
            {
                leftFrontLeg = boneCache["FShoulder.L"];
                Debug.Log($"Auto-found left front leg: FShoulder.L");
            }
            
            if (rightFrontLeg == null && boneCache.ContainsKey("FShoulder.R"))
            {
                rightFrontLeg = boneCache["FShoulder.R"];
                Debug.Log($"Auto-found right front leg: FShoulder.R");
            }
            
            // Find knee bones
            if (boneCache.ContainsKey("Knee.L"))
            {
                leftBackKnee = boneCache["Knee.L"];
                Debug.Log($"Auto-found left back knee: Knee.L");
            }
            
            if (boneCache.ContainsKey("Knee.R"))
            {
                rightBackKnee = boneCache["Knee.R"];
                Debug.Log($"Auto-found right back knee: Knee.R");
            }
            
            if (boneCache.ContainsKey("FKnee.L"))
            {
                leftFrontKnee = boneCache["FKnee.L"];
                Debug.Log($"Auto-found left front knee: FKnee.L");
            }
            
            if (boneCache.ContainsKey("FKnee.R"))
            {
                rightFrontKnee = boneCache["FKnee.R"];
                Debug.Log($"Auto-found right front knee: FKnee.R");
            }
        }

        // Store base rotations for legs
        if (leftBackLeg != null)
        {
            leftBackLegBaseRotation = leftBackLeg.localRotation;
        }
        if (rightBackLeg != null)
        {
            rightBackLegBaseRotation = rightBackLeg.localRotation;
        }
        if (leftFrontLeg != null)
        {
            leftFrontLegBaseRotation = leftFrontLeg.localRotation;
        }
        if (rightFrontLeg != null)
        {
            rightFrontLegBaseRotation = rightFrontLeg.localRotation;
        }
        
        // Store base rotations for knees
        if (leftBackKnee != null)
        {
            leftBackKneeBaseRotation = leftBackKnee.localRotation;
        }
        if (rightBackKnee != null)
        {
            rightBackKneeBaseRotation = rightBackKnee.localRotation;
        }
        if (leftFrontKnee != null)
        {
            leftFrontKneeBaseRotation = leftFrontKnee.localRotation;
        }
        if (rightFrontKnee != null)
        {
            rightFrontKneeBaseRotation = rightFrontKnee.localRotation;
        }
        
        legRotationsInitialized = true;
    }
    
    void SetupTailFins()
    {
        leftTailFinBones.Clear();
        rightTailFinBones.Clear();
        leftMidFinBones.Clear();
        rightMidFinBones.Clear();
        leftTailFinTipBones.Clear();
        rightTailFinTipBones.Clear();
        tailFinBaseRotations.Clear();
        tailFinTipBaseRotations.Clear();
        
        // Auto-find all tail fin bones
        // Structure: 
        // - MidFin1-3.L/R (3 mid-fins on each side attached to Tail.1) - NO V-angle
        // - Fin1-5.L/R (5 tail fins on each side attached to Tail.4 and Tail.005) - WITH V-angle
        // Each fin has child bones like Bone.127.L -> Bone.127.L.001
        if (autoFindTailFinBones)
        {
            // Find all 3 left mid-fins (no V-angle)
            for (int i = 1; i <= 3; i++)
            {
                string finName = $"MidFin{i}.L";
                if (boneCache.ContainsKey(finName))
                {
                    Transform fin = boneCache[finName];
                    leftMidFinBones.Add(fin);
                    tailFinBaseRotations[fin] = fin.localRotation;
                    
                    // Collect shake bones for this fin (second-to-last bone)
                    CollectFinShakeBones(fin, leftTailFinTipBones);
                    
                    Debug.Log($"Auto-found left mid-fin: {finName}");
                }
            }
            
            // Find all 5 left tail fins (with V-angle)
            for (int i = 1; i <= 5; i++)
            {
                string finName = $"Fin{i}.L";
                if (boneCache.ContainsKey(finName))
                {
                    Transform fin = boneCache[finName];
                    leftTailFinBones.Add(fin);
                    tailFinBaseRotations[fin] = fin.localRotation;
                    
                    // Collect shake bones for this fin (second-to-last bone)
                    CollectFinShakeBones(fin, leftTailFinTipBones);
                    
                    Debug.Log($"Auto-found left tail fin: {finName}");
                }
            }
            
            // Find all 3 right mid-fins (no V-angle)
            for (int i = 1; i <= 3; i++)
            {
                string finName = $"MidFin{i}.R";
                if (boneCache.ContainsKey(finName))
                {
                    Transform fin = boneCache[finName];
                    rightMidFinBones.Add(fin);
                    tailFinBaseRotations[fin] = fin.localRotation;
                    
                    // Collect shake bones for this fin (second-to-last bone)
                    CollectFinShakeBones(fin, rightTailFinTipBones);
                    
                    Debug.Log($"Auto-found right mid-fin: {finName}");
                }
            }
            
            // Find all 5 right tail fins (with V-angle)
            for (int i = 1; i <= 5; i++)
            {
                string finName = $"Fin{i}.R";
                if (boneCache.ContainsKey(finName))
                {
                    Transform fin = boneCache[finName];
                    rightTailFinBones.Add(fin);
                    tailFinBaseRotations[fin] = fin.localRotation;
                    
                    // Collect shake bones for this fin (second-to-last bone)
                    CollectFinShakeBones(fin, rightTailFinTipBones);
                    
                    Debug.Log($"Auto-found right tail fin: {finName}");
                }
            }
        }
        
        Debug.Log($"Setup tail fins: {leftTailFinBones.Count} left tail fins, {rightTailFinBones.Count} right tail fins");
        Debug.Log($"Setup mid fins: {leftMidFinBones.Count} left mid-fins, {rightMidFinBones.Count} right mid-fins");
        Debug.Log($"Fin shake bones: {leftTailFinTipBones.Count} left, {rightTailFinTipBones.Count} right");
    }
    
    void CollectFinShakeBones(Transform finRoot, List<Transform> shakeBones)
    {
        // Each fin has a structure like: FinX.L -> Bone.XXX.L -> Bone.XXX.L.001
        // We want to shake the second-to-last bone (Bone.XXX.L)
        // This gives a more visible shake effect
        
        // First child is typically the first segment
        if (finRoot.childCount > 0)
        {
            Transform firstChild = finRoot.GetChild(0);
            
            // Store base rotation for the shake bone
            if (!tailFinTipBaseRotations.ContainsKey(firstChild))
            {
                tailFinTipBaseRotations[firstChild] = firstChild.localRotation;
            }
            
            // Add this bone to shake list
            shakeBones.Add(firstChild);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        timeAccumulator += Time.deltaTime;
        
        // Calculate movement speed
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        currentMovementSpeed = distanceMoved / Time.deltaTime;
        lastPosition = transform.position;

        // Update tail control
        if (enableTailControl && tailBones.Count > 0)
        {
            UpdateTailControl();
        }

        // Debug output
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Rotation Speed: {currentRotationSpeed:F2}°/s, Axis: {currentRotationAxis}, Tail Bend: {currentTailBendAxis}");
        }
    }

    void UpdateTailControl()
    {
        // Calculate rotation velocity (angular velocity)
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
        float angle;
        Vector3 axis;
        deltaRotation.ToAngleAxis(out angle, out axis);
        
        // Normalize angle to -180 to 180
        if (angle > 180f) angle -= 360f;
        
        // Get rotation speed (degrees per second)
        currentRotationSpeed = angle / Time.deltaTime;
        
        // Store the rotation axis (direction of rotation)
        if (Mathf.Abs(currentRotationSpeed) > 0.1f) // Only update if rotating meaningfully
        {
            currentRotationAxis = axis;
        }
        
        // Calculate target tail bend axis (opposite to rotation axis for counterbalance)
        // Scale by rotation speed
        float bendMagnitude = Mathf.Clamp(Mathf.Abs(currentRotationSpeed) * 0.4f, 0f, maxTailRotationResponse);
        targetTailBendAxis = -currentRotationAxis * bendMagnitude;
        
        // Smooth transition
        currentTailBendAxis = Vector3.Lerp(currentTailBendAxis, targetTailBendAxis, Time.deltaTime * tailRotationSmoothSpeed);
        
        // Store current rotation for next frame
        lastRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (!isInitialized) return;

        // Apply tail animation
        if (enableTailControl && tailBones.Count > 0)
        {
            AnimateTail();
        }

        // Apply leg animation
        if (enableLegControl && legRotationsInitialized)
        {
            AnimateLegs();
        }
        
        // Apply tail fin animation
        if (enableTailFinControl && (leftTailFinBones.Count > 0 || rightTailFinBones.Count > 0 || leftMidFinBones.Count > 0 || rightMidFinBones.Count > 0))
        {
            AnimateTailFins();
        }
    }

    void AnimateTail()
    {
        // Convert the bend axis to local space of the dragon
        Vector3 localBendAxis = transform.InverseTransformDirection(currentTailBendAxis);
        
        // Extract pitch (X) and yaw (Z) components from the local bend axis
        float pitchBend = localBendAxis.x;
        float yawBend = -localBendAxis.z; // Invert yaw

        // Apply progressive bend down the tail chain
        for (int i = 0; i < tailBones.Count; i++)
        {
            if (tailBones[i] == null || i >= tailBaseRotations.Count) continue;

            // Calculate bend distribution (more bend towards the tip)
            float normalizedPosition = (float)i / (tailBones.Count - 1);
            float bendMultiplier = Mathf.Pow(normalizedPosition, tailBendCurve);
            
            // Distribute bend across the chain
            float segmentPitchBend = (pitchBend / tailBones.Count) * (1f + bendMultiplier);
            float segmentYawBend = (yawBend / tailBones.Count) * (1f + bendMultiplier);
            
            // Apply rotation: X axis for pitch (up-down), Z axis for yaw (side-to-side)
            Quaternion bendRotation = Quaternion.Euler(segmentPitchBend, 0, segmentYawBend);
            tailBones[i].localRotation = tailBaseRotations[i] * bendRotation;
        }
    }

    void AnimateLegs()
    {
        // Back legs - backward lean + subtle idle wave
        if (leftBackLeg != null)
        {
            float leftWave = Mathf.Sin(timeAccumulator * legIdleFrequency * Mathf.PI * 2f) * legIdleAmplitude;
            // Apply backward lean (positive X rotation for back legs) + idle wave
            Quaternion leftRotation = Quaternion.Euler(backLegFlightLeanAngle + leftWave, 0, 0);
            leftBackLeg.localRotation = leftBackLegBaseRotation * leftRotation;
        }

        if (rightBackLeg != null)
        {
            float rightWave = Mathf.Sin((timeAccumulator + legPhaseOffset) * legIdleFrequency * Mathf.PI * 2f) * legIdleAmplitude;
            // Apply backward lean (positive X rotation for back legs) + idle wave
            Quaternion rightRotation = Quaternion.Euler(backLegFlightLeanAngle + rightWave, 0, 0);
            rightBackLeg.localRotation = rightBackLegBaseRotation * rightRotation;
        }
        
        // Front legs - backward lean + subtle idle wave with different phase
        if (leftFrontLeg != null)
        {
            float leftFrontWave = Mathf.Sin((timeAccumulator + frontBackPhaseOffset) * legIdleFrequency * Mathf.PI * 2f) * legIdleAmplitude;
            // Apply backward lean (positive X rotation for front legs) + idle wave
            Quaternion leftFrontRotation = Quaternion.Euler(frontLegFlightLeanAngle + leftFrontWave, 0, 0);
            leftFrontLeg.localRotation = leftFrontLegBaseRotation * leftFrontRotation;
        }
        
        if (rightFrontLeg != null)
        {
            float rightFrontWave = Mathf.Sin((timeAccumulator + frontBackPhaseOffset + legPhaseOffset) * legIdleFrequency * Mathf.PI * 2f) * legIdleAmplitude;
            // Apply backward lean (positive X rotation for front legs) + idle wave
            Quaternion rightFrontRotation = Quaternion.Euler(frontLegFlightLeanAngle + rightFrontWave, 0, 0);
            rightFrontLeg.localRotation = rightFrontLegBaseRotation * rightFrontRotation;
        }
        
        // Apply knee bends
        if (leftBackKnee != null)
        {
            float leftKneeWave = Mathf.Sin(timeAccumulator * legIdleFrequency * Mathf.PI * 2f) * (legIdleAmplitude * 0.5f);
            Quaternion kneeRotation = Quaternion.Euler(backKneeBendAngle + leftKneeWave, 0, 0);
            leftBackKnee.localRotation = leftBackKneeBaseRotation * kneeRotation;
        }
        
        if (rightBackKnee != null)
        {
            float rightKneeWave = Mathf.Sin((timeAccumulator + legPhaseOffset) * legIdleFrequency * Mathf.PI * 2f) * (legIdleAmplitude * 0.5f);
            Quaternion kneeRotation = Quaternion.Euler(backKneeBendAngle + rightKneeWave, 0, 0);
            rightBackKnee.localRotation = rightBackKneeBaseRotation * kneeRotation;
        }
        
        if (leftFrontKnee != null)
        {
            float leftFrontKneeWave = Mathf.Sin((timeAccumulator + frontBackPhaseOffset) * legIdleFrequency * Mathf.PI * 2f) * (legIdleAmplitude * 0.5f);
            Quaternion kneeRotation = Quaternion.Euler(frontKneeBendAngle + leftFrontKneeWave, 0, 0);
            leftFrontKnee.localRotation = leftFrontKneeBaseRotation * kneeRotation;
        }
        
        if (rightFrontKnee != null)
        {
            float rightFrontKneeWave = Mathf.Sin((timeAccumulator + frontBackPhaseOffset + legPhaseOffset) * legIdleFrequency * Mathf.PI * 2f) * (legIdleAmplitude * 0.5f);
            Quaternion kneeRotation = Quaternion.Euler(frontKneeBendAngle + rightFrontKneeWave, 0, 0);
            rightFrontKnee.localRotation = rightFrontKneeBaseRotation * kneeRotation;
        }
    }
    
    void AnimateTailFins()
    {
        // Apply V-shape angle to tail fin root bones only (not mid-fins)
        foreach (Transform fin in leftTailFinBones)
        {
            if (fin != null && tailFinBaseRotations.ContainsKey(fin))
            {
                // Left tail fins angle upward (positive X rotation)
                Quaternion vShapeRotation = Quaternion.Euler(tailFinVAngle, 0, 0);
                fin.localRotation = tailFinBaseRotations[fin] * vShapeRotation;
            }
        }
        
        foreach (Transform fin in rightTailFinBones)
        {
            if (fin != null && tailFinBaseRotations.ContainsKey(fin))
            {
                // Right tail fins angle upward (positive X rotation)
                Quaternion vShapeRotation = Quaternion.Euler(tailFinVAngle, 0, 0);
                fin.localRotation = tailFinBaseRotations[fin] * vShapeRotation;
            }
        }
        
        // Mid-fins keep their base rotation (no V-angle)
        foreach (Transform fin in leftMidFinBones)
        {
            if (fin != null && tailFinBaseRotations.ContainsKey(fin))
            {
                fin.localRotation = tailFinBaseRotations[fin];
            }
        }
        
        foreach (Transform fin in rightMidFinBones)
        {
            if (fin != null && tailFinBaseRotations.ContainsKey(fin))
            {
                fin.localRotation = tailFinBaseRotations[fin];
            }
        }
        
        // Apply shake to tip bones (all fins get shake)
        if (enableTailFinShake)
        {
            ApplyTailFinShake(leftTailFinTipBones);
            ApplyTailFinShake(rightTailFinTipBones);
        }
    }
    
    void ApplyTailFinShake(List<Transform> shakeBones)
    {
        foreach (Transform shakeBone in shakeBones)
        {
            if (shakeBone == null || !tailFinTipBaseRotations.ContainsKey(shakeBone))
                continue;
            
            float shake = CalculateTailFinShake(shakeBone);
            
            // Apply shake as rotation around local X axis (up-down flutter)
            Quaternion shakeRotation = Quaternion.Euler(shake, 0, 0);
            shakeBone.localRotation = tailFinTipBaseRotations[shakeBone] * shakeRotation;
        }
    }
    
    float CalculateTailFinShake(Transform bone)
    {
        // Initialize unique offset for this bone if not exists
        if (!tailFinShakeOffsets.ContainsKey(bone))
        {
            tailFinShakeOffsets[bone] = Random.Range(0f, 100f);
        }
        
        float boneOffset = tailFinShakeOffsets[bone];
        float time = Time.time * tailFinShakeSpeed + tailFinShakeTimeOffset + boneOffset;
        
        // Multi-frequency shake for realistic turbulence
        float shake = Mathf.Sin(time) * 0.5f;
        shake += Mathf.Sin(time * 1.5f + boneOffset) * 0.3f;
        shake += Mathf.Sin(time * 2.1f - boneOffset) * 0.2f;
        
        // Add noise
        shake += (Mathf.PerlinNoise(time * 0.4f, boneOffset) - 0.5f) * tailFinShakeRandomness;
        
        // Scale by intensity
        shake *= tailFinShakeIntensity;
        
        // Scale with movement speed if enabled
        if (scaleTailFinShakeWithSpeed)
        {
            float speedFactor = Mathf.Clamp01(currentMovementSpeed / 10f);
            shake *= Mathf.Lerp(0.4f, 1.8f, speedFactor);
        }
        
        return shake;
    }

    // Public API
    public void SetTailEnabled(bool enabled)
    {
        enableTailControl = enabled;
    }

    public void SetLegEnabled(bool enabled)
    {
        enableLegControl = enabled;
    }
    
    public void SetTailFinEnabled(bool enabled)
    {
        enableTailFinControl = enabled;
    }

    public float GetCurrentRotationSpeed()
    {
        return currentRotationSpeed;
    }
    
    public Vector3 GetCurrentTailBend()
    {
        return currentTailBendAxis;
    }
    
    public float GetCurrentMovementSpeed()
    {
        return currentMovementSpeed;
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ToothlessFlightController))]
public class ToothlessFlightControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ToothlessFlightController controller = (ToothlessFlightController)target;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Setup", UnityEditor.EditorStyles.boldLabel);

        if (GUILayout.Button("Initialize"))
        {
            controller.Initialize();
        }

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Current State", UnityEditor.EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.LabelField($"Rotation Speed: {controller.GetCurrentRotationSpeed():F2}°/s");
            UnityEditor.EditorGUILayout.LabelField($"Movement Speed: {controller.GetCurrentMovementSpeed():F2} m/s");
            UnityEditor.EditorGUILayout.LabelField($"Tail Bend Axis: {controller.GetCurrentTailBend()}");
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("Enter Play mode to see live values", UnityEditor.MessageType.Info);
        }

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Controls", UnityEditor.EditorStyles.boldLabel);

        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(controller.enableTailControl ? "Disable Tail" : "Enable Tail"))
        {
            controller.SetTailEnabled(!controller.enableTailControl);
        }
        if (GUILayout.Button(controller.enableLegControl ? "Disable Legs" : "Enable Legs"))
        {
            controller.SetLegEnabled(!controller.enableLegControl);
        }
        UnityEditor.EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button(controller.enableTailFinControl ? "Disable Tail Fins" : "Enable Tail Fins"))
        {
            controller.SetTailFinEnabled(!controller.enableTailFinControl);
        }

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.HelpBox(
            "TAIL: Responds to dragon rotation + idle sine wave\n" +
            "LEGS: Lean backwards for flight pose + subtle idle movement\n" +
            "TAIL FINS: V-shape angle upwards + wind shake on tips\n\n" +
            "The tail will automatically bend opposite to rotation direction and sway gently when idle.\n" +
            "The legs maintain a backwards lean suitable for flight with slight movement.\n" +
            "The tail fins angle upward in a V-shape and tips flutter in the wind.",
            UnityEditor.MessageType.Info);
    }
}
#endif
