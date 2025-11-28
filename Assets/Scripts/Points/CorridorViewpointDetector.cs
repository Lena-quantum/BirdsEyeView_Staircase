using UnityEngine;

namespace Points
{
	/// <summary>
	/// Detects whether the player is inside or outside the corridor volume.
	/// Used to determine if raycasts should pass through the corridor shell (outer walls/ceiling).
	/// </summary>
	public class CorridorViewpointDetector : MonoBehaviour
	{
		[Header("Corridor Bounds")]
		[Tooltip("Optional: GameObject whose Renderer bounds define the corridor volume. If null, uses manual bounds.")]
		[SerializeField] private GameObject _corridorModel;
		
		[Tooltip("Manual corridor bounds (used if _corridorModel is null).")]
		[SerializeField] private Bounds _manualCorridorBounds = new Bounds(Vector3.zero, new Vector3(10f, 3f, 10f));
		
		[Tooltip("Padding to expand bounds for 'inside' detection (prevents edge cases).")]
		[SerializeField] private float _insidePadding = 0.5f;
		
		[Header("Player Reference")]
		[Tooltip("XR Origin or main camera transform to check position.")]
		[SerializeField] private Transform _playerTransform;
		
		[Header("Debug")]
		[SerializeField] private bool _showDebugGizmos = true;
		[SerializeField] private Color _insideColor = Color.green;
		[SerializeField] private Color _outsideColor = Color.red;
		
		private Bounds _effectiveBounds;
		private bool _boundsInitialized;
		
		/// <summary>
		/// Whether the player is currently inside the corridor volume.
		/// </summary>
		public bool IsInsideCorridor
		{
			get
			{
				if (!_boundsInitialized)
				{
					InitializeBounds();
				}
				
				if (_playerTransform == null)
				{
					// Try to find XR Origin or main camera
					GameObject xrOrigin = GameObject.Find("XR Origin");
					if (xrOrigin != null)
					{
						_playerTransform = xrOrigin.transform;
					}
					else
					{
						Camera mainCam = Camera.main;
						if (mainCam != null)
						{
							_playerTransform = mainCam.transform;
						}
					}
					
					if (_playerTransform == null)
					{
						Debug.LogWarning("CorridorViewpointDetector: No player transform found. Defaulting to 'inside'.");
						return true; // Default to inside (conservative)
					}
				}
				
				Vector3 playerPos = _playerTransform.position;
				Bounds paddedBounds = new Bounds(_effectiveBounds.center, _effectiveBounds.size + Vector3.one * _insidePadding * 2f);
				return paddedBounds.Contains(playerPos);
			}
		}
		
		/// <summary>
		/// Get the current effective corridor bounds.
		/// </summary>
		public Bounds CorridorBounds
		{
			get
			{
				if (!_boundsInitialized)
				{
					InitializeBounds();
				}
				return _effectiveBounds;
			}
		}
		
		private void Awake()
		{
			InitializeBounds();
		}
		
		private void InitializeBounds()
		{
			if (_corridorModel != null)
			{
				Renderer renderer = _corridorModel.GetComponent<Renderer>();
				if (renderer != null)
				{
					_effectiveBounds = renderer.bounds;
					_boundsInitialized = true;
					Debug.Log($"CorridorViewpointDetector: Using bounds from {_corridorModel.name}: {_effectiveBounds}");
					return;
				}
				
				// Try to get bounds from all renderers in children
				Renderer[] renderers = _corridorModel.GetComponentsInChildren<Renderer>();
				if (renderers.Length > 0)
				{
					_effectiveBounds = renderers[0].bounds;
					for (int i = 1; i < renderers.Length; i++)
					{
						_effectiveBounds.Encapsulate(renderers[i].bounds);
					}
					_boundsInitialized = true;
					Debug.Log($"CorridorViewpointDetector: Using combined bounds from {renderers.Length} renderers: {_effectiveBounds}");
					return;
				}
			}
			
			// Fallback to manual bounds
			_effectiveBounds = _manualCorridorBounds;
			_boundsInitialized = true;
			Debug.Log($"CorridorViewpointDetector: Using manual bounds: {_effectiveBounds}");
		}
		
		/// <summary>
		/// Check if a position is inside the corridor volume.
		/// </summary>
		public bool IsPositionInsideCorridor(Vector3 position)
		{
			if (!_boundsInitialized)
			{
				InitializeBounds();
			}
			Bounds paddedBounds = new Bounds(_effectiveBounds.center, _effectiveBounds.size + Vector3.one * _insidePadding * 2f);
			return paddedBounds.Contains(position);
		}
		
		private void OnDrawGizmos()
		{
			if (!_showDebugGizmos) return;
			
			Bounds bounds = _boundsInitialized ? _effectiveBounds : _manualCorridorBounds;
			bool isInside = Application.isPlaying && IsInsideCorridor;
			
			Gizmos.color = isInside ? _insideColor : _outsideColor;
			Gizmos.DrawWireCube(bounds.center, bounds.size);
			
			// Draw padded bounds
			Bounds paddedBounds = new Bounds(bounds.center, bounds.size + Vector3.one * _insidePadding * 2f);
			Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
			Gizmos.DrawCube(paddedBounds.center, paddedBounds.size);
			
			// Draw player position
			if (Application.isPlaying && _playerTransform != null)
			{
				Gizmos.color = isInside ? Color.green : Color.red;
				Gizmos.DrawSphere(_playerTransform.position, 0.1f);
			}
		}
	}
}

