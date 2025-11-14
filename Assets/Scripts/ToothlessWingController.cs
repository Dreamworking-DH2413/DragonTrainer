using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple wing controller for Toothless that uses the imported bone hierarchy
/// No JSON needed - works directly with Unity transforms
/// </summary>
public class ToothlessWingController : MonoBehaviour
{
    [Header("Control")]
    [Range(0f, 1f)]
    [Tooltip("0 = Wings Extended, 1 = Wings Folded")]
    public float leftWingFold = 0f;
    
    [Range(0f, 1f)]
    [Tooltip("0 = Wings Extended, 1 = Wings Folded")]
    public float rightWingFold = 0f;

    [Header("Flapping Control")]
    [Range(-1f, 1f)]
    [Tooltip("Rotate elbow to flap: -1 = down, 0 = neutral, 1 = up")]
    public float leftWingFlap = 0f;
    
    [Range(-1f, 1f)]
    [Tooltip("Rotate elbow to flap: -1 = down, 0 = neutral, 1 = up")]
    public float rightWingFlap = 0f;
    
    [Range(0f, 90f)]
    [Tooltip("Maximum flap angle in degrees")]
    public float maxFlapAngle = 45f;

    [Header("Forward/Backward Movement")]
    [Range(-1f, 1f)]
    [Tooltip("Move wing forward/backward: -1 = backward, 0 = neutral, 1 = forward")]
    public float leftWingForwardBack = 0f;
    
    [Range(-1f, 1f)]
    [Tooltip("Move wing forward/backward: -1 = backward, 0 = neutral, 1 = forward")]
    public float rightWingForwardBack = 0f;
    
    [Range(0f, 90f)]
    [Tooltip("Maximum forward/backward rotation angle in degrees")]
    public float maxForwardBackAngle = 30f;

    [Range(0.1f, 5f)]
    public float animationSpeed = 1f;

    [Header("Setup")]
    public Transform armatureRoot;
    public bool autoFindArmature = true;

    [Header("Procedural Animation")]
    [Tooltip("Use procedural folding instead of captured poses")]
    public bool useProceduralAnimation = true;

    [Header("Fold Parameters - Left Wing")]
    public float leftShoulderFold = 0f;
    public float leftElbowFold = 65f;
    public float leftForearmFold = -130f;
    public float leftWristFold = 120f;
    public float leftFingerSpread = 40f;
    public float leftFingerSpacing = -18f;

