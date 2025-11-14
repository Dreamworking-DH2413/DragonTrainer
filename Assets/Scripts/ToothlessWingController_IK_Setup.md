# Toothless Wing Controller - IK Setup Guide

## Overview
This script now works alongside Unity's IK system. The script **only controls wing folding**, while **IK controls wing flapping**.

## How It Works

### Division of Labor:
- **IK System** → Controls `WingShoulder.L/R`, `WingElbow.L/R`, and `WingForeArm.L/R` bones for **flapping**
- **This Script** → Controls `WingWrist.L/R` and `WingBone1-6.L/R` (finger bones) for **folding**

### Key Setting:
- **Use IK For Flapping** (checkbox in inspector) - When enabled (default), the script will skip animating shoulder and elbow bones, leaving them free for IK control.

## Setup Steps

### 1. Setup IK in Unity:
1. Add IK targets for the wings (empty GameObjects positioned where wing tips should be)
2. Setup your IK system (Unity Animation Rigging, Final IK, or custom IK)
3. Configure IK to control `WingShoulder.L/R` and `WingElbow.L/R` bones
4. Test that IK can move the wings up and down for flapping

### 2. Setup Wing Folding:
1. Add the `ToothlessWingController` script to your dragon GameObject
2. Assign the `Armature Root` (or enable `Auto Find Armature`)
3. Click "Initialize" button in the inspector
4. Make sure **"Use IK For Flapping"** is checked
5. Adjust the fold parameters if needed:
   - `leftForearmFold` / `rightForearmFold` - Controls forearm rotation when folding
   - `leftWristFold` / `rightWristFold` - Controls wrist rotation when folding
   - `leftFingerSpread` / `rightFingerSpread` - Controls how fingers spread out
   - `leftFingerSpacing` / `rightFingerSpacing` - Controls spacing between fingers

### 3. Test:
1. Use IK targets to move wings up and down (flapping motion)
2. Use the `leftWingFold` and `rightWingFold` sliders (0 = extended, 1 = folded) to fold/unfold wings
3. Both systems should work together without fighting each other

## Script Execution Order
The script uses `LateUpdate()` for wing folding animation, which runs after IK updates. This ensures:
1. IK moves the shoulder/elbow bones first (flapping)
2. Then the script folds the forearm/wrist/fingers (folding)

## Public API Methods

```csharp
// Fold/extend wings
ExtendWings()      // Extend both wings
FoldWings()        // Fold both wings
ExtendLeftWing()   // Extend left wing only
FoldLeftWing()     // Fold left wing only
ExtendRightWing()  // Extend right wing only
FoldRightWing()    // Fold right wing only

// Direct control
leftWingFold = 0.5f;  // 0-1 range
rightWingFold = 0.5f; // 0-1 range
```

## Example Usage

```csharp
// Get the wing controller
ToothlessWingController wingController = GetComponent<ToothlessWingController>();

// Fold wings when landing
wingController.FoldWings();

// Extend wings when taking off
wingController.ExtendWings();

// Partially fold (gliding)
wingController.leftWingFold = 0.3f;
wingController.rightWingFold = 0.3f;

// Asymmetric folding (turning)
wingController.FoldLeftWing();
wingController.ExtendRightWing();
```

## Troubleshooting

**Problem:** IK and script are fighting each other
- **Solution:** Make sure "Use IK For Flapping" is enabled

**Problem:** Wings don't fold properly
- **Solution:** Adjust the fold parameter values in the inspector. Different rigs may need different values.

**Problem:** Wings fold but don't flap
- **Solution:** Check that your IK is setup correctly and targeting the shoulder/elbow bones

**Problem:** Wings flap but don't fold
- **Solution:** Make sure the script is initialized. Click the "Initialize" button in the inspector.

## Performance Notes
- The script only animates bones when needed (uses `LateUpdate()`)
- When IK is enabled, shoulder and elbow bones are completely skipped, reducing overhead
- The procedural animation is very efficient - no animation curves or heavy calculations

