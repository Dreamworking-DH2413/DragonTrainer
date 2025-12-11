using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Simple wing controller for Toothless that uses the imported bone hierarchy
/// No JSON needed - works directly with Unity transforms
/// </summary>
public class ToothlessWingController : MonoBehaviour
{
    [Header("Control Mode")]
    [Tooltip("Enable automatic wing control based on IK target positions")]
    public bool automaticControl = true;
    
    [Header("Manual Control (when automaticControl = false)")]
    [Range(0f, 1f)]
    [Tooltip("0 = Wings Extended, 1 = Wings Folded")]
    public float leftWingFold = 0f;
    
    [Range(0f, 1f)]
    [Tooltip("0 = Wings Extended, 1 = Wings Folded")]
    public float rightWingFold = 0f;

    [Range(0.1f, 5f)]
    public float animationSpeed = 1f;

    [Header("Automatic Control - IK Targets")]
    [Tooltip("Left wing IK target transform")]
    public Transform leftWingTarget;
    
    [Tooltip("Right wing IK target transform")]
    public Transform rightWingTarget;
    
    [Header("Automatic Control - Folding")]
    [Tooltip("Distance beyond which wings don't fold at all")]
    public float foldThresholdDistance = 2.0f;
    
    [Tooltip("Distance at which wings are fully folded")]
    public float minFoldDistance = 0.5f;
    
    [Tooltip("How quickly wings respond to distance changes")]
    public float foldSmoothSpeed = 5f;
    
    [Header("Automatic Control - Natural Curve")]
    [Tooltip("Base downward curve of wing fingers (simulates aerodynamic forces)")]
    public float baseDownwardCurve = 15f;
    
    [Tooltip("Maximum additional curve during downward flapping (downstroke)")]
    public float maxDownstrokeCurveBoost = 10f;
    
    [Tooltip("Maximum additional curve during upward flapping (upstroke)")]
    public float maxUpstrokeCurveBoost = 8f;
    
    [Tooltip("Speed threshold for maximum curve boost (units per second)")]
    public float maxCurveSpeed = 5f;
    
    [Tooltip("How smoothly curve changes blend")]
    public float curveBlendSpeed = 3f;
    
    [Header("Automatic Control - Shoulder Rotation")]
    [Tooltip("Maximum shoulder rotation based on flapping velocity (degrees)")]
    public float maxShoulderRotation = 20f;
    
    [Tooltip("Height above body where maximum shoulder rotation occurs")]
    public float maxShoulderRotationHeight = 1f;
    
    [Tooltip("How smoothly shoulder rotation changes")]
    public float shoulderRotationSpeed = 5f;
    
    [Header("Automatic Control - Finger Tip Aerodynamics")]
    [Tooltip("Maximum finger tip rotation based on flapping (degrees)")]
    public float maxFingerTipRotation = 15f;
    
    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool showDebugInfo = false;
    
    [Header("Manual Curve Test")]
    [Range(0f, 90f)]
    [Tooltip("Manual downward curve override for testing (only works when automaticControl is OFF)")]
    public float manualCurveTest = 15f;
    
    [Range(-30f, 30f)]
    [Tooltip("Manual finger tip rotation override for testing (only works when automaticControl is OFF)")]
    public float manualFingerTipTest = 0f;

    [Header("Setup")]
    public Transform armatureRoot;
    public bool autoFindArmature = true;
    
    [Tooltip("RigBuilder component - will auto-find if not assigned")]
    public RigBuilder rigBuilder;
    
    [Header("IK Compatibility")]
    [Tooltip("Skip animating shoulder, elbow, and forearm bones - let IK control them for flapping")]
    public bool useIKForFlapping = true;

    [Header("Procedural Animation")]
    [Tooltip("Use procedural folding instead of captured poses")]
    public bool useProceduralAnimation = true;

    [Header("Fold Parameters - Left Wing")]
    public float leftWristFold = 120f;
    public float leftFingerSpread = 40f;
    public float leftFingerSpacing = -18f;

    [Header("Fold Parameters - Right Wing")]
    public float rightWristFold = -120f;
    public float rightFingerSpread = -40f;
    public float rightFingerSpacing = 18f;

