// ============================================================
//  Visual Novel Framework – Dialogue Graph Editor Window
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Localization.Settings;

namespace DragonGate.Editor
{
    public class DialogueGraphEditorWindow : EditorWindow
    {
        // ── 레이아웃 상수 ─────────────────────────────────────────────
        private const float NODE_W = 210f;
        private const float HEADER_H = 28f;
        private const float PREVIEW_H = 22f;
        private const float CHOICE_ROW_H = 20f;
        private const float PORT_R = 7f;
        private const float INSPECTOR_W = 310f;
        private const float TOOLBAR_H = 24f;

        // ── 팔레트 ────────────────────────────────────────────────────
        private static readonly Color BG_COLOR = new Color(0.13f, 0.13f, 0.15f);
        private static readonly Color GRID_FINE = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color GRID_COARSE = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color INSPECTOR_BG = new Color(0.17f, 0.17f, 0.20f);
        private static readonly Color BORDER_DEFAULT = new Color(0.40f, 0.40f, 0.50f);
        private static readonly Color BORDER_SELECTED = new Color(0.25f, 0.75f, 1.00f);

        private static readonly Dictionary<DialogueNodeType, Color> NODE_COLORS =
            new Dictionary<DialogueNodeType, Color>
            {
                { DialogueNodeType.Start, new Color(0.15f, 0.48f, 0.22f) },
                { DialogueNodeType.NPC, new Color(0.20f, 0.30f, 0.48f) },
                { DialogueNodeType.Player, new Color(0.28f, 0.42f, 0.30f) },
                { DialogueNodeType.Narration, new Color(0.38f, 0.28f, 0.48f) },
                { DialogueNodeType.ChapterEnd, new Color(0.50f, 0.18f, 0.18f) },
            };

        private static readonly Dictionary<DialogueNodeType, string> NODE_ICONS =
            new Dictionary<DialogueNodeType, string>
            {
                { DialogueNodeType.Start, "▶ START" },
                { DialogueNodeType.NPC, "💬 NPC" },
                { DialogueNodeType.Player, "🗣 PLAYER" },
                { DialogueNodeType.Narration, "📖 NARRATION" },
                { DialogueNodeType.ChapterEnd, "■ CHAPTER END" },
            };

        // ── 상태 ──────────────────────────────────────────────────────
        private DialogueGraph graph;
        private DialogueNode selectedNode;
        private SerializedObject _graphSO;

        // 캔버스 뷰
        private Vector2 scrollOffset = Vector2.zero;

        // 드래그
        private bool isDraggingNode;
        private bool isDraggingCanvas;
        private Vector2 lastMousePos;

        // 연결 중
        private DialogueNode connectFromNode;
        private int connectFromChoiceIdx; // -1 = next port

        // 인스펙터 스크롤
        private Vector2 inspectorScroll;

        // 접기 상태
        private bool foldEnterEvents = true;
        private bool foldExitEvents = false;

        // ── 메뉴 진입점 ───────────────────────────────────────────────

        [MenuItem("DragonGate/Open Visual Novel Dialogue Graph Editor")]
        public static DialogueGraphEditorWindow OpenWindow()
        {
            var w = GetWindow<DialogueGraphEditorWindow>("Visual Novel Graph Editor");
            w.minSize = new Vector2(900, 600);
            return w;
        }

        public static void OpenGraph(DialogueGraph g)
        {
            var w = OpenWindow();
            w.LoadGraph(g);
        }

        private void OnEnable()
        {
            if (graph != null)
                _graphSO = new SerializedObject(graph);
                
            // 로컬라이제이션 시스템 초기화 먼저 완료
            var initOp = LocalizationSettings.InitializationOperation;
            if (!initOp.IsDone)
                initOp.WaitForCompletion();

            if (LocalizationSettings.SelectedLocale == null)
            {
                var firstLocale = LocalizationSettings.AvailableLocales.Locales[0];
                LocalizationSettings.SelectedLocale = firstLocale;
            }
        }

        // ── 그래프 로드 ───────────────────────────────────────────────

        private void LoadGraph(DialogueGraph g)
        {
            graph = g;
            selectedNode = null;
            _graphSO = g != null ? new SerializedObject(g) : null;
            Repaint();
        }

