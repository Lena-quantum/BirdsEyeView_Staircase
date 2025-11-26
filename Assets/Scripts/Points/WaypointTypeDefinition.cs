using UnityEngine;

namespace Points
{
	/// <summary>
	/// Defines the types of waypoints available in the VR drone flight path system.
	/// Optimized for indoor flight with 2 distinct behaviors.
	/// </summary>
	public enum WaypointType
	{
		/// <summary>
		/// Standard waypoint - drone stops, rotates to observe, then continues.
		/// This is the default/normal waypoint behavior for indoor navigation.
		/// </summary>
		StopTurnGo = 0,

		/// <summary>
		/// Drone stops and performs a slow 360-degree rotation for recording.
		/// Uses two-point system: anchor position + elevated recording position.
		/// </summary>
		Record360 = 1
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
				case WaypointType.StopTurnGo: return "Stop-Turn-Go";
				case WaypointType.Record360: return "Record 360°";
				default: return "Unknown";
			}
		}

		/// <summary>
		/// Get the color associated with a waypoint type for visual differentiation.
		/// Custom colors specified for thesis study.
		/// </summary>
		public static Color GetTypeColor(WaypointType type)
		{
			switch (type)
			{
				case WaypointType.StopTurnGo:
					return HexToColor("F9FF00"); // Bright yellow - flythrough waypoint
				
				case WaypointType.Record360:
					return HexToColor("F74429"); // Orange-red - 360° recording
				
				default:
					return Color.white;
			}
		}

		/// <summary>
		/// Convert hex color string to Unity Color.
		/// </summary>
		private static Color HexToColor(string hex)
		{
			// Remove # if present
			hex = hex.TrimStart('#');
			
			// Parse RGB values
			if (hex.Length == 6)
			{
				byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
				byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
				byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
				
				return new Color(r / 255f, g / 255f, b / 255f);
			}
			
			return Color.white; // Fallback
		}

		/// <summary>
		/// Get a short description of what the waypoint type does.
		/// </summary>
		public static string GetTypeDescription(WaypointType type)
		{
			switch (type)
			{
				case WaypointType.StopTurnGo:
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
				case WaypointType.StopTurnGo:
					return new string[] { "rotation_degrees" }; // How much to rotate

				case WaypointType.Record360:
					return new string[] { "duration_s" }; // How long the 360 takes

				default:
					return new string[] { };
			}
		}

		/// <summary>
		/// Get default parameter values for a waypoint type.
		/// Indoor flight optimized with 25cm precision tolerance.
		/// </summary>
		public static System.Collections.Generic.Dictionary<string, object> GetDefaultParameters(WaypointType type)
		{
			var defaults = new System.Collections.Generic.Dictionary<string, object>();

			switch (type)
			{
				case WaypointType.StopTurnGo:
					// Standard waypoint for indoor navigation
					defaults["acceptance_radius"] = 0.25f;     // 25cm - indoor precision
					defaults["hold_time"] = 2.0f;              // 2 seconds at waypoint
					defaults["rotation_degrees"] = 0.0f;       // Optional rotation angle
					defaults["speed_ms"] = 0.5f;               // Slow indoor speed (0.5 m/s)
					break;

				case WaypointType.Record360:
					// Recording waypoint with 360° rotation
					defaults["duration_s"] = 15.0f;            // 15s recording time
					defaults["acceptance_radius"] = 0.25f;     // 25cm precision
					defaults["hold_time"] = 15.0f;             // Hold during recording
					defaults["speed_ms"] = 0.3f;               // Very slow approach
					defaults["rotation_speed_deg_s"] = 10.0f;  // 10 deg/s = 36s for 360°
					defaults["recording_height_offset"] = 0.5f; // Default 0.5m above anchor
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

