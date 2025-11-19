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
		[SerializeField] private float _recordPauseSeconds = 1.0f; // pause before/after record rotations

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
		private Vector3 _lastFlatForward = Vector3.forward;

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
			
			// Thesis Feature: Notify experiment tracker
			var experimentManager = Experiment.ExperimentDataManager.Instance;
			if (experimentManager != null)
			{
				experimentManager.OnDroneFlightStarted();
			}
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
			StopFlight(true);
			Play();
		}

		/// <summary>
		/// Stop the flight and return to idle.
		/// </summary>
		public void Stop()
		{
			StopFlight(true);
		}

		/// <summary>
		/// Reset the drone to the first waypoint without immediately restarting the flight.
		/// </summary>
		public void ResetToStart()
		{
			// Ensure we are not in the middle of a flight
			StopFlight(true);

			var route = GetRouteToFollow();
			if (route == null || route.PointCount == 0)
			{
				return;
			}

			var segments = GetValidWaypointSegments(route);
			if (segments.Count == 0)
			{
				return;
			}

			var first = segments[0];
			Vector3? nextPosition = segments.Count > 1 ? segments[1].Position : (Vector3?)null;

			SpawnDroneAt(first.Position, nextPosition);
			_currentState = FlightState.Idle;
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

			// Spawn drone at first waypoint (will orient toward next if available)
			Vector3? lookTarget = segments.Count > 1 ? segments[1].Position : (Vector3?)null;
			SpawnDroneAt(segments[0].Position, lookTarget);

			// Face the first leg of the route if we have at least two points
			if (segments.Count > 1)
			{
				Vector3 initialForward = Vector3.ProjectOnPlane(segments[1].Position - segments[0].Position, Vector3.up);
				if (initialForward.sqrMagnitude > 0.0001f)
				{
					initialForward.Normalize();
					_lastFlatForward = initialForward;
					if (_droneInstance != null)
					{
						_droneInstance.transform.rotation = Quaternion.LookRotation(initialForward, Vector3.up);
					}
				}
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
						yield return StartCoroutine(StopAndRotate(to, _stopRotateDuration));
						break;

					case WaypointType.Record360:
						// Stop and perform 360° rotation
						yield return StartCoroutine(Record360Rotation(to, _record360Duration));
						break;
				}
			}

			// Flight complete
			_currentState = FlightState.Idle;
			Debug.Log("DronePathFollower: Flight complete!");
			
			// Thesis Feature: Notify experiment tracker
			var experimentManager = Experiment.ExperimentDataManager.Instance;
			if (experimentManager != null)
			{
				experimentManager.OnDroneFlightCompleted(true);
			}
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

			Quaternion startRotation = ComputeLevelRotation(_droneInstance.transform.forward);
			_droneInstance.transform.rotation = startRotation;
			UpdateLastForward(startRotation * Vector3.forward);

			Vector3 desiredForward = Vector3.ProjectOnPlane(to - from, Vector3.up);
			if (desiredForward.sqrMagnitude < 0.0001f)
			{
				desiredForward = _lastFlatForward;
			}
			else
			{
				desiredForward.Normalize();
				_lastFlatForward = desiredForward;
			}
			Quaternion targetRotation = Quaternion.LookRotation(_lastFlatForward, Vector3.up);

			while (elapsed < travelTime)
			{
				if (_currentState == FlightState.Paused)
				{
					yield return null;
					continue;
				}

				if (_currentState != FlightState.Playing)
				{
					yield break;
				}

				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / travelTime);

				// Smooth interpolation
				_droneInstance.transform.position = Vector3.Lerp(startPos, to, t);

				// Orient drone towards destination
				_droneInstance.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

				yield return null;
			}

			// Ensure we end at exact position
			if (_droneInstance != null)
			{
				_droneInstance.transform.position = to;
				_droneInstance.transform.rotation = targetRotation;
				UpdateLastForward(targetRotation * Vector3.forward);
			}
		}

		private void StopFlight(bool destroyDrone)
		{
			if (_flightCoroutine != null)
			{
				StopCoroutine(_flightCoroutine);
				_flightCoroutine = null;
			}

			_currentState = FlightState.Idle;

			if (destroyDrone && _droneInstance != null)
			{
				Destroy(_droneInstance);
				_droneInstance = null;
			}
		}

		private void SpawnDroneAt(Vector3 position, Vector3? nextPosition)
		{
			if (_dronePrefab != null)
			{
				Transform parent = _droneSpawnParent != null ? _droneSpawnParent : transform;
				_droneInstance = Instantiate(_dronePrefab, position, Quaternion.identity, parent);
				_droneInstance.transform.localScale = Vector3.one * _droneScale;
			}
			else
			{
				_droneInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_droneInstance.transform.localScale = Vector3.one * 0.2f * _droneScale;
				_droneInstance.transform.position = position;
				_droneInstance.name = "Drone (Generated)";
			}

			if (_droneInstance == null)
			{
				return;
			}

			_droneInstance.transform.position = position;

			Quaternion targetRotation;
			if (nextPosition.HasValue)
			{
				Vector3 forward = Vector3.ProjectOnPlane(nextPosition.Value - position, Vector3.up);
				if (forward.sqrMagnitude > 0.0001f)
				{
					forward.Normalize();
					targetRotation = Quaternion.LookRotation(forward, Vector3.up);
					UpdateLastForward(forward);
				}
				else
				{
					targetRotation = ComputeLevelRotation(_droneInstance.transform.forward);
					UpdateLastForward(targetRotation * Vector3.forward);
				}
			}
			else
			{
				targetRotation = ComputeLevelRotation(_droneInstance.transform.forward);
				UpdateLastForward(targetRotation * Vector3.forward);
			}

			_droneInstance.transform.rotation = targetRotation;
		}

		/// <summary>
		/// Coroutine that pauses and rotates the drone at a waypoint.
		/// </summary>
		private IEnumerator StopAndRotate(WaypointSegment waypoint, float duration)
		{
			if (_droneInstance == null) yield break;

			Quaternion startRotation = ComputeLevelRotation(_droneInstance.transform.forward);
			_droneInstance.transform.rotation = startRotation;
			UpdateLastForward(startRotation * Vector3.forward);

			Quaternion targetRotation = Quaternion.AngleAxis(waypoint.Yaw, Vector3.up);

			float elapsed = 0f;

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
					_droneInstance.transform.position = waypoint.Position;
					_droneInstance.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
				}

				yield return null;
			}

			if (_droneInstance != null)
			{
				_droneInstance.transform.position = waypoint.Position;
				_droneInstance.transform.rotation = targetRotation;
				UpdateLastForward(targetRotation * Vector3.forward);
			}
		}

		/// <summary>
		/// Coroutine that performs a slow 360° rotation at a waypoint.
		/// </summary>
		private IEnumerator Record360Rotation(WaypointSegment waypoint, float duration)
		{
			if (_droneInstance == null) yield break;

			if (_recordPauseSeconds > 0f)
			{
				yield return PauseAtPosition(waypoint.Position, _recordPauseSeconds);
			}

			Quaternion baseRotation = ComputeLevelRotation(_droneInstance.transform.forward);
			_droneInstance.transform.rotation = baseRotation;
			UpdateLastForward(baseRotation * Vector3.forward);

			float elapsed = 0f;

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
					_droneInstance.transform.position = waypoint.Position;
					_droneInstance.transform.rotation = baseRotation * Quaternion.AngleAxis(t * 360f, Vector3.up);
				}

				yield return null;
			}

			// Reset to start rotation
			if (_droneInstance != null)
			{
				_droneInstance.transform.position = waypoint.Position;
				_droneInstance.transform.rotation = baseRotation;
				UpdateLastForward(baseRotation * Vector3.forward);
			}

			if (_recordPauseSeconds > 0f)
			{
				yield return PauseAtPosition(waypoint.Position, _recordPauseSeconds);
			}
		}

		private IEnumerator PauseAtPosition(Vector3 position, float duration)
		{
			if (_droneInstance == null) yield break;
			if (duration <= 0f) yield break;

			float elapsed = 0f;
			while (elapsed < duration)
			{
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
				if (_droneInstance != null)
				{
					_droneInstance.transform.position = position;
				}
				yield return null;
			}
		}

		private Quaternion ComputeLevelRotation(Vector3 forward)
		{
			Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
			if (flatForward.sqrMagnitude < 0.0001f)
			{
				flatForward = _lastFlatForward.sqrMagnitude > 0.0f ? _lastFlatForward : Vector3.forward;
			}
			else
			{
				flatForward.Normalize();
			}

			return Quaternion.LookRotation(flatForward, Vector3.up);
		}

		private void UpdateLastForward(Vector3 forward)
		{
			Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
			if (flatForward.sqrMagnitude >= 0.0001f)
			{
				_lastFlatForward = flatForward.normalized;
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
