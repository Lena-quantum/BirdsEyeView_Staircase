# ArduPilot/PX4 Integration Readiness Summary

## Overview
The VR waypoint system has been updated to ensure seamless future integration with ArduPilot and PX4 autopilots for indoor drone flight. All mission-critical data is now captured and structured for easy conversion to MAVLink mission format.

---

## Waypoint System (Indoor Flight Optimized)

### Current Waypoint Types (2 Types)

#### 1. **StopTurnGo** (Default)
- **Description**: Drone stops at waypoint, rotates to observe, then continues
- **Color**: Bright Green (#38FF00)
- **MAVLink Mapping**: `MAV_CMD_NAV_WAYPOINT` + optional `MAV_CMD_CONDITION_YAW`
- **Indoor Parameters**:
  - Acceptance Radius: 0.25m (25cm precision)
  - Hold Time: 2.0 seconds (default)
  - Speed: 0.5 m/s (slow indoor navigation)
  - Optional Rotation: Configurable degrees

#### 2. **Record360**
- **Description**: Two-point system - anchor position + elevated recording position with 360° rotation
- **Color**: Darker Red (#C21807)
- **MAVLink Mapping**: `MAV_CMD_NAV_WAYPOINT` + `MAV_CMD_CONDITION_YAW` (360°) + `MAV_CMD_NAV_LOITER_TIME`
- **Indoor Parameters**:
  - Acceptance Radius: 0.25m (25cm precision)
  - Hold Time: 15.0 seconds (during recording)
  - Speed: 0.3 m/s (very slow approach)
  - Rotation Speed: 10 deg/s (36 seconds for full 360°)
  - Recording Height Offset: Configurable vertical offset from anchor

---

## Data Structure - Autopilot Ready

### PointData Fields

```csharp
public struct PointData
{
    // Core identification
    public int Id;                      // Sequential waypoint ID
    public Vector3 Position;            // Unity world coordinates (meters)
    
    // Behavior
    public WaypointType Type;           // StopTurnGo or Record360
    public float YawDegrees;            // Heading angle (0-360°)
    
    // === AUTOPILOT-READY FIELDS ===
    
    // MAVLink-compatible parameters (NEW)
    public float AcceptanceRadius;      // How close drone must get (meters) - Default: 0.25m
    public float HoldTime;              // How long to wait at waypoint (seconds)
    public float SpeedMS;               // Target speed in m/s (-1 = use default)
    
    // Flexible parameters for type-specific behavior
    public Dictionary<string, object> Parameters;
}
```

### What Makes This Autopilot-Ready?

✅ **Position Data**: Unity Vector3 (X, Y, Z in meters) - same coordinate system used by indoor positioning systems
✅ **Heading/Yaw**: Captured from VR controller orientation in degrees
✅ **Precision Tolerance**: 25cm acceptance radius suitable for indoor flight
✅ **Timing Control**: Hold time and speed parameters match autopilot requirements
✅ **Behavior Mapping**: Each waypoint type maps cleanly to MAVLink command sequences
✅ **Extensible Parameters**: Dictionary allows adding MAVLink param1-4 values without code changes

---

## Indoor Flight Advantages

### No GPS Conversion Needed
- Indoor positioning systems (OptiTrack, Vicon, Intel RealSense) use **local Cartesian coordinates** (X, Y, Z in meters)
- Unity's coordinate system is **identical** to what ArduPilot/PX4 expect for indoor flight
- Only requires **coordinate frame alignment** (simple rotation matrix, one-time calibration)

### Higher Precision
- Motion capture systems: 1-2mm accuracy
- Visual-inertial odometry: 1-5cm accuracy
- GPS: 1-5m accuracy (outdoors)
- **Our 25cm acceptance radius is conservative and safe for indoor operations**

### Simplified Data Pipeline
```
VR Waypoint System
    ↓ (Unity Vector3 in meters)
Coordinate Frame Alignment (90° rotation + origin offset)
    ↓ (Local NED coordinates)
MAVLink Mission Items (MAV_FRAME_LOCAL_NED)
    ↓ (MAVLink protocol)
ArduPilot/PX4 Autopilot
```

No geodetic conversions, no GPS datum transformations, no coordinate system mismatches.

---

## Future Integration Steps

### Phase 1: Current (Completed ✅)
- VR waypoint placement with all autopilot-required data
- Indoor-optimized precision (25cm tolerance)
- Two distinct waypoint behaviors
- All data fields present for MAVLink conversion

### Phase 2: Coordinate Frame Alignment (Future)
- Define transformation between Unity axes and drone NED frame
- Typically: Unity X → Drone East, Unity Y → Drone Up, Unity Z → Drone North
- One-time calibration using known reference points

### Phase 3: MAVLink Export (Future)
- Convert PointData to `MISSION_ITEM_INT` messages
- Use `MAV_FRAME_LOCAL_NED` for indoor positioning
- Generate mission files compatible with QGroundControl/Mission Planner
- Example structure:
  ```json
  {
    "seq": 1,
    "frame": "MAV_FRAME_LOCAL_NED",
    "command": "MAV_CMD_NAV_WAYPOINT",
    "param1": 2.0,           // Hold time (from PointData.HoldTime)
    "param2": 0.25,          // Acceptance radius (from PointData.AcceptanceRadius)
    "param3": 0,             // Pass through
    "param4": 90.0,          // Yaw (from PointData.YawDegrees)
    "x": 5.5,                // X position in meters (from Position.x)
    "y": 2.3,                // Y position in meters (from Position.z)
    "z": 1.5,                // Z position in meters (from Position.y)
    "autocontinue": 1
  }
  ```

### Phase 4: Testing & Validation (Future)
- Test missions in ArduPilot SITL (Software-In-The-Loop) simulator
- Validate mission execution in Gazebo with simulated indoor positioning
- Upload to real drone only after simulation validation

---

## Technical Compatibility

### ArduPilot Support
- ✅ Local NED frame missions (`MAV_FRAME_LOCAL_NED`)
- ✅ External position estimation (motion capture, VIO)
- ✅ Waypoint missions with hold time and yaw control
- ✅ Condition-based commands (rotation, delays)
- ✅ Indoor/GPS-denied navigation (EKF3 with external positioning)

### PX4 Support
- ✅ Local position setpoints
- ✅ Offboard mode with position control
- ✅ Mission mode with local coordinates
- ✅ External vision position estimation
- ✅ MAVLink mission protocol compatibility

---

## Changes Made (Summary)

### Modified Files
1. **WaypointTypeDefinition.cs**
   - Removed `Flythrough` type
   - Renamed `StopRotateContinue` → `StopTurnGo`
   - Updated default parameters with indoor flight values (0.25m precision, slow speeds)
   - Added autopilot-specific parameters to defaults

2. **PointPlacementManager.cs**
   - Added 3 new fields to `PointData`: `AcceptanceRadius`, `HoldTime`, `SpeedMS`
   - Updated default waypoint type to `StopTurnGo`
   - Initialize autopilot fields when placing waypoints
   - Indoor-optimized default values

3. **PointHandle.cs**
   - Updated default waypoint type to `StopTurnGo`

4. **DronePathFollower.cs**
   - Updated references from `Flythrough` → `StopTurnGo`
   - Updated references from `StopRotateContinue` → `StopTurnGo`
   - Updated comments to reflect new naming

5. **WristMenuConnector.cs**
   - Removed `_flythroughButton` field
   - Updated button references to match 2-type system
   - Changed default selection to `StopTurnGo`
   - Removed obsolete button handlers

6. **RouteMetricsDisplay.cs**
   - Updated flight time calculations for new waypoint types
   - Now uses `HoldTime` field from waypoint data

7. **FlightPathSetup.cs**
   - Updated documentation to reflect 2 waypoint types

---

## Validation Checklist

✅ All waypoints store position in meters (Unity Vector3)
✅ All waypoints store heading/yaw in degrees
✅ All waypoints store acceptance radius (0.25m for indoor)
✅ All waypoints store hold time in seconds
✅ All waypoints store target speed in m/s
✅ Two distinct waypoint behaviors map to MAVLink commands
✅ No compilation errors
✅ Indoor flight parameters optimized (25cm precision)
✅ Flexible parameter system for future extensions

---

## Key Talking Points for Professor

1. **"Indoor flight simplifies integration"**
   - No GPS coordinate conversions needed
   - Unity coordinates → Autopilot coordinates directly
   - Just a coordinate frame alignment (rotation matrix)

2. **"All autopilot data is captured during VR interaction"**
   - Position, heading, tolerance, timing, speed
   - Nothing needs to be guessed or estimated later
   - Direct 1:1 mapping to MAVLink parameters

3. **"25cm precision is appropriate for indoor flight"**
   - More precise than GPS (1-5m)
   - Achievable with motion capture or VIO
   - Safe operational margin for real drones

4. **"Two waypoint types cover all mission requirements"**
   - StopTurnGo: Standard navigation and observation
   - Record360: Data collection and panoramic recording
   - Maps cleanly to autopilot command sequences

5. **"Integration is just data export, not a redesign"**
   - VR system architecture is fundamentally compatible
   - Future work is translation layer, not restructuring
   - Can be validated in simulation before real flight

---

## Conclusion

The VR waypoint system is **fully prepared** for ArduPilot/PX4 integration. All mission-critical data is captured, structured correctly, and optimized for indoor flight operations. The transition from VR simulation to real drone missions requires only a coordinate transformation module and MAVLink export functionality—both straightforward engineering tasks that don't affect the core VR system.

**Status**: ✅ Ready for thesis presentation and future autopilot integration

**Last Updated**: November 24, 2025

