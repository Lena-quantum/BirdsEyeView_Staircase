using System.Collections.Generic;
using UnityEngine;

namespace Points
{
	/// <summary>
	/// Renders visual lines between points in flight paths with arrows and numbered badges.
	/// </summary>
	public class PathRenderer : MonoBehaviour
	{
		[Header("Path Line Settings")]
		[SerializeField] private Material _pathLineMaterial;
		[SerializeField] private float _lineWidth = 0.01f;
		[SerializeField] private bool _showArrows = true;
		[SerializeField] private float _arrowSpacing = 0.5f;
		[SerializeField] private bool _smoothPath = false;
		[SerializeField] private int _smoothSegments = 10;

		[Header("Point Badge Settings")]
		[SerializeField] private GameObject _pointBadgePrefab;
		[SerializeField] private float _badgeOffset = 0.08f; // Height above waypoint (reduced from 0.1)
		[SerializeField] private Vector3 _badgeScale = Vector3.one * 0.03f; // Badge size (reduced from 0.1)
		[SerializeField] private int _badgeFontSize = 32; // TextMesh font size for badges

		[Header("Performance")]
		[SerializeField] private int _maxLineSegments = 1000;
		[SerializeField] private bool _useObjectPooling = true;

	private FlightPathManager _pathManager;
	private readonly List<LineRenderer> _activeLineRenderers = new List<LineRenderer>();
	private readonly List<GameObject> _activeBadges = new List<GameObject>();
	private readonly List<GameObject> _activeArrows = new List<GameObject>();
	private readonly Queue<LineRenderer> _lineRendererPool = new Queue<LineRenderer>();
	private readonly Queue<GameObject> _badgePool = new Queue<GameObject>();
	private readonly Queue<GameObject> _arrowPool = new Queue<GameObject>();
	private Transform _linesParent;
	private Transform _badgesParent;
	
	// Constants for Start/End point IDs (must match FlightPathManager)
	private const int START_POINT_ID = -1;
	private const int END_POINT_ID = -2;

		/// <summary>
		/// Width of path lines in meters.
		/// </summary>
		public float LineWidth
		{
			get => _lineWidth;
			set => _lineWidth = Mathf.Max(0.001f, value);
		}

		/// <summary>
		/// Whether to show directional arrows along the path.
		/// </summary>
		public bool ShowArrows
		{
			get => _showArrows;
			set => _showArrows = value;
		}

		/// <summary>
		/// Distance between arrows in meters.
		/// </summary>
		public float ArrowSpacing
		{
			get => _arrowSpacing;
			set => _arrowSpacing = Mathf.Max(0.1f, value);
		}

		/// <summary>
		/// Whether to use smooth curves instead of straight lines.
		/// </summary>
		public bool SmoothPath
		{
			get => _smoothPath;
			set => _smoothPath = value;
		}

		/// <summary>
		/// Number of segments for smooth curve interpolation.
		/// </summary>
		public int SmoothSegments
		{
			get => _smoothSegments;
			set => _smoothSegments = Mathf.Max(2, value);
		}

		private void Awake()
		{
			CreateParentObjects();
			CreateDefaultMaterials();
			CreateDefaultBadgePrefab();
		}

		private void OnDestroy()
		{
			ClearAllPaths();
		}

		/// <summary>
		/// Initialize the path renderer with a flight path manager.
		/// </summary>
		public void Initialize(FlightPathManager pathManager)
		{
			_pathManager = pathManager;
			
			if (_pathManager != null)
			{
				_pathManager.OnPathModeChanged += HandlePathModeChanged;
				_pathManager.OnActiveRouteChanged += HandleActiveRouteChanged;
				_pathManager.OnPointAddedToRoute += HandlePointAddedToRoute;
				_pathManager.OnRouteCleared += HandleRouteCleared; // Thesis Feature: Clear visuals when route deleted
			}
		}

