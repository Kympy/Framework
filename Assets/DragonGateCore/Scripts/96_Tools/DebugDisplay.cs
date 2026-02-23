#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace DragonGate
{
    public class DebugDisplay : MonoBehaviour
    {
        protected GUIStyle _centerStyle;
        protected GUIStyle _leftStyle;
        protected Rect _originRect;
        protected Rect rect;

        private bool _visible = false;
        
        [Header("Visibility Toggle")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        [SerializeField] private bool _enableMobileGesture = true;
        // Normalized rect (x,y,w,h) from top-left in screen space
        [SerializeField] private Rect _mobileToggleAreaNormalized = new Rect(0f, 0f, 0.15f, 0.15f);
        [SerializeField] private int _mobileTapCount = 3;
        [SerializeField] private float _mobileTapWindow = 1.2f;
#endif

        // Gesture state
        private int _tapCounter;
        private float _firstTapTime;

        protected float ratio = 0.03f;
        
        private const float divGB = 1024f * 1024f * 1024f;

        private float totalRamGB = -1;
        
        public static DebugDisplay Instance { get; private set; }

        // New fields for scalable, clean stats panel
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _panelStyle;
        private float _lineHeight;
        private float _labelWidth;
        // Value columns: current, avg, worst
        private float _valueColWidth, _avgColWidth, _worstColWidth;
        // Thresholds (tune per project)
        [SerializeField] private float _cpuWarnMs = 16.7f;   // ~60 FPS budget
        [SerializeField] private float _cpuBadMs  = 33.3f;   // ~30 FPS budget
        [SerializeField] private int   _dcWarn    = 300;     // DrawCalls warn
        [SerializeField] private int   _dcBad     = 700;     // DrawCalls bad
        [SerializeField] private float _fpsWarn   = 50f;     // warn if below
        [SerializeField] private float _fpsBad    = 30f;     // bad if below

        // Running averages / extrema
        private double _fpsSum;   private int _fpsSamples;   private float _fpsWorst;
        private double _cpuSum;   private int _cpuSamples;   private float _cpuMax;
#if UNITY_EDITOR
        private long   _dcSum;    private int _dcSamples;    private int   _dcMax;
#endif
        private double _memSum;   private int _memSamples;   private float _memMax;

        // Stats cache
        private float _updateInterval = 0.25f; // seconds
        private float _timeLeft;
        private int _frameCount;
        private float _fps;
        private float _cpuMs;
        private float _memGB;

#if UNITY_EDITOR
        private int _drawCalls;
        private int _batches;
        private int _setPass;
        private int _tris;
        private int _verts;
        // Accumulators for averages and worst values
        private long _batchesSum; private int _batchesSamples; private int _batchesMax;
        private long _setPassSum; private int _setPassSamples; private int _setPassMax;
        private long _trisSum;    private int _trisSamples;    private int _trisMax;
        private long _vertsSum;   private int _vertsSamples;   private int _vertsMax;
#endif

        private bool _inited;
        private bool _guiStylesInited;

        public static void CreateInstance<T>() where T : DebugDisplay
        {
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
            }
            Instance = new GameObject("DebugDisplay").AddComponent<T>();
            DontDestroyOnLoad(Instance.gameObject);
        }

        private void InitIfNeeded()
        {
            if (_inited)
                return;

            int width = Screen.width, height = Screen.height;
            float startX = 0;
            float startY = 0;
#if UNITY_ANDROID || UNITY_IOS
            bool isPortrait = Screen.width < Screen.height;
            startX = isPortrait ? 0 : Screen.width * 0.05f;
            startY = isPortrait ? Screen.height * 0.05f : 0;
            ratio = 0.02f;
#endif
            _originRect = new Rect(startX, startY, width, height * ratio);

            rect = _originRect;
            _centerStyle = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = Mathf.RoundToInt(height * ratio),
                normal = { textColor = Color.white }
            };
            _leftStyle = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = Mathf.RoundToInt(height * ratio),
                normal = { textColor = Color.white }
            };

            totalRamGB = (SystemInfo.systemMemorySize / 1024f).Round(1); // 1 decimal is fine

            _timeLeft = _updateInterval;
            _frameCount = 0;

            _inited = true;
        }

        private void InitGuiStylesIfNeeded()
        {
            if (_guiStylesInited)
                return;

            // This method may use GUI.* safely because it's called from OnGUI
            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeTex(new Color(0f,0f,0f,0.4f));
            _panelStyle.normal.textColor = Color.white;
            _panelStyle.alignment = TextAnchor.UpperLeft;
            _panelStyle.padding = new RectOffset(8, 8, 6, 6);

            _labelStyle = new GUIStyle(_leftStyle)
            {
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold
            };
            _valueStyle = new GUIStyle(_leftStyle)
            {
                alignment = TextAnchor.UpperRight
            };

            _lineHeight = _leftStyle.lineHeight > 0 ? _leftStyle.lineHeight + 2 : Mathf.RoundToInt(Screen.height * ratio) + 4;
            _labelWidth = Mathf.Round(Screen.width * 0.22f);
            // Value columns: current, avg, worst
            _valueColWidth = Mathf.Round(Screen.width * 0.12f);
            _avgColWidth   = Mathf.Round(Screen.width * 0.12f);
            _worstColWidth   = Mathf.Round(Screen.width * 0.12f);

            _guiStylesInited = true;
        }

        protected virtual void Awake()
        {
            InitIfNeeded();
        }

        protected virtual void Start()
        {
            InitIfNeeded();
        }

        private void Update()
        {
            // Keyboard toggle (PC/Console)
            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_enableMobileGesture)
            {
                // Build top-left pixel rect from normalized rect defined from TOP-LEFT origin
                float areaW = Mathf.Clamp01(_mobileToggleAreaNormalized.width) * Screen.width;
                float areaH = Mathf.Clamp01(_mobileToggleAreaNormalized.height) * Screen.height;
                // Top-left origin to Unity screen (bottom-left origin)
                Rect area = new Rect(0f, Screen.height - areaH, areaW, areaH);

                // Any Began touch inside area counts as a tap
                bool tapped = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && area.Contains(t.position))
                    {
                        tapped = true; break;
                    }
                }

                float now = Time.unscaledTime;
                if (tapped)
                {
                    if (_tapCounter == 0)
                    {
                        _firstTapTime = now;
                        _tapCounter = 1;
                    }
                    else
                    {
                        // If within window, accumulate; otherwise restart window
                        if (now - _firstTapTime <= _mobileTapWindow)
                        {
                            _tapCounter++;
                        }
                        else
                        {
                            _firstTapTime = now;
                            _tapCounter = 1;
                        }
                    }

                    if (_tapCounter >= Mathf.Max(1, _mobileTapCount))
                    {
                        _visible = !_visible;
                        _tapCounter = 0; // reset
                    }
                }
                else if (_tapCounter > 0 && now - _firstTapTime > _mobileTapWindow)
                {
                    // Window expired without enough taps
                    _tapCounter = 0;
                }
            }
