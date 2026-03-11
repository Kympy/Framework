using System;
using UnityEditor;
using UnityEngine;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        // ── 그래프 로드 ───────────────────────────────────────────────

        private void LoadGraph(DialogueGraph g)
        {
            _graph = g;
            SelectNode(null);
            _graphSO = g != null ? new SerializedObject(g) : null;
            Repaint();
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
            if (_graph == null) return;
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VNFramework] '{_graph.name}' 저장 완료");
        }
    }
}
