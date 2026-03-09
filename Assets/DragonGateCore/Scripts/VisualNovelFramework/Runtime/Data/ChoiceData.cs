using System;
using UnityEngine.Localization;

namespace DragonGate
{
    // ─────────────────────────────────────────────
    //  선택지 데이터
    // ─────────────────────────────────────────────
    [Serializable]
    public class ChoiceData
    {
        public string Id;
        public LocalizedString ChoiceText;
        public string TargetNodeId;
        public bool   IsEnabled = true;
    }
}
