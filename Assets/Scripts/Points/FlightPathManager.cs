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

		private readonly List<FlightPath> _routes = new List<FlightPath>();
		private FlightPath _currentRoute;
		private string _activeRouteName;
		private int _nextRouteNumber = 1;

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
		/// All available routes in the system.
		/// </summary>
		public IReadOnlyList<FlightPath> AllRoutes => _routes;

		/// <summary>
		/// Number of routes currently created.
		/// </summary>
		public int RouteCount => _routes.Count;

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
		/// Start a new route with an optional name.
		/// </summary>
		public void StartNewRoute(string routeName = null)
		{
			// Finish current route if one exists
			if (_currentRoute != null)
			{
				FinishCurrentRoute();
			}

			string name = routeName ?? $"{_defaultRoutePrefix} {_nextRouteNumber++}";
			_currentRoute = new FlightPath(name);
			_activeRouteName = name;

			OnRouteStarted?.Invoke(_currentRoute);
			OnActiveRouteChanged?.Invoke(_currentRoute);

			Debug.Log($"Started new route: {name}");
		}

		/// <summary>
		/// Reopen a completed route for editing/extending.
		/// </summary>
		public void ReopenRouteForEditing(FlightPath route)
		{
			if (route == null) return;

			// Finish current route if one exists
			if (_currentRoute != null)
			{
				FinishCurrentRoute();
			}

			// Remove the route from completed routes list
			_routes.Remove(route);

			// Set it as the active route for editing
			_currentRoute = route;
			_activeRouteName = route.RouteName;

			OnActiveRouteChanged?.Invoke(_currentRoute);

			Debug.Log($"Reopened route '{route.RouteName}' for editing. Current points: {route.PointCount}");
		}

		/// <summary>
		/// Finish the current route and optionally close the loop.
		/// </summary>
		public void FinishCurrentRoute(bool closeLoop = false)
		{
			if (_currentRoute == null || _currentRoute.IsEmpty)
			{
				return;
			}

			_currentRoute.IsClosed = closeLoop;
			_routes.Add(_currentRoute);

			// Reset point colors to original when route is finished
			ResetPointColorsToOriginal(_currentRoute);

			OnRouteFinished?.Invoke(_currentRoute);

			Debug.Log($"Finished route: {_currentRoute.RouteName} with {_currentRoute.PointCount} points" +
					  (closeLoop ? " (closed)" : " (open)"));

			_currentRoute = null;
			_activeRouteName = null;
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
		/// Set the active route for viewing/editing by name.
		/// </summary>
		public void SetActiveRoute(string routeName)
		{
			var route = _routes.FirstOrDefault(r => r.RouteName == routeName);
			if (route != null)
			{
				_activeRouteName = routeName;
				OnActiveRouteChanged?.Invoke(route);
				Debug.Log($"Switched to route: {routeName}");
			}
		}

		/// <summary>
		/// Get the currently active route for viewing/editing.
		/// </summary>
		public FlightPath GetActiveRoute()
		{
			if (_currentRoute != null)
			{
				return _currentRoute;
			}

			if (!string.IsNullOrEmpty(_activeRouteName))
			{
				return _routes.FirstOrDefault(r => r.RouteName == _activeRouteName);
			}

			return null;
		}

		/// <summary>
		/// Get all routes in the system.
		/// </summary>
		public IReadOnlyList<FlightPath> GetAllRoutes()
		{
			return _routes;
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
		/// Clear all routes from the system.
		/// </summary>
		public void ClearAllRoutes()
		{
			_routes.Clear();
			_currentRoute = null;
			_activeRouteName = null;
			OnActiveRouteChanged?.Invoke(null);
			Debug.Log("Cleared all routes.");
		}

		/// <summary>
		/// Remove a specific route by name.
		/// </summary>
		public bool RemoveRoute(string routeName)
		{
			var route = _routes.FirstOrDefault(r => r.RouteName == routeName);
			if (route != null)
			{
				_routes.Remove(route);
				if (_activeRouteName == routeName)
				{
					_activeRouteName = null;
					OnActiveRouteChanged?.Invoke(null);
				}
				OnRouteCleared?.Invoke(route);
				Debug.Log($"Removed route: {routeName}");
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get a route by name.
		/// </summary>
		public FlightPath GetRoute(string routeName)
		{
			return _routes.FirstOrDefault(r => r.RouteName == routeName);
		}

		/// <summary>
		/// Check if a point is part of any route.
		/// </summary>
		public bool IsPointInAnyRoute(int pointId)
		{
			return _routes.Any(route => route.ContainsPoint(pointId)) ||
				   (_currentRoute != null && _currentRoute.ContainsPoint(pointId));
		}

		/// <summary>
		/// Get all routes that contain a specific point.
		/// </summary>
		public List<FlightPath> GetRoutesContainingPoint(int pointId)
		{
			var containingRoutes = new List<FlightPath>();

			foreach (var route in _routes)
			{
				if (route.ContainsPoint(pointId))
				{
					containingRoutes.Add(route);
				}
			}

			if (_currentRoute != null && _currentRoute.ContainsPoint(pointId))
			{
				containingRoutes.Add(_currentRoute);
			}

			return containingRoutes;
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
		/// Export route data for external systems.
		/// </summary>
		public RouteExportData ExportRouteData(string routeName = null)
		{
			var route = !string.IsNullOrEmpty(routeName) ? GetRoute(routeName) : GetActiveRoute();
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
