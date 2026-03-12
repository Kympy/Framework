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
        public bool Fade = false;
        
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
        
        public DialogueEvent Clone(DialogueEvent outObject = null)
        {
            outObject ??= new DialogueEvent();

            outObject.eventType = eventType;
            outObject.Background = Background;
            outObject.CharacterAsset = CharacterAsset;
            outObject.CharacterEase = CharacterEase;
            outObject.CharacterViewportPosition = CharacterViewportPosition;
            outObject.CharacterScale = CharacterScale;
            outObject.Fade = Fade;
            outObject.AnimationTrigger = AnimationTrigger;
            outObject.UIAsset = UIAsset;
            outObject.FxAsset = FxAsset;
            outObject.FxViewportPosition = FxViewportPosition;
            outObject.FxRotation = FxRotation;
            outObject.ShakeType = ShakeType;
            outObject.ShakeStrength = ShakeStrength;
            outObject.AudioClip = AudioClip;
            outObject.Volume = Volume;
            outObject.Duration = Duration;
            outObject.StartColor = StartColor;
            outObject.EndColor = EndColor;
            outObject.WaitForCompletion = WaitForCompletion;
            return outObject;
        }
    }
}
