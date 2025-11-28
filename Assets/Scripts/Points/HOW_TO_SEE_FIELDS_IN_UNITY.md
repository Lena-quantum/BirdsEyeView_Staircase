# How to See Viewpoint Detector Fields in Unity Inspector

## Problem: Fields Not Showing in Inspector

If you don't see the "Viewpoint-Aware Raycasting" section in `RayDepthController` or `PathModeController`, follow these steps:

---

## Step 1: Check Unity Console for Errors

1. **Open Console**: `Window` → `General` → `Console` (or press `Ctrl+Shift+C` / `Cmd+Shift+C`)
2. **Look for RED errors** (not warnings)
3. **If you see errors**, Unity won't compile the scripts and fields won't show

**Common errors:**
- "The type or namespace name 'CorridorViewpointDetector' could not be found"
- Compilation errors in CorridorViewpointDetector.cs

**Fix:** Make sure `CorridorViewpointDetector.cs` exists and has no errors

---

## Step 2: Force Unity to Recompile

1. **Save all scripts** (Ctrl+S / Cmd+S in your code editor)
2. **Go back to Unity Editor**
3. **Wait for compilation** (check bottom-right corner - should say "Compiling..." then disappear)
4. **If stuck on "Compiling..."**, there's a compilation error - check Console

**Alternative method:**
1. In Unity, go to `Assets` → `Reimport All`
2. Wait for Unity to reimport everything
3. This forces Unity to recompile all scripts

---

## Step 3: Verify Scripts Are in Correct Location

**Check these files exist:**
```
Assets/Scripts/Points/CorridorViewpointDetector.cs  ← Must exist
Assets/Scripts/Points/RayDepthController.cs          ← Must exist
Assets/Scripts/Points/PathModeController.cs         ← Must exist
```

**If any are missing:**
- The fields won't show because Unity can't compile the scripts

---

## Step 4: Check Namespace

All three scripts should be in the `Points` namespace:

**CorridorViewpointDetector.cs:**
```csharp
namespace Points
{
    public class CorridorViewpointDetector : MonoBehaviour
```

**RayDepthController.cs:**
```csharp
namespace Points
{
    public class RayDepthController : MonoBehaviour
```

**PathModeController.cs:**
```csharp
namespace Points
{
    public class PathModeController : MonoBehaviour
```

**If namespaces don't match**, Unity won't be able to reference the types.

---

## Step 5: Where to Find the Fields in Inspector

Once Unity compiles successfully:

1. **Select GameObject** with `RayDepthController` component
2. **Scroll down** in Inspector
3. **Look for section**: **"Viewpoint-Aware Raycasting"**
4. **You should see:**
   - **Viewpoint Detector** (Object field - drag CorridorViewpointDetector here)
   - **Corridor Shell Layer** (LayerMask dropdown)

**Same for PathModeController:**
1. **Select GameObject** with `PathModeController` component
2. **Scroll down** in Inspector
3. **Look for section**: **"Viewpoint-Aware Raycasting"**
4. **Same fields as above**

---

## Step 6: If Fields Still Don't Show

### Option A: Check Script Compilation Status

1. **Select** `RayDepthController.cs` in Project window
2. **Check Inspector** (should show script info, not errors)
3. **If you see errors**, fix them first

### Option B: Reimport Specific Scripts

1. **Right-click** `CorridorViewpointDetector.cs` → **"Reimport"**
2. **Right-click** `RayDepthController.cs` → **"Reimport"**
3. **Right-click** `PathModeController.cs` → **"Reimport"**
4. Wait for Unity to compile

### Option C: Check for Duplicate Scripts

1. **Search** for "RayDepthController" in Project window (Ctrl+F / Cmd+F)
2. **If you find multiple copies**, delete the old ones
3. **Keep only** the one in `Assets/Scripts/Points/`

---

## Step 7: Manual Verification

**Check if the code is actually in the files:**

1. **Open** `RayDepthController.cs` in your code editor
2. **Search for** "Viewpoint-Aware" (Ctrl+F / Cmd+F)
3. **You should see:**
```csharp
// Birds-eye View Feature: Viewpoint-aware raycasting
[Header("Viewpoint-Aware Raycasting")]
[Tooltip("Detector for inside/outside corridor viewpoint...")]
[SerializeField] private CorridorViewpointDetector _viewpointDetector;
```

4. **If you DON'T see this**, the file wasn't updated properly

---

## Quick Test: Create a Simple Test

To verify Unity can compile scripts:

1. **Create new C# script**: Right-click in Project → `Create` → `C# Script`
2. **Name it**: `TestViewpoint.cs`
3. **Open it** and paste:
```csharp
using UnityEngine;
using Points;

public class TestViewpoint : MonoBehaviour
{
    [SerializeField] private CorridorViewpointDetector detector;
}
```

4. **Save** and check Console
5. **If this compiles**, Unity can see CorridorViewpointDetector
6. **If this fails**, there's a problem with CorridorViewpointDetector.cs

---

## Expected Result

Once everything compiles, in Unity Inspector you should see:

```
RayDepthController (Script)
├── ... (existing fields) ...
└── Viewpoint-Aware Raycasting
    ├── Viewpoint Detector: [None (Corridor Viewpoint Detector)]
    └── Corridor Shell Layer: [Nothing]
```

**Then you can:**
1. Drag `CorridorViewpointDetector` GameObject to "Viewpoint Detector" field
2. Select "CorridorShell" from "Corridor Shell Layer" dropdown

---

## Still Not Working?

**Check these:**
1. ✅ Unity Console has NO red errors
2. ✅ All three script files exist in `Assets/Scripts/Points/`
3. ✅ All scripts are in `Points` namespace
4. ✅ Unity finished compiling (no "Compiling..." in bottom-right)
5. ✅ You're looking at the correct GameObject with the component

**If all checked and still not working:**
- Share the error messages from Console
- Check if there are multiple versions of the scripts
- Try restarting Unity

