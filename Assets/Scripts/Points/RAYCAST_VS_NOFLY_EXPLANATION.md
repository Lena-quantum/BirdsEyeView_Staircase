# Understanding Raycast vs No-Fly Zone Layers

## The Key Question

**"If interior colliders are on Environment layer, and we come from outside to set a point inside the corridor, won't it not work?"**

**Answer: It SHOULD work! Here's why:**

---

## How It Works

### When Outside Corridor:

**Raycast Mask:**
```
baseMask = ~0 (all layers)
- CorridorShell (excluded) = permeable for raycasting
+ Environment (included) = ray can hit interior surfaces
```

**Result:**
- ✅ Ray passes through `CorridorShell` (outer walls)
- ✅ Ray hits `Environment` layer (interior floors, walls, ceiling)
- ✅ Ghost snaps to interior surface
- ✅ You can place waypoints on interior surfaces

**No-Fly Zone Check:**
```
Uses: _environmentLayer (Environment layer)
Checks: Distance from ghost to Environment layer colliders
```

**Result:**
- ✅ If ghost is > 0.45m from Environment surfaces → Placement allowed
- ✅ If ghost is < 0.45m from Environment surfaces → Placement blocked (grey ghost, red shell)

---

## The Two Different Behaviors

### 1. Raycasting (for surface snapping)
- **Purpose**: Find where to place the ghost
- **When outside**: Excludes `CorridorShell`, includes `Environment`
- **Result**: Ray can hit interior floors/walls/ceiling

### 2. No-Fly Zone (for safety)
- **Purpose**: Prevent placement too close to obstacles
- **Always**: Checks `Environment` layer (regardless of viewpoint)
- **Result**: Blocks placement if too close to Environment surfaces

---

## Example Scenario: Placing Waypoint on Floor from Outside

1. **Player outside corridor**
2. **Ray passes through** `CorridorShell` (outer wall) - permeable
3. **Ray hits** `Environment` layer (interior floor) - stops here
4. **Ghost snaps to floor** position
5. **No-fly zone check**:
   - If floor is > 0.45m away → ✅ Placement allowed
   - If floor is < 0.45m away → ❌ Placement blocked (too close to floor)

**This is CORRECT behavior!** You can place waypoints on the floor, but not if you're too close to it.

---

## Potential Issue: Ray Hitting Ceiling First

**Problem:** If you're outside and point down, the ray might hit the interior ceiling before the floor.

**Solution:** 
- Use depth control (A/B buttons) to adjust ghost position
- Or point more directly at the floor
- The ray will hit the first Environment surface it encounters

---

## Layer Setup (Correct)

```
CorridorShell Layer:
├── OuterWall_North    ← Ray passes through when outside
├── OuterWall_South    ← Ray passes through when outside
└── OuterCeiling       ← Ray passes through when outside

Environment Layer:
├── InteriorFloor      ← Ray hits this, no-fly zone checks this
├── InteriorWall_North ← Ray hits this, no-fly zone checks this
├── InteriorCeiling    ← Ray hits this, no-fly zone checks this
└── Door_01           ← Ray hits this, no-fly zone checks this
```

---

## Why This Works

**Raycasting:**
- When outside: `mask = allLayers & ~CorridorShell`
- This means: Include Environment, exclude CorridorShell
- Ray can hit Environment surfaces (floors, walls, ceiling)

**No-Fly Zone:**
- Always checks: `_environmentLayer` (Environment layer)
- This is separate from raycasting
- Checks distance to Environment colliders

**They work together:**
1. Raycast finds where to place ghost (on Environment surface)
2. No-fly zone checks if that position is safe (distance to Environment surfaces)
3. If safe → place waypoint
4. If not safe → block placement

---

## If It's Not Working

**Check 1: Raycast Mask**
- When outside, does ray pass through outer walls? ✅
- Does ray hit interior surfaces? ✅
- If not, check `_corridorShellLayer` is set correctly

**Check 2: No-Fly Zone**
- Does ghost turn grey when too close to interior surfaces? ✅
- Do red warning shells appear? ✅
- If not, check:
  - Interior surfaces are on `Environment` layer
  - `_environmentLayer` includes `Environment` layer
  - Colliders exist on interior surfaces

**Check 3: Layer Separation**
- Outer walls/ceiling on `CorridorShell`? ✅
- Interior surfaces on `Environment`? ✅
- These should be DIFFERENT objects/layers

---

## Summary

**Your concern is valid, but the system is designed to handle it:**

- ✅ Raycasting: Excludes `CorridorShell` when outside, includes `Environment`
- ✅ No-Fly Zone: Always checks `Environment` layer
- ✅ Result: You can place waypoints on interior surfaces from outside, but placement is blocked if too close to obstacles

**The key:** `CorridorShell` and `Environment` are different layers for different purposes:
- `CorridorShell` = Permeable for raycasting (outer boundaries)
- `Environment` = Solid for raycasting AND checked for no-fly zones (interior obstacles)

