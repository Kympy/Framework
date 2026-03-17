using UnityEngine;

namespace DragonGate
{
    public abstract class UILoadingScreen : UICore
    {
        public bool IsProgressComplete { get; protected set; } = false;
        
        private float _targetProgress = 0;

        protected override void OnVisible()
        {
            base.OnVisible();
            _targetProgress = -1;
        }

        public void SetProgress(float progress)
        {
            if (_targetProgress >= progress) return;
            _targetProgress = Mathf.Clamp01(progress);
            OnLoadingProgressChanged(_targetProgress);
        }
        
        protected abstract void OnLoadingProgressChanged(float progress);
    }
}
