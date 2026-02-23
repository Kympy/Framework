using System;
using System.Linq;
using System.Reflection;
using DragonGate;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// BT 에디터에서 노드 하나를 표현하는 뷰.
/// public / [SerializeField] 필드를 자동으로 편집 UI로 만든다.
/// </summary>
public class BTNodeView : Node
{
    public string Guid { get; private set; }
    public string CategoryName { get; private set; }
    public string TypeName { get; private set; }
    public string NodeKey { get; set; }
    public bool IsRoot { get; set; }

    public Port InputPort { get; private set; }
    public Port OutputPort { get; private set; }

    /// <summary>
    /// IsRoot 토글이 true로 설정될 때 발생. BTGraphView에서 다른 노드의 Root를 해제하는 데 사용.
    /// </summary>
    public event Action<BTNodeView> OnRootSelected;

    private Toggle _rootToggle;
    private string _paramJson;

    // 파라미터 편집용 임시 인스턴스 (실제 런타임 인스턴스가 아님)
    private object _nodeInstance;
    private Label _categoryLabel;

    private static readonly Color RootColor      = new(0.2f, 0.6f, 0.2f);
    private static readonly Color CompositeColor = new(0.2f, 0.4f, 0.7f);
    private static readonly Color DecoratorColor = new(0.6f, 0.4f, 0.1f);
    private static readonly Color LeafColor      = new(0.35f, 0.35f, 0.35f);

    // 새 노드 생성용
    public BTNodeView(string categoryName, string typeName, string displayName, Vector2 position)
        : this(false, System.Guid.NewGuid().ToString(), categoryName, typeName, displayName, position, null) { }

    // 에셋에서 로드된 노드 복원용 (paramJson을 생성자에서 받아 UI가 올바른 값으로 시작)
    public BTNodeView(bool isRoot, string guid, string categoryName, string typeName, string displayName, Vector2 position, string paramJson)
    {
        IsRoot = isRoot;
        Guid = guid;
        CategoryName = categoryName;
        TypeName = typeName;
        title = displayName;
        // titleContainer를 세로 레이아웃 + 자동 높이로 변경
        titleContainer.style.flexDirection = FlexDirection.Column;
        titleContainer.style.alignItems = Align.FlexStart;
        titleContainer.style.height = StyleKeyword.Auto;
        titleContainer.style.minHeight = 0;
        titleContainer.style.flexGrow = 0;

        // 전체 노드도 세로 흐름 보장
        mainContainer.style.flexDirection = FlexDirection.Column;
        extensionContainer.style.flexGrow = 1;
        CreateCategoryLabel();
        CreateRootToggle();
        _paramJson = paramJson;

        SetPosition(new Rect(position, new Vector2(200, 150)));
        style.height = StyleKeyword.Auto;
        style.minHeight = 120;

        var category = GetNodeCategory(typeName);
        ApplyStyle(category);
        CreatePorts(category);
        BuildParamFields();

        RefreshExpandedState();
        RefreshPorts();
    }

    private void CreateRootToggle()
    {
        _rootToggle = new Toggle("Root");
        _rootToggle.value = IsRoot;
        _rootToggle.style.marginLeft = 6;
        _rootToggle.style.marginTop = 2;

        _rootToggle.RegisterValueChangedCallback(evt =>
        {
            IsRoot = evt.newValue;
            SetAsRoot(IsRoot);
            if (IsRoot) OnRootSelected?.Invoke(this);
        });

        titleContainer.Add(_rootToggle);
    }

    private void CreateCategoryLabel()
    {
        _categoryLabel = new Label(CategoryName);
        _categoryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _categoryLabel.style.fontSize = 10;
        _categoryLabel.style.marginLeft = 4;
        _categoryLabel.style.marginRight = 4;
        _categoryLabel.style.marginTop = 2;
        _categoryLabel.style.marginBottom = 2;

        titleContainer.Add(_categoryLabel);
    }

    // ─── 카테고리 / 스타일 ───────────────────────────────────────────────

