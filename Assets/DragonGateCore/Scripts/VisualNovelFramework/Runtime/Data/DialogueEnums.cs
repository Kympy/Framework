using System;

namespace DragonGate
{
    [Serializable]
    public enum DialogueNodeType
    {
        Start      = 0,
        Character  = 10,
        Narration  = 20,
        Condition  = 30,
        ChapterEnd = 100,
    }

    /// <summary>
    /// 컨디션 노드에서 선택할 조건 종류.
    /// 게임 콘텐츠 레이어에서 값을 추가하고, IConditionEvaluator 구현체에서 처리하세요.
    /// </summary>
    [Serializable]
    public enum ConditionType
    {
        None = 0,
        HasItem
    }

    [System.Serializable]
    public enum DialogueEventType
    {
        SetBackground = 10,
        ShowCharacter = 20,
        MoveCharacter = 21,
        HideCharacter = 30,
        ColorCharacter = 31,
        InvertCharacter = 32,
        HideAllCharacter = 40,
        PlayAnimation = 50,
        PlayEffect = 60,
        Shake = 61,
        ShowUI = 70,
        HideUI = 80,
        PlayBGM = 90,
        StopBGM = 100,
        BgmVolume = 101,
        PlaySFX = 110,
        FadeIn = 120,
        FadeOut = 130,
        Wait = 140,
    }

    [System.Serializable]
    public enum DialogueShakeType
    {
        Camera,
        Character,
        Text,
    }
}
