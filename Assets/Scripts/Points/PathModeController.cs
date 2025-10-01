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
		[SerializeField] private LayerMask _pointLayerMask = -1;

		[Header("Input Settings")]
		[SerializeField] private float _hapticAmplitude = 0.3f;
		[SerializeField] private float _hapticDuration = 0.05f;

		private InputDevice _rightHand;
		private InputDevice _leftHand;
		private bool _rightGripPrev;
		private bool _bButtonPrev;
		private bool _triggerPrev;

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

			// DISABLED: Grip for finishing route (now used for path mode toggle)
			// bool gripButton = ReadButton(_rightHand, CommonUsages.gripButton);
			// if (EdgePressed(gripButton, ref _gripButtonPrev))
			// {
			// 	FinishCurrentRoute();
			// }

			// Handle point hovering for visual feedback
			HandlePointHovering();
		}

		private void HandleTriggerInput()
		{
			// Raycast to find point under the right controller
			if (_pointManager?.PointsParent == null) return;

			var rightControllerTransform = GetRightControllerTransform();
			if (rightControllerTransform == null) return;

			Vector3 origin = rightControllerTransform.position;
			Vector3 direction = rightControllerTransform.forward;

			// First try to hit a point handle
			if (Physics.Raycast(origin, direction, out RaycastHit hit, 10f, _pointLayerMask))
			{
				var pointHandle = hit.collider.GetComponent<PointHandle>();
				if (pointHandle != null)
				{
					// Add point to current route
					AddPointToRoute(pointHandle);
					return;
				}
			}

			// If no point hit, start a new route if none exists
			if (_pathManager.ActiveRoute == null)
			{
				_pathManager.StartNewRoute();
				ProvideHapticFeedback(_hapticAmplitude, _hapticDuration * 2f);
			}
		}

		private void AddPointToRoute(PointHandle pointHandle)
		{
			if (pointHandle == null || _pathManager == null) return;

			// Check if point is already in the current route
			var activeRoute = _pathManager.ActiveRoute;
			if (activeRoute != null && activeRoute.ContainsPoint(pointHandle.Id))
			{
				// If clicking on the first point of a valid route, close the loop
				if (activeRoute.PointCount >= 3 && activeRoute.PointIds[0] == pointHandle.Id)
				{
					FinishCurrentRoute(true); // Close the loop
					return;
				}

				// Otherwise, ignore duplicate points
				return;
			}

			// Start new route if none exists
			if (activeRoute == null)
			{
				_pathManager.StartNewRoute();
			}

			// Add the point to the current route
			activeRoute.AddPoint(pointHandle.Id);

			ProvideHapticFeedback(_hapticAmplitude, _hapticDuration);
		}

		private void FinishCurrentRoute(bool closeLoop = false)
		{
			if (_pathManager?.ActiveRoute == null) return;

			_pathManager.FinishCurrentRoute(closeLoop);
			ProvideHapticFeedback(_hapticAmplitude * 1.5f, _hapticDuration * 2f);
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
				else if (isInActiveRoute)
				{
					targetColor = activeRoute.PathColor; // Route color when in active route
				}
				else if (_pathManager.IsPointInAnyRoute(pointHandle.Id))
				{
					targetColor = Color.gray; // Gray when in other routes
				}
				else
				{
					targetColor = _pointManager.PlacedPointColor; // Default color
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
