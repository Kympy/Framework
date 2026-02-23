namespace DragonGate
{
    public interface IInputHandler
    {
        public EInputResult UpdateInput(float deltaTime);
    }

    public enum EInputResult
    {
        Continue,
        Break,
    }
}