using System.Collections;
using UnityEngine;

namespace Points
{
	/// <summary>
	/// Simple billboard text that faces the main camera and can fade out.
	/// Supports both TextMesh and TextMeshPro.
	/// </summary>
	public class PointLabelBillboard : MonoBehaviour
	{
		[SerializeField] private TextMesh _textMesh;
		[SerializeField] private TMPro.TextMeshPro _textMeshPro;
		[SerializeField] private float _fadeDuration = 0.35f;

		private Coroutine _fadeRoutine;

	private void Awake()
	{
		// Auto-wire the TextMesh if not assigned
		if (_textMesh == null)
		{
			_textMesh = GetComponent<TextMesh>();
		}
		
		// Auto-wire TextMeshPro if not assigned
		if (_textMeshPro == null)
		{
			_textMeshPro = GetComponent<TMPro.TextMeshPro>();
		}
	}

	/// <summary>
	/// Set the displayed text.
	/// </summary>
	public void SetText(string text)
	{
		// Stop any fade routine
		if (_fadeRoutine != null)
		{
			StopCoroutine(_fadeRoutine);
			_fadeRoutine = null;
		}
		
		if (_textMesh != null)
		{
			var c = _textMesh.color;
			_textMesh.color = new Color(c.r, c.g, c.b, 1f);
			_textMesh.text = text;
		}
		
		if (_textMeshPro != null)
		{
			var c = _textMeshPro.color;
			_textMeshPro.color = new Color(c.r, c.g, c.b, 1f);
			_textMeshPro.text = text;
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
		Color startColor = Color.white;
		
		if (_textMesh != null)
		{
			startColor = _textMesh.color;
		}
		else if (_textMeshPro != null)
		{
			startColor = _textMeshPro.color;
		}
		else
		{
			yield break;
		}
		
		float t = 0f;
		while (t < _fadeDuration)
		{
			t += Time.deltaTime;
			float a = Mathf.Lerp(startColor.a, 0f, Mathf.Clamp01(t / _fadeDuration));
			Color newColor = new Color(startColor.r, startColor.g, startColor.b, a);
			
			if (_textMesh != null)
			{
				_textMesh.color = newColor;
			}
			
			if (_textMeshPro != null)
			{
				_textMeshPro.color = newColor;
			}
			
			yield return null;
		}
	}
	}
}


