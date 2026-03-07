using System;
using UnityEngine;

namespace DragonGate
{
    public class AssetReleaseHelper : MonoBehaviour
    {
        private bool Check()
        {
            // 플레이모드 변경 시 스킵
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && !UnityEditor.EditorApplication.isPlaying)
                return false;
#endif

            // 앱 종료 시 스킵
            if (!Application.isPlaying)
                return false;

            return true;
        }
        
        protected virtual void Release()
        {
            // 이걸 상속해서 각 auto releaser마다 내용 구현해야함.
        }

        private void CheckAndRelease()
        {
            if (Check() == false) return;
            Release();
        }
    
        private void OnDestroy()
        {
            CheckAndRelease();
        }
    }
}
