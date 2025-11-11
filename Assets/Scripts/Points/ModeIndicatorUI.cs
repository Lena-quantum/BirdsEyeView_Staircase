using UnityEngine;

namespace Points
{
	/// <summary>
	/// Updates the wrist mode UI so the active state matches the current placement/path mode.
	/// </summary>
	public class ModeIndicatorUI : MonoBehaviour
	{
		[SerializeField] private FlightPathManager _pathManager;

		[Header("Point Mode Visuals")]
		[SerializeField] private GameObject _pointActiveRoot;
		[SerializeField] private GameObject _pointInactiveRoot;

		[Header("Path Mode Visuals")]
		[SerializeField] private GameObject _pathActiveRoot;
		[SerializeField] private GameObject _pathInactiveRoot;

		private void Awake()
		{
			if (_pathManager == null)
			{
				_pathManager = FindFirstObjectByType<FlightPathManager>();
			}
		}

		private void OnEnable()
		{
			if (_pathManager != null)
			{
				_pathManager.OnPathModeChanged += HandlePathModeChanged;
				UpdateVisuals(_pathManager.PathModeEnabled);
			}
			else
			{
				UpdateVisuals(false);
			}
		}

		private void OnDisable()
		{
			if (_pathManager != null)
			{
				_pathManager.OnPathModeChanged -= HandlePathModeChanged;
			}
		}

		private void HandlePathModeChanged(bool pathModeEnabled)
		{
			UpdateVisuals(pathModeEnabled);
		}

		private void UpdateVisuals(bool pathModeEnabled)
		{
			if (_pointActiveRoot != null) _pointActiveRoot.SetActive(!pathModeEnabled);
			if (_pointInactiveRoot != null) _pointInactiveRoot.SetActive(pathModeEnabled);

			if (_pathActiveRoot != null) _pathActiveRoot.SetActive(pathModeEnabled);
			if (_pathInactiveRoot != null) _pathInactiveRoot.SetActive(!pathModeEnabled);
		}
	}
}

