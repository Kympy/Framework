using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DragonGate
{
    public enum ELayoutStateType
    {
        GameObjectActive,   // GameObject.SetActive
        ComponentEnabled,   // Behaviour.enabled
        LocalPosition,      // Transform.localPosition
        LocalRotation,      // Transform.localEulerAngles
        LocalScale,         // Transform.localScale
        ComponentField,     // 컴포넌트의 임의 필드/프로퍼티 (reflection)
    }

    public enum EFieldValueType
    {
        Bool,
        Int,
        Float,
        Vector2,
        Vector3,
        Color,
    }

    [Serializable]
    public class LayoutStateEntry
    {
        public ELayoutStateType Type;

        // 대상 오브젝트 / 컴포넌트
        public GameObject  TargetObject;     // GameObjectActive, LocalPosition/Rotation/Scale
        public Component   TargetComponent;  // ComponentEnabled, ComponentField
        public string      FieldName;        // ComponentField: 필드/프로퍼티 이름
        public EFieldValueType FieldValueType; // ComponentField: 값 타입

        // 저장된 값 (타입에 따라 하나만 사용)
        public bool    BoolValue;
        public int     IntValue;
        public float   FloatValue;
        public Vector2 Vector2Value;
        public Vector3 Vector3Value;  // LocalRotation 은 EulerAngles 로 저장
        public Color   ColorValue;

        // ─── 현재 씬/프리팹 상태를 캡처해 저장 ──────────────────────────
        public void Capture()
        {
            switch (Type)
            {
                case ELayoutStateType.GameObjectActive:
                    if (TargetObject) BoolValue = TargetObject.activeSelf;
                    break;

                case ELayoutStateType.ComponentEnabled:
                    if (TargetComponent is Behaviour b) BoolValue = b.enabled;
                    break;

                case ELayoutStateType.LocalPosition:
                    if (TargetObject) Vector3Value = TargetObject.transform.localPosition;
                    break;

                case ELayoutStateType.LocalRotation:
                    if (TargetObject) Vector3Value = TargetObject.transform.localEulerAngles;
                    break;

                case ELayoutStateType.LocalScale:
                    if (TargetObject) Vector3Value = TargetObject.transform.localScale;
                    break;

                case ELayoutStateType.ComponentField:
                    CaptureField();
                    break;
            }
        }

        // ─── 저장된 값을 씬/오브젝트에 적용 ────────────────────────────
        public void Apply()
        {
            switch (Type)
            {
                case ELayoutStateType.GameObjectActive:
                    if (TargetObject) TargetObject.SetActive(BoolValue);
                    break;

                case ELayoutStateType.ComponentEnabled:
                    if (TargetComponent is Behaviour b) b.enabled = BoolValue;
                    break;

                case ELayoutStateType.LocalPosition:
                    if (TargetObject) TargetObject.transform.localPosition = Vector3Value;
                    break;

                case ELayoutStateType.LocalRotation:
                    if (TargetObject) TargetObject.transform.localEulerAngles = Vector3Value;
                    break;

                case ELayoutStateType.LocalScale:
                    if (TargetObject) TargetObject.transform.localScale = Vector3Value;
                    break;

                case ELayoutStateType.ComponentField:
                    ApplyField();
                    break;
            }
        }

        // ─── ComponentField 내부 ─────────────────────────────────────────
        private void CaptureField()
        {
            if (!TargetComponent || string.IsNullOrEmpty(FieldName)) return;

            var value = GetMemberValue(TargetComponent, FieldName);
            if (value == null) return;

            switch (FieldValueType)
            {
                case EFieldValueType.Bool:    BoolValue    = (bool)value;    break;
                case EFieldValueType.Int:     IntValue     = (int)value;     break;
                case EFieldValueType.Float:   FloatValue   = (float)value;   break;
                case EFieldValueType.Vector2: Vector2Value = (Vector2)value; break;
                case EFieldValueType.Vector3: Vector3Value = (Vector3)value; break;
                case EFieldValueType.Color:   ColorValue   = (Color)value;   break;
            }
        }

        private void ApplyField()
        {
            if (!TargetComponent || string.IsNullOrEmpty(FieldName)) return;

            object value = FieldValueType switch
            {
                EFieldValueType.Bool    => (object)BoolValue,
                EFieldValueType.Int     => IntValue,
                EFieldValueType.Float   => FloatValue,
                EFieldValueType.Vector2 => Vector2Value,
                EFieldValueType.Vector3 => Vector3Value,
                EFieldValueType.Color   => ColorValue,
                _                       => null,
            };

            if (value != null) SetMemberValue(TargetComponent, FieldName, value);
        }

        // ─── Reflection 헬퍼 (Editor 에서도 공유) ───────────────────────
        internal static object GetMemberValue(Component component, string memberName)
        {
            var type = component.GetType();

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) return field.GetValue(component);

            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop?.CanRead == true) return prop.GetValue(component);

            return null;
        }

        internal static void SetMemberValue(Component component, string memberName, object value)
        {
            var type = component.GetType();

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) { field.SetValue(component, value); return; }

            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop?.CanWrite == true) prop.SetValue(component, value);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    public class LayoutData
    {
        public string Name;
        public List<LayoutStateEntry> Entries = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    public class LayoutState : CoreBehaviour
    {
        [SerializeField] private List<LayoutData> _layouts = new();

        /// <summary>이름으로 레이아웃을 찾아 적용합니다.</summary>
        public void ApplyLayout(string layoutName)
        {
            var layout = _layouts.Find(l => l.Name == layoutName);
            if (layout == null)
            {
                Debug.LogWarning($"[LayoutStateComponent] Layout '{layoutName}' not found on '{name}'.");
                return;
            }
            ApplyLayout(layout);
        }

        /// <summary>인덱스로 레이아웃을 적용합니다.</summary>
        public void ApplyLayout(int index)
        {
            if (index < 0 || index >= _layouts.Count)
            {
                Debug.LogWarning($"[LayoutStateComponent] Layout index {index} out of range on '{name}'.");
                return;
            }
            ApplyLayout(_layouts[index]);
        }

        private void ApplyLayout(LayoutData layout)
        {
            foreach (var entry in layout.Entries)
                entry.Apply();
        }

#if UNITY_EDITOR
        // 에디터에서 직접 접근할 때만 사용
        public List<LayoutData> EditorLayouts => _layouts;
#endif
    }
}