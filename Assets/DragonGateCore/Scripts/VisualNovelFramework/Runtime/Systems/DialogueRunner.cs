using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    /// <summary>
    /// 대화 흐름을 관리하는 핵심 런타임 컴포넌트.
    /// DialogueGraph를 받아 노드를 순회하며 UI·이벤트를 제어한다.
    /// </summary>
    public class DialogueRunner : PlacedMonoBehaviourSingleton<DialogueRunner>
    {
        [Header("References")]
        [SerializeField] private Camera _camera;

        [SerializeField] private DialogueBackground _background;
        [SerializeField] private Light _mainLight;

        [Header("UI")]
        [SerializeField] private AssetReference _dialogueUIPrefab;

        // ── 이벤트 ──────────────────────────────────────────────────────
        public event Action<DialogueNode> OnNodeEnter; // 노드 들어올 때
        public event Action<DialogueNode> OnNodeExit; // 노드 나갈 때

        // ── 컨디션 평가자 ────────────────────────────────────────────────
        /// <summary>
        /// 프레임워크 외부에서 IConditionEvaluator 구현체를 등록하세요.
        /// 등록하지 않으면 Condition 노드는 항상 false 로 평가됩니다.
        /// </summary>
        public IConditionEvaluator ConditionEvaluator { get; set; }

        public AssetReferenceT<DialogueGraph> CurrentGraphReference => _currentGraphReference;
        public string CurrentNodeId => _currentNode?.nodeId;
        public List<DialogueNode> CurrentPath => _currentPath;
        public Dictionary<string, int> CurrentChoices => _currentChoices;

        // ── 상태 ────────────────────────────────────────────────────────
        private AssetReferenceT<DialogueGraph> _currentGraphReference;
        private DialogueGraph _currentGraph;
        private DialogueNode _currentNode;
        private string _loadTargetNodeId;
        private string _currentBackgroundKey;
        private List<DialogueNode> _currentPath = new();
        private Dictionary<string, int> _currentChoices = new();
        public bool IsRunning { get; private set; }

        private EventExecutor _eventExecutor;
        private DialogueCharacterManager _characterManager;
        private UIDialogue _uiDialogue;
        private CancellationTokenSource _tokenSource;

        protected override void Awake()
        {
            base.Awake();
            _eventExecutor = new EventExecutor(this);
            _characterManager = new DialogueCharacterManager(this);

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    _camera = GameUtil.CreateOrthographicCamera("Generated Camera");
                }
            }

            CameraManager.EnableCamera(_camera);
            transform.position = Vector3.zero;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            HideDialogueUI();
            if (_currentGraph != null)
            {
                AssetManager.Instance.ReleaseAsset(_currentGraph);
                _currentGraph = null;
            }
        }

        // ── 공개 API ────────────────────────────────────────────────────

        public async UniTask PreLoad()
        {
            var graph = await AssetManager.Instance.GetAssetAsync<DialogueGraph>(_currentGraphReference.RuntimeKey.ToString());
            _currentGraph = graph;
            _currentNode = _loadTargetNodeId == null ? graph.GetStartNode() : graph.GetNode(_loadTargetNodeId);
        }

        public void SetPreLoadTarget(AssetReferenceT<DialogueGraph> graphReference, string nodeId)
        {
            _currentGraphReference = graphReference;
            _loadTargetNodeId = nodeId;
        }

        // 로드된 걸 바로 실행
        public void StartGraph(DialogueSnapshotData snapshot)
        {
            StartGraph(_currentGraph, _currentNode.nodeId);
            RestoreSnapshot(snapshot);
        }

        public void StartGraph(AssetReferenceT<DialogueGraph> graphReference, string nodeId)
        {
            var graph = AssetManager.Instance.GetAsset<DialogueGraph>(graphReference.RuntimeKey.ToString());
            _currentGraphReference = graphReference;
            StartGraph(graph, nodeId);
        }

        /// <summary>특정 노드 ID부터 대화 시작.</summary>
        public void StartGraph(DialogueGraph graph, string nodeId)
        {
            if (graph == null) return;
            _currentGraph = graph;

            DialogueNode targetNode = null;
            targetNode = nodeId == null ? graph.GetStartNode() : graph.GetNode(nodeId);
            if (targetNode == null)
            {
                Debug.LogError($"[VNFramework] Node '{nodeId}' not found");
                return;
            }
            CancelToken();
            ShowDialogueUI();
            DGDebug.Log($"Start Dialogue. {graph.name}", Color.gold);
            IsRunning = true;
            EnterNode(targetNode).Forget();
        }

        public async UniTask RestoreFromPath(DialogueGraph graph, List<DialogueNode> path, Dictionary<string, int> choices)
        {
            // 노드 경로 복사
            _currentPath.Clear();
            for (int i = 0; i < path.Count; i++)
            {
                var node = path[i];
                _currentPath.Add(node);
            }
            // 선택지 경로 복사
            _currentChoices.Clear();
            foreach (var choice in choices)
            {
                _currentChoices.Add(choice.Key, choice.Value);
            }
            
            await RestorePassedEvents(path);
            StartGraph(graph, path[^1].nodeId);
        }

        private void EndGraph()
        {
            DGDebug.Log($"End Chapter", Color.gold);
            IsRunning = false;
        }

        public void CancelGraph()
        {
            DGDebug.Log($"Cancel Chapter", Color.gold);
            CancelToken();
            IsRunning = false;
            _currentNode = null;
            _currentGraph = null;
        }

        private void OnGraphEnd(AssetReferenceT<DialogueGraph> nextChapterReference)
        {
            _uiDialogue?.HideDialogue();
            
            if (nextChapterReference == null || nextChapterReference.RuntimeKeyIsValid() == false)
            {
                DGDebug.Log("Next Chapter Reference Null", Color.crimson);
                return;
            }

            // 여기서 이제 추후에 필요한 챕터 사이간의 로딩이나 delay 연출을 보여줄 수 있음.
            if (_currentGraph != null)
            {
                AssetManager.Instance.ReleaseAsset(_currentGraph);
                _currentGraph = null;
            }

            StartGraph(nextChapterReference, null);
        }

        // ── 내부 노드 처리 ───────────────────────────────────────────────

        private async UniTask EnterNode(DialogueNode node)
        {
            _currentNode = node;
            OnNodeEnter?.Invoke(node);
            DGDebug.Log($"Enter Node : {node.nodeId}", Color.darkSalmon);
            
            // 이벤트 실행 전 특수처리
            if (node.NodeType == DialogueNodeType.Start)
                OnStartInternalEvent();

            // Enter 이벤트 실행
            GetTokenSource();
            if (_eventExecutor != null && node.EnterEvents?.Count > 0)
                await _eventExecutor.ExecuteEvents(node.EnterEvents);
            if (IsValidCancelToken() == false)
            {
                DGDebug.Log("Enter Node Canceled.", Color.orangeRed);
                return;
            }

            switch (node.NodeType)
            {
                case DialogueNodeType.Start:
                    // Start 노드는 표시 없이 바로 다음으로
                    AdvanceToNextNode();
                    break;

                case DialogueNodeType.ChapterEnd:
                    await ExitNode(node);
                    IsRunning = false;
                    HideAllCharacter();
                    EndGraph();
                    OnGraphEnd(node.NextChapter);
                    break;

                case DialogueNodeType.Condition:
                    await ExitNode(node);
                    if (ConditionEvaluator == null)
                        Debug.LogWarning("[VNFramework] Condition 노드에 도달했지만 IConditionEvaluator 가 등록되지 않았습니다. True 로 처리합니다.");
                    bool conditionResult = ConditionEvaluator?.Evaluate(node.Conditions) ?? true;
                    string nextNodeId = conditionResult ? node.TrueNodeId : node.FalseNodeId;
                    if (!string.IsNullOrEmpty(nextNodeId))
                    {
                        var nextNode = _currentGraph.GetNode(nextNodeId);
                        if (nextNode != null)
                        {
                            EnterNode(nextNode).Forget();
                            break;
                        }
                    }
                    else
                    {
                        DGDebug.LogError("Next Node is not exists on Condition Node.");
                    }

                    EndGraph();
                    break;

                default:
                    _uiDialogue?.DisplayNode(node,
                        choice => HandleChoiceSelected(choice).Forget(),
                        () => HandleAdvance().Forget());
                    break;
            }
        }
        // 지나온 경로의 이벤트를 일부 복구함.
        private async UniTask RestorePassedEvents(List<DialogueNode> nodePath)
        {
            // 마지막노드(현재 노드)는 이벤트 호출할 필요가 없음.
            for (int i = 0; i < nodePath.Count - 1; i++)
            {
                var node = nodePath[i];
                if (node.NodeType == DialogueNodeType.Start)
                    OnStartInternalEvent();

                for (int j = 0; j < node.EnterEvents?.Count; j++)
                {
                    var enterEvent = node.EnterEvents[j];
                    await InvokeEvent(enterEvent);
                }
                for (int j = 0; j < node.ExitEvents?.Count; j++)
                {
                    var exitEvent = node.ExitEvents[j];
                    await InvokeEvent(exitEvent);
                }
            }
            // 복구가 필요한 이벤트(이후 노드에 누적으로 영향 가능성이 있는 이벤트)만 복구.
            async UniTask InvokeEvent(DialogueEvent e)
            {
                switch (e.eventType)
                {
                    default:
                    {
                        return;
                    }
                    case DialogueEventType.BgmVolume:
                    case DialogueEventType.FadeIn:
                    case DialogueEventType.FadeOut:
                    case DialogueEventType.ColorCharacter:
                    case DialogueEventType.HideCharacter:
                    case DialogueEventType.MoveCharacter:
                    case DialogueEventType.ShowCharacter:
                    case DialogueEventType.PlayAnimation:
                    case DialogueEventType.SetBackground:
                    case DialogueEventType.HideAllCharacter:
                    case DialogueEventType.HideUI:
                    case DialogueEventType.ShowUI:
                    case DialogueEventType.PlayBGM:
                    case DialogueEventType.StopBGM:
                    {
                        await _eventExecutor.CompleteEvent(e);
                        break;
                    }
                }
            }
        }

        private void OnStartInternalEvent()
        {
            HideAllCharacter();
        }

        private async UniTask ExitNode(DialogueNode node)
        {
            DGDebug.Log($"Exit Node : {node.nodeId}", Color.darkSalmon);
            OnNodeExit?.Invoke(node);
            if (_eventExecutor != null && node.ExitEvents?.Count > 0)
                await _eventExecutor.ExecuteEvents(node.ExitEvents);
        }

        // ── UI 콜백 ─────────────────────────────────────────────────────

        private async UniTask HandleAdvance()
        {
            if (_currentNode == null) return;
            await ExitNode(_currentNode);

            if (string.IsNullOrEmpty(_currentNode.NextNodeId))
            {
                EndGraph();
                return;
            }

            var next = _currentGraph.GetNode(_currentNode.NextNodeId);
            if (next == null)
            {
                EndGraph();
                return;
            }

            await EnterNode(next);
        }

        private async UniTask HandleChoiceSelected(ChoiceData choice)
        {
            if (choice == null) return;
            await ExitNode(_currentNode);

            if (string.IsNullOrEmpty(choice.TargetNodeId))
            {
                EndGraph();
                return;
            }

            var next = _currentGraph.GetNode(choice.TargetNodeId);
            if (next == null)
            {
                Debug.LogWarning($"[VNFramework] Choice target '{choice.TargetNodeId}' not found");
                EndGraph();
                return;
            }

            await EnterNode(next);
        }

        private void AdvanceToNextNode()
        {
            if (string.IsNullOrEmpty(_currentNode.NextNodeId))
            {
                EndGraph();
                return;
            }

            var next = _currentGraph.GetNode(_currentNode.NextNodeId);
            if (next != null)
            {
                EnterNode(next).Forget();
            }
            else
            {
                EndGraph();
            }
        }

        private void CreateUIDialogue()
        {
            if (_uiDialogue == null)
            {
                _uiDialogue = UIManager.Instance.ShowPanel<UIDialogue>(_dialogueUIPrefab.RuntimeKey.ToString());
            }
        }

        private void ShowDialogueUI()
        {
            if (_uiDialogue == null)
            {
                _uiDialogue = UIManager.Instance.ShowPanel<UIDialogue>(_dialogueUIPrefab.RuntimeKey.ToString());
                return;
            }
            UIManager.Instance.Show(_uiDialogue);
        }

        private void HideDialogueUI()
        {
            if (_uiDialogue == null) return;
            UIManager.Instance.HidePanel(_uiDialogue);
            _uiDialogue = null;
        }

        public void HideAllCharacter()
        {
            _characterManager.HideAllCharacter();
        }

        public void HideCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef)
        {
            _characterManager.HideCharacter(assetRef);
        }

        public void ShowCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 position, float scale = 1f)
        {
            if (assetRef == null || assetRef.RuntimeKeyIsValid() == false)
            {
                return;
            }

            if (scale <= 0f)
            {
                DGDebug.Log("Character Scale is less or equal to zero!!", Color.red);
            }

            _characterManager.ShowCharacter(assetRef, position, scale);
        }

        public async UniTask ShowFadeCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 position, float duration, float scale = 1f)
        {
            if (assetRef == null || assetRef.RuntimeKeyIsValid() == false)
            {
                return;
            }

            if (scale <= 0f)
            {
                DGDebug.Log("Character Scale is less or equal to zero!!", Color.red);
            }
            _characterManager.ShowCharacter(assetRef, position, scale);
            await _characterManager.ToTransparent(assetRef, duration);
        }

        public async UniTask HideFadeCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, float duration)
        {
            await _characterManager.ToVisible(assetRef, duration);
            _characterManager.HideCharacter(assetRef);
        }

        public async UniTask MoveCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 position, Ease ease, float duration, float scale = 1f)
        {
            if (assetRef == null || assetRef.RuntimeKeyIsValid() == false) return;
            if (ease == Ease.Unset)
                _characterManager.TeleportCharacter(assetRef, position);
            else
                await _characterManager.MoveCharacter(assetRef, position, ease, duration);
        }

        public async UniTask ColorCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Color start, Color end, float duration)
        {
            if (assetRef == null || assetRef.RuntimeKeyIsValid() == false) return;
            await _characterManager.ColorFade(assetRef, start, end, duration);
        }

        public void PlayCharacterAnimation(AssetReferenceT<DialogueCharacterAsset> assetRef, string triggerName)
        {
            _characterManager.PlayAnimation(assetRef, triggerName);
        }

        public async UniTask SetBackground(string key, float duration = -1)
        {
            _currentBackgroundKey = key;
            if (duration >= 0f)
                await _background.SwitchBackground(key, duration);
            else
                _background.SetBackground(key);
        }

        public DialogueSnapshotData CaptureSnapshot()
        {
            return new DialogueSnapshotData
            {
                BackgroundSpriteKey = _currentBackgroundKey,
                BgmKey = SoundManager.Instance.CurrentBgmKey,
                BgmVolume = SoundManager.Instance.GetBgmGroupVolume(),
                Characters = _characterManager.GetSnapshots(),
            };
        }

        public void RestoreSnapshot(DialogueSnapshotData snapshot)
        {
            if (!string.IsNullOrEmpty(snapshot.BackgroundSpriteKey))
                SetBackground(snapshot.BackgroundSpriteKey);

            if (!string.IsNullOrEmpty(snapshot.BgmKey))
                SoundManager.Instance.PlayBGM(snapshot.BgmKey, snapshot.BgmVolume).Forget();

            if (snapshot.Characters != null)
            {
                foreach (var c in snapshot.Characters)
                    ShowCharacter(c.CharacterAsset, c.Position, c.Scale);
            }
        }

        public void ShakeCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 strength, float duration)
        {
            _characterManager.ShakeCharacter(assetRef, strength, duration);
        }

        public void ShakeText(Vector2 strength, float duration)
        {
            if (_uiDialogue == null)
            {
                DGDebug.LogError("UI Dialogue is not created.");
                return;
                // ShowDialogueUI();
            }

            _uiDialogue.ShakeText(strength, duration);
        }
    }
}
