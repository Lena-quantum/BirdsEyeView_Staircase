using UnityEngine;
using UnityEngine.XR;

namespace Points
{
	/// <summary>
	/// Handles input when in path building mode, extending the existing input system.
	/// </summary>
	public class PathModeController : MonoBehaviour
	{
		[SerializeField] private FlightPathManager _pathManager;
		[SerializeField] private PointPlacementManager _pointManager;
		[SerializeField] private PathRenderer _pathRenderer;
		[SerializeField] private PathWarningPopup _warningPopup;
		[SerializeField] private LayerMask _pointLayerMask = -1;

		[Header("Input Settings")]
		[SerializeField] private float _hapticAmplitude = 0.3f;
		[SerializeField] private float _hapticDuration = 0.05f;

		private InputDevice _rightHand;
		private InputDevice _leftHand;
		private bool _rightGripPrev;
		private bool _bButtonPrev;
		private bool _aButtonPrev;
		private bool _triggerPrev;
		
		// When set, indicates we want to continue the route starting from this point
		private int? _resumeFromPointId;

		/// <summary>
		/// Layer mask for point raycast detection.
		/// </summary>
		public LayerMask PointLayerMask
		{
			get => _pointLayerMask;
			set => _pointLayerMask = value;
		}

		private void Awake()
		{
			if (_pathManager == null)
			{
				_pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
			}

			if (_pointManager == null)
			{
				_pointManager = UnityEngine.Object.FindFirstObjectByType<PointPlacementManager>();
			}
			
			if (_pathRenderer == null)
			{
				_pathRenderer = UnityEngine.Object.FindFirstObjectByType<PathRenderer>();
			}

			// Reset merge anchor whenever the active route changes
			if (_pathManager != null)
			{
				_pathManager.OnActiveRouteChanged += _ => { _resumeFromPointId = null; };
			}
		}

		private void Start()
		{
			_rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
			_leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
		}

		private void Update()
		{
			if (_pathManager == null) return;

			// Update input devices
			_rightHand = EnsureDevice(_rightHand, XRNode.RightHand);
			_leftHand = EnsureDevice(_leftHand, XRNode.LeftHand);

			// Handle path mode toggle (Right Grip button - changed from Menu to avoid Oculus Home)
			bool rightGrip = ReadButton(_rightHand, CommonUsages.gripButton);
			if (EdgePressed(rightGrip, ref _rightGripPrev))
			{
				// Clear any pending merge anchor when toggling mode
				_resumeFromPointId = null;
				_pathManager.TogglePathMode();
				ProvideHapticFeedback(_hapticAmplitude, _hapticDuration);
			}

			// Only handle path mode input when path mode is enabled
			if (!_pathManager.PathModeEnabled) return;

			HandlePathModeInput();
		}

		private void HandlePathModeInput()
		{
			// Handle trigger for adding points to route
			bool trigger = ReadButton(_rightHand, CommonUsages.triggerButton);
			if (EdgePressed(trigger, ref _triggerPrev))
			{
				HandleTriggerInput();
			}

			// Handle B button for undo
			bool bButton = ReadButton(_rightHand, CommonUsages.secondaryButton);
			if (EdgePressed(bButton, ref _bButtonPrev))
			{
				_pathManager.UndoLastPoint();
				ProvideHapticFeedback(_hapticAmplitude * 0.5f, _hapticDuration);
			}

			// A button is used for depth control in normal mode - not used in path mode

			// DISABLED: Grip for finishing route (now used for path mode toggle)
			// bool gripButton = ReadButton(_rightHand, CommonUsages.gripButton);
			// if (EdgePressed(gripButton, ref _gripButtonPrev))
			// {
			// 	FinishCurrentRoute();
			// }

			// Handle point hovering for visual feedback - DISABLED to avoid conflicts with RayDepthController
			// HandlePointHovering();
		}

