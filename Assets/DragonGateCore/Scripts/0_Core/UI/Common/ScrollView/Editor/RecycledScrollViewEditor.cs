#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    [CustomEditor(typeof(RecycledScrollView))]
    public class RecylcedScrollViewEditor : ScrollRectEditor
    {
        SerializedProperty _elementPrefab;
        SerializedProperty _gridConstraint;
        SerializedProperty _constraintCount;
        SerializedProperty _spacing;
        SerializedProperty _padding;
        SerializedProperty _childAlignment;
        SerializedProperty _prewarmCount;
        SerializedProperty _bufferElementCount;

        private readonly List<GameObject> _previewObjects = new();
        private int _previewItemCount = 10;

        protected override void OnEnable()
        {
            base.OnEnable();
            _elementPrefab      = serializedObject.FindProperty("_elementPrefab");
            _gridConstraint     = serializedObject.FindProperty("_gridConstraint");
            _constraintCount    = serializedObject.FindProperty("_constraintCount");
            _spacing            = serializedObject.FindProperty("_spacing");
            _padding            = serializedObject.FindProperty("_padding");
            _childAlignment     = serializedObject.FindProperty("_childAlignment");
            _prewarmCount       = serializedObject.FindProperty("_prewarmCount");
            _bufferElementCount = serializedObject.FindProperty("_bufferElementCount");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearPreview();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recycled View", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_elementPrefab,      new GUIContent("Element Prefab"));
            EditorGUILayout.PropertyField(_gridConstraint,     new GUIContent("Grid Constraint"));
            EditorGUILayout.PropertyField(_constraintCount,    new GUIContent("Constraint Count"));
            EditorGUILayout.PropertyField(_spacing,            new GUIContent("Spacing"));
            EditorGUILayout.PropertyField(_padding,            new GUIContent("Padding"));
            EditorGUILayout.PropertyField(_childAlignment,     new GUIContent("Child Alignment"));
            EditorGUILayout.PropertyField(_prewarmCount,       new GUIContent("Prewarm Count"));
            EditorGUILayout.PropertyField(_bufferElementCount, new GUIContent("Buffer Elements"));

            if (_constraintCount.intValue    < 1) _constraintCount.intValue    = 1;
            if (_prewarmCount.intValue       < 0) _prewarmCount.intValue       = 0;
            if (_bufferElementCount.intValue < 0) _bufferElementCount.intValue = 0;

            serializedObject.ApplyModifiedProperties();

            // ── 충돌 컴포넌트 경고 ──────────────────────────────────
            var scrollView = (RecycledScrollView)target;
            if (scrollView.content != null)
            {
                bool hasLG  = scrollView.content.GetComponent<LayoutGroup>()      != null;
                bool hasCSF = scrollView.content.GetComponent<ContentSizeFitter>() != null;
                if (hasLG || hasCSF)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "Content에 LayoutGroup 또는 ContentSizeFitter가 감지됐습니다.\n" +
                        "RecycledScrollView와 충돌합니다. 런타임에 자동 제거되지만, 에디터에서도 제거하세요.",
                        MessageType.Error);
                    if (GUILayout.Button("지금 제거 (Undo 지원)"))
                    {
                        if (hasLG)  Undo.DestroyObjectImmediate(scrollView.content.GetComponent<LayoutGroup>());
                        if (hasCSF) Undo.DestroyObjectImmediate(scrollView.content.GetComponent<ContentSizeFitter>());
                    }
                }
            }

            // ── 레이아웃 정보 ────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layout Info (Edit Mode)", EditorStyles.boldLabel);

            var prefab = _elementPrefab.objectReferenceValue as RecycledScrollViewElement;
            if (prefab != null && scrollView.viewport != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.viewport);

                var     prefabRt     = prefab.GetComponent<RectTransform>();
                Vector2 elemSize     = prefabRt != null ? prefabRt.sizeDelta : Vector2.zero;
                Vector2 viewportSize = scrollView.viewport.rect.size;
                Vector2 spacing      = _spacing.vector2Value;
                int     buffer       = _bufferElementCount.intValue;
                bool    isFixedCols  = _gridConstraint.enumValueIndex == 0; // FixedColumnCount
                int     constCount   = Mathf.Max(1, _constraintCount.intValue);
                bool    isVertical   = scrollView.vertical;

                float elemH = elemSize.y + spacing.y;
                float elemW = elemSize.x + spacing.x;

                // 스크롤 방향으로 몇 줄이 뷰포트에 들어오는지
                int visibleAlongScroll = isVertical
                    ? Mathf.CeilToInt(viewportSize.y / Mathf.Max(1f, elemH))
                    : Mathf.CeilToInt(viewportSize.x / Mathf.Max(1f, elemW));
                int withBuffer = visibleAlongScroll + buffer;
                // pool = 스크롤 방향 표시 줄 수 × 고정 축 슬롯 수
                int poolSize = withBuffer * constCount;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Vector2Field("Viewport 크기",      viewportSize);
                    EditorGUILayout.Vector2Field("Element 크기",        elemSize);
                    EditorGUILayout.LabelField  ("고정 축",             isFixedCols ? $"열(Column) = {constCount}" : $"행(Row) = {constCount}");
                    EditorGUILayout.IntField    ("뷰포트 내 표시 (줄 수)", visibleAlongScroll);
                    EditorGUILayout.IntField    ("버퍼 포함 표시 (줄 수)", withBuffer);
                    EditorGUILayout.IntField    ("예상 Pool Size",        poolSize);
                }

                if (elemSize == Vector2.zero)
                    EditorGUILayout.HelpBox("Element Prefab의 sizeDelta가 0입니다. RectTransform 크기를 확인하세요.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Element Prefab 또는 Viewport가 설정되지 않았습니다.", MessageType.Info);
            }

            // ── 에디터 Preview ───────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            _previewItemCount = EditorGUILayout.IntField("Preview 아이템 수", _previewItemCount);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show Preview"))
                {
                    ClearPreview();
                    SpawnPreview(scrollView, _previewItemCount);
                }
                using (new EditorGUI.DisabledScope(_previewObjects.Count == 0))
                {
                    if (GUILayout.Button("Clear Preview"))
                        ClearPreview();
                }
            }

            if (_previewObjects.Count > 0)
                EditorGUILayout.HelpBox($"Preview 중: {_previewObjects.Count}개 / Clear Preview로 제거하세요.", MessageType.Info);
        }

        // ── Preview 생성 ─────────────────────────────────────────────
        private void SpawnPreview(RecycledScrollView scrollView, int count)
        {
            var prefab = _elementPrefab.objectReferenceValue as RecycledScrollViewElement;
            if (prefab == null || scrollView.content == null) return;

            Vector2 spacing     = _spacing.vector2Value;
            var     prefabRt    = prefab.GetComponent<RectTransform>();
            Vector2 elemSize    = prefabRt != null ? prefabRt.sizeDelta : Vector2.zero;
            bool    isFixedCols = _gridConstraint.enumValueIndex == 0;
            int     constCount  = Mathf.Max(1, _constraintCount.intValue);

            if (elemSize == Vector2.zero)
            {
                Debug.LogWarning("[RecycledScrollView] Element sizeDelta가 0이므로 Preview를 표시할 수 없습니다.");
                return;
            }

            int cols, rows;
            if (isFixedCols)
            {
                cols = constCount;
                rows = Mathf.CeilToInt(count / (float)cols);
            }
            else
            {
                rows = constCount;
                cols = Mathf.CeilToInt(count / (float)rows);
            }

            float elemW = elemSize.x + spacing.x;
            float elemH = elemSize.y + spacing.y;

            var pad = _padding.rectValue;
            var alignment = (TextAnchor)_childAlignment.enumValueIndex;

            float gridW = cols * elemSize.x + Mathf.Max(0, cols - 1) * spacing.x;
            float gridH = rows * elemSize.y + Mathf.Max(0, rows - 1) * spacing.y;

            float viewW = scrollView.viewport != null ? scrollView.viewport.rect.width  : 0f;
            float viewH = scrollView.viewport != null ? scrollView.viewport.rect.height : 0f;

            float totalW = gridW + pad.left + pad.right;
            float totalH = gridH + pad.top  + pad.bottom;

            float startX = pad.left;
            float startY = pad.top;

            if (totalW < viewW)
            {
                int hAlign = (int)alignment % 3;
                startX = hAlign switch { 1 => (viewW - gridW) * 0.5f, 2 => viewW - gridW - pad.right, _ => pad.left };
                totalW = viewW;
            }
            if (totalH < viewH)
            {
                int vAlign = (int)alignment / 3;
                startY = vAlign switch { 1 => (viewH - gridH) * 0.5f, 2 => viewH - gridH - pad.bottom, _ => pad.top };
                totalH = viewH;
            }

            // Content 크기 설정 (스크롤 가능 영역)
            Undo.RecordObject(scrollView.content, "RecycledScrollView Preview");
            scrollView.content.anchorMin        = new Vector2(0, 1);
            scrollView.content.anchorMax        = new Vector2(0, 1);
            scrollView.content.pivot            = new Vector2(0, 1);
            scrollView.content.anchoredPosition = Vector2.zero;
            scrollView.content.sizeDelta        = new Vector2(totalW, totalH);

            for (int i = 0; i < count; i++)
            {
                int   row = i / cols;
                int   col = i % cols;
                float x   = startX + col * elemW + elemSize.x * 0.5f;
                float y   = -(startY + row * elemH + elemSize.y * 0.5f);

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scrollView.content);
                go.name = $"[Preview] {i}";

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0, 1);
                rt.anchorMax        = new Vector2(0, 1);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);

                Undo.RegisterCreatedObjectUndo(go, "RecycledScrollView Preview");
                _previewObjects.Add(go);
            }

            EditorSceneManager.MarkSceneDirty(scrollView.gameObject.scene);
        }

        private void ClearPreview()
        {
            foreach (var go in _previewObjects)
            {
                if (go != null)
                    Undo.DestroyObjectImmediate(go);
            }
            _previewObjects.Clear();

            var scrollView = target as RecycledScrollView;
            if (scrollView?.content == null) return;
            var leftovers = new List<GameObject>();
            foreach (Transform child in scrollView.content)
            {
                if (child.name.StartsWith("[Preview]"))
                    leftovers.Add(child.gameObject);
            }
            foreach (var go in leftovers)
                Undo.DestroyObjectImmediate(go);
        }

        // ── 씬에 RecycledScrollView 추가 메뉴 ───────────────────────
        private const string PrefabAssetPath = "Assets/DragonGateCore/Resources/UI/Common/Editor/RecycledScrollView.prefab";

        [MenuItem("GameObject/UI (Custom)/RecycledScrollView", false, 10)]
        private static void CreatePreset(MenuCommand menuCommand)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            if (prefab == null)
            {
                Debug.LogError($"Cannot found prefab: {PrefabAssetPath}");
                return;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject  instance;

            if (prefabStage != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, prefabStage.scene);
                instance.transform.SetParent(prefabStage.prefabContentsRoot.transform, false);
            }
            else
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (menuCommand.context is GameObject parent)
                    GameObjectUtility.SetParentAndAlign(instance, parent);
            }

            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            Undo.RegisterCreatedObjectUndo(instance, "CreatePreset_RecycledScrollView");
            Selection.activeGameObject = instance;
        }
    }
}
#endif