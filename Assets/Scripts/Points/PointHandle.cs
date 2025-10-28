using UnityEngine;

namespace Points
{
	/// <summary>
	/// Component for a placed point. Handles hover/selection and exposes its ID and appearance.
	/// </summary>
	[RequireComponent(typeof(SphereCollider))]
	public class PointHandle : MonoBehaviour
	{
		[SerializeField] private int _id;
		[SerializeField] private float _radius = 0.05f;
		[SerializeField] private Color _color = Color.yellow;
		[SerializeField] private Renderer _renderer;
		[SerializeField] private Material _defaultMaterial;
		[SerializeField] private Material _highlightMaterial;
		[SerializeField] private PointLabelBillboard _label;
		[SerializeField] private GameObject _routeBadge;
		[SerializeField] private TextMesh _routeBadgeText;

		private PointPlacementManager _manager;
		private FlightPathManager _pathManager;
		private bool _hovered;
		private int _routeIndex = -1;

		/// <summary>
		/// Unique point ID assigned by the manager.
		/// </summary>
		public int Id => _id;

		/// <summary>
		/// Initialize the point handle.
		/// </summary>
		public void Initialize(int id, Color color, float radius, PointPlacementManager manager)
		{
			_manager = manager;
			_id = id;
			_color = color;
			_radius = radius;
			_pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
			
			CreateRouteBadge();
			ApplyAppearance();
			UpdateLabel();
		}

		private void Reset()
		{
			var col = GetComponent<SphereCollider>();
			if (col != null)
			{
				col.isTrigger = false;
			}
		}

		private void Awake()
		{
			if (_renderer == null)
			{
				_renderer = GetComponentInChildren<Renderer>();
			}
			ApplyAppearance();
		}

		private void Start()
		{
			_pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
			if (_pathManager != null)
			{
				_pathManager.OnActiveRouteChanged += UpdateRouteBadge;
				_pathManager.OnPointAddedToRoute += OnPointAddedToRoute;
			}
		}

		private void OnDestroy()
		{
			if (_pathManager != null)
			{
				_pathManager.OnActiveRouteChanged -= UpdateRouteBadge;
				_pathManager.OnPointAddedToRoute -= OnPointAddedToRoute;
			}
		}

		private void ApplyAppearance()
		{
			var col = GetComponent<SphereCollider>();
			if (col != null)
			{
				// Make collider much larger than visual radius for much easier targeting
				col.radius = _radius * 5.0f; // 5x the visual size for very easy selection
				col.isTrigger = false; // Make sure it's not a trigger
			}
			if (_renderer != null)
			{
				foreach (var mat in _renderer.materials)
				{
					if (mat != null && mat.HasProperty("_Color"))
					{
						mat.color = _color;
					}
				}
			}
		}

	// DISABLED: OnMouse handlers not needed for VR - interaction handled by RayDepthController
	// private void OnMouseEnter()
	// {
	// 	SetHovered(true);
	// }

	// private void OnMouseExit()
	// {
	// 	SetHovered(false);
	// }

	// private void OnMouseDown()
	// {
	// 	_manager?.NotifySelected(this);
	// }

		private void SetHovered(bool hovered)
		{
			_hovered = hovered;
			ApplyHoverMaterial();
			_manager?.NotifyHovered(this, hovered);
		}

		/// <summary>
		/// Public method to set hover state from external scripts.
		/// </summary>
		public void SetHoveredState(bool hovered)
		{
			SetHovered(hovered);
		}

		private void ApplyHoverMaterial()
		{
			if (_renderer == null || _defaultMaterial == null || _highlightMaterial == null) return;
			_renderer.sharedMaterial = _hovered ? _highlightMaterial : _defaultMaterial;
		}

		/// <summary>
		/// Update the point's visual appearance based on route membership.
		/// </summary>
		public void UpdateVisualState()
		{
			if (_pathManager == null) return;

			var activeRoute = _pathManager.GetActiveRoute();
			bool isInActiveRoute = activeRoute != null && activeRoute.ContainsPoint(_id);

			// Update point color based on route membership
			if (_renderer != null)
			{
				Color targetColor = _color;

				// Only change color if point is in the ACTIVE route being built
				if (isInActiveRoute)
				{
					targetColor = activeRoute.PathColor;
				}
				// Keep original color for completed routes - don't gray them out

				foreach (var material in _renderer.materials)
				{
					if (material != null && material.HasProperty("_Color"))
					{
						material.color = targetColor;
					}
				}
			}

			UpdateRouteBadge(activeRoute);
		}

