#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DragonGate.Editor
{
    public class PoolScopeMonitorWindow : EditorWindow
    {
        private const float LabelWidth = 170f;
        private const float BarWidth = 200f;
        private const float BarHeight = 20f;
        private const float StatsWidth = 140f;

        private Vector2 _scrollPosition = Vector2.zero;
        private GUIStyle _headerStyle;
        private GUIStyle _poolNameStyle;
        private GUIStyle _statsStyle;
        private GUIStyle _sectionStyle;
        private bool _isStyleInitialized = false;

        [MenuItem("Window/PoolScope/Monitor")]
        public static void Open()
        {
            PoolScopeMonitorWindow window = GetWindow<PoolScopeMonitorWindow>();
            window.titleContent = new GUIContent("PoolScope Monitor");
            window.minSize = new Vector2(440f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            _isStyleInitialized = false;
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStylesInitialized();

            if (Application.isPlaying == false)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Enter Play Mode to view pool state.", _statsStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                return;
            }

            DrawHeader();
            DrawPoolList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("PoolScope Monitor", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{PoolScope.Instance.RegisteredPools.Count} pool(s)", _statsStyle);
            GUILayout.Space(8f);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label("Pool Name", _sectionStyle, GUILayout.Width(LabelWidth));
            GUILayout.Label("Usage", _sectionStyle, GUILayout.Width(BarWidth));
            GUILayout.Label("Used / Total   Peak", _sectionStyle, GUILayout.Width(StatsWidth));
            GUILayout.EndHorizontal();

            DrawSeparator();
        }

        private void DrawPoolList()
        {
            IReadOnlyDictionary<string, IPoolInfoProvider> pools = PoolScope.Instance.RegisteredPools;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            bool drewComponentHeader = false;
            bool drewClassHeader = false;

            foreach (KeyValuePair<string, IPoolInfoProvider> entry in pools)
            {
                bool isClassPool = entry.Key.StartsWith("[Class]");

                if (isClassPool == false && drewComponentHeader == false)
                {
                    DrawSectionLabel("Component Pools");
                    drewComponentHeader = true;
                }
                if (isClassPool && drewClassHeader == false)
                {
                    EditorGUILayout.Space(4f);
                    DrawSectionLabel("Class Pools");
                    drewClassHeader = true;
                }

                DrawRow(entry.Value);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(IPoolInfoProvider info)
        {
            EditorGUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);

            GUILayout.Label(info.PoolName, _poolNameStyle, GUILayout.Width(LabelWidth));

            Rect barRect = GUILayoutUtility.GetRect(BarWidth, BarHeight);
            DrawUsageBar(barRect, info);

            string statsText = $"{info.CurrentInUse} / {info.TotalCount}   Peak: {info.PeakUsage}";
            GUILayout.Label(statsText, _statsStyle, GUILayout.Width(StatsWidth));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(8f + LabelWidth);
            GUILayout.Label($"In Pool: {info.LeftInPool}  |  In Use: {info.CurrentInUse}", _statsStyle, GUILayout.Width(BarWidth + StatsWidth));
            GUILayout.EndHorizontal();
        }

        private void DrawUsageBar(Rect barRect, IPoolInfoProvider info)
        {
            EditorGUI.DrawRect(barRect, new Color(0.18f, 0.18f, 0.18f));

            if (info.TotalCount <= 0) return;

            float usedRatio = (float)info.CurrentInUse / info.TotalCount;
            float availableRatio = (float)info.LeftInPool / info.TotalCount;

            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * usedRatio, barRect.height), info.BarColor);

            Color availableColor = info.BarColor * 0.4f;
            availableColor.a = 1f;
            EditorGUI.DrawRect(new Rect(barRect.x + barRect.width * usedRatio, barRect.y, barRect.width * availableRatio, barRect.height), availableColor);

            if (info.PeakUsage > 0)
            {
                float peakX = barRect.x + barRect.width * ((float)info.PeakUsage / info.TotalCount) - 1f;
                EditorGUI.DrawRect(new Rect(peakX, barRect.y, 2f, barRect.height), new Color(1f, 1f, 1f, 0.85f));
            }

            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width, 1f), new Color(0.35f, 0.35f, 0.35f));
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1f, barRect.width, 1f), new Color(0.35f, 0.35f, 0.35f));
        }

        private void DrawSectionLabel(string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label($"-- {label} --", _sectionStyle);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(2f);
            Rect lineRect = GUILayoutUtility.GetRect(position.width, 1f);
            EditorGUI.DrawRect(lineRect, new Color(0.35f, 0.35f, 0.35f));
            EditorGUILayout.Space(4f);
        }

        private void EnsureStylesInitialized()
        {
            if (_isStyleInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _poolNameStyle = new GUIStyle(EditorStyles.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            _statsStyle = new GUIStyle(EditorStyles.label) { fontSize = 10, normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };
            _sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };

            _isStyleInitialized = true;
        }
    }
}
#endif
