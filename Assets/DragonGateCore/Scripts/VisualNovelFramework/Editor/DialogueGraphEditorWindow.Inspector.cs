using UnityEditor;
using UnityEngine;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        private const float SPLITTER_W = 5f;
        private const float INSPECTOR_MIN_W = 200f;
        private const float INSPECTOR_MAX_W = 700f;
        
        // 인스펙터 스크롤
        private Vector2 inspectorScroll;
        // 인스펙터 패널 크기
        private float _inspectorWidth = 310f;
        private bool _isDraggingSplitter;
        // 설정 패널 전환
        private bool _showSettings;
        
        // ── 캐시된 GUIContent (static: 재생성 불필요) ────────────────
        private static readonly GUIContent s_conditionTypeLabel = new GUIContent("조건 타입");
        private static readonly GUIContent s_checkTypeLabel = new GUIContent("연산 타입");
        private static readonly GUIContent s_paramTypeLabel = new GUIContent("값 타입");
        private static readonly GUIContent s_valueLabel = new GUIContent("값");
        private static readonly GUIContent s_speakerNameLabel = new GUIContent("화자 이름");
        private static readonly GUIContent s_dialogueTextLabel = new GUIContent("대화 텍스트");
        private static readonly GUIContent s_dialogueTextSpeedLabel = new GUIContent("텍스트 속도");
        private static readonly GUIContent s_dialogueTextSizeLabel = new GUIContent("텍스트 크기");
        private static readonly GUIContent s_dialogueTextColorLabel = new GUIContent("텍스트 색상");
        private static readonly GUIContent s_nextChapterLabel = new GUIContent("다음 챕터");
        private static readonly GUIContent s_choiceTextLabel = new GUIContent("텍스트");
        private static readonly GUIContent s_isEnabledLabel = new GUIContent("활성화");
        
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
                DrawGraphSettingsPanel();
            else if (_graph == null)
                DrawNoGraphInspector();
            else if (string.IsNullOrEmpty(_selectedNodeId))
                DrawGraphInspector();
            else
                DrawNodeInspector(SelectedNode);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGraphSettingsPanel()
        {
            GUILayout.Label("Graph Settings", _inspectorTitle14);
            GUILayout.Space(8);

            var settings = GetOrCreatePreviewSettings();
            if (_graphSettingsSO == null || _graphSettingsSO.targetObject != settings)
                _graphSettingsSO = new SerializedObject(settings);

            _graphSettingsSO.Update();
            EditorGUILayout.PropertyField(_graphSettingsSO.FindProperty("DialogueRunnerPrefab"), new GUIContent("DialogueRunner 프리팹"));
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_graphSettingsSO.FindProperty("DefaultTextSpeed"), new GUIContent("기본 텍스트 속도"));
            EditorGUILayout.PropertyField(_graphSettingsSO.FindProperty("DefaultTextSize"), new GUIContent("기본 텍스트 크기"));
            EditorGUILayout.PropertyField(_graphSettingsSO.FindProperty("DefaultTextColor"), new GUIContent("기본 텍스트 색상"));
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_graphSettingsSO.FindProperty("DefaultCharacterViewportPosition"), new GUIContent("기본 캐릭터 위치"));
            EditorGUILayout.PropertyField(_graphSettingsSO.FindProperty("DefaultCharacterScale"), new GUIContent("기본 캐릭터 스케일"));
            _graphSettingsSO.ApplyModifiedProperties();
        }

        private static DialoguePreviewSettings GetOrCreatePreviewSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DialoguePreviewSettings>(GRAPH_SETTINGS_RESOURCE_PATH);
            if (settings != null) return settings;

            settings = CreateInstance<DialoguePreviewSettings>();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GRAPH_SETTINGS_RESOURCE_PATH)!);
            AssetDatabase.CreateAsset(settings, GRAPH_SETTINGS_RESOURCE_PATH);
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
            _graph.GraphTitle = EditorGUILayout.TextField("제목", _graph.GraphTitle);
            _graph.GraphId = EditorGUILayout.TextField("Graph ID", _graph.GraphId);
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Start Node ID", string.IsNullOrEmpty(_graph.StartNodeId) ? "없음" : _graph.StartNodeId);
            EditorGUILayout.LabelField("노드 수", _graph.Nodes.Count.ToString());
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_graph);

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
            int nodeIdx = _graph.Nodes.IndexOf(node);
            if (nodeIdx < 0) return;
            string nodePath = $"Nodes.Array.data[{nodeIdx}]";
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
                if (node.NodeType == DialogueNodeType.Narration ||  node.NodeType == DialogueNodeType.Character)
                {
                    EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.SpeakerName"), s_speakerNameLabel);
                    GUILayout.Space(6);
                }
                // 대화 텍스트
                var dialogueTextProp = so.FindProperty($"{nodePath}.DialogueText");
                dialogueTextProp.isExpanded = true;
                EditorGUILayout.PropertyField(dialogueTextProp, s_dialogueTextLabel, true);
                // 대화 속도 크기 색상
                EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.TextSpeed"), s_dialogueTextSpeedLabel);
                EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.TextSize"), s_dialogueTextSizeLabel);
                EditorGUILayout.PropertyField(so.FindProperty($"{nodePath}.TextColor"), s_dialogueTextColorLabel);
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
                choiceProp.isExpanded = true;
                for (int i = 0; i < choiceProp.arraySize; i++)
                {
                    var choiceDataProp = choiceProp.GetArrayElementAtIndex(i);
                    choiceDataProp.isExpanded = true;
                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"선택지 {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        RemoveLocalizationKey(LOCALIZATION_CHOICE_TABLE, string.Format(LOCALIZATION_CHOICE_KEY_FORMAT, node.Choices[i].Id));
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
                    CreateChoice(node, choiceProp, so);
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
            DrawEventsSection(node, "진입 이벤트 (Enter)", so, $"{nodePath}.EnterEvents", ref foldEnterEvents);
            GUILayout.Space(4);
            DrawEventsSection(node, "퇴장 이벤트 (Exit)", so, $"{nodePath}.ExitEvents", ref foldExitEvents);

            // BeginChangeCheck 결과와 SO 변경 모두 반영
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_graph);

            so.ApplyModifiedProperties();
        }
    }
}
