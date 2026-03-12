using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace DragonGate
{
    /// <summary>
    /// 하나의 대화 씬(또는 챕터)을 표현하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVisualNovelDialogueGraph",
        menuName = "Visual Novel/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        [FormerlySerializedAs("graphId")]
        public string GraphId = "";
        [FormerlySerializedAs("graphTitle")]
        public string GraphTitle = "";

        [FormerlySerializedAs("nodes")]
        public List<DialogueNode> Nodes = new ();
        [FormerlySerializedAs("startNodeId")]
        public string StartNodeId = "";

        // ── 런타임 조회 ──────────────────────────────────────────────────

        [CanBeNull]
        public DialogueNode GetNode(string id)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node.nodeId == id) return node;
            }

            return null;
        }

        public DialogueNode GetStartNode() => GetNode(StartNodeId);

        public void GetPath(DialogueNode targetNode, List<DialogueNode> outPath, Dictionary<string, int> outSelectedChoices)
        {
            outPath.Clear();
            outSelectedChoices.Clear();
            using var _ = HashSetPool<DialogueNode>.Get(out var visited);
            FindPaths(GetStartNode(), targetNode, outPath, outSelectedChoices, visited, findAll: false, null, null);
        }

        public void GetAllPaths(DialogueNode targetNode, List<List<DialogueNode>> outPaths, List<Dictionary<string, int>> outSelectedChoices)
        {
            outPaths.Clear();
            outSelectedChoices.Clear();
            using var _1 = HashSetPool<DialogueNode>.Get(out var visited);
            using var _2 = ListPool<DialogueNode>.Get(out var currentPath);
            var currentChoices = new Dictionary<string, int>();
            FindPaths(GetStartNode(), targetNode, currentPath, currentChoices, visited, findAll: true, outPaths, outSelectedChoices);
        }

        private bool FindPaths(DialogueNode current, DialogueNode targetNode,
            List<DialogueNode> currentPath, Dictionary<string, int> currentChoices,
            HashSet<DialogueNode> visited, bool findAll,
            List<List<DialogueNode>> outPaths, List<Dictionary<string, int>> outSelectedChoices)
        {
            if (current == null || visited.Contains(current)) return false;

            currentPath.Add(current);
            visited.Add(current);

            if (current == targetNode)
            {
                if (findAll)
                {
                    // 스냅샷 저장 후 계속 탐색
                    outPaths.Add(new List<DialogueNode>(currentPath));
                    outSelectedChoices.Add(new Dictionary<string, int>(currentChoices));
                }
                else
                {
                    // 단일 경로 - 찾았으므로 즉시 반환 (currentPath는 그대로 유지)
                    return true;
                }
            }
            else
            {
                switch (current.NodeType)
                {
                    case DialogueNodeType.Condition:
                        foreach (var nextId in new[] { current.TrueNodeId, current.FalseNodeId })
                        {
                            if (FindPaths(GetNode(nextId), targetNode, currentPath, currentChoices,
                                    visited, findAll, outPaths, outSelectedChoices))
                                if (!findAll)
                                    return true;
                        }

                        break;

                    default:
                        if (current.Choices.IsValid())
                        {
                            for (int i = 0; i < current.Choices.Count; i++)
                            {
                                currentChoices[current.nodeId] = i;
                                if (FindPaths(GetNode(current.Choices[i].TargetNodeId), targetNode,
                                        currentPath, currentChoices, visited, findAll, outPaths, outSelectedChoices))
                                    if (!findAll)
                                        return true;
                            }

                            currentChoices.Remove(current.nodeId);
                        }
                        else
                        {
                            if (FindPaths(GetNode(current.NextNodeId), targetNode, currentPath,
                                    currentChoices, visited, findAll, outPaths, outSelectedChoices))
                                if (!findAll)
                                    return true;
                        }

                        break;
                }
            }

            // 백트래킹 (단일 경로에서 찾은 경우는 여기 안 옴)
            currentPath.RemoveLast();
            visited.Remove(current);
            return false;
        }

        // ── 에디터 전용 유틸 ─────────────────────────────────────────────

#if UNITY_EDITOR
        public DialogueNode CreateNode(DialogueNodeType type, Vector2 position)
        {
            var node = new DialogueNode
            {
                nodeId = Guid.NewGuid().ToString(),
                NodeType = type,
                editorPosition = position,
            };

            Nodes.Add(node);

            // 최초 Start 노드는 자동으로 startNodeId에 등록
            if (type == DialogueNodeType.Start && string.IsNullOrEmpty(StartNodeId))
                StartNodeId = node.nodeId;

            UnityEditor.EditorUtility.SetDirty(this);
            return node;
        }

        public void DeleteNode(string id)
        {
            Nodes.RemoveAll(n => n.nodeId == id);

            // 끊어진 참조 정리
            foreach (var n in Nodes)
            {
                if (n.NextNodeId == id) n.NextNodeId = null;
                if (n.TrueNodeId == id) n.TrueNodeId = null;
                if (n.FalseNodeId == id) n.FalseNodeId = null;
                foreach (var c in n.Choices)
                    if (c.TargetNodeId == id)
                        c.TargetNodeId = null;
            }

            if (StartNodeId == id) StartNodeId = "";

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
