using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;

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

        [CanBeNull]
        public DialogueNode GetNode(string id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.nodeId == id) return node;
            }
            return null;
        }

        public DialogueNode GetStartNode() => GetNode(startNodeId);

        public void GetPath(DialogueNode fromNode, List<DialogueNode> outPath, Dictionary<string, int> selectedChoices)
        {
            var startNode = GetStartNode();
            outPath.Clear();
            selectedChoices.Clear();
            using var _ = HashSetPool<DialogueNode>.Get(out var visitedNodes);
            FindPath(startNode, fromNode, outPath, selectedChoices, visitedNodes);
        }

        private bool FindPath(DialogueNode current, DialogueNode targetNode, 
            List<DialogueNode> outPath, Dictionary<string, int> selectedChoices, 
            HashSet<DialogueNode> visitedNodes)
        {
            if (current == null || visitedNodes.Contains(current)) return false;

            outPath.Add(current);
            visitedNodes.Add(current);

            if (current == targetNode) return true;

            switch (current.NodeType)
            {
                case DialogueNodeType.Condition:
                {
                    // Condition은 선택지 인덱스 개념 없이 True/False 탐색
                    foreach (var nextId in new[] { current.TrueNodeId, current.FalseNodeId })
                    {
                        if (FindPath(GetNode(nextId), targetNode, outPath, selectedChoices, visitedNodes))
                            return true;
                    }
                    break;
                }
                default:
                {
                    if (current.Choices.IsValid())
                    {
                        // 선택지 하나씩 시도, 성공한 인덱스만 기록
                        for (int i = 0; i < current.Choices.Count; i++)
                        {
                            var nextNode = GetNode(current.Choices[i].TargetNodeId);
                            selectedChoices[current.nodeId] = i; // 시도할 인덱스 먼저 기록

                            if (FindPath(nextNode, targetNode, outPath, selectedChoices, visitedNodes))
                                return true; // 이 선택지로 찾았으면 그대로 반환
                        }
                        // 모든 선택지 실패 시 제거
                        selectedChoices.Remove(current.nodeId);
                    }
                    else
                    {
                        if (FindPath(GetNode(current.NextNodeId), targetNode, outPath, selectedChoices, visitedNodes))
                            return true;
                    }
                    break;
                }
            }

            // 백트래킹
            outPath.RemoveLast();
            visitedNodes.Remove(current); // visited도 백트래킹 필요
            return false;
        }

        // ── 에디터 전용 유틸 ─────────────────────────────────────────────

#if UNITY_EDITOR
        public DialogueNode CreateNode(DialogueNodeType type, Vector2 position)
        {
            var node = new DialogueNode
            {
                nodeId          = Guid.NewGuid().ToString(),
                NodeType        = type,
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
                if (n.NextNodeId  == id) n.NextNodeId  = null;
                if (n.TrueNodeId  == id) n.TrueNodeId  = null;
                if (n.FalseNodeId == id) n.FalseNodeId = null;
                foreach (var c in n.Choices)
                    if (c.TargetNodeId == id) c.TargetNodeId = null;
            }

            if (startNodeId == id) startNodeId = "";

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
