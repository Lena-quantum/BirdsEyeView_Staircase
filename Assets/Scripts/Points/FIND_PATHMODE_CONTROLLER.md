# How to Find GameObject with PathModeController Component

## Method 1: Search in Hierarchy (Easiest)

1. **Open Unity Hierarchy** (usually on the left side)
2. **Click in the search box** at the top of Hierarchy (looks like a magnifying glass 🔍)
3. **Type**: `t:PathModeController` (the `t:` means "type" or "component")
4. **Press Enter**
5. Unity will show only GameObjects that have `PathModeController` component
6. **Click** on the GameObject that appears

**Visual Guide:**
```
Hierarchy
┌─────────────────────────────┐
│ 🔍 [t:PathModeController]   │ ← Type here
├─────────────────────────────┤
│ ✅ GameObjectName            │ ← This GameObject has the component
└─────────────────────────────┘
```

---

## Method 2: Search by Component Name

1. **In Hierarchy**, click the search box
2. **Type**: `PathModeController` (without `t:`)
3. Unity will filter to show matching GameObjects
4. **Click** on the one that has the component

---

## Method 3: Use Find Objects

1. **In Unity menu bar**, go to: `Edit` → `Find` → `Find References In Scene`
2. **OR** press `Ctrl+F` / `Cmd+F` (if enabled)
3. **Type**: `PathModeController`
4. Unity will highlight GameObjects with this component

---

## Method 4: Manual Search (If you know the name)

1. **Look in Hierarchy** for common names:
   - "PathModeController"
   - "FlightPathManager" (might be on the same GameObject)
   - "PathRenderer" (might be on the same GameObject)
   - "FlightPathSetup" (setup GameObject)
   - Any GameObject you remember adding the component to

2. **Click** on each GameObject
3. **Check Inspector** - if you see "Path Mode Controller (Script)" component, that's it!

---

## Method 5: Check All GameObjects

1. **In Hierarchy**, expand all GameObjects (click arrows to expand)
2. **Click** on each GameObject one by one
3. **Look in Inspector** (right side) for "Path Mode Controller (Script)"
4. When you find it, that's your GameObject!

---

## Method 6: Use Project Search (Find Script Usage)

1. **In Project window**, find `PathModeController.cs`
2. **Right-click** on the script file
3. **Select**: "Find References In Scene" (if available)
4. Unity will show which GameObjects use this script

---

## What to Look For in Inspector

When you select a GameObject, the Inspector shows:

```
Inspector
┌─────────────────────────────┐
│ GameObject Name             │
│ Tag: Untagged                │
│ Layer: Default               │
├─────────────────────────────┤
│ Transform                   │
│ ├── Position                │
│ ├── Rotation                │
│ └── Scale                   │
├─────────────────────────────┤
│ Path Mode Controller (Script) ← THIS IS IT!
│ ├── Path Manager: [...]     │
│ ├── Point Manager: [...]    │
│ └── ...                     │
└─────────────────────────────┘
```

**If you see "Path Mode Controller (Script)"**, that's the right GameObject!

---

## Common Locations

The `PathModeController` component is usually on:

- **FlightPathSetup** GameObject (if you used the setup script)
- **FlightPathManager** GameObject (same GameObject as FlightPathManager)
- **PathRenderer** GameObject (same GameObject as PathRenderer)
- A dedicated **"PathModeController"** GameObject
- **XR Origin** (or a child of it) - less common

---

## Quick Steps Summary

**Easiest Method:**
1. **Hierarchy** → Click search box 🔍
2. **Type**: `t:PathModeController`
3. **Click** the GameObject that appears
4. **Check Inspector** - you should see "Path Mode Controller (Script)"
5. **Scroll down** to find "Viewpoint-Aware Raycasting" section

---

## Visual Example

```
Unity Editor Layout:

┌─────────────┬──────────────────┬─────────────────┐
│  Hierarchy  │     Scene View   │    Inspector    │
├─────────────┼──────────────────┼─────────────────┤
│ 🔍 [search] │                  │                 │
│ Scene       │                  │ Path Mode       │
│ ├── XR...   │                  │ Controller      │
│ ├── Flight..│ ← Click this     │ (Script)        │
│ └── ...     │                  │ ├── Path Mgr    │
│             │                  │ └── Viewpoint...│
└─────────────┴──────────────────┴─────────────────┘
```

---

## After You Find It

1. **Select** the GameObject (it should be highlighted in Hierarchy)
2. **Look at Inspector** (right side)
3. **Find** "Path Mode Controller (Script)" component
4. **Scroll down** in that component
5. **Look for** section: **"Viewpoint-Aware Raycasting"**
6. **You'll see:**
   - **"Viewpoint Detector"** field (Object field)
   - **"Corridor Shell Layer"** field (LayerMask dropdown)

---

## Assigning CorridorViewpointDetector

Once you found the GameObject:

1. **Make sure** you have created "CorridorViewpointDetector" GameObject (see CREATE_VIEWPOINT_DETECTOR.md)
2. **In Inspector**, scroll to "Viewpoint-Aware Raycasting" section
3. **Drag** the "CorridorViewpointDetector" GameObject from Hierarchy into "Viewpoint Detector" field
   - OR click the circle/target icon next to the field and select it
4. **Set** "Corridor Shell Layer" to "CorridorShell" (from dropdown)

---

## Troubleshooting

### Problem: Search doesn't find anything

**Solution:**
- Make sure the GameObject has the component attached
- Try searching without `t:` prefix
- Check if component is disabled (checkbox might be unchecked)
- The component might be on a child GameObject - expand parent objects

### Problem: Can't see Inspector

**Solution:**
- **Window** → **General** → **Inspector** (or press `Ctrl+3` / `Cmd+3`)
- Make sure Inspector window is open

### Problem: Component is on a child GameObject

**Solution:**
- Expand parent GameObjects in Hierarchy (click arrows)
- Check child GameObjects
- The component might be on a child, not the parent

### Problem: Multiple GameObjects Found

**Solution:**
- Click on each one
- Check Inspector to see which one has "Path Mode Controller (Script)"
- Usually there should only be one in the scene

---

## Pro Tip: Bookmark It

Once you find it:
1. **Right-click** the GameObject in Hierarchy
2. **Select**: "Add to Favorites" (if available)
3. Or just remember its name for next time!

---

## Complete Setup Checklist

After finding PathModeController GameObject:

- [ ] Found GameObject with PathModeController component
- [ ] Opened Inspector and see "Path Mode Controller (Script)"
- [ ] Scrolled to "Viewpoint-Aware Raycasting" section
- [ ] Created "CorridorViewpointDetector" GameObject (if not done yet)
- [ ] Dragged "CorridorViewpointDetector" into "Viewpoint Detector" field
- [ ] Set "Corridor Shell Layer" to "CorridorShell"
- [ ] Same steps completed for RayDepthController

---

## Summary

**Quick Steps:**
1. **Hierarchy** → Search: `t:PathModeController`
2. **Click** the GameObject
3. **Inspector** → Scroll to "Viewpoint-Aware Raycasting"
4. **Drag** "CorridorViewpointDetector" into field
5. **Set** "Corridor Shell Layer" to "CorridorShell"

Done! ✅

