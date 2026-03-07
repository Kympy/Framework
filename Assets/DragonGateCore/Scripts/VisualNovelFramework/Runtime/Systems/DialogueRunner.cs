using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DragonGate;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    /// <summary>
    /// 대화 흐름을 관리하는 핵심 런타임 컴포넌트.
    /// DialogueGraph를 받아 노드를 순회하며 UI·이벤트를 제어한다.
    /// </summary>
    public class DialogueRunner : PlacedMonoBehaviourSingleton<DialogueRunner>, ICancelable
    {
        [Header("Start")]
        [SerializeField] private DialogueGraph _startingDialogue;
        [Header("References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private Light _mainLight;
        [Header("UI")]
        [SerializeField] private AssetReference _dialogueUIPrefab;
        public SpriteRenderer Background => _background;

        // ── 이벤트 ──────────────────────────────────────────────────────
        public event Action<DialogueNode> OnNodeEnter; // 노드 들어올 때
        public event Action<DialogueNode> OnNodeExit; // 노드 나갈 때
        public event Action               OnDialogueEnd; // 대화 종료 시 콜백

        // ── 컨디션 평가자 ────────────────────────────────────────────────
        /// <summary>
        /// 프레임워크 외부에서 IConditionEvaluator 구현체를 등록하세요.
        /// 등록하지 않으면 Condition 노드는 항상 false 로 평가됩니다.
        /// </summary>
        public IConditionEvaluator ConditionEvaluator { get; set; }

        // ── 상태 ────────────────────────────────────────────────────────
        private DialogueGraph _currentGraph;
        private DialogueNode  _currentNode;
        public  bool          IsRunning { get; private set; }
        
        private EventExecutor _eventExecutor;
        private DialogueCharacterManager _characterManager = new();
        private UIDialogue _uiDialogue;
        private CancellationTokenSource _tokenSource;

        protected override void Awake()
        {
            base.Awake();
            _eventExecutor = new EventExecutor(this);
            
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    _camera = GameUtil.CreateOrthographicCamera("Generated Camera");
                }
            }
            CameraManager.EnableCamera(_camera);
        }

        // ── 공개 API ────────────────────────────────────────────────────

        /// <summary>그래프의 startNode부터 대화 시작.</summary>
        public void StartDialogue(DialogueGraph graph)
        {
            if (graph == null) { Debug.LogError("[VNFramework] graph is null"); return; }
            _currentGraph = graph;
            var start = graph.GetStartNode();
            if (start == null) { Debug.LogError("[VNFramework] No start node in graph!"); return; }
            IsRunning = true;
            EnterNode(start);
        }

        /// <summary>특정 노드 ID부터 대화 시작.</summary>
        public void StartDialogue(DialogueGraph graph, string nodeId)
        {
            if (graph == null) return;
            _currentGraph = graph;
            var node = graph.GetNode(nodeId);
            if (node == null) { Debug.LogError($"[VNFramework] Node '{nodeId}' not found"); return; }
            IsRunning = true;
            EnterNode(node);
        }

        public void StopDialogue()
        {
            IsRunning = false;
            HideDialogueUI();
            _currentNode  = null;
            _currentGraph = null;
        }

        // ── 내부 노드 처리 ───────────────────────────────────────────────

        private void EnterNode(DialogueNode node)
        {
            _currentNode = node;
            OnNodeEnter?.Invoke(node);

            // Enter 이벤트 실행
            if (_eventExecutor != null && node.EnterEvents?.Count > 0)
                _eventExecutor.ExecuteEvents(node.EnterEvents).Forget();

            switch (node.NodeType)
            {
                case DialogueNodeType.Start:
                    // Start 노드는 표시 없이 바로 다음으로
                    AdvanceToNextNode();
                    break;

                case DialogueNodeType.ChapterEnd:
                    ExitNode(node);
                    IsRunning = false;
                    HideDialogueUI();
                    HideAllCharacter();
                    OnDialogueEnd?.Invoke();
                    OnChapterEnd(node.NextChapter);
                    break;

                case DialogueNodeType.Condition:
                    ExitNode(node);
                    if (ConditionEvaluator == null)
                        Debug.LogWarning("[VNFramework] Condition 노드에 도달했지만 IConditionEvaluator 가 등록되지 않았습니다. True 로 처리합니다.");
                    bool conditionResult = ConditionEvaluator?.Evaluate(node.Conditions) ?? true;
                    string nextNodeId = conditionResult ? node.TrueNodeId : node.FalseNodeId;
                    if (!string.IsNullOrEmpty(nextNodeId))
                    {
                        var nextNode = _currentGraph.GetNode(nextNodeId);
                        if (nextNode != null)
                        {
                            EnterNode(nextNode);
                            return;
                        }
                    }
                    else
                    {
                        DGDebug.LogError("Next Node is not exists on Condition Node.");
                    }
                    EndDialogue();
                    break;

                default:
                    ShowDialogueUI();
                    _uiDialogue?.DisplayNode(node, HandleChoiceSelected, HandleAdvance);
                    break;
            }
        }

        private void ExitNode(DialogueNode node)
        {
            OnNodeExit?.Invoke(node);
            if (_eventExecutor != null && node.ExitEvents?.Count > 0)
                _eventExecutor.ExecuteEvents(node.ExitEvents).Forget();
        }

        // ── UI 콜백 ─────────────────────────────────────────────────────

        private void HandleAdvance()
        {
            if (_currentNode == null) return;
            ExitNode(_currentNode);

            if (string.IsNullOrEmpty(_currentNode.NextNodeId))
            {
                EndDialogue();
                return;
            }

            var next = _currentGraph.GetNode(_currentNode.NextNodeId);
            if (next == null) { EndDialogue(); return; }
            EnterNode(next);
        }

        private void HandleChoiceSelected(ChoiceData choice)
        {
            if (choice == null) return;
            ExitNode(_currentNode);

            if (string.IsNullOrEmpty(choice.TargetNodeId))
            {
                EndDialogue();
                return;
            }

            var next = _currentGraph.GetNode(choice.TargetNodeId);
            if (next == null)
            {
                Debug.LogWarning($"[VNFramework] Choice target '{choice.TargetNodeId}' not found");
                EndDialogue();
                return;
            }

            EnterNode(next);
        }

        private void AdvanceToNextNode()
        {
            if (string.IsNullOrEmpty(_currentNode.NextNodeId)) { EndDialogue(); return; }
            var next = _currentGraph.GetNode(_currentNode.NextNodeId);
            if (next != null) EnterNode(next);
            else EndDialogue();
        }

        private void EndDialogue()
        {
            IsRunning = false;
            HideDialogueUI();
            OnDialogueEnd?.Invoke();
        }

        private void OnChapterEnd(AssetReference nextChapterReference)
        {
            
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
        }

        public void HideAllCharacter()
        {
            _characterManager.HideAllCharacter();
        }

        public void HideCharacter(int id)
        {
            _characterManager.HideCharacter(id);
        }

        public void ShowCharacter(DialogueCharacterAsset asset, Vector2 position, float scale = 1f)
        {
            if (asset.IsValidCharacterAsset == false)
            {
                return;
            }

            if (scale <= 0f)
            {
                DGDebug.Log("Character Scale is less or equal to zero!!", Color.red);
            }
            _characterManager.ShowCharacter(asset, position, scale);
        }

        public void PlayCharacterAnimation(int characterId, string triggerName)
        {
            _characterManager.PlayAnimation(characterId, triggerName);
        }

        public void SetBackground(string key)
        {
            _background.SetSprite(key);
            CameraManager.CurrentCamera.FitCameraToSpriteRenderer(_background);
        }

        private DialogueCharacter GetCharacter(AssetReference reference)
        {
            var character = PoolManager.Instance.GetComponent<DialogueCharacter>(reference.RuntimeKey.ToString());
            return character;
        }
    }
}
