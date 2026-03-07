using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DragonGate
{
    /// <summary>
    /// 대화 UI를 담당하는 컴포넌트.
    /// Canvas 하위에 배치하고 Inspector에서 각 UI 요소를 연결.
    /// </summary>
    public class UIDialogue : PanelCore
    {
        // ── Inspector 연결 ──────────────────────────────────────────────

        [Header("대화 박스")]
        public GameObject     dialoguePanel;
        [SerializeField] private LocalizedTextMeshProUGUI _speakerNameText;
        [SerializeField] private LocalizedTextMeshProUGUI _dialogueBodyText;
        [SerializeField] private BetterButton         _nextButton;       // 클릭 → 다음 대화

        [Header("선택지 패널")]
        [SerializeField] private RecycledScrollView _choiceScrollView;

        [Header("노드 타입별 색상")]
        public Color colorNPC       = new Color(0.2f, 0.55f, 0.85f);
        public Color colorPlayer    = new Color(0.3f, 0.75f, 0.45f);
        public Color colorNarration = new Color(0.75f, 0.65f, 0.9f);
        public Image speakerNameBackground;

        [Header("설정")]
        public float textSpeed = 0.025f;   // 글자당 초
        public bool  autoSkipOnClick = true;

        // ── 내부 상태 ───────────────────────────────────────────────────

        private Action              onAdvance;
        private Action<ChoiceData>  onChoice;

        private Coroutine typewriterCo;
        private bool      isTyping;
        private string    fullText;
        private DialogueNode currentNode;
        private List<ChoiceData> _choices = new List<ChoiceData>();

        // ── Unity 생명주기 ──────────────────────────────────────────────

        private void Awake()
        {
            _nextButton?.onClick.AddListener(OnAdvanceClicked);
            _choiceScrollView.Init(GetChoiceCount, OnUpdateChoices, OnInitChoices);
        }

        /// <summary>DialogueRunner에서 호출. 노드 내용을 UI에 표시.</summary>
        public void DisplayNode(DialogueNode node,
                                Action<ChoiceData> choiceCb,
                                Action             advanceCb)
        {
            currentNode = node;
            onAdvance   = advanceCb;
            onChoice    = choiceCb;

            HideChoices();
            _nextButton.gameObject.SetActive(false);

            if (node.NodeType == DialogueNodeType.Narration)
            {
                // 내레이션은 화자 이름이 없을 수도 있음.
                if (node.NarrationSpeakerName == null || node.NarrationSpeakerName.IsEmpty)
                {
                    _speakerNameText.Clear();
                }
                else
                {
                    _speakerNameText.SetCopy(node.NarrationSpeakerName);
                }
            }
            else if (node.NodeType == DialogueNodeType.Character)
            {
                _speakerNameText.SetCopy(node.SpeakerCharacter.Name);
            }

            // 노드 타입별 색상
            if (speakerNameBackground != null)
                speakerNameBackground.color = GetNodeColor(node.NodeType);

            // 타이프라이터 시작
            if (typewriterCo != null)
            {
                StopCoroutine(typewriterCo);
            }
            fullText     = node.DialogueText.GetLocalizedString();
            typewriterCo = StartCoroutine(Typewriter(fullText, node));
        }

        // ── 타이프라이터 ─────────────────────────────────────────────────

        private IEnumerator Typewriter(string text, DialogueNode node)
        {
            isTyping             = true;
            _dialogueBodyText.text = "";

            foreach (char c in text)
            {
                _dialogueBodyText.text += c;
                yield return new WaitForSeconds(textSpeed);
            }

            isTyping = false;
            OnTextComplete(node);
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
            if (isTyping && autoSkipOnClick)
            {
                // 타이핑 스킵 → 전체 텍스트 즉시 표시
                if (typewriterCo != null) StopCoroutine(typewriterCo);
                isTyping              = false;
                _dialogueBodyText.text = fullText;
                OnTextComplete(currentNode);
                return;
            }
            onAdvance?.Invoke();
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
            DialogueNodeType.Character    => colorPlayer,
            DialogueNodeType.Narration => colorNarration,
            _                          => Color.gray,
        };
    }
}