        // ══════════════════════════════════════════════════════════════
        //  OnGUI
        // ══════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            // graphSO가 무효화된 경우 재생성
            if (graph != null && (_graphSO == null || _graphSO.targetObject == null))
                _graphSO = new SerializedObject(graph);
                
            var canvasRect = new Rect(0, TOOLBAR_H, position.width - INSPECTOR_W, position.height - TOOLBAR_H);
            var inspectorRect = new Rect(position.width - INSPECTOR_W, TOOLBAR_H, INSPECTOR_W, position.height - TOOLBAR_H);

            DrawToolbar();
            DrawCanvas(canvasRect);
            DrawInspectorPanel(inspectorRect);
            HandleEvents(Event.current, canvasRect);

            if (GUI.changed) Repaint();
        }

        // ══════════════════════════════════════════════════════════════
        //  Toolbar
        // ══════════════════════════════════════════════════════════════

        private void DrawToolbar()
        {
            GUILayout.BeginArea(new Rect(0, 0, position.width - INSPECTOR_W, TOOLBAR_H));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("📂 Load", EditorStyles.toolbarButton, GUILayout.Width(70)))
                PickAndLoadGraph();

            if (GUILayout.Button("✨ New", EditorStyles.toolbarButton, GUILayout.Width(60)))
                CreateNewGraph();

