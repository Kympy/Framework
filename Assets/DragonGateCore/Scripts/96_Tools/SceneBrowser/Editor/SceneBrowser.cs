using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DragonGate.Editor
{
    public sealed class SceneBrowserWindow : EditorWindow
    {
        private const string StartScenePrefKey = "SceneBrowser.StartScene";
        private const string FavoriteScenesPrefKey = "SceneBrowser.FavoriteScenes";

        private Vector2 _scrollPosition;
        private List<string> _scenePaths;
        private string _statusMessage;
        private HashSet<string> _favoriteScenes;
        private string _sceneSearchText = string.Empty;

        [MenuItem("Tools/Scene 탐색기")]
        public static void Open()
        {
            GetWindow<SceneBrowserWindow>("Scene 탐색기");
        }

        private void OnEnable()
        {
            LoadFavorites();
            RefreshScenes();
        }
        
        private void LoadFavorites()
        {
            var raw = EditorPrefs.GetString(FavoriteScenesPrefKey, string.Empty);
            _favoriteScenes = new HashSet<string>(
                string.IsNullOrEmpty(raw) ? new string[0] : raw.Split('|')
            );
        }

        private void RefreshScenes()
        {
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            _scenePaths = new List<string>(guids.Length);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".unity"))
                {
                    _scenePaths.Add(path);
                }
            }

            var favorites = GetFavoriteScenes();
            _scenePaths.Sort((a, b) =>
            {
                var aFav = favorites.Contains(a);
                var bFav = favorites.Contains(b);
                if (aFav != bFav)
                    return aFav ? -1 : 1;

                return Path.GetFileName(a).CompareTo(Path.GetFileName(b));
            });
            SetStatus($"씬 목록 새로고침 완료 ({_scenePaths.Count}개)");
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(10); 
            DrawDescription();
            EditorGUILayout.Space(8);
            DrawSceneList();
            EditorGUILayout.Space(6);
            DrawStatusBar();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("SCENE 탐색기", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);

                if (GUILayout.Button("▶ 시작 씬부터 플레이", GUILayout.Height(32)))
                {
                    PlayFromStartScene();
                }

                GUI.backgroundColor = previousColor;

                GUILayout.Space(8);

                if (GUILayout.Button("목록 새로고침", GUILayout.Height(32), GUILayout.Width(110)))
                {
                    RefreshScenes();
                }
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("시작 씬", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var startScenePath = EditorPrefs.GetString(StartScenePrefKey, string.Empty);
                    EditorGUILayout.SelectableLabel(
                        string.IsNullOrEmpty(startScenePath) ? "<설정되지 않음>" : startScenePath,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight)
                    );

                    if (GUILayout.Button("현재 씬으로 설정", GUILayout.Width(120)))
                    {
                        SetCurrentSceneAsStart();
                    }
                }
            }
        }

        private void DrawDescription()
        {
            EditorGUILayout.HelpBox(
                "• 씬 목록에서 Open 버튼을 누르면 해당 씬을 바로 엽니다.\n" +
                "• '시작 씬으로 설정'한 씬은 플레이 시 항상 첫 씬으로 로드됩니다.\n" +
                "• '시작 씬부터 플레이'는 현재 씬을 저장한 뒤 시작 씬으로 이동해 Play 합니다.",
                MessageType.Info
            );
        }

        private void DrawSceneList()
        {
            EditorGUILayout.LabelField("씬 목록", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("검색", GUILayout.Width(40));
                var newSearch = EditorGUILayout.TextField(_sceneSearchText);
                if (newSearch != _sceneSearchText)
                {
                    _sceneSearchText = newSearch;
                    Repaint();
                }
            }
            EditorGUILayout.Space(6);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var scenePath in _scenePaths)
            {
                if (!string.IsNullOrEmpty(_sceneSearchText))
                {
                    var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    if (!sceneName.ToLowerInvariant().Contains(_sceneSearchText.ToLowerInvariant()))
                        continue;
                }

                DrawSceneEntry(scenePath);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusBar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    string.IsNullOrEmpty(_statusMessage) ? "대기 중…" : _statusMessage,
                    EditorStyles.wordWrappedLabel
                );
            }
        }

        private void DrawSceneEntry(string scenePath)
        {
            using (new EditorGUILayout.VerticalScope(
                new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(8, 8, 6, 6)
                }))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    var sceneNameStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 13,
                        normal = { textColor = new Color(0.85f, 0.95f, 1f) }
                    };
                    EditorGUILayout.LabelField(sceneName, sceneNameStyle);

                    GUILayout.FlexibleSpace();

                    var isFavorite = IsFavorite(scenePath);
                    GUI.color = isFavorite ? Color.yellow : Color.white;
                    if (GUILayout.Button(isFavorite ? "★" : "☆", GUILayout.Width(24)))
                    {
                        ToggleFavorite(scenePath);
                    }
                    GUI.color = Color.white;

                    GUILayout.Space(4);

                    if (GUILayout.Button("열기", GUILayout.Width(60)))
                    {
                        OpenScene(scenePath);
                        SetStatus($"씬 열기: {sceneName}");
                    }

                    if (GUILayout.Button("시작 씬 설정", GUILayout.Width(90)))
                    {
                        EditorPrefs.SetString(StartScenePrefKey, scenePath);
                        SetStatus($"시작 씬 설정됨: {sceneName}");
                    }
                }

                EditorGUILayout.LabelField(scenePath, EditorStyles.miniLabel);
            }
        }

        private static void OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        private void SetCurrentSceneAsStart()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorPrefs.SetString(StartScenePrefKey, scene.path);
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                SetStatus($"현재 씬을 시작 씬으로 설정: {sceneName}");
            }
        }

        private void PlayFromStartScene()
        {
            var startScenePath = EditorPrefs.GetString(StartScenePrefKey, string.Empty);
            if (string.IsNullOrEmpty(startScenePath))
            {
                EditorUtility.DisplayDialog("씬 브라우저", "시작 씬이 설정되지 않았습니다.", "확인");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SetStatus("시작 씬부터 플레이 시작");
                EditorSceneManager.OpenScene(startScenePath, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
        }

        private void SetStatus(string message)
        {
            _statusMessage = message;
            Repaint();
        }

        private HashSet<string> GetFavoriteScenes()
        {
            var raw = EditorPrefs.GetString(FavoriteScenesPrefKey, string.Empty);
            return new HashSet<string>(string.IsNullOrEmpty(raw) ? new string[0] : raw.Split('|'));
        }

        private bool IsFavorite(string scenePath)
        {
            return _favoriteScenes.Contains(scenePath);
        }

        private void ToggleFavorite(string scenePath)
        {
            if (_favoriteScenes.Contains(scenePath))
                _favoriteScenes.Remove(scenePath);
            else
                _favoriteScenes.Add(scenePath);

            EditorPrefs.SetString(
                FavoriteScenesPrefKey,
                string.Join("|", _favoriteScenes)
            );

            RefreshScenes();
        }
    }
}
