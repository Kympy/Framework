using System;
using DG.Tweening;
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
        public AssetReferenceSprite Background;

        // 캐릭터 공통 식별자 (ShowCharacter / HideCharacter / PlayAnimation / SetCharacterEmotion)
        public AssetReferenceT<DialogueCharacterAsset> CharacterAsset;

        // ShowCharacter 전용: 뷰포트 기준 위치 (0~1), 크기 배율
        public Vector2 CharacterViewportPosition = new Vector2(0.5f, 0.5f);
        public float   CharacterScale            = 1f;
        public Ease CharacterEase = Ease.Linear;
        
        // UI
        public AssetReferenceGameObject UIAsset;
        
        // Animation / Effect
        public string AnimationTrigger;
        
        // Fx
        public AssetReferenceGameObject FxAsset;
        public Vector2 FxViewportPosition = new Vector2(0.5f, 0.5f);
        public Vector2 FxRotation        = new Vector2(0f, 0f);
        
        // Shake
        public DialogueShakeType ShakeType;
        public Vector2 ShakeStrength = new Vector2(1f, 1f);

        // Bgm & Sfx
        public AssetReferenceT<AudioClip> AudioClip;
        public float Volume = 1f;

        // Fade / Wait
        public float Duration = 0.5f;
        public Color StartColor = Color.white;
        public Color EndColor = Color.white;

        // 이 이벤트가 완료될 때까지 다음 이벤트 대기
        public bool WaitForCompletion = false;
        
        public DialogueEvent Clone() => new DialogueEvent
        {
            eventType         = eventType,
            Background       = Background,
            CharacterAsset    = CharacterAsset,
            CharacterEase = CharacterEase,
            CharacterViewportPosition   = CharacterViewportPosition,
            CharacterScale =  CharacterScale,
            AnimationTrigger  = AnimationTrigger,
            UIAsset       = UIAsset,
            FxAsset       = FxAsset,
            FxViewportPosition = FxViewportPosition,
            FxRotation     = FxRotation,
            ShakeType       = ShakeType,
            ShakeStrength    = ShakeStrength,
            AudioClip       = AudioClip,
            Volume            = Volume,
            Duration          = Duration,
            StartColor        = StartColor,
            EndColor         = EndColor,
            WaitForCompletion = WaitForCompletion,
        };
    }
}
