using System;

namespace DragonGate
{
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
    public enum ConditionType
    {
        None = 0,
        HasItem
    }

    public enum DialogueEventType
    {
        SetBackground = 10,
        ShowCharacter = 20,
        HideCharacter = 30,
        HideAllCharacter = 40,
        PlayAnimation = 50,
        PlayEffect = 60,
        ShowUI = 70,
        HideUI = 80,
        PlayBGM = 90,
        StopBGM = 100,
        PlaySFX = 110,
        FadeIn = 120,
        FadeOut = 130,
        Wait = 140,
    }

}
