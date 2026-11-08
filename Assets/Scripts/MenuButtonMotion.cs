using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] private VisualSettings visualSettings;
        private Button button;
        private Vector3 restScale;
        private float scale = 1f, velocity, pressRemaining;
        private bool hovered, selected, pointerDown;

        private void Awake()
        {
            button = GetComponent<Button>();
            restScale = transform.localScale;
        }

        private void OnEnable()
        {
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        }

        private void Update()
        {
            if (visualSettings == null) return;
            var style = visualSettings.menuButtons;
            bool interactive = button.IsInteractable();
            float target = interactive && (hovered || selected) ? style.hoverScale : 1f;
            if (interactive && (pointerDown || pressRemaining > 0f)) target = style.pressScale;
            float dt = Time.unscaledDeltaTime;
            pressRemaining = Mathf.Max(0f, pressRemaining - dt);

            // Exact damped-spring integration gives a small overshoot without frame-rate-dependent bounce.
            float omega = Mathf.Max(0.1f, style.bounceFrequency) * Mathf.PI * 2f;
            float damping = Mathf.Clamp(style.bounceDamping, 0.1f, 0.99f);
            float frequency = omega * Mathf.Sqrt(1f - damping * damping);
            float displacement = scale - target;
            float coefficient = (velocity + damping * omega * displacement) / frequency;
            float decay = Mathf.Exp(-damping * omega * dt);
            float cosine = Mathf.Cos(frequency * dt), sine = Mathf.Sin(frequency * dt);
            float oscillation = displacement * cosine + coefficient * sine;
            scale = target + decay * oscillation;
            velocity = decay * (-damping * omega * oscillation + frequency * (coefficient * cosine - displacement * sine));
            transform.localScale = restScale * scale;
        }

        public void OnPointerEnter(PointerEventData data) => hovered = true;
        public void OnPointerExit(PointerEventData data) => hovered = false;
        public void OnSelect(BaseEventData data) => selected = true;
        public void OnDeselect(BaseEventData data) => selected = false;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;
            pointerDown = true;
            Press();
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) pointerDown = false;
        }

        public void OnSubmit(BaseEventData data)
        {
            if (isActiveAndEnabled && button.IsInteractable()) Press();
        }

        private void Press()
        {
            if (visualSettings != null) pressRemaining = visualSettings.menuButtons.pressDuration;
        }

        private void OnDisable()
        {
            if (button == null) return;
            hovered = selected = pointerDown = false;
            scale = 1f;
            velocity = pressRemaining = 0f;
            transform.localScale = restScale;
        }
    }
}
