#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DragonGate.Editor
{
    /// <summary>
    /// Project 창에서 DialogueGraph 에셋을 선택했을 때
    /// Inspector에 "에디터 열기" 버튼을 표시한다.
    /// </summary>
    [CustomEditor(typeof(DialogueGraph))]
    public class DialogueGraphInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = (DialogueGraph)target;

            // ── 열기 버튼 ────────────────────────────────────────
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
            if (GUILayout.Button("📝  Graph Editor 열기", GUILayout.Height(36)))
                DialogueGraphEditorWindow.OpenGraph(graph);
            GUI.backgroundColor = Color.white;

            GUILayout.Space(8);

            // ── 요약 정보 ────────────────────────────────────────
            EditorGUILayout.LabelField("Graph ID",    graph.graphId,    EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Start Node",  graph.startNodeId ?? "없음", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("노드 수",     graph.nodes?.Count.ToString() ?? "0", EditorStyles.miniLabel);

            GUILayout.Space(6);

            // ── 기본 인스펙터 (접기 가능) ─────────────────────────
            DrawDefaultInspector();
        }
    }
}
#endif
