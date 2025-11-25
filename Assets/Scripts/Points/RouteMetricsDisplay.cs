using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Points
{
	/// <summary>
	/// Displays real-time route metrics for thesis data collection.
	/// Shows distance, estimated time, and waypoint type breakdown.
	/// </summary>
	public class RouteMetricsDisplay : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private FlightPathManager _pathManager;
		[SerializeField] private PointPlacementManager _pointManager;
		[SerializeField] private Canvas _canvas;

		[Header("UI Elements")]
		[SerializeField] private Text _headerText;
		[SerializeField] private Text _distanceText;
		[SerializeField] private Text _timeText;
		[SerializeField] private Text _waypointsText;
		[SerializeField] private Text _typeBreakdownText;

		[Header("Settings")]
		[SerializeField] private float _defaultDroneSpeed = 1.0f; // m/s
		[SerializeField] private Vector3 _displayOffset = new Vector3(0.5f, 0.3f, 1.0f); // Offset from camera
		[SerializeField] private float _uiScale = 0.001f;
		[SerializeField] private bool _onlyShowInPathMode = true;

		private Camera _mainCamera;
		private float _lastUpdateTime = 0f;
		private float _updateInterval = 0.1f; // Update every 100ms

		private void Awake()
		{
			_mainCamera = Camera.main;

			// Find references if not set
			if (_pathManager == null)
			{
				_pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
			}

			if (_pointManager == null)
			{
				_pointManager = UnityEngine.Object.FindFirstObjectByType<PointPlacementManager>();
			}

			// Create UI if not already set up
			if (_canvas == null)
			{
				CreateUI();
			}
		}

		private void Start()
		{
			// Subscribe to path manager events
			if (_pathManager != null)
			{
				_pathManager.OnPathModeChanged += HandlePathModeChanged;
				_pathManager.OnPointAddedToRoute += HandlePointAddedToRoute;
				_pathManager.OnRouteFinished += HandleRouteFinished;
			}

			UpdateDisplay();
		}

		private void OnDestroy()
		{
			if (_pathManager != null)
			{
				_pathManager.OnPathModeChanged -= HandlePathModeChanged;
				_pathManager.OnPointAddedToRoute -= HandlePointAddedToRoute;
				_pathManager.OnRouteFinished -= HandleRouteFinished;
			}
		}

		private void Update()
		{
			// Position UI relative to camera
			UpdateDisplayPosition();

			// Update metrics periodically
			if (Time.time - _lastUpdateTime > _updateInterval)
			{
				UpdateDisplay();
				_lastUpdateTime = Time.time;
			}
		}

		private void UpdateDisplayPosition()
		{
			if (_mainCamera == null) return;

			// Position in front and slightly to the right/up of camera
			transform.position = _mainCamera.transform.position + _mainCamera.transform.TransformDirection(_displayOffset);
			
			// Face camera
			transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);
		}

		private void HandlePathModeChanged(bool pathModeEnabled)
		{
			if (_onlyShowInPathMode)
			{
				SetDisplayVisible(pathModeEnabled);
			}
			UpdateDisplay();
		}

		private void HandlePointAddedToRoute(FlightPath route, int pointCount)
		{
			UpdateDisplay();
		}

		private void HandleRouteFinished(FlightPath route)
		{
			UpdateDisplay();
		}

		private void SetDisplayVisible(bool visible)
		{
			if (_canvas != null)
			{
				_canvas.gameObject.SetActive(visible);
			}
		}

		private void CreateUI()
		{
			// Create canvas
			GameObject canvasObj = new GameObject("Route Metrics Canvas");
			canvasObj.transform.SetParent(transform);
			canvasObj.transform.localPosition = Vector3.zero;
			canvasObj.transform.localRotation = Quaternion.identity;

			_canvas = canvasObj.AddComponent<Canvas>();
			_canvas.renderMode = RenderMode.WorldSpace;
			_canvas.worldCamera = _mainCamera;

			// Scale for VR
			canvasObj.transform.localScale = Vector3.one * _uiScale;

			// Add CanvasScaler
			var scaler = canvasObj.AddComponent<CanvasScaler>();
			scaler.dynamicPixelsPerUnit = 10f;

			// Add GraphicRaycaster (not needed for display-only, but good practice)
			canvasObj.AddComponent<GraphicRaycaster>();

			// Create UI layout
			CreateUILayout();
		}

		private void CreateUILayout()
		{
			if (_canvas == null) return;

			// Create background panel
			GameObject panelObj = new GameObject("Panel");
			panelObj.transform.SetParent(_canvas.transform);
			RectTransform panelRect = panelObj.AddComponent<RectTransform>();
			panelRect.sizeDelta = new Vector2(400, 300);
			panelRect.anchoredPosition = Vector2.zero;

			Image panelImage = panelObj.AddComponent<Image>();
			panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

			// Create text elements
			CreateTextElements(panelObj.transform);
		}

		private void CreateTextElements(Transform parent)
		{
			Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			float startY = 120f;
			float lineHeight = 35f;

			// Header
			_headerText = CreateText(parent, "ROUTE STATISTICS", new Vector2(0, startY), 22, Color.cyan, defaultFont);
			_headerText.fontStyle = FontStyle.Bold;

			// Distance
			_distanceText = CreateText(parent, "Distance: 0.00 m", new Vector2(0, startY - lineHeight * 1), 18, Color.white, defaultFont);

			// Time
			_timeText = CreateText(parent, "Est. Time: 0.0 s", new Vector2(0, startY - lineHeight * 2), 18, Color.white, defaultFont);

			// Waypoints
			_waypointsText = CreateText(parent, "Waypoints: 0", new Vector2(0, startY - lineHeight * 3), 18, Color.white, defaultFont);

			// Type breakdown
			_typeBreakdownText = CreateText(parent, "", new Vector2(0, startY - lineHeight * 4), 14, Color.gray, defaultFont);
			_typeBreakdownText.alignment = TextAnchor.UpperCenter;
		}

		private Text CreateText(Transform parent, string content, Vector2 position, int fontSize, Color color, Font font)
		{
			GameObject textObj = new GameObject("Text_" + content.Substring(0, Mathf.Min(10, content.Length)));
			textObj.transform.SetParent(parent);
			RectTransform textRect = textObj.AddComponent<RectTransform>();
			textRect.sizeDelta = new Vector2(380, fontSize + 10);
			textRect.anchoredPosition = position;

			Text text = textObj.AddComponent<Text>();
			text.text = content;
			text.font = font;
			text.fontSize = fontSize;
			text.alignment = TextAnchor.MiddleCenter;
			text.color = color;

			return text;
		}

		/// <summary>
		/// Update all displayed metrics.
		/// </summary>
		public void UpdateDisplay()
		{
			if (_pathManager == null || _pointManager == null) return;

			var activeRoute = _pathManager.ActiveRoute;
			
			if (activeRoute == null || activeRoute.IsEmpty)
			{
				DisplayEmptyState();
				return;
			}

			// Calculate metrics
			float distance = CalculateRouteDistance(activeRoute);
			float estimatedTime = EstimateFlightTime(activeRoute, distance);
			int waypointCount = activeRoute.PointCount;
			var typeBreakdown = GetWaypointTypeBreakdown(activeRoute);

			// Update UI
			if (_distanceText != null)
			{
				_distanceText.text = $"Distance: {distance:F2} m";
			}

			if (_timeText != null)
			{
				_timeText.text = $"Est. Time: {estimatedTime:F1} s";
			}

			if (_waypointsText != null)
			{
				_waypointsText.text = $"Waypoints: {waypointCount}";
			}

			if (_typeBreakdownText != null)
			{
				_typeBreakdownText.text = FormatTypeBreakdown(typeBreakdown);
			}
		}

		private void DisplayEmptyState()
		{
			if (_distanceText != null) _distanceText.text = "Distance: 0.00 m";
			if (_timeText != null) _timeText.text = "Est. Time: 0.0 s";
			if (_waypointsText != null) _waypointsText.text = "Waypoints: 0";
			if (_typeBreakdownText != null) _typeBreakdownText.text = "No route active";
		}

		/// <summary>
		/// Calculate total distance of route by summing segment lengths.
		/// </summary>
		private float CalculateRouteDistance(FlightPath route)
		{
			if (route == null || route.PointCount < 2) return 0f;

			var positions = route.GetWorldPositions(_pointManager);
			if (positions.Count < 2) return 0f;

			float totalDistance = 0f;
			for (int i = 0; i < positions.Count - 1; i++)
			{
				totalDistance += Vector3.Distance(positions[i], positions[i + 1]);
			}

			// Add closing segment if route is closed
			if (route.IsClosed && positions.Count >= 3)
			{
				totalDistance += Vector3.Distance(positions[positions.Count - 1], positions[0]);
			}

			return totalDistance;
		}

		/// <summary>
		/// Estimate flight time based on distance and waypoint types.
		/// </summary>
		private float EstimateFlightTime(FlightPath route, float distance)
		{
			if (route == null || route.IsEmpty) return 0f;

			// Base flight time = distance / speed
			float flightTime = distance / _defaultDroneSpeed;

			// Add time for waypoint-specific behaviors
			foreach (int pointId in route.PointIds)
			{
				var pointData = _pointManager.GetPointData(pointId);
				if (!pointData.HasValue) continue;

			// Add delays for specific waypoint types (indoor flight optimized)
			switch (pointData.Value.Type)
			{
				case WaypointType.StopTurnGo:
					// Use hold time from waypoint data
					flightTime += pointData.Value.HoldTime > 0 ? pointData.Value.HoldTime : 2.0f;
					break;

				case WaypointType.Record360:
					if (pointData.Value.Parameters != null && pointData.Value.Parameters.ContainsKey("duration_s"))
					{
						flightTime += (float)pointData.Value.Parameters["duration_s"];
					}
					else
					{
						flightTime += 15.0f; // Default 15 seconds for 360° rotation
					}
					break;
			}
			}

			return flightTime;
		}

		/// <summary>
		/// Get count of each waypoint type in the route.
		/// </summary>
		private System.Collections.Generic.Dictionary<WaypointType, int> GetWaypointTypeBreakdown(FlightPath route)
		{
			var breakdown = new System.Collections.Generic.Dictionary<WaypointType, int>();
			
			// Initialize all types to 0
			foreach (WaypointType type in System.Enum.GetValues(typeof(WaypointType)))
			{
				breakdown[type] = 0;
			}

			if (route == null || route.IsEmpty) return breakdown;

			// Count each type
			foreach (int pointId in route.PointIds)
			{
				var pointData = _pointManager.GetPointData(pointId);
				if (pointData.HasValue)
				{
					breakdown[pointData.Value.Type]++;
				}
			}

			return breakdown;
		}

		/// <summary>
		/// Format type breakdown for display.
		/// </summary>
		private string FormatTypeBreakdown(System.Collections.Generic.Dictionary<WaypointType, int> breakdown)
		{
			var nonZeroTypes = breakdown.Where(kvp => kvp.Value > 0).ToList();
			
			if (nonZeroTypes.Count == 0)
			{
				return "No waypoints";
			}

			var lines = new System.Text.StringBuilder();
			foreach (var kvp in nonZeroTypes)
			{
				string typeName = WaypointTypeDefinition.GetTypeName(kvp.Key);
				lines.AppendLine($"{typeName}: {kvp.Value}");
			}

			return lines.ToString().TrimEnd();
		}

		/// <summary>
		/// Set the default drone speed for time estimation.
		/// </summary>
		public void SetDroneSpeed(float speedMps)
		{
			_defaultDroneSpeed = Mathf.Max(0.1f, speedMps);
			UpdateDisplay();
		}

		/// <summary>
		/// Get the current route metrics as a data structure.
		/// </summary>
		public RouteMetrics GetCurrentMetrics()
		{
			if (_pathManager == null || _pointManager == null)
			{
				return new RouteMetrics();
			}

			var activeRoute = _pathManager.ActiveRoute;
			if (activeRoute == null || activeRoute.IsEmpty)
			{
				return new RouteMetrics();
			}

			float distance = CalculateRouteDistance(activeRoute);
			float time = EstimateFlightTime(activeRoute, distance);
			var breakdown = GetWaypointTypeBreakdown(activeRoute);

			return new RouteMetrics
			{
				TotalDistance = distance,
				EstimatedTime = time,
				WaypointCount = activeRoute.PointCount,
				TypeBreakdown = breakdown
			};
		}
	}

	/// <summary>
	/// Data structure for route metrics.
	/// </summary>
	[System.Serializable]
	public struct RouteMetrics
	{
		public float TotalDistance;
		public float EstimatedTime;
		public int WaypointCount;
		public System.Collections.Generic.Dictionary<WaypointType, int> TypeBreakdown;
	}
}

