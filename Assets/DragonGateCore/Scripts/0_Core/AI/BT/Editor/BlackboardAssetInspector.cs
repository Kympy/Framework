using DragonGate;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlackboardAsset))]
public class BlackboardAssetInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var keysProperty = serializedObject.FindProperty("keys");
        EditorGUILayout.PropertyField(keysProperty, includeChildren: false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Key"))
        {
            keysProperty.InsertArrayElementAtIndex(keysProperty.arraySize);
            var newElement = keysProperty.GetArrayElementAtIndex(keysProperty.arraySize - 1);

            var keyTypeProperty = newElement.FindPropertyRelative("KeyType");
            var nameProperty = newElement.FindPropertyRelative("Name");

            keyTypeProperty.enumValueIndex = 0;
            nameProperty.stringValue = string.Empty;
        }

        if (keysProperty.isExpanded)
        {
            EditorGUI.indentLevel++;

            for (int index = 0; index < keysProperty.arraySize; index++)
            {
                var element = keysProperty.GetArrayElementAtIndex(index);
                var keyTypeProperty = element.FindPropertyRelative("KeyType");
                var nameProperty = element.FindPropertyRelative("Name");
                              
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    keysProperty.DeleteArrayElementAtIndex(index);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(keyTypeProperty);
                EditorGUILayout.PropertyField(nameProperty);

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        EditorGUILayout.Space();

        var generatedClassNameProperty =
            serializedObject.FindProperty("generatedKeysClassName");
        var generatedFilePathProperty =
            serializedObject.FindProperty("generatedKeysFilePath");

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(generatedClassNameProperty);
            EditorGUILayout.PropertyField(generatedFilePathProperty);
        }

        var asset = (BlackboardAsset)target;

        generatedClassNameProperty.stringValue = asset.name.RemoveWhitespaceAndSpecialCharacters();

        var assetPath = AssetDatabase.GetAssetPath(asset);
        var folder = System.IO.Path.GetDirectoryName(assetPath);
        generatedFilePathProperty.stringValue =
            $"{folder}/{generatedClassNameProperty.stringValue}.Keys.Generated.cs";

        bool canGenerate =
            !string.IsNullOrEmpty(generatedClassNameProperty.stringValue) &&
            !string.IsNullOrEmpty(generatedFilePathProperty.stringValue);

        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            if (GUILayout.Button("Generate Keys", GUILayout.Height(30)))
            {
                BlackboardKeyCodeGenerator.Generate(asset);
            }
        }

        if (!canGenerate)
        {
            EditorGUILayout.HelpBox(
                "Generated Class Name과 Generated Keys File Path에 문제가 있습니다.",
                MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}