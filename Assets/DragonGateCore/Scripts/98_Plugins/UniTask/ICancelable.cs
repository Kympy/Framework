using System.Threading;

namespace DragonGate
{
    public interface ICancelable
    {
        public CancellationTokenSource GetTokenSource();
        public void CancelToken();
        public bool IsValid();
    }
}
