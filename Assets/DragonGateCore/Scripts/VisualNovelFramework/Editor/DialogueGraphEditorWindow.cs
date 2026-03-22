// ============================================================
//  Visual Novel Framework – Dialogue Graph Editor Window
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow : EditorWindow
    {
        // ── 상태 ──────────────────────────────────────────────────────
        private DialogueGraph _graph;
        private string _selectedNodeId;
        private int _selectedLocaleIdx;
        private string[] _localeNames;
        private DialogueNode _copiedNode;
        private DialogueEvent _copiedEvent;
        private List<List<DialogueNode>> _availablePaths = new();
        private List<Dictionary<string, int>> _pathSelectedChoices = new();
        private int _currentPathIndex = 0;
        private SerializedObject _graphSO;
        private float _savedNotifyTime = 3f;
        private float _notifyElapsedTime = 0f;
        private DialogueNode SelectedNode => _graph?.Nodes.Find(n => n.nodeId == _selectedNodeId);

        // 설정
        private SerializedObject _graphSettingsSO;
        private const string GRAPH_SETTINGS_RESOURCE_PATH = "Assets/DragonGateCore/VisualNovelFramework/Resources/DialogueGraphSettings.asset";
        
        // 작업 씬
        private const string WORK_SCENE_PATH = "Assets/Scenes/DialoguePreview.unity";

        // ── 메뉴 진입점 ───────────────────────────────────────────────

        [MenuItem("DragonGate/Open Visual Novel Dialogue Graph Editor")]
        public static DialogueGraphEditorWindow OpenWindow()
        {
            var w = GetWindow<DialogueGraphEditorWindow>("Visual Novel Graph Editor");
            w.minSize = new Vector2(900, 600);
            return w;
        }

        public static void OpenGraph(DialogueGraph graph)
        {
            var w = OpenWindow();
            w.LoadGraph(graph);
        }

        private static void OpenDialoguePreviewScene(DialogueGraph graph)
        {
            if (File.Exists(WORK_SCENE_PATH))
            {
                if (EditorSceneManager.GetActiveScene().path != WORK_SCENE_PATH)
                    EditorSceneManager.OpenScene(WORK_SCENE_PATH);
                    
                EnsureStarter();
            }
            else
            {
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                EnsureStarter();
                
                string sceneDir = Path.GetDirectoryName(WORK_SCENE_PATH);
                if (!Directory.Exists(sceneDir))
                    Directory.CreateDirectory(sceneDir);
                EditorSceneManager.SaveScene(newScene, WORK_SCENE_PATH);
            }

            void EnsureStarter()
            {
                if (FindAnyObjectByType<DialoguePreviewStarter>() == null)
                    new GameObject("Starter").AddComponent<DialoguePreviewStarter>();
            }
            
            EditorApplication.isPlaying = true;
        }

        private async UniTask Preview(DialogueGraph graph, DialogueNode node)
        {
            if (DialogueRunner.HasInstance == false) return;
            if (UniTaskLock.IsLocked(graph))
            {
                DGDebug.Log("이중 클릭 방지 : 경로를 계산 중입니다..");
                return;
            }
            using var _ = new UniTaskLock(graph);
            _availablePaths.Clear();
            _pathSelectedChoices.Clear();
            _currentPathIndex = 0;
            graph.GetAllPaths(node, _availablePaths, _pathSelectedChoices);
            if (_availablePaths.Count == 0)
            {
                DGDebug.Log("No Available Paths Found!", Color.orange);
                return;
            }
            await DialogueRunner.Instance.RestoreFromPath(graph, _availablePaths[0], _pathSelectedChoices[0]);
            Repaint();
        }

        private async UniTask ChangePath(int pathIndex)
        {
            if (DialogueRunner.HasInstance == false) return;
            if (pathIndex < 0 || pathIndex > _availablePaths.Count - 1)
            {
                DGDebug.LogError($"Path Index 가 범위를 벗어남. {pathIndex}");
                return;
            }
            if (UniTaskLock.IsLocked(_graph))
            {
                DGDebug.Log("이중 클릭 방지 : 경로를 변경 중입니다..");
                return;
            }
            using var _ = new UniTaskLock(_graph);
            await DialogueRunner.Instance.RestoreFromPath(_graph, _availablePaths[pathIndex], _pathSelectedChoices[pathIndex]);
            Repaint();
        }

        private void OnEnable()
        {
            connectFromNode = null; // 유령 커넥션 방지
            _stylesReady = false;

            if (_graph != null)
                _graphSO = new SerializedObject(_graph);

            // 로컬라이제이션 시스템 초기화 먼저 완료
            // var initOp = LocalizationSettings.InitializationOperation;
            // if (!initOp.IsDone)
            //     initOp.WaitForCompletion();
            RefreshLocales();
        }

        // ══════════════════════════════════════════════════════════════
        //  OnGUI
        // ══════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            EnsureStyles();

            // graphSO가 무효화된 경우 재생성
            if (_graph != null && (_graphSO == null || _graphSO.targetObject == null))
                _graphSO = new SerializedObject(_graph);


            var canvasRect = new Rect(0, TOOLBAR_H, position.width - _inspectorWidth - SPLITTER_W, position.height - TOOLBAR_H);
            var splitterRect = new Rect(position.width - _inspectorWidth - SPLITTER_W, TOOLBAR_H, SPLITTER_W, position.height - TOOLBAR_H);
            var inspectorRect = new Rect(position.width - _inspectorWidth, TOOLBAR_H, _inspectorWidth, position.height - TOOLBAR_H);

            DrawToolbar();
            DrawCanvas(canvasRect);
            DrawInspectorPanel(inspectorRect);
            HandleSplitter(splitterRect);
            HandleEvents(Event.current, canvasRect);

            if (_notifyElapsedTime > 0f)
            {
                var notifyRect = new Rect((canvasRect.xMin + canvasRect.xMax) * 0.5f, (canvasRect.yMax - 60f), 400f, 40f);
                DrawNotifyPopup(notifyRect, "저장되었습니다.");
                _notifyElapsedTime -= Time.deltaTime;
            }

            if (GUI.changed) Repaint();
        }
    }
}
#endif
