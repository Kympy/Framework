using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    public static class RemoveMissingComponents
    {
        // [MenuItem("GameObject/Remove Missing Components (Recursive)", true)]
        // private static bool Validate(MenuCommand command)
        // {
        //     return command.context is GameObject;
        // }

        [MenuItem("GameObject/Prefab/Remove Missing Components (Recursive)")]
        private static void RemoveMissing(MenuCommand command)
        {
            var rootObject = command.context as GameObject;
            if (rootObject == null)
                return;

            Undo.RegisterFullObjectHierarchyUndo(rootObject, "Remove Missing Components");

            int removedCount = 0;

            var transforms = rootObject.GetComponentsInChildren<Transform>(true);
            foreach (var transform in transforms)
            {
                removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            }

            Debug.Log($"[{rootObject.name}] Missing Component 제거 완료: {removedCount}개");
        }
    }
}
