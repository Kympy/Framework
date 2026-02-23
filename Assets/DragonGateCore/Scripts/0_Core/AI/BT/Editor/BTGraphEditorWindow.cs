using DragonGate;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// BT 비주얼 에디터 메인 윈도우.
/// DragonGate > BT Graph Editor 메뉴 또는 BTGraphAsset 더블클릭으로 열린다.
/// </summary>
public class BTGraphEditorWindow : EditorWindow
{
    private BTGraphView _graphView;
    private ObjectField _assetField;
    private TextField _targetClassField;
    private TextField _generatedPathField;

    [SerializeField] private BTGraphAsset _currentAsset;

    [MenuItem("DragonGate/BT Graph Editor")]
    public static void Open()
    {
        var window = GetWindow<BTGraphEditorWindow>("BT Graph Editor");
        window.minSize = new Vector2(800, 600);
    }

    public static void Open(BTGraphAsset asset)
    {
        var window = GetWindow<BTGraphEditorWindow>("BT Graph Editor");
        window.minSize = new Vector2(800, 600);
        window.LoadAsset(asset);
    }

    private void OnEnable()
    {
        BuildLayout();

        if (_currentAsset != null)
        {
            _assetField.SetValueWithoutNotify(_currentAsset);
            _graphView.graphViewChanged -= OnGraphViewChanged;
            _graphView.LoadFromAsset(_currentAsset);
            _graphView.graphViewChanged += OnGraphViewChanged;
            RefreshCodeGenFields();
            titleContent = new GUIContent($"BT Graph - {_currentAsset.name}");
        }
    }

    private void OnDisable()
    {
        if (_graphView != null)
            _graphView.graphViewChanged -= OnGraphViewChanged;
        EditorApplication.delayCall -= SaveGraphDelayed;
    }

    private void BuildLayout()
    {
        rootVisualElement.Clear();

        // ── 1행: 저장 / 에셋 선택 ───────────────────────────────────────
        var toolbar = new Toolbar();
        toolbar.Add(new ToolbarButton(SaveAsset) { text = "Save" });
        toolbar.Add(new ToolbarSpacer());
        toolbar.Add(new Label("Asset: "));

        _assetField = new ObjectField { objectType = typeof(BTGraphAsset), style = { minWidth = 200 } };
        _assetField.RegisterValueChangedCallback(e => LoadAsset(e.newValue as BTGraphAsset));
        toolbar.Add(_assetField);
        rootVisualElement.Add(toolbar);

        // ── 2행: 코드 생성 설정 ─────────────────────────────────────────
        var codeGenBar = new Toolbar();

        codeGenBar.Add(new Label("Class: ") { style = { unityTextAlign = TextAnchor.MiddleLeft } });
        _targetClassField = new TextField { style = { minWidth = 140 } };
        _targetClassField.RegisterValueChangedCallback(e =>
        {
            if (_currentAsset != null) { _currentAsset.targetClassName = e.newValue; EditorUtility.SetDirty(_currentAsset); }
        });
        codeGenBar.Add(_targetClassField);

        codeGenBar.Add(new Label("  Output: ") { style = { unityTextAlign = TextAnchor.MiddleLeft } });
        _generatedPathField = new TextField { style = { flexGrow = 1 } };
        _generatedPathField.RegisterValueChangedCallback(e =>
        {
            if (_currentAsset != null) { _currentAsset.generatedFilePath = e.newValue; EditorUtility.SetDirty(_currentAsset); }
        });
        codeGenBar.Add(_generatedPathField);

        codeGenBar.Add(new ToolbarButton(PickOutputPath) { text = "..." });
        rootVisualElement.Add(codeGenBar);

        // ── 그래프 뷰 ────────────────────────────────────────────────────
        _graphView = new BTGraphView { name = "BT Graph" };
        _graphView.style.flexGrow = 1;
        _graphView.graphViewChanged += OnGraphViewChanged;
        rootVisualElement.Add(_graphView);
    }

