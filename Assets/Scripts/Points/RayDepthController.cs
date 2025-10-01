using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace Points
{
	/// <summary>
	/// Controls the depth along the right-hand ray using XR InputDevices and updates ghost/readout.
	/// </summary>
	public class RayDepthController : MonoBehaviour
	{
		[SerializeField] private PointPlacementManager _manager;
		[SerializeField] private Transform _rightControllerTransform;
		[SerializeField] private Transform _leftControllerTransform;
		[SerializeField] private Transform _ghostTransform;
		[SerializeField] private LineRenderer _rayLine;
		[SerializeField] private LayerMask _raycastMask = ~0;
		[SerializeField] private float _defaultDepth = 2.0f;
		[SerializeField] private float _readoutFadeDelay = 0.5f;
		[SerializeField] private float _repeatDelay = 0.25f;
		[SerializeField] private FlightPathManager _pathManager;

		private float _currentDepth;
		private bool _triggerPrev;
		private bool _aPrev;
		private bool _bPrev;
		private bool _leftTriggerPrev;
		private float _nextRepeatTimeA;
		private float _nextRepeatTimeB;
		private InputDevice _rightHand;
		private InputDevice _leftHand;

		private void OnEnable()
		{
			_currentDepth = Mathf.Clamp(_defaultDepth, _manager != null ? _manager.MinDepth : 0.2f, _manager != null ? _manager.MaxDepth : 10f);
			_rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
			_leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
		}

		private void Start()
		{
			if (_pathManager == null)
			{
				_pathManager = UnityEngine.Object.FindFirstObjectByType<FlightPathManager>();
			}
		}

		private void Update()
		{
			if (_manager == null || _ghostTransform == null || _rightControllerTransform == null)
			{
				return;
			}

			_rightHand = EnsureDevice(_rightHand, XRNode.RightHand);
			_leftHand = EnsureDevice(_leftHand, XRNode.LeftHand);

			// DISABLED: Precision mode via grip to avoid conflicts
			// bool precision = ReadButton(_rightHand, CommonUsages.gripButton);
			// float precisionMul = precision ? _manager.PrecisionMultiplier : 1f;
			float precisionMul = 1f; // Always use normal precision for now

			// DISABLED: Thumbstick depth control to avoid conflicts with XR Rig
			// Vector2 stick;
			// if (_rightHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick))
			// {
			// 	float delta = stick.y * (_manager.DepthSpeed * precisionMul) * Time.deltaTime;
			// 	_currentDepth = Mathf.Clamp(_currentDepth + delta, _manager.MinDepth, _manager.MaxDepth);
			// }

			bool aBtn = ReadButton(_rightHand, CommonUsages.primaryButton);
			bool bBtn = ReadButton(_rightHand, CommonUsages.secondaryButton);
			if (EdgePressed(aBtn, ref _aPrev) || (aBtn && Time.time >= _nextRepeatTimeA))
			{
				_currentDepth = Mathf.Clamp(_currentDepth + _manager.DepthStep * precisionMul, _manager.MinDepth, _manager.MaxDepth);
				_manager.TickHaptics();
				_nextRepeatTimeA = Time.time + _repeatDelay;
			}
			if (EdgePressed(bBtn, ref _bPrev) || (bBtn && Time.time >= _nextRepeatTimeB))
			{
				_currentDepth = Mathf.Clamp(_currentDepth - _manager.DepthStep * precisionMul, _manager.MinDepth, _manager.MaxDepth);
				_manager.TickHaptics();
				_nextRepeatTimeB = Time.time + _repeatDelay;
			}

			Vector3 origin = _rightControllerTransform.position;
			Vector3 dir = _rightControllerTransform.forward;

			// DISABLED: Left grip for surface snapping to avoid conflicts
			// bool leftGrip = ReadButton(_leftHand, CommonUsages.gripButton);
			// bool useSnap = _manager.SurfaceSnappingEnabled && !leftGrip;
			bool useSnap = _manager.SurfaceSnappingEnabled;
			bool snapped = false;
			Vector3 ghostPos = origin + dir * _currentDepth;
			if (useSnap)
			{
				RaycastHit hit;
				if (Physics.Raycast(origin, dir, out hit, _currentDepth + 0.01f, _raycastMask, QueryTriggerInteraction.Ignore))
				{
					ghostPos = hit.point;
					snapped = true;
				}
			}

			_ghostTransform.position = ghostPos;
			bool valid = _currentDepth >= _manager.MinDepth && _currentDepth <= _manager.MaxDepth;
			_manager.UpdateGhostVisualValidity(valid && (!useSnap || snapped || Mathf.Abs(_currentDepth - Mathf.Clamp(_currentDepth, _manager.MinDepth, _manager.MaxDepth)) < 0.0001f));
			_manager.UpdateReadout($"{_currentDepth:F2} m");

			if (_rayLine != null)
			{
				_rayLine.positionCount = 2;
				_rayLine.SetPosition(0, origin);
				_rayLine.SetPosition(1, origin + dir * Mathf.Min(_currentDepth, 10f));
			}

			// Handle right trigger for point placement
			bool trigger = ReadButton(_rightHand, CommonUsages.triggerButton);
			if (EdgePressed(trigger, ref _triggerPrev))
			{
				if (valid)
				{
					// Check if we're in path mode
					if (_pathManager != null && _pathManager.PathModeEnabled)
					{
						// In path mode, try to select an existing point instead of placing new one
						HandlePathModeTrigger();
					}
					else
					{
						// Normal point placement mode
						_manager.PlaceAtCurrentGhost();
						_manager.ConfirmHaptics();
						StopAllCoroutines();
						StartCoroutine(FadeReadoutRoutine());
					}
				}
			}

			// Handle left trigger for point removal
			bool leftTrigger = ReadButton(_leftHand, CommonUsages.triggerButton);
			if (EdgePressed(leftTrigger, ref _leftTriggerPrev))
			{
				HandlePointRemoval();
			}
		}

		private IEnumerator FadeReadoutRoutine()
		{
			yield return new WaitForSeconds(_readoutFadeDelay);
			_manager.FadeReadout();
		}

		private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
		{
			if (!device.isValid) return false;
			bool v;
			return device.TryGetFeatureValue(usage, out v) && v;
		}

		private static bool EdgePressed(bool current, ref bool prev)
		{
			bool pressed = current && !prev;
			prev = current;
			return pressed;
		}

		private static InputDevice EnsureDevice(InputDevice device, XRNode node)
		{
			if (!device.isValid)
			{
				device = InputDevices.GetDeviceAtXRNode(node);
			}
			return device;
		}

		/// <summary>
		/// Handle trigger input when in path mode - try to select existing points.
		/// </summary>
		private void HandlePathModeTrigger()
		{
			Vector3 origin = _rightControllerTransform.position;
			Vector3 dir = _rightControllerTransform.forward;

			// Raycast to find point handles
			if (Physics.Raycast(origin, dir, out RaycastHit hit, _currentDepth + 0.5f))
			{
				var pointHandle = hit.collider.GetComponent<PointHandle>();
				if (pointHandle != null)
				{
					// Select the point for path building
					_manager.NotifySelected(pointHandle);
					_manager.ConfirmHaptics();
					StopAllCoroutines();
					StartCoroutine(FadeReadoutRoutine());
					return;
				}
			}

			// If no point hit, still provide haptic feedback but don't place a point
			_manager.TickHaptics(0.1f, 0.02f);
		}

		/// <summary>
		/// Handle left trigger for point removal - hover over a point and press left trigger to remove it.
		/// </summary>
		private void HandlePointRemoval()
		{
			Vector3 origin = _rightControllerTransform.position;
			Vector3 dir = _rightControllerTransform.forward;

			// Raycast to find point handles
			if (Physics.Raycast(origin, dir, out RaycastHit hit, _currentDepth + 0.5f))
			{
				var pointHandle = hit.collider.GetComponent<PointHandle>();
				if (pointHandle != null)
				{
					// Remove the point
					_manager.RemovePoint(pointHandle.Id);
					_manager.ConfirmHaptics();
					StopAllCoroutines();
					StartCoroutine(FadeReadoutRoutine());
					return;
				}
			}

			// If no point hit, provide feedback that nothing was removed
			_manager.TickHaptics(0.1f, 0.02f);
		}

		/// <summary>
		/// Update readout text based on current mode.
		/// </summary>
		public void UpdateModeReadout()
		{
			if (_manager == null) return;

			string readoutText = $"{_currentDepth:F2} m";
			
			if (_pathManager != null && _pathManager.PathModeEnabled)
			{
				readoutText += " [PATH MODE]";
				var activeRoute = _pathManager.ActiveRoute;
				if (activeRoute != null)
				{
					readoutText += $" - {activeRoute.RouteName} ({activeRoute.PointCount})";
				}
			}

			_manager.UpdateReadout(readoutText);
		}
	}
}


