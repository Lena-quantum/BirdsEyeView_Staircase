# Experiment Data Tracking System - Setup Guide

## Overview
This system automatically tracks all user interactions during the VR drone path planning experiment and saves detailed data to JSON files.

## Quick Setup (5 minutes)

### 1. Add ExperimentDataManager to Scene

1. In **Hierarchy**, create an empty GameObject: `Right-click → Create Empty`
2. Rename it to **`ExperimentManager`**
3. Add the component: **`ExperimentDataManager`**
4. Configure in Inspector:
   - **Participant ID**: Set to `P01` (or P02, P03... up to P16)
   - **Task Variant**: Select from dropdown:
     - `RoomView_Corridor`
     - `RoomView_Staircase`
     - `BirdsEye_Corridor`
     - `BirdsEye_Staircase`
   - **Main Camera**: Drag your Main Camera (should auto-assign)
   - **Flight Path Manager**: Drag from scene (should auto-find)
   - **Point Placement Manager**: Drag from scene (should auto-find)
   - **Drone Path Follower**: Drag from scene (should auto-find)
   - **Movement Sample Rate**: Leave at `0.1` (10Hz tracking)

### 2. Add Submit Button to Wrist Menu

1. Open your wrist menu prefab/GameObject
2. Add a new button called **`SubmitButton`**
3. Style it like your other buttons (same size, colors, etc.)
4. Add the **`UIButtonScaleFeedback`** component if you want hover animations
5. **Important**: Do NOT add `ExperimentSubmitButton` yet - you'll connect it manually

### 3. Connect Submit Button (After Setup)

Once your Submit button is created:
1. Add component **`ExperimentSubmitButton`** to the button GameObject
2. In Inspector, the script will handle everything automatically

## What Gets Tracked

### Automatically Tracked:
- ✅ **Trial duration** (from scene load to submit)
- ✅ **All waypoint placements** (position, type, yaw, timestamp)
- ✅ **All waypoint deletions**
- ✅ **Path segments** (connections between waypoints, distances, angles)
- ✅ **Blocked placements** (attempts to place in no-fly zones)
- ✅ **Blocked segments** (attempts to connect through no-fly zones)
- ✅ **Participant movement** (camera position sampled at 10Hz)
- ✅ **Total distance moved**
- ✅ **Drone flight** (start time, duration, success/failure)
- ✅ **Path quality metrics** (smoothness, angular changes, lengths)
- ✅ **Event log** (timestamped list of all interactions)

### Calculated Metrics:
- **Path smoothness** (average angular change between segments)
- **Path length** (3D distance, horizontal distance, vertical change)
- **Segment statistics** (average length, standard deviation)
- **Point type breakdown** (how many fly-through, stop-rotate, record-360)

## Data Output

### File Location (Quest 2):
```
/sdcard/Android/data/com.YourCompany.VRTestProject/files/ExperimentData/
├── RoomView_Corridor/
│   ├── P01_RoomView_Corridor.json
│   ├── P02_RoomView_Corridor.json
│   └── ... (up to P16)
```

### Extracting Data from Quest 2:
```bash
# Connect Quest 2 via USB
# Enable Developer Mode and USB Debugging
# Run this command:
adb pull /sdcard/Android/data/com.YourCompany.VRTestProject/files/ExperimentData/ ./ExperimentData/
```

### JSON Structure (Example):
```json
{
  "metadata": {
    "participantId": "P01",
    "taskVariant": "RoomView_Corridor",
    "viewMode": "RoomView",
    "environment": "Corridor",
    "timestampStart": "2025-11-18T14:32:10Z",
    "timestampEnd": "2025-11-18T14:37:00Z",
    "durationSeconds": 290
  },
  "waypointMetrics": {
    "totalPlaced": 15,
    "totalDeleted": 3,
    "finalCount": 12,
    "byType": {
      "FlyThrough": 8,
      "StopRotate": 3,
      "Record360": 1
    }
  },
  "pathMetrics": {
    "totalLength3D": 47.5,
    "totalLengthHorizontal2D": 42.1,
    "totalVerticalChange": 8.3,
    "avgAngularChangeDeg": 45.2,
    "maxAngularChangeDeg": 87.5
  },
  "errorMetrics": {
    "placementBlockedCount": 5,
    "segmentBlockedCount": 2,
    "droneFlightFailed": false
  },
  "participantMovement": {
    "totalDistanceMoved": 45.3,
    "samples": [
      {"time": 0.0, "position": {"x": 0, "y": 1.7, "z": 0}},
      {"time": 0.1, "position": {"x": 0.1, "y": 1.7, "z": 0.05}}
    ]
  }
  // ... and much more!
}
```

