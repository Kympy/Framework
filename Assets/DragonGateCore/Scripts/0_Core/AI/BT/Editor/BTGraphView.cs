using System.Collections.Generic;
using System.Linq;
using DragonGate;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// BT 에디터의 그래프 캔버스.
/// 노드 배치/연결, 에셋 저장/로드를 담당한다.
/// </summary>
public class BTGraphView : GraphView
{
    public BTGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // 기본 스타일시트 (없으면 무시)
        var uss = UnityEngine.Resources.Load<StyleSheet>("BTGraphView");
        if (uss != null) styleSheets.Add(uss);
    }

    /// <summary>
    /// 연결 가능한 포트 목록. 방향이 반대이고 같은 노드가 아니면 연결 가능.
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports
            .Where(p => p != startPort
                     && p.node != startPort.node
                     && p.direction != startPort.direction)
            .ToList();
    }

    /// <summary>
    /// 우클릭 컨텍스트 메뉴: 등록된 BTNode 타입 목록을 보여준다.
    /// </summary>
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var localPos = contentViewContainer.WorldToLocal(evt.mousePosition);

        foreach (var entry in BTNodeTypeRegistry.GetAllEntries())
        {
            var menuPath = string.IsNullOrEmpty(entry.Category)
                ? $"노드 추가/{entry.DisplayName}"
                : $"노드 추가/{entry.Category}/{entry.DisplayName}";

            // 클로저 캡처를 위한 로컬 복사
            var captured = entry;
            evt.menu.AppendAction(menuPath, _ =>
                CreateNodeView(captured.Category, captured.TypeName, captured.DisplayName, localPos));
        }

        base.BuildContextualMenu(evt);
    }

    public BTNodeView CreateNodeView(string categoryName, string typeName, string displayName, Vector2 position)
    {
        var nodeView = new BTNodeView(categoryName, typeName, displayName, position);
        RegisterRootToggle(nodeView);
        AddElement(nodeView);
        return nodeView;
    }

    private void RegisterRootToggle(BTNodeView nodeView)
    {
        nodeView.OnRootSelected += selected =>
        {
            foreach (var nv in nodes.ToList().OfType<BTNodeView>())
            {
                if (nv != selected)
                    nv.SetAsRoot(false);
            }
        };
    }

    // ─── 저장 / 로드 ─────────────────────────────────────────────────

    public void LoadFromAsset(BTGraphAsset asset)
    {
        ClearGraph();
        if (asset == null) return;

        var nodeViewMap = new Dictionary<string, BTNodeView>();

        // 노드 뷰 생성
        foreach (var nodeData in asset.nodes)
        {
            var displayName = BTNodeTypeRegistry.GetDisplayName(nodeData.typeName);
            var nodeView = new BTNodeView(nodeData.IsRoot, nodeData.guid, nodeData.CategoryName, nodeData.typeName, displayName, nodeData.position, nodeData.paramJson);
            nodeView.NodeKey = nodeData.nodeKey;
            nodeView.SetAsRoot(nodeData.guid == asset.rootGuid);
            RegisterRootToggle(nodeView);

            AddElement(nodeView);
            nodeView.RefreshExpandedState();
            nodeView.RefreshPorts();
            nodeViewMap[nodeData.guid] = nodeView;
        }

        // 엣지 연결
        foreach (var nodeData in asset.nodes)
        {
            if (!nodeViewMap.TryGetValue(nodeData.guid, out var parentView)) continue;
            if (parentView.OutputPort == null) continue;

            foreach (var childGuid in nodeData.childrenGuids)
            {
                if (!nodeViewMap.TryGetValue(childGuid, out var childView)) continue;

                var edge = parentView.OutputPort.ConnectTo(childView.InputPort);
                AddElement(edge);
            }
        }
    }

    public bool SaveToAsset(BTGraphAsset asset)
    {
        var nodeViews = nodes.ToList().OfType<BTNodeView>().ToList();
        // 루트가 최소 1개 있어야 함을 보장하려고..
        var rootNode = nodeViews.FirstOrDefault(n => n.IsRoot);
        if (rootNode == null)
        {
            DGDebug.LogError("Root 노드가 존재하지 않습니다.");
            return false;
        }
        asset.rootGuid = rootNode.Guid;
        // 에셋 노드 클리어하고 추가
        asset.nodes.Clear();

        // 부모→자식 맵: edges 전체 쿼리 대신 각 노드의 OutputPort.connections 에서 직접 읽는다.
        // edge.output.node 역추적은 GraphView 버전에 따라 불안정하므로 사용하지 않는다.
        var childrenMap = new Dictionary<string, List<string>>();
        foreach (var nv in nodeViews)
            childrenMap[nv.Guid] = new List<string>();

        var sortBuffer = new List<BTNodeView>();
        foreach (var nodeView in nodeViews)
        {
            if (nodeView.OutputPort == null) continue;
            sortBuffer.Clear();
            foreach (var edge in nodeView.OutputPort.connections)
            {
                if (edge.input?.node is BTNodeView childView)
                    sortBuffer.Add(childView);
            }
            // 그래프 X 좌표 기준 정렬: 왼쪽 노드 = 우선순위 높음(인덱스 0)
            sortBuffer.Sort((a, b) => a.GetPosition().x.CompareTo(b.GetPosition().x));
            foreach (var childView in sortBuffer)
            {
                childrenMap[nodeView.Guid].Add(childView.Guid);
            }
        }

        foreach (var nodeView in nodeViews)
        {
            asset.nodes.Add(new BTNodeData
            {
                IsRoot = nodeView.IsRoot,
                guid = nodeView.Guid,
                nodeKey = nodeView.NodeKey ?? "",
                CategoryName = nodeView.CategoryName ?? "",
                typeName = nodeView.TypeName,
                position = nodeView.GetPosition().position,
                childrenGuids = childrenMap[nodeView.Guid],
                paramJson = nodeView.GetParamJson() ?? ""
            });
        }

        return true;
    }

    private void ClearGraph()
    {
        edges.ToList().ForEach(RemoveElement);
        nodes.ToList().ForEach(RemoveElement);
    }
}