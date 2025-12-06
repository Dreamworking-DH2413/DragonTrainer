using UnityEngine;
using System.Collections.Generic;

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
    [Tooltip("Degrees of forward cup during downstroke (target below shoulder)")]
    public float downstrokeCurve = 15f;
    
    [Tooltip("Degrees of backward bend during upstroke (target above shoulder)")]
    public float upstrokeCurve = 10f;
    
    [Tooltip("How smoothly curve changes blend")]
    public float curveBlendSpeed = 3f;

    [Header("Setup")]
    public Transform armatureRoot;
    public bool autoFindArmature = true;
    
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
    
    // Cached bone references for curve application
    private Transform leftWristBone;
    private Transform rightWristBone;
    private Transform leftShoulderBone;
    private Transform rightShoulderBone;

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
    }

    void UpdateAutomaticControl()
    {
        // Update left wing
        if (leftWingTarget != null && leftShoulderBone != null)
        {
            float distance = Vector3.Distance(leftWingTarget.position, leftShoulderBone.position);
            float targetFold = CalculateFoldAmount(distance);
            currentLeftFold = Mathf.Lerp(currentLeftFold, targetFold, Time.deltaTime * foldSmoothSpeed);
            leftWingFold = currentLeftFold;

            // Calculate curve based on vertical position
            float verticalOffset = leftWingTarget.position.y - leftShoulderBone.position.y;
            float targetCurve = CalculateCurveAmount(verticalOffset);
            currentLeftCurve = Mathf.Lerp(currentLeftCurve, targetCurve, Time.deltaTime * curveBlendSpeed);
        }

        // Update right wing
        if (rightWingTarget != null && rightShoulderBone != null)
        {
            float distance = Vector3.Distance(rightWingTarget.position, rightShoulderBone.position);
            float targetFold = CalculateFoldAmount(distance);
            currentRightFold = Mathf.Lerp(currentRightFold, targetFold, Time.deltaTime * foldSmoothSpeed);
            rightWingFold = currentRightFold;

            // Calculate curve based on vertical position
            float verticalOffset = rightWingTarget.position.y - rightShoulderBone.position.y;
            float targetCurve = CalculateCurveAmount(verticalOffset);
            currentRightCurve = Mathf.Lerp(currentRightCurve, targetCurve, Time.deltaTime * curveBlendSpeed);
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

    float CalculateCurveAmount(float verticalOffset)
    {
        // Negative offset = target below shoulder = downstroke = forward curve (positive)
        // Positive offset = target above shoulder = upstroke = backward curve (negative)
        
        if (verticalOffset < 0)
        {
            // Downstroke - forward cup
            return Mathf.Clamp(Mathf.Abs(verticalOffset) * 2f, 0f, downstrokeCurve);
        }
        else
        {
            // Upstroke - backward bend
            return Mathf.Clamp(-verticalOffset * 2f, -upstrokeCurve, 0f);
        }
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

        // Cache wrist and shoulder bones for automatic control
        if (boneCache.ContainsKey("WingWrist.L"))
            leftWristBone = boneCache["WingWrist.L"];
        if (boneCache.ContainsKey("WingWrist.R"))
            rightWristBone = boneCache["WingWrist.R"];
        if (boneCache.ContainsKey("WingShoulder.L"))
            leftShoulderBone = boneCache["WingShoulder.L"];
        if (boneCache.ContainsKey("WingShoulder.R"))
            rightShoulderBone = boneCache["WingShoulder.R"];
    }

    void LateUpdate()
    {
        if (!isInitialized) return;

        // Animate wings (fingers)
        AnimateWing(leftWingBones, leftWingFold);
        AnimateWing(rightWingBones, rightWingFold);

        // Apply natural curve to wrists (happens after IK)
        if (automaticControl)
        {
            ApplyNaturalCurve();
        }
    }

    void ApplyNaturalCurve()
    {
        // Apply subtle wrist rotation for natural wing flex
        if (leftWristBone != null)
        {
            // Apply curve as additional local rotation (additive to IK result)
            Quaternion curveRotation = Quaternion.Euler(currentLeftCurve, 0, 0);
            leftWristBone.localRotation *= curveRotation;
        }

        if (rightWristBone != null)
        {
            // Apply curve as additional local rotation (additive to IK result)
            Quaternion curveRotation = Quaternion.Euler(currentRightCurve, 0, 0);
            rightWristBone.localRotation *= curveRotation;
        }
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

    void AnimateWingProcedural(List<WingBoneData> bones, float foldAmount)
    {
        bool isLeftWing = bones.Count > 0 && bones[0].boneName.Contains(".L");

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
                
                foldRotation = baseRotation * Quaternion.Euler(0, 0, fingerFold);
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
            UnityEditor.EditorGUILayout.LabelField($"Left Fold: {controller.leftWingFold:F2}");
            UnityEditor.EditorGUILayout.LabelField($"Right Fold: {controller.rightWingFold:F2}");
            
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
