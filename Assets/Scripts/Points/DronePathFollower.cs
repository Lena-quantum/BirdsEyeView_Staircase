using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Points
{
	/// <summary>
	/// Animates a virtual drone along a flight path, respecting waypoint types and behaviors.
	/// Supports play/pause/restart and adjustable speed.
	/// </summary>
	public class DronePathFollower : MonoBehaviour
	{
		[Header("Drone Setup")]
		[SerializeField] private GameObject _dronePrefab;
		[SerializeField] private Transform _droneSpawnParent;
		[SerializeField] private float _droneScale = 1.0f; // Scale multiplier for drone size
		[SerializeField] private Vector3 _droneOffset = Vector3.zero; // Offset from waypoint position

		[Header("Flight Settings")]
		[SerializeField] private float _baseSpeed = 1.0f; // meters per second (base speed)
		[SerializeField] private float _speedMultiplier = 1.0f; // Speed multiplier (1.0 = normal, 2.0 = 2x faster)
		[SerializeField] private float _stopRotateDuration = 2.0f; // seconds to pause at StopRotateContinue waypoints
		[SerializeField] private float _record360Duration = 15.0f; // seconds for full 360° rotation at Record360 waypoints

		[Header("References")]
		[SerializeField] private FlightPathManager _pathManager;
		[SerializeField] private PointPlacementManager _pointManager;

		public enum FlightState
		{
			Idle,      // Not flying
			Playing,   // Flying along path
			Paused     // Paused mid-flight
		}

		private FlightState _currentState = FlightState.Idle;
		private GameObject _droneInstance;
		private Coroutine _flightCoroutine;

		/// <summary>
		/// Current flight state (Idle, Playing, Paused).
		/// </summary>
		public FlightState State => _currentState;

		/// <summary>
		/// Current speed multiplier (1.0 = normal speed).
		/// </summary>
		public float SpeedMultiplier
		{
			get => _speedMultiplier;
			set => _speedMultiplier = Mathf.Max(0.1f, Mathf.Min(10f, value));
		}

		/// <summary>
		/// Whether the drone is currently flying.
		/// </summary>
		public bool IsFlying => _currentState == FlightState.Playing;

		/// <summary>
		/// Whether the drone is paused.
		/// </summary>
		public bool IsPaused => _currentState == FlightState.Paused;

		private void Awake()
		{
			if (_pathManager == null)
			{
				_pathManager = FindFirstObjectByType<FlightPathManager>();
			}

			if (_pointManager == null)
			{
				_pointManager = FindFirstObjectByType<PointPlacementManager>();
			}
		}

		/// <summary>
		/// Start flying along the active/completed route.
		/// </summary>
		public void Play()
		{
			var route = GetRouteToFollow();
			if (route == null || route.PointCount < 2)
			{
				Debug.LogWarning("DronePathFollower: No valid route to follow. Need at least 2 waypoints.");
				return;
			}

			// Resume from paused state or start fresh
			if (_currentState == FlightState.Paused && _flightCoroutine != null)
			{
				_currentState = FlightState.Playing;
				return;
			}

			// Stop any existing flight
			Stop();

			_currentState = FlightState.Playing;
			_flightCoroutine = StartCoroutine(FlyRoute(route));
		}

		/// <summary>
		/// Pause the current flight.
		/// </summary>
		public void Pause()
		{
			if (_currentState == FlightState.Playing)
			{
				_currentState = FlightState.Paused;
			}
		}

		/// <summary>
		/// Restart the flight from the beginning.
		/// </summary>
		public void Restart()
		{
			Stop();
			Play();
		}

		/// <summary>
		/// Stop the flight and return to idle.
		/// </summary>
		public void Stop()
		{
			if (_flightCoroutine != null)
			{
				StopCoroutine(_flightCoroutine);
				_flightCoroutine = null;
			}

			_currentState = FlightState.Idle;

			// Destroy drone instance
			if (_droneInstance != null)
			{
				Destroy(_droneInstance);
				_droneInstance = null;
			}
		}

		/// <summary>
		/// Get the route to follow (completed route takes priority, then active).
		/// </summary>
		private FlightPath GetRouteToFollow()
		{
			if (_pathManager == null) return null;

			// Prefer completed route, but allow active route if no completed one exists
			var route = _pathManager.CompletedRoute ?? _pathManager.ActiveRoute;
			return route;
		}

		/// <summary>
		/// Get valid waypoint positions, skipping gaps (pointId <= 0) and missing points.
		/// </summary>
		private List<WaypointSegment> GetValidWaypointSegments(FlightPath route)
		{
			var segments = new List<WaypointSegment>();

			if (route == null || _pointManager == null) return segments;

			// Build list of valid waypoints with their types
			var validWaypoints = new List<WaypointSegment>();
			foreach (int pointId in route.PointIds)
			{
				// Skip gap markers (0 or negative IDs)
				if (pointId <= 0) continue;

				var pointHandle = _pointManager.GetPoint(pointId);
				if (pointHandle == null) continue;

				var segment = new WaypointSegment
				{
					PointId = pointId,
					Position = pointHandle.transform.position + _droneOffset,
					Type = pointHandle.WaypointType,
					Yaw = pointHandle.transform.eulerAngles.y
				};

				validWaypoints.Add(segment);
			}

			return validWaypoints;
		}

		/// <summary>
		/// Coroutine that flies the drone along the route.
		/// </summary>
		private IEnumerator FlyRoute(FlightPath route)
		{
			var segments = GetValidWaypointSegments(route);
			if (segments.Count < 2)
			{
				Debug.LogWarning("DronePathFollower: Route has less than 2 valid waypoints.");
				_currentState = FlightState.Idle;
				yield break;
			}

			// Spawn drone at first waypoint
			if (_dronePrefab != null)
			{
				Transform parent = _droneSpawnParent != null ? _droneSpawnParent : transform;
				_droneInstance = Instantiate(_dronePrefab, segments[0].Position, Quaternion.identity, parent);
				_droneInstance.transform.localScale = Vector3.one * _droneScale;
			}
			else
			{
				// Create a simple sphere if no prefab assigned
				_droneInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_droneInstance.transform.localScale = Vector3.one * 0.2f * _droneScale;
				_droneInstance.transform.position = segments[0].Position;
				_droneInstance.name = "Drone (Generated)";
			}

			// Fly between waypoints
			for (int i = 0; i < segments.Count - 1; i++)
			{
				var from = segments[i];
				var to = segments[i + 1];

				// Fly to next waypoint
				yield return StartCoroutine(FlyToPosition(from.Position, to.Position));

				// Handle waypoint-specific behavior
				switch (to.Type)
				{
					case WaypointType.Flythrough:
						// No pause - continue immediately
						break;

					case WaypointType.StopRotateContinue:
						// Pause and rotate to observe
						yield return StartCoroutine(StopAndRotate(to.Position, _stopRotateDuration));
						break;

					case WaypointType.Record360:
						// Stop and perform 360° rotation
						yield return StartCoroutine(Record360Rotation(to.Position, _record360Duration));
						break;
				}
			}

			// Flight complete
			_currentState = FlightState.Idle;
			Debug.Log("DronePathFollower: Flight complete!");
		}

		/// <summary>
		/// Coroutine that moves the drone from one position to another.
		/// </summary>
		private IEnumerator FlyToPosition(Vector3 from, Vector3 to)
		{
			if (_droneInstance == null) yield break;

			float distance = Vector3.Distance(from, to);
			float travelTime = distance / (_baseSpeed * _speedMultiplier);

			if (travelTime <= 0f)
			{
				_droneInstance.transform.position = to;
				yield break;
			}

			float elapsed = 0f;
			Vector3 startPos = from;

			// Wait for play state (paused state will block here)
			while (_currentState == FlightState.Paused)
			{
				yield return null;
			}

			while (elapsed < travelTime && _currentState == FlightState.Playing)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / travelTime);

				// Smooth interpolation
				_droneInstance.transform.position = Vector3.Lerp(startPos, to, t);

				// Orient drone towards destination
				Vector3 direction = (to - _droneInstance.transform.position).normalized;
				if (direction.magnitude > 0.01f)
				{
					Quaternion targetRotation = Quaternion.LookRotation(direction);
					_droneInstance.transform.rotation = Quaternion.Slerp(_droneInstance.transform.rotation, targetRotation, Time.deltaTime * 2f);
				}

				// Handle pause
				if (_currentState == FlightState.Paused)
				{
					startPos = _droneInstance.transform.position;
					travelTime -= elapsed;
					elapsed = 0f;

					while (_currentState == FlightState.Paused)
					{
						yield return null;
					}
				}

				yield return null;
			}

			// Ensure we end at exact position
			if (_droneInstance != null)
			{
				_droneInstance.transform.position = to;
			}
		}

		/// <summary>
		/// Coroutine that pauses and rotates the drone at a waypoint.
		/// </summary>
		private IEnumerator StopAndRotate(Vector3 position, float duration)
		{
			if (_droneInstance == null) yield break;

			float elapsed = 0f;
			Quaternion startRotation = _droneInstance.transform.rotation;
			Quaternion endRotation = startRotation * Quaternion.Euler(0, 180, 0); // Rotate 180° to look around

			while (elapsed < duration)
			{
				// Wait for play state if paused
				if (_currentState == FlightState.Paused)
				{
					while (_currentState == FlightState.Paused)
					{
						yield return null;
					}
					continue;
				}

				if (_currentState != FlightState.Playing) yield break;

				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);

				if (_droneInstance != null)
				{
					_droneInstance.transform.position = position;
					_droneInstance.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
				}

				yield return null;
			}
		}

		/// <summary>
		/// Coroutine that performs a slow 360° rotation at a waypoint.
		/// </summary>
		private IEnumerator Record360Rotation(Vector3 position, float duration)
		{
			if (_droneInstance == null) yield break;

			float elapsed = 0f;
			Quaternion startRotation = _droneInstance.transform.rotation;

			while (elapsed < duration)
			{
				// Wait for play state if paused
				if (_currentState == FlightState.Paused)
				{
					while (_currentState == FlightState.Paused)
					{
						yield return null;
					}
					continue;
				}

				if (_currentState != FlightState.Playing) yield break;

				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);

				if (_droneInstance != null)
				{
					_droneInstance.transform.position = position;
					_droneInstance.transform.rotation = startRotation * Quaternion.Euler(0, t * 360f, 0);
				}

				yield return null;
			}

			// Reset to start rotation
			if (_droneInstance != null)
			{
				_droneInstance.transform.rotation = startRotation;
			}
		}

		private void OnDestroy()
		{
			Stop();
		}

		/// <summary>
		/// Data structure for a waypoint segment in the flight path.
		/// </summary>
		private struct WaypointSegment
		{
			public int PointId;
			public Vector3 Position;
			public WaypointType Type;
			public float Yaw;
		}
	}
}