    [Header("Pose Capture (Alternative)")]
    [Tooltip("Manually set this pose as the extended (open) wing pose")]
    public bool captureExtendedPose = false;
    
    [Tooltip("Manually set this pose as the folded (closed) wing pose")]
    public bool captureFoldedPose = false;

    // Wing bone data
    [System.Serializable]
    public class WingBoneData
    {
        public Transform bone;
        public string boneName;
        public Quaternion extendedRotation;
        public Quaternion foldedRotation;
    }

    private List<WingBoneData> leftWingBones = new List<WingBoneData>();
    private List<WingBoneData> rightWingBones = new List<WingBoneData>();
    private Dictionary<string, Transform> boneCache = new Dictionary<string, Transform>();
    private bool isInitialized = false;

    // Auto-control state
    private float currentLeftFold = 0f;
    private float currentRightFold = 0f;
    private float currentLeftCurve = 0f;
    private float currentRightCurve = 0f;
    
    // Velocity tracking for targets
    private Vector3 lastLeftTargetLocalPosition;
    private Vector3 lastRightTargetLocalPosition;
    private float leftTargetVerticalVelocity = 0f;
    private float rightTargetVerticalVelocity = 0f;
    
    // Shoulder rotation state
    private float currentLeftShoulderRotation = 0f;
    private float currentRightShoulderRotation = 0f;
    private Quaternion leftShoulderBaseRotation;
    private Quaternion rightShoulderBaseRotation;
    private bool shoulderRotationsInitialized = false;
    
    // Cached bone references for curve application
    private Transform leftWristBone;
    private Transform rightWristBone;
    private Transform leftForearmBone;
    private Transform rightForearmBone;
    private Transform leftShoulderBone;
    private Transform rightShoulderBone;
    
    // Store base rotations to apply curve additively
    private Quaternion leftWristBaseRotation;
    private Quaternion rightWristBaseRotation;
    private Quaternion leftForearmBaseRotation;
    private Quaternion rightForearmBaseRotation;

    // Wing bone names from Blender
    private string[] leftWingBoneNames = new string[] {
        "WingShoulder.L", "WingElbow.L", "WingForeArm.L", "WingWrist.L",
        "WingBone1.L", "WingBone2.L", "WingBone3.L", "WingBone4.L", "WingBone5.L", "WingBone6.L"
    };

    private string[] rightWingBoneNames = new string[] {
        "WingShoulder.R", "WingElbow.R", "WingForeArm.R", "WingWrist.R",
        "WingBone1.R", "WingBone2.R", "WingBone3.R", "WingBone4.R", "WingBone5.R", "WingBone6.R"
    };

    void Start()
    {
        Initialize();
        AutoFindTargetsIfNeeded();
        
        // Find RigBuilder if not assigned
        if (rigBuilder == null)
        {
            rigBuilder = GetComponent<RigBuilder>();
            if (rigBuilder != null)
            {
                Debug.Log("Auto-found RigBuilder component");
            }
        }
    }

    void Update()
    {
        // Editor helpers for pose capture
        if (captureExtendedPose)
        {
            captureExtendedPose = false;
            CaptureExtendedPose();
        }

        if (captureFoldedPose)
        {
            captureFoldedPose = false;
            CaptureFoldedPose();
        }

        // Update automatic control
        if (automaticControl && isInitialized)
        {
            UpdateAutomaticControl();
        }
    }

    void AutoFindTargetsIfNeeded()
    {
        // Auto-find IK targets if not assigned
        if (leftWingTarget == null)
        {
            GameObject targetObj = GameObject.Find("LeftWingTarget");
            if (targetObj != null)
            {
                leftWingTarget = targetObj.transform;
                Debug.Log("Auto-found LeftWingTarget");
            }
        }

        if (rightWingTarget == null)
        {
            GameObject targetObj = GameObject.Find("RightWingTarget");
            if (targetObj != null)
            {
                rightWingTarget = targetObj.transform;
                Debug.Log("Auto-found RightWingTarget");
            }
        }
        
        // Initialize target positions for velocity tracking in local space
        if (leftWingTarget != null)
        {
            lastLeftTargetLocalPosition = leftWingTarget.parent != null
                ? leftWingTarget.parent.InverseTransformPoint(leftWingTarget.position)
                : transform.InverseTransformPoint(leftWingTarget.position);
        }
        if (rightWingTarget != null)
        {
            lastRightTargetLocalPosition = rightWingTarget.parent != null
                ? rightWingTarget.parent.InverseTransformPoint(rightWingTarget.position)
                : transform.InverseTransformPoint(rightWingTarget.position);
        }
    }

