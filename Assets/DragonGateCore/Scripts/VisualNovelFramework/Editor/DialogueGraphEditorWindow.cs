// ============================================================
//  Visual Novel Framework – Dialogue Graph Editor Window
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using DragonGate;
using Unity.VisualScripting;

namespace DragonGate.Editor
{
    [InitializeOnLoad]
    public class DialogueGraphEditorWindow : EditorWindow
    {
        // ── 레이아웃 상수 ─────────────────────────────────────────────
        private const float NODE_W = 210f;
        private const float HEADER_H = 28f;
        private const float PREVIEW_HEIGHT = 22f;
        private const float CHOICE_ROW_HEIGHT = 20f;
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
                { DialogueNodeType.Character, new Color(0.28f, 0.42f, 0.30f) },
                { DialogueNodeType.Narration, new Color(0.38f, 0.28f, 0.48f) },
                { DialogueNodeType.ChapterEnd, new Color(0.50f, 0.18f, 0.18f) },
                { DialogueNodeType.Condition, new Color(0.45f, 0.35f, 0.12f) },
            };

        private static readonly Dictionary<DialogueNodeType, string> NODE_ICONS =
            new Dictionary<DialogueNodeType, string>
            {
                { DialogueNodeType.Start, "▶ START" },
                { DialogueNodeType.Character, "🗣 CHARACTER" },
                { DialogueNodeType.Narration, "📖 NARRATION" },
                { DialogueNodeType.ChapterEnd, "■ CHAPTER END" },
                { DialogueNodeType.Condition, "? CONDITION" },
            };

        // ── 상태 ──────────────────────────────────────────────────────
        private DialogueGraph graph;
        private string _selectedNodeId;
        private int _selectedLocaleIdx;
        private string[] _localeNames;
        private DialogueNode SelectedNode => graph?.nodes.Find(n => n.nodeId == _selectedNodeId);
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
        
        // 줌
        private float _zoom = 1f;
        private Matrix4x4 _prevMatrix;
        private const float ZOOM_MIN = 0.6f;
        private const float ZOOM_MAX = 1.3f;
        private const float ZOOM_STEP = 0.1f;

        // 접기 상태
        private bool foldEnterEvents = true;
        private bool foldExitEvents = false;

        // 설정 패널
        private bool _showSettings;
        private SerializedObject _previewSettingsSO;
        private const string PREVIEW_SETTINGS_RESOURCE_PATH = "Assets/DragonGateCore/VisualNovelFramework/Resources/DialoguePreviewSettings.asset";

        // ── 캐시된 GUIContent (static: 재생성 불필요) ────────────────
        private static readonly GUIContent s_conditionTypeLabel = new GUIContent("조건 타입");
        private static readonly GUIContent s_checkTypeLabel = new GUIContent("연산 타입");
        private static readonly GUIContent s_paramTypeLabel = new GUIContent("값 타입");
        private static readonly GUIContent s_valueLabel = new GUIContent("값");
        private static readonly GUIContent s_speakerNameLabel = new GUIContent("화자 이름 (내레이션)");
        private static readonly GUIContent s_characterAssetLabel = new GUIContent("캐릭터 에셋");
        private static readonly GUIContent s_dialogueTextLabel = new GUIContent("대화 텍스트");
        private static readonly GUIContent s_dialogueTextSpeedLabel = new GUIContent("텍스트 속도");
        private static readonly GUIContent s_nextChapterLabel = new GUIContent("다음 챕터");
        private static readonly GUIContent s_choiceTextLabel = new GUIContent("텍스트");
        private static readonly GUIContent s_isEnabledLabel = new GUIContent("활성화");
        private static readonly GUIContent s_bgSpriteLabel = new GUIContent("배경 스프라이트");
        private static readonly GUIContent s_positionLabel = new GUIContent("위치");
        private static readonly GUIContent s_characterLabel = new GUIContent("캐릭터");
        private static readonly GUIContent s_characterIdLabel = new GUIContent("캐릭터 ID");
        private static readonly GUIContent s_characterScaleLabel = new GUIContent("크기 배율");
        private static readonly GUIContent s_emotionSpriteLabel = new GUIContent("감정 스프라이트");
        private static readonly GUIContent s_objNameLabel = new GUIContent("오브젝트 이름");
        private static readonly GUIContent s_animTriggerLabel = new GUIContent("애니메이션 트리거");
        private static readonly GUIContent s_effectPrefabLabel = new GUIContent("이펙트 Prefab");
        private static readonly GUIContent s_uiObjLabel = new GUIContent("UI 오브젝트 이름");
        private static readonly GUIContent s_bgmLabel = new GUIContent("BGM");
        private static readonly GUIContent s_bgmVolumeLabel = new GUIContent("BGM Volume");
        private static readonly GUIContent s_bgmFadeLabel = new GUIContent("BGM Fade Duration");
        private static readonly GUIContent s_sfxLabel = new GUIContent("SFX");
        private static readonly GUIContent s_sfxVolumeLabel = new GUIContent("SFX Volume");
        private static readonly GUIContent s_durationLabel = new GUIContent("시간(초)");
        private static readonly GUIContent s_waitForCompLabel = new GUIContent("완료 대기");

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
        
