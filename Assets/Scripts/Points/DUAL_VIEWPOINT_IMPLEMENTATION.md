# Dual Viewpoint Implementation Summary

## Problem Statement

The system needed to support two viewpoints:
1. **Room View (Inside)**: Player inside corridor - ray stops at colliders (existing behavior)
2. **Birds-Eye View (Outside)**: Player outside corridor - ray must pass through outer walls/ceiling to place waypoints inside

**Key Requirement**: Decouple raycasting behavior from drone safety checks. No-fly zones must always be enforced regardless of viewpoint.

## Solution Architecture

### Core Components

1. **`CorridorViewpointDetector.cs`** (NEW)
   - Detects if player is inside or outside corridor volume
   - Uses bounds detection (from GameObject Renderer or manual bounds)
   - Provides `IsInsideCorridor` property

2. **`RayDepthController.cs`** (MODIFIED)
   - Added viewpoint-aware raycast mask logic
   - When outside: excludes `CorridorShell` layer from raycasts
   - When inside: uses normal mask (all layers)
   - Surface snapping raycast now respects viewpoint

3. **`PathModeController.cs`** (MODIFIED)
   - Added viewpoint-aware raycast mask logic
   - Waypoint selection raycasts respect viewpoint
   - Allows selecting waypoints through corridor shell when outside

### Layer System

**New Layer: "CorridorShell"**
- Assigned to: Outer walls, ceiling (exterior surfaces)
- Purpose: Permeable for raycasting when outside, solid when inside
- Excluded from: No-fly zone checks (those use "Environment" layer)

**Existing Layer: "Environment"**
- Assigned to: Interior floors, interior walls, stairs, obstacles
- Purpose: Always checked for raycasts and no-fly zones
- Never excluded: Always part of collision detection

## Implementation Details

### Viewpoint Detection

```csharp
// CorridorViewpointDetector.cs
public bool IsInsideCorridor
{
    get
    {
        Vector3 playerPos = _playerTransform.position;
        Bounds paddedBounds = new Bounds(_effectiveBounds.center, 
            _effectiveBounds.size + Vector3.one * _insidePadding * 2f);
        return paddedBounds.Contains(playerPos);
    }
}
```

### Raycast Mask Logic

```csharp
// RayDepthController.cs & PathModeController.cs
private LayerMask GetViewpointAwareRaycastMask(LayerMask baseMask)
{
    if (_viewpointDetector == null || _corridorShellLayer == 0)
        return baseMask; // Default behavior (inside)
    
    if (_viewpointDetector.IsInsideCorridor)
        return baseMask; // Inside: solid colliders
    
    // Outside: exclude corridor shell (permeable)
    return baseMask & ~_corridorShellLayer;
}
```

### No-Fly Zone Checks (Unchanged)

```csharp
// PointPlacementManager.cs - CheckGhostCollisionWithObstacles()
Collider[] nearbyColliders = Physics.OverlapSphere(ghostPos, _droneRadius, _environmentLayer);

// PathModeController.cs - SegmentBlockedBetween()
Physics.CheckCapsule(capsuleStart, capsuleEnd, radius, _environmentLayer, ...);
```

**Key Point**: No-fly zone checks always use `_environmentLayer`, which does NOT include `CorridorShell`. This means:
- Outer walls/ceiling are NOT checked for no-fly zones (they're just visual boundaries)
- Interior surfaces ARE checked for no-fly zones (safety bubble enforced)

## Behavior Matrix

| Viewpoint | Raycast Behavior | No-Fly Zone Behavior |
|-----------|------------------|----------------------|
| **Inside** | Stops at all colliders (including CorridorShell) | Checks Environment layer (interior surfaces) |
| **Outside** | Passes through CorridorShell, stops at Environment | Checks Environment layer (interior surfaces) |

## Files Modified

1. **`CorridorViewpointDetector.cs`** - NEW
   - Viewpoint detection component
   - Bounds calculation
   - Debug gizmos

2. **`RayDepthController.cs`** - MODIFIED
   - Added `_viewpointDetector` reference
   - Added `_corridorShellLayer` field
   - Modified surface snapping raycast to use `GetViewpointAwareRaycastMask()`
   - Auto-detection of detector and layer

3. **`PathModeController.cs`** - MODIFIED
   - Added `_viewpointDetector` reference
   - Added `_corridorShellLayer` field
   - Modified waypoint selection raycasts to use `GetViewpointAwareRaycastMask()`
   - Auto-detection of detector and layer

4. **`BIRDS_EYE_VIEW_SETUP.md`** - NEW
   - Complete setup guide
   - Troubleshooting section
   - Testing procedures

## Backward Compatibility

✅ **Fully backward compatible**:
- If `CorridorViewpointDetector` is not assigned, system assumes "inside" (default behavior)
- If `CorridorShell` layer is not set, system uses normal raycast mask
- Existing scenes continue to work without changes
- No-fly zone behavior is unchanged

## Testing Checklist

- [ ] Inside view: Ray stops at walls/ceiling
- [ ] Inside view: Waypoint placement works normally
- [ ] Inside view: No-fly zones prevent placement too close to obstacles
- [ ] Outside view: Ray passes through outer walls/ceiling
- [ ] Outside view: Ray still hits interior floors/walls
- [ ] Outside view: Waypoint placement works on interior surfaces
- [ ] Outside view: No-fly zones still prevent placement too close to ceiling/walls
- [ ] Path segments: Validation works from both viewpoints
- [ ] Red warning shells: Appear correctly from both viewpoints

## Future Enhancements

Potential improvements:
1. **Visual feedback**: Show different ray color when outside (e.g., blue when permeable)
2. **Automatic bounds**: Auto-detect corridor bounds from all environment colliders
3. **Multiple corridors**: Support multiple corridor volumes
4. **Smooth transition**: Fade ray behavior when crossing boundary