    void UpdateAutomaticControl()
    {
        // Update left wing
        if (leftWingTarget != null && leftShoulderBone != null)
        {
            // Calculate velocity in local space relative to the target's parent (the dragon)
            // This ensures we're measuring the actual wing flapping motion, not dragon body movement
            Vector3 currentLocalPos = leftWingTarget.parent != null 
                ? leftWingTarget.parent.InverseTransformPoint(leftWingTarget.position)
                : transform.InverseTransformPoint(leftWingTarget.position);
            
            float verticalDelta = currentLocalPos.y - lastLeftTargetLocalPosition.y;
            leftTargetVerticalVelocity = verticalDelta / Time.deltaTime;
            lastLeftTargetLocalPosition = currentLocalPos;
            
            float distance = Vector3.Distance(leftWingTarget.position, leftShoulderBone.position);
            float targetFold = CalculateFoldAmount(distance);
            currentLeftFold = Mathf.Lerp(currentLeftFold, targetFold, Time.deltaTime * foldSmoothSpeed);
            leftWingFold = currentLeftFold;

            // Calculate curve based on vertical velocity (downward movement)
            float targetCurve = CalculateCurveAmount(leftTargetVerticalVelocity);
            currentLeftCurve = Mathf.Lerp(currentLeftCurve, targetCurve, Time.deltaTime * curveBlendSpeed);
            
            // Calculate shoulder rotation based on flapping direction (velocity)
            float targetShoulderRotation = CalculateShoulderRotationFromVelocity(leftTargetVerticalVelocity);
            currentLeftShoulderRotation = Mathf.Lerp(currentLeftShoulderRotation, targetShoulderRotation, Time.deltaTime * shoulderRotationSpeed);

            if (showDebugInfo && Time.frameCount % 30 == 0) // Log every 30 frames
            {
                Debug.Log($"Left Wing - Distance: {distance:F2}, Fold: {currentLeftFold:F2}, VertVelocity: {leftTargetVerticalVelocity:F2}, Curve: {currentLeftCurve:F2}, ShoulderRot: {currentLeftShoulderRotation:F2}");
            }
        }

        // Update right wing
        if (rightWingTarget != null && rightShoulderBone != null)
        {
            // Calculate velocity in local space relative to the target's parent (the dragon)
            // This ensures we're measuring the actual wing flapping motion, not dragon body movement
            Vector3 currentLocalPos = rightWingTarget.parent != null 
                ? rightWingTarget.parent.InverseTransformPoint(rightWingTarget.position)
                : transform.InverseTransformPoint(rightWingTarget.position);
            
            float verticalDelta = currentLocalPos.y - lastRightTargetLocalPosition.y;
            rightTargetVerticalVelocity = verticalDelta / Time.deltaTime;
            lastRightTargetLocalPosition = currentLocalPos;
            
            float distance = Vector3.Distance(rightWingTarget.position, rightShoulderBone.position);
            float targetFold = CalculateFoldAmount(distance);
            currentRightFold = Mathf.Lerp(currentRightFold, targetFold, Time.deltaTime * foldSmoothSpeed);
            rightWingFold = currentRightFold;

            // Calculate curve based on vertical velocity (downward movement)
            float targetCurve = CalculateCurveAmount(rightTargetVerticalVelocity);
            currentRightCurve = Mathf.Lerp(currentRightCurve, targetCurve, Time.deltaTime * curveBlendSpeed);
            
            // Calculate shoulder rotation based on flapping direction (velocity)
            float targetShoulderRotation = CalculateShoulderRotationFromVelocity(rightTargetVerticalVelocity);
            currentRightShoulderRotation = Mathf.Lerp(currentRightShoulderRotation, targetShoulderRotation, Time.deltaTime * shoulderRotationSpeed);
        }
    }

