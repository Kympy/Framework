using UnityEditor;
using UnityEditor.UI;

namespace Framework
{
    [CustomEditor(typeof(BetterVerticalLayoutGroup))]
    public class BetterVerticalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        private SerializedProperty _xDistance;

        protected override void OnEnable()
        {
            base.OnEnable();
            _xDistance = serializedObject.FindProperty("_xDistance");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_xDistance);
            serializedObject.ApplyModifiedProperties();
        }
    }
}