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

        // Character sprite
        public string      CharacterId;
        public Sprite      CharacterSprite;
        public CharacterPosition CharacterPosition;

        // Animation / Effect
        public string    AnimationTrigger;

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
