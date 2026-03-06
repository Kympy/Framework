using System;

namespace DragonGate
{
    public enum DialogueNodeType
    {
        Start      = 0,
        NPC        = 1,
        Player     = 2,
        Narration  = 3,
        ChapterEnd = 4,
    }

    public enum DialogueEventType
    {
        SetBackground,
        ShowCharacterSprite,
        HideCharacterSprite,
        SetCharacterEmotion,
        PlayAnimation,
        PlayEffect,
        ShowUI,
        HideUI,
        PlayBGM,
        StopBGM,
        PlaySFX,
        FadeIn,
        FadeOut,
        Wait,
    }

    public enum CharacterPosition { Left, Center, Right }
}