#endif
        }

        protected Rect NextRect(Rect current)
        {
            return new Rect(current.x, current.y + current.height, current.width, current.height);
        }

        // Helper for solid color texture
        private Texture2D MakeTex(Color c)
        {
            var tex = new Texture2D(1,1,TextureFormat.RGBA32,false);
            tex.SetPixel(0,0,c);
            tex.Apply();
            return tex;
        }

        protected virtual void OnGUI()
        {
            InitIfNeeded();
            InitGuiStylesIfNeeded();
            if (!_visible) return;

            // Update cached stats at interval
            _timeLeft -= Time.unscaledDeltaTime;
            _frameCount++;
            if (_timeLeft <= 0f)
            {
                float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-6f);
                _fps = _frameCount / _updateInterval;
                _cpuMs = dt * 1000f; // approx per-frame CPU time (unscaled)

                long memoryBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                _memGB = (memoryBytes / divGB).Round(2);

#if UNITY_EDITOR
                _drawCalls = UnityEditor.UnityStats.drawCalls;
                _batches   = UnityEditor.UnityStats.batches;
                _setPass   = UnityEditor.UnityStats.setPassCalls;
                _tris      = UnityEditor.UnityStats.triangles;
                _verts     = UnityEditor.UnityStats.vertices;
#endif

                // Update avg/worst or avg/max
                _fpsSum += _fps; _fpsSamples++;
                if (_fpsSamples == 1)
                    _fpsWorst = _fps;
                else if (_fps < _fpsWorst) _fpsWorst = _fps;

                _cpuSum += _cpuMs; _cpuSamples++; if (_cpuMs > _cpuMax) _cpuMax = _cpuMs;
                _memSum += _memGB; _memSamples++; if (_memGB > _memMax) _memMax = (float)_memGB;
#if UNITY_EDITOR
                _dcSum  += _drawCalls; _dcSamples++; if (_drawCalls > _dcMax) _dcMax = _drawCalls;
                // Accumulate averages and worst for batches/setpass/tris/verts
                _batchesSum += _batches; _batchesSamples++; if (_batches > _batchesMax) _batchesMax = _batches;
                _setPassSum += _setPass; _setPassSamples++; if (_setPass > _setPassMax) _setPassMax = _setPass;
                _trisSum    += _tris;    _trisSamples++;    if (_tris    > _trisMax)    _trisMax    = _tris;
                _vertsSum   += _verts;   _vertsSamples++;   if (_verts   > _vertsMax)   _vertsMax   = _verts;
#endif
                _timeLeft = _updateInterval;
                _frameCount = 0;
            }

            // Base rect anchored from _originRect but autosized by content lines
            rect = _originRect;

            // ---- Dynamic sizing: measure text to fit background to content ----
            // Prepare strings for each metric
            string fpsCurStr = _fps.Round(0).ToString();
            string fpsAvgStr = (_fpsSamples>0 ? (_fpsSum/_fpsSamples).Round(1) : 0).ToString();
            string fpsWorstStr = _fpsWorst.Round(0).ToString();

            string cpuCurStr = _cpuMs.Round(2) + " ms";
            string cpuAvgStr = (_cpuSamples>0 ? (_cpuSum/_cpuSamples).Round(2) : 0).ToString() + " ms";
            string cpuWorstStr = _cpuMax.Round(2) + " ms";

