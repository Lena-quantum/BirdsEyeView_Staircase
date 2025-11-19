using System;
using System.Collections.Generic;
using UnityEngine;

namespace Points
{
	/// <summary>
	/// Manages point placement state, data persistence, and events for point handles.
	/// </summary>
	public class PointPlacementManager : MonoBehaviour
	{
		[Serializable]
		public struct PointData
		{
			public int Id;
			public Vector3 Position;
			public Color Color;
			public float Radius;
			public DateTime CreatedAt;
			
			// Thesis Feature: Waypoint type system
			public WaypointType Type;
			public float YawDegrees;
			public Dictionary<string, object> Parameters;
		}

		public event Action<PointData> OnPointPlaced;
		public event Action<PointHandle> OnPointSelected;
		public event Action<PointHandle, bool> OnPointHovered;

		[SerializeField] private float _minDepth = 0.2f;
		[SerializeField] private float _maxDepth = 10f;
		[SerializeField] private float _depthSpeed = 1.0f; // meters per second per stick unit
		[SerializeField] private float _depthStep = 0.1f;
		[SerializeField] private float _precisionMultiplier = 0.1f;
		[SerializeField] private bool _surfaceSnappingEnabled = true;
		[SerializeField] private float _placedPointRadius = 0.05f;
		[SerializeField] private Color _placedPointColor = Color.yellow;
		[SerializeField] private Color _ghostValidColor = Color.green;
		[SerializeField] private Color _ghostInvalidColor = Color.red;

	[SerializeField] private Transform _rightHandRayOrigin;
	[SerializeField] private Transform _ghostTransform;
	[SerializeField] private Renderer _ghostRenderer;
	[SerializeField] private PointLabelBillboard _depthReadout;
	[SerializeField] private HapticsHelper _hapticsHelper;
	[SerializeField] private RayDepthController _rayDepthController;
	[SerializeField] private PointHandle _pointHandlePrefab;
	[SerializeField] private Transform _pointsParent;
	
	// Thesis Feature: Collision avoidance parameters
	[SerializeField] private float _droneRadius = 0.5f; // 50 cm safety buffer
	[SerializeField] private LayerMask _environmentLayer = 1 << 0; // Default layer initially
	[SerializeField] private Color _collisionGhostColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grey semi-transparent

	private readonly List<PointData> _points = new List<PointData>();
	private readonly Dictionary<int, PointHandle> _idToHandle = new Dictionary<int, PointHandle>();
	private int _nextId = 1;
	
	// Thesis Feature: Current waypoint type selection
	private WaypointType _currentTypeSelection = WaypointType.Flythrough;
	
	// Thesis Feature: Track highlighted obstacles for collision feedback
	private readonly HashSet<ObstacleHighlighter> _currentlyHighlightedObstacles = new HashSet<ObstacleHighlighter>();

		/// <summary>
		/// Minimum allowed placement depth in meters.
		/// </summary>
		public float MinDepth => _minDepth;

		/// <summary>
		/// Maximum allowed placement depth in meters.
		/// </summary>
		public float MaxDepth => _maxDepth;

		/// <summary>
		/// Continuous depth adjustment speed (m/s per stick unit).
		/// </summary>
		public float DepthSpeed => _depthSpeed;

		/// <summary>
		/// Step amount in meters for A/B adjustments.
		/// </summary>
		public float DepthStep => _depthStep;

		/// <summary>
		/// Multiplier applied while precision is held.
		/// </summary>
		public float PrecisionMultiplier => _precisionMultiplier;

		/// <summary>
		/// Whether surface snapping is enabled.
		/// </summary>
		public bool SurfaceSnappingEnabled => _surfaceSnappingEnabled;

		/// <summary>
		/// Color used for valid ghost preview.
		/// </summary>
		public Color GhostValidColor => _ghostValidColor;

		/// <summary>
		/// Color used for invalid/clamped ghost preview.
		/// </summary>
		public Color GhostInvalidColor => _ghostInvalidColor;

		/// <summary>
		/// Default radius used for spawned point handles.
		/// </summary>
		public float PlacedPointRadius => _placedPointRadius;

		/// <summary>
		/// Default color used for spawned point handles.
		/// </summary>
		public Color PlacedPointColor => _placedPointColor;

		/// <summary>
		/// Prefab used for creating placed points.
		/// </summary>
		public PointHandle PointHandlePrefab => _pointHandlePrefab;

		/// <summary>
		/// Parent transform for spawned points.
		/// </summary>
		public Transform PointsParent => _pointsParent;

		/// <summary>
		/// Safety radius used for collision checks (meters).
		/// </summary>
		public float DroneRadius => _droneRadius;

		/// <summary>
		/// Layer mask that defines environment obstacles for collision checks.
		/// </summary>
		public LayerMask EnvironmentLayerMask => _environmentLayer;

		/// <summary>
		/// Currently selected waypoint type for new placements.
		/// </summary>
		public WaypointType CurrentTypeSelection
		{
			get => _currentTypeSelection;
			set
			{
				_currentTypeSelection = value;
				UpdateGhostColorForType();
			}
		}

