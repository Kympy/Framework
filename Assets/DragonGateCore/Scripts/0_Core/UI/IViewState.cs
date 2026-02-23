namespace DragonGate
{
    public interface IViewState<TState> where TState : struct
    {
        public void SetViewState(in TState state);
    }
}