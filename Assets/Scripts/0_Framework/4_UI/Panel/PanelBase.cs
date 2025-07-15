using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public abstract class PanelBase : MonoBehaviour
    {
        public bool UseCaching { get; protected set; } = false;
        public abstract void InitPanel();
        public abstract UniTask BeforeShow(IPanelParameter parameter = null);
    }
}