//#if UNITY_EDITOR
#if UNITY_EDITOR
            string dcCurStr  = _drawCalls.ToString();
            string dcAvgStr  = (_dcSamples>0 ? ((double)_dcSum/_dcSamples).Round(0) : 0).ToString();
            string dcWorstStr  = _dcMax.ToString();
            // Editor-only: batches/setpass/tris/verts avg/worst strings
            string batchesAvgStr = (_batchesSamples>0 ? ((double)_batchesSum/_batchesSamples).Round(0) : 0).ToString();
            string batchesWorstStr = _batchesMax.ToString();
            string setPassAvgStr = (_setPassSamples>0 ? ((double)_setPassSum/_setPassSamples).Round(0) : 0).ToString();
            string setPassWorstStr = _setPassMax.ToString();
            string trisAvgStr = (_trisSamples>0 ? ((double)_trisSum/_trisSamples).Round(0) : 0).ToString();
            string trisWorstStr = _trisMax.ToString();
            string vertsAvgStr = (_vertsSamples>0 ? ((double)_vertsSum/_vertsSamples).Round(0) : 0).ToString();
            string vertsWorstStr = _vertsMax.ToString();
#else
            string dcCurStr  = "N/A";
            string dcAvgStr  = "-";
            string dcWorstStr  = "-";