		private void Awake()
		{
			if (_ghostRenderer == null)
			{
				var tr = _ghostTransform != null ? _ghostTransform.GetComponentInChildren<Renderer>() : null;
				if (tr != null) _ghostRenderer = tr;
			}
			
			// Thesis Feature: Initialize ghost with default type color
			UpdateGhostColorForType();
		}

	/// <summary>
	/// Place a new point at the current ghost position and register it.
	/// </summary>
	public void PlaceAtCurrentGhost()
	{
		if (_ghostTransform == null || _pointHandlePrefab == null)
		{
			Debug.LogWarning("PointPlacementManager: Missing ghost transform or point handle prefab.");
			return;
		}
		
		Vector3 position = _ghostTransform.position;
		var experimentManager = Experiment.ExperimentDataManager.Instance;
		
		// Thesis Feature: Prevent placement in collision zones
		if (CheckGhostCollisionWithObstacles())
		{
			Debug.LogWarning("PointPlacementManager: Cannot place waypoint in collision zone (too close to obstacle).");
			
			// Thesis Feature: Notify experiment tracker
			if (experimentManager != null)
			{
				experimentManager.OnPlacementBlocked(position, "NoFlyZone");
			}
			
			return;
		}

		int id = _nextId++;
			
			// Thesis Feature: Use type-specific color
			Color color = WaypointTypeDefinition.GetTypeColor(_currentTypeSelection);
			float radius = _placedPointRadius;

			PointHandle handle = Instantiate(_pointHandlePrefab, position, Quaternion.identity, _pointsParent != null ? _pointsParent : transform);
			handle.Initialize(id, color, radius, this, _currentTypeSelection); // Pass type directly

			// Thesis Feature: Calculate yaw from right controller forward direction
			float yaw = 0f;
			if (_rightHandRayOrigin != null)
			{
				yaw = _rightHandRayOrigin.eulerAngles.y;
			}

			var data = new PointData
			{
				Id = id,
				Position = position,
				Color = color,
				Radius = radius,
				CreatedAt = DateTime.UtcNow,
				Type = _currentTypeSelection,
				YawDegrees = yaw,
				Parameters = WaypointTypeDefinition.GetDefaultParameters(_currentTypeSelection)
			};

			_points.Add(data);
			_idToHandle[id] = handle;

			OnPointPlaced?.Invoke(data);
			
			// Thesis Feature: Notify experiment tracker
			if (experimentManager != null)
			{
				experimentManager.OnWaypointPlaced(position, ConvertToPointType(_currentTypeSelection), yaw);
			}
		}

		/// <summary>
		/// Undo (remove) the most recently placed point, if any.
		/// </summary>
		public void UndoLast()
		{
			if (_points.Count == 0) return;
			PointData last = _points[_points.Count - 1];
			_points.RemoveAt(_points.Count - 1);
			if (_idToHandle.TryGetValue(last.Id, out PointHandle handle))
			{
				_idToHandle.Remove(last.Id);
				if (handle != null)
				{
					Destroy(handle.gameObject);
				}
			}
		}

