# No-Fly Zone Setup: Critical Layer Configuration

## The Problem

The no-fly zone should work from BOTH inside and outside viewpoints. The issue is likely **layer configuration**.

---

## Key Concept: Two Different Layers for Two Different Purposes

### CorridorShell Layer
- **Purpose**: Permeable for raycasting when outside
- **What goes here**: Outer walls, outer ceiling (exterior surfaces)
- **No-fly zone**: NOT checked (these are just visual boundaries)

### Environment Layer  
- **Purpose**: Always checked for no-fly zones (safety bubble)
- **What goes here**: Interior ceiling, interior walls, doors, floors, stairs, obstacles
- **No-fly zone**: ALWAYS checked (regardless of viewpoint)

---

## Critical Setup Steps

### Step 1: Identify Your Surfaces

**Outer Surfaces (CorridorShell):**
- ✅ Outer walls (exterior faces)
- ✅ Outer ceiling (top surface from outside)
- ❌ NOT interior surfaces

**Interior Surfaces (Environment):**
- ✅ Interior ceiling (the one inside the corridor)
- ✅ Interior walls (walls inside the corridor)
- ✅ Doors
- ✅ Floors
- ✅ Stairs
- ✅ Any obstacles

### Step 2: Assign Layers Correctly

1. **Select all INTERIOR surfaces** (ceiling, walls, doors inside corridor)
2. **Set Layer to**: "Environment" (or "Default" if that's what `_environmentLayer` uses)
3. **Select all OUTER surfaces** (outer walls, outer ceiling)
4. **Set Layer to**: "CorridorShell"

### Step 3: Verify PointPlacementManager Settings

1. **Find** GameObject with `PointPlacementManager` component
2. **In Inspector**, find **"_environmentLayer"** field
3. **Make sure** it includes the layer your interior surfaces are on
   - If interior surfaces are on "Default" (Layer 0), default should work
   - If interior surfaces are on "Environment" layer, select "Environment" in the layer mask

---

## How No-Fly Zone Works

The no-fly zone check (`CheckGhostCollisionWithObstacles`) does this:

```csharp
// Line 775 in PointPlacementManager.cs
Collider[] nearbyColliders = Physics.OverlapSphere(ghostPos, _droneRadius, _environmentLayer);
```

**Key Points:**
- ✅ Always uses `_environmentLayer` (NOT `CorridorShell`)
- ✅ Works from BOTH inside and outside viewpoints
- ✅ Checks interior surfaces (ceiling, walls, doors) for collision
- ❌ Does NOT check `CorridorShell` layer (outer walls)

---

## Visual Feedback

When ghost is too close to an obstacle on `Environment` layer:

1. **Ghost turns GREY** (collision color)
2. **Red warning shell appears** around the obstacle
3. **Placement is BLOCKED** (can't place waypoint)

This should work from BOTH:
- ✅ Inside corridor (room view)
- ✅ Outside corridor (birds-eye view)

---

## Troubleshooting

### Problem: No-Fly Zone Not Working

**Check 1: Layer Assignment**
- Are interior surfaces (ceiling, walls, doors) on `Environment` layer?
- Are they NOT on `CorridorShell` layer?

**Check 2: PointPlacementManager Settings**
- Is `_environmentLayer` set to include the layer with interior surfaces?
- Check Inspector: `PointPlacementManager` → `_environmentLayer`

**Check 3: Colliders**
- Do interior surfaces have Collider components?
- Are colliders enabled?

**Check 4: Drone Radius**
- Is `_droneRadius` set correctly? (default: 0.45m)
- Check Inspector: `PointPlacementManager` → `_droneRadius`

### Problem: Ghost Doesn't Turn Grey

**Check:**
- Is `UpdateGhostVisualValidity` being called?
- Check Console for errors
- Verify `_ghostRenderer` is assigned in Inspector

### Problem: Red Warning Shells Don't Appear

**Check:**
- Are obstacles on `Environment` layer?
- Do obstacles have Collider components?
- Check Console for `ObstacleHighlighter` errors

---

## Example Setup

**Correct Setup:**
```
CorridorShell Layer:
├── OuterWall_North
├── OuterWall_South  
├── OuterWall_East
├── OuterWall_West
└── OuterCeiling

Environment Layer:
├── InteriorCeiling        ← Checked for no-fly zone
├── InteriorWall_North     ← Checked for no-fly zone
├── InteriorWall_South     ← Checked for no-fly zone
├── Door_01                ← Checked for no-fly zone
├── Floor                  ← Checked for no-fly zone
└── Stairs                 ← Checked for no-fly zone
```

**Wrong Setup:**
```
CorridorShell Layer:
├── OuterWall_North
├── OuterWall_South
├── InteriorCeiling        ← WRONG! Should be on Environment
└── InteriorWall_North     ← WRONG! Should be on Environment
```

---

## Verification Test

1. **Position player inside corridor**
2. **Point ray at interior ceiling**
3. **Move ghost close to ceiling** (within 0.45m)
4. **Expected**: Ghost turns grey, red shell appears, placement blocked

5. **Position player outside corridor**
6. **Point ray through outer wall** (should pass through)
7. **Hit interior ceiling** (ray should stop)
8. **Move ghost close to interior ceiling** (within 0.45m)
9. **Expected**: Ghost turns grey, red shell appears, placement blocked

---

## Summary

**The key is layer separation:**
- **CorridorShell** = Outer surfaces (permeable for raycasting, NOT checked for no-fly)
- **Environment** = Interior surfaces (ALWAYS checked for no-fly, regardless of viewpoint)

**No-fly zone works from both viewpoints** because it always checks `_environmentLayer`, which should include interior surfaces.

