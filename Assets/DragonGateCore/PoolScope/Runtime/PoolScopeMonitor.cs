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
            if (_isBindingKey)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    // Reject mouse buttons
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
        }

        private void HandleMobileGesture()
        {
            if (_useGesture == false) return;

            if (Input.touchCount == GestureTouchCount)
            {
                if (_gestureTracking == false)
                {
                    _gestureTracking  = true;
                    _gestureStartTime = Time.realtimeSinceStartup;
                }

                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase == TouchPhase.Ended)
                    {
                        float elapsed = Time.realtimeSinceStartup - _gestureStartTime;
                        if (elapsed <= GestureTapMaxDuration)
                        {
                            _isVisible       = !_isVisible;
                            _gestureTracking = false;
                        }
                        return;
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

            if (Input.GetMouseButton(0) == false)
            {
                _isResizing = false;
                SavePrefs();
                return;
            }

            Vector2 mousePosGui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            Vector2 delta = mousePosGui - _resizeStartMouse;
            _windowRect.width  = Mathf.Max(MinWindowWidth,  _resizeStartSize.x + delta.x);
            _windowRect.height = Mathf.Max(MinWindowHeight, _resizeStartSize.y + delta.y);
        }

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
            GUILayout.Label(info.PeakUsage.ToString(), _styleStats, GUILayout.Width(peakW));
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