		/// <summary>
		/// Remove a specific point by ID.
		/// </summary>
		public bool RemovePoint(int pointId)
		{
			// Find and remove from data list
			for (int i = _points.Count - 1; i >= 0; i--)
			{
				if (_points[i].Id == pointId)
				{
					_points.RemoveAt(i);
					break;
				}
			}

			// Remove from handle dictionary and destroy GameObject
			if (_idToHandle.TryGetValue(pointId, out PointHandle handle))
			{
				_idToHandle.Remove(pointId);
				if (handle != null)
				{
					// Thesis Feature: Remove waypoint from route (keep remaining segments)
					var pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
					if (pathManager != null && pathManager.IsPointInRoute(pointId))
					{
						pathManager.RemoveWaypointFromRoute(pointId);
					}
					
					// Thesis Feature: Notify experiment tracker
					var experimentManager = Experiment.ExperimentDataManager.Instance;
					if (experimentManager != null)
					{
						experimentManager.OnWaypointDeleted(pointId);
					}

					Destroy(handle.gameObject);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a read-only list of all placed points.
		/// </summary>
		public IReadOnlyList<PointData> GetPoints()
		{
			return _points;
		}

		/// <summary>
		/// Get the current handle instance for a point ID, or null if not found.
		/// </summary>
		public PointHandle GetPoint(int id)
		{
			_idToHandle.TryGetValue(id, out PointHandle handle);
			return handle;
		}

		internal void NotifyHovered(PointHandle handle, bool isHovered)
		{
			// Clear hover state from all points first
			foreach (var kvp in _idToHandle)
			{
				if (kvp.Value != null && kvp.Value != handle)
				{
					kvp.Value.SetHoveredState(false);
				}
			}

			// Set hover state on the target handle
			if (handle != null && isHovered)
			{
				handle.SetHoveredState(true);
			}

			OnPointHovered?.Invoke(handle, isHovered);
		}

		internal void NotifySelected(PointHandle handle)
		{
			OnPointSelected?.Invoke(handle);
		}

	internal void UpdateGhostVisualValidity(bool isValid)
	{
		if (_ghostRenderer == null || _ghostTransform == null) return;
		
		// Thesis Feature: Collision avoidance check
		bool hasCollision = CheckGhostCollisionWithObstacles();
		
		// Override validity if collision detected
		bool finalValidity = isValid && !hasCollision;
		
		// Thesis Feature: Use type-specific color, or grey if collision detected
		Color typeColor = WaypointTypeDefinition.GetTypeColor(_currentTypeSelection);
		Color target;
		
		if (hasCollision)
		{
			// Grey semi-transparent when in collision zone
			target = _collisionGhostColor;
		}
		else if (finalValidity)
		{
			// Full brightness when valid
			target = typeColor;
		}
		else
		{
			// Dim if invalid (other reasons like depth clamping)
			target = typeColor * 0.5f;
		}
		
		foreach (var mat in _ghostRenderer.sharedMaterials)
		{
			if (mat != null && mat.HasProperty("_Color"))
			{
				mat.color = target;
			}
		}
	}
	
	/// <summary>
	/// Check if the ghost sphere is within the drone radius of any obstacle.
	/// Highlights obstacles that are too close and returns true if collision detected.
	/// </summary>
	private bool CheckGhostCollisionWithObstacles()
	{
		if (_ghostTransform == null) return false;
		
		Vector3 ghostPos = _ghostTransform.position;
		bool hasCollision = false;
		
		// Find all colliders within drone radius
		Collider[] nearbyColliders = Physics.OverlapSphere(ghostPos, _droneRadius, _environmentLayer);
		
		// Track which obstacles should be highlighted this frame
		HashSet<ObstacleHighlighter> shouldBeHighlighted = new HashSet<ObstacleHighlighter>();
		
		foreach (Collider col in nearbyColliders)
		{
			// Calculate distance to closest point on collider surface
			Vector3 closestPoint = col.ClosestPoint(ghostPos);
			float distance = Vector3.Distance(ghostPos, closestPoint);
			
			// If within drone radius, this is a collision
			if (distance < _droneRadius)
			{
				hasCollision = true;
				
				// Get or add ObstacleHighlighter component
				ObstacleHighlighter highlighter = col.GetComponent<ObstacleHighlighter>();
				if (highlighter == null)
				{
					highlighter = col.gameObject.AddComponent<ObstacleHighlighter>();
				}
				
				// Mark for highlighting with drone radius
				highlighter.Highlight(_droneRadius);
				shouldBeHighlighted.Add(highlighter);
			}
		}
		
		// Unhighlight obstacles that are no longer in range
		foreach (var highlighter in _currentlyHighlightedObstacles)
		{
			if (highlighter != null && !shouldBeHighlighted.Contains(highlighter))
			{
				highlighter.Unhighlight();
			}
		}
		
		// Update tracked set
		_currentlyHighlightedObstacles.Clear();
		foreach (var highlighter in shouldBeHighlighted)
		{
			_currentlyHighlightedObstacles.Add(highlighter);
		}
		
		return hasCollision;
	}

		internal void UpdateReadout(string text)
		{
			if (_depthReadout != null)
			{
				_depthReadout.SetText(text);
			}
		}

		internal void FadeReadout()
		{
			if (_depthReadout != null)
			{
				_depthReadout.FadeOut();
			}
		}

		internal void TickHaptics(float amplitude = 0.2f, float duration = 0.02f)
		{
			if (_hapticsHelper != null)
			{
				_hapticsHelper.Tick(amplitude, duration);
			}
		}

		internal void ConfirmHaptics(float amplitude = 0.6f, float duration = 0.08f)
		{
			if (_hapticsHelper != null)
			{
				_hapticsHelper.Pulse(amplitude, duration);
			}
		}

		/// <summary>
		/// Update ghost sphere color to match currently selected waypoint type.
		/// </summary>
		private void UpdateGhostColorForType()
		{
			if (_ghostRenderer == null) return;
			
			Color typeColor = WaypointTypeDefinition.GetTypeColor(_currentTypeSelection);
			foreach (var mat in _ghostRenderer.sharedMaterials)
			{
				if (mat != null && mat.HasProperty("_Color"))
				{
					mat.color = typeColor;
				}
			}
		}

		/// <summary>
		/// Get the data for a specific point by ID.
		/// </summary>
		public PointData? GetPointData(int id)
		{
			foreach (var point in _points)
			{
				if (point.Id == id)
				{
					return point;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Convert WaypointType to Experiment.PointType for tracking.
		/// </summary>
		private Experiment.PointType ConvertToPointType(WaypointType waypointType)
		{
			switch (waypointType)
			{
				case WaypointType.Flythrough:
					return Experiment.PointType.FlyThrough;
				case WaypointType.StopRotateContinue:
					return Experiment.PointType.StopAndRotate;
				case WaypointType.Record360:
					return Experiment.PointType.Record360;
				default:
					return Experiment.PointType.FlyThrough;
			}
		}
	}
}



