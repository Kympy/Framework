using UnityEditor;
using UnityEngine;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        // 드래그
        private bool isDraggingNode;
        private bool isDraggingCanvas;
        private Vector2 lastMousePos;
        
        // 연결 중
        private DialogueNode connectFromNode;
        private int connectFromChoiceIdx; // -1 = next port
        
        private void OnClickNode(DialogueNode node)
        {
            _showSettings = false;
            if (EditorApplication.isPlaying)
                StartRunner(_graph, node);
        }

        private void SelectNode(DialogueNode node)
        {
            // 텍스트 포커스 초기화
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;

            if (node == null) return;

            _selectedNodeId = node.nodeId;
            GUI.changed = true;
        }

        private void DeleteSelectedNode()
        {
            var node = SelectedNode;
            if (node == null) return;
            if (EditorUtility.DisplayDialog("노드 삭제", $"'{node.NodeTitle}'을 삭제할까요?", "삭제", "취소"))
            {
                RemoveLocalizationEntries(node);
                _graph.DeleteNode(node.nodeId);
                SelectNode(null);
            }
        }
        // ══════════════════════════════════════════════════════════════
        //  이벤트 처리
        // ══════════════════════════════════════════════════════════════

        private void HandleSplitter(Rect splitterRect)
        {
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(splitterRect, new Color(0f, 0f, 0f, 0.35f));

            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when splitterRect.Contains(e.mousePosition):
                    _isDraggingSplitter = true;
                    e.Use();
                    break;
                case EventType.MouseDrag when _isDraggingSplitter:
                    _inspectorWidth = Mathf.Clamp(position.width - e.mousePosition.x, INSPECTOR_MIN_W, INSPECTOR_MAX_W);
                    e.Use();
                    Repaint();
                    break;
                case EventType.MouseUp when _isDraggingSplitter:
                    _isDraggingSplitter = false;
                    e.Use();
                    break;
            }
        }

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
                        EditorUtility.SetDirty(_graph);
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

#if UNITY_STANDALONE_OSX
                case EventType.KeyDown when e.command && e.keyCode == KeyCode.S:
#endif
                case EventType.KeyDown when e.control && e.keyCode == KeyCode.S:
                    SaveGraph();
                    break;

#if UNITY_STANDALONE_OSX
                case EventType.KeyDown when e.keyCode == KeyCode.Backspace:
#endif
                case EventType.KeyDown when e.keyCode == KeyCode.Delete && !string.IsNullOrEmpty(_selectedNodeId):
                    DeleteSelectedNode();
                    e.Use();
                    break;

                case EventType.KeyDown when e.control || e.command:
                    switch (e.keyCode)
                    {
                        case KeyCode.C when !string.IsNullOrEmpty(_selectedNodeId):
                            _copiedNode = _graph.GetNode(_selectedNodeId);
                            e.Use();
                            break;

                        case KeyCode.V when _copiedNode != null:
                            PasteNode(_copiedNode);
                            e.Use();
                            break;

                        case KeyCode.D when !string.IsNullOrEmpty(_selectedNodeId): // Ctrl+D 복제
                            PasteNode(_graph.GetNode(_selectedNodeId));
                            e.Use();
                            break;
                    }

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
            return new Vector2((screenPos.x - canvasRect.x) / _zoom, (screenPos.y - canvasRect.y) / _zoom);
        }

        private void OnLeftDown(Event e, Rect canvasRect)
        {
            if (_graph == null) return;

            // var mousePosition = e.mousePosition - new Vector2(0, TOOLBAR_H);
            var mousePosition = ScreenToCanvas(e.mousePosition, canvasRect);

            foreach (var n in _graph.nodes)
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

            for (int nodeIndex = _graph.nodes.Count - 1; nodeIndex >= 0; nodeIndex--)
            {
                var node = _graph.nodes[nodeIndex];
                if (GetNodeRect(node).Contains(mousePosition))
                {
                    SelectNode(node);
                    OnClickNode(node);
                    isDraggingNode = true;
                    GUI.changed = true;
                    e.Use();
                    return;
                }
            }

            SelectNode(null);
            isDraggingCanvas = true;
            GUI.changed = true;
        }

        private void OnLeftUp(Event e, Rect canvasRect)
        {
            if (connectFromNode == null) return;

            // var mp = e.mousePosition - new Vector2(0, TOOLBAR_H);
            var mousePosition = ScreenToCanvas(e.mousePosition, canvasRect);

            if (_graph != null)
            {
                foreach (var n in _graph.nodes)
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

                        EditorUtility.SetDirty(_graph);
                        break;
                    }
                }
            }

            connectFromNode = null;
            e.Use();
        }

        private void OnRightDown(Event e, Rect canvasRect)
        {
            if (_graph == null)
            {
                ShowCanvasMenu(e.mousePosition, canvasRect);
                e.Use();
                return;
            }

            // var mp = e.mousePosition - new Vector2(0, TOOLBAR_H);
            var mousePosition = ScreenToCanvas(e.mousePosition, canvasRect);
            foreach (var n in _graph.nodes)
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
                _graph.startNodeId = node.nodeId;
                EditorUtility.SetDirty(_graph);
            });
            m.AddSeparator("");
            m.AddItem(new GUIContent("연결 모두 해제"), false, () =>
            {
                node.NextNodeId = null;
                node.Choices?.ForEach(c => c.TargetNodeId = null);
                EditorUtility.SetDirty(_graph);
            });
            m.AddSeparator("");
            m.AddItem(new GUIContent("노드 삭제"), false, () =>
            {
                if (EditorUtility.DisplayDialog("노드 삭제", $"'{node.NodeTitle}' 노드를 삭제할까요?", "삭제", "취소"))
                {
                    _graph.DeleteNode(node.nodeId);
                    if (_selectedNodeId == node.nodeId)
                    {
                        SelectNode(null);
                    }
                }
            });
            m.ShowAsContext();
        }

        private void ShowCanvasMenu(Vector2 mousePos, Rect canvasRect)
        {
            if (_graph == null) return;
            var worldPos = ScreenToCanvas(mousePos, canvasRect) - scrollOffset;
            var m = new GenericMenu();
            m.AddItem(new GUIContent("추가/🗣 Character 노드"), false, () => AddNodeAt(DialogueNodeType.Character, worldPos));
            m.AddItem(new GUIContent("추가/📖 Narration 노드"), false, () => AddNodeAt(DialogueNodeType.Narration, worldPos));
            m.AddItem(new GUIContent("추가/? Condition 노드"), false, () => AddNodeAt(DialogueNodeType.Condition, worldPos));
            m.AddItem(new GUIContent("추가/▶ Start 노드"), false, () => AddNodeAt(DialogueNodeType.Start, worldPos));
            m.AddItem(new GUIContent("추가/■ Chapter End 노드"), false, () => AddNodeAt(DialogueNodeType.ChapterEnd, worldPos));
            m.ShowAsContext();
        }
    }
}
