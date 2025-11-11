using UnityEngine;

namespace Points
{
	/// <summary>
	/// Manages the red holographic buffer zone visualization when a ghost waypoint gets too close to an obstacle.
	/// Creates a semi-transparent red shell matching the obstacle's shape, offset by the drone radius.
	/// </summary>
	public class ObstacleHighlighter : MonoBehaviour
	{
	private GameObject _bufferZoneVisual;
	private Renderer _visualRenderer;
	private Material _visualMaterial;
	private float _currentAlpha;
	private bool _isHighlighted;
	private float _droneRadius;
	private Collider _obstacleCollider;
	
	[Header("Visual Settings")]
	[SerializeField] private Color _warningColor = new Color(1f, 0.2f, 0.2f, 0.4f); // Red semi-transparent (40% opacity)
	[SerializeField] private float _fadeSpeed = 5f;
	[SerializeField] private bool _skipFade = false; // Set TRUE to always show at full alpha

	/// <summary>
	/// Activate the red warning zone showing the buffer boundary.
	/// </summary>
	public void Highlight(float droneRadius)
	{
		_isHighlighted = true;
		
		// Create buffer zone visual if it doesn't exist or if radius changed
		if (_bufferZoneVisual == null || Mathf.Abs(_droneRadius - droneRadius) > 0.01f)
		{
			_droneRadius = droneRadius;
			
			// Destroy old visual if radius changed
			if (_bufferZoneVisual != null)
			{
				Destroy(_bufferZoneVisual);
				_bufferZoneVisual = null;
			}
			
			CreateBufferZoneVisual();
		}
		
		// Activate visual
		if (_bufferZoneVisual != null)
		{
			_bufferZoneVisual.SetActive(true);
		}
	}

		/// <summary>
		/// Deactivate the red warning zone.
		/// </summary>
		public void Unhighlight()
		{
			_isHighlighted = false;
		}

	private void Update()
	{
		if (_bufferZoneVisual == null || _visualMaterial == null) return;
		
		// Smoothly fade in or out (or skip fade if debugging)
		float targetAlpha = _isHighlighted ? _warningColor.a : 0f;
		
		if (_skipFade)
		{
			// Debug mode: always show at full alpha
			_currentAlpha = _warningColor.a;
		}
		else
		{
			_currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, Time.deltaTime * _fadeSpeed);
		}

		// Apply alpha to material - try all possible color properties
		Color currentColor = new Color(_warningColor.r, _warningColor.g, _warningColor.b, _currentAlpha);
		_visualMaterial.color = currentColor;
		if (_visualMaterial.HasProperty("_TintColor")) 
		{
			_visualMaterial.SetColor("_TintColor", currentColor);
		}
		if (_visualMaterial.HasProperty("_Color")) 
		{
			_visualMaterial.SetColor("_Color", currentColor);
		}
		
		// Hide visual when fully faded out (unless skip fade is on)
		if (!_skipFade && _currentAlpha < 0.01f && !_isHighlighted)
		{
			_bufferZoneVisual.SetActive(false);
		}
	}

	private void CreateBufferZoneVisual()
	{
		Debug.LogError("====== COLLISION DETECTION: CREATING RED BUFFER ZONE ======");
		
		_obstacleCollider = GetComponent<Collider>();
		if (_obstacleCollider == null)
		{
			Debug.LogError($"====== NO COLLIDER FOUND ON {gameObject.name} ======");
			return;
		}

		Debug.LogError($"====== OBSTACLE NAME: {gameObject.name} ======");
		Debug.LogError($"====== OBSTACLE POSITION: {transform.position} ======");

		// Try to duplicate the visual mesh first
		MeshFilter obstacleMeshFilter = GetComponent<MeshFilter>();
		if (obstacleMeshFilter != null && obstacleMeshFilter.sharedMesh != null)
		{
			Debug.LogError("====== USING MESH-BASED VISUAL ======");
			CreateMeshBasedVisual(obstacleMeshFilter);
		}
		else
		{
			Debug.LogError("====== USING COLLIDER-BASED VISUAL ======");
			// Fallback: Create visual based on collider type
			CreateColliderBasedVisual();
		}
		
		if (_bufferZoneVisual != null)
		{
			_bufferZoneVisual.SetActive(false);
		}
	}