#endif

            string memCurStr = _memGB.Round(2) + " GB";
            string memAvgStr = (_memSamples>0 ? (_memSum/_memSamples).Round(2) : 0).ToString() + " GB";
            string memWorstStr = _memMax.Round(2) + " GB";

            // Measure labels and values
            float pad = 8f; // per column padding
            float labelW = Mathf.Max(
                _labelWidth,
                _labelStyle.CalcSize(new GUIContent("Metric")).x,
                _labelStyle.CalcSize(new GUIContent("FPS")).x,
                _labelStyle.CalcSize(new GUIContent("CPU")).x,
                _labelStyle.CalcSize(new GUIContent("DrawCalls")).x,
                _labelStyle.CalcSize(new GUIContent("RAM")).x
#if UNITY_EDITOR
                ,_labelStyle.CalcSize(new GUIContent("Batches")).x,
                _labelStyle.CalcSize(new GUIContent("SetPass")).x,
                _labelStyle.CalcSize(new GUIContent("Tris")).x,
                _labelStyle.CalcSize(new GUIContent("Verts")).x
#endif
            ) + pad;

            float curW = Mathf.Max(
                _valueColWidth,
                _valueStyle.CalcSize(new GUIContent("Current")).x,
                _valueStyle.CalcSize(new GUIContent(fpsCurStr)).x,
                _valueStyle.CalcSize(new GUIContent(cpuCurStr)).x,
                _valueStyle.CalcSize(new GUIContent(dcCurStr)).x,
                _valueStyle.CalcSize(new GUIContent(memCurStr)).x
            ) + pad;

            float avgW = Mathf.Max(
                _avgColWidth,
                _valueStyle.CalcSize(new GUIContent("Avg")).x,
                _valueStyle.CalcSize(new GUIContent(fpsAvgStr)).x,
                _valueStyle.CalcSize(new GUIContent(cpuAvgStr)).x,
                _valueStyle.CalcSize(new GUIContent(dcAvgStr)).x,
                _valueStyle.CalcSize(new GUIContent(memAvgStr)).x
#if UNITY_EDITOR
                ,_valueStyle.CalcSize(new GUIContent(batchesAvgStr)).x
                ,_valueStyle.CalcSize(new GUIContent(setPassAvgStr)).x
                ,_valueStyle.CalcSize(new GUIContent(trisAvgStr)).x
                ,_valueStyle.CalcSize(new GUIContent(vertsAvgStr)).x
#endif
            ) + pad;

            float worstW = Mathf.Max(
                _worstColWidth,
                _valueStyle.CalcSize(new GUIContent("Worst")).x,
                _valueStyle.CalcSize(new GUIContent(fpsWorstStr)).x,
                _valueStyle.CalcSize(new GUIContent(cpuWorstStr)).x,
                _valueStyle.CalcSize(new GUIContent(dcWorstStr)).x,
                _valueStyle.CalcSize(new GUIContent(memWorstStr)).x
#if UNITY_EDITOR
                ,_valueStyle.CalcSize(new GUIContent(batchesWorstStr)).x
                ,_valueStyle.CalcSize(new GUIContent(setPassWorstStr)).x
                ,_valueStyle.CalcSize(new GUIContent(trisWorstStr)).x
                ,_valueStyle.CalcSize(new GUIContent(vertsWorstStr)).x
#endif
            ) + pad;

            // Compute panel height by number of rows
            int rows = 4; // FPS, CPU, DrawCall, RAM
#if UNITY_EDITOR
            rows += 4; // Batches, SetPass, Tris, Verts (optional extras)
