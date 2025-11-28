# Multi-Pass Raycast Solution

## The Problem

When outside the corridor:
- Ray needs to pass through `CorridorShell` (outer walls/ceiling)
- But ray must still hit `Environment` layer (interior floors, walls, ceiling)
- Simple layer mask exclusion doesn't work if interior surfaces are also on `CorridorShell`

## The Solution: Multi-Pass Raycast

Instead of excluding `CorridorShell` from the raycast mask, we use a **multi-pass approach**:

1. **First Pass**: Check if ray hits `CorridorShell`
   - If yes: Pass through it, continue raycast from that point
   - If no: Continue to next pass

2. **Second Pass**: Check for `Environment` layer surfaces
   - Ray continues from past the shell
   - Hits interior floors, walls, ceiling
   - This is the target surface for waypoint placement

## How It Works

### Inside Corridor (Room View)
- Uses normal single-pass raycast
- All colliders are solid
- Behavior unchanged

### Outside Corridor (Birds-Eye View)
- **Pass 1**: Ray hits `CorridorShell` → Pass through, continue
- **Pass 2**: Ray hits `Environment` → Stop here, use this surface
- Result: Can place waypoints on interior surfaces from outside

## Code Implementation

**New Method: `RaycastThroughShell()`**

```csharp
private bool RaycastThroughShell(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit finalHit)
{
    // If inside: normal raycast
    if (_viewpointDetector.IsInsideCorridor)
    {
        return Physics.Raycast(origin, direction, out finalHit, maxDistance, _surfaceRaycastMask, ...);
    }
    
    // If outside: multi-pass
    while (remainingDistance > 0.001f)
    {
        // Check for CorridorShell
        if (Physics.Raycast(currentOrigin, direction, out hit, remainingDistance, _corridorShellLayer, ...))
        {
            // Pass through shell, continue raycast
            currentOrigin = hit.point + direction * 0.01f;
            remainingDistance -= (hit.distance + 0.01f);
            continue;
        }
        
        // Check for Environment layer
        if (Physics.Raycast(currentOrigin, direction, out hit, remainingDistance, environmentMask, ...))
        {
            // Found interior surface!
            finalHit = hit;
            return true;
        }
        
        break;
    }
    
    return false;
}
```

## Advantages

1. ✅ **Works regardless of layer setup**
   - Doesn't matter if interior surfaces are on `CorridorShell` or `Environment`
   - Multi-pass handles both cases

2. ✅ **More flexible**
   - Can pass through multiple `CorridorShell` layers
   - Still finds `Environment` surfaces behind them

3. ✅ **No-fly zone still works**
   - No-fly zone checks `Environment` layer separately
   - Not affected by raycast behavior

## Layer Setup (Simplified)

With multi-pass raycast, layer setup is more flexible:

**Option 1: Separate Layers (Recommended)**
- `CorridorShell`: Outer walls/ceiling
- `Environment`: Interior surfaces

**Option 2: Same Layer (Also Works)**
- Both on `CorridorShell`: Multi-pass will still work
- But no-fly zone won't check them (needs `Environment` layer)

**Best Practice:**
- Outer surfaces: `CorridorShell` layer
- Interior surfaces: `Environment` layer
- No-fly zone checks: `Environment` layer

## Testing

### Test Inside:
1. Position player inside corridor
2. Point ray at wall
3. Expected: Ray stops at wall (normal behavior)

### Test Outside:
1. Position player outside corridor
2. Point ray through outer wall
3. Expected: 
   - Ray passes through outer wall (`CorridorShell`)
   - Ray hits interior floor/wall/ceiling (`Environment`)
   - Ghost snaps to interior surface
   - Can place waypoint (if not in no-fly zone)

### Test No-Fly Zone:
1. From outside, place ghost close to interior ceiling
2. Expected:
   - Ghost turns grey
   - Red warning shell appears
   - Placement blocked

## Summary

**Multi-pass raycast solves the problem:**
- ✅ Ray passes through `CorridorShell` when outside
- ✅ Ray still hits `Environment` surfaces (interior floors, walls, ceiling)
- ✅ Can place waypoints on interior surfaces from outside
- ✅ No-fly zone still works (checks `Environment` layer separately)

**Key Point:** The raycast and no-fly zone are now completely independent:
- **Raycast**: Multi-pass to find surfaces (for placement)
- **No-Fly Zone**: Always checks `Environment` layer (for safety)

