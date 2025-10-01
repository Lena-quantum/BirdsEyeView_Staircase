using UnityEngine;
using UnityEngine.XR;

namespace Points
{
	/// <summary>
	/// Sends simple haptic impulses to the right-hand controller using XR InputDevices.
	/// </summary>
	public class HapticsHelper : MonoBehaviour
	{
		[SerializeField] private float _defaultAmplitude = 0.2f;
		[SerializeField] private float _defaultDuration = 0.02f;

		/// <summary>
		/// Send a light tick haptic.
		/// </summary>
		public void Tick(float amplitude = -1f, float duration = -1f)
		{
			float amp = amplitude >= 0f ? amplitude : _defaultAmplitude;
			float dur = duration >= 0f ? duration : _defaultDuration;
			Send(amp, dur);
		}

		/// <summary>
		/// Send a stronger confirmation pulse.
		/// </summary>
		public void Pulse(float amplitude = 0.6f, float duration = 0.08f)
		{
			Send(amplitude, duration);
		}

		private static void Send(float amplitude, float duration)
		{
			var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
			if (!right.isValid) return;
			HapticCapabilities caps;
			if (right.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
			{
				uint channel = 0;
				right.SendHapticImpulse(channel, Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
			}
		}
	}
}


