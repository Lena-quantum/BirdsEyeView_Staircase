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
	[SerializeField] private Button _flythroughButton; // Yellow "Flythrough Waypoint" button
	[SerializeField] private Button _recordButton; // Red "Record" button

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
		
		// Set initial selection to StopTurnGo (yellow waypoint type - default)
		SelectType(WaypointType.StopTurnGo);
		
		Debug.Log("WristMenuConnector: Buttons connected to waypoint type system (2 types)");
		}

	private void SetupButtonListeners()
	{
		if (_flythroughButton != null)
		{
			_flythroughButton.onClick.AddListener(() => 
			{
				Debug.Log("WristMenuConnector: Flythrough button clicked!");
				SelectType(WaypointType.StopTurnGo);
			});
			Debug.Log("WristMenuConnector: Connected Flythrough Waypoint button (yellow) -> StopTurnGo");
		}
		else
		{
			Debug.LogWarning("WristMenuConnector: Flythrough Waypoint button not assigned!");
		}

		if (_recordButton != null)
		{
			_recordButton.onClick.AddListener(() => 
			{
				Debug.Log("WristMenuConnector: Record button clicked!");
				SelectType(WaypointType.Record360);
			});
			Debug.Log("WristMenuConnector: Connected Record button (red)");
		}
		else
		{
			Debug.LogWarning("WristMenuConnector: Record button not assigned!");
		}
	}

		/// <summary>
		/// Select a waypoint type - changes ghost color and future waypoint placements.
		/// </summary>
		public void SelectType(WaypointType type)
		{
			if (_pointManager == null)
			{
				Debug.LogError("WristMenuConnector: PointPlacementManager is null! Cannot change waypoint type.");
				return;
			}

			Debug.Log($"WristMenuConnector: Setting waypoint type to {type} (was {_pointManager.CurrentTypeSelection})");
			_pointManager.CurrentTypeSelection = type;
			
			Debug.Log($"WristMenuConnector: Selected type {WaypointTypeDefinition.GetTypeName(type)} - Color: {WaypointTypeDefinition.GetTypeColor(type)}");
			Debug.Log($"WristMenuConnector: PointManager.CurrentTypeSelection is now {_pointManager.CurrentTypeSelection}");
			
			// Visual feedback - highlight selected button
			UpdateButtonVisuals(type);
		}

	private void UpdateButtonVisuals(WaypointType selectedType)
	{
		// Update button colors to show selection (Flythrough button maps to StopTurnGo)
		UpdateButton(_flythroughButton, WaypointType.StopTurnGo, selectedType);
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
	public void SelectFlythrough()
	{
		SelectType(WaypointType.StopTurnGo);
	}

	public void SelectRecord360()
	{
		SelectType(WaypointType.Record360);
	}
	}
}