    float CalculateFoldAmount(float distance)
    {
        // Beyond threshold = no fold
        if (distance >= foldThresholdDistance)
        {
            return 0f;
        }
        
        // At or below min distance = full fold
        if (distance <= minFoldDistance)
        {
            return 1f;
        }
        
        // Linear interpolation between min and threshold
        float foldRange = foldThresholdDistance - minFoldDistance;
        float foldAmount = 1f - ((distance - minFoldDistance) / foldRange);
        return Mathf.Clamp01(foldAmount);
    }

    float CalculateCurveAmount(float verticalVelocity)
    {
        // Wings always curve downward due to aerodynamic forces
        // Add extra curve during active flapping - faster = more curve
        
        float curveBoost = 0f;
        float speed = Mathf.Abs(verticalVelocity);
        
        // Calculate speed factor (0 to 1) based on how fast the wing is moving
        float speedFactor = Mathf.Clamp01(speed / maxCurveSpeed);
        
        if (verticalVelocity < 0)
        {
            // Wing moving downward (downstroke) - curve based on speed
            curveBoost = maxDownstrokeCurveBoost * speedFactor;
        }
        else if (verticalVelocity > 0)
        {
            // Wing moving upward (upstroke) - curve based on speed
            curveBoost = maxUpstrokeCurveBoost * speedFactor;
        }
        
        // Always apply base curve + speed-based flapping boost
        // Result is always positive (downward curve)
        return baseDownwardCurve + curveBoost;
    }
    
    float CalculateShoulderRotationFromVelocity(float verticalVelocity)
    {
        // Shoulder rotates based on flapping direction to simulate aerodynamic forces
        // Downward flapping (negative velocity) = shoulder rotates forward (positive)
        // Upward flapping (positive velocity) = shoulder rotates backward (negative)
        
        float velocitySensitivity = 4f; // How much velocity affects shoulder rotation
        
        // Calculate rotation based on velocity (inverted)
        // Clamp to max shoulder rotation value
        float rotation = -verticalVelocity * velocitySensitivity;
        return Mathf.Clamp(rotation, -maxShoulderRotation, maxShoulderRotation);
    }

    public void Initialize()
    {
        // Find armature
        if (autoFindArmature && armatureRoot == null)
        {
            // Try to find Animator component
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                armatureRoot = animator.transform;
            }
            
            // Try common armature names
            if (armatureRoot == null)
            {
                armatureRoot = transform.Find("Armature") ?? transform.Find("ArmatureT") ?? transform.Find("Root");
            }
        }

        if (armatureRoot == null)
        {
            // Debug.LogError("Armature root not found! Please assign it manually.");
            return;
        }

        // Build bone cache
        boneCache.Clear();
        BuildBoneCache(armatureRoot);
        // Debug.Log($"Built bone cache with {boneCache.Count} bones");

        // Setup wing bones
        SetupWingBones();

