namespace DragonGate
{
    public interface IPoolable
    {
        void OnGet();
        void OnReturn();
    }
}
