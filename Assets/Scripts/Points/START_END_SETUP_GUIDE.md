# Start/End Point Setup Guide

## Overview

This guide explains how to set up **Start** and **End** points for your VR drone path experiment. These are pre-placed markers that define where the flight path must begin and end, ensuring consistency across trials.

---

## What Are Start/End Points?

- **Start Point**: A white box on the floor labeled "START" - the drone path MUST begin here
- **End Point**: A black box on the floor labeled "END" - the drone path MUST end here
- **Purpose**: Ensures real-life drone has consistent takeoff/landing positions
- **Validation**: System blocks path building if not connected Start → waypoints → End

---

## Quick Setup (5 Minutes)

### Step 1: Create Start Point

1. **Right-click in Hierarchy** → `3D Object` → `Cube`
2. **Rename** to `StartPoint`
3. **Set Transform**:
   - Position: Your desired start location (e.g., `(1, 0.25, 2)`)
   - Scale: `(0.5, 0.5, 0.5)` (same size as waypoints)
4. **Verify Layer**: 
   - In Inspector, **Layer** should be `Default` (Layer 0)
   - ⚠️ This should already be correct, but verify it!
5. **Verify Collider**:
   - Ensure **Box Collider** component is present (it should be by default)
6. **Set Color**:
   - In Inspector, find the **Mesh Renderer** component
   - Expand **Materials** → **Element 0**
   - Set **Color** to **White** (`#FFFFFF`)
7. **Add Component** → `Start End Point` (script)
8. In Inspector, set **Point Type** to `Start`

### Step 2: Add Text Label to Start Point

1. **Right-click `StartPoint`** → `3D Object` → `3D Text` (TextMeshPro)
   - If prompted to import TMP Essentials, click **Import**
2. **Rename** to `StartLabel`
3. **Set Transform**:
   - Position: `(0, 0.55, 0)` (above the box)
   - Rotation: `(0, 0, 0)`
   - Scale: `(0.1, 0.1, 0.1)`
4. **Configure TextMeshPro**:
   - Text: `START`
   - Font Size: `32`
   - Color: **Black** (`#000000`) for contrast
   - Alignment: Center/Center
   - Auto Size: Off

### Step 3: Create End Point (Same Process)

1. **Duplicate `StartPoint`**: Select it, press `Ctrl+D`
2. **Rename** to `EndPoint`
3. **Set Transform Position** to your desired ending location (e.g., `(10, 0.25, 15)`)
4. **Verify Layer**: Should already be `Default` (inherited from duplication)
5. **Select `EndPoint`** → In Inspector, change **Point Type** to `End`
6. **Update Visual**:
   - In Mesh Renderer, set **Color** to **Black** (`#000000`)
   - Select `StartLabel` child → Rename to `EndLabel`, change text to `END`, color to **White** (`#FFFFFF`)

### Step 4: Assign References in Inspector

1. **Select `StartPoint`** in Hierarchy
2. In **Start End Point** component:
   - Drag `StartPoint` itself → **Box Renderer** field (it has the MeshRenderer)
   - Drag `StartLabel` → **Label** field
3. **Repeat for `EndPoint`**:
   - Drag `EndPoint` itself → **Box Renderer** field
   - Drag `EndLabel` → **Label** field

---

## Verification Checklist

✅ **Start Point**:
- [ ] GameObject named `StartPoint` exists
- [ ] `StartEndPoint` component attached with Type = `Start`
- [ ] White box visual (0.5 × 0.5 × 0.5)
- [ ] "START" label in black text above box
- [ ] Positioned on floor (Y = 0)
- [ ] Box Renderer and Label fields assigned

✅ **End Point**:
- [ ] GameObject named `EndPoint` exists
- [ ] `StartEndPoint` component attached with Type = `End`
- [ ] Black box visual (0.5 × 0.5 × 0.5)
- [ ] "END" label in white text above box
- [ ] Positioned on floor (Y = 0)
- [ ] Box Renderer and Label fields assigned

✅ **System Integration**:
- [ ] `FlightPathManager` automatically finds Start/End on scene load
- [ ] Check Console for "Start point registered with ID -1" message
- [ ] Check Console for "End point registered with ID -2" message

---

## Testing the System

### Test 1: Valid Path
1. **Enter Play Mode**
2. **Enable Path Mode** (Right Grip button)
3. **Place a waypoint** anywhere
4. **Try to connect** waypoint → waypoint
   - ❌ Should show error: "Path must start at Start point"
5. **Point at Start box** and **press Trigger**
6. **Point at your waypoint** and **press Trigger**
   - ✅ Should connect successfully
7. **Place another waypoint** and connect
8. **Point at End box** and **press Trigger**
   - ✅ Should connect successfully
9. **Path should render**: Start → Waypoint1 → Waypoint2 → End

### Test 2: Invalid Connections
1. **Try to connect** Start → Start
   - ❌ "Start point already connected"
2. **Try to connect** Waypoint → Start
   - ❌ "Cannot connect to Start point"
