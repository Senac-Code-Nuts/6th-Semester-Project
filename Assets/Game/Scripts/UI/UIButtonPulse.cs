using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PiGame.UI
{
    [RequireComponent(typeof(Selectable))]
    public class UIButtonPulse : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField, Min(1f)] private float _selectedScale = 1.04f;
        [SerializeField, Range(0f, 0.05f)] private float _pulseAmount = 0.012f;
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 2f;
        [SerializeField, Min(0.1f)] private float _transitionSpeed = 14f;

        private Selectable _selectable;
        private Vector3 _baseScale;
        private bool _isSelected;
        private bool _isPointerInside;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            _selectable ??= GetComponent<Selectable>();
            _baseScale = transform.localScale;
            _isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            _isPointerInside = false;
        }

        private void Update()
        {
            bool highlighted = _selectable != null && _selectable.IsInteractable() && (_isSelected || _isPointerInside);
            float pulse = highlighted
                ? 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * _pulseSpeed) * _pulseAmount
                : 1f;
            float scale = highlighted ? _selectedScale * pulse : 1f;
            float interpolation = 1f - Mathf.Exp(-_transitionSpeed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * scale, interpolation);
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
            _isSelected = false;
            _isPointerInside = false;
        }

        public void OnSelect(BaseEventData eventData) => _isSelected = true;
        public void OnDeselect(BaseEventData eventData) => _isSelected = false;
        public void OnPointerEnter(PointerEventData eventData) => _isPointerInside = true;
        public void OnPointerExit(PointerEventData eventData) => _isPointerInside = false;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _selectedScale = Mathf.Max(1f, _selectedScale);
            _pulseAmount = Mathf.Clamp(_pulseAmount, 0f, 0.05f);
            _pulseSpeed = Mathf.Max(0.1f, _pulseSpeed);
            _transitionSpeed = Mathf.Max(0.1f, _transitionSpeed);
        }
#endif
    }
}
