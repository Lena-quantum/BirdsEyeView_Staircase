using Unity.XR.CoreUtils;
using UnityEngine;

namespace Player
{
	/// <summary>
	/// Ensures the XR Origin starts at a fixed spawn transform (position + orientation).
	/// </summary>
	public class PlayerSpawnPoint : MonoBehaviour
	{
		[SerializeField] private XROrigin _xrOrigin;
		[SerializeField] private Transform _spawnTransform;

		private void Awake()
		{
			if (_spawnTransform == null)
			{
				_spawnTransform = transform;
			}

			if (_xrOrigin == null)
			{
				_xrOrigin = FindFirstObjectByType<XROrigin>();
			}
		}

		private void Start()
		{
			if (_xrOrigin == null || _spawnTransform == null)
			{
				Debug.LogWarning("PlayerSpawnPoint could not find XROrigin or spawn transform.");
				return;
			}

			// Move the rig so the camera starts at the desired world position/height.
			_xrOrigin.MoveCameraToWorldLocation(_spawnTransform.position);
			_xrOrigin.MatchOriginUpCameraForward(Vector3.up, _spawnTransform.forward);
		}
	}
}

