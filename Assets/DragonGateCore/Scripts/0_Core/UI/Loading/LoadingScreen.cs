
using UnityEngine;
using UnityEngine.Events;

namespace DragonGate
{
    public abstract class LoadingScreen : UICore
    {
        public abstract void Show();
        public abstract void Hide();
        public abstract void SetProgress(float progress, float duration = 1);
    }
}
