using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DragonGate
{
    /// <summary>
    /// 대화 UI를 담당하는 컴포넌트.
    /// Canvas 하위에 배치하고 Inspector에서 각 UI 요소를 연결.
    /// </summary>
    public class UIDialogue : PanelCore, IInputHandler
    {
        // ── Inspector 연결 ──────────────────────────────────────────────
        [SerializeField] private CanvasGroup _dialogueCanvasGroup;
        [Header("화자")]
        [SerializeField] private Image _speakerNameBackground;
        [SerializeField] private LocalizedTextMeshProUGUI _speakerNameText;
        [SerializeField] private bool _useSpeakerNameBackgroundColor = false;
        
        [Header("대화 박스")]
        [SerializeField] private LocalizedTextMeshProUGUI _dialogueBodyText;
        
        [Header("다음")]
        [SerializeField] private BetterButton         _nextButton;       // 클릭 → 다음 대화

        [Header("선택지 패널")]
        [SerializeField] private RecycledScrollView _choiceScrollView;

        [Header("노드 타입별 색상")]
        [SerializeField] private Color _colorCharacter    = new Color(0.3f, 0.75f, 0.45f);
        [SerializeField] private Color _colorNarration = new Color(0.75f, 0.65f, 0.9f);

        [Header("설정")]
        public bool  CanSkipOnClick = true;

        public bool InputEnabled => gameObject.activeSelf;
        
        // ── 내부 상태 ───────────────────────────────────────────────────
        private Action              onAdvance;
        private Action<ChoiceData>  onChoice;
        
        private bool      _isTyping;
        private string    _fullText;
        private float _typingProgress = 0;
        private DialogueNode _currentNode;
        private List<ChoiceData> _choices = new List<ChoiceData>();

        private bool _isDialogueShowing = false;
        private Tweener _dialogueShowTween;
        private Tweener _dialogueHideTween;

        private CancellationTokenSource _textTokenSource;

        // ── Unity 생명주기 ──────────────────────────────────────────────

        private void Awake()
        {
            _nextButton?.OnLeftUp.AddListener(OnAdvanceClicked);
            _choiceScrollView.Init(GetChoiceCount, OnUpdateChoices, OnInitChoices);

            _dialogueHideTween = _dialogueCanvasGroup.DOFade(0, 0.5f);
            _dialogueShowTween = _dialogueCanvasGroup.DOFade(1, 0.5f);
            _dialogueHideTween.SetAutoKill(false);
            _dialogueShowTween.SetAutoKill(false);
            _dialogueHideTween.Pause();
            _dialogueShowTween.Pause();
            
            _dialogueCanvasGroup.alpha = 0;
            _nextButton.SetActive(false);
            _speakerNameBackground.SetActive(false);
            _isDialogueShowing = false;
        }

        public override void SetVisible(UnityAction onVisible = null)
        {
            base.SetVisible(onVisible);
            InputManager.AddInput(this);
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            InputManager.RemoveInput(this);
        }

        /// <summary>DialogueRunner에서 호출. 노드 내용을 UI에 표시.</summary>
        public void DisplayNode(DialogueNode node,
                                Action<ChoiceData> choiceCb,
                                Action             advanceCb)
        {
            _currentNode = node;
            onAdvance   = advanceCb;
            onChoice    = choiceCb;

            HideChoices();
            _nextButton.gameObject.SetActive(false);
            
            // 노드 타입별 색상
            if (_useSpeakerNameBackgroundColor && _speakerNameBackground != null)
                _speakerNameBackground.color = GetNodeColor(node.NodeType);

            if (node.NodeType == DialogueNodeType.Character || node.NodeType == DialogueNodeType.Narration)
            {
                // 화자 이름이 없을 수도 있음.
                if (node.SpeakerName == null || node.SpeakerName.IsEmpty)
                {
                    _speakerNameText.Clear();
                    _speakerNameBackground?.SetActive(false);
                }
                else
                {
                    _speakerNameText.SetCopy(node.SpeakerName);
                    _speakerNameBackground?.SetActive(true);
                }
                // 대화 텍스트 속성 설정
                _dialogueBodyText.fontSize = node.TextSize;
                _dialogueBodyText.color = node.TextColor;
                
                // 배경 보이기
                ShowDialogue();
                
                // 타이프라이터 시작
                CancelTypeWriter();
                _fullText     = node.DialogueText.GetLocalizedString();
                Typewriter(_fullText, node).Forget();
            }
            else
            {
                _speakerNameBackground?.SetActive(false);
                _nextButton.gameObject.SetActive(true);
                HideDialogue();
            }
        }

        public void ShakeText(Vector2 strength, float duration)
        {
            _dialogueBodyText.transform.DOShakePosition(duration, strength);
        }

        public void ShowDialogue()
        {
            if (_isDialogueShowing == false)
            {
                _dialogueHideTween.Pause();
                _dialogueShowTween.Restart();
                _isDialogueShowing = true;
            }
        }

        public void HideDialogue()
        {
            if (_isDialogueShowing)
            {
                _dialogueShowTween.Pause();
                _dialogueHideTween.Restart();
                _isDialogueShowing = false;
            }
        }

        // ── 타이프라이터 ─────────────────────────────────────────────────

        private async UniTask Typewriter(string text, DialogueNode node)
        {
            _isTyping             = true;
            _dialogueBodyText.Clear();
            _typingProgress = 0;
            EnsureTextTokenSource();

            int i = 0;
            int length = text.Length;
            foreach (char c in text)
            {
                i++;
                _dialogueBodyText.text += c;
                _typingProgress = i / (float)length;
                await UniTask.WaitForSeconds(node.TextSpeed, cancellationToken: _textTokenSource.Token);
            }
            
            _isTyping = false;
            OnTextComplete(node);
        }

        private void EnsureTextTokenSource()
        {
            if (_textTokenSource == null ||  _textTokenSource.IsCancellationRequested)
            {
                _textTokenSource = UniTaskHelper.CreateObjectToken(this);
            }
        }

        private void CancelTypeWriter()
        {
            _textTokenSource?.Cancel();
            _textTokenSource?.Dispose();
            _textTokenSource = null;
            _typingProgress = 0;
            _isTyping = false;
        }

        private void OnTextComplete(DialogueNode node)
        {
            bool hasChoices = node.Choices != null && node.Choices.Count > 0;
            if (hasChoices)
                BuildChoices(node.Choices);
            else
                _nextButton.gameObject.SetActive(true);
        }

        // ── 선택지 ──────────────────────────────────────────────────────

        private void BuildChoices(List<ChoiceData> choices)
        {
            _choiceScrollView.SetActive(true);
            _choices.Clear();
            _choices.AddRange(choices);
            _choiceScrollView.Refresh();
        }

        private int GetChoiceCount()
        {
            return _choices.Count;
        }

        private void OnInitChoices(RecycledScrollViewElement element)
        {
            if (element is not UIChoiceElement uiChoiceElement) return;
            uiChoiceElement.OnChoiceClicked -= OnChoiceClicked;
            uiChoiceElement.OnChoiceClicked += OnChoiceClicked;
        }

        private void OnUpdateChoices(RecycledScrollViewElement element)
        {
            if (element is not UIChoiceElement uiChoiceElement) return;
            uiChoiceElement.SetData(_choices[element.Index]);
        }

        private void HideChoices()
        {
            _choiceScrollView?.SetActive(false);
        }

        // ── 버튼 콜백 ───────────────────────────────────────────────────

        private void OnAdvanceClicked()
        {
            _nextButton.gameObject.SetActive(false);
            _dialogueBodyText.Clear();
            onAdvance?.Invoke();
        }

        private void OnSkipClicked()
        {
            if (_isTyping == false || CanSkipOnClick == false) return;
            if (_typingProgress < 0.1f) return;
            // 타이핑 스킵 → 전체 텍스트 즉시 표시
            CancelTypeWriter();
            _dialogueBodyText.SetText(_fullText);
            OnTextComplete(_currentNode);
        }

        private void OnChoiceClicked(ChoiceData choice)
        {
            HideChoices();
            onChoice?.Invoke(choice);
        }

        private void OnChoiceClicked(int index)
        {
            HideChoices();
            var choiceData = _choices[index];
            onChoice?.Invoke(choiceData);
        }

        // ── 유틸 ────────────────────────────────────────────────────────

        private Color GetNodeColor(DialogueNodeType t) => t switch
        {
            DialogueNodeType.Character    => _colorCharacter,
            DialogueNodeType.Narration => _colorNarration,
            _                          => Color.gray,
        };

        public EInputResult UpdateInput(float deltaTime)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnSkipClicked();
                return EInputResult.Break;
            }
            return EInputResult.Continue;
        }
    }
}
