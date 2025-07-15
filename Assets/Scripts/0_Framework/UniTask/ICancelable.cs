using System.Threading;

namespace Framework
{
    public interface ICancelable
    {
        public CancellationTokenSource GetTokenSource();

        public void CancelToken();
    }
}
