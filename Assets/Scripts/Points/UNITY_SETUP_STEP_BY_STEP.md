# Unity Setup: Step-by-Step Guide for Dual Viewpoint System

## Prerequisites
- Unity project with VR setup (OpenXR/Meta Quest)
- Existing waypoint system with `PointPlacementManager`, `RayDepthController`, `PathModeController`
- Corridor/photogrammetry model in scene

---

## STEP 1: Create CorridorShell Layer

1. **Open Unity Editor**
2. **Go to**: `Edit` → `Project Settings` (or `Unity` → `Preferences` on Mac)
3. **Click**: `Tags and Layers` in the left sidebar
4. **Find**: An empty **User Layer** (usually starts at Layer 8)
5. **Type**: `CorridorShell` in the empty field
6. **Note**: The layer index (e.g., "8" if it's the 8th user layer)

**Visual Guide:**
```
Tags and Layers
├── Tags
└── Layers
    ├── Builtin Layers (0-7)
    └── User Layers
        ├── Layer 8: [CorridorShell] ← Type here
        ├── Layer 9: [Empty]
        └── ...
```

---

## STEP 2: Assign CorridorShell Layer to Outer Walls/Ceiling

### 2.1 Identify Outer Surfaces

**What should be on CorridorShell:**
- ✅ Outer walls (exterior faces of the corridor)
- ✅ Ceiling (top surface)
- ✅ Any surfaces that block the view from outside but should be "permeable" for raycasting

**What should NOT be on CorridorShell:**
- ❌ Interior floors
- ❌ Interior walls
- ❌ Stairs
- ❌ Any surfaces you want to place waypoints on

### 2.2 Assign the Layer

**Option A: Select Multiple Objects**
1. In **Hierarchy**, hold `Ctrl` (Windows) or `Cmd` (Mac) and click to select multiple GameObjects
2. In **Inspector**, find the **Layer** dropdown (top of Inspector, next to "Tag")
3. Select **"CorridorShell"**
4. Unity will ask: "Do you want to change the layer for all child objects?"
   - Click **"Yes, change children"** if you want to apply to all children
   - Click **"No, this object only"** if you only want to change the selected objects

**Option B: Select One Object at a Time**
1. Select a GameObject (e.g., "OuterWall_01")
2. In **Inspector**, click the **Layer** dropdown
3. Select **"CorridorShell"**
4. Repeat for all outer walls and ceiling

**Visual Guide:**
```
Inspector
┌─────────────────────────┐
│ GameObject Name         │
│ Tag: Untagged           │
│ Layer: [CorridorShell ▼]│ ← Click here
└─────────────────────────┘
```

---

## STEP 3: Verify Environment Layer for Interior Surfaces

1. **Check**: What layer are your interior floors/walls on?
   - Usually: **"Default"** or **"Environment"**
2. **Verify**: In `PointPlacementManager` Inspector:
   - Find **"_environmentLayer"** field
   - Make sure it includes the layer your interior surfaces are on
   - If interior surfaces are on "Default" (Layer 0), the default setting should work
   - If you have an "Environment" layer, make sure it's selected in the layer mask

**Visual Guide:**
```
PointPlacementManager (Inspector)
├── Collision Avoidance Parameters
│   ├── _droneRadius: 0.45
│   └── _environmentLayer: [Default] ← Should include interior surfaces
```

---

## STEP 4: Set Up CorridorViewpointDetector

### 4.1 Create GameObject

1. In **Hierarchy**, right-click in empty space
2. Select **"Create Empty"**
3. Rename it to **"CorridorViewpointDetector"**

### 4.2 Add Component

1. Select **"CorridorViewpointDetector"** GameObject
2. In **Inspector**, click **"Add Component"**
3. Type: **"CorridorViewpointDetector"**
4. Select the component from the list

### 4.3 Configure Component

**Option A: Use Corridor Model (Recommended)**
1. In **Hierarchy**, find your main corridor/photogrammetry GameObject
2. Drag it into **"Corridor Model"** field in Inspector
3. The component will auto-detect bounds from the Renderer

**Option B: Manual Bounds**
1. Leave **"Corridor Model"** empty
2. Set **"Manual Corridor Bounds"**:
   - **Center**: Position of corridor center (e.g., `(0, 1.5, 0)`)
   - **Size**: Size of corridor (e.g., `(10, 3, 20)` for 10m wide, 3m tall, 20m long)

### 4.4 Set Player Transform (Optional)

1. In **Hierarchy**, find **"XR Origin"** (or your VR camera setup)
2. Drag it into **"Player Transform"** field
3. **OR**: Leave empty - component will auto-detect at runtime

### 4.5 Enable Debug Gizmos (Optional, for Testing)

1. In Inspector, check **"Show Debug Gizmos"**
2. In Scene view, you'll see:
   - **Green wireframe cube**: When inside corridor
   - **Red wireframe cube**: When outside corridor
   - **Green/Red sphere**: Player position

**Visual Guide:**
```
CorridorViewpointDetector (Inspector)
├── Corridor Bounds
│   ├── Corridor Model: [Drag corridor GameObject here]
│   ├── Manual Corridor Bounds
│   │   ├── Center: (0, 1.5, 0)
│   │   └── Size: (10, 3, 20)
│   └── Inside Padding: 0.5
├── Player Reference
│   └── Player Transform: [Drag XR Origin here]
└── Debug
    ├── Show Debug Gizmos: ✓
    ├── Inside Color: Green
    └── Outside Color: Red
```

---

## STEP 5: Configure RayDepthController

1. In **Hierarchy**, find the GameObject with **`RayDepthController`** component
2. Select it
3. In **Inspector**, scroll to **"Viewpoint-Aware Raycasting"** section
4. **Viewpoint Detector**: Drag **"CorridorViewpointDetector"** GameObject here
5. **Corridor Shell Layer**: 
   - Click the dropdown
   - Select **"CorridorShell"**
   - **OR**: Leave empty - component will auto-detect by name

**Visual Guide:**
```
RayDepthController (Inspector)
├── ... (existing fields) ...
└── Viewpoint-Aware Raycasting
    ├── Viewpoint Detector: [Drag CorridorViewpointDetector]
    └── Corridor Shell Layer: [CorridorShell ▼]
```

---

## STEP 6: Configure PathModeController

1. In **Hierarchy**, find the GameObject with **`PathModeController`** component
2. Select it
3. In **Inspector**, scroll to **"Viewpoint-Aware Raycasting"** section
4. **Viewpoint Detector**: Drag **"CorridorViewpointDetector"** GameObject here
5. **Corridor Shell Layer**: Select **"CorridorShell"** (same as Step 5)

**Visual Guide:**
```
PathModeController (Inspector)
├── ... (existing fields) ...
└── Viewpoint-Aware Raycasting
    ├── Viewpoint Detector: [Drag CorridorViewpointDetector]
    └── Corridor Shell Layer: [CorridorShell ▼]
```

---

## STEP 7: Test the System

### 7.1 Test Inside View (Room View)

1. **Position player inside corridor** (in VR or by moving XR Origin in Scene view)
2. **Enable Scene view Gizmos** (if using debug gizmos)
3. **Point controller ray** at a wall
4. **Expected**: Ray stops at wall (doesn't pass through)
5. **Place waypoint** on floor
6. **Expected**: Waypoint places normally
7. **Try placing waypoint too close to ceiling**
8. **Expected**: Red warning shell appears, placement blocked

### 7.2 Test Outside View (Birds-Eye View)

1. **Position player outside corridor** (above or to the side)
2. **Point controller ray** at outer wall
3. **Expected**: Ray passes through outer wall
4. **Point ray at interior floor**
5. **Expected**: Ray hits floor (stops at interior surface)
6. **Place waypoint** on interior floor
7. **Expected**: Waypoint places successfully
8. **Try placing waypoint too close to ceiling**
9. **Expected**: Red warning shell appears, placement blocked (no-fly zone still enforced)

### 7.3 Verify Debug Gizmos

1. In **Scene view**, select **"CorridorViewpointDetector"**
2. **Enable "Show Debug Gizmos"** if not already
3. **Move XR Origin** inside/outside corridor
4. **Expected**: 
   - **Green cube** when inside
   - **Red cube** when outside
   - **Green/Red sphere** shows player position

---

## STEP 8: Troubleshooting

### Problem: Ray Still Stops at Outer Walls from Outside

**Solution:**
1. Check that outer walls are on **"CorridorShell"** layer
2. Check that `RayDepthController._corridorShellLayer` is set to **"CorridorShell"**
3. Check that `CorridorViewpointDetector` is detecting "outside" correctly:
   - Enable debug gizmos
   - Check if cube is red when outside
4. Check Console for errors

### Problem: Can't Place Waypoints Inside from Outside

**Solution:**
1. Verify interior surfaces (floors, interior walls) are on **"Environment"** layer (NOT "CorridorShell")
2. Check that `PointPlacementManager._environmentLayer` includes the layer your interior surfaces are on
3. Verify interior surfaces have **Collider** components
4. Check Console for collision detection errors

### Problem: No-Fly Zones Not Working

**Solution:**
1. This should NOT be affected by viewpoint changes
2. Verify `PointPlacementManager._environmentLayer` includes interior surfaces
3. Check that interior surfaces have colliders
4. Verify `_droneRadius` is set correctly (default: 0.45m)
5. Check Console for `CheckGhostCollisionWithObstacles` errors

### Problem: Viewpoint Detection Not Working

**Solution:**
1. Check `CorridorViewpointDetector` bounds:
   - Enable debug gizmos
   - Verify wireframe cube matches your corridor size
2. Verify player transform is assigned or can be auto-detected:
   - Check Console for "No player transform found" warnings
   - Manually assign XR Origin to "Player Transform" field
3. Check that player position is actually outside bounds when testing:
   - Use Scene view to verify XR Origin position
   - Adjust "Manual Corridor Bounds" if needed

### Problem: Auto-Detection Not Working

**Solution:**
1. **CorridorViewpointDetector**: Manually assign in Inspector
2. **CorridorShell Layer**: Manually set layer mask in Inspector
3. Check Console for auto-detection messages:
   - Should see: "Auto-found CorridorViewpointDetector"
   - Should see: "Auto-found CorridorShell layer"

---

## Quick Reference Checklist

- [ ] Created "CorridorShell" layer
- [ ] Assigned outer walls/ceiling to "CorridorShell" layer
- [ ] Verified interior surfaces are on "Environment" layer (or whatever `_environmentLayer` uses)
- [ ] Created "CorridorViewpointDetector" GameObject
- [ ] Configured `CorridorViewpointDetector` (model or manual bounds)
- [ ] Assigned `CorridorViewpointDetector` to `RayDepthController`
- [ ] Assigned `CorridorViewpointDetector` to `PathModeController`
- [ ] Set "CorridorShell" layer mask in `RayDepthController`
- [ ] Set "CorridorShell" layer mask in `PathModeController`
- [ ] Tested inside view (ray stops at walls)
- [ ] Tested outside view (ray passes through outer walls)
- [ ] Verified no-fly zones work from both viewpoints

---

## Summary

**What Changed:**
- ✅ Raycasting behavior adapts based on viewpoint
- ✅ No-fly zones always enforced (unchanged)
- ✅ Backward compatible (works without setup)

**Key Concept:**
- **Inside**: Ray stops at all colliders (normal behavior)
- **Outside**: Ray passes through `CorridorShell`, stops at `Environment`
- **Safety**: Always checks `Environment` layer for no-fly zones

**Files Modified:**
- `CorridorViewpointDetector.cs` (NEW)
- `RayDepthController.cs` (MODIFIED)
- `PathModeController.cs` (MODIFIED)

