using UnityEngine;
using UnityEngine.UI;

namespace Points
{
	/// <summary>
	/// Simple connector script to wire up drone control menu buttons.
	/// Attach this to your DroneControlMenu canvas.
	/// </summary>
	public class DroneMenuConnector : MonoBehaviour
	{
		[Header("Button References")]
		[SerializeField] private Button _playButton;
		[SerializeField] private Button _pauseButton;
		[SerializeField] private Button _resetButton;

		[Header("Drone Reference")]
		[SerializeField] private DronePathFollower _droneFollower;

		private void Awake()
		{
			// Auto-find drone follower if not assigned
			if (_droneFollower == null)
			{
				_droneFollower = FindFirstObjectByType<DronePathFollower>();
			}

			// Connect button clicks
			if (_playButton != null)
			{
				_playButton.onClick.AddListener(OnPlayClicked);
			}

			if (_pauseButton != null)
			{
				_pauseButton.onClick.AddListener(OnPauseClicked);
			}

			if (_resetButton != null)
			{
				_resetButton.onClick.AddListener(OnResetClicked);
			}
		}

		private void OnPlayClicked()
		{
			if (_droneFollower != null)
			{
				_droneFollower.Play();
				Debug.Log("Drone: Play");
			}
			else
			{
				Debug.LogWarning("DroneMenuConnector: No DronePathFollower found!");
			}
		}

		private void OnPauseClicked()
		{
			if (_droneFollower != null)
			{
				_droneFollower.Pause();
				Debug.Log("Drone: Pause");
			}
		}

		private void OnResetClicked()
		{
			if (_droneFollower != null)
			{
				_droneFollower.ResetToStart();
				Debug.Log("Drone: ResetToStart");
			}
		}

		private void OnDestroy()
		{
			// Clean up listeners
			if (_playButton != null)
			{
				_playButton.onClick.RemoveListener(OnPlayClicked);
			}

			if (_pauseButton != null)
			{
				_pauseButton.onClick.RemoveListener(OnPauseClicked);
			}

			if (_resetButton != null)
			{
				_resetButton.onClick.RemoveListener(OnResetClicked);
			}
		}
	}
}

