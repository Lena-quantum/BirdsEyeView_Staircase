# Start/End Point Feature - Technical Summary

## Feature Overview

Pre-placed **Start** and **End** markers that enforce a strict path structure: Start → waypoints → End. This ensures the real-life drone has consistent takeoff/landing positions across all trials.

---

## Key Components

### 1. **StartEndPoint.cs**
- Component attached to Start/End GameObjects
- Stores point type (Start or End)
- Registers with `FlightPathManager` on scene load
- IDs: Start = -1, End = -2

### 2. **FlightPathManager.cs** (Modified)
- `RegisterStartEndPoints()`: Finds and registers Start/End in scene
- `CanCreateSegment(fromId, toId)`: Validates segment before creation
- `IsPathComplete()`: Checks if path starts with Start and ends with End
- `OnPathValidationError` event: Sends error messages to UI

### 3. **PathModeController.cs** (Modified)
- Calls `CanCreateSegment()` before adding points to route
- Listens to `OnPathValidationError` and displays in `PathWarningPopup`
- Blocks invalid connections with haptic feedback

### 4. **PathRenderer.cs** (Modified)
- `GetPointPosition()`: Helper to get position for any point ID (including Start/End)
- Renders Start/End in path lines
- Shows "START" and "END" badges instead of numbers

### 5. **ExperimentDataManager.cs** (Modified)
- `PathMetrics` now includes `startPosition` and `endPosition`
- Automatically captured and saved to JSON

---

## Validation Rules

### ✅ Allowed
- **First segment**: Start → Waypoint
- **Middle segments**: Waypoint → Waypoint
- **Last segment**: Waypoint → End
- **Start**: ONE outgoing connection only
- **End**: ONE incoming connection only

### ❌ Blocked
- Starting from waypoint (not Start)
- Connecting TO Start
- Connecting FROM End
- Multiple connections from Start
- Multiple connections to End

---

## Error Messages

| Action | Error Message |
|--------|---------------|
| First segment not from Start | "Path must start at Start point" |
| Start already connected | "Start point already connected" |
| Trying to connect TO Start | "Cannot connect to Start point" |
| Trying to connect FROM End | "Cannot connect from End point" |
| End already connected | "End point already connected" |

---

## Visual Design

| Element | Color | Size | Label |
|---------|-------|------|-------|
| Start Point | White (`#FFFFFF`) | 0.5 × 0.5 × 0.5 | "START" (black text) |
| End Point | Black (`#000000`) | 0.5 × 0.5 × 0.5 | "END" (white text) |

- **Position**: On floor (Y = 0)
- **No vertical beam** (unlike Record360 waypoints)
- **Always visible** (cannot be deleted by participant)

---

## Data Output

### JSON Structure
```json
{
  "pathMetrics": {
    "startPosition": {
      "x": 1.0,
      "y": 0.0,
      "z": 2.0
    },
    "endPosition": {
      "x": 10.0,
      "y": 0.0,
      "z": 15.0
    },
    "totalLength3D": 15.3,
    "segmentCount": 3,
    ...
  }
}
```

---

## Setup Workflow

1. Create `StartPoint` GameObject → Add `StartEndPoint` component (Type = Start)
2. Add white box visual + "START" label
3. Create `EndPoint` GameObject → Add `StartEndPoint` component (Type = End)
4. Add black box visual + "END" label
5. Position both on floor at desired locations
6. Assign references in Inspector
7. Test in Play Mode

**See `START_END_SETUP_GUIDE.md` for detailed instructions.**

---

## Integration Points

### FlightPathManager
- Automatically finds Start/End on `Start()`
- Validates segments in `CanCreateSegment()`
- Provides Start/End references via `GetStartPoint()` / `GetEndPoint()`

### PathModeController
- Checks validation before adding points
- Shows error popup on invalid actions
- Provides haptic feedback

### PathRenderer
- Renders Start/End in path lines
- Shows custom badges ("START" / "END")
- Handles negative IDs correctly

### ExperimentDataManager
- Captures Start/End positions in `CalculatePathMetrics()`
- Saves to JSON automatically

---

## Testing Checklist

- [ ] Start/End points visible in scene
- [ ] Console shows registration messages on Play
- [ ] Cannot start path from waypoint
- [ ] Can connect Start → Waypoint
- [ ] Cannot connect Waypoint → Start
- [ ] Can connect Waypoint → End
- [ ] Cannot connect End → Waypoint
- [ ] Path renders Start → waypoints → End
- [ ] Badges show "START" and "END"
- [ ] JSON includes startPosition and endPosition

---

## Known Limitations

1. **Single Start/End per scene**: System uses first found Start/End if multiple exist
2. **No dynamic repositioning**: Start/End positions are fixed at scene load
3. **No deletion protection in Editor**: Can still be deleted manually (but shouldn't be)

---

## Future Enhancements (Optional)

- [ ] Visual highlight when hovering over Start/End in VR
- [ ] Snap-to-grid for precise positioning
- [ ] Config file for Start/End positions per variant
- [ ] Runtime validation on trial start (check path completeness)
- [ ] Custom colors per task variant

---

## Files Modified

- ✅ `Assets/Scripts/Points/StartEndPoint.cs` (NEW)
- ✅ `Assets/Scripts/Points/FlightPathManager.cs` (MODIFIED)
- ✅ `Assets/Scripts/Points/PathModeController.cs` (MODIFIED)
- ✅ `Assets/Scripts/Points/PathRenderer.cs` (MODIFIED)
- ✅ `Assets/Scripts/Experiment/ExperimentDataManager.cs` (MODIFIED)

---

## Documentation

- 📘 `START_END_SETUP_GUIDE.md` - Detailed setup instructions
- 📘 `START_END_FEATURE_SUMMARY.md` - This file (technical overview)

---

**Feature Status**: ✅ Complete and ready for testing

**Next Steps**:
1. Follow setup guide to create Start/End points
2. Test path building with validation
3. Verify JSON output includes Start/End positions
4. Run pilot trial before full study

🚀 **Good luck with your experiment!**