		/// <summary>
		/// Render a specific flight path.
		/// </summary>
		public void RenderPath(FlightPath path, PointPlacementManager pointManager)
		{
			if (path == null || pointManager == null || path.PointCount < 2)
			{
				return;
			}

			// Build and render segments with gaps when points are missing or explicit breaks are present
			var currentSegment = new List<Vector3>();
			foreach (int pointId in path.PointIds)
			{
			// Treat ID == 0 as explicit breaks between segments
			if (pointId == 0)
			{
				if (currentSegment.Count >= 2)
				{
					RenderPathLine(currentSegment, path.PathColor);
				}
				currentSegment.Clear();
				continue;
			}

			// Get position (handles Start/End points and regular waypoints)
			Vector3? position = GetPointPosition(pointId, pointManager);
			if (!position.HasValue)
			{
				// Missing point: end current segment
				if (currentSegment.Count >= 2)
				{
					RenderPathLine(currentSegment, path.PathColor);
				}
				currentSegment.Clear();
				continue;
			}

			currentSegment.Add(position.Value);
			}

			if (currentSegment.Count >= 2)
			{
				RenderPathLine(currentSegment, path.PathColor);
			}

			// Note: Point badges are rendered by PointHandle and StartEndPoint components themselves
			// RenderPointBadges(path, pointManager); // DISABLED - causes duplicate badges
		}

		/// <summary>
		/// Clear all rendered paths.
		/// </summary>
		public void ClearAllPaths()
		{
			ClearActiveLineRenderers();
			ClearActiveBadges();
			ClearActiveArrows();
		}

	/// <summary>
	/// Update the rendering of the currently active route.
	/// </summary>
	public void UpdateActiveRoute()
	{
		if (_pathManager == null) return;

		var activeRoute = _pathManager.GetActiveRoute();
		var pointManager = UnityEngine.Object.FindFirstObjectByType<PointPlacementManager>();
		
		// Clear first so removed segments disappear before re-rendering
		ClearAllPaths();

		if (_pathManager.PathModeEnabled && activeRoute != null)
		{
			// In path mode: render all completed routes + active route
			RenderAllCompletedRoutes();
			RenderPath(activeRoute, pointManager);
		}
			else
			{
				// Not in path mode: render all completed routes
				RenderAllCompletedRoutes();
			}
		}

		/// <summary>
		/// Render completed route. Thesis Feature: Single route simplified.
		/// </summary>
		public void RenderAllCompletedRoutes()
		{
			if (_pathManager == null) return;

			var pointManager = UnityEngine.Object.FindFirstObjectByType<PointPlacementManager>();
			if (pointManager == null) return;

			// Render the completed route if it exists
			var completedRoute = _pathManager.CompletedRoute;
			if (completedRoute != null && completedRoute.PointCount >= 2)
			{
				RenderPath(completedRoute, pointManager);
			}
		}

		private void CreateParentObjects()
		{
			// Create parent objects for organization
			var pathRenderersParent = new GameObject("Path Renderers");
			pathRenderersParent.transform.SetParent(transform);
			_linesParent = pathRenderersParent.transform;

			var badgesParent = new GameObject("Path Badges");
			badgesParent.transform.SetParent(transform);
			_badgesParent = badgesParent.transform;
		}

		private void CreateDefaultMaterials()
		{
			if (_pathLineMaterial == null)
			{
				_pathLineMaterial = new Material(Shader.Find("Sprites/Default"));
			_pathLineMaterial.color = new Color(0f, 0.7f, 0.7f); // Darker cyan - easier on the eyes
			_pathLineMaterial.renderQueue = 3000; // Transparent
			}
		}

