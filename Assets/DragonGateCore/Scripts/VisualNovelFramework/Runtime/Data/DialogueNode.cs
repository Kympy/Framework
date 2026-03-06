using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

namespace DragonGate
{
    [Serializable]
    public class DialogueNode
    {
        // ── 식별 ────────────────────────────────
        public string         nodeId;
        public DialogueNodeType nodeType;

        // ── 대화 내용 ───────────────────────────
        public LocalizedString SpeakerName;
        public LocalizedString DialogueText;
        public AssetReference SpeakerPortrait;

        // ── 분기 ────────────────────────────────
        /// <summary>선택지가 있을 때 사용. 비어있으면 nextNodeId로 자동 진행.</summary>
        public List<ChoiceData> Choices = new List<ChoiceData>();

        /// <summary>선택지가 없을 때 자동으로 이동할 다음 노드.</summary>
        public string NextNodeId;

        // ── 챕터 이동 (ChapterEnd 전용) ─────────
        public string TargetChapterId;

        // ── 이벤트 ──────────────────────────────
        /// <summary>이 노드에 진입할 때 실행할 이벤트 목록.</summary>
        public List<DialogueEvent> EnterEvents = new List<DialogueEvent>();

        /// <summary>이 노드를 빠져나갈 때 실행할 이벤트 목록.</summary>
        public List<DialogueEvent> ExitEvents  = new List<DialogueEvent>();

        // ── 에디터 전용 ─────────────────────────
        public Vector2 editorPosition;

        // 에디터 표시용 헬퍼
        public string NodeTitle =>
            nodeType == DialogueNodeType.Start      ? "START"       :
            nodeType == DialogueNodeType.ChapterEnd ? "CHAPTER END" :
            (SpeakerName == null || SpeakerName.IsEmpty)       ? nodeType.ToString() :
            SpeakerName.TableEntryReference.ToString();
    }
}
