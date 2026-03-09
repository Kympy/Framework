namespace DragonGate
{
    public interface IInputHandler
    {
        public bool InputEnabled { get; }
        public EInputResult UpdateInput(float deltaTime);
    }

    public enum EInputResult
    {
        Continue,
        Break,
    }
}