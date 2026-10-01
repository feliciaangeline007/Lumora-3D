using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoinConvoy
{
    /// <summary>
    /// Ergonomic touch button for driving and steering controls.
    /// Supports multi-touch, press animation feedback, color brightening,
    /// and visual synchronization with keyboard keys for desktop play.
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum ActionType { Gas, Brake, Left, Right }

        [Header("Configuration")]
        [SerializeField] private ActionType action;
        [SerializeField] private CarController car;

        [Header("Visual Feedback")]
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float animSpeed = 16f;

        private Image buttonImage;
        private RectTransform rectTransform;
        private Color normalColor;
        private Color pressedColor;
        private bool isPointerDown;
        private int activePointerId = int.MinValue;
        private float currentScale = 1f;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            buttonImage = GetComponent<Image>();

            if (buttonImage != null)
            {
                normalColor = buttonImage.color;
                // Brighten color on press
                pressedColor = new Color(
                    Mathf.Min(1f, normalColor.r * 1.35f + 0.1f),
                    Mathf.Min(1f, normalColor.g * 1.35f + 0.1f),
                    Mathf.Min(1f, normalColor.b * 1.35f + 0.1f),
                    Mathf.Min(1f, normalColor.a * 1.15f)
                );
            }
        }

        private void Update()
        {
            // CarController consumes touch input in FixedUpdate, so keep the command
            // alive for as long as the finger remains on the button.
            if (isPointerDown) Apply(true);

            bool isKeyPressed = CheckKeyboardPressed();
            bool active = isPointerDown || isKeyPressed;

            // Smooth scale pop
            float targetScale = active ? pressedScale : 1f;
            currentScale = Mathf.Lerp(currentScale, targetScale, animSpeed * Time.unscaledDeltaTime);
            if (rectTransform != null)
            {
                rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);
            }

            // Smooth color shift
            if (buttonImage != null)
            {
                Color targetColor = active ? pressedColor : normalColor;
                buttonImage.color = Color.Lerp(buttonImage.color, targetColor, animSpeed * Time.unscaledDeltaTime);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isPointerDown && activePointerId != eventData.pointerId) return;
            isPointerDown = true;
            activePointerId = eventData.pointerId;
            Apply(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPointerDown || activePointerId != eventData.pointerId) return;
            isPointerDown = false;
            activePointerId = int.MinValue;
            Apply(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isPointerDown || activePointerId != eventData.pointerId) return;
            isPointerDown = false;
            activePointerId = int.MinValue;
            Apply(false);
        }

        private void OnDisable()
        {
            ReleasePointer();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ReleasePointer();
        }

        private void ReleasePointer()
        {
            if (!isPointerDown) return;
            isPointerDown = false;
            activePointerId = int.MinValue;
            Apply(false);
        }

        private void Apply(bool pressed)
        {
            if (car == null) return;
            switch (action)
            {
                case ActionType.Gas: car.SetTouchGas(pressed ? 1f : 0f); break;
                case ActionType.Brake: car.SetTouchBrake(pressed); break;
                case ActionType.Left: car.SetTouchSteer(-1f, pressed); break;
                case ActionType.Right: car.SetTouchSteer(1f, pressed); break;
            }
        }

        private bool CheckKeyboardPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            switch (action)
            {
                case ActionType.Gas: return kb.wKey.isPressed || kb.upArrowKey.isPressed;
                case ActionType.Brake: return kb.sKey.isPressed || kb.downArrowKey.isPressed || kb.spaceKey.isPressed;
                case ActionType.Left: return kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                case ActionType.Right: return kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            }
#else
            switch (action)
            {
                case ActionType.Gas: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case ActionType.Brake: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Space);
                case ActionType.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                case ActionType.Right: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            }
#endif
            return false;
        }
    }
}
