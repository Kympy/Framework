#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DGFramework.Editor.Tools.AssetFinder
{
    public class FindMissing : EditorWindow
    {
        private static List<string> missingEntries = new List<string>();
        private static int totalCount = 0;

        public enum EFindRange
        {
            AllPrefab,
            ActiveScene,
        }

        public enum EFindOption
        {
            All,
            ComponentOnly,
            MaterialOnly,
        }

        private static void Find(EFindRange range, EFindOption option)
        {
            missingEntries.Clear();
            if (range == EFindRange.AllPrefab)
            {
                // 프로젝트 내 프리팹 검사
                string[] guids = AssetDatabase.FindAssets("t:Prefab");
                totalCount = guids.Length;
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab)
                    {
                        ShowProgress(path, i + 1);
                        CheckGameObject(prefab, path, missingEntries, option);
                    }
                }
            }
            else if (range == EFindRange.ActiveScene)
            {
                // 현재 열린 씬 검사
                CheckSceneObjects(SceneManager.GetActiveScene(), missingEntries, option);
            }

            ShowResult();
        }

        [MenuItem("Tools/Missing 찾기/모든 프리팹/전체")]
        public static void FindAllAll()
        {
            Find(EFindRange.AllPrefab, EFindOption.All);
        }

        [MenuItem("Tools/Missing 찾기/모든 프리팹/컴포넌트")]
        public static void FindAllComponent()
        {
            Find(EFindRange.AllPrefab, EFindOption.ComponentOnly);
        }

        [MenuItem("Tools/Missing 찾기/모든 프리팹/머터리얼")]
        public static void FindAllMaterial()
        {
            Find(EFindRange.AllPrefab, EFindOption.MaterialOnly);
        }

        [MenuItem("Tools/Missing 찾기/현재 씬/전체")]
        public static void FindSceneAll()
        {
            Find(EFindRange.ActiveScene, EFindOption.All);
        }

        [MenuItem("Tools/Missing 찾기/현재 씬/컴포넌트")]
        public static void FindSceneComponent()
        {
            Find(EFindRange.ActiveScene, EFindOption.ComponentOnly);
        }

        [MenuItem("Tools/Missing 찾기/현재 씬/머터리얼")]
        public static void FindSceneMaterial()
        {
            Find(EFindRange.ActiveScene, EFindOption.MaterialOnly);
        }

        private static void ShowResult()
        {
            EditorUtility.ClearProgressBar();
            // 결과 출력
            if (missingEntries.Count > 0)
            {
                string result = string.Join("\n\n", missingEntries);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < missingEntries.Count; i++)
                {
                    sb.Append($"[{i + 1}] : ");
                    sb.AppendLine(missingEntries[i]);
                    sb.AppendLine();
                }

                // 바탕화면 경로 설정
                string desktopPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "MissingComponentsReport.txt");
                File.WriteAllText(desktopPath, sb.ToString());
                EditorUtility.DisplayDialog("Missing Components Found", $"누락된 항목이 발견되었습니다.({missingEntries.Count})\n리포트 저장 위치: {desktopPath}\n\n{sb.ToString()}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Issues Found", "모든 에셋이 정상적입니다.", "OK");
            }
        }

        private static void CheckSceneObjects(UnityEngine.SceneManagement.Scene scene, List<string> missingEntries, EFindOption option)
        {
            var sceneObjects = scene.GetRootGameObjects();
            totalCount = sceneObjects.Length;
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                ShowProgress(sceneObjects[i].name, i + 1);
                CheckGameObject(sceneObjects[i], $"Scene: {scene.name}", missingEntries, option);
            }
        }

        private static void CheckGameObject(GameObject obj, string context, List<string> missingEntries, EFindOption option)
        {
            if (option != EFindOption.MaterialOnly)
            {
                // 누락된 컴포넌트 검사
                Component[] components = obj.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        missingEntries.Add($"[ Missing Component ]\n\tPath : {context}\n\tDetail : {GetFullPath(obj)}");
                    }
                }
            }

            if (option != EFindOption.ComponentOnly)
            {
                // Renderer의 머티리얼 검사
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer && renderer.sharedMaterial == null)
                {
                    missingEntries.Add($"[ Missing Material ]\n\tPath : {context}\n\tDetail : {GetFullPath(obj)}");
                }
            }

            // 자식 오브젝트 재귀 검사
            foreach (Transform child in obj.transform)
            {
                CheckGameObject(child.gameObject, context, missingEntries, option);
            }
        }

        private static string GetFullPath(GameObject obj)
        {
            return obj.transform.parent == null ? obj.name : $"{GetFullPath(obj.transform.parent.gameObject)}/{obj.name}";
        }

        private const string windowTitle = "Find Missing";

        private static void ShowProgress(string path, int current)
        {
            EditorUtility.DisplayProgressBar(windowTitle, $"({current}/{totalCount}) {path}", current / (float)totalCount);
        }
    }
}

#endif