		private void HandleTriggerInput()
		{
			// Raycast to find point under the right controller
			if (_pointManager?.PointsParent == null) return;

			var rightControllerTransform = GetRightControllerTransform();
			if (rightControllerTransform == null) return;

			Vector3 origin = rightControllerTransform.position;
			Vector3 direction = rightControllerTransform.forward;

			// First try to hit a point handle with much longer range for better reliability
			if (Physics.Raycast(origin, direction, out RaycastHit hit, 50f, _pointLayerMask))
			{
				Debug.Log($"PathMode raycast hit: {hit.collider.name} at distance {hit.distance}");
				var pointHandle = hit.collider.GetComponent<PointHandle>();
				if (pointHandle != null)
				{
					Debug.Log($"Found PointHandle {pointHandle.Id} for path building");
					// Add point to current route
					AddPointToRoute(pointHandle);
					return;
				}
				else
				{
					Debug.Log($"Hit {hit.collider.name} but no PointHandle component found");
				}
			}
			else
			{
				Debug.Log("PathMode raycast hit nothing");
			}

			// If no point is hit, treat as a no-op to avoid accidentally clearing/starting routes
			// Do not start a new route on empty-space clicks
			return;
		}

		private void AddPointToRoute(PointHandle pointHandle)
		{
			if (pointHandle == null || _pathManager == null) return;

			// Check if point is already in the current route
			var activeRoute = _pathManager.ActiveRoute;
			if (activeRoute != null && activeRoute.ContainsPoint(pointHandle.Id))
			{
				// If we are resuming from a point and the user clicked another existing point,
				// try to merge segments by removing the break between them
				if (_resumeFromPointId.HasValue && _resumeFromPointId.Value != pointHandle.Id)
				{
				if (SegmentBlockedBetween(_resumeFromPointId.Value, pointHandle.Id))
				{
					Debug.LogWarning("PathModeController: Cannot merge segments through a no-fly zone.");
					_warningPopup?.ShowMessage("Path blocked: segment enters a no-fly zone. Select a different point.");
					ProvideHapticFeedback(_hapticAmplitude * 0.2f, _hapticDuration * 2f);
					
					// Thesis Feature: Notify experiment tracker
					var prevHandle = _pointManager?.GetPoint(_resumeFromPointId.Value);
					if (prevHandle != null)
					{
						var experimentManager = Experiment.ExperimentDataManager.Instance;
						if (experimentManager != null)
						{
							experimentManager.OnSegmentBlocked(
								prevHandle.transform.position, 
								pointHandle.transform.position,
								"NoFlyZone"
							);
						}
					}
					
					return;
				}

					bool merged = _pathManager.MergeSegmentsBetween(_resumeFromPointId.Value, pointHandle.Id);
					if (merged)
					{
						_resumeFromPointId = pointHandle.Id; // now continue from this point
						ProvideHapticFeedback(_hapticAmplitude, _hapticDuration);
						return;
					}
				}

				// If clicking on the LAST point of the current route, allow continuing
				if (activeRoute.PointIds[activeRoute.PointIds.Count - 1] == pointHandle.Id)
				{
					Debug.Log($"Selected last point {pointHandle.Id} - ready to continue route");
					ProvideHapticFeedback(_hapticAmplitude, _hapticDuration);
					_resumeFromPointId = pointHandle.Id;
					return; // Don't add duplicate, just indicate we're ready to continue
				}
				
				// If clicking on the first point of a valid route, close the loop
				if (activeRoute.PointCount >= 3 && activeRoute.PointIds[0] == pointHandle.Id)
				{
					FinishCurrentRoute(true); // Close the loop
					return;
				}

				// If point is already in route and it's not last/first, set resume anchor
				_resumeFromPointId = pointHandle.Id;
				Debug.Log($"Anchored continuation at point {pointHandle.Id}. Select another existing point to merge, or a new point to extend.");
				ProvideHapticFeedback(_hapticAmplitude, _hapticDuration * 0.75f);
				return;
			}

			// Start new route if none exists
			if (activeRoute == null)
			{
				// Thesis Feature: If clicking any point that exists in the completed route,
				// continue that route for in-place editing instead of starting fresh
				var completedRoute = _pathManager.CompletedRoute;
				if (completedRoute != null && completedRoute.ContainsPoint(pointHandle.Id))
				{
					Debug.Log($"Continuing completed route from point {pointHandle.Id}");
					// Continue the completed route instead of creating a new one
					_resumeFromPointId = null; // ensure no stale anchor can auto-merge
					_pathManager.ContinueCurrentRoute();
					
					// Update visuals
					pointHandle.UpdateVisualState();
					if (_pathRenderer != null)
					{
						_pathRenderer.UpdateActiveRoute();
					}
					
					ProvideHapticFeedback(_hapticAmplitude, _hapticDuration);
					_resumeFromPointId = pointHandle.Id;
					Debug.Log($"Route continued. Select another existing point to merge, or a new point to extend.");
					return; // Don't add the point again - it's already in the route
				}
				else
				{
					_pathManager.StartNewRoute();
					activeRoute = _pathManager.ActiveRoute; // Get the newly created route
				}
			}

			// Prevent creating segments that pass through collision zones
			if (activeRoute != null)
			{
				int? startPointId = null;
				if (_resumeFromPointId.HasValue && _resumeFromPointId.Value != pointHandle.Id && activeRoute.ContainsPoint(_resumeFromPointId.Value))
				{
					startPointId = _resumeFromPointId.Value;
				}
				else
				{
					int lastPointId = GetLastRealPointId(activeRoute);
					if (lastPointId > 0 && lastPointId != pointHandle.Id)
					{
						startPointId = lastPointId;
					}
				}

				if (startPointId.HasValue && SegmentBlockedBetween(startPointId.Value, pointHandle.Id))
				{
					Debug.LogWarning("PathModeController: Cannot connect route through a no-fly zone.");
					_warningPopup?.ShowMessage("Path blocked: segment enters a no-fly zone. Select a different point.");
					ProvideHapticFeedback(_hapticAmplitude * 0.2f, _hapticDuration * 2f);
					
					// Thesis Feature: Notify experiment tracker
					var prevHandle = _pointManager?.GetPoint(startPointId.Value);
					if (prevHandle != null)
					{
						var experimentManager = Experiment.ExperimentDataManager.Instance;
						if (experimentManager != null)
						{
							experimentManager.OnSegmentBlocked(
								prevHandle.transform.position, 
								pointHandle.transform.position,
								"NoFlyZone"
							);
						}
					}
					
					return;
				}
			}

			// Add or insert the point based on anchor selection
			bool added = false;
			int? previousPointId = null;
			
			if (_resumeFromPointId.HasValue)
			{
				// Insert directly after the anchor to build forward inside a gap
				previousPointId = _resumeFromPointId.Value;
				added = activeRoute.InsertPointAfter(_resumeFromPointId.Value, pointHandle.Id);
				if (added)
				{
					_resumeFromPointId = pointHandle.Id; // chain forward
				}
			}
			if (!added)
			{
				// Default to appending if no anchor or insert failed
				int lastId = GetLastRealPointId(activeRoute);
				if (lastId > 0) previousPointId = lastId;
				
				activeRoute.AddPoint(pointHandle.Id);
				_resumeFromPointId = pointHandle.Id;
			}
			
			// Thesis Feature: Notify experiment tracker of segment creation
			if (previousPointId.HasValue && _pointManager != null)
			{
				var prevHandle = _pointManager.GetPoint(previousPointId.Value);
				if (prevHandle != null)
				{
					var experimentManager = Experiment.ExperimentDataManager.Instance;
					if (experimentManager != null)
					{
						experimentManager.OnSegmentCreated(
							previousPointId.Value, 
							pointHandle.Id, 
							prevHandle.transform.position, 
							pointHandle.transform.position
						);
					}
				}
			}
			
			// Update the point's visual state immediately
			pointHandle.UpdateVisualState();
			
			// Update path rendering immediately for real-time feedback
			if (_pathRenderer != null)
			{
				_pathRenderer.UpdateActiveRoute();
			}

			ProvideHapticFeedback(_hapticAmplitude, _hapticDuration);
		}

