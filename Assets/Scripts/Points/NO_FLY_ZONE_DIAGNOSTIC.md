# No-Fly Zone Diagnostic Guide

## Quick Check: Is No-Fly Zone Working?

The no-fly zone should work from BOTH inside and outside viewpoints. If it's not working, follow these steps:

---

## Step 1: Check Layer Configuration

### What Layer Are Your Interior Surfaces On?

1. **Select** an interior ceiling, wall, or door GameObject
2. **Check Inspector** → **Layer** dropdown
3. **Note** what layer it's on (e.g., "Default", "Environment", etc.)

### What Is PointPlacementManager._environmentLayer Set To?

1. **Find** GameObject with `PointPlacementManager` component
2. **In Inspector**, find **"_environmentLayer"** field
3. **Check** which layer(s) are selected in the layer mask

### They Must Match!

- If interior surfaces are on **"Default"** (Layer 0), `_environmentLayer` should include **"Default"**
- If interior surfaces are on **"Environment"**, `_environmentLayer` should include **"Environment"**

**Default setting:** `_environmentLayer = 1 << 0` means it checks **"Default"** layer (Layer 0)

---

## Step 2: Verify Colliders Exist

1. **Select** an interior ceiling/wall/door
2. **Check Inspector** for a **Collider** component (BoxCollider, MeshCollider, etc.)
3. **Make sure** the collider is **enabled** (checkbox checked)

**If no collider:** No-fly zone can't detect it!

---

## Step 3: Test No-Fly Zone

### Test Inside Corridor:

1. **Position player inside corridor**
2. **Point ray at interior ceiling**
3. **Move ghost very close to ceiling** (within 0.5m)
4. **Expected Results:**
   - ✅ Ghost turns **GREY**
   - ✅ **Red warning shell** appears around ceiling
   - ✅ **Placement blocked** (can't place waypoint)

### Test Outside Corridor:

1. **Position player outside corridor**
2. **Point ray through outer wall** (should pass through)
3. **Hit interior ceiling** (ray should stop)
4. **Move ghost very close to interior ceiling** (within 0.5m)
5. **Expected Results:**
   - ✅ Ghost turns **GREY**
   - ✅ **Red warning shell** appears around ceiling
   - ✅ **Placement blocked** (can't place waypoint)

---

## Step 4: Check Console for Errors

1. **Open Console**: `Window` → `General` → `Console`
2. **Look for errors** related to:
   - `CheckGhostCollisionWithObstacles`
   - `ObstacleHighlighter`
   - `UpdateGhostVisualValidity`

**If you see errors**, fix them first.

---

## Common Issues and Fixes

### Issue 1: Interior Surfaces on Wrong Layer

**Symptom:** No-fly zone doesn't detect interior ceiling/walls

**Fix:**
1. Select all interior surfaces (ceiling, walls, doors)
2. Set Layer to **"Default"** (or whatever `_environmentLayer` checks)
3. OR change `_environmentLayer` in `PointPlacementManager` to match your layer

### Issue 2: Interior Surfaces on CorridorShell Layer

**Symptom:** No-fly zone doesn't work, but raycasting works

**Fix:**
1. **CorridorShell** should ONLY have outer walls/ceiling
2. **Interior surfaces** should be on **"Default"** or **"Environment"** layer
3. Move interior surfaces to correct layer

### Issue 3: No Colliders on Interior Surfaces

**Symptom:** No-fly zone doesn't detect anything

**Fix:**
1. Add **Collider** components to interior surfaces
2. Use **BoxCollider**, **MeshCollider**, or appropriate collider type
3. Make sure colliders are **enabled**

### Issue 4: _environmentLayer Not Set Correctly

**Symptom:** No-fly zone doesn't detect anything

**Fix:**
1. Check what layer your interior surfaces are on
2. In `PointPlacementManager`, set `_environmentLayer` to include that layer
3. Use layer mask dropdown to select the correct layer(s)

---

## Layer Setup Summary

**Correct Setup:**

```
CorridorShell Layer (for raycasting permeability):
├── OuterWall_North
├── OuterWall_South
├── OuterWall_East
├── OuterWall_West
└── OuterCeiling

Default/Environment Layer (for no-fly zone):
├── InteriorCeiling        ← Must have Collider!
├── InteriorWall_North     ← Must have Collider!
├── InteriorWall_South     ← Must have Collider!
├── Door_01                ← Must have Collider!
├── Floor                  ← Must have Collider!
└── Stairs                 ← Must have Collider!
```

**PointPlacementManager Settings:**
- `_environmentLayer` = **"Default"** (or "Environment" if you use that layer)
- `_droneRadius` = **0.45** (45cm safety buffer)

---

## Debug: Add Console Logging

If you want to see what's happening, you can temporarily add debug logs to `CheckGhostCollisionWithObstacles()`:

```csharp
// In PointPlacementManager.cs, line 775
Collider[] nearbyColliders = Physics.OverlapSphere(ghostPos, _droneRadius, _environmentLayer);
Debug.Log($"No-fly check: Found {nearbyColliders.Length} colliders at {ghostPos} using layer mask {_environmentLayer}");
```

This will show in Console:
- How many colliders are found
- What layer mask is being used
- Where the ghost is positioned

---

## Expected Behavior

**From Inside (Room View):**
- ✅ Ray stops at all colliders (normal)
- ✅ Ghost turns grey when too close to interior surfaces
- ✅ Red warning shells appear
- ✅ Placement blocked when in no-fly zone

**From Outside (Birds-Eye View):**
- ✅ Ray passes through `CorridorShell` (outer walls)
- ✅ Ray stops at `Environment` layer (interior surfaces)
- ✅ Ghost turns grey when too close to interior surfaces
- ✅ Red warning shells appear
- ✅ Placement blocked when in no-fly zone

**Key Point:** No-fly zone works the SAME from both viewpoints because it always checks `_environmentLayer`, which should include interior surfaces.

---

## Still Not Working?

If no-fly zone still doesn't work after checking all of the above:

1. **Share** what layer your interior surfaces are on
2. **Share** what `_environmentLayer` is set to in `PointPlacementManager`
3. **Share** any Console errors
4. **Test** with a simple setup:
   - Create a cube on "Default" layer
   - Add BoxCollider to cube
   - Try placing waypoint close to cube
   - Should see grey ghost and red shell

