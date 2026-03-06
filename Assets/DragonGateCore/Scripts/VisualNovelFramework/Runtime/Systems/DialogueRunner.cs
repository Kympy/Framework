using System;
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
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _playerCharacter;
        [SerializeField] private SpriteRenderer _npcCharacter;
        [SerializeField] private AssetReference _dialogueUIPrefab;
        public SpriteRenderer Background => _background;

        // ── 이벤트 ──────────────────────────────────────────────────────
        public event Action<DialogueNode> OnNodeEnter;
        public event Action<DialogueNode> OnNodeExit;
        public event Action               OnDialogueEnd;
        public event Action<string>       OnChapterTransition; // arg: targetChapterId

        // ── 상태 ────────────────────────────────────────────────────────
        private DialogueGraph _currentGraph;
        private DialogueNode  _currentNode;
        public  bool          IsRunning { get; private set; }
        
        private EventExecutor _eventExecutor;
        private UIDialogue _uiDialogue;
        private CancellationTokenSource _tokenSource;

        protected override void Awake()
        {
            base.Awake();
            _eventExecutor = new EventExecutor(this);
        }

        private void Start()
        {
            StartDialogue(_startingDialogue);
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

            switch (node.nodeType)
            {
                case DialogueNodeType.Start:
                    // Start 노드는 표시 없이 바로 다음으로
                    AdvanceToNextNode();
                    break;

                case DialogueNodeType.ChapterEnd:
                    ExitNode(node);
                    IsRunning = false;
                    HideDialogueUI();
                    HideAllPortraits();
                    OnChapterTransition?.Invoke(node.TargetChapterId);
                    OnDialogueEnd?.Invoke();
                    break;

                default:
                    ShowDialogueUI();
                    ShowPortrait(node.nodeType, node.SpeakerPortrait);
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

        private void ShowDialogueUI()
        {
            if (_uiDialogue == null)
            {
                _uiDialogue = UIManager.Instance.ShowPopup<UIDialogue>(_dialogueUIPrefab.RuntimeKey.ToString());
            }
        }

        private void HideDialogueUI()
        {
            if (_uiDialogue == null) return;
            UIManager.Instance.HidePopup(_uiDialogue);
        }

        private void ShowPortrait(DialogueNodeType nodeType, AssetReference spriteRef)
        {
            var key = spriteRef.RuntimeKeyIsValid() ? spriteRef.RuntimeKey.ToString() : "";
            switch (nodeType)
            {
                case DialogueNodeType.Player:
                {
                    ShowPlayerPortrait(key);
                    break;
                }
                case DialogueNodeType.NPC:
                {
                    ShowNPCPortrait(key);
                    break;
                }
                default:
                {
                    _playerCharacter.sprite = null;
                    _npcCharacter.sprite = null;
                    break;
                }
            }
        }

        private void ShowPlayerPortrait(string key)
        {
            if (string.IsNullOrEmpty(key))
            {   
                _playerCharacter.sprite = null;
                return;
            }
            _playerCharacter.SetSprite(key);
        }

        private void ShowNPCPortrait(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                _npcCharacter.sprite = null;
                return;
            }
            _npcCharacter.SetSprite(key);
        }

        private void HideAllPortraits()
        {
            _playerCharacter.sprite = null;
            _npcCharacter.sprite = null;
        }
    }
}
