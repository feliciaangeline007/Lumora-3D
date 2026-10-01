using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace EnchantedForest
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TouchGameControls : MonoBehaviour
    {
        private const float JoystickRadius = 82f;

        private PlayerController3D _player;
        private GameModeManager _gameMode;
        private ThirdPersonCameraFollow _cameraFollow;
        private TouchJoystickInput _joystick;
        private CanvasGroup _gameplayControlsGroup;
        private CanvasGroup _startGroup;
        private bool _jumpPressed;

        public Vector2 Movement => _joystick != null ? _joystick.Movement : Vector2.zero;

        public static TouchGameControls CreateFor(PlayerController3D player)
        {
#if ENABLE_INPUT_SYSTEM
            bool touchPlatform = Application.isMobilePlatform || (Application.isEditor && UnityEngine.InputSystem.Touchscreen.current != null);
#else
            bool touchPlatform = Application.isMobilePlatform;
#endif
            if (!touchPlatform) return null;

            EnsureEventSystem();
            GameObject controlsObject = new GameObject("Lumora_TouchControls");
            TouchGameControls controls = controlsObject.AddComponent<TouchGameControls>();
            controls.Initialize(player);
            return controls;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<BaseInputModule>() != null) return;

#if ENABLE_INPUT_SYSTEM
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#elif ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#else
            Debug.LogError("[Lumora] Tidak ada input module UI yang didukung; kontrol sentuh tidak dapat menerima input.");
#endif
        }

        private void Initialize(PlayerController3D player)
        {
            _player = player;
            _gameMode = GameModeManager.Instance;
            _cameraFollow = player.GetComponentInChildren<ThirdPersonCameraFollow>(true);

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            CreateGameplayControls();
            CreateStartPrompt();
        }

        private void Update()
        {
            if (_gameMode == null) _gameMode = GameModeManager.Instance;

            bool playerMode = _gameMode == null || _gameMode.currentMode == GameModeManager.CameraMode.PlayerThirdPerson;
            bool canPlay = _player != null && _player.isActiveAndEnabled && playerMode
                && (_gameMode == null || _gameMode.IsPlaying);
            bool needsStartPrompt = _gameMode != null && !_gameMode.HasStarted;

            _gameplayControlsGroup.alpha = canPlay && !needsStartPrompt ? 1f : 0f;
            _gameplayControlsGroup.interactable = canPlay && !needsStartPrompt;
            _gameplayControlsGroup.blocksRaycasts = canPlay && !needsStartPrompt;

            _startGroup.alpha = needsStartPrompt ? 1f : 0f;
            _startGroup.interactable = needsStartPrompt;
            _startGroup.blocksRaycasts = needsStartPrompt;

            if (!canPlay || needsStartPrompt)
            {
                _joystick.ResetInput();
                _jumpPressed = false;
            }
        }

        private void CreateGameplayControls()
        {
            _gameplayControlsGroup = CreateGroup("GameplayControls", transform);
            CreateCameraLookZone();

            RectTransform joystickRoot = CreateRect("MoveJoystick", _gameplayControlsGroup.transform,
                new Vector2(0f, 0f), new Vector2(220f, 220f), new Vector2(150f, 150f));
            CreateImage("OuterRing", joystickRoot, new Vector2(220f, 220f),
                new Color(0.025f, 0.075f, 0.075f, 0.68f));
            CreateImage("InnerRing", joystickRoot, new Vector2(184f, 184f),
                new Color(0.11f, 0.24f, 0.23f, 0.72f));
            RectTransform knob = CreateImage("Knob", joystickRoot, new Vector2(88f, 88f),
                new Color(0.31f, 0.96f, 0.85f, 0.94f)).rectTransform;
            _joystick = joystickRoot.gameObject.AddComponent<TouchJoystickInput>();
            _joystick.Initialize(knob, JoystickRadius);
            CreateLabel("GERAK", joystickRoot, new Vector2(0f, -58f), 17, new Color(1f, 1f, 1f, 0.9f));

            RectTransform jumpRoot = CreateRect("JumpButton", _gameplayControlsGroup.transform,
                new Vector2(1f, 0f), new Vector2(148f, 148f), new Vector2(-154f, 156f));
            CreateImage("JumpDisc", jumpRoot, new Vector2(148f, 148f),
                new Color(0.93f, 0.55f, 0.18f, 0.94f));
            CreateLabel("LOMPAT", jumpRoot, Vector2.zero, 20, Color.white);
            jumpRoot.gameObject.AddComponent<TouchJumpInput>().Initialize(this);
        }

        private void CreateCameraLookZone()
        {
            GameObject zone = new GameObject("CameraLookZone", typeof(RectTransform), typeof(Image));
            zone.transform.SetParent(_gameplayControlsGroup.transform, false);
            RectTransform rect = zone.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.42f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image surface = zone.GetComponent<Image>();
            surface.color = new Color(1f, 1f, 1f, 0f);
            surface.raycastTarget = true;
            zone.AddComponent<TouchCameraLookInput>().Initialize(_cameraFollow);
            zone.transform.SetAsFirstSibling();
        }

        private void CreateStartPrompt()
        {
            _startGroup = CreateGroup("StartPrompt", transform);
            RectTransform prompt = CreateRect("StartButton", _startGroup.transform,
                new Vector2(0.5f, 0.18f), new Vector2(430f, 100f), Vector2.zero);
            Image startDisc = CreateImage("StartDisc", prompt, new Vector2(430f, 100f),
                new Color(0.04f, 0.16f, 0.15f, 0.86f));
            CreateLabel("TAP TO PLAY", prompt, Vector2.zero, 30, new Color(0.31f, 0.96f, 0.85f));
            Button startButton = prompt.gameObject.AddComponent<Button>();
            startButton.targetGraphic = startDisc;
            startButton.onClick.AddListener(StartGameFromTouch);
        }

        private void StartGameFromTouch()
        {
            if (_gameMode == null) _gameMode = GameModeManager.Instance;
            if (_gameMode != null) _gameMode.StartGameFromTouch();
        }

        private static CanvasGroup CreateGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            group.transform.SetParent(parent, false);
            RectTransform rect = group.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return group.GetComponent<CanvasGroup>();
        }

        private static RectTransform CreateRect(
            string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            GameObject element = new GameObject(name, typeof(RectTransform));
            element.transform.SetParent(parent, false);
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), size, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static void CreateLabel(string text, Transform parent, Vector2 position, int fontSize, Color color)
        {
            RectTransform rect = CreateRect(text, parent, new Vector2(0.5f, 0.5f), new Vector2(220f, 44f), position);
            Text label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
        }

        public void PressJump()
        {
            _jumpPressed = true;
        }

        public bool ConsumeJumpPressed()
        {
            bool pressed = _jumpPressed;
            _jumpPressed = false;
            return pressed;
        }
    }

    public sealed class TouchJoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _rectTransform;
        private RectTransform _knob;
        private float _radius;
        private int _activePointerId = int.MinValue;

        public Vector2 Movement { get; private set; }

        public void Initialize(RectTransform knob, float radius)
        {
            _rectTransform = (RectTransform)transform;
            _knob = knob;
            _radius = radius;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != int.MinValue) return;
            _activePointerId = eventData.pointerId;
            UpdateJoystick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointerId) UpdateJoystick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            ResetInput();
        }

        public void ResetInput()
        {
            _activePointerId = int.MinValue;
            Movement = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateJoystick(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPosition))
                return;

            Vector2 displacement = Vector2.ClampMagnitude(localPosition, _radius);
            Movement = displacement / _radius;
            _knob.anchoredPosition = displacement;
        }
    }

    public sealed class TouchJumpInput : MonoBehaviour, IPointerDownHandler
    {
        private TouchGameControls _controls;

        public void Initialize(TouchGameControls controls)
        {
            _controls = controls;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _controls.PressJump();
        }
    }

    public sealed class TouchCameraLookInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private ThirdPersonCameraFollow _cameraFollow;
        private int _activePointerId = int.MinValue;

        public void Initialize(ThirdPersonCameraFollow cameraFollow)
        {
            _cameraFollow = cameraFollow;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != int.MinValue) return;
            _activePointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointerId && _cameraFollow != null)
            {
                _cameraFollow.ApplyTouchLook(eventData.delta);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointerId) _activePointerId = int.MinValue;
        }
    }
}
