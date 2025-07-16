namespace Framework
{
    public struct PopupOption
    {
        public bool AllowBackgroundHideAction;
        public EPopupBackgroundType BackgroundType;

        public PopupOption(bool allowBackgroundHideAction = true, EPopupBackgroundType backgroundType = EPopupBackgroundType.Dimmed)
        {
            AllowBackgroundHideAction = allowBackgroundHideAction;
            BackgroundType = backgroundType;
        }
    }

    public enum EPopupBackgroundType
    {
        Dimmed,
        Transparent,
    }
}