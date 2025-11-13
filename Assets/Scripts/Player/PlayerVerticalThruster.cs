using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace Player
{
	/// <summary>
	/// Adds vertical locomotion mapped to the left controller's Y (up) and X (down) buttons.
	/// Keeps existing locomotion untouched and only offsets the XR Origin in world space.
	/// </summary>
	public class PlayerVerticalThruster : MonoBehaviour
	{
		[SerializeField] private XROrigin _xrOrigin;
		[SerializeField] private float _verticalSpeed = 1.5f; // meters per second
		[SerializeField] private CharacterController _characterController;

		private InputDevice _leftHand;

		private void Awake()
		{
			if (_xrOrigin == null)
			{
				_xrOrigin = GetComponent<XROrigin>();
			}

			if (_xrOrigin == null)
			{
				_xrOrigin = FindFirstObjectByType<XROrigin>();
			}

			if (_characterController == null && _xrOrigin != null)
			{
				_characterController = _xrOrigin.GetComponent<CharacterController>();
			}
		}

		private void OnEnable()
		{
			InputDevices.deviceConnected += OnDeviceConnected;
			_leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
		}

		private void OnDisable()
		{
			InputDevices.deviceConnected -= OnDeviceConnected;
		}

		private void OnDeviceConnected(InputDevice device)
		{
			if ((device.characteristics & (InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller)) != 0)
			{
				_leftHand = device;
			}
		}

		private void Update()
		{
			if (_xrOrigin == null)
			{
				return;
			}

			_leftHand = EnsureDevice(_leftHand, XRNode.LeftHand);
			if (!_leftHand.isValid)
			{
				return;
			}

			bool ascendPressed = ReadButton(_leftHand, CommonUsages.secondaryButton); // Y button on left controller
			bool descendPressed = ReadButton(_leftHand, CommonUsages.primaryButton);   // X button on left controller

			float direction = 0f;
			if (ascendPressed) direction += 1f;
			if (descendPressed) direction -= 1f;

			if (Mathf.Approximately(direction, 0f))
			{
				return;
			}

			float deltaY = direction * _verticalSpeed * Time.deltaTime;
			MoveRigVertically(deltaY);
		}

		private void MoveRigVertically(float amount)
		{
			if (_characterController != null && _characterController.enabled)
			{
				Vector3 motion = new Vector3(0f, amount, 0f);
				_characterController.Move(motion);
				return;
			}

			Transform originTransform = _xrOrigin.Origin != null ? _xrOrigin.Origin.transform : _xrOrigin.transform;
			if (originTransform == null)
			{
				return;
			}

			originTransform.position += new Vector3(0f, amount, 0f);
		}

		private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
		{
			return device.isValid && device.TryGetFeatureValue(usage, out bool value) && value;
		}

		private static InputDevice EnsureDevice(InputDevice device, XRNode node)
		{
			if (device.isValid) return device;
			return InputDevices.GetDeviceAtXRNode(node);
		}
	}
}