## Workflow for Data Collection

### Per Build (e.g., RoomView_Corridor):

1. **Set Task Variant** in Inspector: `RoomView_Corridor`
2. **Set Participant ID**: `P01`
3. **Build APK** for Quest 2
4. **Install on Quest 2**
5. **Participant completes trial**
6. **Participant presses "Submit" button** on wrist menu
7. **System shows "Trial Complete!" message**
8. **Extract JSON via USB**:
   ```bash
   adb pull /sdcard/Android/data/com.YourCompany.VRTestProject/files/ExperimentData/RoomView_Corridor/P01_RoomView_Corridor.json
   ```
9. **Repeat for P02-P16** (rebuild with new ID each time)

### For Next Task Variant:

1. Change **Task Variant** to `RoomView_Staircase` (or next variant)
2. Reset **Participant ID** to `P01`
3. Rebuild and repeat process

## Analysis

### Importing to Python (pandas):
```python
import pandas as pd
import json

# Load JSON
with open('P01_RoomView_Corridor.json') as f:
    data = json.load(f)

# Access metrics
print(f"Duration: {data['metadata']['durationSeconds']}s")
print(f"Points placed: {data['waypointMetrics']['totalPlaced']}")
print(f"Path length: {data['pathMetrics']['totalLength3D']}m")
```

### Creating Summary CSV:
You can manually create a CSV from multiple JSON files, or use Python:
```python
import pandas as pd
import json
import glob

rows = []
for file in glob.glob('ExperimentData/*/*.json'):
    with open(file) as f:
        data = json.load(f)
        rows.append({
            'ParticipantID': data['metadata']['participantId'],
            'TaskVariant': data['metadata']['taskVariant'],
            'Duration': data['metadata']['durationSeconds'],
            'PointsPlaced': data['waypointMetrics']['totalPlaced'],
            'PathLength3D': data['pathMetrics']['totalLength3D'],
            'AvgAngularChange': data['pathMetrics']['avgAngularChangeDeg'],
            'PlacementErrors': data['errorMetrics']['placementBlockedCount']
        })

df = pd.DataFrame(rows)
df.to_csv('experiment_summary.csv', index=False)
```

## Troubleshooting

### "ExperimentDataManager.Instance is null"
- Ensure `ExperimentManager` GameObject exists in scene
- Ensure it has `ExperimentDataManager` component
- Ensure it's not disabled

### "No data file created"
- Check Quest 2 permissions: Settings → Apps → Your App → Permissions → Storage
- Check logs for save errors
- Ensure `Submit` button called `SubmitTrial()`

### "Movement samples are empty"
- Ensure Main Camera is assigned in Inspector
- Ensure trial is active (scene loaded)

### "Drone flight duration is 0"
- Ensure `DronePathFollower` is assigned
- Ensure drone flight was started with Play button
- Ensure flight completed (didn't just pause)

## Notes

- **One trial per build**: Each run saves ONE JSON file (overwrites if participant ID + variant match)
- **No CSV auto-generation**: JSON only. Create CSV manually or with Python script.
- **10Hz movement tracking**: 600 samples per minute. 5-minute trial = ~3,000 position samples.
- **File size**: Expect ~50-200KB per JSON file depending on trial complexity.
- **Timestamps**: UTC timezone, ISO 8601 format.

## Contact

For questions or issues, refer to the main project documentation or contact the research team.