		private void CreateDefaultBadgePrefab()
		{
			if (_pointBadgePrefab == null)
			{
				_pointBadgePrefab = new GameObject("Point Badge");
				
				var textMesh = _pointBadgePrefab.AddComponent<TextMesh>();
				textMesh.text = "1";
				textMesh.fontSize = _badgeFontSize; // Use configurable size
				textMesh.color = Color.white;
				textMesh.anchor = TextAnchor.MiddleCenter;
				textMesh.alignment = TextAlignment.Center;
				textMesh.characterSize = 0.08f; // Smaller character size for better rendering
				
				// Ensure the badge always faces the camera
				_pointBadgePrefab.AddComponent<PointLabelBillboard>();

				var meshRenderer = _pointBadgePrefab.GetComponent<MeshRenderer>();
				meshRenderer.sortingOrder = 100;

				// Add a background circle
				var background = new GameObject("Background");
				background.transform.SetParent(_pointBadgePrefab.transform);
				background.transform.localPosition = Vector3.zero;
				background.transform.localScale = Vector3.one * 1.2f;

				var backgroundRenderer = background.AddComponent<SpriteRenderer>();
				backgroundRenderer.sprite = CreateCircleSprite();
				backgroundRenderer.color = new Color(0, 0, 0, 0.7f);
				backgroundRenderer.sortingOrder = 99;

				_pointBadgePrefab.SetActive(false);
			}
		}

		private Sprite CreateCircleSprite()
		{
			var texture = new Texture2D(32, 32);
			var pixels = new Color[32 * 32];
			var center = new Vector2(16, 16);
			var radius = 15f;

			for (int y = 0; y < 32; y++)
			{
				for (int x = 0; x < 32; x++)
				{
					var distance = Vector2.Distance(new Vector2(x, y), center);
					pixels[y * 32 + x] = distance <= radius ? Color.white : Color.clear;
				}
			}

			texture.SetPixels(pixels);
			texture.Apply();

			return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
		}

	private List<Vector3> GetPathPositions(FlightPath path, PointPlacementManager pointManager)
	{
		var positions = new List<Vector3>();
		
		foreach (int pointId in path.PointIds)
		{
			if (pointId == 0) continue; // Skip explicit breaks
			
			Vector3? position = GetPointPosition(pointId, pointManager);
			if (position.HasValue)
			{
				positions.Add(position.Value);
			}
		}

			// Add closed loop connection if needed
			if (path.IsClosed && positions.Count >= 3)
			{
				positions.Add(positions[0]);
			}

			// Apply smoothing if enabled
			if (_smoothPath && positions.Count >= 3)
			{
				return CreateSmoothPath(positions);
			}

			return positions;
		}

		private List<Vector3> CreateSmoothPath(List<Vector3> controlPoints)
		{
			var smoothPoints = new List<Vector3>();
			
			// Catmull-Rom spline interpolation
			for (int i = 0; i < controlPoints.Count - 1; i++)
			{
				Vector3 p0 = i > 0 ? controlPoints[i - 1] : controlPoints[i];
				Vector3 p1 = controlPoints[i];
				Vector3 p2 = controlPoints[i + 1];
				Vector3 p3 = i < controlPoints.Count - 2 ? controlPoints[i + 2] : controlPoints[i + 1];

				for (int j = 0; j <= _smoothSegments; j++)
				{
					float t = (float)j / _smoothSegments;
					Vector3 point = CatmullRom(p0, p1, p2, p3, t);
					smoothPoints.Add(point);
				}
			}

			return smoothPoints;
		}

		private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			float t2 = t * t;
			float t3 = t2 * t;

			return 0.5f * (
				(2 * p1) +
				(-p0 + p2) * t +
				(2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
				(-p0 + 3 * p1 - 3 * p2 + p3) * t3
			);
		}