    private BTNodeCategory GetNodeCategory(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type == null) return BTNodeCategory.Leaf;
        if (typeof(CompositeNode).IsAssignableFrom(type)) return BTNodeCategory.Composite;
        if (typeof(DecoratorNode).IsAssignableFrom(type)) return BTNodeCategory.Decorator;
        return BTNodeCategory.Leaf;
    }

    private void ApplyStyle(BTNodeCategory category)
    {
        if (IsRoot)
        {
            titleContainer.style.backgroundColor = new StyleColor(RootColor);
            return;
        }
        titleContainer.style.backgroundColor = new StyleColor(category switch
        {
            BTNodeCategory.Composite => CompositeColor,
            BTNodeCategory.Decorator => DecoratorColor,
            _                        => LeafColor
        });
    }

    public void SetAsRoot(bool isRoot)
    {
        IsRoot = isRoot;
        _rootToggle?.SetValueWithoutNotify(isRoot);
        var category = GetNodeCategory(TypeName);
        ApplyStyle(category);
    }

    // ─── 포트 ────────────────────────────────────────────────────────────

    private void CreatePorts(BTNodeCategory category)
    {
        InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(BTNode));
        InputPort.portName = "";
        inputContainer.Add(InputPort);

        switch (category)
        {
            case BTNodeCategory.Composite:
                OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(BTNode));
                OutputPort.portName = "";
                outputContainer.Add(OutputPort);
                break;
            case BTNodeCategory.Decorator:
                OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(BTNode));
                OutputPort.portName = "";
                outputContainer.Add(OutputPort);
                break;
            // Leaf: 출력 포트 없음
        }
    }

    // ─── 파라미터 필드 UI ────────────────────────────────────────────────

    private void BuildParamFields()
    {
        var type = Type.GetType(TypeName);
        if (type == null) return;

        var fields = GetSerializableFields(type);

        // 임시 인스턴스 생성
        try { _nodeInstance = Activator.CreateInstance(type); }
        catch { return; }

        // 저장된 paramJson이 있으면 복원
        if (!string.IsNullOrEmpty(_paramJson))
        {
            try { JsonUtility.FromJsonOverwrite(_paramJson, _nodeInstance); }
            catch { /* 무시 */ }
        }

        // 인스턴스 현재 값을 기준으로 paramJson 동기화
        _paramJson = JsonUtility.ToJson(_nodeInstance);

        foreach (var field in fields)
        {
            var fieldUI = CreateFieldUI(field);
            if (fieldUI != null)
                extensionContainer.Add(fieldUI);
        }

        expanded = true;
    }

    private static FieldInfo[] GetSerializableFields(Type type)
    {
        return type
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f =>
                (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                && f.DeclaringType != typeof(BTNode))
            .ToArray();
    }

    private VisualElement CreateFieldUI(FieldInfo field)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginTop = 2;
        row.style.marginBottom = 2;
        row.style.paddingLeft = 6;
        row.style.paddingRight = 6;

        var label = new Label(ObjectNames.NicifyVariableName(field.Name));
        label.style.minWidth = 70;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        row.Add(label);

        var currentVal = _nodeInstance != null ? field.GetValue(_nodeInstance) : null;

        VisualElement inputEl = field.FieldType switch
        {
            var t when t == typeof(float)  => MakeField(new FloatField(),   (FloatField f)   => { f.value = currentVal is float  v ? v : 0f;  f.RegisterValueChangedCallback(e => SetField(field, e.newValue)); }),
            var t when t == typeof(int)    => MakeField(new IntegerField(),  (IntegerField f) => { f.value = currentVal is int    v ? v : 0;   f.RegisterValueChangedCallback(e => SetField(field, e.newValue)); }),
            var t when t == typeof(string) => MakeField(new TextField(),    (TextField f)    => { f.value = currentVal as string ?? "";       f.RegisterValueChangedCallback(e => SetField(field, e.newValue)); }),
            var t when t == typeof(bool)   => MakeField(new Toggle(),       (Toggle f)       => { f.value = currentVal is bool   v && v;      f.RegisterValueChangedCallback(e => SetField(field, e.newValue)); }),
            _ => null
        };

        if (inputEl == null) return null;

        inputEl.style.flexGrow = 1;
        row.Add(inputEl);
        return row;
    }

    // 필드 설정 + paramJson 갱신
    private void SetField(FieldInfo field, object value)
    {
        field.SetValue(_nodeInstance, value);
        _paramJson = JsonUtility.ToJson(_nodeInstance);
    }

    // 타입 안전한 필드 초기화 헬퍼
    private static VisualElement MakeField<T>(T field, Action<T> setup) where T : VisualElement
    {
        setup(field);
        return field;
    }

    // ─── paramJson 접근 ──────────────────────────────────────────────────

    public string GetParamJson() => _paramJson;
}

public enum BTNodeCategory
{
    Root,
    Composite,
    Decorator,
    Leaf
}