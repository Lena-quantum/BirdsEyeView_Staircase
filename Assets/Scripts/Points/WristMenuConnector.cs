using UnityEngine;
using UnityEngine.UI;

namespace Points
{
	/// <summary>
	/// Connects the manually created WristUICanvas buttons to the waypoint type selection system.
	/// Attach this script to your WristUICanvas GameObject.
	/// Indoor flight optimized with 2 waypoint types.
	/// </summary>
	public class WristMenuConnector : MonoBehaviour
	{
		[Header("Button References - Assign in Inspector")]
		[SerializeField] private Button _stopTurnGoButton;
		[SerializeField] private Button _recordButton;

		[Header("References - Auto-found")]
		[SerializeField] private PointPlacementManager _pointManager;

		private void Start()
		{
			// Find point manager
			if (_pointManager == null)
			{
				_pointManager = UnityEngine.Object.FindFirstObjectByType<PointPlacementManager>();
			}

			if (_pointManager == null)
			{
				Debug.LogError("WristMenuConnector: PointPlacementManager not found!");
				return;
			}

		// Connect button events
		SetupButtonListeners();
		
		// Set initial selection to StopTurnGo (default waypoint type)
		SelectType(WaypointType.StopTurnGo);
		
		Debug.Log("WristMenuConnector: Buttons connected to waypoint type system (2 types)");
		}

	private void SetupButtonListeners()
	{
		if (_stopTurnGoButton != null)
		{
			_stopTurnGoButton.onClick.AddListener(() => SelectType(WaypointType.StopTurnGo));
			Debug.Log("WristMenuConnector: Connected Stop-Turn-Go button");
		}
		else
		{
			Debug.LogWarning("WristMenuConnector: Stop-Turn-Go button not assigned!");
		}

		if (_recordButton != null)
		{
			_recordButton.onClick.AddListener(() => SelectType(WaypointType.Record360));
			Debug.Log("WristMenuConnector: Connected Record360 button");
		}
		else
		{
			Debug.LogWarning("WristMenuConnector: Record360 button not assigned!");
		}
	}

		/// <summary>
		/// Select a waypoint type - changes ghost color and future waypoint placements.
		/// </summary>
		public void SelectType(WaypointType type)
		{
			if (_pointManager == null) return;

			_pointManager.CurrentTypeSelection = type;
			
			Debug.Log($"WristMenuConnector: Selected type {WaypointTypeDefinition.GetTypeName(type)} - Color: {WaypointTypeDefinition.GetTypeColor(type)}");
			
			// Visual feedback - highlight selected button
			UpdateButtonVisuals(type);
		}

	private void UpdateButtonVisuals(WaypointType selectedType)
	{
		// Update button colors to show selection (2 waypoint types)
		UpdateButton(_stopTurnGoButton, WaypointType.StopTurnGo, selectedType);
		UpdateButton(_recordButton, WaypointType.Record360, selectedType);
	}

		private void UpdateButton(Button button, WaypointType buttonType, WaypointType selectedType)
		{
			if (button == null) return;

			var image = button.GetComponent<Image>();
			if (image == null) return;

			Color baseColor = WaypointTypeDefinition.GetTypeColor(buttonType);
			
			if (buttonType == selectedType)
			{
				// Brighten selected button
				image.color = baseColor;
			}
			else
			{
				// Dim unselected buttons
				image.color = baseColor * 0.6f;
			}
		}

	// Public methods for manual button OnClick() assignment in Inspector
	public void SelectStopTurnGo()
	{
		SelectType(WaypointType.StopTurnGo);
	}

	public void SelectRecord360()
	{
		SelectType(WaypointType.Record360);
	}
	}
}

