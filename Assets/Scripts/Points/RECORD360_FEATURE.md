# Record360 Two-Point Placement System

## Overview

The Record360 waypoint type now uses a **two-step placement system** that allows users to specify both a path anchor point and a separate recording height. This solves the problem of the drone only filming at the path height, enabling recordings at ceiling level, floor level, or any other height while maintaining a safe navigation path.

---

## User Flow

### Step 1: Place Anchor Point
1. **Select** the Record360 waypoint type from the wrist menu
2. **Position** the ghost sphere where you want the drone to enter/exit the recording zone
   - This is typically at normal navigation height (e.g., 1-2m)
   - This point connects to the rest of the flight path
3. **Press trigger** to place the anchor point
   - A **grey vertical line** appears from floor to ceiling through the anchor
   - A **second red ghost sphere** appears on this line (starts at anchor height)
   - The main ghost sphere is hidden during height adjustment

### Step 2: Adjust Recording Height
1. **Aim your controller** at different heights along the vertical line
   - The red ghost sphere slides up and down to follow your aim
   - It's constrained to the vertical line through the anchor
2. **Fine-tune** the height to where you want the 360° recording to happen
   - Can go all the way to the ceiling
   - Can go down to the floor
   - Or anywhere in between
3. **Press trigger** to confirm the recording height
   - The waypoint is created with both positions stored
   - The vertical line and recording ghost disappear
   - The main ghost reappears for placing more waypoints

---

## Drone Flight Behavior

When the drone reaches a Record360 waypoint with separate anchor and recording positions:

1. **Fly to anchor point** (normal path height)
2. **Pause briefly**
3. **Fly vertically** (straight up or down) to the recording height
4. **Pause briefly**
5. **Perform 360° rotation** at recording height
6. **Pause briefly**
7. **Fly vertically** back to the anchor point
8. **Continue** to the next waypoint in the path

This creates a vertical "detour" that keeps the main path safe while allowing flexible recording positions.

---

## Technical Implementation

### Components

#### `RecordingHeightController.cs`
- Manages the vertical line visual (floor to ceiling)
- Controls the recording ghost sphere that slides along the line
- Handles raycasting to find floor and ceiling heights
- Projects the controller ray onto the vertical line to determine recording height

#### `PointPlacementManager.cs` Updates
- New placement state machine: `None`, `PlacingAnchor`, `AdjustingHeight`
- `HandleRecord360Placement()`: Two-step placement logic
- `ConfirmRecord360Placement()`: Finalize and create waypoint with both positions
- `CancelRecord360Placement()`: Abort the placement process
- `IsAdjustingRecordingHeight`: Property to check current state

#### `PointHandle.cs` Updates
- `RecordingPosition`: Nullable Vector3 storing the recording height
- `HasRecordingPosition`: Whether this waypoint has a separate recording position
- `SetRecordingPosition()`: Method to set the recording position after anchor placement

#### `RayDepthController.cs` Updates
- Checks `IsAdjustingRecordingHeight` in the Update loop
- Calls `UpdateRecordingPointFromRay()` during height adjustment instead of normal ghost positioning
- Trigger press confirms recording height during adjustment

#### `DronePathFollower.cs` Updates
- `Record360Rotation()` coroutine updated to:
  - Check if waypoint has a separate recording position
  - Fly vertically to recording height if different from anchor
  - Perform 360° rotation at recording position
  - Fly back to anchor position before continuing path

---

## Visual Elements

### Vertical Guide Line
- **Color**: Grey (configurable)
- **Width**: 0.5cm (thin, non-intrusive)
- **Height**: Floor to ceiling (auto-detected via raycasts)
- **Purpose**: Visual guide showing the vertical constraint for recording height

### Recording Ghost Sphere
- **Color**: Red (same as Record360 waypoint type)
- **Size**: 0.05m radius (same as placed waypoints)
- **Behavior**: Slides up/down along vertical line, following controller aim
- **Constraint**: Cannot move horizontally, only vertically along the line

### Anchor Waypoint
- **Visual**: Red sphere at the anchor position
- **Purpose**: Shows where the drone enters/exits the recording zone
- **Connection**: Connects to previous and next waypoints in the path

---

## Parameters Stored

For each Record360 waypoint, the following data is stored:

```csharp
PointData {
    Position = anchorPosition,        // Where drone enters/exits
    Type = WaypointType.Record360,
    Parameters = {
        "recording_height": recordingPosition.y,
        "recording_position": recordingPosition,  // Full Vector3
        "duration_s": 15.0f
    }
}
```

The `PointHandle` also stores:
- `RecordingPosition`: Vector3? (nullable) - full 3D position for recording
- `HasRecordingPosition`: bool - whether a separate position was set

---

## Cancellation

Currently, the only way to cancel the height adjustment is:
- Switch to a different waypoint type (this will reset the state)
- Delete the anchor point (if you realize it's in the wrong location)

**Future Enhancement**: Could add a dedicated "Cancel" button or allow Grip+Trigger to cancel.

---

## Configuration

All settings can be adjusted in the Unity Inspector:

### RecordingHeightController
- `Line Color`: Color of the vertical guide line
- `Line Width`: Thickness of the line (default: 0.005m)
- `Max Raycast Height`: Maximum height to search for ceiling (default: 50m)
- `Default Ceiling Height`: Fallback if no ceiling detected (default: 4m)
- `Floor Offset`: Small offset from detected floor (default: 0.1m)

### PointPlacementManager
- `Recording Ghost Transform`: Reference to the sliding ghost sphere
- `Recording Ghost Renderer`: Renderer for coloring the ghost
- `Recording Height Controller`: The controller component (auto-created if missing)

### DronePathFollower
- `Record Pause Seconds`: Pause duration before/after recording (default: 1s)
- `Record 360 Duration`: How long the 360° rotation takes (default: 15s)

---

## Testing Checklist

- [ ] Place a Record360 waypoint at 1m height
- [ ] Vertical line appears from floor to ceiling
- [ ] Recording ghost appears and follows controller aim
- [ ] Recording ghost slides smoothly along the line
- [ ] Can adjust recording height to ceiling
- [ ] Can adjust recording height to floor
- [ ] Trigger confirms the placement
- [ ] Waypoint appears at anchor position (red)
- [ ] Create a path with 3+ waypoints including Record360
- [ ] Fly the drone - it pauses at anchor
- [ ] Drone flies vertically to recording height
- [ ] Drone performs 360° rotation at recording height
- [ ] Drone returns to anchor height
- [ ] Drone continues to next waypoint

---

## Future Enhancements

1. **Visual indicator** on the placed anchor waypoint showing the recording height (e.g., a thin vertical line or arrow)
2. **Dotted line** option for the vertical guide (more subtle)
3. **Snap points** at floor, mid-height, and ceiling for quick selection
4. **Cancel gesture** (e.g., Grip + Trigger) to abort height adjustment
5. **Preview visualization** showing the vertical path the drone will take
6. **Sound feedback** when sliding the recording point up/down

---

## Experiment Data Tracking

The experiment tracking system automatically captures:
- Anchor waypoint position
- Recording height position
- Type: `PointType.Record360`
- All standard waypoint metrics

The separation of anchor and recording positions is preserved in the JSON output for analysis.

