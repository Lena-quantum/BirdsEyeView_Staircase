# How to Find GameObjects with Specific Components in Unity

## Method 1: Search in Hierarchy (Easiest)

1. **Open Unity Hierarchy** (usually on the left side)
2. **Click in the search box** at the top of Hierarchy (looks like a magnifying glass 🔍)
3. **Type**: `t:RayDepthController` (the `t:` means "type" or "component")
4. **Press Enter**
5. Unity will show only GameObjects that have `RayDepthController` component
6. **Click** on the GameObject that appears

**Visual Guide:**
```
Hierarchy
┌─────────────────────────────┐
│ 🔍 [t:RayDepthController]   │ ← Type here
├─────────────────────────────┤
│ ✅ GameObjectName            │ ← This GameObject has the component
└─────────────────────────────┘
```

---

## Method 2: Search by Component Name

1. **In Hierarchy**, click the search box
2. **Type**: `RayDepthController` (without `t:`)
3. Unity will filter to show matching GameObjects
4. **Click** on the one that has the component

---

## Method 3: Use Find Objects

1. **In Unity menu bar**, go to: `Edit` → `Find` → `Find References In Scene`
2. **OR** press `Ctrl+F` / `Cmd+F` (if enabled)
3. **Type**: `RayDepthController`
4. Unity will highlight GameObjects with this component

---

## Method 4: Manual Search (If you know the name)

1. **Look in Hierarchy** for common names:
   - "RayDepthController"
   - "RightController"
   - "XR Origin" (might be a child)
   - "Player"
   - "VRController"
   - Any GameObject you remember adding the component to

2. **Click** on each GameObject
3. **Check Inspector** - if you see "Ray Depth Controller (Script)" component, that's it!

---

## Method 5: Check All GameObjects

1. **In Hierarchy**, expand all GameObjects (click arrows to expand)
2. **Click** on each GameObject one by one
3. **Look in Inspector** (right side) for "Ray Depth Controller (Script)"
4. When you find it, that's your GameObject!

---

## Method 6: Use Project Search (Find Script Usage)

1. **In Project window**, find `RayDepthController.cs`
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
│ Ray Depth Controller (Script) ← THIS IS IT!
│ ├── Manager: [...]          │
│ ├── Right Controller...    │
│ └── ...                     │
└─────────────────────────────┘
```

**If you see "Ray Depth Controller (Script)"**, that's the right GameObject!

---

## Common Locations

The `RayDepthController` component is usually on:

- **XR Origin** (or a child of it)
- **Right Controller** GameObject
- **Player** GameObject
- **VR Controller** GameObject
- A dedicated **"RayDepthController"** GameObject

---

## Quick Steps Summary

**Easiest Method:**
1. **Hierarchy** → Click search box 🔍
2. **Type**: `t:RayDepthController`
3. **Click** the GameObject that appears
4. **Check Inspector** - you should see "Ray Depth Controller (Script)"
5. **Scroll down** to find "Viewpoint-Aware Raycasting" section

---

## Same for PathModeController

To find the GameObject with `PathModeController`:

1. **Hierarchy** → Search box
2. **Type**: `t:PathModeController`
3. **Click** the GameObject
4. **Check Inspector** for "Path Mode Controller (Script)"

---

## Visual Example

```
Unity Editor Layout:

┌─────────────┬──────────────────┬─────────────────┐
│  Hierarchy  │     Scene View   │    Inspector    │
├─────────────┼──────────────────┼─────────────────┤
│ 🔍 [search] │                  │                 │
│ Scene       │                  │ Ray Depth       │
│ ├── XR...   │                  │ Controller      │
│ ├── Right...│ ← Click this     │ (Script)        │
│ └── ...     │                  │ ├── Manager     │
│             │                  │ └── Viewpoint...│
└─────────────┴──────────────────┴─────────────────┘
```

---

## Troubleshooting

### Problem: Search doesn't find anything

**Solution:**
- Make sure the GameObject has the component attached
- Try searching without `t:` prefix
- Check if component is disabled (checkbox might be unchecked)

### Problem: Can't see Inspector

**Solution:**
- **Window** → **General** → **Inspector** (or press `Ctrl+3` / `Cmd+3`)
- Make sure Inspector window is open

### Problem: Component is on a child GameObject

**Solution:**
- Expand parent GameObjects in Hierarchy (click arrows)
- Check child GameObjects
- The component might be on a child, not the parent

---

## Pro Tip: Bookmark It

Once you find it:
1. **Right-click** the GameObject in Hierarchy
2. **Select**: "Add to Favorites" (if available)
3. Or just remember its name for next time!

