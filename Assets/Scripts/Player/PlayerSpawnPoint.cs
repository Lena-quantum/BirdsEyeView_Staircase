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

			// Wait for XR system to fully initialize, then position
			StartCoroutine(PositionAfterXRInit());
		}

		private System.Collections.IEnumerator PositionAfterXRInit()
		{
			// Wait for XR to initialize (multiple frames)
			for (int i = 0; i < 3; i++)
			{
				yield return null;
			}

			// Use MoveCameraToWorldLocation which properly handles floor offset
			// But we need to adjust the target Y to account for the camera's height above the origin
			Vector3 targetCameraPos = _spawnTransform.position;
			
			// Get the camera's current local Y offset from origin
			Transform cameraTransform = _xrOrigin.Camera.transform;
			if (_xrOrigin.CameraFloorOffsetObject != null)
			{
				cameraTransform = _xrOrigin.CameraFloorOffsetObject.transform;
			}
			
			Vector3 cameraLocalPos = _xrOrigin.transform.InverseTransformPoint(cameraTransform.position);
			float cameraLocalY = cameraLocalPos.y;
			
			// Adjust target position: if camera is 1.6m above origin, we need to move origin down by that amount
			// so camera ends up at spawn Y
			targetCameraPos.y -= cameraLocalY;
			
			// Move the camera to the adjusted position
			_xrOrigin.MoveCameraToWorldLocation(targetCameraPos);
			_xrOrigin.MatchOriginUpCameraForward(Vector3.up, _spawnTransform.forward);
			
			// Wait and verify
			yield return null;
			
			Vector3 finalCameraPos = cameraTransform.position;
			float error = Mathf.Abs(_spawnTransform.position.y - finalCameraPos.y);
			
			if (error > 0.05f)
			{
				// Fine-tune: directly adjust origin position
				float correction = _spawnTransform.position.y - finalCameraPos.y;
				_xrOrigin.transform.position += Vector3.up * correction;
			}
		}
	}
}