		private void FinishCurrentRoute(bool closeLoop = false)
		{
			if (_pathManager?.ActiveRoute == null) return;

			_pathManager.FinishCurrentRoute(closeLoop);
			ProvideHapticFeedback(_hapticAmplitude * 1.5f, _hapticDuration * 2f);
		}

		/// <summary>
		/// Thesis Feature: Check if the completed route ends with this point (single route mode).
		/// </summary>
		private bool CompletedRouteEndsWithPoint(int pointId)
		{
			if (_pathManager == null) return false;

			var completedRoute = _pathManager.CompletedRoute;
			if (completedRoute != null && completedRoute.PointCount > 0)
			{
				return completedRoute.PointIds[completedRoute.PointIds.Count - 1] == pointId;
			}
			return false;
		}

		private void HandlePointHovering()
		{
			// Raycast to find hovered point
			if (_pointManager?.PointsParent == null) return;

			var rightControllerTransform = GetRightControllerTransform();
			if (rightControllerTransform == null) return;

			Vector3 origin = rightControllerTransform.position;
			Vector3 direction = rightControllerTransform.forward;

			if (Physics.Raycast(origin, direction, out RaycastHit hit, 10f, _pointLayerMask))
			{
				var pointHandle = hit.collider.GetComponent<PointHandle>();
				if (pointHandle != null)
				{
					// Provide visual feedback for hovered points
					HandlePointHover(pointHandle, true);
					return;
				}
			}

			// Clear hover state for all points
			ClearAllPointHovers();
		}

