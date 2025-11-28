# Troubleshooting: Can't Find CorridorViewpointDetector

## Problem: Component Not Showing in Unity

If you don't see "CorridorViewpointDetector" in the Add Component menu, try these steps:

---

## Solution 1: Wait for Unity to Compile

1. **Check Unity Console** (Window → General → Console)
2. Look for **compilation errors** (red text)
3. If there are errors, fix them first
4. Wait for Unity to finish compiling (bottom-right corner shows "Compiling..." then disappears)

**How to check:**
- Look at bottom-right of Unity Editor
- Should say "Compiling..." then disappear when done
- If stuck on "Compiling...", there's likely a compilation error

---

## Solution 2: Force Reimport Script

1. In **Project** window, navigate to: `Assets/Scripts/Points/`
2. Find **`CorridorViewpointDetector.cs`**
3. **Right-click** on the file
4. Select **"Reimport"**
5. Wait for Unity to recompile

---

## Solution 3: Check File Location

**Correct Location:**
```
Assets/
└── Scripts/
    └── Points/
        └── CorridorViewpointDetector.cs  ← Should be here
```

**If file is in wrong location:**
1. Move it to `Assets/Scripts/Points/`
2. Unity will auto-detect and compile

---

## Solution 4: Verify Script Content

1. **Double-click** `CorridorViewpointDetector.cs` to open in code editor
2. **Verify** the file starts with:
```csharp
using UnityEngine;

namespace Points
{
    public class CorridorViewpointDetector : MonoBehaviour
    {
        ...
    }
}
```

3. If content looks wrong, the file may be corrupted

---

## Solution 5: Check Namespace

The script is in the `Points` namespace. In Unity's Add Component menu:

1. Click **"Add Component"**
2. Type: **"CorridorViewpointDetector"** (without namespace)
3. OR type: **"Points.CorridorViewpointDetector"** (with namespace)

**Note:** Unity usually shows components without namespace prefix, but try both.

---

## Solution 6: Manual Component Addition

If the component still doesn't show:

1. **Select** a GameObject in Hierarchy
2. In **Inspector**, click **"Add Component"**
3. Type: **"Corridor"** (partial name)
4. Look for **"Corridor Viewpoint Detector"** in the list
5. If still not there, check Console for errors

---

## Solution 7: Check for Compilation Errors

1. **Open Console** (Window → General → Console)
2. Look for **red error messages**
3. Common issues:
   - Missing `using` statements
   - Syntax errors
   - Missing dependencies

**If you see errors:**
- Fix the errors first
- Unity won't show components if scripts have compilation errors

---

## Solution 8: Restart Unity (Last Resort)

1. **Save your scene** (Ctrl+S / Cmd+S)
2. **Close Unity Editor**
3. **Reopen Unity**
4. Wait for compilation to finish
5. Try adding component again

---

## Verification: Is the Script Actually There?

**Check in Project Window:**
1. Navigate to: `Assets/Scripts/Points/`
2. Look for: **`CorridorViewpointDetector.cs`**
3. If you see it, Unity should recognize it

**If file doesn't exist:**
- The script may not have been created
- Check if file was accidentally deleted
- Recreate the file if needed

---

## Quick Test: Create Empty Script

To verify Unity can compile scripts:

1. **Right-click** in Project window → `Create` → `C# Script`
2. Name it: **"TestScript"**
3. **Double-click** to open
4. **Save** (Ctrl+S / Cmd+S)
5. Check if it compiles (no errors in Console)

**If TestScript compiles but CorridorViewpointDetector doesn't:**
- There's likely an error in CorridorViewpointDetector.cs
- Check Console for specific error messages

---

## Still Not Working?

If none of these solutions work:

1. **Check Console** for specific error messages
2. **Share the error message** (if any)
3. **Verify** the file path is correct
4. **Check** that the file isn't in a folder Unity ignores (like hidden folders)

---

## Expected Behavior

Once working, you should be able to:

1. **Select any GameObject**
2. Click **"Add Component"**
3. Type **"Corridor"** or **"Viewpoint"**
4. See **"Corridor Viewpoint Detector"** in the list
5. Click it to add the component

The component will appear in Inspector with these fields:
- Corridor Model
- Manual Corridor Bounds
- Inside Padding
- Player Transform
- Show Debug Gizmos

