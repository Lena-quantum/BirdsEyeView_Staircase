# Birds-Eye View / Dual Viewpoint Setup Guide

This guide explains how to set up the dual viewpoint system that allows waypoint placement from both inside and outside the corridor.

## Overview

The system supports two viewpoints:

1. **Room View (Inside Corridor)**: Player is inside the corridor (1:1 photogrammetry)
   - Ray stops at colliders (walls, ceiling, etc.) - **normal behavior**
   - Waypoint placement and path validation use no-fly zones as before

2. **Birds-Eye View (Outside Corridor)**: Player is outside the corridor, hovering above/around it
   - Ray passes through corridor shell (outer walls/ceiling) to allow clicking into interior
   - Waypoint placement still respects no-fly zones (safety bubble)

## Key Concept

**Raycasting behavior** is decoupled from **drone safety checks**:
- **Raycasting**: Permeable shell from outside, solid from inside
- **Drone Safety**: Always enforced (OverlapSphere/CheckCapsule) regardless of viewpoint

## Setup Steps

### 1. Create CorridorShell Layer

1. Go to **Edit → Project Settings → Tags and Layers**
2. Under **Layers**, find an empty user layer (e.g., Layer 8)
3. Name it **"CorridorShell"**
4. Note the layer index (e.g., 8)

### 2. Assign CorridorShell Layer to Outer Walls/Ceiling

1. Select all outer walls and ceiling GameObjects in your scene
2. In the Inspector, set their **Layer** to **"CorridorShell"**
3. **Important**: Keep interior surfaces (floors, interior walls, stairs) on the **"Environment"** layer (or whatever layer you use for collision detection)

**What should be on CorridorShell:**
- Outer walls (exterior faces of the corridor)
- Ceiling (top surface)
- Any other surfaces that should be "permeable" when viewing from outside

**What should NOT be on CorridorShell:**
- Interior floors
- Interior walls
- Stairs
- Any surfaces you want to be able to place waypoints on

### 3. Set Up CorridorViewpointDetector

1. Create an empty GameObject in your scene (e.g., "CorridorViewpointDetector")
2. Add the `CorridorViewpointDetector` component
3. Configure:
   - **Corridor Model**: (Optional) Drag your main corridor/photogrammetry GameObject here. The component will auto-detect bounds from its Renderer.
   - **Manual Corridor Bounds**: (If no model assigned) Set the center and size of your corridor volume manually
   - **Inside Padding**: 0.5m (default) - expands bounds slightly to prevent edge cases
   - **Player Transform**: (Optional) Drag XR Origin or Main Camera. If null, auto-detects at runtime.

### 4. Configure RayDepthController

1. Find the `RayDepthController` component in your scene
2. In the Inspector, under **"Viewpoint-Aware Raycasting"**:
   - **Viewpoint Detector**: Drag the `CorridorViewpointDetector` GameObject
   - **Corridor Shell Layer**: Set to the layer mask for "CorridorShell" (e.g., if CorridorShell is layer 8, set to "CorridorShell" or use the layer mask dropdown)

**Note**: If left unassigned, the component will auto-find `CorridorViewpointDetector` and auto-detect the "CorridorShell" layer by name.

### 5. Configure PathModeController (Optional)

1. Find the `PathModeController` component in your scene
2. In the Inspector, under **"Viewpoint-Aware Raycasting"**:
   - **Viewpoint Detector**: Drag the same `CorridorViewpointDetector` GameObject
   - **Corridor Shell Layer**: Set to the same layer mask as in `RayDepthController`

**Note**: If left unassigned, auto-detection will occur.

## How It Works

### Inside Corridor (Room View)

1. `CorridorViewpointDetector.IsInsideCorridor` returns `true`
2. Raycasts use normal mask (includes all layers)
3. Ray stops at all colliders (walls, ceiling, etc.)
4. **Behavior is identical to before** - no changes

### Outside Corridor (Birds-Eye View)

1. `CorridorViewpointDetector.IsInsideCorridor` returns `false`
2. Raycasts exclude `CorridorShell` layer
3. Ray passes through outer walls/ceiling
4. Ray still hits interior surfaces (floors, interior walls) on "Environment" layer
5. Waypoint placement still validated by no-fly zones (OverlapSphere/CheckCapsule)

### No-Fly Zone Validation (Always Active)

Regardless of viewpoint:
- `CheckGhostCollisionWithObstacles()` still uses `Physics.OverlapSphere()` with `_droneRadius`
- `SegmentBlockedBetween()` still uses `Physics.CheckCapsule()` with `_droneRadius`
- Both check the `_environmentLayer` (which includes interior surfaces)
- Red warning shells still appear when too close to obstacles

## Testing

1. **Test Inside View**:
   - Position player inside corridor
   - Verify ray stops at walls/ceiling
   - Verify waypoint placement works normally
   - Verify no-fly zones still prevent placement too close to obstacles

2. **Test Outside View**:
   - Position player outside corridor (above or to the side)
   - Verify ray passes through outer walls/ceiling
   - Verify ray still hits interior floors/walls
   - Verify waypoint placement works on interior surfaces
   - Verify no-fly zones still prevent placement too close to ceiling/walls

## Troubleshooting

### Ray Still Stops at Outer Walls

- Check that outer walls are on "CorridorShell" layer
- Check that `CorridorViewpointDetector` is detecting "outside" correctly (check debug gizmos)
- Check that `RayDepthController._corridorShellLayer` is set correctly

### Waypoints Can't Be Placed Inside from Outside

- Verify interior surfaces (floors, interior walls) are on "Environment" layer (not "CorridorShell")
- Check that `_environmentLayer` in `PointPlacementManager` includes the "Environment" layer
- Verify the ray is actually hitting interior surfaces (check with debug ray visualization)

### No-Fly Zones Not Working

- This should not be affected by viewpoint changes
- Verify `_environmentLayer` in `PointPlacementManager` includes interior surfaces
- Check that interior surfaces have colliders
- Verify `_droneRadius` is set correctly (default: 0.45m)

### Viewpoint Detection Not Working

- Check `CorridorViewpointDetector` bounds (enable debug gizmos)
- Verify player transform is assigned or can be auto-detected
- Check that player position is actually outside the bounds when testing

## Technical Details

### Layer Masks

- **CorridorShell Layer**: Only outer walls/ceiling. Excluded from raycasts when outside.
- **Environment Layer**: Interior surfaces. Always checked for raycasts and no-fly zones.

### Detection Method

`CorridorViewpointDetector` uses `Bounds.Contains()` to check if player position is inside the corridor volume. A small padding is added to prevent edge cases.

### Raycast Masks

- **Inside**: `baseMask` (includes all layers)
- **Outside**: `baseMask & ~_corridorShellLayer` (excludes CorridorShell)

### Safety Checks (Unchanged)

- `CheckGhostCollisionWithObstacles()`: Uses `Physics.OverlapSphere(ghostPos, _droneRadius, _environmentLayer)`
- `SegmentBlockedBetween()`: Uses `Physics.CheckCapsule(start, end, _droneRadius, _environmentLayer)`

Both always check `_environmentLayer`, regardless of viewpoint.

