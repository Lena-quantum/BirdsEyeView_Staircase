using UnityEngine;

namespace Points
{
	/// <summary>
	/// Marks a GameObject as a Start or End point for the flight path.
	/// These are pre-placed by the experimenter and define where the path must begin and end.
	/// </summary>
	public class StartEndPoint : MonoBehaviour
	{
		public enum PointType
		{
			Start,
			End
		}

		[Header("Configuration")]
		[SerializeField] private PointType _pointType = PointType.Start;

		[Header("Visual")]
		[SerializeField] private MeshRenderer _boxRenderer;
		[SerializeField] private TMPro.TextMeshPro _label;

		// Assigned by FlightPathManager
		private int _pointId = -1;
		private bool _isRegistered = false;
		
		// Route badge
		private GameObject _routeBadge;
		private TextMesh _routeBadgeText;
		private FlightPathManager _pathManager;

		public PointType Type => _pointType;
		public int PointId => _pointId;
		public bool IsRegistered => _isRegistered;
		public Vector3 Position => transform.position;

	private void Awake()
	{
		// Set up visuals based on type
		if (_boxRenderer != null)
		{
			Color color = _pointType == PointType.Start ? Color.white : Color.black;
			_boxRenderer.material.color = color;
		}

		if (_label != null)
		{
			_label.text = _pointType == PointType.Start ? "START" : "END";
			_label.color = _pointType == PointType.Start ? Color.black : Color.white; // Contrast
			
			// Add billboard behavior to label so it always faces the camera
			var billboard = _label.gameObject.GetComponent<PointLabelBillboard>();
			if (billboard == null)
			{
				_label.gameObject.AddComponent<PointLabelBillboard>();
			}
		}
		
		CreateRouteBadge();
	}
	
	private void Start()
	{
		_pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
		if (_pathManager != null)
		{
			_pathManager.OnPointAddedToRoute += OnPointAddedToRoute;
			_pathManager.OnRouteFinished += OnRouteFinished;
			_pathManager.OnRouteCleared += OnRouteCleared;
		}
	}
	
	private void OnDestroy()
	{
		if (_pathManager != null)
		{
			_pathManager.OnPointAddedToRoute -= OnPointAddedToRoute;
			_pathManager.OnRouteFinished -= OnRouteFinished;
			_pathManager.OnRouteCleared -= OnRouteCleared;
		}
	}

	/// <summary>
	/// Called by FlightPathManager to register this point in the path system.
	/// </summary>
	public void Register(int id)
	{
		_pointId = id;
		_isRegistered = true;
		Debug.Log($"{_pointType} point registered with ID {id} at position {Position}");
	}

	/// <summary>
	/// Unregister this point (e.g., if scene is reset).
	/// </summary>
	public void Unregister()
	{
		_pointId = -1;
		_isRegistered = false;
	}
	
	/// <summary>
	/// Create the route badge for displaying point order in routes.
	/// </summary>
	private void CreateRouteBadge()
	{
		if (_routeBadge != null) return;

		_routeBadge = new GameObject($"Route Badge {_pointType}");
		_routeBadge.transform.SetParent(transform);
		_routeBadge.transform.localPosition = Vector3.up * 0.65f; // Above the box
		_routeBadge.transform.localScale = Vector3.one * 0.18f; // Slightly reduced from 0.25f

		_routeBadgeText = _routeBadge.AddComponent<TextMesh>();
		_routeBadgeText.text = "";
		_routeBadgeText.fontSize = 28; // Slightly reduced from 32
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

		// Add billboard behavior
		var billboard = _routeBadge.AddComponent<PointLabelBillboard>();

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
	public void UpdateVisualState()
	{
		if (_pathManager == null || _routeBadge == null || _routeBadgeText == null) return;

		var activeRoute = _pathManager.GetActiveRoute();
		
		if (activeRoute != null && activeRoute.ContainsPoint(_pointId))
		{
			// Point is in active route - show badge with route index
			int routeIndex = GetRouteIndex(activeRoute);
			_routeBadgeText.text = routeIndex.ToString();
			_routeBadgeText.color = activeRoute.PathColor;
			_routeBadge.SetActive(true);
		}
		else
		{
			// Point is not in route
			_routeBadge.SetActive(false);
		}
	}
	
	/// <summary>
	/// Get the 1-based index of this point in the route.
	/// </summary>
	private int GetRouteIndex(FlightPath route)
	{
		if (route == null) return -1;
		
		int pointIndexInRoute = route.GetPointIndex(_pointId);
		return pointIndexInRoute + 1; // Convert to 1-based
	}
	
	/// <summary>
	/// Handle when a point is added to a route.
	/// </summary>
	private void OnPointAddedToRoute(FlightPath route, int pointCount)
	{
		if (route != null && route.ContainsPoint(_pointId))
		{
			UpdateVisualState();
		}
	}
	
	/// <summary>
	/// Handle when a route is finished.
	/// </summary>
	private void OnRouteFinished(FlightPath route)
	{
		UpdateVisualState();
	}
	
	/// <summary>
	/// Handle when a route is cleared.
	/// </summary>
	private void OnRouteCleared(FlightPath route)
	{
		UpdateVisualState();
	}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			// Draw a visual indicator in the editor
			Gizmos.color = _pointType == PointType.Start ? Color.green : Color.red;
			Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
			
			// Draw label position
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.1f);
		}
#endif
	}
}