		private void RenderPathLine(List<Vector3> positions, Color color)
		{
			var lineRenderer = GetPooledLineRenderer();
			if (lineRenderer == null) return;

			lineRenderer.material.color = color;
			lineRenderer.startWidth = _lineWidth;
			lineRenderer.endWidth = _lineWidth;
			lineRenderer.positionCount = Mathf.Min(positions.Count, _maxLineSegments);
			
			for (int i = 0; i < lineRenderer.positionCount; i++)
			{
				lineRenderer.SetPosition(i, positions[i]);
			}

			lineRenderer.gameObject.SetActive(true);
			_activeLineRenderers.Add(lineRenderer);
		}

		private void RenderArrows(List<Vector3> positions, Color color)
		{
			if (positions.Count < 2) return;

			for (int i = 0; i < positions.Count - 1; i++)
			{
				Vector3 start = positions[i];
				Vector3 end = positions[i + 1];
				Vector3 direction = (end - start).normalized;
				float distance = Vector3.Distance(start, end);

				float currentDistance = _arrowSpacing;
				while (currentDistance < distance)
				{
					Vector3 arrowPos = start + direction * currentDistance;
					CreateArrow(arrowPos, direction, color);
					currentDistance += _arrowSpacing;
				}
			}
		}

		private void CreateArrow(Vector3 position, Vector3 direction, Color color)
		{
			var arrow = GetPooledArrow();
			if (arrow == null) return;

			arrow.transform.position = position;
			arrow.transform.rotation = Quaternion.LookRotation(direction);
			arrow.SetActive(true);
			_activeArrows.Add(arrow);

			var renderer = arrow.GetComponent<Renderer>();
			if (renderer != null)
			{
				renderer.material.color = color;
			}
		}

	private void RenderPointBadges(FlightPath path, PointPlacementManager pointManager)
	{
		// Continuous numbering: Start=1, waypoints=2,3,4..., End=last number
		int continuousIndex = 0;
		foreach (int pointId in path.PointIds)
		{
			if (pointId == 0) continue; // skip explicit breaks
			
			Vector3? position = GetPointPosition(pointId, pointManager);
			if (!position.HasValue) continue;
			
			// Increment index for each point
			continuousIndex++;
			
			// Determine badge label
			string label = continuousIndex.ToString();
			
			CreatePointBadge(position.Value, label, path.PathColor);
		}
	}

		/// <summary>
		/// Get the starting index for numbering. Thesis Feature: Always 0 for single route.
		/// </summary>
		private int GetContinuousRouteStartIndex(FlightPath targetPath)
		{
			// Simple: always start at 0 for single route mode
			return 0;
		}

		private void CreatePointBadge(Vector3 position, string text, Color color)
		{
			var badge = GetPooledBadge();
			if (badge == null) return;

			// Ensure billboard behavior exists (covers case of custom prefab)
			if (badge.GetComponent<PointLabelBillboard>() == null)
			{
				badge.AddComponent<PointLabelBillboard>();
			}

			badge.transform.position = position + Vector3.up * _badgeOffset;
			badge.transform.localScale = _badgeScale;

			var textMesh = badge.GetComponent<TextMesh>();
			if (textMesh != null)
			{
				textMesh.text = text;
				textMesh.color = color;
				textMesh.fontSize = _badgeFontSize; // Use configurable font size
			}

			badge.SetActive(true);
			_activeBadges.Add(badge);
		}

		private LineRenderer GetPooledLineRenderer()
		{
			if (_useObjectPooling && _lineRendererPool.Count > 0)
			{
				var pooled = _lineRendererPool.Dequeue();
				pooled.gameObject.SetActive(false);
				return pooled;
			}

			var lineObj = new GameObject("Path Line");
			lineObj.transform.SetParent(_linesParent);
			var lineRenderer = lineObj.AddComponent<LineRenderer>();
			lineRenderer.material = _pathLineMaterial;
			lineRenderer.useWorldSpace = true;
			lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lineRenderer.receiveShadows = false;

			return lineRenderer;
		}

		private GameObject GetPooledBadge()
		{
			if (_useObjectPooling && _badgePool.Count > 0)
			{
				return _badgePool.Dequeue();
			}

			return Instantiate(_pointBadgePrefab, _badgesParent);
		}

