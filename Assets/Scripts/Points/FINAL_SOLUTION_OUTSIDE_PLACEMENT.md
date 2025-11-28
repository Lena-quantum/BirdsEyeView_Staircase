# Final Solution: Placing Waypoints from Outside

## The Requirement

1. **From outside**: Ray passes through ALL colliders to reach interior corridor space
2. **Waypoint placement**: Allowed at any position inside corridor (depth-based)
3. **No-fly zone**: Still enforced - blocks placement if too close (< 0.45m) to walls, ceiling, or doors

## The Solution

### When Inside Corridor (Room View)
- **Surface snapping**: Enabled (ray stops at colliders)
- **Ghost positioning**: Snaps to surface hit by ray
- **No-fly zone**: Checks Environment layer colliders
- **Behavior**: Unchanged from before

### When Outside Corridor (Birds-Eye View)
- **Surface snapping**: Disabled (ray passes through all colliders)
- **Ghost positioning**: Depth-based (`origin + direction * depth`)
- **No-fly zone**: Still checks Environment layer colliders
- **Result**: Can place waypoints anywhere inside corridor, but blocked if too close to obstacles

## How It Works

### Outside Viewpoint Flow:

1. **Player outside corridor**
2. **Ray passes through** all colliders (CorridorShell and Environment)
3. **Ghost positioned** at `controllerPosition + direction * depth` (inside corridor)
4. **No-fly zone check** runs at ghost position:
   - Checks distance to Environment layer colliders (walls, ceiling, doors)
   - If distance < 0.45m → Ghost turns grey, placement blocked
   - If distance >= 0.45m → Ghost normal color, placement allowed

### Code Changes:

```csharp
// Detect if outside
bool isOutside = _viewpointDetector != null && !_viewpointDetector.IsInsideCorridor;

if (useSnap && !isOutside)
{
    // Inside: Normal surface snapping
    // Ray stops at colliders, ghost snaps to surface
}
// Outside: No surface snapping
// Ghost stays at depth-based position (origin + dir * depth)
// No-fly zone will still check this position
```

## Key Points

1. **Raycast behavior**: 
   - Inside: Stops at colliders (surface snapping)
   - Outside: Passes through all colliders (depth-based positioning)

2. **No-fly zone**:
   - Always checks Environment layer colliders
   - Works from both inside and outside viewpoints
   - Blocks placement if too close to walls/ceiling/doors

3. **Ghost visual feedback**:
   - Normal color: Safe position (> 0.45m from obstacles)
   - Grey color: Too close to obstacle (< 0.45m)
   - Red warning shells: Appear around obstacles when too close

## Testing

### Test Inside (Room View):
1. Position player inside corridor
2. Point ray at wall
3. **Expected**: Ray stops at wall, ghost snaps to wall
4. Move ghost close to ceiling
5. **Expected**: Ghost turns grey, red shell appears, placement blocked

### Test Outside (Birds-Eye View):
1. Position player outside corridor
2. Point ray through outer wall (should pass through)
3. Adjust depth (A/B buttons) to position ghost inside corridor
4. **Expected**: Ghost appears inside corridor at depth-based position
5. Move ghost close to interior ceiling/wall
6. **Expected**: Ghost turns grey, red shell appears, placement blocked
7. Move ghost away from obstacles (> 0.45m)
8. **Expected**: Ghost normal color, placement allowed

## Layer Setup

**CorridorShell Layer:**
- Outer walls, outer ceiling
- Permeable for raycasting when outside (but not used anymore since we don't surface-snap)

**Environment Layer:**
- Interior walls, interior ceiling, doors, floors
- Checked by no-fly zone (always, from both viewpoints)
- Must have Collider components

## Summary

**The solution:**
- ✅ From outside: Ray passes through all colliders
- ✅ Ghost positioned at depth-based location (inside corridor)
- ✅ No-fly zone still works (checks Environment layer)
- ✅ Placement blocked if too close to walls/ceiling/doors
- ✅ Placement allowed if safe distance from obstacles

**Key insight:** Decouple raycast behavior from no-fly zone checks:
- **Raycast**: For positioning (depth-based when outside)
- **No-fly zone**: For safety (always checks Environment layer)

