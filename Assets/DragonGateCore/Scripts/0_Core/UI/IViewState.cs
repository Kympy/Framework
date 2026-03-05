namespace DragonGate
{
    public interface IViewState<TViewData>
    {
        public void SetViewState(in TViewData viewData);
    }
}