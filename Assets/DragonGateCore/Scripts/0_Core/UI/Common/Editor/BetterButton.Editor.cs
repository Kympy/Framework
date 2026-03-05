using UnityEditor;

namespace DragonGate
{
    [CustomEditor(typeof(BetterButton))]
    public class BetterButtonEditor : UnityEditor.UI.ButtonEditor
    {
        private SerializedProperty _buttonText;
        private SerializedProperty _dimmedObject;
        private SerializedProperty _clickSound;
        private SerializedProperty _enterSound;

        protected override void OnEnable()
        {
            base.OnEnable();
            _buttonText = serializedObject.FindProperty("_buttonText");
            _dimmedObject = serializedObject.FindProperty(nameof(_dimmedObject));
            _clickSound = serializedObject.FindProperty("ClickSound");
            _enterSound = serializedObject.FindProperty("EnterSound");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // Button 기본 필드
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BetterButton Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_buttonText);
            EditorGUILayout.PropertyField(_dimmedObject);
            EditorGUILayout.PropertyField(_clickSound);
            EditorGUILayout.PropertyField(_enterSound);

            serializedObject.ApplyModifiedProperties();
        }
    }
}