    [Header("Fold Parameters - Right Wing")]
    public float rightShoulderFold = 0f;
    public float rightElbowFold = -65f;  // Opposite direction for right side
    public float rightForearmFold = 130f;
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
            Debug.LogError("Armature root not found! Please assign it manually.");
            return;
        }

        // Build bone cache
        boneCache.Clear();
        BuildBoneCache(armatureRoot);
        Debug.Log($"Built bone cache with {boneCache.Count} bones");

        // Setup wing bones
        SetupWingBones();

        isInitialized = true;
        Debug.Log("Wing controller initialized!");
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
                Debug.Log($"Added left wing bone: {boneName}");
            }
            else
            {
                Debug.LogWarning($"Left wing bone not found: {boneName}");
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
                Debug.Log($"Added right wing bone: {boneName}");
            }
            else
            {
                Debug.LogWarning($"Right wing bone not found: {boneName}");
            }
        }

        Debug.Log($"Setup complete: {leftWingBones.Count} left bones, {rightWingBones.Count} right bones");
    }

    void LateUpdate()
    {
        if (!isInitialized) return;

        // Animate wings
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

    void AnimateWingProcedural(List<WingBoneData> bones, float foldAmount)
    {
        bool isLeftWing = bones.Count > 0 && bones[0].boneName.Contains(".L");

        foreach (var boneData in bones)
        {
            if (boneData.bone == null) continue;

            // Get the base (extended) rotation
            Quaternion baseRotation = boneData.extendedRotation;
            Quaternion foldRotation = baseRotation;

            // Apply different rotations based on bone type
            // Wings fold vertically (Z-axis rotation for up/down movement)
            if (boneData.boneName.Contains("Shoulder"))
            {
                float foldAngle = isLeftWing ? leftShoulderFold : rightShoulderFold;
                float forwardBackValue = isLeftWing ? leftWingForwardBack : rightWingForwardBack;
                float forwardBackAngle = forwardBackValue * maxForwardBackAngle;
                
                // Invert the angle for right wing since it's mirrored
                if (!isLeftWing)
                {
                    forwardBackAngle = -forwardBackAngle;
                }
                
                // Apply fold on Z-axis and forward/back on ?-axis
                foldRotation = baseRotation 
                               * Quaternion.Euler(0, 0, foldAngle * foldAmount)
                               * Quaternion.Euler(0, 0, forwardBackAngle);
            }
            else if (boneData.boneName.Contains("Elbow"))
            {
                // Flap and fold should be independent - flap in body space, fold in local space
                float foldAngle = isLeftWing ? leftElbowFold : rightElbowFold;
                float flapValue = isLeftWing ? leftWingFlap : rightWingFlap;
                float flapAngle = flapValue * maxFlapAngle;
                
                // Invert the angle for right wing since it's mirrored
                if (!isLeftWing)
                {
                    flapAngle = -flapAngle;
                }
                
                // Calculate the world rotation we want (base + body space flap)
                Quaternion targetWorldRotation = armatureRoot.rotation * Quaternion.Euler(0, 0, flapAngle) * 
                                                 Quaternion.Inverse(armatureRoot.rotation) * 
                                                 boneData.bone.parent.rotation * baseRotation;
                
                // Convert to local space
                Quaternion flapInLocal = Quaternion.Inverse(boneData.bone.parent.rotation) * targetWorldRotation;
                
                // Now apply the fold on top in local space
                foldRotation = flapInLocal * Quaternion.Euler(0, 0, foldAngle * foldAmount);
            }
            else if (boneData.boneName.Contains("ForeArm"))
            {
                float foldAngle = isLeftWing ? leftForearmFold : rightForearmFold;
                foldRotation = baseRotation * Quaternion.Euler(0, 0, foldAngle * foldAmount);
            }
            else if (boneData.boneName.Contains("Wrist"))
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
            Debug.LogWarning("Initialize first!");
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

        Debug.Log("✓ Captured EXTENDED wing pose!");
    }

    public void CaptureFoldedPose()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Initialize first!");
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

        Debug.Log("✓ Captured FOLDED wing pose!");
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

    // Flapping methods
    public void StartFlapping(float frequency = 2f, float intensity = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(FlapCoroutine(frequency, intensity));
    }

    public void StopFlapping()
    {
        StopAllCoroutines();
        leftWingFlap = 0f;
        rightWingFlap = 0f;
    }

    // Forward/backward sweep methods
    public void StartWingSweep(float frequency = 1f, float intensity = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(SweepCoroutine(frequency, intensity));
    }

    public void StopWingSweep()
    {
        StopAllCoroutines();
        leftWingForwardBack = 0f;
        rightWingForwardBack = 0f;
    }

    System.Collections.IEnumerator SweepCoroutine(float frequency, float intensity)
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * frequency;
            
            // Sine wave for smooth sweeping motion
            float sweepValue = Mathf.Sin(time * Mathf.PI * 2f) * intensity;
            
            leftWingForwardBack = sweepValue;
            rightWingForwardBack = sweepValue;
            
            yield return null;
        }
    }

    System.Collections.IEnumerator FlapCoroutine(float frequency, float intensity)
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * frequency;
            
            // Sine wave for smooth flapping motion
            float flapValue = Mathf.Sin(time * Mathf.PI * 2f) * intensity;
            
            leftWingFlap = flapValue;
            rightWingFlap = flapValue;
            
            yield return null;
        }
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

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Animation Controls", UnityEditor.EditorStyles.boldLabel);

        // Flapping controls
        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Flapping"))
        {
            controller.StartFlapping(2f, 1f);
        }
        if (GUILayout.Button("Stop Flapping"))
        {
            controller.StopFlapping();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();

        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Flap Slow"))
        {
            controller.StartFlapping(1f, 0.7f);
        }
        if (GUILayout.Button("Flap Fast"))
        {
            controller.StartFlapping(4f, 1f);
        }
        UnityEditor.EditorGUILayout.EndHorizontal();

        // Wing sweep controls
        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Sweep"))
        {
            controller.StartWingSweep(1f, 1f);
        }
        if (GUILayout.Button("Stop Sweep"))
        {
            controller.StopWingSweep();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();

        // Set flap states
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Set Flap States", UnityEditor.EditorStyles.boldLabel);
        
        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Flap Up"))
        {
            controller.leftWingFlap = -1f;
            controller.rightWingFlap = -1f;
        }
        if (GUILayout.Button("Flap Neutral"))
        {
            controller.leftWingFlap = 0f;
            controller.rightWingFlap = 0f;
        }
        if (GUILayout.Button("Flap Down"))
        {
            controller.leftWingFlap = 1f;
            controller.rightWingFlap = 1f;
        }
        UnityEditor.EditorGUILayout.EndHorizontal();

        // Set forward/back states
        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Forward"))
        {
            controller.leftWingForwardBack = -1f;
            controller.rightWingForwardBack = -1f;
        }
        if (GUILayout.Button("Neutral"))
        {
            controller.leftWingForwardBack = 0f;
            controller.rightWingForwardBack = 0f;
        }
        if (GUILayout.Button("Backward"))
        {
            controller.leftWingForwardBack = 1f;
            controller.rightWingForwardBack = 1f;
        }
        UnityEditor.EditorGUILayout.EndHorizontal();
    }
}
#endif
