using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class PoolScopeMonitor : MonoBehaviour
    {
        private const string PrefsKeyToggleKey  = "PoolScope_ToggleKey";
        private const string PrefsKeyWindowX    = "PoolScope_WindowX";
        private const string PrefsKeyWindowY    = "PoolScope_WindowY";
        private const string PrefsKeyWindowW    = "PoolScope_WindowW";
        private const string PrefsKeyWindowH    = "PoolScope_WindowH";
        private const string PrefsKeyShowButton = "PoolScope_ShowButton";
        private const string PrefsKeyUseGesture = "PoolScope_UseGesture";

        private const float ScreenSizeRatio     = 0.7f;
        private const float MinWindowWidth      = 300f;
        private const float MinWindowHeight     = 200f;
        private const float ResizeHandleSize    = 16f;
        private const float FloatingButtonSize  = 48f;
        private const int   GestureTouchCount   = 3;
        private const float GestureTapMaxDuration = 0.25f;

        private bool    _isVisible       = false;
        private bool    _showSettings    = false;
        private bool    _isBindingKey    = false;
        private Rect    _windowRect;
        private Vector2 _scrollPosition  = Vector2.zero;

        private KeyCode _toggleKey       = KeyCode.BackQuote;
        private bool    _showFloatButton = true;
        private bool    _useGesture      = true;

        private bool    _isResizing      = false;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartSize;

        private float   _gestureStartTime = 0f;
        private bool    _gestureTracking  = false;

        private float DefaultWindowWidth  => Screen.width  * ScreenSizeRatio;
        private float DefaultWindowHeight => Screen.height * ScreenSizeRatio;
        private float Scale => Mathf.Min(_windowRect.width / DefaultWindowWidth, _windowRect.height / DefaultWindowHeight);

        private GUIStyle _styleTitle;
        private GUIStyle _styleLabel;
        private GUIStyle _styleStats;
        private GUIStyle _styleButton;
        private GUIStyle _styleButtonActive;
        private GUIStyle _styleResizeHandle;
        private GUIStyle _styleCloseButton;
        private float    _lastScale = -1f;

        private void Awake()
        {
            LoadPrefs();
        }

        private void Start()
        {
            // Screen size is reliable here. Apply default if no saved size.
            if (_windowRect.width < MinWindowWidth)
                _windowRect.width  = DefaultWindowWidth;
            if (_windowRect.height < MinWindowHeight)
                _windowRect.height = DefaultWindowHeight;
        }

        private void LoadPrefs()
        {
            _toggleKey       = (KeyCode)PlayerPrefs.GetInt(PrefsKeyToggleKey, (int)KeyCode.BackQuote);
            _showFloatButton = PlayerPrefs.GetInt(PrefsKeyShowButton, 1) == 1;
            _useGesture      = PlayerPrefs.GetInt(PrefsKeyUseGesture, 1) == 1;

            float x = PlayerPrefs.GetFloat(PrefsKeyWindowX, 10f);
            float y = PlayerPrefs.GetFloat(PrefsKeyWindowY, 10f);
            float w = PlayerPrefs.GetFloat(PrefsKeyWindowW, 0f);
            float h = PlayerPrefs.GetFloat(PrefsKeyWindowH, 0f);
            // 0 means no saved value - use screen-based default (Screen not reliable in Awake, resolved in Start)
            _windowRect = new Rect(x, y, w, h);
        }

        private void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefsKeyToggleKey,  (int)_toggleKey);
            PlayerPrefs.SetInt(PrefsKeyShowButton, _showFloatButton ? 1 : 0);
            PlayerPrefs.SetInt(PrefsKeyUseGesture, _useGesture      ? 1 : 0);
            PlayerPrefs.SetFloat(PrefsKeyWindowX,  _windowRect.x);
            PlayerPrefs.SetFloat(PrefsKeyWindowY,  _windowRect.y);
            PlayerPrefs.SetFloat(PrefsKeyWindowW,  _windowRect.width);
            PlayerPrefs.SetFloat(PrefsKeyWindowH,  _windowRect.height);
            PlayerPrefs.Save();
        }

        private void Update()
        {
            HandleKeyboardToggle();
            HandleMobileGesture();
            HandleResize();
        }

        private void HandleKeyboardToggle()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (_isBindingKey)
            {
                var currentKeyboard = UnityEngine.InputSystem.Keyboard.current;
                if (currentKeyboard == null) return;

                foreach (var key in UnityEngine.InputSystem.Keyboard.current.allKeys)
                {
                    if (key.wasPressedThisFrame)
                    {
                        _toggleKey    = InputSystemKeyToKeyCode(key.keyCode);
                        _isBindingKey = false;
                        SavePrefs();
                        return;
                    }
                }
                return;
            }

            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current[LegacyKeyCodeToInputSystemKey(_toggleKey)].wasPressedThisFrame)
                _isVisible = true;