#endif
            int totalRows = rows + 1; // +1 for header row
            float panelHeight = totalRows * _lineHeight + _panelStyle.padding.top + _panelStyle.padding.bottom;
            float desiredWidth = labelW + curW + avgW + worstW + _panelStyle.padding.horizontal + 4f;
            var panelRect = new Rect(rect.x, rect.y, Mathf.Min(Screen.width * 0.95f, Mathf.Max(360f, desiredWidth)), panelHeight);

            GUI.Box(panelRect, GUIContent.none, _panelStyle);

            // Inner content area
            var x = panelRect.x + _panelStyle.padding.left;
            var y = panelRect.y + _panelStyle.padding.top;

            // Color logic for metrics
            Color ColorForMetric(float value, float warn, float bad, bool higherIsWorse)
            {
                if (higherIsWorse)
                {
                    if (value >= bad) return Color.red;
                    if (value >= warn) return new Color(1f,0.65f,0f); // orange
                    return Color.white;
                }
                else
                {
                    if (value <= bad) return Color.red;
                    if (value <= warn) return new Color(1f,0.65f,0f);
                    return Color.white;
                }
            }

            void Row(string label, string cur, float curVal, float warn, float bad, bool higherIsWorse, string avg, string worst)
            {
                var labelRect = new Rect(x, y, labelW, _lineHeight);
                var curRect   = new Rect(labelRect.xMax, y, curW, _lineHeight);
                var avgRect   = new Rect(curRect.xMax,   y, avgW, _lineHeight);
                var worstRect = new Rect(avgRect.xMax,   y, worstW, _lineHeight);

                GUI.Label(labelRect, label, _labelStyle);

                var prev = GUI.color;
                GUI.color = ColorForMetric(curVal, warn, bad, higherIsWorse);
                GUI.Label(curRect, cur, _valueStyle);
                GUI.color = prev;

                GUI.Label(avgRect, avg, _valueStyle);
                GUI.Label(worstRect, worst, _valueStyle);

                y += _lineHeight;
            }

            // Headers
            var headerLabel = new Rect(x, y, labelW, _lineHeight);
            var headerCur   = new Rect(headerLabel.xMax, y, curW, _lineHeight);
            var headerAvg   = new Rect(headerCur.xMax,   y, avgW, _lineHeight);
            var headerWorst = new Rect(headerAvg.xMax,   y, worstW, _lineHeight);
            GUI.Label(headerLabel, "Metric", _labelStyle);
            GUI.Label(headerCur,   "Current", _valueStyle);
            GUI.Label(headerAvg,   "Avg",     _valueStyle);
            GUI.Label(headerWorst,   "Worst",     _valueStyle);
            y += _lineHeight;

            // Rows
            Row("FPS", fpsCurStr, _fps, _fpsWarn, _fpsBad, false, fpsAvgStr, fpsWorstStr);
            Row("CPU", cpuCurStr, _cpuMs, _cpuWarnMs, _cpuBadMs, true, cpuAvgStr, cpuWorstStr);
#if UNITY_EDITOR
            Row("DrawCalls", dcCurStr, _drawCalls, _dcWarn, _dcBad, true, dcAvgStr, dcWorstStr);
#else
            Row("DrawCalls", dcCurStr, 0, 0, 0, true, dcAvgStr, dcWorstStr);
#endif
            Row("RAM", memCurStr, (float)_memGB, totalRamGB*0.75f, totalRamGB*0.9f, true, memAvgStr, memWorstStr);
#if UNITY_EDITOR
            Row("Batches", _batches.ToString(), _batches, 0, 0, true, batchesAvgStr, batchesWorstStr);
            Row("SetPass", _setPass.ToString(), _setPass, 0, 0, true, setPassAvgStr, setPassWorstStr);
            Row("Tris", _tris.ToString(), _tris, 0, 0, true, trisAvgStr, trisWorstStr);
            Row("Verts", _verts.ToString(), _verts, 0, 0, true, vertsAvgStr, vertsWorstStr);
#endif

            // Hotkey to reset averages/worst/max
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
            {
                _fpsSum = _cpuSum = _memSum = 0; _fpsSamples = _cpuSamples = _memSamples = 0;
                _fpsWorst = 0; _cpuMax = 0; _memMax = 0;
#if UNITY_EDITOR
                _dcSum = 0; _dcSamples = 0; _dcMax = 0;
                _batchesSum = 0; _batchesSamples = 0; _batchesMax = 0;
                _setPassSum = 0; _setPassSamples = 0; _setPassMax = 0;
                _trisSum = 0; _trisSamples = 0; _trisMax = 0;
                _vertsSum = 0; _vertsSamples = 0; _vertsMax = 0;
#endif
            }
        }
    }
}