		private void HandlePointHover(PointHandle pointHandle, bool isHovered)
		{
			if (pointHandle == null) return;

			// Update point visual state based on route membership
			UpdatePointVisualState(pointHandle, isHovered);
		}

		private void UpdatePointVisualState(PointHandle pointHandle, bool isHovered)
		{
			if (pointHandle == null || _pathManager == null) return;

			var activeRoute = _pathManager.ActiveRoute;
			bool isInActiveRoute = activeRoute != null && activeRoute.ContainsPoint(pointHandle.Id);

			// Change point color based on route membership
			var renderer = pointHandle.GetComponent<Renderer>();
			if (renderer != null)
			{
				Color targetColor;
				
				if (isHovered)
				{
					targetColor = Color.white; // Bright white when hovered
				}
				else
				{
					// Thesis Feature: Always use type-specific color
					targetColor = WaypointTypeDefinition.GetTypeColor(pointHandle.WaypointType);
					
					// Subtle highlight if in active route
					if (isInActiveRoute)
					{
						targetColor = Color.Lerp(targetColor, Color.white, 0.2f);
					}
				}

				// Smoothly transition to target color
				foreach (var material in renderer.materials)
				{
					if (material != null && material.HasProperty("_Color"))
					{
						material.color = Color.Lerp(material.color, targetColor, Time.deltaTime * 10f);
					}
				}
			}
		}

		private void ClearAllPointHovers()
		{
			if (_pointManager == null) return;

			var points = _pointManager.GetPoints();
			foreach (var pointData in points)
			{
				var pointHandle = _pointManager.GetPoint(pointData.Id);
				if (pointHandle != null)
				{
					UpdatePointVisualState(pointHandle, false);
				}
			}
		}

