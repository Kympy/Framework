using System;
using System.Collections.Generic;
using UnityEditor;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        // Choice 추가
        private void CreateChoice(DialogueNode node, SerializedProperty choiceProp, SerializedObject so)
        {
            var guid = Guid.NewGuid().ToString();
            choiceProp.InsertArrayElementAtIndex(choiceProp.arraySize);
            var newElem = choiceProp.GetArrayElementAtIndex(choiceProp.arraySize - 1);
            newElem.FindPropertyRelative("Id").stringValue = guid;
            newElem.FindPropertyRelative("IsEnabled").boolValue = true;
            newElem.FindPropertyRelative("TargetNodeId").stringValue = string.Empty;
            so.ApplyModifiedProperties();

            string key = string.Format(LOCALIZATION_CHOICE_KEY_FORMAT, guid);
            node.Choices[^1].ChoiceText = AddLocalizationKey(LOCALIZATION_CHOICE_TABLE, key);
            EditorUtility.SetDirty(_graph);
        }

        private void CloneChoice(DialogueNode targetNode, DialogueNode sourceNode)
        {
            targetNode.Choices = new List<ChoiceData>();
            foreach (var sourceChoice in sourceNode.Choices)
            {
                var guid = Guid.NewGuid().ToString();
                string key = string.Format(LOCALIZATION_CHOICE_KEY_FORMAT, guid);
                var newChoiceData = new ChoiceData()
                {
                    Id = guid,
                    IsEnabled = sourceChoice.IsEnabled,
                    TargetNodeId = null,
                    ChoiceText = AddLocalizationKey(LOCALIZATION_CHOICE_TABLE, key),
                };
                targetNode.Choices.Add(newChoiceData);
            }
        }
    }
}
