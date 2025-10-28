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

		private readonly List<PointData> _points = new List<PointData>();
		private readonly Dictionary<int, PointHandle> _idToHandle = new Dictionary<int, PointHandle>();
		private int _nextId = 1;

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

		private void Awake()
		{
			if (_ghostRenderer == null)
			{
				var tr = _ghostTransform != null ? _ghostTransform.GetComponentInChildren<Renderer>() : null;
				if (tr != null) _ghostRenderer = tr;
			}
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

			int id = _nextId++;
			Vector3 position = _ghostTransform.position;
			Color color = _placedPointColor;
			float radius = _placedPointRadius;

			PointHandle handle = Instantiate(_pointHandlePrefab, position, Quaternion.identity, _pointsParent != null ? _pointsParent : transform);
			handle.Initialize(id, color, radius, this);

			var data = new PointData
			{
				Id = id,
				Position = position,
				Color = color,
				Radius = radius,
				CreatedAt = DateTime.UtcNow
			};

			_points.Add(data);
			_idToHandle[id] = handle;

			OnPointPlaced?.Invoke(data);
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
					// Remove any routes that contain this point
					var pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
					if (pathManager != null)
					{
						var routesToRemove = pathManager.GetRoutesContainingPoint(pointId);
						foreach (var route in routesToRemove)
						{
							pathManager.RemoveRoute(route.RouteName);
						}
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
			if (_ghostRenderer == null) return;
			Color target = isValid ? _ghostValidColor : _ghostInvalidColor;
			foreach (var mat in _ghostRenderer.sharedMaterials)
			{
				if (mat != null && mat.HasProperty("_Color"))
				{
					mat.color = target;
				}
			}
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

	}
}


