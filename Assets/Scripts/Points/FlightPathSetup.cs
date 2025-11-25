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
		[SerializeField] private bool _createRouteMetrics = false; // Disabled - can enable if needed
		[SerializeField] private bool _ensureSimpleFloor = true;
		[SerializeField] private Vector2 _floorSize = new Vector2(40f, 40f);
		[SerializeField] private float _floorHeight = 0f;
		[SerializeField] private Material _floorMaterial;
		[SerializeField] private bool _assignFloorLayer = true;
		[SerializeField] private string _floorLayerName = "Environment";

		[Header("Component References")]
		[SerializeField] private PointPlacementManager _pointManager;
		[SerializeField] private FlightPathManager _pathManager;
		[SerializeField] private PathRenderer _pathRenderer;
		[SerializeField] private PathModeController _pathModeController;
		[SerializeField] private RouteMetricsDisplay _routeMetrics;

		private GameObject _generatedFloor;
		private const string GeneratedFloorName = "Generated Flight Path Floor";

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

			// Thesis Feature: Create RouteMetricsDisplay (optional)
			if (_createRouteMetrics && _routeMetrics == null)
			{
				CreateRouteMetricsDisplay();
			}

			// Connect components
			ConnectComponents();

			// Optionally ensure a simple floor exists in the scene
			EnsureSimpleFloor();

			Debug.Log("Flight Path Builder system setup complete!");
		}

		private void CreateRouteMetricsDisplay()
		{
			var metricsObj = new GameObject("Route Metrics Display");
			_routeMetrics = metricsObj.AddComponent<RouteMetricsDisplay>();
			Debug.Log("Created RouteMetricsDisplay");
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

			// Note: Wrist menu is manually created by user (WristUICanvas in scene)
			// RouteMetricsDisplay is optional

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

		private void EnsureSimpleFloor()
		{
			if (!_ensureSimpleFloor)
			{
				return;
			}

			if (_generatedFloor == null)
			{
				_generatedFloor = GameObject.Find(GeneratedFloorName);
			}

			if (_generatedFloor == null)
			{
				_generatedFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
				_generatedFloor.name = GeneratedFloorName;
			}

			if (_generatedFloor == null)
			{
				Debug.LogWarning("FlightPathSetup: Unable to create simple floor.");
				return;
			}

			_generatedFloor.transform.position = new Vector3(0f, _floorHeight, 0f);

			float width = Mathf.Max(1f, _floorSize.x);
			float depth = Mathf.Max(1f, _floorSize.y);
			// Unity plane primitive is 10x10 units by default
			_generatedFloor.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);

			var renderer = _generatedFloor.GetComponent<Renderer>();
			if (renderer != null && _floorMaterial != null)
			{
				renderer.sharedMaterial = _floorMaterial;
			}

			if (_assignFloorLayer && !string.IsNullOrWhiteSpace(_floorLayerName))
			{
				int layer = LayerMask.NameToLayer(_floorLayerName);
				if (layer >= 0)
				{
					_generatedFloor.layer = layer;
				}
			}
		}

		/// <summary>
		/// Get setup instructions for the user.
		/// </summary>
		public string GetSetupInstructions()
		{
			return @"VR Drone Flight Path Planning - Thesis Setup Instructions:

1. Ensure you have a PointPlacementManager in your scene
2. Ensure you have WristUICanvas with 3 buttons for waypoint type selection
3. Attach this FlightPathSetup script to a GameObject
4. Click 'Setup Flight Path System' in the inspector
5. Connect your buttons to call PointPlacementManager.CurrentTypeSelection
6. COLLISION AVOIDANCE: Create an 'Environment' layer and assign it to all obstacles/walls
   - Go to Layers dropdown (top right) > Add Layer > Create 'Environment' layer
   - Select all photogrammetric models/walls and set Layer to 'Environment'
   - In PointPlacementManager inspector, set '_environmentLayer' to 'Environment'
   - Drone radius is set to 0.5m (50cm safety buffer) by default

Controls:
- A/B Buttons: Adjust placement depth
- Trigger: Place waypoint / Add to route (in Path Mode)
- Right Grip: Toggle Path Mode
- B Button (Path Mode): Undo last waypoint
- Left Trigger: Remove pointed waypoint

Waypoint Types (2 types for indoor flight study):
- Stop-Turn-Go (Green 38FF00): Drone stops, rotates to observe, then continues (standard waypoint)
- Record 360° (Red C21807): Drone stops and rotates slowly 360° for recording (two-point system)

Collision Avoidance (NEW):
- Ghost sphere turns GREY when too close to obstacles (<50cm)
- Red semi-transparent SHELL appears matching the obstacle's shape
- Shell is inflated by drone radius (50cm buffer) showing the no-fly zone boundary
- For walls: flat plane 50cm in front of wall
- For boxes/meshes: inflated transparent copy of the obstacle shape
- Placement is BLOCKED until ghost moves to safe zone
- Red shell fades in/out smoothly as you approach/leave collision zone";
		}
	}
}