#else
            if (_isBindingKey)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6) continue;
                    if (Input.GetKeyDown(keyCode))
                    {
                        _toggleKey    = keyCode;
                        _isBindingKey = false;
                        SavePrefs();
                        return;
                    }
                }
                return;
            }

            if (Input.GetKeyDown(_toggleKey))
                _isVisible = true;
#endif
        }

        private void HandleMobileGesture()
        {
            if (_useGesture == false) return;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
            if (touchscreen == null) return;

            int activeTouches = 0;
            bool anyEnded = false;
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var phase = touchscreen.touches[i].phase.ReadValue();
                if (phase != UnityEngine.InputSystem.TouchPhase.None)
                    activeTouches++;
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                    anyEnded = true;
            }
#else
            int activeTouches = Input.touchCount;
            bool anyEnded = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Ended)
                    anyEnded = true;
            }
#endif

            if (activeTouches == GestureTouchCount)
            {
                if (_gestureTracking == false)
                {
                    _gestureTracking  = true;
                    _gestureStartTime = Time.realtimeSinceStartup;
                }

                if (anyEnded)
                {
                    float elapsed = Time.realtimeSinceStartup - _gestureStartTime;
                    if (elapsed <= GestureTapMaxDuration)
                    {
                        _isVisible       = !_isVisible;
                        _gestureTracking = false;
                    }
                }
            }
            else
            {
                _gestureTracking = false;
            }
        }

        private void HandleResize()
        {
            if (_isResizing == false) return;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            bool mouseHeld = UnityEngine.InputSystem.Mouse.current != null &&
                             UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current != null
                ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
                : Vector2.zero;
#else
            bool mouseHeld = Input.GetMouseButton(0);
            Vector2 mousePos = Input.mousePosition;
#endif

            if (mouseHeld == false)
            {
                _isResizing = false;
                SavePrefs();
                return;
            }

            Vector2 mousePosGui = new Vector2(mousePos.x, Screen.height - mousePos.y);
            Vector2 delta = mousePosGui - _resizeStartMouse;
            _windowRect.width  = Mathf.Max(MinWindowWidth,  _resizeStartSize.x + delta.x);
            _windowRect.height = Mathf.Max(MinWindowHeight, _resizeStartSize.y + delta.y);
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static UnityEngine.InputSystem.Key LegacyKeyCodeToInputSystemKey(KeyCode keyCode)
        {
            // Covers common keys. Falls back to None for unmapped keys.
            switch (keyCode)
            {
                case KeyCode.BackQuote:    return UnityEngine.InputSystem.Key.Backquote;
                case KeyCode.Tab:          return UnityEngine.InputSystem.Key.Tab;
                case KeyCode.Return:       return UnityEngine.InputSystem.Key.Enter;
                case KeyCode.Escape:       return UnityEngine.InputSystem.Key.Escape;
                case KeyCode.Space:        return UnityEngine.InputSystem.Key.Space;
                case KeyCode.Delete:       return UnityEngine.InputSystem.Key.Delete;
                case KeyCode.Backspace:    return UnityEngine.InputSystem.Key.Backspace;
                case KeyCode.UpArrow:      return UnityEngine.InputSystem.Key.UpArrow;
                case KeyCode.DownArrow:    return UnityEngine.InputSystem.Key.DownArrow;
                case KeyCode.LeftArrow:    return UnityEngine.InputSystem.Key.LeftArrow;
                case KeyCode.RightArrow:   return UnityEngine.InputSystem.Key.RightArrow;
                case KeyCode.F1:           return UnityEngine.InputSystem.Key.F1;
                case KeyCode.F2:           return UnityEngine.InputSystem.Key.F2;
                case KeyCode.F3:           return UnityEngine.InputSystem.Key.F3;
                case KeyCode.F4:           return UnityEngine.InputSystem.Key.F4;
                case KeyCode.F5:           return UnityEngine.InputSystem.Key.F5;
                case KeyCode.F6:           return UnityEngine.InputSystem.Key.F6;
                case KeyCode.F7:           return UnityEngine.InputSystem.Key.F7;
                case KeyCode.F8:           return UnityEngine.InputSystem.Key.F8;
                case KeyCode.F9:           return UnityEngine.InputSystem.Key.F9;
                case KeyCode.F10:          return UnityEngine.InputSystem.Key.F10;
                case KeyCode.F11:          return UnityEngine.InputSystem.Key.F11;
                case KeyCode.F12:          return UnityEngine.InputSystem.Key.F12;
                case KeyCode.Alpha0:       return UnityEngine.InputSystem.Key.Digit0;
                case KeyCode.Alpha1:       return UnityEngine.InputSystem.Key.Digit1;
                case KeyCode.Alpha2:       return UnityEngine.InputSystem.Key.Digit2;
                case KeyCode.Alpha3:       return UnityEngine.InputSystem.Key.Digit3;
                case KeyCode.Alpha4:       return UnityEngine.InputSystem.Key.Digit4;
                case KeyCode.Alpha5:       return UnityEngine.InputSystem.Key.Digit5;
                case KeyCode.Alpha6:       return UnityEngine.InputSystem.Key.Digit6;
                case KeyCode.Alpha7:       return UnityEngine.InputSystem.Key.Digit7;
                case KeyCode.Alpha8:       return UnityEngine.InputSystem.Key.Digit8;
                case KeyCode.Alpha9:       return UnityEngine.InputSystem.Key.Digit9;
                case KeyCode.A:            return UnityEngine.InputSystem.Key.A;
                case KeyCode.B:            return UnityEngine.InputSystem.Key.B;
                case KeyCode.C:            return UnityEngine.InputSystem.Key.C;
                case KeyCode.D:            return UnityEngine.InputSystem.Key.D;
                case KeyCode.E:            return UnityEngine.InputSystem.Key.E;
                case KeyCode.F:            return UnityEngine.InputSystem.Key.F;
                case KeyCode.G:            return UnityEngine.InputSystem.Key.G;
                case KeyCode.H:            return UnityEngine.InputSystem.Key.H;
                case KeyCode.I:            return UnityEngine.InputSystem.Key.I;
                case KeyCode.J:            return UnityEngine.InputSystem.Key.J;
                case KeyCode.K:            return UnityEngine.InputSystem.Key.K;
                case KeyCode.L:            return UnityEngine.InputSystem.Key.L;
                case KeyCode.M:            return UnityEngine.InputSystem.Key.M;
                case KeyCode.N:            return UnityEngine.InputSystem.Key.N;
                case KeyCode.O:            return UnityEngine.InputSystem.Key.O;
                case KeyCode.P:            return UnityEngine.InputSystem.Key.P;
                case KeyCode.Q:            return UnityEngine.InputSystem.Key.Q;
                case KeyCode.R:            return UnityEngine.InputSystem.Key.R;
                case KeyCode.S:            return UnityEngine.InputSystem.Key.S;
                case KeyCode.T:            return UnityEngine.InputSystem.Key.T;
                case KeyCode.U:            return UnityEngine.InputSystem.Key.U;
                case KeyCode.V:            return UnityEngine.InputSystem.Key.V;
                case KeyCode.W:            return UnityEngine.InputSystem.Key.W;
                case KeyCode.X:            return UnityEngine.InputSystem.Key.X;
                case KeyCode.Y:            return UnityEngine.InputSystem.Key.Y;
                case KeyCode.Z:            return UnityEngine.InputSystem.Key.Z;
                default:                   return UnityEngine.InputSystem.Key.None;
            }
        }

        private static KeyCode InputSystemKeyToKeyCode(UnityEngine.InputSystem.Key key)
        {
            switch (key)
            {
                case UnityEngine.InputSystem.Key.Backquote: return KeyCode.BackQuote;
                case UnityEngine.InputSystem.Key.Tab:       return KeyCode.Tab;
                case UnityEngine.InputSystem.Key.Enter:     return KeyCode.Return;
                case UnityEngine.InputSystem.Key.Escape:    return KeyCode.Escape;
                case UnityEngine.InputSystem.Key.Space:     return KeyCode.Space;
                case UnityEngine.InputSystem.Key.Delete:    return KeyCode.Delete;
                case UnityEngine.InputSystem.Key.Backspace: return KeyCode.Backspace;
                case UnityEngine.InputSystem.Key.UpArrow:   return KeyCode.UpArrow;
                case UnityEngine.InputSystem.Key.DownArrow: return KeyCode.DownArrow;
                case UnityEngine.InputSystem.Key.LeftArrow: return KeyCode.LeftArrow;
                case UnityEngine.InputSystem.Key.RightArrow:return KeyCode.RightArrow;
                case UnityEngine.InputSystem.Key.F1:        return KeyCode.F1;
                case UnityEngine.InputSystem.Key.F2:        return KeyCode.F2;
                case UnityEngine.InputSystem.Key.F3:        return KeyCode.F3;
                case UnityEngine.InputSystem.Key.F4:        return KeyCode.F4;
                case UnityEngine.InputSystem.Key.F5:        return KeyCode.F5;
                case UnityEngine.InputSystem.Key.F6:        return KeyCode.F6;
                case UnityEngine.InputSystem.Key.F7:        return KeyCode.F7;
                case UnityEngine.InputSystem.Key.F8:        return KeyCode.F8;
                case UnityEngine.InputSystem.Key.F9:        return KeyCode.F9;
                case UnityEngine.InputSystem.Key.F10:       return KeyCode.F10;
                case UnityEngine.InputSystem.Key.F11:       return KeyCode.F11;
                case UnityEngine.InputSystem.Key.F12:       return KeyCode.F12;
                case UnityEngine.InputSystem.Key.Digit0:    return KeyCode.Alpha0;
                case UnityEngine.InputSystem.Key.Digit1:    return KeyCode.Alpha1;
                case UnityEngine.InputSystem.Key.Digit2:    return KeyCode.Alpha2;
                case UnityEngine.InputSystem.Key.Digit3:    return KeyCode.Alpha3;
                case UnityEngine.InputSystem.Key.Digit4:    return KeyCode.Alpha4;
                case UnityEngine.InputSystem.Key.Digit5:    return KeyCode.Alpha5;
                case UnityEngine.InputSystem.Key.Digit6:    return KeyCode.Alpha6;
                case UnityEngine.InputSystem.Key.Digit7:    return KeyCode.Alpha7;
                case UnityEngine.InputSystem.Key.Digit8:    return KeyCode.Alpha8;
                case UnityEngine.InputSystem.Key.Digit9:    return KeyCode.Alpha9;
                case UnityEngine.InputSystem.Key.A:         return KeyCode.A;
                case UnityEngine.InputSystem.Key.B:         return KeyCode.B;
                case UnityEngine.InputSystem.Key.C:         return KeyCode.C;
                case UnityEngine.InputSystem.Key.D:         return KeyCode.D;
                case UnityEngine.InputSystem.Key.E:         return KeyCode.E;
                case UnityEngine.InputSystem.Key.F:         return KeyCode.F;
                case UnityEngine.InputSystem.Key.G:         return KeyCode.G;
                case UnityEngine.InputSystem.Key.H:         return KeyCode.H;
                case UnityEngine.InputSystem.Key.I:         return KeyCode.I;
                case UnityEngine.InputSystem.Key.J:         return KeyCode.J;
                case UnityEngine.InputSystem.Key.K:         return KeyCode.K;
                case UnityEngine.InputSystem.Key.L:         return KeyCode.L;
                case UnityEngine.InputSystem.Key.M:         return KeyCode.M;
                case UnityEngine.InputSystem.Key.N:         return KeyCode.N;
                case UnityEngine.InputSystem.Key.O:         return KeyCode.O;
                case UnityEngine.InputSystem.Key.P:         return KeyCode.P;
                case UnityEngine.InputSystem.Key.Q:         return KeyCode.Q;
                case UnityEngine.InputSystem.Key.R:         return KeyCode.R;
                case UnityEngine.InputSystem.Key.S:         return KeyCode.S;
                case UnityEngine.InputSystem.Key.T:         return KeyCode.T;
                case UnityEngine.InputSystem.Key.U:         return KeyCode.U;
                case UnityEngine.InputSystem.Key.V:         return KeyCode.V;
                case UnityEngine.InputSystem.Key.W:         return KeyCode.W;
                case UnityEngine.InputSystem.Key.X:         return KeyCode.X;
                case UnityEngine.InputSystem.Key.Y:         return KeyCode.Y;
                case UnityEngine.InputSystem.Key.Z:         return KeyCode.Z;
                default:                                    return KeyCode.None;
            }
        }
#endif

        private void OnGUI()
        {
            EnsureStylesScaled();

            if (_isBindingKey)
                DrawBindingOverlay();

            if (_showFloatButton && _isVisible == false)
                DrawFloatingButton();

            if (_isVisible == false) return;

            _windowRect = GUI.Window(9981, _windowRect, DrawWindow, GUIContent.none);
            ClampWindowToScreen();
        }

        private void DrawFloatingButton()
        {
            Rect rect = new Rect(
                Screen.width  - FloatingButtonSize - 8f,
                Screen.height - FloatingButtonSize - 8f,
                FloatingButtonSize,
                FloatingButtonSize
            );
            if (GUI.Button(rect, "PS", _styleButton))
                _isVisible = true;
        }

        private void DrawBindingOverlay()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            float w = 300f;
            float h = 60f;
            GUI.Box(new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h), "Press any key to bind... (mouse buttons excluded)");
        }

        private void DrawWindow(int windowId)
        {
            float s = Scale;
            float titleBarHeight = _styleTitle.fontSize + 16f;
            float btnH           = titleBarHeight - 6f;

            // Title bar background
            Rect titleBarRect = new Rect(0, 0, _windowRect.width, titleBarHeight);
            PoolScopeGUI.DrawRect(titleBarRect, new Color(0.15f, 0.15f, 0.15f));

            // Close button (right side)
            float closeBtnW  = titleBarHeight;
            Rect closeBtnRect = new Rect(_windowRect.width - closeBtnW - 3f, 3f, closeBtnW, btnH);
            if (GUI.Button(closeBtnRect, "X", _styleCloseButton))
            {
                _isVisible    = false;
                _showSettings = false;
            }

            // Settings / Monitor toggle button
            float settingsBtnW   = 120f * s;
            Rect settingsBtnRect = new Rect(_windowRect.width - closeBtnW - settingsBtnW - 6f, 3f, settingsBtnW, btnH);
            if (GUI.Button(settingsBtnRect, _showSettings ? "Monitor" : "Settings", _styleButton))
                _showSettings = !_showSettings;

            // Title label - vertically centered in bar
            Rect titleLabelRect = new Rect(6f, 0f, _windowRect.width - closeBtnW - settingsBtnW - 12f, titleBarHeight);
            GUI.Label(titleLabelRect, "PoolScope Monitor", _styleTitle);

            GUILayout.Space(titleBarHeight + 4f);

            if (_showSettings)
                DrawSettingsPanel();
            else
                DrawMonitorPanel();

            DrawResizeHandle();
            GUI.DragWindow(titleBarRect);
        }

        private void DrawMonitorPanel()
        {
            float s = Scale;
            IReadOnlyDictionary<string, IPoolInfoProvider> pools = PoolScope.Instance.RegisteredPools;

            float fontSize      = _styleLabel.fontSize;
            float scrollbarW    = GUI.skin.verticalScrollbar.fixedWidth + 4f;
            float padding       = 16f;
            float usableWidth   = _windowRect.width - padding;
            float scrollRowW    = usableWidth - scrollbarW;
            float labelWidth    = usableWidth   * 0.4f;
            float barWidth      = usableWidth   * 0.3f;
            float usedTotalW    = usableWidth   * 0.18f;
            float peakW         = usableWidth   * 0.12f;
            float scrollLabelW  = scrollRowW    * 0.4f;
            float scrollBarW    = scrollRowW    * 0.3f;
            float scrollUsedW   = scrollRowW    * 0.18f;
            float scrollPeakW   = scrollRowW    * 0.12f;
            float barHeight     = fontSize + 8f;
            float spacing       = 4f * s;

            // Header row (outside scrollview, no scrollbar offset)
            const float colGap = 8f;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pool Name",    _styleTitle, GUILayout.Width(labelWidth - colGap));
            GUILayout.Space(colGap);
            GUILayout.Label("Usage",        _styleTitle, GUILayout.Width(barWidth - colGap));
            GUILayout.Space(colGap);
            GUILayout.Label("Used / Total", _styleTitle, GUILayout.Width(usedTotalW - colGap));
            GUILayout.Space(colGap);
            GUILayout.Label("Peak",         _styleTitle, GUILayout.Width(peakW));
            GUILayout.EndHorizontal();

            DrawLine();

            float scrollHeight = _windowRect.height - (20f * s) - 80f;
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(scrollHeight));

            foreach (KeyValuePair<string, IPoolInfoProvider> entry in pools)
                DrawRow(entry.Value, scrollLabelW, scrollBarW, scrollUsedW, scrollPeakW, barHeight, spacing);

            GUILayout.EndScrollView();

            DrawLine();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Pools: {pools.Count}", _styleStats);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(ResizeHandleSize);
        }

        private void DrawRow(IPoolInfoProvider info, float labelWidth, float barWidth, float usedTotalW, float peakW, float barHeight, float spacing)
        {
            const float colGap = 8f;
            GUILayout.Space(spacing);
            GUILayout.BeginHorizontal();
            GUILayout.Label(info.PoolName, _styleLabel, GUILayout.Width(labelWidth - colGap));
            GUILayout.Space(colGap);

            Rect barRect = GUILayoutUtility.GetRect(barWidth - colGap, barHeight, GUILayout.Width(barWidth - colGap));
            DrawUsageBar(barRect, info);
            GUILayout.Space(colGap);

            GUILayout.Label($"{info.CurrentInUse} / {info.TotalCount}", _styleStats, GUILayout.Width(usedTotalW - colGap));
            GUILayout.Space(colGap);
            GUILayout.Label($"P:{info.PeakUsage}", _styleStats, GUILayout.Width(peakW));
            GUILayout.EndHorizontal();
        }

        private void DrawUsageBar(Rect barRect, IPoolInfoProvider info)
        {
            PoolScopeGUI.DrawRect(barRect, new Color(0.18f, 0.18f, 0.18f, 0.9f));
            if (info.TotalCount <= 0) return;

            float usedRatio      = (float)info.CurrentInUse / info.TotalCount;
            float availableRatio = (float)info.LeftInPool   / info.TotalCount;

            PoolScopeGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * usedRatio, barRect.height), info.BarColor);

            Color dimColor = info.BarColor * 0.45f;
            dimColor.a = 1f;
            PoolScopeGUI.DrawRect(new Rect(barRect.x + barRect.width * usedRatio, barRect.y, barRect.width * availableRatio, barRect.height), dimColor);

            if (info.PeakUsage > 0)
            {
                float peakX = barRect.x + barRect.width * ((float)info.PeakUsage / info.TotalCount) - 1f;
                PoolScopeGUI.DrawRect(new Rect(peakX, barRect.y, 2f, barRect.height), Color.white);
            }
        }

        private void DrawSettingsPanel()
        {
            float s = Scale;
            float fontSize    = _styleLabel.fontSize;
            float rowHeight   = fontSize + 16f;
            float labelWidth  = fontSize * 8f;
            float btnWidth    = fontSize * 4f;

            GUILayout.Space(8f * s);

            // Hotkey row
            GUILayout.BeginHorizontal(GUILayout.Height(rowHeight));
            GUILayout.Label("Toggle Key:", _styleLabel, GUILayout.Width(labelWidth));
            string bindLabel = _isBindingKey ? "Press key..." : _toggleKey.ToString();
            GUIStyle keyStyle = _isBindingKey ? _styleButtonActive : _styleButton;
            if (GUILayout.Button(bindLabel, keyStyle, GUILayout.Width(fontSize * 8f), GUILayout.Height(rowHeight)))
                _isBindingKey = !_isBindingKey;
            GUILayout.EndHorizontal();

            GUILayout.Space(6f * s);
            DrawLine();
            GUILayout.Space(6f * s);

            // Floating button row
            GUILayout.BeginHorizontal(GUILayout.Height(rowHeight));
            GUILayout.Label("Floating Button:", _styleLabel, GUILayout.Width(labelWidth));
            if (GUILayout.Button(_showFloatButton ? "ON" : "OFF", _showFloatButton ? _styleButtonActive : _styleButton, GUILayout.Width(btnWidth), GUILayout.Height(rowHeight)))
            {
                _showFloatButton = !_showFloatButton;
                SavePrefs();
            }
            GUILayout.Label("Shows PS button when hidden. (Mobile)", _styleStats);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f * s);

            // Gesture row
            GUILayout.BeginHorizontal(GUILayout.Height(rowHeight));
            GUILayout.Label("3-Finger Tap:", _styleLabel, GUILayout.Width(labelWidth));
            if (GUILayout.Button(_useGesture ? "ON" : "OFF", _useGesture ? _styleButtonActive : _styleButton, GUILayout.Width(btnWidth), GUILayout.Height(rowHeight)))
            {
                _useGesture = !_useGesture;
                SavePrefs();
            }
            GUILayout.Label("Tap with 3 fingers to toggle. (Mobile)", _styleStats);
            GUILayout.EndHorizontal();

            GUILayout.Space(ResizeHandleSize);
        }

        private void DrawResizeHandle()
        {
            float handleX    = _windowRect.width  - ResizeHandleSize;
            float handleY    = _windowRect.height - ResizeHandleSize;
            Rect  handleRect = new Rect(handleX, handleY, ResizeHandleSize, ResizeHandleSize);

            GUI.Label(handleRect, "◢", _styleResizeHandle);

            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            {
                _isResizing        = true;
                _resizeStartMouse  = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                _resizeStartSize   = new Vector2(_windowRect.width, _windowRect.height);
                Event.current.Use();
            }
        }

        private void DrawLine()
        {
            Rect lineRect = GUILayoutUtility.GetRect(_windowRect.width - 16f, 1f);
            PoolScopeGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f));
            GUILayout.Space(2f);
        }

        private void ClampWindowToScreen()
        {
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Screen.width  - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Screen.height - _windowRect.height);
        }

        private void OnApplicationQuit()
        {
            SavePrefs();
        }

        private void EnsureStylesScaled()
        {
            float currentScale = Scale;
            if (_stylesInitialized && Mathf.Abs(currentScale - _lastScale) < 0.01f) return;

            int baseFontTitle  = Mathf.RoundToInt(40f * currentScale);
            int baseFontLabel  = Mathf.RoundToInt(36f * currentScale);
            int baseFontStats  = Mathf.RoundToInt(32f * currentScale);
            int baseFontBtn    = Mathf.RoundToInt(32f * currentScale);

            _styleTitle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = baseFontTitle
            };

            _styleLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize  = baseFontLabel,
                alignment = TextAnchor.MiddleLeft
            };

            _styleStats = new GUIStyle(GUI.skin.label)
            {
                fontSize  = baseFontStats,
                alignment = TextAnchor.MiddleRight,
                normal    = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };

            _styleButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = baseFontBtn
            };

            _styleButtonActive = new GUIStyle(GUI.skin.button)
            {
                fontSize = baseFontBtn,
                normal   = { textColor = new Color(0.4f, 1f, 0.4f) }
            };

            _styleResizeHandle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.RoundToInt(44f * currentScale),
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.55f, 0.55f, 0.55f) }
            };

            _styleCloseButton = new GUIStyle(GUI.skin.button)
            {
                fontSize  = baseFontBtn,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            _lastScale         = currentScale;
            _stylesInitialized = true;
        }

        private bool _stylesInitialized = false;
    }

    public static class PoolScopeGUI
    {
        private static Texture2D _whiteTexture;

        private static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                return _whiteTexture;
            }
        }

        public static void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color  = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color  = prev;
        }
    }
}