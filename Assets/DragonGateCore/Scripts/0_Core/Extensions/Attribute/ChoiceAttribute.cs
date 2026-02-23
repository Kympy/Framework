using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IntChoice : PropertyAttribute
    {
        public Type SourceType;
        public IntChoice(Type sourceType) => SourceType = sourceType;
    }

    [CustomPropertyDrawer(typeof(IntChoice))]
    public class IntChoiceDrawer : PropertyDrawer
    {
        private static readonly Dictionary<Type, List<Entry>> _cache = new();

        private class Entry
        {
            public string Label;
            public int Value;
            public bool Deprecated;
        }

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            var intChoice = (IntChoice)attribute;
            var list = GetEntries(intChoice.SourceType);

            // 현재 값이 목록에 없으면 “(Unknown: nnn)” 항목 추가
            var current = prop.intValue;
            var idx = list.FindIndex(e => e.Value == current);
            var display = list.Select(e => e.Deprecated ? $"{e.Label}  (deprecated)" : e.Label).ToList();

            if (idx < 0)
            {
                display.Insert(0, $"(Unknown: {current})");
                list.Insert(0, new Entry { Label = $"(Unknown: {current})", Value = current, Deprecated = true });
                idx = 0;
            }

            var newIdx = EditorGUI.Popup(pos, label, idx, display.Select(s => new GUIContent(s)).ToArray());
            prop.intValue = list[newIdx].Value;
        }

        private static List<Entry> GetEntries(Type t)
        {
            if (_cache.TryGetValue(t, out var cached)) return cached;

            var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            var fields = t.GetFields(flags)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(int));

            var list = new List<Entry>();
            foreach (var f in fields)
            {
                var val = (int)f.GetRawConstantValue();

                // 표시 이름: 기본은 필드명, 필요하면 당신이 규칙 추가 가능
                var name = f.Name;

                // [Obsolete] 달리면 표시에서 deprecated 표기
                var obs = f.GetCustomAttribute<ObsoleteAttribute>() != null;

                list.Add(new Entry { Label = name, Value = val, Deprecated = obs });
            }

            // 값 기준 정렬(보기 편하게). 저장 값엔 영향 없음.
            list = list.OrderBy(e => e.Value).ToList();
            _cache[t] = list;
            return list;
        }
    }

    /// <summary>
    /// 코드에 직접 나열한 문자열들을 드롭다운으로 표시하거나 주어진 타입내에 정의된 string 상수들을 수집하여 드롭다운으로 표시합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StringChoice : PropertyAttribute
    {
        public readonly string[] FixedChoices;
        public readonly Type SourceType;

        public StringChoice(params string[] choices)
        {
            FixedChoices = choices ?? Array.Empty<string>();
            SourceType = null;
        }
        
        public StringChoice(Type sourceType) => SourceType = sourceType;
    }

    [CustomPropertyDrawer(typeof(StringChoice))]
    public class StringChoicesDrawer : PropertyDrawer
    {
        private static readonly Dictionary<Type, string[]> CachedChoicesByType = new();

        public override void OnGUI(Rect positionRectangle, SerializedProperty serializedProperty, GUIContent label)
        {
            if (serializedProperty.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(positionRectangle, serializedProperty, label);
                return;
            }

            var attributeInstance = (StringChoice)attribute;
            IReadOnlyList<string> options = GetOptions(attributeInstance);

            string currentValue = serializedProperty.stringValue ?? string.Empty;
            int currentIndex = IndexOf(options, currentValue);

            int newIndex = EditorGUI.Popup(
                positionRectangle,
                label,
                Mathf.Max(currentIndex, 0),
                options.Select(v => new GUIContent(v)).ToArray()
            );

            if (newIndex >= 0 && newIndex < options.Count && newIndex != currentIndex)
            {
                serializedProperty.stringValue = options[newIndex];
            }
            // 목록에 없는 값이면 그대로 유지 (강제 덮어쓰기 안 함)
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static IReadOnlyList<string> GetOptions(StringChoice attributeInstance)
        {
            // 고정 목록
            if (attributeInstance.SourceType == null)
                return attributeInstance.FixedChoices ?? Array.Empty<string>();

            // 타입 기반 상수 수집 (캐시)
            if (!CachedChoicesByType.TryGetValue(attributeInstance.SourceType, out var cached))
            {
                cached = CollectConstStrings(attributeInstance.SourceType);
                CachedChoicesByType[attributeInstance.SourceType] = cached;
            }
            return cached;
            
            
        }

        private static string[] CollectConstStrings(Type sourceType)
        {
            var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            return sourceType
                .GetFields(flags)
                .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue())
                .Distinct()
                .ToArray();
        }

        private static int IndexOf(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == value)
                    return i;
            return -1;
        }
    }
}