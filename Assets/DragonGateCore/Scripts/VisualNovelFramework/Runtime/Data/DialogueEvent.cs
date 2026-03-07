using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
// ─────────────────────────────────────────────
    //  이벤트 데이터
    // ─────────────────────────────────────────────
    [Serializable]
    public class DialogueEvent
    {
        public DialogueEventType eventType;
        public AssetReference Asset;

        // 캐릭터 공통 식별자 (ShowCharacter / HideCharacter / PlayAnimation / SetCharacterEmotion)
        public DialogueCharacterAsset CharacterAsset;

        // ShowCharacter 전용: 뷰포트 기준 위치 (0~1), 크기 배율
        public Vector2 CharacterViewportPosition = new Vector2(0.5f, 0f);
        public float   CharacterScale            = 1f;

        // Animation / Effect
        public string AnimationTrigger;

        // UI
        public string UiElementId;

        // Bgm & Sfx
        public float Volume;
        public float BgmFadeDuration = 0.2f;

        // Fade / Wait
        public float Duration = 0.5f;

        // 이 이벤트가 완료될 때까지 다음 이벤트 대기
        public bool WaitForCompletion = false;
    }
}
