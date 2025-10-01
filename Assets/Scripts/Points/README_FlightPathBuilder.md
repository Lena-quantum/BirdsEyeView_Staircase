# VR Flight Path Builder

A comprehensive flight path building system for Unity VR projects that extends the existing point placement functionality.

## Overview

The Flight Path Builder allows users to create ordered flight paths by connecting existing placed points. It provides visual feedback with lines, arrows, and numbered badges while maintaining compatibility with the existing point placement system.

## Features

- **Path Building Mode**: Toggle between normal point placement and path building
- **Visual Path Rendering**: Lines between points with directional arrows
- **Numbered Point Badges**: Shows point order in routes (1, 2, 3...)
- **Multiple Routes**: Create and manage multiple flight paths
- **Route Management**: Start, finish, undo, and clear routes
- **Export Ready**: Get world positions and resampled paths for external systems
- **VR Optimized**: Smooth performance with haptic feedback

## Components

### Core Components

1. **FlightPath.cs** - Data structure for storing route information
2. **FlightPathManager.cs** - Manages multiple routes and integrates with PointPlacementManager
3. **PathRenderer.cs** - Renders visual lines, arrows, and badges
4. **PathModeController.cs** - Handles input when in path building mode
5. **RouteUI.cs** - Simple UI for route management (optional)

### Extended Components

6. **PointHandle.cs** - Extended to show route membership and numbered badges
7. **RayDepthController.cs** - Extended to handle path mode input
8. **FlightPathSetup.cs** - Helper script for easy system setup

## Setup Instructions

### Quick Setup

1. **Attach FlightPathSetup** to a GameObject in your scene
2. **Configure** the setup options in the inspector
3. **Run Setup** using the "Setup Flight Path System" context menu item
4. **Validate** using "Validate Setup" to ensure everything is connected

### Manual Setup

1. **Ensure PointPlacementManager** exists in your scene
2. **Add FlightPathManager** to a GameObject
3. **Add PathRenderer** to the same GameObject
4. **Add PathModeController** to the same GameObject
5. **Create RouteUI** (optional) for UI management
6. **Connect references** between components

## Controls

### Path Mode Controls
- **Menu Button**: Toggle Path Mode on/off
- **Trigger** (Path Mode): Add hovered point to current route
- **B Button**: Undo last point in current route
- **Grip**: Finish current route

### Normal Mode Controls
- **Trigger**: Place new point (unchanged from existing system)
- **A/B Buttons**: Adjust depth (unchanged from existing system)
- **Stick**: Continuous depth adjustment (unchanged from existing system)

## Usage Workflow

### Basic Path Building

1. **Place Points**: Use existing point placement system to place waypoints
2. **Enter Path Mode**: Press Menu button to toggle Path Mode
3. **Start Route**: Click a point to start a new route
4. **Build Path**: Click additional points in sequence
5. **Finish Route**: Press Grip or click first point again to close loop
6. **Exit Path Mode**: Press Menu button to return to placement mode

### Multiple Routes

1. **Create First Route**: Follow basic workflow above
2. **Start New Route**: Use UI "New Route" button or click another point
3. **Switch Routes**: Use UI dropdown to switch between routes
4. **Manage Routes**: Use UI buttons to clear, finish, or undo

## API Reference

### FlightPathManager

```csharp
// Core functionality
void TogglePathMode()
void StartNewRoute(string routeName = null)
void FinishCurrentRoute(bool closeLoop = false)
void UndoLastPoint()

// Route management
FlightPath GetActiveRoute()
IReadOnlyList<FlightPath> GetAllRoutes()
List<Vector3> GetActiveRouteWorldPositions()

// Data export
RouteExportData ExportRouteData(string routeName = null)
```

### FlightPath

```csharp
// Route data
string RouteName { get; set; }
List<int> PointIds { get; }
bool IsClosed { get; set; }
Color PathColor { get; set; }

// Operations
void AddPoint(int pointId)
bool RemoveLastPoint()
List<Vector3> GetWorldPositions(PointPlacementManager manager)
List<Vector3> GetResampledPath(PointPlacementManager manager, float spacing = 0.5f)
```

### PathRenderer

```csharp
// Visual settings
float LineWidth { get; set; }
bool ShowArrows { get; set; }
bool SmoothPath { get; set; }

// Rendering
void RenderPath(FlightPath path, PointPlacementManager pointManager)
void UpdateActiveRoute()
void ClearAllPaths()
```

## Events

The system provides events for integration:

```csharp
// FlightPathManager events
OnPathModeChanged(bool pathModeEnabled)
OnRouteStarted(FlightPath route)
OnRouteFinished(FlightPath route)
OnActiveRouteChanged(FlightPath route)
OnPointAddedToRoute(FlightPath route, int pointCount)
```

## Configuration

### PathRenderer Settings

- **Line Width**: Thickness of path lines (default: 0.01m)
- **Show Arrows**: Display directional arrows along paths
- **Arrow Spacing**: Distance between arrows (default: 0.5m)
- **Smooth Path**: Use Catmull-Rom splines instead of straight lines
- **Smooth Segments**: Number of segments for smooth curves (default: 10)

### RouteUI Settings

- **Show On Wrist**: Position UI on right controller wrist
- **UI Scale**: Scale factor for world space UI (default: 0.002)
- **Wrist Offset**: Offset from controller for UI positioning

## Performance Considerations

- **Object Pooling**: Line renderers and badges are pooled for performance
- **Max Line Segments**: Configurable limit for complex paths (default: 1000)
- **VR Optimization**: Designed for 90fps VR performance
- **Memory Management**: Automatic cleanup of unused visual elements

## Integration with Existing System

The Flight Path Builder is designed to extend your existing point placement system without breaking functionality:

- **PointPlacementManager**: Events are extended, not replaced
- **PointHandle**: New features added, existing functionality preserved
- **RayDepthController**: Path mode input added alongside existing controls
- **Input System**: Uses existing XR InputDevices patterns

## Troubleshooting

### Common Issues

1. **No PointPlacementManager**: Ensure you have a PointPlacementManager in your scene
2. **Missing Components**: Use FlightPathSetup to automatically create required components
3. **Input Not Working**: Check that XR InputDevices are properly configured
4. **UI Not Showing**: Ensure EventSystem exists for UI components

### Debug Information

- Use `FlightPathSetup.ValidateSetup()` to check component connections
- Check console for setup and validation messages
- Enable debug logging in FlightPathManager for detailed event tracking

## Export and Integration

### Getting Route Data

```csharp
// Get world positions for active route
var positions = pathManager.GetActiveRouteWorldPositions();

// Get resampled path with even spacing
var route = pathManager.GetActiveRoute();
var resampled = route.GetResampledPath(pointManager, 0.5f);

// Export complete route data
var exportData = pathManager.ExportRouteData("Route A");
```

### External System Integration

The system provides clean data structures for integration with:
- Drone flight controllers
- Path planning algorithms
- Data export systems
- Simulation environments

## Future Enhancements

Potential extensions to the system:
- Path validation and collision detection
- Real-time path editing
- Advanced spline types (Bezier, B-spline)
- Path optimization algorithms
- Multi-user collaborative path building
- Path recording and playback

## License

This Flight Path Builder system extends the existing point placement functionality and follows the same project conventions and coding standards.
