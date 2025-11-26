using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Points
{
	/// <summary>
	/// Manages multiple flight paths/routes and integrates with the existing point placement system.
	/// </summary>
	public class FlightPathManager : MonoBehaviour
	{
		[SerializeField] private PointPlacementManager _pointManager;
		[SerializeField] private PathRenderer _pathRenderer;
		[SerializeField] private bool _pathModeEnabled = false;
		[SerializeField] private string _defaultRoutePrefix = "Route";

		public event Action<bool> OnPathModeChanged;
		public event Action<FlightPath> OnRouteStarted;
		public event Action<FlightPath> OnRouteFinished;
		public event Action<FlightPath> OnActiveRouteChanged;
		public event Action<FlightPath, int> OnPointAddedToRoute;
		public event Action<FlightPath> OnRouteCleared;
		public event Action<string> OnPathValidationError; // New: For displaying error messages

		// Thesis Feature: Simplified to single route management
		private FlightPath _currentRoute;
		private FlightPath _completedRoute; // Store last completed route for reference

		// Start/End point tracking
		private StartEndPoint _startPoint;
		private StartEndPoint _endPoint;
		private const int START_POINT_ID = -1;
		private const int END_POINT_ID = -2;

		/// <summary>
		/// Whether path building mode is currently active.
		/// </summary>
		public bool PathModeEnabled
		{
			get => _pathModeEnabled;
			private set
			{
				if (_pathModeEnabled != value)
				{
					_pathModeEnabled = value;
					OnPathModeChanged?.Invoke(value);
					
					// When exiting path mode, automatically finish current route if it has 2+ points
					if (!value && _currentRoute != null && _currentRoute.PointCount >= 2)
					{
						FinishCurrentRoute();
						
						// Ensure all completed routes are rendered
						if (_pathRenderer != null)
						{
							_pathRenderer.RenderAllCompletedRoutes();
						}
					}
				}
			}
		}

		/// <summary>
		/// The currently active route being built or edited.
		/// </summary>
		public FlightPath ActiveRoute => _currentRoute;

		/// <summary>
		/// The last completed route (read-only access for metrics).
		/// </summary>
		public FlightPath CompletedRoute => _completedRoute;

		private void Awake()
		{
			if (_pointManager == null)
			{
				_pointManager = UnityEngine.Object.FindFirstObjectByType<PointPlacementManager>();
			}

			if (_pathRenderer == null)
			{
				_pathRenderer = GetComponent<PathRenderer>();
				if (_pathRenderer == null)
				{
					_pathRenderer = gameObject.AddComponent<PathRenderer>();
				}
			}
		}

	private void Start()
	{
		if (_pointManager != null)
		{
			_pointManager.OnPointSelected += HandlePointSelected;
		}

		if (_pathRenderer != null)
		{
			_pathRenderer.Initialize(this);
		}

		// Find and register Start/End points in the scene
		RegisterStartEndPoints();
	}

		private void OnDestroy()
		{
			if (_pointManager != null)
			{
				_pointManager.OnPointSelected -= HandlePointSelected;
			}
		}

		/// <summary>
		/// Toggle path building mode on/off.
		/// </summary>
		public void TogglePathMode()
		{
			PathModeEnabled = !PathModeEnabled;
		}

		/// <summary>
		/// Start a new route. Thesis Feature: Single route mode - replaces any existing route.
		/// </summary>
		public void StartNewRoute(string routeName = null)
		{
			// Thesis Feature: Clear previous route completely for new planning session
			_currentRoute = null;
			_completedRoute = null;

			string name = routeName ?? $"{_defaultRoutePrefix}";
			_currentRoute = new FlightPath(name);

			OnRouteStarted?.Invoke(_currentRoute);
			OnActiveRouteChanged?.Invoke(_currentRoute);

			Debug.Log($"Started new route: {name}");
		}

		/// <summary>
		/// Thesis Feature: Continue building the current route (simpler than reopening).
		/// </summary>
		public void ContinueCurrentRoute()
		{
			if (_currentRoute == null && _completedRoute != null)
			{
				// Reactivate the completed route
				_currentRoute = _completedRoute;
				_completedRoute = null;
				
				OnActiveRouteChanged?.Invoke(_currentRoute);
				Debug.Log($"Continuing route '{_currentRoute.RouteName}'. Current points: {_currentRoute.PointCount}");
			}
		}

		/// <summary>
		/// Finish the current route and optionally close the loop. Thesis Feature: Single route mode.
		/// </summary>
		public void FinishCurrentRoute(bool closeLoop = false)
		{
			if (_currentRoute == null || _currentRoute.IsEmpty)
			{
				return;
			}

			_currentRoute.IsClosed = closeLoop;
			
			// Store as completed route
			_completedRoute = _currentRoute;

			// Reset point colors to original when route is finished
			ResetPointColorsToOriginal(_currentRoute);

			OnRouteFinished?.Invoke(_currentRoute);

			Debug.Log($"Finished route: {_currentRoute.RouteName} with {_currentRoute.PointCount} points" +
					  (closeLoop ? " (closed)" : " (open)"));

			_currentRoute = null;
			OnActiveRouteChanged?.Invoke(null);
		}

		/// <summary>
		/// Reset all points in a route back to their original colors and update badges.
		/// </summary>
		private void ResetPointColorsToOriginal(FlightPath route)
		{
			if (route == null || _pointManager == null) return;

			foreach (int pointId in route.PointIds)
			{
				var pointHandle = _pointManager.GetPoint(pointId);
				if (pointHandle != null)
				{
					// DON'T reset to yellow - keep points blue when they're in completed routes
					// Just update visual state to ensure badges are correct
					pointHandle.UpdateVisualState();
				}
			}
		}

		/// <summary>
		/// Undo the last point added to the current route.
		/// </summary>
		public void UndoLastPoint()
		{
			if (_currentRoute == null || _currentRoute.IsEmpty)
			{
				return;
			}

			bool removed = _currentRoute.RemoveLastPoint();
			if (removed)
			{
				OnPointAddedToRoute?.Invoke(_currentRoute, _currentRoute.PointCount - 1);
				Debug.Log($"Undid last point. Route now has {_currentRoute.PointCount} points.");
			}
		}

		/// <summary>
		/// Get the currently active or completed route. Thesis Feature: Single route simplified.
		/// </summary>
		public FlightPath GetActiveRoute()
		{
			return _currentRoute ?? _completedRoute;
		}

		/// <summary>
		/// Get world positions for all points in the active route.
		/// </summary>
		public List<Vector3> GetActiveRouteWorldPositions()
		{
			var activeRoute = GetActiveRoute();
			if (activeRoute == null || _pointManager == null)
			{
				return new List<Vector3>();
			}

			return activeRoute.GetWorldPositions(_pointManager);
		}

		/// <summary>
		/// Clear the current route completely. Thesis Feature: Single route mode.
		/// </summary>
		public void ClearCurrentRoute()
		{
			if (_currentRoute != null)
			{
				OnRouteCleared?.Invoke(_currentRoute);
			}
			if (_completedRoute != null)
			{
				OnRouteCleared?.Invoke(_completedRoute);
			}
			
			_currentRoute = null;
			_completedRoute = null;
			OnActiveRouteChanged?.Invoke(null);
			Debug.Log("Cleared current route.");
		}

		/// <summary>
		/// Thesis Feature: Remove a specific waypoint from the route without clearing entire route.
		/// </summary>
		public void RemoveWaypointFromRoute(int pointId)
		{
			bool removed = false;
			
			// Remove from current route if present
			if (_currentRoute != null && _currentRoute.ContainsPoint(pointId))
			{
				removed = _currentRoute.RemovePoint(pointId);
				if (removed)
				{
					Debug.Log($"Removed waypoint {pointId} from current route. Remaining points: {_currentRoute.PointCount}");
					OnActiveRouteChanged?.Invoke(_currentRoute);
				}
			}
			
			// Remove from completed route if present
			if (_completedRoute != null && _completedRoute.ContainsPoint(pointId))
			{
				removed = _completedRoute.RemovePoint(pointId);
				if (removed)
				{
					Debug.Log($"Removed waypoint {pointId} from completed route. Remaining points: {_completedRoute.PointCount}");
					OnActiveRouteChanged?.Invoke(_completedRoute);
				}
			}
		}

		/// <summary>
		/// Merge segments by removing a break marker between two existing points in the route.
		/// </summary>
		public bool MergeSegmentsBetween(int fromPointId, int toPointId)
		{
			var route = GetActiveRoute();
			if (route == null) return false;
			if (!route.ContainsPoint(fromPointId) || !route.ContainsPoint(toPointId)) return false;

			bool removed = route.RemoveFirstBreakBetween(fromPointId, toPointId);
			if (removed)
			{
				OnActiveRouteChanged?.Invoke(route);
				if (_pathRenderer != null)
				{
					_pathRenderer.UpdateActiveRoute();
				}
			}
			return removed;
		}

		/// <summary>
		/// Check if a point is part of the current/completed route. Thesis Feature: Single route.
		/// </summary>
		public bool IsPointInRoute(int pointId)
		{
			if (_currentRoute != null && _currentRoute.ContainsPoint(pointId))
			{
				return true;
			}
			if (_completedRoute != null && _completedRoute.ContainsPoint(pointId))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get the route index of a point (1-based for display).
		/// </summary>
		public int GetPointRouteIndex(int pointId, FlightPath route = null)
		{
			var targetRoute = route ?? GetActiveRoute();
			if (targetRoute == null)
			{
				return -1;
			}

			int index = targetRoute.GetPointIndex(pointId);
			return index >= 0 ? index + 1 : -1; // Convert to 1-based for display
		}

		private void HandlePointSelected(PointHandle pointHandle)
		{
			if (!PathModeEnabled || pointHandle == null)
			{
				return;
			}

			// Start a new route if none exists
			if (_currentRoute == null)
			{
				StartNewRoute();
			}

			// Add the point to the current route
			_currentRoute.AddPoint(pointHandle.Id);
			OnPointAddedToRoute?.Invoke(_currentRoute, _currentRoute.PointCount);

			Debug.Log($"Added point {pointHandle.Id} to route {_currentRoute.RouteName}. " +
					 $"Route now has {_currentRoute.PointCount} points.");

			// Update path rendering immediately
			if (_pathRenderer != null)
			{
				_pathRenderer.UpdateActiveRoute();
			}

			// Provide haptic feedback
			if (_pointManager != null)
			{
				_pointManager.TickHaptics(0.3f, 0.05f);
			}
		}

		/// <summary>
		/// Export current route data. Thesis Feature: Simplified single route export.
		/// </summary>
		public RouteExportData ExportRouteData()
		{
			var route = GetActiveRoute();
			if (route == null || _pointManager == null)
			{
				return new RouteExportData();
			}

			var worldPositions = route.GetWorldPositions(_pointManager);
			var resampledPositions = route.GetResampledPath(_pointManager, 0.5f);

			return new RouteExportData
			{
				RouteName = route.RouteName,
				PointIds = new List<int>(route.PointIds),
				WorldPositions = worldPositions,
				ResampledPositions = resampledPositions,
				IsClosed = route.IsClosed,
				CreatedAt = route.CreatedAt,
				PointCount = route.PointCount
			};
		}

		#region Start/End Point Management

		/// <summary>
		/// Find and register Start/End points in the scene.
		/// </summary>
		private void RegisterStartEndPoints()
		{
			StartEndPoint[] points = UnityEngine.Object.FindObjectsByType<StartEndPoint>(FindObjectsSortMode.None);
			Debug.Log($"[FlightPathManager] Found {points.Length} Start/End point(s) in scene");

			foreach (var point in points)
			{
				if (point.Type == StartEndPoint.PointType.Start)
				{
					if (_startPoint != null)
					{
						Debug.LogWarning($"Multiple Start points found in scene. Already have one at {_startPoint.Position}, ignoring one at {point.Position}");
						continue;
					}
					_startPoint = point;
					_startPoint.Register(START_POINT_ID);
					Debug.Log($"[FlightPathManager] Registered Start point at position: {_startPoint.Position}");
				}
				else if (point.Type == StartEndPoint.PointType.End)
				{
					if (_endPoint != null)
					{
						Debug.LogWarning($"Multiple End points found in scene. Already have one at {_endPoint.Position}, ignoring one at {point.Position}");
						continue;
					}
					_endPoint = point;
					_endPoint.Register(END_POINT_ID);
					Debug.Log($"[FlightPathManager] Registered End point at position: {_endPoint.Position}");
				}
			}

			if (_startPoint == null)
			{
				Debug.LogWarning("No Start point found in scene. Path validation will be disabled.");
			}
			if (_endPoint == null)
			{
				Debug.LogWarning("No End point found in scene. Path validation will be disabled.");
			}
		}

		/// <summary>
		/// Check if a segment can be created between two points based on Start/End validation rules.
		/// </summary>
		/// <returns>True if segment is valid, false otherwise (with error message via event)</returns>
		public bool CanCreateSegment(int fromPointId, int toPointId)
		{
			// If no Start/End points are configured, allow all segments
			if (_startPoint == null || _endPoint == null)
			{
				return true;
			}

			var route = GetActiveRoute();

		// Rule 1: First point MUST be Start point
		if (route == null || route.IsEmpty)
		{
			if (fromPointId != START_POINT_ID)
			{
				OnPathValidationError?.Invoke("Path must start at Start point");
				return false;
			}
		}

		// Rule 2: Cannot connect TO Start point (it's only a starting point)
		if (toPointId == START_POINT_ID)
		{
			OnPathValidationError?.Invoke("Cannot connect to Start point");
			return false;
		}

		// Rule 3: Cannot connect FROM End point (it's only an ending point)
		if (fromPointId == END_POINT_ID)
		{
			OnPathValidationError?.Invoke("Cannot connect from End point");
			return false;
		}

		// Rule 4: End point can only appear once in the route
		if (toPointId == END_POINT_ID && route != null && route.ContainsPoint(END_POINT_ID))
		{
			OnPathValidationError?.Invoke("End point already in path");
			return false;
		}
		
		// Rule 5: Start point can only appear once in the route (at the beginning)
		if (toPointId == START_POINT_ID && route != null && route.ContainsPoint(START_POINT_ID))
		{
			OnPathValidationError?.Invoke("Start point already in path");
			return false;
		}

			return true;
		}

		/// <summary>
		/// Validate that the current path is complete (Start → waypoints → End).
		/// </summary>
		public bool IsPathComplete()
		{
			if (_startPoint == null || _endPoint == null)
			{
				return true; // No validation if Start/End not configured
			}

			var route = GetActiveRoute();
			if (route == null || route.IsEmpty)
			{
				return false;
			}

			// Check if path starts with Start and ends with End
			bool startsWithStart = route.PointIds.Count > 0 && route.PointIds[0] == START_POINT_ID;
			bool endsWithEnd = route.PointIds.Count > 0 && route.PointIds[route.PointIds.Count - 1] == END_POINT_ID;

			return startsWithStart && endsWithEnd;
		}

		/// <summary>
		/// Get the Start point (if registered).
		/// </summary>
		public StartEndPoint GetStartPoint() => _startPoint;

		/// <summary>
		/// Get the End point (if registered).
		/// </summary>
		public StartEndPoint GetEndPoint() => _endPoint;

		#endregion
	}

	/// <summary>
	/// Data structure for exporting route information to external systems.
	/// </summary>
	[System.Serializable]
	public class RouteExportData
	{
		public string RouteName;
		public List<int> PointIds;
		public List<Vector3> WorldPositions;
		public List<Vector3> ResampledPositions;
		public bool IsClosed;
		public DateTime CreatedAt;
		public int PointCount;

		public RouteExportData()
		{
			RouteName = "";
			PointIds = new List<int>();
			WorldPositions = new List<Vector3>();
			ResampledPositions = new List<Vector3>();
			IsClosed = false;
			CreatedAt = DateTime.UtcNow;
			PointCount = 0;
		}
	}
}