		private Transform GetRightControllerTransform()
		{
			// Try to find the right controller transform from the existing ray depth controller
			var rayDepthController = UnityEngine.Object.FindFirstObjectByType<RayDepthController>();
			if (rayDepthController != null)
			{
				// Use reflection to access the private field
				var field = typeof(RayDepthController).GetField("_rightControllerTransform", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				if (field != null)
				{
					return field.GetValue(rayDepthController) as Transform;
				}
			}

			// Fallback: try to find by tag or name
			var rightController = GameObject.FindWithTag("RightController");
			if (rightController == null)
			{
				rightController = GameObject.Find("RightHand Controller");
			}

			return rightController?.transform;
		}

		private bool SegmentBlockedBetween(int fromPointId, int toPointId)
		{
			if (_pointManager == null) return false;
			if (fromPointId <= 0 || toPointId <= 0 || fromPointId == toPointId) return false;

			var fromHandle = _pointManager.GetPoint(fromPointId);
			var toHandle = _pointManager.GetPoint(toPointId);
			if (fromHandle == null || toHandle == null) return false;

			Vector3 start = fromHandle.transform.position;
			Vector3 end = toHandle.transform.position;
			if (Vector3.Distance(start, end) < 0.01f) return false;

			float radius = Mathf.Max(0.01f, _pointManager.DroneRadius);
			LayerMask mask = _pointManager.EnvironmentLayerMask;

			// Trim a little off the capsule so barely touching the surface isn't counted as a violation
			Vector3 direction = (end - start);
			float distance = direction.magnitude;
			Vector3 offset = distance > 0.001f ? direction.normalized * Mathf.Min(0.05f, distance * 0.25f) : Vector3.zero;
			Vector3 capsuleStart = start + offset;
			Vector3 capsuleEnd = end - offset;

			if (Vector3.Distance(capsuleStart, capsuleEnd) < 0.005f)
			{
				capsuleStart = start;
				capsuleEnd = end;
			}

			return Physics.CheckCapsule(capsuleStart, capsuleEnd, radius, mask, QueryTriggerInteraction.Ignore);
		}

		private static int GetLastRealPointId(FlightPath route)
		{
			if (route == null) return -1;
			for (int i = route.PointIds.Count - 1; i >= 0; i--)
			{
				int id = route.PointIds[i];
				if (id > 0) return id;
			}

			return -1;
		}

		private void ProvideHapticFeedback(float amplitude, float duration)
		{
			if (_pointManager != null)
			{
				_pointManager.TickHaptics(amplitude, duration);
			}
		}

		private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
		{
			if (!device.isValid) return false;
			bool value;
			return device.TryGetFeatureValue(usage, out value) && value;
		}

		private static bool EdgePressed(bool current, ref bool prev)
		{
			bool pressed = current && !prev;
			prev = current;
			return pressed;
		}

		private static InputDevice EnsureDevice(InputDevice device, XRNode node)
		{
			if (!device.isValid)
			{
				device = InputDevices.GetDeviceAtXRNode(node);
			}
			return device;
		}

		/// <summary>
		/// Get the current path building status for UI display.
		/// </summary>
		public string GetPathModeStatus()
		{
			if (_pathManager == null) return "Path Manager Not Found";

			if (!_pathManager.PathModeEnabled)
			{
				return "Path Mode: OFF";
			}

			var activeRoute = _pathManager.ActiveRoute;
			if (activeRoute == null)
			{
				return "Path Mode: ON - Click a point to start";
			}

			return $"Path Mode: ON - {activeRoute.RouteName} ({activeRoute.PointCount} points)";
		}

		/// <summary>
		/// Get instructions for current path building state.
		/// </summary>
		public string GetPathModeInstructions()
		{
			if (_pathManager == null || !_pathManager.PathModeEnabled)
			{
				return "Press Right Grip to enter Path Mode";
			}

			var activeRoute = _pathManager.ActiveRoute;
			if (activeRoute == null)
			{
				return "Click a point to start a new route";
			}

			if (activeRoute.PointCount < 2)
			{
				return "Click another point to continue the route";
			}

			return "Click points to extend route, B to undo";
		}
	}
}
