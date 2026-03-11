using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        // ── 레이아웃 상수 ─────────────────────────────────────────────
        private const float TOOLBAR_H = 24f;
        
        // 캔버스 뷰
        private Vector2 scrollOffset = Vector2.zero;
        // 줌
        private float _zoom = 1f;
        private Matrix4x4 _prevMatrix;
        private const float ZOOM_MIN = 0.6f;
        private const float ZOOM_MAX = 1.3f;
        private const float ZOOM_STEP = 0.1f;
        
        // ── 캐시된 GUIStyle ───────────────────────────────────────────
        private bool _stylesReady;
        private GUIStyle _nodeHeaderStyle;
        private GUIStyle _nodePreviewStyle;
        private GUIStyle _nodePreviewRichStyle;
        private GUIStyle _nodeChoiceStyle;
        private GUIStyle _nodeTrueStyle;
        private GUIStyle _nodeFalseStyle;
        private GUIStyle _nodeNextStyle;
        private GUIStyle _inspectorTitle14;
        private GUIStyle _inspectorTitle12;
        private GUIStyle _inspectorTitle13;
        private GUIStyle _centeredLabelStyle;
        
        private const float UNITY_TAB_H = 21f; // EditorWindow 탭 높이 고정값
        
        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _nodeHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = Color.white },
            };

            _nodePreviewStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = false,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
            };

            _nodePreviewRichStyle = new GUIStyle(_nodePreviewStyle) { richText = true };
            _nodeChoiceStyle = new GUIStyle(_nodePreviewStyle) { normal = { textColor = new Color(1f, 0.88f, 0.4f) } };
            _nodeTrueStyle = new GUIStyle(_nodePreviewStyle) { normal = { textColor = new Color(0.4f, 0.95f, 0.45f) } };
            _nodeFalseStyle = new GUIStyle(_nodePreviewStyle) { normal = { textColor = new Color(0.95f, 0.45f, 0.45f) } };
            _nodeNextStyle = new GUIStyle(_nodePreviewStyle) { normal = { textColor = new Color(0.55f, 0.95f, 0.55f) } };

            _inspectorTitle14 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _inspectorTitle12 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _inspectorTitle13 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            _centeredLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                richText = true,
            };

            _stylesReady = true;
        }
        // ══════════════════════════════════════════════════════════════
        //  Toolbar
        // ══════════════════════════════════════════════════════════════

        private void DrawToolbar()
        {
            GUILayout.BeginArea(new Rect(0, 0, position.width - _inspectorWidth - SPLITTER_W, TOOLBAR_H));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("📂 Load", EditorStyles.toolbarButton, GUILayout.Width(70)))
                PickAndLoadGraph();

            if (GUILayout.Button("✨ New", EditorStyles.toolbarButton, GUILayout.Width(60)))
                CreateNewGraph();

            if (_graph != null)
            {
                GUILayout.Label($"  {_graph.name}", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("+ Character", EditorStyles.toolbarButton, GUILayout.Width(80))) AddNode(DialogueNodeType.Character);
                if (GUILayout.Button("+ Narration", EditorStyles.toolbarButton, GUILayout.Width(80))) AddNode(DialogueNodeType.Narration);
                if (GUILayout.Button("+ Condition", EditorStyles.toolbarButton, GUILayout.Width(80))) AddNode(DialogueNodeType.Condition);
                if (GUILayout.Button("+ Start", EditorStyles.toolbarButton, GUILayout.Width(60))) AddNode(DialogueNodeType.Start);
                if (GUILayout.Button("+ End", EditorStyles.toolbarButton, GUILayout.Width(55))) AddNode(DialogueNodeType.ChapterEnd);
                GUILayout.Space(8);
                if (GUILayout.Button("⌖ Reset View", EditorStyles.toolbarButton, GUILayout.Width(85)))
                {
                    scrollOffset = Vector2.zero;
                }

                if (GUILayout.Button("💾 Save", EditorStyles.toolbarButton, GUILayout.Width(60))) SaveGraph();
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("그래프를 로드하거나 새로 만드세요", EditorStyles.toolbarButton);
            }

            GUILayout.FlexibleSpace();
            var playButton = EditorApplication.isPlaying == false ? "▶️ Play" : "⏹️ Stop";
            if (GUILayout.Button(playButton, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                else
                {
                    OpenDialoguePreviewScene(_graph);
                }
            }
            
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("🌐", EditorStyles.toolbarButton, GUILayout.Width(22));
            _selectedLocaleIdx = EditorGUILayout.Popup(_selectedLocaleIdx, _localeNames, EditorStyles.toolbarPopup, GUILayout.Width(90));
            if (EditorGUI.EndChangeCheck() && _selectedLocaleIdx != -1)
            {
                var locales = LocalizationSettings.AvailableLocales.Locales;
                if (_selectedLocaleIdx < locales.Count)
                {
                    LocalizationSettings.SelectedLocale = locales[_selectedLocaleIdx];
                    // 노드 캔버스 미리보기 텍스트 갱신
                    Repaint();
                }
            }
                
            var settingsLabel = _showSettings ? "- Settings" : "⚙ Settings";
            if (GUILayout.Button(settingsLabel, EditorStyles.toolbarButton, GUILayout.Width(80)))
                _showSettings = !_showSettings;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ══════════════════════════════════════════════════════════════
        //  Canvas
        // ══════════════════════════════════════════════════════════════

        private void DrawCanvas(Rect rect)
        {
            EditorGUI.DrawRect(rect, BG_COLOR);
            DrawGrid(rect, 20f, GRID_FINE);
            DrawGrid(rect, 100f, GRID_COARSE);

            // GUI.BeginGroup(rect);

            if (_graph == null)
            {
                GUI.BeginGroup(rect);
                DrawCenteredLabel(rect, "그래프를 로드하거나 새로 만드세요\n(툴바 또는 우클릭 메뉴 사용)");
                GUI.EndGroup();
                return;
            }
            BeginZoomArea(rect);
            
            // ── 줌 행렬 적용 ──────────────────────────────────────────────
            var prevMatrix = GUI.matrix;
            // var zoomPivot = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            // GUIUtility.ScaleAroundPivot(Vector2.one * _zoom, zoomPivot);
            // var pivot = Vector2.zero;
            // GUIUtility.ScaleAroundPivot(Vector2.one * _zoom, pivot);

            foreach (var n in _graph.nodes) DrawNodeConnections(n);

            if (connectFromNode != null)
            {
                var from = GetOutputPortPos(connectFromNode, connectFromChoiceIdx);
                // BeginZoomArea 안: e.mousePosition은 pre-scale GUI 공간 = 드로잉 좌표와 동일 공간.
                var mousePosInCanvas = Event.current.mousePosition;

                // 가장 가까운 Input 포트에 스냅 → 시각적 끝점 정렬
                var snapEnd = mousePosInCanvas;
                foreach (var n in _graph.nodes)
                {
                    if (n == connectFromNode) continue;
                    var portPos = GetInputPortPos(n);
                    if (Vector2.Distance(mousePosInCanvas, portPos) < PORT_R + 5f)
                    {
                        snapEnd = portPos;
                        break;
                    }
                }

                DrawBezier(from, snapEnd, new Color(1f, 0.9f, 0.3f, 0.7f));
                Repaint();
            }

            foreach (var n in _graph.nodes) DrawNode(n);
            
            GUI.matrix = prevMatrix;

            // GUI.EndGroup();
            EndZoomArea(rect);
        }
        
        private void BeginZoomArea(Rect rect)
        {
            // Unity 기본 윈도우 클립을 스택에서 꺼냄
            GUI.EndClip();

            // 줌 아웃 시 더 넓은 영역을 클리핑 허용
            var zoomedRect = new Rect(
                rect.x,
                rect.y + UNITY_TAB_H,
                rect.width  / _zoom,
                rect.height / _zoom
            );
            GUI.BeginClip(zoomedRect);

            _prevMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * _zoom, Vector2.zero);
        }
        
        private void EndZoomArea(Rect rect)
        {
            GUI.matrix = _prevMatrix;
            GUI.EndClip();

            // 꺼냈던 윈도우 클립 복원
            GUI.BeginClip(new Rect(0, UNITY_TAB_H, Screen.width, Screen.height));
        }

        // ── 그리드 ────────────────────────────────────────────────────

        private void DrawGrid(Rect rect, float step, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;

            float ox = scrollOffset.x % step;
            float oy = scrollOffset.y % step;

            for (float x = ox; x < rect.width; x += step)
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, rect.height));
            for (float y = oy; y < rect.height; y += step)
                Handles.DrawLine(new Vector3(0, y), new Vector3(rect.width, y));

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawCenteredLabel(Rect rect, string text)
        {
            GUI.Label(new Rect(0, 0, rect.width, rect.height), text, _centeredLabelStyle);
        }
    }
}
