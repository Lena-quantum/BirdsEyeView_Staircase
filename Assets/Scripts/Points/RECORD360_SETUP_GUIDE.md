# Record360 Two-Point System - Unity Setup Guide

## Quick Setup Instructions

Follow these steps to set up the new Record360 two-point placement system in your Unity scene.

---

## 1. Create the Recording Ghost Sphere

The recording ghost is the second red sphere that slides along the vertical line.

### Steps:
1. In the Hierarchy, find your existing **Ghost** sphere under the point placement system
2. **Duplicate** the Ghost sphere (Ctrl+D or Cmd+D)
3. **Rename** it to `RecordingGhost`
4. **Set its parent** to the same parent as the main Ghtreost (or to the PointPlacementManager GameObject)
5. **Position** it at (0, 0, 0) relative to its parent
6. **Set layer** to the same as the main Ghost
7. **Disable** it initially (uncheck the checkbox in the Inspector)

### Visual Settings:
- **Scale**: Should match the main Ghost sphere
- **Material**: Should use the same material as the main Ghost
- **Color**: Will be set to red automatically by code

---

## 2. Configure PointPlacementManager

### Steps:
1. **Select** the PointPlacementManager GameObject in the Hierarchy
2. In the Inspector, find the **PointPlacementManager** component
3. You should see new fields added:

#### New Fields to Set:
- **Recording Height Controller**: Leave empty (auto-created at runtime)
- **Recording Ghost Transform**: Drag the `RecordingGhost` GameObject here
- **Recording Ghost Renderer**: Drag the Renderer component from `RecordingGhost` here
  - Tip: Expand RecordingGhost in the Hierarchy, it should have a child with a Renderer

### Existing Fields to Verify:
- **Environment Layer**: Should be set to your Environment layer (used for floor/ceiling detection)
- **Drone Radius**: Should be 0.45m (for collision detection)

---

## 3. Test the Setup

### In Unity Editor (Play Mode):
1. **Put on** your VR headset (or use XR Device Simulator)
2. **Open** the wrist menu
3. **Select** the Record360 waypoint type (red button)
4. **Aim** your right controller and see the red ghost sphere
5. **Press trigger** to place the anchor point
6. **Verify**:
   - ✅ Grey vertical line appears from floor to ceiling
   - ✅ Second red ghost sphere appears on the line
   - ✅ Main ghost sphere is hidden
7. **Move your controller** up and down
8. **Verify**:
   - ✅ Recording ghost slides along the vertical line
   - ✅ It follows your controller aim
   - ✅ It can reach the ceiling
   - ✅ It can reach the floor
9. **Press trigger** to confirm recording height
10. **Verify**:
    - ✅ Red waypoint appears at anchor position
    - ✅ Vertical line disappears
    - ✅ Main ghost reappears
    - ✅ You can place more waypoints

### Test Drone Flight:
1. **Create a path** with at least 3 waypoints (including a Record360)
2. **Place** the Record360 waypoint anchor at ~1m height
3. **Adjust** recording height to ceiling or floor
4. **Complete** the path
5. **Press Play** on the drone
6. **Verify**:
   - ✅ Drone flies to anchor point
   - ✅ Drone pauses briefly
   - ✅ Drone flies vertically to recording height
   - ✅ Drone performs 360° rotation
   - ✅ Drone flies back to anchor
   - ✅ Drone continues to next waypoint

---

## 4. Troubleshooting

### Recording Ghost Doesn't Appear
- **Check**: Is `RecordingGhost` assigned in PointPlacementManager?
- **Check**: Is `RecordingGhostRenderer` assigned?
- **Check**: Is RecordingGhost enabled in the Hierarchy after placing anchor? (Should be enabled by code)

### Vertical Line Doesn't Appear
- **Check**: Is `RecordingHeightController` component added to PointPlacementManager GameObject?
- **Check**: In the Console, look for log message: "RecordingHeightController: Floor=X, Ceiling=Y"
- **Check**: Is Environment Layer set correctly?