        // 작업 씬
        private const string WORK_SCENE_PATH = "Assets/Scenes/DialoguePreview.unity";

        // ── 메뉴 진입점 ───────────────────────────────────────────────

        [MenuItem("DragonGate/Open Visual Novel Dialogue Graph Editor")]
        public static DialogueGraphEditorWindow OpenWindow()
        {
            var w = GetWindow<DialogueGraphEditorWindow>("Visual Novel Graph Editor");
            w.minSize = new Vector2(900, 600);
            return w;
        }

        public static void OpenGraph(DialogueGraph graph)
        {
            var w = OpenWindow();
            w.LoadGraph(graph);
        }

        private static void OpenDialoguePreviewScene(DialogueGraph graph)
        {
            if (File.Exists(WORK_SCENE_PATH))
            {
                if (EditorSceneManager.GetActiveScene().path != WORK_SCENE_PATH)
                    EditorSceneManager.OpenScene(WORK_SCENE_PATH);
                    
                EnsureStarter();
            }
            else
            {
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                EnsureStarter();
                
                string sceneDir = Path.GetDirectoryName(WORK_SCENE_PATH);
                if (!Directory.Exists(sceneDir))
                    Directory.CreateDirectory(sceneDir);
                EditorSceneManager.SaveScene(newScene, WORK_SCENE_PATH);
            }

            void EnsureStarter()
            {
                if (FindAnyObjectByType<DialoguePreviewStarter>() == null)
                    new GameObject("Starter").AddComponent<DialoguePreviewStarter>();
            }
            
            EditorApplication.isPlaying = true;
        }

        private static void StartRunner(DialogueGraph graph, DialogueNode node)
        {
            if (DialogueRunner.HasInstance == false) return;
            DialogueRunner.Instance.StartDialogue(graph, node.nodeId);
        }

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

        private void OnEnable()
        {
            connectFromNode = null; // 유령 커넥션 방지
            _stylesReady = false;

            if (graph != null)
                _graphSO = new SerializedObject(graph);

            // 로컬라이제이션 시스템 초기화 먼저 완료
            // var initOp = LocalizationSettings.InitializationOperation;
            // if (!initOp.IsDone)
            //     initOp.WaitForCompletion();
            RefreshLocales();
        }

        // ── 그래프 로드 ───────────────────────────────────────────────

        private void LoadGraph(DialogueGraph g)
        {
            graph = g;
            _selectedNodeId = null;
            _graphSO = g != null ? new SerializedObject(g) : null;
            Repaint();
        }
        
