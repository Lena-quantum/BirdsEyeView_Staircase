using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Points
{
	/// <summary>
	/// Provides a hovering UI panel with controls for the drone path follower.
	/// Includes Play, Pause, Restart buttons and a speed multiplier slider.
	/// </summary>
	public class DroneControlUI : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private DronePathFollower _droneFollower;
		[SerializeField] private Canvas _canvas;

		[Header("UI Elements")]
		[SerializeField] private Button _playButton;
		[SerializeField] private Button _pauseButton;
		[SerializeField] private Button _restartButton;
		[SerializeField] private Slider _speedSlider;
		[SerializeField] private TextMeshProUGUI _speedLabel;
		[SerializeField] private TextMeshProUGUI _statusLabel;

		[Header("UI Settings")]
		[SerializeField] private float _speedMin = 0.5f;
		[SerializeField] private float _speedMax = 5.0f;
		[SerializeField] private float _defaultSpeed = 1.0f;
		[SerializeField] private bool _snapToPresets = true; // Snap to common speeds (0.5, 1, 1.5, 2, 3, 5)
		[SerializeField] private float _snapTolerance = 0.15f;

		[Header("Positioning")]
		[SerializeField] private Transform _followTarget; // Camera or hand controller
		[SerializeField] private Vector3 _offsetFromTarget = new Vector3(0, 0, 1.5f); // 1.5m in front
		[SerializeField] private bool _billboardToCamera = true; // Face the camera
		[SerializeField] private float _positionSmoothing = 0.1f;

		private Camera _mainCamera;
		private Vector3 _targetPosition;

		private void Awake()
		{
			_mainCamera = Camera.main;

			if (_droneFollower == null)
			{
				_droneFollower = FindFirstObjectByType<DronePathFollower>();
			}

			if (_followTarget == null && _mainCamera != null)
			{
				_followTarget = _mainCamera.transform;
			}

			SetupUI();
		}

		private void Start()
		{
			InitializeSpeedSlider();
			UpdateUI();
		}

		private void Update()
		{
			UpdatePosition();
			UpdateUI();
		}

		/// <summary>
		/// Setup UI button listeners and initial state.
		/// </summary>
		private void SetupUI()
		{
			if (_playButton != null)
			{
				_playButton.onClick.AddListener(OnPlayClicked);
			}

			if (_pauseButton != null)
			{
				_pauseButton.onClick.AddListener(OnPauseClicked);
			}

			if (_restartButton != null)
			{
				_restartButton.onClick.AddListener(OnRestartClicked);
			}

			if (_speedSlider != null)
			{
				_speedSlider.minValue = _speedMin;
				_speedSlider.maxValue = _speedMax;
				_speedSlider.value = _defaultSpeed;
				_speedSlider.onValueChanged.AddListener(OnSpeedChanged);
			}
		}

		/// <summary>
		/// Initialize speed slider with preset values if snapping is enabled.
		/// </summary>
		private void InitializeSpeedSlider()
		{
			if (_speedSlider == null) return;

			if (_snapToPresets)
			{
				// Set slider to use whole numbers for presets
				_speedSlider.wholeNumbers = false; // Allow smooth sliding, but snap on release
			}
		}

		/// <summary>
		/// Update UI position to follow target.
		/// </summary>
		private void UpdatePosition()
		{
			if (_followTarget == null || _canvas == null) return;

			// Calculate target position in front of the follow target
			_targetPosition = _followTarget.position + _followTarget.forward * _offsetFromTarget.z
				+ _followTarget.up * _offsetFromTarget.y
				+ _followTarget.right * _offsetFromTarget.x;

			// Smooth movement
			_canvas.transform.position = Vector3.Lerp(_canvas.transform.position, _targetPosition, _positionSmoothing);

			// Billboard to camera
			if (_billboardToCamera && _mainCamera != null)
			{
				_canvas.transform.LookAt(_canvas.transform.position + _mainCamera.transform.rotation * Vector3.forward,
					_mainCamera.transform.rotation * Vector3.up);
			}
		}

		/// <summary>
		/// Update UI button states and labels based on drone follower state.
		/// </summary>
		private void UpdateUI()
		{
			if (_droneFollower == null) return;

			// Update button interactability
			bool isPlaying = _droneFollower.IsFlying;
			bool isPaused = _droneFollower.IsPaused;

			if (_playButton != null)
			{
				_playButton.interactable = !isPlaying; // Disable when playing
			}

			if (_pauseButton != null)
			{
				_pauseButton.interactable = isPlaying; // Only enable when playing
			}

			if (_restartButton != null)
			{
				_restartButton.interactable = isPlaying || isPaused; // Enable when playing or paused
			}

			// Update status label
			if (_statusLabel != null)
			{
				string status = "Idle";
				if (isPlaying) status = "Playing";
				else if (isPaused) status = "Paused";

				_statusLabel.text = $"Status: {status}";
			}

			// Update speed label
			if (_speedLabel != null && _speedSlider != null)
			{
				float speed = _speedSlider.value;
				_speedLabel.text = $"Speed: {speed:F1}x";
			}
		}

		/// <summary>
		/// Handle play button click.
		/// </summary>
		private void OnPlayClicked()
		{
			if (_droneFollower != null)
			{
				_droneFollower.Play();
			}
		}

		/// <summary>
		/// Handle pause button click.
		/// </summary>
		private void OnPauseClicked()
		{
			if (_droneFollower != null)
			{
				_droneFollower.Pause();
			}
		}

		/// <summary>
		/// Handle restart button click.
		/// </summary>
		private void OnRestartClicked()
		{
			if (_droneFollower != null)
			{
				_droneFollower.Restart();
			}
		}

		/// <summary>
		/// Handle speed slider value change.
		/// </summary>
		private void OnSpeedChanged(float value)
		{
			// Apply speed multiplier to drone follower
			if (_droneFollower != null)
			{
				// Snap to preset values if enabled
				if (_snapToPresets)
				{
					float snappedValue = SnapToPreset(value);
					if (Mathf.Abs(value - snappedValue) < _snapTolerance)
					{
						_speedSlider.value = snappedValue;
						value = snappedValue;
					}
				}

				_droneFollower.SpeedMultiplier = value;
			}

			// Update label
			if (_speedLabel != null)
			{
				_speedLabel.text = $"Speed: {value:F1}x";
			}
		}

		/// <summary>
		/// Snap speed value to nearest preset.
		/// </summary>
		private float SnapToPreset(float value)
		{
			float[] presets = { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 4.0f, 5.0f };
			float nearest = presets[0];
			float minDistance = Mathf.Abs(value - nearest);

			foreach (float preset in presets)
			{
				float distance = Mathf.Abs(value - preset);
				if (distance < minDistance)
				{
					minDistance = distance;
					nearest = preset;
				}
			}

			return nearest;
		}

		/// <summary>
		/// Set the target to follow (e.g., camera or controller).
		/// </summary>
		public void SetFollowTarget(Transform target)
		{
			_followTarget = target;
		}

		/// <summary>
		/// Show or hide the UI.
		/// </summary>
		public void SetVisible(bool visible)
		{
			if (_canvas != null)
			{
				_canvas.gameObject.SetActive(visible);
			}
		}
	}
}