### Recording Ghost Doesn't Follow Controller
- **Check**: RayDepthController is calling `UpdateRecordingPointFromRay()`
- **Check**: In Console, look for "Record360 Anchor placed at..." message

### Vertical Line Wrong Height
- **Check**: Environment Layer includes floor and ceiling objects
- **Check**: Floor/ceiling have colliders
- **Adjust**: `Max Raycast Height` in RecordingHeightController (default 50m)
- **Adjust**: `Default Ceiling Height` as fallback (default 4m)

### Drone Doesn't Fly Vertically
- **Check**: PointHandle has `RecordingPosition` set (check in Inspector during Play mode)
- **Check**: Console shows "DronePathFollower: Record360 waypoint has separate recording position"
- **Check**: Anchor and recording positions are different (vertical distance > 1cm)

### Recording Ghost Wrong Color
- **Should be**: Red (same as Record360 waypoint type)
- **Check**: `UpdateRecordingGhostColor()` is being called in `RecordingHeightController.ActivateAt()`

---

## 5. Optional Configuration

### Vertical Line Appearance
On the **RecordingHeightController** component (auto-created):
- `Line Color`: Change to your preferred guide color (default: grey)
- `Line Width`: Make thicker for better visibility (default: 0.005m = 5mm)

### Floor/Ceiling Detection
- `Max Raycast Height`: Increase if you have very tall ceilings (default: 50m)
- `Default Ceiling Height`: Fallback if no ceiling detected (default: 4m)
- `Floor Offset`: Small offset from floor to avoid z-fighting (default: 0.1m)

### Drone Recording Behavior
On the **DronePathFollower** component:
- `Record Pause Seconds`: How long to pause before/after recording (default: 1s)
- `Record 360 Duration`: How long the full 360° rotation takes (default: 15s)

---

## 6. Scene Hierarchy Structure

Your scene should look something like this:

```
Scene
├── XR Origin (XR Rig)
│   └── ... (player, controllers, etc.)
├── Point System
│   ├── PointPlacementManager
│   │   ├── Ghost (main ghost sphere)
│   │   │   └── Sphere (Renderer)
│   │   ├── RecordingGhost (NEW - recording height ghost)
│   │   │   └── Sphere (Renderer)
│   │   └── RecordingHeightController (auto-created at runtime)
│   ├── FlightPathManager
│   ├── PathRenderer
│   └── Points (parent for placed waypoints)
├── Drone
│   └── DronePathFollower
└── Environment
    ├── Floor (with collider, Environment layer)
    ├── Ceiling (with collider, Environment layer)
    └── Walls (with colliders, Environment layer)
```

---

## 7. Unity Inspector Reference

### PointPlacementManager Component
```
Point Placement Manager (Script)
├── [Placement Settings]
│   ├── Min Depth: 0.2
│   ├── Max Depth: 10
│   └── ... (other settings)
├── [References]
│   ├── Right Hand Ray Origin: RightHandController
│   ├── Ghost Transform: Ghost
│   ├── Ghost Renderer: Ghost/Sphere (Renderer)
│   └── ... (other references)
├── [Collision Avoidance]
│   ├── Drone Radius: 0.45
│   ├── Environment Layer: Environment
│   └── Collision Ghost Color: (0.5, 0.5, 0.5, 0.5)
└── [Record360 Two-Point System] (NEW)
    ├── Recording Height Controller: (auto-created)
    ├── Recording Ghost Transform: RecordingGhost
    └── Recording Ghost Renderer: RecordingGhost/Sphere (Renderer)
```

---

## 8. Build Settings

No changes needed to build settings. The new system works automatically in:
- ✅ Unity Editor
- ✅ Quest 2 (Android build)
- ✅ Other VR platforms

---

## Need Help?

If you encounter issues:
1. Check the **Console** for error messages
2. Enable **Debug.Log** in RecordingHeightController to see floor/ceiling detection
3. Review the full documentation: `RECORD360_FEATURE.md`
4. Check that all components are properly assigned in the Inspector

---

**Happy Recording!** 🎥🚁

