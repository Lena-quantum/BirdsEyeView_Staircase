using UnityEngine;

namespace Points
{
	/// <summary>
	/// Helper script to set up the Flight Path Builder system in a scene.
	/// Attach this to a GameObject and configure it to automatically set up all required components.
	/// </summary>
	public class FlightPathSetup : MonoBehaviour
	{
		[Header("Setup Configuration")]
		[SerializeField] private bool _autoSetupOnStart = true;
		[SerializeField] private bool _createPathManager = true;
		[SerializeField] private bool _createPathRenderer = true;
		[SerializeField] private bool _createPathModeController = true;
		[SerializeField] private bool _createRouteUI = true;

		[Header("Component References")]
		[SerializeField] private PointPlacementManager _pointManager;
		[SerializeField] private FlightPathManager _pathManager;
		[SerializeField] private PathRenderer _pathRenderer;
		[SerializeField] private PathModeController _pathModeController;
		[SerializeField] private RouteUI _routeUI;

		[Header("UI Setup")]
		[SerializeField] private bool _createUIPrefab = true;
		[SerializeField] private GameObject _uiPrefab;

		private void Start()
		{
			if (_autoSetupOnStart)
			{
				SetupFlightPathSystem();
			}
		}

		/// <summary>
		/// Set up the complete Flight Path Builder system.
		/// </summary>
		[ContextMenu("Setup Flight Path System")]
		public void SetupFlightPathSystem()
		{
			Debug.Log("Setting up Flight Path Builder system...");

			// Find or create PointPlacementManager
			if (_pointManager == null)
			{
				_pointManager = UnityEngine.Object.FindFirstObjectByType<PointPlacementManager>();
				if (_pointManager == null)
				{
					Debug.LogWarning("No PointPlacementManager found! Please ensure you have a PointPlacementManager in your scene.");
					return;
				}
			}

			// Create FlightPathManager
			if (_createPathManager && _pathManager == null)
			{
				_pathManager = gameObject.GetComponent<FlightPathManager>();
				if (_pathManager == null)
				{
					_pathManager = gameObject.AddComponent<FlightPathManager>();
				}
				Debug.Log("Created FlightPathManager");
			}

			// Create PathRenderer
			if (_createPathRenderer && _pathRenderer == null)
			{
				_pathRenderer = gameObject.GetComponent<PathRenderer>();
				if (_pathRenderer == null)
				{
					_pathRenderer = gameObject.AddComponent<PathRenderer>();
				}
				Debug.Log("Created PathRenderer");
			}

			// Create PathModeController
			if (_createPathModeController && _pathModeController == null)
			{
				_pathModeController = gameObject.GetComponent<PathModeController>();
				if (_pathModeController == null)
				{
					_pathModeController = gameObject.AddComponent<PathModeController>();
				}
				Debug.Log("Created PathModeController");
			}

			// Create RouteUI
			if (_createRouteUI && _routeUI == null)
			{
				CreateRouteUI();
			}

			// Connect components
			ConnectComponents();

			Debug.Log("Flight Path Builder system setup complete!");
		}

		private void CreateRouteUI()
		{
			if (_createUIPrefab && _uiPrefab != null)
			{
				// Instantiate UI prefab
				var uiObject = Instantiate(_uiPrefab);
				_routeUI = uiObject.GetComponent<RouteUI>();
				Debug.Log("Created RouteUI from prefab");
			}
			else
			{
				// Create basic UI programmatically
				CreateBasicRouteUI();
			}
		}

		private void CreateBasicRouteUI()
		{
			// Create UI GameObject
			var uiObject = new GameObject("Route UI");
			uiObject.transform.SetParent(transform);

			// Add Canvas
			var canvas = uiObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;

			// Add CanvasScaler
			var scaler = uiObject.AddComponent<UnityEngine.UI.CanvasScaler>();
			scaler.dynamicPixelsPerUnit = 10f;

			// Add GraphicRaycaster
			uiObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

			// Create EventSystem if needed
			if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
			{
				var eventSystemObject = new GameObject("EventSystem");
				eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
				eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
			}

			// Add RouteUI component
			_routeUI = uiObject.AddComponent<RouteUI>();

			Debug.Log("Created basic RouteUI");
		}

		private void ConnectComponents()
		{
			// Connect FlightPathManager
			if (_pathManager != null)
			{
				// Set PointPlacementManager reference
				var pathManagerField = typeof(FlightPathManager).GetField("_pointManager", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				pathManagerField?.SetValue(_pathManager, _pointManager);

				// Set PathRenderer reference
				var pathRendererField = typeof(FlightPathManager).GetField("_pathRenderer", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				pathRendererField?.SetValue(_pathManager, _pathRenderer);
			}

			// Connect PathModeController
			if (_pathModeController != null)
			{
				var pathManagerField = typeof(PathModeController).GetField("_pathManager", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				pathManagerField?.SetValue(_pathModeController, _pathManager);

				var pointManagerField = typeof(PathModeController).GetField("_pointManager", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				pointManagerField?.SetValue(_pathModeController, _pointManager);
			}

			// Connect PathRenderer
			if (_pathRenderer != null && _pathManager != null)
			{
				_pathRenderer.Initialize(_pathManager);
			}
		}

		/// <summary>
		/// Validate that all required components are properly set up.
		/// </summary>
		[ContextMenu("Validate Setup")]
		public bool ValidateSetup()
		{
			bool isValid = true;

			if (_pointManager == null)
			{
				Debug.LogError("PointPlacementManager is missing!");
				isValid = false;
			}

			if (_pathManager == null)
			{
				Debug.LogError("FlightPathManager is missing!");
				isValid = false;
			}

			if (_pathRenderer == null)
			{
				Debug.LogError("PathRenderer is missing!");
				isValid = false;
			}

			if (_pathModeController == null)
			{
				Debug.LogError("PathModeController is missing!");
				isValid = false;
			}

			if (_routeUI == null)
			{
				Debug.LogWarning("RouteUI is missing - UI functionality will not be available.");
			}

			if (isValid)
			{
				Debug.Log("Flight Path Builder setup validation passed!");
			}
			else
			{
				Debug.LogError("Flight Path Builder setup validation failed!");
			}

			return isValid;
		}

		/// <summary>
		/// Get setup instructions for the user.
		/// </summary>
		public string GetSetupInstructions()
		{
			return @"Flight Path Builder Setup Instructions:

1. Ensure you have a PointPlacementManager in your scene
2. Attach this FlightPathSetup script to a GameObject
3. Click 'Setup Flight Path System' in the inspector or run it at start
4. Validate the setup using 'Validate Setup'
5. Test the system:
   - Place some points using the existing point placement system
   - Press Menu button to toggle Path Mode
   - Click existing points to build routes
   - Use B button to undo, Grip to finish routes

Controls:
- Menu Button: Toggle Path Mode
- Trigger (Path Mode): Add point to route
- B Button: Undo last point
- Grip: Finish current route

The system extends your existing point placement without breaking current functionality.";
		}
	}
}
