using UnityEngine;

namespace Points
{
	/// <summary>
	/// Defines the types of waypoints available in the VR drone flight path system.
	/// Simplified for thesis study with 3 distinct behaviors.
	/// </summary>
	public enum WaypointType
	{
		/// <summary>
		/// Standard navigation waypoint - drone flies through without stopping.
		/// </summary>
		Flythrough = 0,

		/// <summary>
		/// Drone stops, rotates to observe, then continues flying.
		/// </summary>
		StopRotateContinue = 1,

		/// <summary>
		/// Drone stops and performs a slow 360-degree rotation for recording.
		/// </summary>
		Record360 = 2
	}

	/// <summary>
	/// Provides metadata and visual settings for each waypoint type.
	/// </summary>
	public static class WaypointTypeDefinition
	{
		/// <summary>
		/// Get the display name for a waypoint type.
		/// </summary>
		public static string GetTypeName(WaypointType type)
		{
			switch (type)
			{
				case WaypointType.Flythrough: return "Flythrough";
				case WaypointType.StopRotateContinue: return "Stop to Rotate";
				case WaypointType.Record360: return "Record 360°";
				default: return "Unknown";
			}
		}

		/// <summary>
		/// Get the color associated with a waypoint type for visual differentiation.
		/// </summary>
		public static Color GetTypeColor(WaypointType type)
		{
			switch (type)
			{
				case WaypointType.Flythrough: return Color.yellow;              // Yellow - standard navigation
				case WaypointType.StopRotateContinue: return Color.cyan;        // Cyan - stop & rotate
				case WaypointType.Record360: return Color.red;                  // Red - 360° recording
				default: return Color.white;
			}
		}

		/// <summary>
		/// Get a short description of what the waypoint type does.
		/// </summary>
		public static string GetTypeDescription(WaypointType type)
		{
			switch (type)
			{
				case WaypointType.Flythrough:
					return "Drone flies through without stopping";
				case WaypointType.StopRotateContinue:
					return "Drone stops, rotates to observe, then continues";
				case WaypointType.Record360:
					return "Drone stops and rotates 360° slowly for recording";
				default:
					return "Unknown waypoint type";
			}
		}

		/// <summary>
		/// Get the required parameters for a waypoint type.
		/// Returns empty array if no parameters required.
		/// </summary>
		public static string[] GetRequiredParameters(WaypointType type)
		{
			switch (type)
			{
				case WaypointType.Flythrough:
					return new string[] { }; // No parameters

				case WaypointType.StopRotateContinue:
					return new string[] { "rotation_degrees" }; // Future: how much to rotate

				case WaypointType.Record360:
					return new string[] { "duration_s" }; // Future: how long the 360 takes

				default:
					return new string[] { };
			}
		}

		/// <summary>
		/// Get default parameter values for a waypoint type.
		/// </summary>
		public static System.Collections.Generic.Dictionary<string, object> GetDefaultParameters(WaypointType type)
		{
			var defaults = new System.Collections.Generic.Dictionary<string, object>();

			switch (type)
			{
				case WaypointType.Flythrough:
					// No parameters
					break;

				case WaypointType.StopRotateContinue:
					defaults["rotation_degrees"] = 90.0f; // Future: default 90° rotation
					break;

				case WaypointType.Record360:
					defaults["duration_s"] = 15.0f; // Future: 15 seconds for full 360°
					break;
			}

			return defaults;
		}

		/// <summary>
		/// Validate that parameters are present for a waypoint type.
		/// Returns true if all required parameters are present.
		/// </summary>
		public static bool ValidateParameters(WaypointType type, System.Collections.Generic.Dictionary<string, object> parameters)
		{
			if (parameters == null)
			{
				parameters = new System.Collections.Generic.Dictionary<string, object>();
			}

			string[] required = GetRequiredParameters(type);
			foreach (string param in required)
			{
				if (!parameters.ContainsKey(param))
				{
					Debug.LogWarning($"MISSING_PARAM_{param} for waypoint type {type}");
					return false;
				}
			}

			return true;
		}
	}
}

