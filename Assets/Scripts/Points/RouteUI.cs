using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Points
{
	/// <summary>
	/// Simple UI for route management in VR. Can be attached to wrist or overlay.
	/// </summary>
	public class RouteUI : MonoBehaviour
	{
		[Header("UI References")]
		[SerializeField] private Canvas _canvas;
		[SerializeField] private Text _statusText;
		[SerializeField] private Text _instructionsText;
		[SerializeField] private Button _togglePathModeButton;
		[SerializeField] private Button _newRouteButton;
		[SerializeField] private Button _finishRouteButton;
		[SerializeField] private Button _undoButton;
		[SerializeField] private Button _clearAllButton;
		[SerializeField] private Dropdown _routeDropdown;
		[SerializeField] private Toggle _smoothPathToggle;
		[SerializeField] private Toggle _showArrowsToggle;

		[Header("UI Settings")]
		[SerializeField] private bool _showOnWrist = true;
		[SerializeField] private float _uiScale = 0.002f;
		[SerializeField] private Vector3 _wristOffset = new Vector3(0, 0, 0.05f);

		private FlightPathManager _pathManager;
		private PathRenderer _pathRenderer;
		private InputDevice _rightHand;
		private bool _menuButtonPrev;
		private bool _uiVisible = false;

		/// <summary>
		/// Whether the UI is currently visible.
		/// </summary>
		public bool IsUIVisible => _uiVisible;

		private void Awake()
		{
			if (_canvas == null)
			{
				_canvas = GetComponentInChildren<Canvas>();
			}

			SetupUI();
		}

		private void Start()
		{
			_pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
			_pathRenderer = UnityEngine.Object.FindFirstObjectByType<PathRenderer>();
			_rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

			SetupEventListeners();
			UpdateUI();
		}

		private void Update()
		{
			_rightHand = EnsureDevice(_rightHand, XRNode.RightHand);

			// Toggle UI with menu button
			bool menuButton = ReadButton(_rightHand, CommonUsages.menuButton);
			if (EdgePressed(menuButton, ref _menuButtonPrev))
			{
				ToggleUI();
			}

			// Position UI on wrist if enabled
			if (_uiVisible && _showOnWrist)
			{
				PositionUIOnWrist();
			}
		}

		/// <summary>
		/// Toggle the UI visibility.
		/// </summary>
		public void ToggleUI()
		{
			_uiVisible = !_uiVisible;
			_canvas.gameObject.SetActive(_uiVisible);
			
			if (_uiVisible)
			{
				UpdateUI();
			}
		}

		/// <summary>
		/// Show the UI.
		/// </summary>
		public void ShowUI()
		{
			_uiVisible = true;
			_canvas.gameObject.SetActive(true);
			UpdateUI();
		}

		/// <summary>
		/// Hide the UI.
		/// </summary>
		public void HideUI()
		{
			_uiVisible = false;
			_canvas.gameObject.SetActive(false);
		}

		private void SetupUI()
		{
			if (_canvas == null) return;

			// Configure canvas for VR
			_canvas.renderMode = RenderMode.WorldSpace;
			_canvas.worldCamera = Camera.main;
			_canvas.transform.localScale = Vector3.one * _uiScale;

			// Initially hide UI
			_canvas.gameObject.SetActive(false);
		}

		private void SetupEventListeners()
		{
			if (_pathManager != null)
			{
				_pathManager.OnPathModeChanged += OnPathModeChanged;
				_pathManager.OnActiveRouteChanged += OnActiveRouteChanged;
				_pathManager.OnRouteFinished += OnRouteFinished;
			}

			// Button event listeners
			if (_togglePathModeButton != null)
				_togglePathModeButton.onClick.AddListener(TogglePathMode);

			if (_newRouteButton != null)
				_newRouteButton.onClick.AddListener(StartNewRoute);

			if (_finishRouteButton != null)
				_finishRouteButton.onClick.AddListener(FinishRoute);

			if (_undoButton != null)
				_undoButton.onClick.AddListener(UndoLastPoint);

			if (_clearAllButton != null)
				_clearAllButton.onClick.AddListener(ClearAllRoutes);

			// Toggle event listeners
			if (_smoothPathToggle != null)
				_smoothPathToggle.onValueChanged.AddListener(OnSmoothPathToggled);

			if (_showArrowsToggle != null)
				_showArrowsToggle.onValueChanged.AddListener(OnShowArrowsToggled);

			// Dropdown event listener
			if (_routeDropdown != null)
				_routeDropdown.onValueChanged.AddListener(OnRouteSelected);
		}

		private void UpdateUI()
		{
			if (_pathManager == null) return;

			UpdateStatusText();
			UpdateInstructionsText();
			UpdateButtonStates();
			UpdateRouteDropdown();
			UpdateToggles();
		}

		private void UpdateStatusText()
		{
			if (_statusText == null) return;

			string status = _pathManager.PathModeEnabled ? "Path Mode: ON" : "Path Mode: OFF";
			var activeRoute = _pathManager.GetActiveRoute();
			if (activeRoute != null)
			{
				status += $" - {activeRoute.RouteName} ({activeRoute.PointCount} points)";
			}
			status += $" - Total Routes: {_pathManager.RouteCount}";

			_statusText.text = status;
		}

		private void UpdateInstructionsText()
		{
			if (_instructionsText == null) return;

			string instructions;
			if (!_pathManager.PathModeEnabled)
			{
				instructions = "Click 'Toggle Path Mode' to start building routes";
			}
			else if (_pathManager.GetActiveRoute() == null)
			{
				instructions = "Click 'New Route' or click a point to start";
			}
			else
			{
				instructions = "Click points to add to route, 'Finish Route' when done";
			}

			_instructionsText.text = instructions;
		}

		private void UpdateButtonStates()
		{
			bool pathModeEnabled = _pathManager.PathModeEnabled;
			var activeRoute = _pathManager.GetActiveRoute();

			if (_togglePathModeButton != null)
				_togglePathModeButton.interactable = true;

			if (_newRouteButton != null)
				_newRouteButton.interactable = pathModeEnabled;

			if (_finishRouteButton != null)
				_finishRouteButton.interactable = pathModeEnabled && activeRoute != null && activeRoute.PointCount >= 2;

			if (_undoButton != null)
				_undoButton.interactable = pathModeEnabled && activeRoute != null && activeRoute.PointCount > 0;

			if (_clearAllButton != null)
				_clearAllButton.interactable = _pathManager.RouteCount > 0;
		}

		private void UpdateRouteDropdown()
		{
			if (_routeDropdown == null) return;

			var routes = _pathManager.GetAllRoutes();
			var routeNames = new List<string> { "None" };
			
			foreach (var route in routes)
			{
				routeNames.Add(route.RouteName);
			}

			_routeDropdown.ClearOptions();
			_routeDropdown.AddOptions(routeNames);

			// Select active route
			var activeRoute = _pathManager.GetActiveRoute();
			if (activeRoute != null)
			{
				int index = routeNames.IndexOf(activeRoute.RouteName);
				if (index >= 0)
				{
					_routeDropdown.value = index;
				}
			}
		}

		private void UpdateToggles()
		{
			if (_pathRenderer == null) return;

			if (_smoothPathToggle != null)
				_smoothPathToggle.isOn = _pathRenderer.SmoothPath;

			if (_showArrowsToggle != null)
				_showArrowsToggle.isOn = _pathRenderer.ShowArrows;
		}

		private void PositionUIOnWrist()
		{
			var rightController = GameObject.FindWithTag("RightController");
			if (rightController != null)
			{
				transform.position = rightController.transform.position + _wristOffset;
				transform.rotation = rightController.transform.rotation;
			}
		}

		// Button event handlers
		private void TogglePathMode()
		{
			_pathManager?.TogglePathMode();
		}

		private void StartNewRoute()
		{
			_pathManager?.StartNewRoute();
		}

		private void FinishRoute()
		{
			_pathManager?.FinishCurrentRoute();
		}

		private void UndoLastPoint()
		{
			_pathManager?.UndoLastPoint();
		}

		private void ClearAllRoutes()
		{
			_pathManager?.ClearAllRoutes();
		}

		// Toggle event handlers
		private void OnSmoothPathToggled(bool smoothPath)
		{
			if (_pathRenderer != null)
			{
				_pathRenderer.SmoothPath = smoothPath;
			}
		}

		private void OnShowArrowsToggled(bool showArrows)
		{
			if (_pathRenderer != null)
			{
				_pathRenderer.ShowArrows = showArrows;
			}
		}

		// Dropdown event handler
		private void OnRouteSelected(int index)
		{
			if (_routeDropdown == null || _pathManager == null) return;

			var options = _routeDropdown.options;
			if (index >= 0 && index < options.Count)
			{
				string routeName = options[index].text;
				if (routeName == "None")
				{
					// Deselect active route
					_pathManager.SetActiveRoute(null);
				}
				else
				{
					_pathManager.SetActiveRoute(routeName);
				}
			}
		}

		// Event handlers
		private void OnPathModeChanged(bool pathModeEnabled)
		{
			UpdateUI();
		}

		private void OnActiveRouteChanged(FlightPath activeRoute)
		{
			UpdateUI();
		}

		private void OnRouteFinished(FlightPath finishedRoute)
		{
			UpdateUI();
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

		private void OnDestroy()
		{
			if (_pathManager != null)
			{
				_pathManager.OnPathModeChanged -= OnPathModeChanged;
				_pathManager.OnActiveRouteChanged -= OnActiveRouteChanged;
				_pathManager.OnRouteFinished -= OnRouteFinished;
			}
		}

		/// <summary>
		/// Get current route statistics for display.
		/// </summary>
		public RouteStatistics GetRouteStatistics()
		{
			if (_pathManager == null)
			{
				return new RouteStatistics();
			}

			var routes = _pathManager.GetAllRoutes();
			var activeRoute = _pathManager.GetActiveRoute();

			return new RouteStatistics
			{
				TotalRoutes = routes.Count,
				ActiveRouteName = activeRoute?.RouteName ?? "None",
				ActiveRoutePointCount = activeRoute?.PointCount ?? 0,
				PathModeEnabled = _pathManager.PathModeEnabled
			};
		}
	}

	/// <summary>
	/// Statistics about current route state for UI display.
	/// </summary>
	[System.Serializable]
	public class RouteStatistics
	{
		public int TotalRoutes;
		public string ActiveRouteName;
		public int ActiveRoutePointCount;
		public bool PathModeEnabled;

		public RouteStatistics()
		{
			TotalRoutes = 0;
			ActiveRouteName = "None";
			ActiveRoutePointCount = 0;
			PathModeEnabled = false;
		}
	}
}
