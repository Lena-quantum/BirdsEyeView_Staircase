# Experiment Tracking - Quick Reference Card

## 🎯 Quick Setup Checklist

- [ ] Create empty GameObject named `ExperimentManager`
- [ ] Add `ExperimentDataManager` component
- [ ] Set Participant ID (P01-P16)
- [ ] Set Task Variant (dropdown)
- [ ] Assign Main Camera (auto-finds)
- [ ] Create Submit button in wrist menu
- [ ] Add `ExperimentSubmitButton` component to button
- [ ] Test in Play mode
- [ ] Build APK
- [ ] Extract data from Quest 2

## 📋 Inspector Settings

### ExperimentDataManager Component:
| Field | Value |
|-------|-------|
| **Participant ID** | `P01` (change per participant) |
| **Task Variant** | Select from dropdown |
| **Main Camera** | Auto-assigned |
| **Flight Path Manager** | Auto-found |
| **Point Placement Manager** | Auto-found |
| **Drone Path Follower** | Auto-found |
| **Movement Sample Rate** | `0.1` (10Hz) |

## 🔢 Task Variants

| Build # | Task Variant | Participants |
|---------|-------------|--------------|
| 1 | `RoomView_Corridor` | P01-P16 |
| 2 | `RoomView_Staircase` | P01-P16 |
| 3 | `BirdsEye_Corridor` | P01-P16 |
| 4 | `BirdsEye_Staircase` | P01-P16 |

## 📊 Key Metrics Tracked

| Category | Metrics |
|----------|---------|
| **Time** | Trial duration, timestamps |
| **Waypoints** | Placed, deleted, final count, types |
| **Path** | 3D length, horizontal length, vertical change |
| **Quality** | Smoothness (angular changes), segment stats |
| **Errors** | Blocked placements, blocked segments |
| **Movement** | Camera positions (10Hz), total distance |
| **Drone** | Flight duration, success status |

## 💾 Data Extraction

### From Quest 2:
```bash
adb pull /sdcard/Android/data/com.YourCompany.VRTestProject/files/ExperimentData/ ./ExperimentData/
```

### File Location:
```
ExperimentData/
└── [TaskVariant]/
    └── [ParticipantID]_[TaskVariant].json
```

### Example:
```
ExperimentData/
└── RoomView_Corridor/
    └── P01_RoomView_Corridor.json
```

## 🔍 JSON Structure (Key Fields)

```json
{
  "metadata": {
    "participantId": "P01",
    "taskVariant": "RoomView_Corridor",
    "durationSeconds": 290
  },
  "waypointMetrics": {
    "totalPlaced": 15,
    "finalCount": 12
  },
  "pathMetrics": {
    "totalLength3D": 47.5,
    "avgAngularChangeDeg": 45.2
  },
  "errorMetrics": {
    "placementBlockedCount": 5
  },
  "participantMovement": {
    "totalDistanceMoved": 45.3
  }
}
```

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| "Instance is null" | Ensure ExperimentManager exists in scene |
| "No data saved" | Check Quest 2 app permissions (Storage) |
| "Submit doesn't work" | Ensure button has ExperimentSubmitButton component |
| "Movement is empty" | Ensure Main Camera is assigned |

## 🔄 Workflow Per Participant

1. **Set** Participant ID in Inspector
2. **Build** APK
3. **Install** on Quest 2
4. **Run** trial (participant wears headset)
5. **Submit** (participant presses button)
6. **Extract** data via USB
7. **Repeat** for next participant

## 📈 Expected File Count

- **Per build**: 16 files (one per participant)
- **Total**: 64 files (4 variants × 16 participants)

## ⏱️ Timing

- **Setup**: ~5 minutes
- **Per build**: ~2-3 minutes
- **Data extraction**: ~10 seconds
- **Per participant**: Trial duration only

## 📞 Files Reference

- **Setup Guide**: `ExperimentSetup.md`
- **Full Summary**: `IMPLEMENTATION_SUMMARY.md`
- **This Card**: `QUICK_REFERENCE.md`

---

**TIP**: Print this card and keep it next to your workstation during data collection! 📄

