#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    [CustomEditor(typeof(LayoutState))]
    public class LayoutStateEditor : UnityEditor.Editor
    {
        private LayoutState _target;
        private SerializedProperty   _layoutsProp;
        private readonly List<bool>  _foldouts = new();

        // ─── 색상 상수 ──────────────────────────────────────────────────────
        private static readonly Color ColCapture = new(0.50f, 0.85f, 0.50f);
        private static readonly Color ColApply   = new(0.50f, 0.75f, 1.00f);
        private static readonly Color ColRemove  = new(1.00f, 0.50f, 0.50f);
        private static readonly Color ColBox     = new(0.22f, 0.22f, 0.22f, 1f);

        // ─── 필드 캐시 (컴포넌트→지원 필드 목록) ───────────────────────────
        private static readonly Dictionary<Type, List<(string name, EFieldValueType vt)>> _memberCache = new();

        // ─────────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            _target      = (LayoutState)target;
            _layoutsProp = serializedObject.FindProperty("_layouts");
        }

        // ─── 메인 인스펙터 ───────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // foldout 목록 크기 동기화
            while (_foldouts.Count < _layoutsProp.arraySize) _foldouts.Add(true);
            while (_foldouts.Count > _layoutsProp.arraySize) _foldouts.RemoveAt(_foldouts.Count - 1);

            EditorGUILayout.Space(2);

            for (int i = 0; i < _layoutsProp.arraySize; i++)
            {
                if (DrawLayout(i))
                {
                    i--;
                    continue;
                }
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("＋  Add Layout", GUILayout.Height(26)))
            {
                int next = _layoutsProp.arraySize;
                _layoutsProp.InsertArrayElementAtIndex(next);
                var el = _layoutsProp.GetArrayElementAtIndex(next);
                el.FindPropertyRelative("Name").stringValue = $"Layout {next + 1}";
                el.FindPropertyRelative("Entries").ClearArray();
                _foldouts.Add(true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ─── 레이아웃 하나 그리기 ────────────────────────────────────────────
        // 삭제됐으면 true 반환
        private bool DrawLayout(int idx)
        {
            var layoutProp  = _layoutsProp.GetArrayElementAtIndex(idx);
            var nameProp    = layoutProp.FindPropertyRelative("Name");
            var entriesProp = layoutProp.FindPropertyRelative("Entries");

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = ColBox;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = prevBg;

                // ── 헤더 ──────────────────────────────────────────────────
                using (new EditorGUILayout.HorizontalScope())
                {
                    var label = string.IsNullOrWhiteSpace(nameProp.stringValue)
                        ? $"Layout {idx + 1}"
                        : nameProp.stringValue;

                    _foldouts[idx] = EditorGUILayout.Foldout(
                        _foldouts[idx], label, true, EditorStyles.foldoutHeader);

                    GUI.backgroundColor = ColCapture;
                    if (GUILayout.Button("Capture", GUILayout.Width(68), GUILayout.Height(20)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        DoCaptureLayout(idx);
                        serializedObject.Update();
                    }

                    GUI.backgroundColor = ColApply;
                    if (GUILayout.Button("Apply", GUILayout.Width(56), GUILayout.Height(20)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        DoApplyLayout(idx);
                    }

                    GUI.backgroundColor = ColRemove;
                    if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        _layoutsProp.DeleteArrayElementAtIndex(idx);
                        _foldouts.RemoveAt(idx);
                        serializedObject.ApplyModifiedProperties();
                        GUI.backgroundColor = prevBg;
                        return true;
                    }

                    GUI.backgroundColor = prevBg;
                }

                if (!_foldouts[idx]) return false;

                // ── 이름 필드 ────────────────────────────────────────────
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nameProp, new GUIContent("Name"));
                EditorGUILayout.Space(4);

                // ── 엔트리 목록 ──────────────────────────────────────────
                for (int j = 0; j < entriesProp.arraySize; j++)
                {
                    if (DrawEntry(entriesProp, j))
                    {
                        j--;
                        continue;
                    }
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button("＋  Add Entry"))
                    AddEntry(entriesProp);

                EditorGUI.indentLevel--;
            }
            return false;
        }

        // ─── 엔트리 하나 그리기 ──────────────────────────────────────────────
        // 삭제됐으면 true 반환
        private bool DrawEntry(SerializedProperty entriesProp, int idx)
        {
            var e        = entriesProp.GetArrayElementAtIndex(idx);
            var typeProp = e.FindPropertyRelative("Type");
            var objProp  = e.FindPropertyRelative("TargetObject");
            var compProp = e.FindPropertyRelative("TargetComponent");
            var fieldProp = e.FindPropertyRelative("FieldName");
            var fvtProp   = e.FindPropertyRelative("FieldValueType");

            var entryType = (ELayoutStateType)typeProp.enumValueIndex;
            var fvt       = (EFieldValueType)fvtProp.enumValueIndex;

            var prevBg = GUI.backgroundColor;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // ── 1행: 타입 | 대상 | 삭제 ─────────────────────────────
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(typeProp, GUIContent.none, GUILayout.Width(148));

                    switch (entryType)
                    {
                        case ELayoutStateType.GameObjectActive:
                        case ELayoutStateType.LocalPosition:
                        case ELayoutStateType.LocalRotation:
                        case ELayoutStateType.LocalScale:
                            EditorGUILayout.PropertyField(objProp, GUIContent.none);
                            break;

                        case ELayoutStateType.ComponentEnabled:
                        case ELayoutStateType.ComponentField:
                        {
                            var cur = compProp.objectReferenceValue as Component;
                            var next = EditorGUILayout.ObjectField(cur, typeof(Component), true) as Component;
                            if (next != cur)
                            {
                                compProp.objectReferenceValue = next;
                                fieldProp.stringValue = "";
                            }
                            break;
                        }
                    }

                    GUI.backgroundColor = ColRemove;
                    if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
                    {
                        entriesProp.DeleteArrayElementAtIndex(idx);
                        GUI.backgroundColor = prevBg;
                        return true;
                    }
                    GUI.backgroundColor = prevBg;
                }

                // ── 2행: ComponentField → 필드 선택 ─────────────────────
                if (entryType == ELayoutStateType.ComponentField)
                {
                    var comp = compProp.objectReferenceValue as Component;
                    if (comp != null)
                        DrawFieldPicker(comp, fieldProp, fvtProp);
                    else
                        EditorGUILayout.HelpBox("컴포넌트를 먼저 지정하세요.", MessageType.None);
                }

                // ── 3행: 저장된 값 ───────────────────────────────────────
                DrawStoredValue(e, entryType, fvt);
            }
            return false;
        }

        // ─── 컴포넌트 필드 드롭다운 ─────────────────────────────────────────
        private void DrawFieldPicker(Component comp,
            SerializedProperty fieldProp, SerializedProperty fvtProp)
        {
            var members = GetSupportedMembers(comp.GetType());
            if (members.Count == 0)
            {
                EditorGUILayout.HelpBox("지원되는 필드/프로퍼티가 없습니다.", MessageType.Info);
                return;
            }

            var labels  = members.ConvertAll(m => $"{m.name}  ({m.vt})").ToArray();
            int current = members.FindIndex(m => m.name == fieldProp.stringValue);
            if (current < 0) current = 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Field", GUILayout.Width(40));
                int selected = EditorGUILayout.Popup(current, labels);
                if (selected != current || string.IsNullOrEmpty(fieldProp.stringValue))
                {
                    fieldProp.stringValue       = members[selected].name;
                    fvtProp.enumValueIndex      = (int)members[selected].vt;
                }
            }
        }

        // ─── 저장된 값 표시/편집 ────────────────────────────────────────────
        private static void DrawStoredValue(SerializedProperty e,
            ELayoutStateType type, EFieldValueType fvt)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Stored", GUILayout.Width(46));
                switch (type)
                {
                    case ELayoutStateType.GameObjectActive:
                    case ELayoutStateType.ComponentEnabled:
                        EditorGUILayout.PropertyField(
                            e.FindPropertyRelative("BoolValue"), GUIContent.none);
                        break;

                    case ELayoutStateType.LocalPosition:
                    case ELayoutStateType.LocalRotation:
                    case ELayoutStateType.LocalScale:
                        EditorGUILayout.PropertyField(
                            e.FindPropertyRelative("Vector3Value"), GUIContent.none);
                        break;

                    case ELayoutStateType.ComponentField:
                        switch (fvt)
                        {
                            case EFieldValueType.Bool:
                                EditorGUILayout.PropertyField(e.FindPropertyRelative("BoolValue"),    GUIContent.none); break;
                            case EFieldValueType.Int:
                                EditorGUILayout.PropertyField(e.FindPropertyRelative("IntValue"),     GUIContent.none); break;
                            case EFieldValueType.Float:
                                EditorGUILayout.PropertyField(e.FindPropertyRelative("FloatValue"),   GUIContent.none); break;
                            case EFieldValueType.Vector2:
                                EditorGUILayout.PropertyField(e.FindPropertyRelative("Vector2Value"), GUIContent.none); break;
                            case EFieldValueType.Vector3:
                                EditorGUILayout.PropertyField(e.FindPropertyRelative("Vector3Value"), GUIContent.none); break;
                            case EFieldValueType.Color:
                                EditorGUILayout.PropertyField(e.FindPropertyRelative("ColorValue"),   GUIContent.none); break;
                        }
                        break;
                }
            }
        }

        // ─── 빈 엔트리 추가 ─────────────────────────────────────────────────
        private static void AddEntry(SerializedProperty entriesProp)
        {
            int i = entriesProp.arraySize;
            entriesProp.InsertArrayElementAtIndex(i);
            var e = entriesProp.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("Type").enumValueIndex             = 0;
            e.FindPropertyRelative("TargetObject").objectReferenceValue   = null;
            e.FindPropertyRelative("TargetComponent").objectReferenceValue = null;
            e.FindPropertyRelative("FieldName").stringValue           = "";
            e.FindPropertyRelative("BoolValue").boolValue             = false;
            e.FindPropertyRelative("IntValue").intValue               = 0;
            e.FindPropertyRelative("FloatValue").floatValue           = 0f;
            e.FindPropertyRelative("ColorValue").colorValue           = Color.white;
        }

        // ─── Capture (에디터 버튼) ───────────────────────────────────────────
        private void DoCaptureLayout(int idx)
        {
            Undo.RecordObject(_target, "Capture Layout State");
            foreach (var entry in _target.EditorLayouts[idx].Entries)
                entry.Capture();
            EditorUtility.SetDirty(_target);
        }

        // ─── Apply (에디터 버튼) ─────────────────────────────────────────────
        private void DoApplyLayout(int idx)
        {
            var layout   = _target.EditorLayouts[idx];
            var toRecord = new HashSet<UnityEngine.Object>();

            foreach (var entry in layout.Entries)
            {
                switch (entry.Type)
                {
                    case ELayoutStateType.GameObjectActive:
                        if (entry.TargetObject) toRecord.Add(entry.TargetObject);
                        break;
                    case ELayoutStateType.LocalPosition:
                    case ELayoutStateType.LocalRotation:
                    case ELayoutStateType.LocalScale:
                        if (entry.TargetObject) toRecord.Add(entry.TargetObject.transform);
                        break;
                    case ELayoutStateType.ComponentEnabled:
                    case ELayoutStateType.ComponentField:
                        if (entry.TargetComponent) toRecord.Add(entry.TargetComponent);
                        break;
                }
            }

            var arr = new UnityEngine.Object[toRecord.Count];
            toRecord.CopyTo(arr);
            Undo.RecordObjects(arr, "Apply Layout State");

            foreach (var entry in layout.Entries)
                entry.Apply();

            foreach (var obj in toRecord)
                EditorUtility.SetDirty(obj);
        }

        // ─── 지원 멤버 수집 (캐시) ──────────────────────────────────────────
        private static List<(string name, EFieldValueType vt)> GetSupportedMembers(Type type)
        {
            if (_memberCache.TryGetValue(type, out var cached)) return cached;

            var result = new List<(string, EFieldValueType)>();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (var f in type.GetFields(flags))
            {
                var vt = ToFieldValueType(f.FieldType);
                if (vt.HasValue) result.Add((f.Name, vt.Value));
            }

            foreach (var p in type.GetProperties(flags))
            {
                if (!p.CanRead || !p.CanWrite) continue;
                if (p.GetIndexParameters().Length > 0) continue;
                var vt = ToFieldValueType(p.PropertyType);
                if (vt.HasValue) result.Add((p.Name, vt.Value));
            }

            _memberCache[type] = result;
            return result;
        }

        private static EFieldValueType? ToFieldValueType(Type t)
        {
            if (t == typeof(bool))    return EFieldValueType.Bool;
            if (t == typeof(int))     return EFieldValueType.Int;
            if (t == typeof(float))   return EFieldValueType.Float;
            if (t == typeof(Vector2)) return EFieldValueType.Vector2;
            if (t == typeof(Vector3)) return EFieldValueType.Vector3;
            if (t == typeof(Color))   return EFieldValueType.Color;
            return null;
        }
    }
}
#endif