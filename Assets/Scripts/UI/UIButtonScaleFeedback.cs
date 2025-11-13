using UnityEngine;
using UnityEngine.EventSystems;

namespace Points.UI
{
    /// <summary>
    /// Adds a subtle scale-up animation to UI buttons while keeping their existing color transitions intact.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonScaleFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        [Tooltip("Multiplier applied while the pointer hovers over the button.")]
        private float hoverScaleMultiplier = 1.1f;

        [SerializeField]
        [Tooltip("Multiplier applied while the pointer is held down on the button.")]
        private float pressedScaleMultiplier = 1.12f;

        [SerializeField]
        [Tooltip("How quickly the button moves toward the target scale.")]
        private float scaleLerpSpeed = 12f;

        private Vector3 _baseScale;
        private Vector3 _targetScale;
        private bool _isHovering;
        private bool _initialized;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
            _initialized = true;
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                _baseScale = transform.localScale;
            }

            _targetScale = _baseScale;
            transform.localScale = _baseScale;
            _isHovering = false;
        }

        private void Update()
        {
            if (transform.localScale != _targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * scaleLerpSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            SetTargetScale(_baseScale * hoverScaleMultiplier);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            SetTargetScale(_baseScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetTargetScale(_baseScale * pressedScaleMultiplier);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetTargetScale(_isHovering ? _baseScale * hoverScaleMultiplier : _baseScale);
        }

        private void SetTargetScale(Vector3 target)
        {
            _targetScale = target;
        }
    }
}