    // graphViewChanged 는 edgesToCreate / elementsToRemove 가 그래프에 반영되기 *전*에 호출된다.
    // delayCall 로 한 프레임 뒤에 저장해서 변경이 완전히 적용된 상태를 저장한다.
    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (_currentAsset != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_currentAsset)))
        {
            EditorApplication.delayCall -= SaveGraphDelayed;
            EditorApplication.delayCall += SaveGraphDelayed;
        }
        return change;
    }

    private void SaveGraphDelayed()
    {
        EditorApplication.delayCall -= SaveGraphDelayed;
        if (_currentAsset == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_currentAsset))) return;
        bool result = _graphView.SaveToAsset(_currentAsset);
        if (result == false) return;
        EditorUtility.SetDirty(_currentAsset);
        AssetDatabase.SaveAssets();
    }

    private void LoadAsset(BTGraphAsset asset)
    {
        if (asset == null) return;
        _currentAsset = asset;

        // 기본값 자동 채우기
        var assetPath = AssetDatabase.GetAssetPath(asset);
        var dirty = false;
        var replacedClassName = asset.targetClassName.IsNullOrEmpty() ? asset.name.Replace(" ", "") : asset.targetClassName.Replace(" ", "");
        asset.targetClassName = replacedClassName;
        if (string.IsNullOrEmpty(asset.generatedFilePath) && !string.IsNullOrEmpty(assetPath))
        {
            var folder = System.IO.Path.GetDirectoryName(assetPath);
            asset.generatedFilePath = $"{folder}/{replacedClassName}.BT.Generated.cs";
            dirty = true;
        }
        if (dirty) EditorUtility.SetDirty(asset);

        // 로드 중 graphViewChanged 콜백 차단: AddElement마다 중간 상태로 에셋 덮어쓰는 것 방지
        _graphView.graphViewChanged -= OnGraphViewChanged;
        _graphView.LoadFromAsset(asset);
        _graphView.graphViewChanged += OnGraphViewChanged;

        RefreshCodeGenFields();
        _assetField.SetValueWithoutNotify(asset);
        titleContent = new GUIContent($"BT Graph - {asset.name}");
    }

    private void RefreshCodeGenFields()
    {
        _targetClassField?.SetValueWithoutNotify(_currentAsset?.targetClassName ?? "");
        _generatedPathField?.SetValueWithoutNotify(_currentAsset?.generatedFilePath ?? "");
    }

    private void SaveAsset()
    {
        var result = SaveAssetInternal();
        if (result == false) return;
        GenerateCode();
    }

    // GenerateCode에서도 호출하는 순수 저장 로직 (GenerateCode 호출 없음)
    private bool SaveAssetInternal()
    {
        if (_currentAsset == null)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "BT Graph 저장", "New BT Graph", "asset", "저장 위치를 선택하세요");
            if (string.IsNullOrEmpty(path)) return false;

            _currentAsset = CreateInstance<BTGraphAsset>();
            AssetDatabase.CreateAsset(_currentAsset, path);
            _assetField.SetValueWithoutNotify(_currentAsset);
        }

        var result = _graphView.SaveToAsset(_currentAsset);
        if (result == false) return false;
        EditorUtility.SetDirty(_currentAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BTGraphEditor] 저장 완료: {AssetDatabase.GetAssetPath(_currentAsset)}");
        return true;
    }

    private void PickOutputPath()
    {
        var defaultName = string.IsNullOrEmpty(_currentAsset?.targetClassName)
            ? "Brain"
            : _currentAsset.targetClassName;

        var path = EditorUtility.SaveFilePanelInProject(
            "코드 생성 위치", $"{defaultName}.BT.Generated", "cs", "생성할 파일 위치를 선택하세요");
        if (string.IsNullOrEmpty(path)) return;

        if (_currentAsset != null) { _currentAsset.generatedFilePath = path; EditorUtility.SetDirty(_currentAsset); }
        _generatedPathField?.SetValueWithoutNotify(path);
    }

    private void GenerateCode()
    {
        if (_currentAsset == null) { Debug.LogError("[BTGraphEditor] 에셋을 먼저 선택하세요."); return; }
        BTCodeGenerator.Generate(_currentAsset);
    }
}