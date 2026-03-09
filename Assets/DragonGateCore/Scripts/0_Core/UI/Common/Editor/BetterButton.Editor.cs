using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        
        // ── 씬에 LocalizedTMPUGUI 추가 메뉴 ───────────────────────
        private const string PrefabAssetPath = "Assets/DragonGateCore/Resources/UI/Common/Editor/BetterButton.prefab";

        [MenuItem("GameObject/UI (Custom)/Better Button", false, 10)]
        private static void CreatePreset(MenuCommand menuCommand)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            if (prefab == null)
            {
                DGDebug.LogError($"Cannot found prefab: {PrefabAssetPath}");
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
            Undo.RegisterCreatedObjectUndo(instance, "CreatePreset_BetterButton");
            Selection.activeGameObject = instance;
        }
    }
}