        isInitialized = true;
        // Debug.Log("Wing controller initialized!");
    }

    void BuildBoneCache(Transform root)
    {
        boneCache[root.name] = root;
        foreach (Transform child in root)
        {
            BuildBoneCache(child);
        }
    }

    void SetupWingBones()
    {
        leftWingBones.Clear();
        rightWingBones.Clear();

        // Setup left wing
        foreach (string boneName in leftWingBoneNames)
        {
            if (boneCache.ContainsKey(boneName))
            {
                var boneData = new WingBoneData
                {
                    bone = boneCache[boneName],
                    boneName = boneName,
                    extendedRotation = boneCache[boneName].localRotation, // Current pose as extended
                    foldedRotation = boneCache[boneName].localRotation    // Will be set when captured
                };
                leftWingBones.Add(boneData);
                // Debug.Log($"Added left wing bone: {boneName}");
            }
            else
            {
                // Debug.LogWarning($"Left wing bone not found: {boneName}");
            }
        }

        // Setup right wing
        foreach (string boneName in rightWingBoneNames)
        {
            if (boneCache.ContainsKey(boneName))
            {
                var boneData = new WingBoneData
                {
                    bone = boneCache[boneName],
                    boneName = boneName,
                    extendedRotation = boneCache[boneName].localRotation, // Current pose as extended
                    foldedRotation = boneCache[boneName].localRotation    // Will be set when captured
                };
                rightWingBones.Add(boneData);
                // Debug.Log($"Added right wing bone: {boneName}");
            }
            else
            {
                // Debug.LogWarning($"Right wing bone not found: {boneName}");
            }
        }

        // Debug.Log($"Setup complete: {leftWingBones.Count} left bones, {rightWingBones.Count} right bones");

        // Cache wrist, forearm, and shoulder bones for automatic control
        if (boneCache.ContainsKey("WingWrist.L"))
            leftWristBone = boneCache["WingWrist.L"];
        if (boneCache.ContainsKey("WingWrist.R"))
            rightWristBone = boneCache["WingWrist.R"];
        if (boneCache.ContainsKey("WingForeArm.L"))
            leftForearmBone = boneCache["WingForeArm.L"];
        if (boneCache.ContainsKey("WingForeArm.R"))
            rightForearmBone = boneCache["WingForeArm.R"];
        if (boneCache.ContainsKey("WingShoulder.L"))
            leftShoulderBone = boneCache["WingShoulder.L"];
        if (boneCache.ContainsKey("WingShoulder.R"))
            rightShoulderBone = boneCache["WingShoulder.R"];
            
        // Store base rotations for shoulders
        if (leftShoulderBone != null)
            leftShoulderBaseRotation = leftShoulderBone.localRotation;
        if (rightShoulderBone != null)
            rightShoulderBaseRotation = rightShoulderBone.localRotation;
        shoulderRotationsInitialized = true;
    }

    void LateUpdate()
    {
        if (!isInitialized) return;
        
        // Apply shoulder rotations if automatic control is enabled
        if (automaticControl && shoulderRotationsInitialized)
        {
            if (leftShoulderBone != null)
            {
                // Apply rotation around the bone's own local Y axis (inverted for left wing)
                Quaternion additiveRotation = Quaternion.Euler(0, -currentLeftShoulderRotation, 0);
                leftShoulderBone.localRotation = leftShoulderBaseRotation * additiveRotation;
            }
            
            if (rightShoulderBone != null)
            {
                // Apply rotation around the bone's own local Y axis
                Quaternion additiveRotation = Quaternion.Euler(0, currentRightShoulderRotation, 0);
                rightShoulderBone.localRotation = rightShoulderBaseRotation * additiveRotation;
            }
        }

        // Animate wings - curve is now integrated into the finger animation
        AnimateWing(leftWingBones, leftWingFold);
        AnimateWing(rightWingBones, rightWingFold);
    }

    void AnimateWing(List<WingBoneData> bones, float foldAmount)
    {
        if (useProceduralAnimation)
        {
            // Procedural wing folding
            AnimateWingProcedural(bones, foldAmount);
        }
        else
        {
            // Use captured poses
            foreach (var boneData in bones)
            {
                if (boneData.bone == null) continue;

                // Smoothly interpolate between extended and folded rotations
                boneData.bone.localRotation = Quaternion.Slerp(
                    boneData.extendedRotation,
                    boneData.foldedRotation,
                    foldAmount
                );
            }
        }
    }

    // Cache for child bone base rotations to avoid spinning
    private Dictionary<Transform, Quaternion> childBoneBaseRotations = new Dictionary<Transform, Quaternion>();
    
    // Cache for storing velocity per wing for finger tip calculations
    private float cachedLeftVelocity = 0f;
    private float cachedRightVelocity = 0f;

    void ApplyCurveToChildBones(Transform parentBone, float baseCurveDegrees, float wingVelocity, float manualOverride = 0f)
    {
        // Apply progressive curve to child bones (finger segments)
        // Each segment gets increasingly more curve for realistic aerodynamic bending
        // 
        // wingVelocity reference: calculated in the target's parent local space
        // (relative to dragon's orientation, not world space)
        int childIndex = 0;
        int totalChildren = 0;
        
        // Count total children first
        foreach (Transform child in parentBone)
        {
            if (child.name.StartsWith("Bone.")) totalChildren++;
        }
        
        foreach (Transform child in parentBone)
        {
            // Skip non-bone children
            if (!child.name.StartsWith("Bone.")) continue;
            
            // Store base rotation on first encounter
            if (!childBoneBaseRotations.ContainsKey(child))
            {
                childBoneBaseRotations[child] = child.localRotation;
            }
            
            childIndex++;
            
            // Progressive curve increase: each segment curves more than the previous
            // Segment 1: 25%, Segment 2: 35%, Segment 3: 40%
            // This creates a natural increasing bend toward the wing tip
            float segmentMultiplier;
            if (childIndex == 1)
                segmentMultiplier = 0.25f; // First segment - least curve
            else if (childIndex == 2)
                segmentMultiplier = 0.35f; // Middle segment - more curve
            else
                segmentMultiplier = 0.40f; // Final segment - most curve
            
            float segmentCurve = baseCurveDegrees * segmentMultiplier;
            
            // No finger tip adjustment here - only apply to the last grandchild
            Quaternion curveRot = Quaternion.Euler(segmentCurve, 0, 0);
            
            // Apply curve to the BASE rotation, not the current rotation
            child.localRotation = childBoneBaseRotations[child] * curveRot;
            
            // Recursively process grandchildren
            if (child.childCount > 0)
            {
                int grandchildIndex = 0;
                int totalGrandchildren = 0;
                
                // Count total grandchildren first
                foreach (Transform gc in child)
                {
                    if (gc.name.StartsWith("Bone.")) totalGrandchildren++;
                }
                
                foreach (Transform grandchild in child)
                {
                    if (!grandchild.name.StartsWith("Bone.")) continue;
                    
                    grandchildIndex++;
                    
                    // Store base rotation
                    if (!childBoneBaseRotations.ContainsKey(grandchild))
                    {
                        childBoneBaseRotations[grandchild] = grandchild.localRotation;
                    }
                    
                    // Apply curve to this level (50% of base)
                    float grandchildCurve = baseCurveDegrees * 0.50f;
                    
                    // Apply finger tip aerodynamic effect ONLY to the last grandchild bone
                    // Structure: Wrist -> WingBone -> Bone.001 -> Bone.002 (last one is tip)
                    float grandchildTipAdjustment = 0f;
                    if (grandchildIndex == totalGrandchildren && totalGrandchildren > 0)
                    {
                        if (manualOverride != 0f)
                        {
                            // Use manual override for testing
                            grandchildTipAdjustment = manualOverride;
                        }
                        else if (wingVelocity < 0)
                        {
                            // During downstroke, finger tips curve upward (negative rotation)
                            float downstrokeStrength = Mathf.Abs(wingVelocity) * 6f;
                            grandchildTipAdjustment = -Mathf.Min(downstrokeStrength, maxFingerTipRotation);
                        }
                    }
                    
                    Quaternion grandchildCurveRot = Quaternion.Euler(grandchildCurve + grandchildTipAdjustment, 0, 0);
                    grandchild.localRotation = childBoneBaseRotations[grandchild] * grandchildCurveRot;
                }
            }
        }
    }

    void AnimateWingProcedural(List<WingBoneData> bones, float foldAmount)
    {
        bool isLeftWing = bones.Count > 0 && bones[0].boneName.Contains(".L");
        
        // Cache velocity for this wing so finger tips can use it
        if (isLeftWing)
            cachedLeftVelocity = leftTargetVerticalVelocity;
        else
            cachedRightVelocity = rightTargetVerticalVelocity;
        
        // Use manual curve test when automatic control is off
        float curveAmount;
        if (!automaticControl)
        {
            curveAmount = manualCurveTest;
        }
        else
        {
            curveAmount = isLeftWing ? currentLeftCurve : currentRightCurve;
        }
        
        // Reduce curve as wing folds - no curve when fully folded
        // This prevents the curve from interfering with the folded wing pose
        curveAmount *= (1f - foldAmount);

        foreach (var boneData in bones)
        {
            if (boneData.bone == null) continue;

            // Skip shoulder, elbow, and forearm if IK is controlling them
            if (useIKForFlapping && (boneData.boneName.Contains("Shoulder") || 
                                      boneData.boneName.Contains("Elbow") || 
                                      boneData.boneName.Contains("ForeArm")))
            {
                continue;
            }

            // Get the base (extended) rotation
            Quaternion baseRotation = boneData.extendedRotation;
            Quaternion foldRotation = baseRotation;

            // Apply different rotations based on bone type
            if (boneData.boneName.Contains("Wrist"))
            {
                float foldAngle = isLeftWing ? leftWristFold : rightWristFold;
                foldRotation = baseRotation * Quaternion.Euler(0, 0, foldAngle * foldAmount);
            }
            else if (boneData.boneName.Contains("WingBone"))
            {
                // Extract finger number
                int fingerNum = 0;
                for (int i = 1; i <= 6; i++)
                {
                    if (boneData.boneName.Contains($"WingBone{i}"))
                    {
                        fingerNum = i;
                        break;
                    }
                }

                // Progressive finger fold - creates fan effect
                float fingerSpread = isLeftWing ? leftFingerSpread : rightFingerSpread;
                float fingerSpacing = isLeftWing ? leftFingerSpacing : rightFingerSpacing;
                float fingerFold = (fingerSpread + (fingerNum * fingerSpacing)) * foldAmount;
                
                // Calculate curve for this finger
                // Bone 1 = farthest from body (outer edge) = maximum curve (2.0x)
                // Bone 6 = closest to body = minimal curve (0.1x)
                // So we reverse the lerp
                float fingerCurveMultiplier = Mathf.Lerp(2.0f, 0.1f, (fingerNum - 1) / 5.0f);
                float baseCurveDegrees = curveAmount * 3.0f * fingerCurveMultiplier;
                
                // Apply curve to this bone (first segment of finger)
                Quaternion curveRotation = Quaternion.Euler(baseCurveDegrees * 0.3f, 0, 0);
                foldRotation = baseRotation * curveRotation * Quaternion.Euler(0, 0, fingerFold);
                
                // Apply progressive curve to child bones for smooth bend
                // Pass the current wing's velocity to the function
                float wingVelocity = automaticControl ? (isLeftWing ? cachedLeftVelocity : cachedRightVelocity) : 0f;
                float fingerTipOverride = automaticControl ? 0f : manualFingerTipTest;
                ApplyCurveToChildBones(boneData.bone, baseCurveDegrees, wingVelocity, fingerTipOverride);
            }

            boneData.bone.localRotation = foldRotation;
        }
    }

    // Pose capture methods
    public void CaptureExtendedPose()
    {
        if (!isInitialized)
        {
            // Debug.LogWarning("Initialize first!");
            return;
        }

        foreach (var bone in leftWingBones)
        {
            bone.extendedRotation = bone.bone.localRotation;
        }

        foreach (var bone in rightWingBones)
        {
            bone.extendedRotation = bone.bone.localRotation;
        }

        // Debug.Log("✓ Captured EXTENDED wing pose!");
    }

    public void CaptureFoldedPose()
    {
        if (!isInitialized)
        {
            // Debug.LogWarning("Initialize first!");
            return;
        }

        foreach (var bone in leftWingBones)
        {
            bone.foldedRotation = bone.bone.localRotation;
        }

        foreach (var bone in rightWingBones)
        {
            bone.foldedRotation = bone.bone.localRotation;
        }

        // Debug.Log("✓ Captured FOLDED wing pose!");
    }

    // Public API methods
    public void ExtendWings()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFold(0f, 0f));
    }

    public void FoldWings()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFold(1f, 1f));
    }

    public void ExtendLeftWing()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFold(0f, rightWingFold));
    }

    public void FoldLeftWing()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFold(1f, rightWingFold));
    }

    public void ExtendRightWing()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFold(leftWingFold, 0f));
    }

    public void FoldRightWing()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFold(leftWingFold, 1f));
    }


    System.Collections.IEnumerator AnimateFold(float targetLeft, float targetRight)
    {
        while (Mathf.Abs(leftWingFold - targetLeft) > 0.01f || Mathf.Abs(rightWingFold - targetRight) > 0.01f)
        {
            leftWingFold = Mathf.MoveTowards(leftWingFold, targetLeft, Time.deltaTime * animationSpeed);
            rightWingFold = Mathf.MoveTowards(rightWingFold, targetRight, Time.deltaTime * animationSpeed);
            yield return null;
        }
        leftWingFold = targetLeft;
        rightWingFold = targetRight;
    }

    // Public getters for editor display
    public float GetCurrentLeftCurve() => currentLeftCurve;
    public float GetCurrentRightCurve() => currentRightCurve;
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ToothlessWingController))]
public class ToothlessWingControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ToothlessWingController controller = (ToothlessWingController)target;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Setup", UnityEditor.EditorStyles.boldLabel);

        if (GUILayout.Button("Initialize"))
        {
            controller.Initialize();
        }

        UnityEditor.EditorGUILayout.Space();
        
        // Show automatic control status
        if (controller.automaticControl)
        {
            UnityEditor.EditorGUILayout.HelpBox(
                "AUTOMATIC CONTROL ENABLED\n\n" +
                "Wings will automatically fold/extend based on IK target distance from shoulders.\n" +
                "Natural curve will be applied based on target vertical position.\n\n" +
                "Disable 'Automatic Control' above to use manual sliders.",
                UnityEditor.MessageType.Info);

            // Show debug info
            UnityEditor.EditorGUILayout.LabelField("Current State:", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.LabelField($"Left Fold: {controller.leftWingFold:F2} | Curve: {controller.GetCurrentLeftCurve():F2}°");
            UnityEditor.EditorGUILayout.LabelField($"Right Fold: {controller.rightWingFold:F2} | Curve: {controller.GetCurrentRightCurve():F2}°");
            
            if (controller.leftWingTarget == null || controller.rightWingTarget == null)
            {
                UnityEditor.EditorGUILayout.HelpBox(
                    "⚠️ IK Targets not assigned! Assign LeftWingTarget and RightWingTarget references.",
                    UnityEditor.MessageType.Warning);
            }
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox(
                "Manual control mode - use the sliders above to control wing folding.",
                UnityEditor.MessageType.Info);
        }

        UnityEditor.EditorGUILayout.Space();
        
        if (controller.useProceduralAnimation)
        {
            UnityEditor.EditorGUILayout.LabelField("Procedural Animation Mode", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.HelpBox(
                "Using procedural folding! Adjust the fold parameters above to customize the animation.\n\n" +
                "Tip: Use the sliders below to preview the animation, then tweak the fold angles in the inspector.",
                UnityEditor.MessageType.Info);
        }
        else
        {
            UnityEditor.EditorGUILayout.LabelField("Pose Capture Mode", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.HelpBox(
                "1. Pose the wings EXTENDED in the scene\n" +
                "2. Click 'Capture Extended Pose'\n" +
                "3. Pose the wings FOLDED in the scene\n" +
                "4. Click 'Capture Folded Pose'\n" +
                "5. Use the sliders to test!",
                UnityEditor.MessageType.Info);

            if (GUILayout.Button("Capture Extended Pose"))
            {
                controller.CaptureExtendedPose();
            }

            if (GUILayout.Button("Capture Folded Pose"))
            {
                controller.CaptureFoldedPose();
            }
        }

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Wing Controls", UnityEditor.EditorStyles.boldLabel);

        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Extend All"))
        {
            controller.ExtendWings();
        }
        if (GUILayout.Button("Fold All"))
        {
            controller.FoldWings();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();

        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Extend Left"))
        {
            controller.ExtendLeftWing();
        }
        if (GUILayout.Button("Fold Left"))
        {
            controller.FoldLeftWing();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();

        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Extend Right"))
        {
            controller.ExtendRightWing();
        }
        if (GUILayout.Button("Fold Right"))
        {
            controller.FoldRightWing();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();


    }
}
#endif