        // 언어
        private void RefreshLocales()
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                var initOperation = LocalizationSettings.InitializationOperation;
                if (initOperation.IsDone == false) initOperation.WaitForCompletion();
            }
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null || locales.Count == 0)
            {
                _localeNames = new[] { "No Locales" };
                _selectedLocaleIdx = -1;
                return;
            }

            _localeNames = locales.Select(l => l.LocaleName).ToArray();

            // 현재 선택된 Locale에 맞게 인덱스 동기화
            var current = LocalizationSettings.SelectedLocale;
            if (current != null)
            {
                _selectedLocaleIdx = locales.IndexOf(current);
            }
            else
            {
                _selectedLocaleIdx = -1;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  OnGUI
        // ══════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            EnsureStyles();

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
                    OpenDialoguePreviewScene(graph);
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

            if (graph == null)
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

            foreach (var n in graph.nodes) DrawNodeConnections(n);

            if (connectFromNode != null)
            {
                var from = GetOutputPortPos(connectFromNode, connectFromChoiceIdx);
                // var rawEnd = Event.current.mousePosition - new Vector2(0, TOOLBAR_H);
                // var mousePosInCanvas = (Event.current.mousePosition - rect.position) / _zoom;
                var mousePosInCanvas = ScreenToCanvas(Event.current.mousePosition, rect);

                // 가장 가까운 Input 포트에 스냅 → 시각적 끝점 정렬
                var snapEnd = mousePosInCanvas;
                foreach (var n in graph.nodes)
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

            foreach (var n in graph.nodes) DrawNode(n);
            
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

        // ── 노드 그리기 ───────────────────────────────────────────────

        private void DrawNode(DialogueNode node)
        {
            var rect = GetNodeRect(node);

            EditorGUI.DrawRect(new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height),
                new Color(0, 0, 0, 0.45f));

            var baseColor = NODE_COLORS.TryGetValue(node.NodeType, out var nc) ? nc : Color.gray;
            if (_selectedNodeId == node.nodeId)
                baseColor = Color.Lerp(baseColor, new Color(0.5f, 0.8f, 1f), 0.35f);
            EditorGUI.DrawRect(rect, baseColor);

            DrawOutline(rect, _selectedNodeId == node.nodeId ? BORDER_SELECTED : BORDER_DEFAULT, 1.5f);

            var headerRect = new Rect(rect.x, rect.y, rect.width, HEADER_H);
            EditorGUI.DrawRect(headerRect, new Color(0, 0, 0, 0.25f));

            GUI.Label(headerRect,
                NODE_ICONS.TryGetValue(node.NodeType, out var icon) ? icon : node.NodeType.ToString(),
                _nodeHeaderStyle);

            float y = rect.y + HEADER_H;

            string speakerName = null;
            if (node.NodeType == DialogueNodeType.Narration)
            {
                speakerName = node.NarrationSpeakerName == null || node.NarrationSpeakerName.IsEmpty ? "(내레이션)" : node.NarrationSpeakerName.GetLocalizedString();
            }
            else if (node.NodeType == DialogueNodeType.Character)
            {
                speakerName = node.SpeakerCharacter == null || node.SpeakerCharacter.GetName() == null ? "(캐릭터)" : node.SpeakerCharacter.GetName();
            }

            string preview;
            if (node.DialogueText == null || node.DialogueText.IsEmpty)
            {
                preview = "<텍스트 없음>";
            }
            else
            {
                var localized = node.DialogueText.GetLocalizedString();
                preview = localized.Length > 24 ? localized.Substring(0, 24) + "…" : localized;
            }

            if (string.IsNullOrEmpty(speakerName) == false)
            {
                GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, PREVIEW_HEIGHT),
                    $"<b>{speakerName}</b>  {preview}", _nodePreviewRichStyle);
                y += PREVIEW_HEIGHT;
            }
            else if (node.NodeType == DialogueNodeType.ChapterEnd)
            {
                string nextChapter = node.NextChapter == null || node.NextChapter.RuntimeKeyIsValid() == false ? "(챕터 미지정)" : $"→ {AssetManager.GetAssetNameFromGUID(node.NextChapter.AssetGUID)}";
                GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, PREVIEW_HEIGHT), nextChapter, _nodePreviewStyle);
                y += PREVIEW_HEIGHT;
            }

            DrawPort(GetInputPortPos(node), portInputColor: true);

            if (node.NodeType == DialogueNodeType.Condition)
            {
                for (int i = 0; i < node.Conditions.Count; i++)
                {
                    var condition = node.Conditions[i];
                    GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, CHOICE_ROW_HEIGHT),
                        $"{condition.ConditionType} : {condition.CheckType}", _nodePreviewStyle);
                    y += CHOICE_ROW_HEIGHT;
                }

                GUI.Label(new Rect(rect.x + 8, y, rect.width - 24, CHOICE_ROW_HEIGHT), "▸ True", _nodeTrueStyle);
                DrawPort(GetOutputPortPos(node, -1), portInputColor: false);
                y += CHOICE_ROW_HEIGHT;

                GUI.Label(new Rect(rect.x + 8, y, rect.width - 24, CHOICE_ROW_HEIGHT), "▸ False", _nodeFalseStyle);
                DrawPort(GetOutputPortPos(node, -2), portInputColor: false);
            }
            else
            {
                if (node.Choices != null)
                {
                    for (int i = 0; i < node.Choices.Count; i++)
                    {
                        var ch = node.Choices[i];
                        string ct;
                        if (ch.ChoiceText == null || ch.ChoiceText.IsEmpty)
                        {
                            ct = $"Choice {i + 1}";
                        }
                        else
                        {
                            var localized = ch.ChoiceText.GetLocalizedString();
                            ct = localized.Length > 20 ? localized.Substring(0, 20) + "…" : localized;
                        }

                        GUI.Label(new Rect(rect.x + 8, y, rect.width - 24, CHOICE_ROW_HEIGHT), $"▸ {ct}", _nodeChoiceStyle);
                        DrawPort(GetOutputPortPos(node, i), portInputColor: false);
                        y += CHOICE_ROW_HEIGHT;
                    }
                }

                if (node.NodeType != DialogueNodeType.ChapterEnd)
                {
                    GUI.Label(new Rect(rect.x + 8, y, rect.width - 24, CHOICE_ROW_HEIGHT), "▸ Next", _nodeNextStyle);
                    DrawPort(GetOutputPortPos(node, -1), portInputColor: false);
                }
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
            if (from.NodeType == DialogueNodeType.Condition)
            {
                var trueNode = graph.GetNode(from.TrueNodeId);
                if (trueNode != null)
                    DrawBezier(GetOutputPortPos(from, -1), GetInputPortPos(trueNode),
                        new Color(0.4f, 0.95f, 0.45f, 0.8f));
                var falseNode = graph.GetNode(from.FalseNodeId);
                if (falseNode != null)
                    DrawBezier(GetOutputPortPos(from, -2), GetInputPortPos(falseNode),
                        new Color(0.95f, 0.45f, 0.45f, 0.8f));
                return;
            }

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
            if (n.NodeType == DialogueNodeType.Condition)
            {
                // 각 Condition 행 + True 행 + False 행
                h += (n.Conditions.Count + 2) * CHOICE_ROW_HEIGHT;
            }
            else
            {
                if (n.NodeType != DialogueNodeType.Start)
                    h += PREVIEW_HEIGHT;
                if (n.Choices != null)
                    h += n.Choices.Count * CHOICE_ROW_HEIGHT;
                if (n.NodeType != DialogueNodeType.ChapterEnd)
                    h += CHOICE_ROW_HEIGHT;
            }

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

            if (n.NodeType == DialogueNodeType.Condition)
            {
                // 조건 행 이후: True(-1)는 첫 번째, False(-2)는 두 번째
                float baseY = r.y + HEADER_H + n.Conditions.Count * CHOICE_ROW_HEIGHT;
                float rowOffset = (choiceIdx == -2) ? CHOICE_ROW_HEIGHT : 0f;
                return new Vector2(r.xMax, baseY + rowOffset + CHOICE_ROW_HEIGHT * 0.5f);
            }

            float y = r.y + HEADER_H;
            if (n.NodeType != DialogueNodeType.Start) y += PREVIEW_HEIGHT;

            if (choiceIdx >= 0 && n.Choices != null)
                y += choiceIdx * CHOICE_ROW_HEIGHT + CHOICE_ROW_HEIGHT * 0.5f;
            else
                y += (n.Choices?.Count ?? 0) * CHOICE_ROW_HEIGHT + CHOICE_ROW_HEIGHT * 0.5f;

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

            if (_showSettings)
                DrawPreviewSettingsPanel();
            else if (graph == null)
                DrawNoGraphInspector();
            else if (string.IsNullOrEmpty(_selectedNodeId))
                DrawGraphInspector();
            else
                DrawNodeInspector(SelectedNode);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPreviewSettingsPanel()
        {
            GUILayout.Label("Preview Settings", _inspectorTitle14);
            GUILayout.Space(8);

            var settings = GetOrCreatePreviewSettings();
            if (_previewSettingsSO == null || _previewSettingsSO.targetObject != settings)
                _previewSettingsSO = new SerializedObject(settings);

            _previewSettingsSO.Update();
            EditorGUILayout.PropertyField(_previewSettingsSO.FindProperty("DialogueRunnerPrefab"), new GUIContent("DialogueRunner 프리팹"));
            _previewSettingsSO.ApplyModifiedProperties();
        }

        private static DialoguePreviewSettings GetOrCreatePreviewSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DialoguePreviewSettings>(PREVIEW_SETTINGS_RESOURCE_PATH);
            if (settings != null) return settings;

            settings = CreateInstance<DialoguePreviewSettings>();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PREVIEW_SETTINGS_RESOURCE_PATH)!);
            AssetDatabase.CreateAsset(settings, PREVIEW_SETTINGS_RESOURCE_PATH);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private void DrawNoGraphInspector()
        {
            GUILayout.Label("Visual Novel Graph", _inspectorTitle14);
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
            GUILayout.Label("📊  Graph Settings", _inspectorTitle12);
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
            if (_graphSO == null || node == null) return;
            var so = _graphSO;
            int nodeIdx = graph.nodes.IndexOf(node);
            if (nodeIdx < 0) return;
            string nodePath = $"nodes.Array.data[{nodeIdx}]";
            so.Update();

            EditorGUI.BeginChangeCheck();

            var icon = NODE_ICONS.TryGetValue(node.NodeType, out var ic) ? ic : node.NodeType.ToString();
            GUILayout.Label(icon, _inspectorTitle13);
            EditorGUILayout.LabelField("Node ID", node.nodeId, EditorStyles.miniLabel);
            GUILayout.Space(6);

            node.NodeType = (DialogueNodeType)EditorGUILayout.EnumPopup("타입", node.NodeType);
            GUILayout.Space(6);

            if (node.NodeType == DialogueNodeType.Condition)
            {
                GUILayout.Label("조건 설정", EditorStyles.boldLabel);

                var conditionListProp = so.FindProperty($"{nodePath}.Conditions");
                for (int i = 0; i < conditionListProp.arraySize; i++)
                {
                    var conditionProp = conditionListProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("ConditionType"), s_conditionTypeLabel);
                    GUILayout.Space(6);

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"조건 {i + 1}", EditorStyles.boldLabel);
                    // 조건 제거 버튼
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        conditionListProp.DeleteArrayElementAtIndex(i);
                        so.ApplyModifiedProperties();
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        break;
                    }

                    GUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("CheckType"), s_checkTypeLabel);

                    var paramTypeProp = conditionProp.FindPropertyRelative("ParamType");
                    EditorGUILayout.PropertyField(paramTypeProp, s_paramTypeLabel);
                    switch (paramTypeProp.intValue)
                    {
                        case (int)ConditionParamType.Int:
                            EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("IntValue"), s_valueLabel);
                            break;
                        case (int)ConditionParamType.Float:
                            EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("FloatValue"), s_valueLabel);
                            break;
                        case (int)ConditionParamType.Bool:
                            EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("BoolValue"), s_valueLabel);
                            break;
                        case (int)ConditionParamType.String:
                            EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("StringValue"), s_valueLabel);
                            break;
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }

                if (GUILayout.Button("+ 조건 추가"))
                {
                    conditionListProp.InsertArrayElementAtIndex(conditionListProp.arraySize);
                    // 새 요소 초기화
                    var newElem = conditionListProp.GetArrayElementAtIndex(conditionListProp.arraySize - 1);
                    // newElem.FindPropertyRelative("IsEnabled").boolValue = true;
                    // newElem.FindPropertyRelative("TargetNodeId").stringValue = string.Empty;
                    so.ApplyModifiedProperties();
                }

                GUILayout.Label("True 분기", EditorStyles.boldLabel);
                var trueProp = so.FindProperty($"{nodePath}.TrueNodeId");
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("True →",
                    string.IsNullOrEmpty(trueProp.stringValue) ? "없음 (포트로 연결)" : trueProp.stringValue,
                    EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(trueProp.stringValue) && GUILayout.Button("해제", GUILayout.Width(38)))
                {
                    trueProp.stringValue = string.Empty;
                    so.ApplyModifiedProperties();
                }

                GUILayout.EndHorizontal();

                GUILayout.Label("False 분기", EditorStyles.boldLabel);
                var falseProp = so.FindProperty($"{nodePath}.FalseNodeId");
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("False →",
                    string.IsNullOrEmpty(falseProp.stringValue) ? "없음 (포트로 연결)" : falseProp.stringValue,
                    EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(falseProp.stringValue) && GUILayout.Button("해제", GUILayout.Width(38)))
                {
                    falseProp.stringValue = string.Empty;
                    so.ApplyModifiedProperties();
                }

                GUILayout.EndHorizontal();
            }

            if (node.NodeType != DialogueNodeType.Start &&
                node.NodeType != DialogueNodeType.ChapterEnd &&
                node.NodeType != DialogueNodeType.Condition)
            {
                GUILayout.Label("대화 내용", EditorStyles.boldLabel);

                // Narration: 화자 없이 대사만 나올 때를 위한 이름 (선택 사항)
                if (node.NodeType == DialogueNodeType.Narration)
                {
                    EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.NarrationSpeakerName"), s_speakerNameLabel);
                    GUILayout.Space(6);
                }
                else if (node.NodeType == DialogueNodeType.Character)
                {
                    EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.SpeakerCharacter"), s_characterAssetLabel);
                    GUILayout.Space(6);
                }
                // 대화 텍스트
                var dialogueTextProp = so.FindProperty($"{nodePath}.DialogueText");
                EditorGUILayout.PropertyField(dialogueTextProp, s_dialogueTextLabel);
                // 대화 속도
                EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.TextSpeed"), s_dialogueTextSpeedLabel);
            }

            if (node.NodeType == DialogueNodeType.ChapterEnd)
            {
                GUILayout.Label("챕터 전환", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.NextChapter"), s_nextChapterLabel);
            }

            GUILayout.Space(8);

            if (node.NodeType != DialogueNodeType.Start &&
                node.NodeType != DialogueNodeType.ChapterEnd &&
                node.NodeType != DialogueNodeType.Condition)
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

                    EditorGUILayout.PropertyField(choiceDataProp.FindPropertyRelative("ChoiceText"), s_choiceTextLabel);
                    EditorGUILayout.PropertyField(choiceDataProp.FindPropertyRelative("IsEnabled"), s_isEnabledLabel);

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
                var eventType = (DialogueEventType)typeProp.intValue;

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
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), s_bgSpriteLabel);
                        break;

                    case DialogueEventType.ShowCharacter:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterViewportPosition"), s_positionLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterScale"), s_characterScaleLabel);
                        break;

                    case DialogueEventType.HideCharacter:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), s_characterLabel);
                        break;

                    case DialogueEventType.PlayAnimation:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), s_characterLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("AnimationTrigger"), s_animTriggerLabel);
                        break;

                    case DialogueEventType.PlayEffect:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), s_effectPrefabLabel);
                        break;

                    case DialogueEventType.ShowUI:
                    case DialogueEventType.HideUI:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("UiElementId"), s_uiObjLabel);
                        break;

                    case DialogueEventType.PlayBGM:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), s_bgmLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Volume"), s_bgmVolumeLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_bgmFadeLabel);
                        break;

                    case DialogueEventType.PlaySFX:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Asset"), s_sfxLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Volume"), s_sfxVolumeLabel);
                        break;

                    case DialogueEventType.FadeIn:
                    case DialogueEventType.FadeOut:
                    case DialogueEventType.Wait:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        break;
                }

                EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("WaitForCompletion"), s_waitForCompLabel);

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

                    if (isDraggingNode && !string.IsNullOrEmpty(_selectedNodeId))
                    {
                        SelectedNode.editorPosition += delta / _zoom;
                        EditorUtility.SetDirty(graph);
                        e.Use();
                    }
                    else if (isDraggingCanvas || e.button == 2)
                    {
                        scrollOffset += delta / _zoom;
                        e.Use();
                    }
                    else if (connectFromNode != null)
                    {
                        e.Use();
                    }

                    break;

                case EventType.KeyDown when e.keyCode == KeyCode.Delete && !string.IsNullOrEmpty(_selectedNodeId):
                    DeleteSelectedNode();
                    e.Use();
                    break;
                    
                case EventType.ScrollWheel when inCanvas:
                    float zoomDelta = -e.delta.y * ZOOM_STEP;
                    float prevZoom = _zoom;
                    _zoom = Mathf.Clamp(_zoom + zoomDelta, ZOOM_MIN, ZOOM_MAX);

                    // 마우스 위치 기준으로 줌 (마우스 포인터 고정)
                    var mouseInCanvas = e.mousePosition - canvasRect.position;
                    scrollOffset += mouseInCanvas * (1f / _zoom - 1f / prevZoom);

                    e.Use();
                    break;
            }
        }
        
        private Vector2 ScreenToCanvas(Vector2 screenPos, Rect canvasRect)
        {
            // var pivot = new Vector2(canvasRect.width * 0.5f, canvasRect.height * 0.5f);
            // return (screenPos - canvasRect.position - pivot) / _zoom + pivot;
            // pivot을 (0,0) 기준으로 변경
            // return (screenPos - canvasRect.position) / _zoom;
            return new Vector2((screenPos.x - canvasRect.x) / _zoom,  (screenPos.y - canvasRect.y) / _zoom);
        }

        private void OnLeftDown(Event e, Rect canvasRect)
        {
            if (graph == null) return;

            // var mousePosition = e.mousePosition - new Vector2(0, TOOLBAR_H);
            var mousePosition = ScreenToCanvas(e.mousePosition, canvasRect);

            foreach (var n in graph.nodes)
            {
                if (n.NodeType == DialogueNodeType.Condition)
                {
                    if (Vector2.Distance(mousePosition, GetOutputPortPos(n, -1)) < PORT_R + 2)
                    {
                        connectFromNode = n;
                        connectFromChoiceIdx = -1;
                        e.Use();
                        return;
                    }

                    if (Vector2.Distance(mousePosition, GetOutputPortPos(n, -2)) < PORT_R + 2)
                    {
                        connectFromNode = n;
                        connectFromChoiceIdx = -2;
                        e.Use();
                        return;
                    }

                    continue;
                }

                if (n.Choices != null)
                    for (int i = 0; i < n.Choices.Count; i++)
                        if (Vector2.Distance(mousePosition, GetOutputPortPos(n, i)) < PORT_R + 2)
                        {
                            connectFromNode = n;
                            connectFromChoiceIdx = i;
                            e.Use();
                            return;
                        }

                if (n.NodeType != DialogueNodeType.ChapterEnd &&
                    Vector2.Distance(mousePosition, GetOutputPortPos(n, -1)) < PORT_R + 2)
                {
                    connectFromNode = n;
                    connectFromChoiceIdx = -1;
                    e.Use();
                    return;
                }
            }

            for (int nodeIndex = graph.nodes.Count - 1; nodeIndex >= 0; nodeIndex--)
            {
                var node = graph.nodes[nodeIndex];
                if (GetNodeRect(node).Contains(mousePosition))
                {
                    _selectedNodeId = node.nodeId;
                    OnClickNode(node);
                    isDraggingNode = true;
                    GUI.changed = true;
                    e.Use();
                    return;
                }
            }

            _selectedNodeId = null;
            isDraggingCanvas = true;
            GUI.changed = true;
        }

        private void OnLeftUp(Event e, Rect canvasRect)
        {
            if (connectFromNode == null) return;

            // var mp = e.mousePosition - new Vector2(0, TOOLBAR_H);
            var mousePosition = ScreenToCanvas(e.mousePosition, canvasRect);

            if (graph != null)
            {
                foreach (var n in graph.nodes)
                {
                    if (n == connectFromNode) continue;
                    if (Vector2.Distance(mousePosition, GetInputPortPos(n)) < PORT_R + 3)
                    {
                        if (connectFromChoiceIdx >= 0)
                            connectFromNode.Choices[connectFromChoiceIdx].TargetNodeId = n.nodeId;
                        else if (connectFromChoiceIdx == -2)
                            connectFromNode.FalseNodeId = n.nodeId;
                        else if (connectFromNode.NodeType == DialogueNodeType.Condition)
                            connectFromNode.TrueNodeId = n.nodeId;
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
                ShowCanvasMenu(e.mousePosition, canvasRect);
                e.Use();
                return;
            }

            // var mp = e.mousePosition - new Vector2(0, TOOLBAR_H);
            var mousePosition = ScreenToCanvas(e.mousePosition, canvasRect);
            foreach (var n in graph.nodes)
            {
                if (GetNodeRect(n).Contains(mousePosition))
                {
                    ShowNodeMenu(n);
                    e.Use();
                    return;
                }
            }

            ShowCanvasMenu(e.mousePosition, canvasRect);
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
                    if (_selectedNodeId == node.nodeId) _selectedNodeId = null;
                }
            });
            m.ShowAsContext();
        }

        private void ShowCanvasMenu(Vector2 mousePos, Rect canvasRect)
        {
            if (graph == null) return;
            var worldPos = ScreenToCanvas(mousePos, canvasRect) - scrollOffset;
            var m = new GenericMenu();
            m.AddItem(new GUIContent("추가/🗣 Character 노드"), false, () => AddNodeAt(DialogueNodeType.Character, worldPos));
            m.AddItem(new GUIContent("추가/📖 Narration 노드"), false, () => AddNodeAt(DialogueNodeType.Narration, worldPos));
            m.AddItem(new GUIContent("추가/? Condition 노드"), false, () => AddNodeAt(DialogueNodeType.Condition, worldPos));
            m.AddItem(new GUIContent("추가/▶ Start 노드"), false, () => AddNodeAt(DialogueNodeType.Start, worldPos));
            m.AddItem(new GUIContent("추가/■ Chapter End 노드"), false, () => AddNodeAt(DialogueNodeType.ChapterEnd, worldPos));
            m.ShowAsContext();
        }

        private void AddNode(DialogueNodeType type)
        {
            if (graph == null) return;
            var pos = new Vector2(200 - scrollOffset.x, 150 - scrollOffset.y);
            _selectedNodeId = graph.CreateNode(type, pos)?.nodeId;
        }

        private void AddNodeAt(DialogueNodeType type, Vector2 worldPos)
        {
            if (graph == null) return;
            _selectedNodeId = graph.CreateNode(type, worldPos)?.nodeId;
        }

        private void OnClickNode(DialogueNode node)
        {
            _showSettings = false;
            if (EditorApplication.isPlaying)
                StartRunner(graph, node);
        }

        private void DeleteSelectedNode()
        {
            var node = SelectedNode;
            if (node == null) return;
            if (EditorUtility.DisplayDialog("노드 삭제", $"'{node.NodeTitle}'을 삭제할까요?", "삭제", "취소"))
            {
                graph.DeleteNode(node.nodeId);
                _selectedNodeId = null;
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
                NodeType = DialogueNodeType.Start,
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
            GUI.Label(new Rect(0, 0, rect.width, rect.height), text, _centeredLabelStyle);
        }
    }
}
#endif
