# How to Create and Assign CorridorViewpointDetector

## Step-by-Step: Create the GameObject First

You need to **create the CorridorViewpointDetector GameObject** BEFORE you can drag it into the fields.

---

## STEP 1: Create the GameObject

1. **In Unity Hierarchy**, right-click in empty space
2. Select **"Create Empty"** (or press `Ctrl+Shift+N` / `Cmd+Shift+N`)
3. **Rename** it to: **"CorridorViewpointDetector"**
   - Click on the name and type the new name

---

## STEP 2: Add the Component

1. **Select** the "CorridorViewpointDetector" GameObject (click it in Hierarchy)
2. In **Inspector**, click **"Add Component"** button
3. **Type**: `CorridorViewpointDetector` (or just `Corridor`)
4. **Select** "Corridor Viewpoint Detector" from the dropdown
5. **Click** to add it

**Alternative method:**
- In Hierarchy, select "CorridorViewpointDetector"
- In Inspector, click **"Add Component"**
- Search for: **"Corridor"** or **"Viewpoint"**
- Click on **"Corridor Viewpoint Detector"**

---

## STEP 3: Configure the Component (Optional)

1. **Corridor Model**: Drag your corridor/photogrammetry GameObject here (optional)
2. **Manual Corridor Bounds**: Set manually if no model (optional)
3. **Player Transform**: Drag "XR Origin" here (optional - will auto-detect)

---

## STEP 4: Now Drag It Into RayDepthController

1. **Select** the GameObject that has `RayDepthController` component
2. In **Inspector**, scroll to **"Viewpoint-Aware Raycasting"** section
3. **Find** the **"Viewpoint Detector"** field
4. **Drag** the "CorridorViewpointDetector" GameObject from Hierarchy into this field
   - OR click the circle/target icon next to the field and select it

**Visual Guide:**
```
Hierarchy                          Inspector
├── XR Origin                     RayDepthController
├── CorridorViewpointDetector ←   ├── ... (fields) ...
│   └── (Component)                └── Viewpoint-Aware Raycasting
└── ...                            ├── Viewpoint Detector: [Drag here ←]
```

---

## STEP 5: Do the Same for PathModeController

1. **Select** the GameObject that has `PathModeController` component
2. In **Inspector**, scroll to **"Viewpoint-Aware Raycasting"** section
3. **Drag** the same "CorridorViewpointDetector" GameObject into **"Viewpoint Detector"** field

---

## Troubleshooting: Can't Drag

### Problem: Field is Grayed Out / Disabled

**Solution:** The field might be disabled. Check if there's a checkbox or if the component is disabled.

### Problem: Nothing Happens When Dragging

**Solution:** 
1. Make sure you're dragging the **GameObject** (from Hierarchy), not the component
2. Try clicking the **circle/target icon** next to the field instead
3. In the object picker, search for "CorridorViewpointDetector"

### Problem: Field Says "None (Corridor Viewpoint Detector)" But Won't Accept Drag

**Solution:**
1. Make sure the GameObject has the `CorridorViewpointDetector` component attached
2. Check Unity Console for errors
3. Try restarting Unity if it's stuck

### Problem: Can't Find "Corridor Viewpoint Detector" in Add Component Menu

**Solution:**
1. Check Unity Console for compilation errors
2. Make sure `CorridorViewpointDetector.cs` exists in `Assets/Scripts/Points/`
3. Wait for Unity to finish compiling
4. Try typing the full name: "CorridorViewpointDetector"

---

## Quick Verification

After setup, you should have:

**Hierarchy:**
```
Scene
├── XR Origin
├── CorridorViewpointDetector  ← This GameObject exists
│   └── (Corridor Viewpoint Detector component)
└── ... (other objects)
```

**Inspector (RayDepthController):**
```
Viewpoint-Aware Raycasting
├── Viewpoint Detector: [CorridorViewpointDetector]  ← Shows GameObject name
└── Corridor Shell Layer: [CorridorShell]
```

**Inspector (PathModeController):**
```
Viewpoint-Aware Raycasting
├── Viewpoint Detector: [CorridorViewpointDetector]  ← Shows GameObject name
└── Corridor Shell Layer: [CorridorShell]
```

---

## Alternative: Use Object Picker

If dragging doesn't work:

1. **Click** the **circle/target icon** next to "Viewpoint Detector" field
2. **Search** for "CorridorViewpointDetector" in the object picker
3. **Click** on it to assign

---

## Summary

**The key point:** You must create the GameObject FIRST, then you can drag it into the fields.

1. ✅ Create Empty GameObject
2. ✅ Rename to "CorridorViewpointDetector"
3. ✅ Add "Corridor Viewpoint Detector" component
4. ✅ Drag GameObject into RayDepthController field
5. ✅ Drag same GameObject into PathModeController field