		/// <summary>
		/// Create the route badge for displaying point order in routes.
		/// </summary>
		private void CreateRouteBadge()
		{
			if (_routeBadge != null) return;

			_routeBadge = new GameObject($"Route Badge {_id}");
			_routeBadge.transform.SetParent(transform);
			_routeBadge.transform.localPosition = Vector3.up * (_radius + 0.05f);
			_routeBadge.transform.localScale = Vector3.one * 0.1f;

			_routeBadgeText = _routeBadge.AddComponent<TextMesh>();
			_routeBadgeText.text = "";
			_routeBadgeText.fontSize = 24;
			_routeBadgeText.color = Color.white;
			_routeBadgeText.anchor = TextAnchor.MiddleCenter;
			_routeBadgeText.alignment = TextAlignment.Center;

			var meshRenderer = _routeBadge.GetComponent<MeshRenderer>();
			meshRenderer.sortingOrder = 100;

			// Add background
			var background = new GameObject("Badge Background");
			background.transform.SetParent(_routeBadge.transform);
			background.transform.localPosition = Vector3.zero;
			background.transform.localScale = Vector3.one * 1.5f;

			var bgRenderer = background.AddComponent<SpriteRenderer>();
			bgRenderer.sprite = CreateCircleSprite();
			bgRenderer.color = new Color(0, 0, 0, 0.8f);
			bgRenderer.sortingOrder = 99;

			_routeBadge.SetActive(false);
		}

		/// <summary>
		/// Create a simple circle sprite for the badge background.
		/// </summary>
		private Sprite CreateCircleSprite()
		{
			var texture = new Texture2D(16, 16);
			var pixels = new Color[16 * 16];
			var center = new Vector2(8, 8);
			var radius = 7f;

			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					var distance = Vector2.Distance(new Vector2(x, y), center);
					pixels[y * 16 + x] = distance <= radius ? Color.white : Color.clear;
				}
			}

			texture.SetPixels(pixels);
			texture.Apply();

			return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
		}

		/// <summary>
		/// Update the route badge visibility and text.
		/// </summary>
		private void UpdateRouteBadge(FlightPath activeRoute)
		{
			if (_routeBadge == null || _routeBadgeText == null) return;

			// Check if this point is in any completed route
			var completedRoute = FindCompletedRouteContainingPoint();
			
			if (activeRoute != null && activeRoute.ContainsPoint(_id))
			{
				// Point is in active route being built - use continuous numbering
				_routeIndex = GetContinuousRouteIndex(activeRoute);
				_routeBadgeText.text = _routeIndex.ToString();
				_routeBadgeText.color = activeRoute.PathColor;
				_routeBadge.SetActive(true);
			}
			else if (completedRoute != null)
			{
				// Point is in a completed route - keep badge visible with continuous numbering
				_routeIndex = GetContinuousRouteIndex(completedRoute);
				_routeBadgeText.text = _routeIndex.ToString();
				_routeBadgeText.color = completedRoute.PathColor;
				_routeBadge.SetActive(true);
			}
			else
			{
				// Point is not in any route
				_routeIndex = -1;
				_routeBadge.SetActive(false);
			}
		}

		/// <summary>
		/// Get the continuous route index for this point across all routes.
		/// </summary>
		private int GetContinuousRouteIndex(FlightPath route)
		{
			if (_pathManager == null || route == null) return -1;

			// Calculate total points before this route
			int totalPointsBefore = 0;
			var allRoutes = _pathManager.GetAllRoutes();
			
			foreach (var r in allRoutes)
			{
				if (r == route) break;
				totalPointsBefore += r.PointCount;
			}
			
			// Add this point's index within its route
			int pointIndexInRoute = route.GetPointIndex(_id);
			return totalPointsBefore + pointIndexInRoute + 1; // Convert to 1-based
		}

		/// <summary>
		/// Find a completed route that contains this point.
		/// </summary>
		private FlightPath FindCompletedRouteContainingPoint()
		{
			if (_pathManager == null) return null;

			var routes = _pathManager.GetAllRoutes();
			foreach (var route in routes)
			{
				if (route != null && route.ContainsPoint(_id))
				{
					return route;
				}
			}
			return null;
		}

		/// <summary>
		/// Update the point label based on current state.
		/// </summary>
		private void UpdateLabel()
		{
			if (_label != null)
			{
				string labelText = $"#{_id}";
				if (_routeIndex > 0)
				{
					labelText += $" [{_routeIndex}]";
				}
				_label.SetText(labelText);
			}
		}

		/// <summary>
		/// Handle when a point is added to a route.
		/// </summary>
		private void OnPointAddedToRoute(FlightPath route, int pointCount)
		{
			if (route != null && route.ContainsPoint(_id))
			{
				UpdateVisualState();
			}
		}

		/// <summary>
		/// Get the current route index of this point (1-based for display).
		/// </summary>
		public int GetRouteIndex()
		{
			return _routeIndex;
		}

		/// <summary>
		/// Check if this point is in the active route.
		/// </summary>
		public bool IsInActiveRoute()
		{
			if (_pathManager == null) return false;
			var activeRoute = _pathManager.GetActiveRoute();
			return activeRoute != null && activeRoute.ContainsPoint(_id);
		}

		/// <summary>
		/// Check if this point is in any route.
		/// </summary>
		public bool IsInAnyRoute()
		{
			return _pathManager != null && _pathManager.IsPointInAnyRoute(_id);
		}
	}
}


