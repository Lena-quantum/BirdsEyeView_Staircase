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
		[SerializeField] private float _stopRotateDuration = 2.0f; // seconds to pause at StopTurnGo waypoints
		[SerializeField] private float _record360Duration = 15.0f; // seconds for full 360° rotation at Record360 waypoints
		[SerializeField] private float _recordPauseSeconds = 1.0f; // pause before/after record rotations

		[Header("Record Visuals")]
		[SerializeField] private bool _showRecordLightStream = true;
		[SerializeField] private Color _recordLightStreamColor = new Color(1f, 0.3f, 0.1f, 0.85f);
		[SerializeField, Min(0f)] private float _recordLightStreamLength = 2.5f;
		[SerializeField, Min(0f)] private float _recordLightStreamWidth = 0.05f;
		[SerializeField] private Vector3 _recordLightStreamLocalOffset = new Vector3(0f, -0.05f, 0.6f);

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
		private LineRenderer _recordLightStream;
		private Material _recordLightStreamMaterial;

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
	/// Get valid waypoint positions, skipping gaps (pointId == 0) and missing points.
	/// Includes Start/End points (negative IDs).
	/// </summary>
	private List<WaypointSegment> GetValidWaypointSegments(FlightPath route)
	{
		var segments = new List<WaypointSegment>();

		if (route == null || _pointManager == null) return segments;

		// Build list of valid waypoints with their types
		var validWaypoints = new List<WaypointSegment>();
		foreach (int pointId in route.PointIds)
		{
			// Skip only explicit gap markers (0)
			if (pointId == 0) continue;

			// Handle Start/End points (negative IDs)
			if (pointId == -1) // Start point
			{
				var startPoint = _pathManager?.GetStartPoint();
				if (startPoint != null)
				{
					var segment = new WaypointSegment
					{
					PointId = pointId,
					Position = startPoint.Position + _droneOffset,
					Type = WaypointType.StopTurnGo, // Start point - standard navigation point
					Yaw = startPoint.transform.eulerAngles.y
					};
					validWaypoints.Add(segment);
				}
				continue;
			}
			
			if (pointId == -2) // End point
			{
				var endPoint = _pathManager?.GetEndPoint();
				if (endPoint != null)
				{
					var segment = new WaypointSegment
					{
					PointId = pointId,
					Position = endPoint.Position + _droneOffset,
					Type = WaypointType.StopTurnGo, // End point - standard navigation point
					Yaw = endPoint.transform.eulerAngles.y
					};
					validWaypoints.Add(segment);
				}
				continue;
			}

			// Handle regular waypoints (positive IDs)
			var pointHandle = _pointManager.GetPoint(pointId);
			if (pointHandle == null) continue;

			var waypointSegment = new WaypointSegment
			{
				PointId = pointId,
				Position = pointHandle.transform.position + _droneOffset,
				Type = pointHandle.WaypointType,
				Yaw = pointHandle.transform.eulerAngles.y
			};

			validWaypoints.Add(waypointSegment);
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

			// Fly to next waypoint (drone flies straight without rotating)
			yield return StartCoroutine(FlyToPosition(from.Position, to.Position, false));

			// Handle waypoint-specific behavior
			switch (to.Type)
			{
				case WaypointType.StopTurnGo:
					// Pause, then rotate to face next waypoint (if there is one)
					Vector3? nextWaypointPos = (i + 2 < segments.Count) ? segments[i + 2].Position : (Vector3?)null;
					yield return StartCoroutine(StopAndRotateToNext(to, nextWaypointPos, _stopRotateDuration));
					break;

			case WaypointType.Record360:
				// Stop and perform 360° rotation, then rotate to face next waypoint
				Vector3? nextWaypointPosFor360 = (i + 2 < segments.Count) ? segments[i + 2].Position : (Vector3?)null;
				yield return StartCoroutine(Record360Rotation(to, nextWaypointPosFor360, _record360Duration));
				break;
			}
		}

			SetRecordLightStreamVisible(false);

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
	/// <param name="from">Starting position</param>
	/// <param name="to">Target position</param>
	/// <param name="rotateTowardDestination">If true, drone rotates toward destination; if false, maintains current rotation</param>
	private IEnumerator FlyToPosition(Vector3 from, Vector3 to, bool rotateTowardDestination = true)
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

		// Lock rotation at the start - drone flies straight
		Quaternion startRotation = _droneInstance.transform.rotation;

		// Only update target rotation if explicitly requested (used for vertical Record360 movement)
		Quaternion targetRotation = startRotation;
		if (rotateTowardDestination)
		{
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
			targetRotation = Quaternion.LookRotation(_lastFlatForward, Vector3.up);
		}

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

			// Only rotate if requested (for vertical detours in Record360)
			if (rotateTowardDestination)
			{
				_droneInstance.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
			}
			else
			{
				// Maintain current rotation - fly straight
				_droneInstance.transform.rotation = startRotation;
			}

			yield return null;
		}

		// Ensure we end at exact position
		if (_droneInstance != null)
		{
			_droneInstance.transform.position = to;
			if (rotateTowardDestination)
			{
				_droneInstance.transform.rotation = targetRotation;
				UpdateLastForward(targetRotation * Vector3.forward);
			}
			else
			{
				// Keep rotation as-is
				_droneInstance.transform.rotation = startRotation;
			}
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

			if (destroyDrone)
			{
				CleanupRecordLightStream(false);
			}
			else
			{
				SetRecordLightStreamVisible(false);
			}

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

			SetupRecordLightStream();
		}

	/// <summary>
	/// Coroutine that pauses the drone, then rotates it to face the next waypoint.
	/// Thesis Feature: Shortest-angle rotation for efficient indoor navigation.
	/// </summary>
	private IEnumerator StopAndRotateToNext(WaypointSegment currentWaypoint, Vector3? nextWaypointPosition, float pauseDuration)
	{
		if (_droneInstance == null) yield break;

		// Step 1: Pause at current waypoint (half the duration)
		float pauseTime = pauseDuration * 0.5f;
		float elapsed = 0f;

		while (elapsed < pauseTime)
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
				_droneInstance.transform.position = currentWaypoint.Position;
			}

			yield return null;
		}

		// Step 2: Rotate to face next waypoint (if there is one)
		if (nextWaypointPosition.HasValue)
		{
			Quaternion startRotation = _droneInstance.transform.rotation;
			
			// Calculate direction to next waypoint (shortest angle on horizontal plane)
			Vector3 directionToNext = Vector3.ProjectOnPlane(nextWaypointPosition.Value - currentWaypoint.Position, Vector3.up);
			
			if (directionToNext.sqrMagnitude > 0.0001f)
			{
				directionToNext.Normalize();
				Quaternion targetRotation = Quaternion.LookRotation(directionToNext, Vector3.up);

				// Rotate over the remaining duration
				float rotateTime = pauseDuration * 0.5f;
				elapsed = 0f;

				while (elapsed < rotateTime)
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
					float t = Mathf.Clamp01(elapsed / rotateTime);

					if (_droneInstance != null)
					{
						_droneInstance.transform.position = currentWaypoint.Position;
						_droneInstance.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
					}

					yield return null;
				}

				// Ensure final rotation is exact
				if (_droneInstance != null)
				{
					_droneInstance.transform.position = currentWaypoint.Position;
					_droneInstance.transform.rotation = targetRotation;
					UpdateLastForward(directionToNext);
				}
			}
		}
	}

	/// <summary>
	/// Coroutine that performs a slow 360° rotation at a waypoint.
	/// Record360 Feature: Fly to anchor, move vertically to recording height, record, return to anchor, then rotate to next waypoint.
	/// </summary>
	/// <param name="waypoint">Current Record360 waypoint</param>
	/// <param name="nextWaypointPosition">Position of next waypoint (if any) to rotate toward after recording</param>
	/// <param name="duration">Duration of the 360° rotation</param>
	private IEnumerator Record360Rotation(WaypointSegment waypoint, Vector3? nextWaypointPosition, float duration)
	{
		if (_droneInstance == null) yield break;

		// Get the point handle to check if it has a separate recording position
		PointHandle handle = _pointManager != null ? _pointManager.GetPoint(waypoint.PointId) : null;
		Vector3 anchorPosition = waypoint.Position;
		Vector3 recordingPosition = anchorPosition; // Default to anchor if no separate position

		if (handle != null && handle.HasRecordingPosition && handle.RecordingPosition.HasValue)
		{
			recordingPosition = handle.RecordingPosition.Value;
			Debug.Log($"DronePathFollower: Record360 waypoint has separate recording position: Anchor={anchorPosition}, Recording={recordingPosition}");
		}

		bool hasVerticalDetour = Vector3.Distance(anchorPosition, recordingPosition) > 0.01f;

		// Step 1: Pause at anchor position
		if (_recordPauseSeconds > 0f && hasVerticalDetour)
		{
			yield return PauseAtPosition(anchorPosition, _recordPauseSeconds);
		}

		// Step 2: Fly vertically to recording position (if different from anchor)
		// Don't rotate during vertical movement - maintain current orientation
		if (hasVerticalDetour)
		{
			yield return StartCoroutine(FlyToPosition(anchorPosition, recordingPosition, false));
		}

		// Step 3: Perform 360° recording at recording position
		bool showingLightStream = PrepareRecordLightStreamForRecord();

		if (_recordPauseSeconds > 0f)
		{
			yield return PauseAtPosition(recordingPosition, _recordPauseSeconds);
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
				_droneInstance.transform.position = recordingPosition;
				_droneInstance.transform.rotation = baseRotation * Quaternion.AngleAxis(t * 360f, Vector3.up);
			}

			yield return null;
		}

		// Reset to start rotation
		if (_droneInstance != null)
		{
			_droneInstance.transform.position = recordingPosition;
			_droneInstance.transform.rotation = baseRotation;
			UpdateLastForward(baseRotation * Vector3.forward);
		}

		if (showingLightStream)
		{
			SetRecordLightStreamVisible(false);
		}

		if (_recordPauseSeconds > 0f)
		{
			yield return PauseAtPosition(recordingPosition, _recordPauseSeconds);
		}

		// Step 4: Return to anchor position (if we moved vertically)
		// Don't rotate during vertical return - maintain current orientation
		if (hasVerticalDetour)
		{
			yield return StartCoroutine(FlyToPosition(recordingPosition, anchorPosition, false));
		}

		// Step 5: Rotate to face next waypoint (if there is one)
		// This ensures the drone is oriented correctly before flying to the next point
		if (nextWaypointPosition.HasValue && _droneInstance != null)
		{
			Quaternion startRotation = _droneInstance.transform.rotation;
			
			// Calculate direction to next waypoint (shortest angle on horizontal plane)
			Vector3 directionToNext = Vector3.ProjectOnPlane(nextWaypointPosition.Value - anchorPosition, Vector3.up);
			
			if (directionToNext.sqrMagnitude > 0.0001f)
			{
				directionToNext.Normalize();
				Quaternion targetRotation = Quaternion.LookRotation(directionToNext, Vector3.up);

				// Rotate over a short duration (use same as stop rotate duration)
				float rotateTime = _stopRotateDuration * 0.5f;
				elapsed = 0f;

				while (elapsed < rotateTime)
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
					float t = Mathf.Clamp01(elapsed / rotateTime);

					if (_droneInstance != null)
					{
						_droneInstance.transform.position = anchorPosition;
						_droneInstance.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
					}

					yield return null;
				}

				// Ensure final rotation is exact
				if (_droneInstance != null)
				{
					_droneInstance.transform.position = anchorPosition;
					_droneInstance.transform.rotation = targetRotation;
					UpdateLastForward(directionToNext);
				}
			}
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

		private void SetupRecordLightStream()
		{
			if (!_showRecordLightStream || _droneInstance == null)
			{
				return;
			}

			if (_recordLightStream == null)
			{
				var streamGO = new GameObject("RecordLightStream");
				streamGO.transform.SetParent(_droneInstance.transform, false);
				_recordLightStream = streamGO.AddComponent<LineRenderer>();
				_recordLightStream.useWorldSpace = false;
				_recordLightStream.numCapVertices = 6;
				_recordLightStream.textureMode = LineTextureMode.Stretch;
				_recordLightStream.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				_recordLightStream.receiveShadows = false;
				_recordLightStream.material = GetOrCreateRecordLightStreamMaterial();
			}
			else
			{
				_recordLightStream.transform.SetParent(_droneInstance.transform, false);
			}

			_recordLightStream.transform.localPosition = _recordLightStreamLocalOffset;
			_recordLightStream.transform.localRotation = Quaternion.identity;
			_recordLightStream.positionCount = 2;

			UpdateRecordLightStreamVisual();
			SetRecordLightStreamVisible(false);
		}

		private Material GetOrCreateRecordLightStreamMaterial()
		{
			if (_recordLightStreamMaterial == null)
			{
				Shader shader = Shader.Find("Sprites/Default");
				if (shader == null)
				{
					shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
				}

				if (shader == null)
				{
					shader = Shader.Find("Standard");
				}

				_recordLightStreamMaterial = new Material(shader)
				{
					name = "RecordLightStream (Runtime)",
					enableInstancing = true,
					hideFlags = HideFlags.DontSave
				};
			}

			_recordLightStreamMaterial.color = _recordLightStreamColor;
			return _recordLightStreamMaterial;
		}

		private void UpdateRecordLightStreamVisual()
		{
			if (_recordLightStream == null) return;

			_recordLightStream.widthMultiplier = _recordLightStreamWidth;

			float length = Mathf.Max(0f, _recordLightStreamLength);
			_recordLightStream.SetPosition(0, Vector3.zero);
			_recordLightStream.SetPosition(1, Vector3.forward * length);

			var headColor = _recordLightStreamColor;
			var tailColor = new Color(headColor.r, headColor.g, headColor.b, 0f);

			var gradient = new Gradient();
			gradient.SetKeys(
				new[]
				{
					new GradientColorKey(headColor, 0f),
					new GradientColorKey(headColor, 0.4f),
					new GradientColorKey(tailColor, 1f)
				},
				new[]
				{
					new GradientAlphaKey(headColor.a, 0f),
					new GradientAlphaKey(headColor.a, 0.5f),
					new GradientAlphaKey(0f, 1f)
				});

			_recordLightStream.colorGradient = gradient;
		}

		private void SetRecordLightStreamVisible(bool visible)
		{
			if (_recordLightStream != null)
			{
				_recordLightStream.enabled = visible;
			}
		}

		private bool PrepareRecordLightStreamForRecord()
		{
			if (!_showRecordLightStream || _droneInstance == null)
			{
				return false;
			}

			SetupRecordLightStream();

			if (_recordLightStream == null)
			{
				return false;
			}

			UpdateRecordLightStreamVisual();
			SetRecordLightStreamVisible(true);
			return true;
		}

		private void CleanupRecordLightStream(bool destroyMaterial)
		{
			if (_recordLightStream != null)
			{
				if (_recordLightStream.gameObject != null)
				{
					Destroy(_recordLightStream.gameObject);
				}

				_recordLightStream = null;
			}

			if (destroyMaterial && _recordLightStreamMaterial != null)
			{
				Destroy(_recordLightStreamMaterial);
				_recordLightStreamMaterial = null;
			}
		}

		private void OnDestroy()
		{
			Stop();
			CleanupRecordLightStream(true);
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
