using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        private const float NODE_W = 210f;
        private const float HEADER_H = 28f;
        private const float PREVIEW_HEIGHT = 22f;
        private const float CHOICE_ROW_HEIGHT = 20f;
        private const float PORT_R = 7f;
        
        private void AddNode(DialogueNodeType type)
        {
            if (_graph == null) return;
            var pos = new Vector2(200 - scrollOffset.x, 150 - scrollOffset.y);
            var createdNode = CreateNode(type, pos);
            SelectNode(createdNode);
            CreateLocalizationEntries(createdNode);
        }

        private void AddNodeAt(DialogueNodeType type, Vector2 worldPos)
        {
            if (_graph == null) return;
            var createdNode = CreateNode(type, worldPos);
            SelectNode(createdNode);
            CreateLocalizationEntries(createdNode);
        }

        private DialogueNode CreateNode(DialogueNodeType type, Vector2 pos)
        {
            var createdNode = _graph.CreateNode(type, pos);
            var settings = GetOrCreatePreviewSettings();
            createdNode.TextSpeed = settings.DefaultTextSpeed;
            createdNode.TextColor = settings.DefaultTextColor;
            createdNode.TextSize = settings.DefaultTextSize;
            return createdNode;
        }

        private void PasteNode(DialogueNode source)
        {
            if (_graph == null || source == null) return;

            // 깊은 복사
            var newNode = new DialogueNode
            {
                nodeId = Guid.NewGuid().ToString(),
                NodeType = source.NodeType,
                SpeakerName = source.SpeakerName,
                DialogueText = null,
                TextSpeed = source.TextSpeed,
                NextChapter = source.NextChapter,
                TrueNodeId = null, // 연결은 복사하지 않음
                FalseNodeId = null,
                NextNodeId = null,

                // 오프셋 적용해서 위치 겹침 방지
                editorPosition = source.editorPosition + new Vector2(30, 30),

                // 조건 복사
                Conditions = source.Conditions?.ConvertAll(condition => new DialogueCondition()
                {
                    ConditionType = condition.ConditionType,
                    CheckType = condition.CheckType,
                    ParamType = condition.ParamType,
                    BoolValue = condition.BoolValue,
                    IntValue = condition.IntValue,
                    FloatValue = condition.FloatValue,
                    StringValue = condition.StringValue,
                }) ?? new List<DialogueCondition>(),

                // 이벤트 깊은 복사
                EnterEvents = source.EnterEvents?.ConvertAll(e => e.Clone()) ?? new List<DialogueEvent>(),
                ExitEvents = source.ExitEvents?.ConvertAll(e => e.Clone()) ?? new List<DialogueEvent>(),
            };
            // 선택지 깊은 복사 (연결은 초기화)
            CloneChoice(newNode, source);

            _graph.nodes.Add(newNode);
            SelectNode(newNode);

            // 로컬라이제이션 엔트리 새로 생성 (GUID가 달라졌으므로)
            CreateLocalizationEntries(newNode);

            EditorUtility.SetDirty(_graph);
            Repaint();
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

            if (DialogueRunner.HasInstance)
            {
                var playingNode = DialogueRunner.Instance.CurrentNodeId == node.nodeId;
                if (playingNode)
                {
                    EditorGUI.DrawRect(new Rect(rect.x - 5, rect.y - 20, rect.width + 10, rect.height + 20), Color.Lerp(baseColor, Color.orangeRed, 0.35f));
                    GUI.Label(new Rect(rect.x - 5, rect.y - 20, rect.width + 10, 20), "⬇️ CURRENT", _nodeHeaderStyle);
                }
            }

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
                speakerName = node.SpeakerName == null || node.SpeakerName.IsEmpty ? "(내레이션)" : node.SpeakerName.GetLocalizedString();
            }
            else if (node.NodeType == DialogueNodeType.Character)
            {
                speakerName = node.SpeakerName == null || node.SpeakerName.IsEmpty ? "(캐릭터)" : node.SpeakerName.GetLocalizedString();
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
                var trueNode = _graph.GetNode(from.TrueNodeId);
                if (trueNode != null)
                    DrawBezier(GetOutputPortPos(from, -1), GetInputPortPos(trueNode),
                        new Color(0.4f, 0.95f, 0.45f, 0.8f));
                var falseNode = _graph.GetNode(from.FalseNodeId);
                if (falseNode != null)
                    DrawBezier(GetOutputPortPos(from, -2), GetInputPortPos(falseNode),
                        new Color(0.95f, 0.45f, 0.45f, 0.8f));
                return;
            }

            if (from.Choices != null)
            {
                for (int i = 0; i < from.Choices.Count; i++)
                {
                    var t = _graph.GetNode(from.Choices[i].TargetNodeId);
                    if (t != null)
                        DrawBezier(GetOutputPortPos(from, i), GetInputPortPos(t),
                            new Color(1f, 0.85f, 0.25f, 0.8f));
                }
            }

            var next = _graph.GetNode(from.NextNodeId);
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
    }
}