            if (graph != null)
            {
                GUILayout.Label($"  {graph.name}", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("+ NPC", EditorStyles.toolbarButton, GUILayout.Width(60))) AddNode(DialogueNodeType.NPC);
                if (GUILayout.Button("+ Player", EditorStyles.toolbarButton, GUILayout.Width(65))) AddNode(DialogueNodeType.Player);
                if (GUILayout.Button("+ Narration", EditorStyles.toolbarButton, GUILayout.Width(80))) AddNode(DialogueNodeType.Narration);
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

            GUI.BeginGroup(rect);

            if (graph == null)
            {
                DrawCenteredLabel(rect, "그래프를 로드하거나 새로 만드세요\n(툴바 또는 우클릭 메뉴 사용)");
                GUI.EndGroup();
                return;
            }

            foreach (var n in graph.nodes) DrawNodeConnections(n);

            if (connectFromNode != null)
            {
                var from = GetOutputPortPos(connectFromNode, connectFromChoiceIdx);
                DrawBezier(from, Event.current.mousePosition - new Vector2(0, TOOLBAR_H), new Color(1f, 0.9f, 0.3f, 0.7f));
                Repaint();
            }

            foreach (var n in graph.nodes) DrawNode(n);

            GUI.EndGroup();
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

        // ── 노드 그리기 ───────────────────────────────────────────────

        private void DrawNode(DialogueNode node)
        {
            var rect = GetNodeRect(node);

            EditorGUI.DrawRect(new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height),
                new Color(0, 0, 0, 0.45f));

            var baseColor = NODE_COLORS.TryGetValue(node.nodeType, out var nc) ? nc : Color.gray;
            if (selectedNode == node)
                baseColor = Color.Lerp(baseColor, new Color(0.5f, 0.8f, 1f), 0.35f);
            EditorGUI.DrawRect(rect, baseColor);

            DrawOutline(rect, selectedNode == node ? BORDER_SELECTED : BORDER_DEFAULT, 1.5f);

            var headerRect = new Rect(rect.x, rect.y, rect.width, HEADER_H);
            EditorGUI.DrawRect(headerRect, new Color(0, 0, 0, 0.25f));

            var hs = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = Color.white }
            };
            GUI.Label(headerRect,
                NODE_ICONS.TryGetValue(node.nodeType, out var icon) ? icon : node.nodeType.ToString(), hs);

            float y = rect.y + HEADER_H;
            var ps = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = false,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            string speakerName = node.SpeakerName.IsEmpty ? "(화자 없음)" : node.SpeakerName.GetLocalizedString();
            string preview = null;
            if (node.DialogueText.IsEmpty)
            {
                preview = "<텍스트 없음>";
            }
            else
            {
                var localized = node.DialogueText.GetLocalizedString();
                preview = localized.Length > 24 ? localized.Substring(0, 24) + "…" : localized;
            }

            if (node.nodeType != DialogueNodeType.Start && node.nodeType != DialogueNodeType.ChapterEnd)
            {
                GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, PREVIEW_H),
                    $"<b>{speakerName}</b>  {preview}", new GUIStyle(ps) { richText = true });
                y += PREVIEW_H;
            }
            else if (node.nodeType == DialogueNodeType.ChapterEnd)
            {
                string ch = string.IsNullOrEmpty(node.TargetChapterId) ? "(챕터 미지정)" : $"→ {node.TargetChapterId}";
                GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, PREVIEW_H), ch, ps);
                y += PREVIEW_H;
            }

            DrawPort(GetInputPortPos(node), portInputColor: true);

            if (node.Choices != null)
            {
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    var ch = node.Choices[i];
                    var cs = new GUIStyle(ps) { normal = { textColor = new Color(1f, 0.88f, 0.4f) } };
                    string ct = null;
                    if (ch.ChoiceText == null || ch.ChoiceText.IsEmpty)
                    {
                        ct = $"Choice {i + 1}";
                    }
                    else
                    {
                        var localized = ch.ChoiceText.GetLocalizedString();
                        ct = localized.Length > 20 ? localized.Substring(0, 20) + "…" : localized;
                    }

                    GUI.Label(new Rect(rect.x + 8, y, rect.width - 24, CHOICE_ROW_H), $"▸ {ct}", cs);
                    DrawPort(GetOutputPortPos(node, i), portInputColor: false);
                    y += CHOICE_ROW_H;
                }
            }

            if (node.nodeType != DialogueNodeType.ChapterEnd)
            {
                var ns = new GUIStyle(ps) { normal = { textColor = new Color(0.55f, 0.95f, 0.55f) } };
                GUI.Label(new Rect(rect.x + 8, y, rect.width - 24, CHOICE_ROW_H), "▸ Next", ns);
                DrawPort(GetOutputPortPos(node, -1), portInputColor: false);
            }
        }

        private void DrawPort(Vector2 center, bool portInputColor)
        {
            var col = portInputColor ? new Color(0.3f, 0.75f, 1.0f) : new Color(0.95f, 0.85f, 0.25f);
            var r = new Rect(center.x - PORT_R, center.y - PORT_R, PORT_R * 2, PORT_R * 2);
            EditorGUI.DrawRect(r, col);
            DrawOutline(r, Color.white, 1f);
        }

        private void DrawOutline(Rect r, Color color, float w)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawSolidRectangleWithOutline(r, Color.clear, color);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        // ── 연결선 ────────────────────────────────────────────────────

        private void DrawNodeConnections(DialogueNode from)
        {
            if (from.Choices != null)
            {
                for (int i = 0; i < from.Choices.Count; i++)
                {
                    var t = graph.GetNode(from.Choices[i].TargetNodeId);
                    if (t != null)
                        DrawBezier(GetOutputPortPos(from, i), GetInputPortPos(t),
                            new Color(1f, 0.85f, 0.25f, 0.8f));
                }
            }

            var next = graph.GetNode(from.NextNodeId);
            if (next != null)
                DrawBezier(GetOutputPortPos(from, -1), GetInputPortPos(next),
                    new Color(0.4f, 0.95f, 0.45f, 0.8f));
        }

        private void DrawBezier(Vector2 s, Vector2 e, Color col)
        {
            float dx = Mathf.Max(50f, Mathf.Abs(e.x - s.x) * 0.5f);
            Handles.BeginGUI();
            Handles.DrawBezier(s, e,
                new Vector3(s.x + dx, s.y),
                new Vector3(e.x - dx, e.y),
                col, null, 2f);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        // ── 포트 위치 계산 ────────────────────────────────────────────

        private Rect GetNodeRect(DialogueNode n)
        {
            float h = HEADER_H;
            if (n.nodeType != DialogueNodeType.Start)
                h += PREVIEW_H;
            if (n.Choices != null)
                h += n.Choices.Count * CHOICE_ROW_H;
            if (n.nodeType != DialogueNodeType.ChapterEnd)
                h += CHOICE_ROW_H;
            h += 10;

            return new Rect(n.editorPosition.x + scrollOffset.x,
                n.editorPosition.y + scrollOffset.y,
                NODE_W, h);
        }

        private Vector2 GetInputPortPos(DialogueNode n)
        {
            var r = GetNodeRect(n);
            return new Vector2(r.x, r.y + HEADER_H * 0.5f);
        }

        private Vector2 GetOutputPortPos(DialogueNode n, int choiceIdx)
        {
            var r = GetNodeRect(n);
            float y = r.y + HEADER_H;
            if (n.nodeType != DialogueNodeType.Start) y += PREVIEW_H;

            if (choiceIdx >= 0 && n.Choices != null)
                y += choiceIdx * CHOICE_ROW_H + CHOICE_ROW_H * 0.5f;
            else
                y += (n.Choices?.Count ?? 0) * CHOICE_ROW_H + CHOICE_ROW_H * 0.5f;

            return new Vector2(r.xMax, y);
        }

        // ══════════════════════════════════════════════════════════════
        //  Inspector
        // ══════════════════════════════════════════════════════════════

        private void DrawInspectorPanel(Rect rect)
        {
            EditorGUI.DrawRect(rect, INSPECTOR_BG);
            DrawOutline(rect, new Color(0.3f, 0.3f, 0.4f), 1f);

            GUILayout.BeginArea(new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12));
            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);

            if (graph == null)
                DrawNoGraphInspector();
            else if (selectedNode == null)
                DrawGraphInspector();
            else
                DrawNodeInspector(selectedNode);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNoGraphInspector()
        {
            GUILayout.Label("Visual Novel Graph", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            GUILayout.Space(8);
            GUILayout.Label("그래프를 로드하거나 새로 생성하세요.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            if (GUILayout.Button("📂  그래프 로드", GUILayout.Height(30))) PickAndLoadGraph();
            if (GUILayout.Button("✨  새 그래프 생성", GUILayout.Height(30))) CreateNewGraph();
            GUILayout.Space(10);
            var dragTarget = EditorGUILayout.ObjectField("드래그로 로드", null, typeof(DialogueGraph), false) as DialogueGraph;
            if (dragTarget != null) LoadGraph(dragTarget);
        }

        private void DrawGraphInspector()
        {
            GUILayout.Label("📊  Graph Settings", new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            GUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            graph.graphTitle = EditorGUILayout.TextField("제목", graph.graphTitle);
            graph.graphId = EditorGUILayout.TextField("Graph ID", graph.graphId);
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Start Node ID", string.IsNullOrEmpty(graph.startNodeId) ? "없음" : graph.startNodeId);
            EditorGUILayout.LabelField("노드 수", graph.nodes.Count.ToString());
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(graph);

            GUILayout.Space(12);
            GUILayout.Label("단축 조작", EditorStyles.boldLabel);
            GUILayout.Label(
                "• 노드 클릭 → 선택\n" +
                "• 노드 드래그 → 이동\n" +
                "• 출력 포트 드래그 → 연결\n" +
                "• 캔버스 드래그(중클릭) → 뷰 이동\n" +
                "• 우클릭 → 컨텍스트 메뉴\n" +
                "• Delete 키 → 선택 노드 삭제",
                EditorStyles.wordWrappedMiniLabel);
        }

        // ── 노드 인스펙터 ─────────────────────────────────────────────

        private void DrawNodeInspector(DialogueNode node)
        {
            // ── SerializedObject 준비 ─────────────────────────────────
            if (_graphSO == null) return;
            var so = _graphSO;
            int nodeIdx = graph.nodes.IndexOf(node);
            string nodePath = $"nodes.Array.data[{nodeIdx}]";
            so.Update();

            EditorGUI.BeginChangeCheck();

            var icon = NODE_ICONS.TryGetValue(node.nodeType, out var ic) ? ic : node.nodeType.ToString();
            GUILayout.Label(icon, new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
            EditorGUILayout.LabelField("Node ID", node.nodeId, EditorStyles.miniLabel);
            GUILayout.Space(6);

            node.nodeType = (DialogueNodeType)EditorGUILayout.EnumPopup("타입", node.nodeType);
            GUILayout.Space(6);

            if (node.nodeType != DialogueNodeType.Start &&
                node.nodeType != DialogueNodeType.ChapterEnd)
            {
                GUILayout.Label("대화 내용", EditorStyles.boldLabel);
                var speakerNameProp = so.FindProperty($"{nodePath}.SpeakerName");
                EditorGUILayout.PropertyField(speakerNameProp, new GUIContent("화자 이름"));
                GUILayout.Space(6);
                var spriteProp = so.FindProperty($"{nodePath}.SpeakerPortrait");
                EditorGUILayout.PropertyField(spriteProp, new GUIContent("화자 초상화"));
                GUILayout.Space(6);
                var dialogueTextProp = so.FindProperty($"{nodePath}.DialogueText");
                EditorGUILayout.PropertyField(dialogueTextProp, new GUIContent("대화 텍스트"));
            }

            if (node.nodeType == DialogueNodeType.ChapterEnd)
            {
                GUILayout.Label("챕터 전환", EditorStyles.boldLabel);
                node.TargetChapterId = EditorGUILayout.TextField("이동할 챕터 ID", node.TargetChapterId);
            }

            GUILayout.Space(8);

            if (node.nodeType != DialogueNodeType.Start &&
                node.nodeType != DialogueNodeType.ChapterEnd)
            {
                GUILayout.Label("선택지", EditorStyles.boldLabel);

                var choiceProp = so.FindProperty($"{nodePath}.Choices");

                for (int i = 0; i < choiceProp.arraySize; i++)
                {
                    var choiceDataProp = choiceProp.GetArrayElementAtIndex(i);

                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"선택지 {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        // ✅ SerializedProperty로 삭제
                        choiceProp.DeleteArrayElementAtIndex(i);
                        so.ApplyModifiedProperties();
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        break;
                    }

                    GUILayout.EndHorizontal();

                    // ✅ LocalizedString - PropertyField로만 접근해야 isExpanded 유지됨
                    EditorGUILayout.PropertyField(
                        choiceDataProp.FindPropertyRelative("ChoiceText"),
                        new GUIContent("텍스트")
                    );

                    // ✅ IsEnabled도 PropertyField로 통일
                    EditorGUILayout.PropertyField(
                        choiceDataProp.FindPropertyRelative("IsEnabled"),
                        new GUIContent("활성화")
                    );

                    // TargetNodeId는 커스텀 표시가 필요하므로 예외적으로 직접 접근
                    var targetIdProp = choiceDataProp.FindPropertyRelative("TargetNodeId");
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("→ 연결 노드",
                        string.IsNullOrEmpty(targetIdProp.stringValue) ? "없음 (포트로 연결)" : targetIdProp.stringValue,
                        EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(targetIdProp.stringValue) && GUILayout.Button("해제", GUILayout.Width(38)))
                    {
                        // ✅ SerializedProperty로 수정
                        targetIdProp.stringValue = string.Empty;
                        so.ApplyModifiedProperties();
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }

                if (GUILayout.Button("+ 선택지 추가"))
                {
                    // ✅ SerializedProperty로 추가
                    choiceProp.InsertArrayElementAtIndex(choiceProp.arraySize);
                    // 새 요소 초기화
                    var newElem = choiceProp.GetArrayElementAtIndex(choiceProp.arraySize - 1);
                    newElem.FindPropertyRelative("IsEnabled").boolValue = true;
                    newElem.FindPropertyRelative("TargetNodeId").stringValue = string.Empty;
                    so.ApplyModifiedProperties();
                }

                GUILayout.Space(6);

                GUILayout.Label("기본 다음 노드 (Next)", EditorStyles.boldLabel);
                var nextNodeIdProp = so.FindProperty($"{nodePath}.NextNodeId");
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("→",
                    string.IsNullOrEmpty(nextNodeIdProp.stringValue) ? "없음 (포트로 연결)" : nextNodeIdProp.stringValue,
                    EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(nextNodeIdProp.stringValue) && GUILayout.Button("해제", GUILayout.Width(38)))
                {
                    nextNodeIdProp.stringValue = string.Empty;
                    so.ApplyModifiedProperties();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // ── 이벤트 섹션: SerializedObject 경로 전달 ───────────────
            DrawEventsSection("진입 이벤트 (Enter)", so, $"{nodePath}.EnterEvents", ref foldEnterEvents);
            GUILayout.Space(4);
            DrawEventsSection("퇴장 이벤트 (Exit)", so, $"{nodePath}.ExitEvents", ref foldExitEvents);

            // BeginChangeCheck 결과와 SO 변경 모두 반영
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(graph);

            so.ApplyModifiedProperties();
        }

        // ── 이벤트 섹션 ───────────────────────────────────────────────
        //  AssetReference 필드는 SerializedProperty + PropertyField 로,
        //  나머지는 기존 방식 유지.
        // ─────────────────────────────────────────────────────────────

        private void DrawEventsSection(string label,
            SerializedObject so,
            string eventsPath,
            ref bool fold)
        {
            fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, label);
            if (!fold)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            var eventsProp = so.FindProperty(eventsPath);
            if (eventsProp == null)
            {
                EditorGUILayout.HelpBox($"프로퍼티를 찾을 수 없습니다: {eventsPath}", MessageType.Warning);
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            for (int i = 0; i < eventsProp.arraySize; i++)
            {
                var evtProp = eventsProp.GetArrayElementAtIndex(i);
                var typeProp = evtProp.FindPropertyRelative("eventType");
                var eventType = (DialogueEventType)typeProp.enumValueIndex;

                GUILayout.BeginVertical(EditorStyles.helpBox);

                // ── 헤더: 타입 드롭다운 + 삭제 버튼 ─────────────────
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(typeProp, GUIContent.none, GUILayout.Width(160));
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    eventsProp.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }

                GUILayout.EndHorizontal();

                // ── 타입별 필드 ───────────────────────────────────────
                switch (eventType)
                {
                    case DialogueEventType.SetBackground:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), new GUIContent("배경 스프라이트"));
                        break;

                    case DialogueEventType.ShowCharacterSprite:
                    case DialogueEventType.HideCharacterSprite:
                        EditorGUILayout.PropertyField(
                            evtProp.FindPropertyRelative("CharacterId"),
                            new GUIContent("캐릭터 ID"));
                        EditorGUILayout.PropertyField(
                            evtProp.FindPropertyRelative("CharacterPosition"),
                            new GUIContent("위치"));
                        if (eventType == DialogueEventType.ShowCharacterSprite)
                            EditorGUILayout.PropertyField(
                                evtProp.FindPropertyRelative("CharacterSprite"),
                                new GUIContent("스프라이트"));
                        break;

                    case DialogueEventType.SetCharacterEmotion:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterId"), new GUIContent("캐릭터 ID"));
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterSprite"), new GUIContent("감정 스프라이트"));
                        break;

                    case DialogueEventType.PlayAnimation:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterId"), new GUIContent("오브젝트 이름"));
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("AnimationTrigger"), new GUIContent("애니메이션 트리거"));
                        break;

                    // ── AssetReference: PropertyField 필수 ───────────
                    case DialogueEventType.PlayEffect:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), new GUIContent("이펙트 Prefab"));
                        break;

                    case DialogueEventType.ShowUI:
                    case DialogueEventType.HideUI:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("UiElementId"), new GUIContent("UI 오브젝트 이름"));
                        break;

                    case DialogueEventType.PlayBGM:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), new GUIContent("BGM"));
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Volume"), new GUIContent("BGM Volume"));
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), new GUIContent("BGM Fade Duration"));
                        break;
                    case DialogueEventType.PlaySFX:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), new GUIContent("SFX"));
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Volume"), new GUIContent("SFX Volume"));
                        break;

                    case DialogueEventType.FadeIn:
                    case DialogueEventType.FadeOut:
                    case DialogueEventType.Wait:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), new GUIContent("시간(초)"));
                        break;
                }

                EditorGUILayout.PropertyField(
                    evtProp.FindPropertyRelative("WaitForCompletion"),
                    new GUIContent("완료 대기"));

                GUILayout.EndVertical();
                GUILayout.Space(2);
            }

            if (GUILayout.Button("+ 이벤트 추가"))
            {
                eventsProp.InsertArrayElementAtIndex(eventsProp.arraySize);
                so.ApplyModifiedProperties();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ══════════════════════════════════════════════════════════════
        //  이벤트 처리
        // ══════════════════════════════════════════════════════════════

        private void HandleEvents(Event e, Rect canvasRect)
        {
            bool inCanvas = canvasRect.Contains(e.mousePosition);

            switch (e.type)
            {
                case EventType.MouseDown when inCanvas:
                    if (e.button == 0) OnLeftDown(e, canvasRect);
                    else if (e.button == 1) OnRightDown(e, canvasRect);
                    else if (e.button == 2) isDraggingCanvas = true;
                    lastMousePos = e.mousePosition;
                    break;

                case EventType.MouseUp:
                    if (e.button == 0) OnLeftUp(e, canvasRect);
                    isDraggingCanvas = false;
                    isDraggingNode = false;
                    break;

                case EventType.MouseDrag:
                    var delta = e.mousePosition - lastMousePos;
                    lastMousePos = e.mousePosition;

                    if (isDraggingNode && selectedNode != null)
                    {
                        selectedNode.editorPosition += delta;
                        EditorUtility.SetDirty(graph);
                        e.Use();
                    }
                    else if (isDraggingCanvas || e.button == 2)
                    {
                        scrollOffset += delta;
                        e.Use();
                    }
                    else if (connectFromNode != null)
                    {
                        e.Use();
                    }

                    break;

                case EventType.KeyDown when e.keyCode == KeyCode.Delete && selectedNode != null:
                    DeleteSelectedNode();
                    e.Use();
                    break;
            }
        }

        private void OnLeftDown(Event e, Rect canvasRect)
        {
            if (graph == null) return;

            var mp = e.mousePosition - new Vector2(0, TOOLBAR_H);

            foreach (var n in graph.nodes)
            {
                if (n.Choices != null)
                    for (int i = 0; i < n.Choices.Count; i++)
                        if (Vector2.Distance(mp, GetOutputPortPos(n, i)) < PORT_R + 2)
                        {
                            connectFromNode = n;
                            connectFromChoiceIdx = i;
                            e.Use();
                            return;
                        }

                if (n.nodeType != DialogueNodeType.ChapterEnd &&
                    Vector2.Distance(mp, GetOutputPortPos(n, -1)) < PORT_R + 2)
                {
                    connectFromNode = n;
                    connectFromChoiceIdx = -1;
                    e.Use();
                    return;
                }
            }

            for (int ni = graph.nodes.Count - 1; ni >= 0; ni--)
            {
                var n = graph.nodes[ni];
                if (GetNodeRect(n).Contains(mp))
                {
                    selectedNode = n;
                    isDraggingNode = true;
                    GUI.changed = true;
                    e.Use();
                    return;
                }
            }

            selectedNode = null;
            isDraggingCanvas = true;
            GUI.changed = true;
        }

        private void OnLeftUp(Event e, Rect canvasRect)
        {
            if (connectFromNode == null) return;

            var mp = e.mousePosition - new Vector2(0, TOOLBAR_H);

            if (graph != null)
            {
                foreach (var n in graph.nodes)
                {
                    if (n == connectFromNode) continue;
                    if (Vector2.Distance(mp, GetInputPortPos(n)) < PORT_R + 3)
                    {
                        if (connectFromChoiceIdx >= 0)
                            connectFromNode.Choices[connectFromChoiceIdx].TargetNodeId = n.nodeId;
                        else
                            connectFromNode.NextNodeId = n.nodeId;

                        EditorUtility.SetDirty(graph);
                        break;
                    }
                }
            }

            connectFromNode = null;
            e.Use();
        }

        private void OnRightDown(Event e, Rect canvasRect)
        {
            if (graph == null)
            {
                ShowCanvasMenu(e.mousePosition);
                e.Use();
                return;
            }

            var mp = e.mousePosition - new Vector2(0, TOOLBAR_H);
            foreach (var n in graph.nodes)
            {
                if (GetNodeRect(n).Contains(mp))
                {
                    ShowNodeMenu(n);
                    e.Use();
                    return;
                }
            }

            ShowCanvasMenu(e.mousePosition);
            e.Use();
        }

        private void ShowNodeMenu(DialogueNode node)
        {
            var m = new GenericMenu();
            m.AddItem(new GUIContent("Start 노드로 지정"), false, () =>
            {
                graph.startNodeId = node.nodeId;
                EditorUtility.SetDirty(graph);
            });
            m.AddSeparator("");
            m.AddItem(new GUIContent("연결 모두 해제"), false, () =>
            {
                node.NextNodeId = null;
                node.Choices?.ForEach(c => c.TargetNodeId = null);
                EditorUtility.SetDirty(graph);
            });
            m.AddSeparator("");
            m.AddItem(new GUIContent("노드 삭제"), false, () =>
            {
                if (EditorUtility.DisplayDialog("노드 삭제", $"'{node.NodeTitle}' 노드를 삭제할까요?", "삭제", "취소"))
                {
                    graph.DeleteNode(node.nodeId);
                    if (selectedNode == node) selectedNode = null;
                }
            });
            m.ShowAsContext();
        }

        private void ShowCanvasMenu(Vector2 mousePos)
        {
            if (graph == null) return;
            var worldPos = mousePos - new Vector2(0, TOOLBAR_H) - scrollOffset;
            var m = new GenericMenu();
            m.AddItem(new GUIContent("추가/💬 NPC 노드"), false, () => AddNodeAt(DialogueNodeType.NPC, worldPos));
            m.AddItem(new GUIContent("추가/🗣 Player 노드"), false, () => AddNodeAt(DialogueNodeType.Player, worldPos));
            m.AddItem(new GUIContent("추가/📖 Narration 노드"), false, () => AddNodeAt(DialogueNodeType.Narration, worldPos));
            m.AddItem(new GUIContent("추가/▶ Start 노드"), false, () => AddNodeAt(DialogueNodeType.Start, worldPos));
            m.AddItem(new GUIContent("추가/■ Chapter End 노드"), false, () => AddNodeAt(DialogueNodeType.ChapterEnd, worldPos));
            m.ShowAsContext();
        }

        private void AddNode(DialogueNodeType type)
        {
            if (graph == null) return;
            var pos = new Vector2(200 - scrollOffset.x, 150 - scrollOffset.y);
            selectedNode = graph.CreateNode(type, pos);
        }

        private void AddNodeAt(DialogueNodeType type, Vector2 worldPos)
        {
            if (graph == null) return;
            selectedNode = graph.CreateNode(type, worldPos);
        }

        private void DeleteSelectedNode()
        {
            if (selectedNode == null) return;
            if (EditorUtility.DisplayDialog("노드 삭제", $"'{selectedNode.NodeTitle}'을 삭제할까요?", "삭제", "취소"))
            {
                graph.DeleteNode(selectedNode.nodeId);
                selectedNode = null;
            }
        }

        private void PickAndLoadGraph()
        {
            string path = EditorUtility.OpenFilePanel("DialogueGraph 선택", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path.Substring(Application.dataPath.Length);
            var g = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);
            if (g != null) LoadGraph(g);
        }

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject("새 Dialogue Graph 생성",
                "NewDialogueGraph", "asset", "저장 위치 선택");
            if (string.IsNullOrEmpty(path)) return;

            var g = CreateInstance<DialogueGraph>();
            g.graphId = Guid.NewGuid().ToString();
            g.graphTitle = System.IO.Path.GetFileNameWithoutExtension(path);

            var start = new DialogueNode
            {
                nodeId = Guid.NewGuid().ToString(),
                nodeType = DialogueNodeType.Start,
                editorPosition = new Vector2(80, 200),
            };
            g.nodes.Add(start);
            g.startNodeId = start.nodeId;

            AssetDatabase.CreateAsset(g, path);
            AssetDatabase.SaveAssets();

            LoadGraph(g);
            EditorGUIUtility.PingObject(g);
        }

        private void SaveGraph()
        {
            if (graph == null) return;
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VNFramework] '{graph.name}' 저장 완료");
        }

        private void DrawCenteredLabel(Rect rect, string text)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                richText = true,
            };
            GUI.Label(new Rect(0, 0, rect.width, rect.height), text, s);
        }
    }
}
#endif
