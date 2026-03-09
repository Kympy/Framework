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
        public DialogueNodeType NodeType;
        
        // 화자 이름
        public LocalizedString SpeakerName;
        // ── 대화 내용 ───────────────────────────
        public LocalizedString DialogueText;
        public float TextSpeed = 0.1f;

        // ── 분기 ────────────────────────────────
        /// <summary>선택지가 있을 때 사용. 비어있으면 nextNodeId로 자동 진행.</summary>
        public List<ChoiceData> Choices = new List<ChoiceData>();

        /// <summary>선택지가 없을 때 자동으로 이동할 다음 노드.</summary>
        public string NextNodeId;

        // ── 챕터 이동 (ChapterEnd 전용) ─────────
        public AssetReferenceT<DialogueGraph> NextChapter;

        // ── 컨디션 (Condition 전용) ──────────────
        public List<DialogueCondition> Conditions = new List<DialogueCondition>();
        /// <summary>AND 조건이 true 일 때 이동할 노드 ID.</summary>
        public string TrueNodeId;
        /// <summary>AND 조건이 false 일 때 이동할 노드 ID.</summary>
        public string FalseNodeId;

        // ── 이벤트 ──────────────────────────────
        /// <summary>이 노드에 진입할 때 실행할 이벤트 목록.</summary>
        public List<DialogueEvent> EnterEvents = new List<DialogueEvent>();

        /// <summary>이 노드를 빠져나갈 때 실행할 이벤트 목록.</summary>
        public List<DialogueEvent> ExitEvents  = new List<DialogueEvent>();

        // ── 에디터 전용 ─────────────────────────
        public Vector2 editorPosition;

        // 에디터 표시용 헬퍼
        public string NodeTitle =>
            NodeType == DialogueNodeType.Start ? "START" :
            NodeType == DialogueNodeType.ChapterEnd ? "CHAPTER END" :
            NodeType == DialogueNodeType.Condition ? "IF" :
            NodeType == DialogueNodeType.Narration ? SpeakerName == null || SpeakerName.IsEmpty ? "NARRATION" : SpeakerName.GetLocalizedString() :
            NodeType == DialogueNodeType.Character ? SpeakerName == null || SpeakerName.IsEmpty ? "CHARACTER" : SpeakerName.GetLocalizedString() :
            "NODE TITLE";
    }
    
    [Serializable]
    public struct DialogueCondition
    {
        public ConditionType ConditionType;
        // 어떤 타입을 쓸지 태그
        public ConditionParamType ParamType;
        public ConditionParamCheckType CheckType;

        // 타입별 전용 필드 (박싱 없음, Unity 직렬화 가능)
        public int    IntValue;
        public float  FloatValue;
        public bool   BoolValue;
        public string StringValue;

        // ── 타입 안전 접근자 ──────────────────────────────────────────
        public int    AsInt    => IntValue;
        public float  AsFloat  => FloatValue;
        public bool   AsBool   => BoolValue;
        public string AsString => StringValue;
    }

    public enum ConditionParamType { Int, Float, Bool, String }
    public enum ConditionParamCheckType { Greater, GreaterOrEqual, Less, LessOrEqual, Equal }
}
