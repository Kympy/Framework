using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 하나의 대화 씬(또는 챕터)을 표현하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVisualNovelDialogueGraph",
                     menuName  = "Visual Novel/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        public string graphId    = "";
        public string graphTitle = "";

        public List<DialogueNode> nodes       = new List<DialogueNode>();
        public string             startNodeId = "";

        // ── 런타임 조회 ──────────────────────────────────────────────────

        public DialogueNode GetNode(string id)
            => nodes.Find(n => n.nodeId == id);

        public DialogueNode GetStartNode()
            => GetNode(startNodeId);

        // ── 에디터 전용 유틸 ─────────────────────────────────────────────

#if UNITY_EDITOR
        public DialogueNode CreateNode(DialogueNodeType type, Vector2 position)
        {
            var node = new DialogueNode
            {
                nodeId          = Guid.NewGuid().ToString(),
                nodeType        = type,
                editorPosition  = position,
            };

            nodes.Add(node);

            // 최초 Start 노드는 자동으로 startNodeId에 등록
            if (type == DialogueNodeType.Start && string.IsNullOrEmpty(startNodeId))
                startNodeId = node.nodeId;

            UnityEditor.EditorUtility.SetDirty(this);
            return node;
        }

        public void DeleteNode(string id)
        {
            nodes.RemoveAll(n => n.nodeId == id);

            // 끊어진 참조 정리
            foreach (var n in nodes)
            {
                if (n.NextNodeId == id) n.NextNodeId = null;
                foreach (var c in n.Choices)
                    if (c.TargetNodeId == id) c.TargetNodeId = null;
            }

            if (startNodeId == id) startNodeId = "";

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