3. **Try to connect** End → Waypoint
   - ❌ "Cannot connect from End point"

---

## Path Building Rules

### ✅ Valid Actions
- **First connection**: Start → Waypoint
- **Middle connections**: Waypoint → Waypoint (sequential)
- **Last connection**: Waypoint → End
- **Example valid path**: Start → A → B → C → End

### ❌ Invalid Actions
- Starting from a waypoint (must start from Start)
- Connecting TO Start point
- Connecting FROM End point
- Multiple connections from Start (only ONE allowed)
- Multiple connections to End (only ONE allowed)

---

## Path Validation

### Real-Time Validation
- System checks EVERY segment before creation
- Error messages appear in VR as floating UI panels
- Haptic feedback (weak pulse) on blocked actions

### On Trial Submit
- Path completeness is validated
- Start/End positions saved to JSON data
- Example JSON output:
```json
{
  "pathMetrics": {
    "startPosition": {"x": 1.0, "y": 0.0, "z": 2.0},
    "endPosition": {"x": 10.0, "y": 0.0, "z": 15.0},
    "totalLength3D": 15.3,
    ...
  }
}
```

---

## Customization

### Changing Start/End Positions Per Task Variant

**Option A: Duplicate Entire Build**
- Create 4 separate Unity builds (one per variant)
- Each build has Start/End at different positions
- Simplest approach for your 4-variant study

**Option B: Multiple Start/End Pairs in One Scene**
- Create 4 parent GameObjects: `StartEnd_Corridor`, `StartEnd_Staircase`, etc.
- Each contains its own Start/End pair
- Enable only the relevant pair before trial starts
- More complex but keeps everything in one build

### Changing Visual Style

**Box Size**:
- Select `StartBox` or `EndBox`
- Adjust Scale (e.g., `(0.7, 0.7, 0.7)` for larger)

**Colors**:
- Select box material in Project
- Change Albedo color

**Label Text**:
- Select `StartLabel` or `EndLabel`
- Change Font, Size, or Text content

**No Vertical Beam** (as requested):
- Start/End points do NOT have vertical lines through them
- Only Record360 waypoints have vertical guides

---

## Troubleshooting

### "No Start point found in scene"
- **Cause**: `StartPoint` GameObject missing or `StartEndPoint` component not attached
- **Fix**: Follow Step 1-3 above

### "Multiple Start points found in scene"
- **Cause**: More than one GameObject with `StartEndPoint` Type = Start
- **Fix**: Delete duplicate Start points, keep only one

### Start/End not visible in VR
- **Cause**: Box renderer not assigned or material missing
- **Fix**: Check Inspector → Box Renderer field is assigned, material is on box

### Error messages not showing
- **Cause**: `PathWarningPopup` not assigned in `PathModeController`
- **Fix**: Select `PathModeController` in Hierarchy → Assign Warning Popup field

### Path renders but skips Start/End
- **Cause**: Old code treating negative IDs as breaks
- **Fix**: Ensure you're using latest `PathRenderer.cs` with `GetPointPosition()` method

### Start/End positions not in JSON
- **Cause**: Old `ExperimentDataManager` without Start/End tracking
- **Fix**: Ensure `PathMetrics` has `startPosition` and `endPosition` fields

---

## Advanced: Programmatic Setup

If you want to create Start/End points via script:

```csharp
using Points;
using UnityEngine;

public class StartEndSetup : MonoBehaviour
{
    public void CreateStartPoint(Vector3 position)
    {
        GameObject startObj = new GameObject("StartPoint");
        startObj.transform.position = position;
        
        var startPoint = startObj.AddComponent<StartEndPoint>();
        // Configure visuals...
    }
}
```

---

## Integration with Experiment System

### Automatic Tracking
- `ExperimentDataManager` automatically captures Start/End positions
- Saved in `pathMetrics.startPosition` and `pathMetrics.endPosition`
- No manual configuration needed

### Data Output Example
```json
{
  "metadata": {
    "participantId": "P01",
    "taskVariant": "RoomView_Corridor"
  },
  "pathMetrics": {
    "startPosition": {"x": 1.0, "y": 0.0, "z": 2.0},
    "endPosition": {"x": 10.0, "y": 0.0, "z": 15.0},
    "totalLength3D": 15.3,
    "segmentCount": 3
  }
}
```

---

## Summary

1. ✅ Create `StartPoint` and `EndPoint` GameObjects
2. ✅ Add `StartEndPoint` component to each
3. ✅ Add box visual (white for Start, black for End)
4. ✅ Add text label ("START" / "END")
5. ✅ Position on floor at desired locations
6. ✅ Assign references in Inspector
7. ✅ Test path building with validation
8. ✅ Verify data saved to JSON

**You're done!** 🎉

The system will now enforce Start → waypoints → End path structure and track Start/End positions in experiment data.

---

## Questions?

- Check Console logs for registration messages
- Use Scene view Gizmos to visualize Start/End (green/red wireframe cubes)
- Test thoroughly before running actual trials

**Good luck with your user study!** 🚀

