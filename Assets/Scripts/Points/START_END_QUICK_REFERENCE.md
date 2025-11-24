# Start/End Points - Quick Reference Card

## 🎯 What They Do
Force path structure: **Start → waypoints → End**

---

## 🎨 Visual Design

| Point | Color | Label | Size |
|-------|-------|-------|------|
| Start | White | "START" (black) | 0.5 × 0.5 × 0.5 |
| End | Black | "END" (white) | 0.5 × 0.5 × 0.5 |

---

## ✅ Valid Path Building

```
1. Point at Start → Press Trigger ✅
2. Point at Waypoint A → Press Trigger ✅
3. Point at Waypoint B → Press Trigger ✅
4. Point at End → Press Trigger ✅

Result: Start → A → B → End
```

---

## ❌ Invalid Actions

| Action | Error |
|--------|-------|
| Waypoint → Waypoint (first) | "Path must start at Start point" |
| Start → Start | "Start point already connected" |
| Waypoint → Start | "Cannot connect to Start point" |
| End → Waypoint | "Cannot connect from End point" |

---

## 🛠️ Setup (5 Steps)

1. **Create GameObject** → Add `StartEndPoint` component
2. **Add white/black box** (0.5 × 0.5 × 0.5)
3. **Add text label** ("START" / "END")
4. **Position on floor** (Y = 0)
5. **Assign references** in Inspector

**Full guide**: `START_END_SETUP_GUIDE.md`

---

## 📊 Data Output

```json
{
  "pathMetrics": {
    "startPosition": {"x": 1.0, "y": 0.0, "z": 2.0},
    "endPosition": {"x": 10.0, "y": 0.0, "z": 15.0}
  }
}
```

---

## 🐛 Troubleshooting

| Issue | Fix |
|-------|-----|
| "No Start point found" | Create StartPoint GameObject + component |
| Start/End not visible | Assign Box Renderer in Inspector |
| Errors not showing | Assign PathWarningPopup in PathModeController |
| Path skips Start/End | Update PathRenderer.cs to latest version |

---

## 🧪 Testing

1. Try connecting waypoint → waypoint first ❌
2. Connect Start → waypoint ✅
3. Connect waypoint → End ✅
4. Check JSON has startPosition/endPosition ✅

---

## 📁 Files

- `StartEndPoint.cs` - Component script
- `FlightPathManager.cs` - Validation logic
- `PathModeController.cs` - Input handling
- `PathRenderer.cs` - Visualization
- `ExperimentDataManager.cs` - Data tracking

---

**Status**: ✅ Ready to use

**See**: `START_END_SETUP_GUIDE.md` for details