	private void CreateMeshBasedVisual(MeshFilter sourceMeshFilter)
	{
		Debug.LogError("====== CREATING MESH-BASED RED ZONE ======");
		
		// Create duplicate with same mesh - NO PARENT!
		_bufferZoneVisual = new GameObject("NoFlyZone_Visual");
		_bufferZoneVisual.transform.SetParent(null); // CRITICAL: No parent for correct positioning
		
		// Position directly in world space
		_bufferZoneVisual.transform.position = transform.position;
		_bufferZoneVisual.transform.rotation = transform.rotation;
		
		// Copy mesh
		MeshFilter visualMeshFilter = _bufferZoneVisual.AddComponent<MeshFilter>();
		visualMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
		
		// Calculate scale factor to inflate mesh by droneRadius
		Bounds bounds = sourceMeshFilter.sharedMesh.bounds;
		float avgSize = (bounds.size.x + bounds.size.y + bounds.size.z) / 3f;
		float scaleFactor = (avgSize + _droneRadius * 2f) / avgSize;
		
		// Apply world scale (lossyScale includes parent transforms)
		Vector3 worldScale = transform.lossyScale * scaleFactor;
		_bufferZoneVisual.transform.localScale = worldScale;
		
		Debug.LogError($"====== OBSTACLE POSITION: {transform.position} ======");
		Debug.LogError($"====== RED MESH POSITION: {_bufferZoneVisual.transform.position} ======");
		Debug.LogError($"====== MESH BOUNDS SIZE: {bounds.size} ======");
		Debug.LogError($"====== SCALE FACTOR: {scaleFactor} ======");
		Debug.LogError($"====== FINAL SCALE: {worldScale} ======");
		
		// Add renderer with transparent material
		_visualRenderer = _bufferZoneVisual.AddComponent<MeshRenderer>();
		SetupTransparentMaterial();
	}

	private void CreateColliderBasedVisual()
	{
		_bufferZoneVisual = new GameObject("NoFlyZone_Visual");
		
		// CRITICAL FIX: Don't use child hierarchy - position directly in world space
		_bufferZoneVisual.transform.SetParent(null); // No parent!
		_bufferZoneVisual.transform.position = transform.position;
		_bufferZoneVisual.transform.rotation = transform.rotation;
		_bufferZoneVisual.transform.localScale = Vector3.one;
			
			// Create visual based on collider type
			if (_obstacleCollider is BoxCollider boxCollider)
			{
				CreateBoxVisual(boxCollider);
			}
			else if (_obstacleCollider is SphereCollider sphereCollider)
			{
				CreateSphereVisual(sphereCollider);
			}
			else if (_obstacleCollider is CapsuleCollider capsuleCollider)
			{
				CreateCapsuleVisual(capsuleCollider);
			}
			else if (_obstacleCollider is MeshCollider meshCollider)
			{
				CreateMeshColliderVisual(meshCollider);
			}
			else
			{
				// Default to box
				CreateDefaultBoxVisual();
			}
			
			SetupTransparentMaterial();
		}

	private void CreateBoxVisual(BoxCollider boxCollider)
	{
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.SetParent(_bufferZoneVisual.transform);
		
		// Calculate world-space center of the collider
		Vector3 worldCenter = transform.TransformPoint(boxCollider.center);
		
		// Position cube at world-space center (but in local space of parent)
		cube.transform.position = worldCenter;
		cube.transform.rotation = transform.rotation;
		
		// Calculate inflated size (original size + buffer on all sides)
		Vector3 originalSize = boxCollider.size;
		Vector3 inflatedSize = originalSize + Vector3.one * (_droneRadius * 2f);
		
		// Apply obstacle's scale to the inflated size
		Vector3 obstacleScale = transform.lossyScale;
		Vector3 finalSize = new Vector3(
			inflatedSize.x * obstacleScale.x,
			inflatedSize.y * obstacleScale.y,
			inflatedSize.z * obstacleScale.z
		);
		
		cube.transform.localScale = finalSize;
		
		Debug.LogError("====== RED CUBE CREATED ======");
		Debug.LogError($"====== OBSTACLE POSITION: {transform.position} ======");
		Debug.LogError($"====== COLLIDER CENTER (local): {boxCollider.center} ======");
		Debug.LogError($"====== WORLD CENTER: {worldCenter} ======");
		Debug.LogError($"====== RED CUBE POSITION: {cube.transform.position} ======");
		Debug.LogError($"====== ORIGINAL SIZE: {originalSize} ======");
		Debug.LogError($"====== INFLATED SIZE: {inflatedSize} ======");
		Debug.LogError($"====== FINAL SCALE: {finalSize} ======");
		
		// Remove collider from visual
		Collider visualCollider = cube.GetComponent<Collider>();
		if (visualCollider != null)
		{
			Destroy(visualCollider);
		}
		
		_visualRenderer = cube.GetComponent<Renderer>();
	}

		private void CreateSphereVisual(SphereCollider sphereCollider)
		{
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.SetParent(_bufferZoneVisual.transform);
			sphere.transform.localPosition = sphereCollider.center;
			sphere.transform.localRotation = Quaternion.identity;
			
			// Inflate sphere by drone radius
			float inflatedRadius = sphereCollider.radius + _droneRadius;
			sphere.transform.localScale = Vector3.one * (inflatedRadius * 2f);
			
			Destroy(sphere.GetComponent<Collider>());
			_visualRenderer = sphere.GetComponent<Renderer>();
		}

