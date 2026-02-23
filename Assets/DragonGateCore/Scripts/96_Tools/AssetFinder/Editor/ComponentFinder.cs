namespace DragonGate
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    public class ComponentFinder : EditorWindow
    {
        [MenuItem("Tools/컴포넌트 찾기")]
        public static void ShowWindow()
        {
            GetWindow<ComponentFinder>("컴포넌트 찾기");
        }

        private int selectedIndex = 0;
        private string searchString = "";
        private string[] allComponentTypes;
        private string[] filteredComponentTypes;

        private List<GameObject> foundObjects = new List<GameObject>();
        private Vector2 scrollPosition;
        private Vector2 resultScrollPosition;

        private Dictionary<string, Type> specialTypes = new Dictionary<string, Type>()
        {
            // { "StudioEventEmitter", typeof(StudioEventEmitter) }
        };

        private void OnEnable()
        {
            LoadComponentTypes();
        }

        private void LoadComponentTypes()
        {
            // ✅ 현재 로드된 모든 어셈블리에서 UnityEngine 컴포넌트 타입 가져오기
            allComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(Component)) && t.Namespace != null && t.Namespace.StartsWith("UnityEngine") && t.FullName.EndsWith("Transform") == false)
                .Select(t => t.FullName)
                .OrderBy(name => name)
                .ToArray();

            filteredComponentTypes = allComponentTypes;
        }

        private void OnGUI()
        {
            GUILayout.Label("컴포넌트 찾기", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            searchString = EditorGUILayout.TextField("1. 컴포넌트 이름으로 필터링 후 찾기", searchString);
            if (EditorGUI.EndChangeCheck())
            {
                FilterComponentTypes(searchString);
            }

            resultScrollPosition = GUILayout.BeginScrollView(resultScrollPosition, GUILayout.Height(150));
            for (int i = 0; i < filteredComponentTypes.Length; i++)
            {
                if (GUILayout.Button(filteredComponentTypes[i], GUILayout.Height(20)))
                {
                    selectedIndex = Array.IndexOf(allComponentTypes, filteredComponentTypes[i]);
                    FindObjectsWithComponent(allComponentTypes[selectedIndex]);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.Space(50);
            GUILayout.Label("2. 드롭다운에서 선택하여 찾기", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            selectedIndex = EditorGUILayout.Popup("찾을 타입", selectedIndex, allComponentTypes);
            if (GUILayout.Button("찾기"))
            {
                FindObjectsWithComponent(allComponentTypes[selectedIndex]);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(50);

            GUILayout.Label("3. 특수 타입 목록에서 선택하기", EditorStyles.boldLabel);
            foreach (var obj in specialTypes)
            {
                if (GUILayout.Button(obj.Key, GUILayout.Height(20)))
                {
                    FindObjectsWithComponent(obj.Value);
                }
            }

            GUILayout.Space(50);

            GUILayout.Label("결과 :", EditorStyles.boldLabel);
            GUILayout.Label("오브젝트의 계층 구조가 '/'로 구분하여 표시됩니다.");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            if (foundObjects.Count > 0)
            {
                foreach (var obj in foundObjects)
                {
                    if (GUILayout.Button(GetFullName(obj), GUILayout.Height(25)))
                    {
                        SelectGameObject(obj);
                    }
                }
            }
            else
            {
                GUILayout.Label("검색결과가 없습니다.");
            }

            GUILayout.EndScrollView();
        }

        private List<string> full_path = new List<string>();
        private StringBuilder sb = new StringBuilder();

        private void SelectGameObject(GameObject obj)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private void FindObjectsWithComponent(Type type)
        {
            FindObjects(type);
        }

        private void FindObjectsWithComponent(string typeName)
        {
            DGDebug.Log($"Find : {typeName}");
            if (string.IsNullOrEmpty(typeName))
            {
                DGDebug.LogWarning("Component type cannot be empty");
                return;
            }

            Type type = GetTypeFromAllAssemblies(typeName);
            if (type == null)
            {
                DGDebug.LogError($"Component type '{typeName}' not found. Try using full namespace (e.g., UnityEngine.UI.Text).");
                return;
            }

            FindObjects(type);
        }

        private void FindObjects(Type type)
        {
            foundObjects.Clear();
            string titleStr = $"{type.Name} 찾기...";
            EditorUtility.DisplayCancelableProgressBar(titleStr, "준비중...", 0);
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int totalCount = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                EditorUtility.DisplayCancelableProgressBar(titleStr, $"({i + 1}/{guids.Length}) {prefab.name}", i + 1 / (float)guids.Length);
                if (prefab)
                {
                    var compos = prefab.GetComponentsInChildren(type);
                    foreach (var compo in compos)
                    {
                        foundObjects.Add(compo.gameObject);
                    }
                    // foundObjects.Add(prefab);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private string GetFullName(GameObject obj)
        {
            full_path.Clear();
            full_path.Add(obj.name);

            GameObject target = obj;
            while (true)
            {
                if (target.transform.parent != null)
                {
                    full_path.Add(target.transform.parent.gameObject.name);
                    target = target.transform.parent.gameObject;
                    continue;
                }

                break;
            }

            sb.Clear();
            for (int i = full_path.Count - 1; i >= 0; i--)
            {
                sb.Append(full_path[i]);
                if (i != 0)
                    sb.Append('/');
            }

            return sb.ToString();
        }

        // 현재 로드된 모든 어셈블리에서 타입 검색
        private Type GetTypeFromAllAssemblies(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(t => t.FullName == typeName);
        }

        private void FilterComponentTypes(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                filteredComponentTypes = allComponentTypes;
            }
            else
            {
                filteredComponentTypes = allComponentTypes
                    .Where(t => t.ToLower().Contains(filter.ToLower()))
                    .ToArray();
            }
        }

    }
#endif
}
