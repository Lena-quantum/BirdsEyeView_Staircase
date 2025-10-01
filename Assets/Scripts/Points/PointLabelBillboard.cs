using System.Collections;
using UnityEngine;

namespace Points
{
	/// <summary>
	/// Simple billboard text that faces the main camera and can fade out.
	/// </summary>
	public class PointLabelBillboard : MonoBehaviour
	{
		[SerializeField] private TextMesh _textMesh;
		[SerializeField] private float _fadeDuration = 0.35f;

		private Coroutine _fadeRoutine;

		/// <summary>
		/// Set the displayed text.
		/// </summary>
		public void SetText(string text)
		{
			if (_textMesh != null)
			{
				_textMesh.text = text;
			}
		}

		private void LateUpdate()
		{
			var cam = Camera.main;
			if (cam == null) return;
			transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
		}

		/// <summary>
		/// Fades the label out over the configured duration.
		/// </summary>
		public void FadeOut()
		{
			if (_fadeRoutine != null)
			{
				StopCoroutine(_fadeRoutine);
			}
			_fadeRoutine = StartCoroutine(FadeRoutine());
		}

		private IEnumerator FadeRoutine()
		{
			if (_textMesh == null) yield break;
			Color start = _textMesh.color;
			float t = 0f;
			while (t < _fadeDuration)
			{
				t += Time.deltaTime;
				float a = Mathf.Lerp(start.a, 0f, Mathf.Clamp01(t / _fadeDuration));
				_textMesh.color = new Color(start.r, start.g, start.b, a);
				yield return null;
			}
		}
	}
}