		private void CreateCapsuleVisual(CapsuleCollider capsuleCollider)
		{
			GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			capsule.transform.SetParent(_bufferZoneVisual.transform);
			capsule.transform.localPosition = capsuleCollider.center;
			capsule.transform.localRotation = Quaternion.identity;
			
			// Inflate capsule by drone radius
			float inflatedRadius = capsuleCollider.radius + _droneRadius;
			float inflatedHeight = capsuleCollider.height + (_droneRadius * 2f);
			capsule.transform.localScale = new Vector3(inflatedRadius * 2f, inflatedHeight / 2f, inflatedRadius * 2f);
			
			Destroy(capsule.GetComponent<Collider>());
			_visualRenderer = capsule.GetComponent<Renderer>();
		}

		private void CreateMeshColliderVisual(MeshCollider meshCollider)
		{
			if (meshCollider.sharedMesh != null)
			{
				GameObject meshObj = new GameObject("MeshVisual");
				meshObj.transform.SetParent(_bufferZoneVisual.transform);
				meshObj.transform.localPosition = Vector3.zero;
				meshObj.transform.localRotation = Quaternion.identity;
				
				MeshFilter mf = meshObj.AddComponent<MeshFilter>();
				mf.sharedMesh = meshCollider.sharedMesh;
				
				// Calculate scale to inflate
				Bounds bounds = meshCollider.sharedMesh.bounds;
				float avgSize = (bounds.size.x + bounds.size.y + bounds.size.z) / 3f;
				float scaleFactor = (avgSize + _droneRadius * 2f) / avgSize;
				meshObj.transform.localScale = Vector3.one * scaleFactor;
				
				_visualRenderer = meshObj.AddComponent<MeshRenderer>();
			}
			else
			{
				CreateDefaultBoxVisual();
			}
		}

		private void CreateDefaultBoxVisual()
		{
			GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			cube.transform.SetParent(_bufferZoneVisual.transform);
			cube.transform.localPosition = Vector3.zero;
			cube.transform.localRotation = Quaternion.identity;
			cube.transform.localScale = Vector3.one * (_droneRadius * 2f);
			
			Destroy(cube.GetComponent<Collider>());
			_visualRenderer = cube.GetComponent<Renderer>();
		}

	private void SetupTransparentMaterial()
	{
		if (_visualRenderer == null) return;
		
		Debug.LogError("====== SETTING UP TRANSPARENT MATERIAL ======");
		
		// Try multiple shaders that should be available in Quest builds
		Shader shader = null;
		
		// Try 1: Mobile particles (almost always in VR builds)
		shader = Shader.Find("Mobile/Particles/Additive");
		if (shader != null) Debug.LogError("====== FOUND: Mobile/Particles/Additive ======");
		
		// Try 2: Standard particles
		if (shader == null)
		{
			shader = Shader.Find("Particles/Standard Unlit");
			if (shader != null) Debug.LogError("====== FOUND: Particles/Standard Unlit ======");
		}
		
		// Try 3: UI Default (always in builds with UI)
		if (shader == null)
		{
			shader = Shader.Find("UI/Default");
			if (shader != null) Debug.LogError("====== FOUND: UI/Default ======");
		}
		
		// Try 4: Unlit/Transparent (legacy but reliable)
		if (shader == null)
		{
			shader = Shader.Find("Unlit/Transparent");
			if (shader != null) Debug.LogError("====== FOUND: Unlit/Transparent ======");
		}
		
		// Try 5: Just take the existing material's shader
		if (shader == null && _visualRenderer.sharedMaterial != null)
		{
			shader = _visualRenderer.sharedMaterial.shader;
			Debug.LogError($"====== USING EXISTING SHADER: {shader.name} ======");
		}
		
		if (shader == null)
		{
			Debug.LogError("====== ALL SHADERS FAILED - MATERIAL WILL BE PINK ======");
			return;
		}
		
		_visualMaterial = new Material(shader);
		
		// Set semi-transparent red color (40% alpha for subtle warning)
		Color transparentRed = new Color(1f, 0.2f, 0.2f, 0.4f);
		_visualMaterial.color = transparentRed;
		if (_visualMaterial.HasProperty("_TintColor")) _visualMaterial.SetColor("_TintColor", transparentRed);
		if (_visualMaterial.HasProperty("_Color")) _visualMaterial.SetColor("_Color", transparentRed);
		
		// Ensure proper render queue for transparency
		_visualMaterial.renderQueue = 3000;
		
		// Apply material
		_visualRenderer.material = _visualMaterial;
		_visualRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		_visualRenderer.receiveShadows = false;
		
		// Initialize alpha to 0 (will fade in)
		_currentAlpha = 0f;
		
		Debug.LogError($"====== FINAL SHADER: {_visualMaterial.shader.name} ======");
		Debug.LogError($"====== MATERIAL COLOR: R={transparentRed.r} G={transparentRed.g} B={transparentRed.b} A={transparentRed.a} ======");
	}

		private void OnDestroy()
		{
			// Clean up created objects
			if (_bufferZoneVisual != null)
			{
				Destroy(_bufferZoneVisual);
			}
			if (_visualMaterial != null)
			{
				Destroy(_visualMaterial);
			}
		}
	}
}