		private GameObject GetPooledArrow()
		{
			if (_useObjectPooling && _arrowPool.Count > 0)
			{
				return _arrowPool.Dequeue();
			}

			// Create a simple arrow primitive
			var arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			arrow.transform.SetParent(_linesParent);
			arrow.transform.localScale = new Vector3(0.02f, 0.02f, 0.1f);
			
			// Remove collider
			var collider = arrow.GetComponent<Collider>();
			if (collider != null)
			{
				DestroyImmediate(collider);
			}

			return arrow;
		}

		private void ClearActiveLineRenderers()
		{
			foreach (var lineRenderer in _activeLineRenderers)
			{
				if (lineRenderer != null)
				{
					lineRenderer.gameObject.SetActive(false);
					if (_useObjectPooling)
					{
						_lineRendererPool.Enqueue(lineRenderer);
					}
					else
					{
						Destroy(lineRenderer.gameObject);
					}
				}
			}
			_activeLineRenderers.Clear();
		}

		private void ClearActiveBadges()
		{
			foreach (var badge in _activeBadges)
			{
				if (badge != null)
				{
					badge.SetActive(false);
					if (_useObjectPooling)
					{
						_badgePool.Enqueue(badge);
					}
					else
					{
						Destroy(badge);
					}
				}
			}
			_activeBadges.Clear();
		}

		private void ClearActiveArrows()
		{
			foreach (var arrow in _activeArrows)
			{
				if (arrow != null)
				{
					arrow.SetActive(false);
					if (_useObjectPooling)
					{
						_arrowPool.Enqueue(arrow);
					}
					else
					{
						Destroy(arrow);
					}
				}
			}
			_activeArrows.Clear();
		}

		private void HandlePathModeChanged(bool pathModeEnabled)
		{
			// Never clear paths when mode changes - keep all completed paths visible
			// This allows users to see completed paths even when not in path building mode
			if (pathModeEnabled)
			{
				// When entering path mode, refresh the display for active route
				UpdateActiveRoute();
			}
			else
			{
				// When exiting path mode, render all completed routes instead of clearing
				RenderAllCompletedRoutes();
			}
		}

		private void HandleActiveRouteChanged(FlightPath activeRoute)
		{
			UpdateActiveRoute();
		}

		private void HandlePointAddedToRoute(FlightPath route, int pointCount)
		{
			if (route == _pathManager.GetActiveRoute())
			{
				UpdateActiveRoute();
			}
		}

	/// <summary>
	/// Thesis Feature: Handle route being cleared (from waypoint deletion).
	/// </summary>
	private void HandleRouteCleared(FlightPath route)
	{
		Debug.Log("PathRenderer: Route cleared, removing all visual paths");
		ClearAllPaths();
	}

	/// <summary>
	/// Get the world position for any point ID (including Start/End points).
	/// </summary>
	private Vector3? GetPointPosition(int pointId, PointPlacementManager pointManager)
	{
		// Handle Start/End points
		if (pointId == START_POINT_ID)
		{
			var startPoint = _pathManager?.GetStartPoint();
			Vector3? position = startPoint != null ? startPoint.Position : (Vector3?)null;
			if (position.HasValue)
			{
				Debug.Log($"[PathRenderer] Getting Start point position: {position.Value} (GameObject: {startPoint.gameObject.name})");
			}
			else
			{
				Debug.LogWarning("[PathRenderer] Start point is NULL when trying to render!");
			}
			return position;
		}
		if (pointId == END_POINT_ID)
		{
			var endPoint = _pathManager?.GetEndPoint();
			return endPoint != null ? endPoint.Position : (Vector3?)null;
		}

		// Handle regular waypoints
		var handle = pointManager?.GetPoint(pointId);
		return handle != null ? handle.transform.position : (Vector3?)null;
	}
}
}
