using System;
using UnityEngine;

namespace DragonGate
{
    public class AssetReleaseHelper : MonoBehaviour
    {
        private void OnDestroy()
        {
            // Skip cleanup during domain reload (play mode exit)
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && !UnityEditor.EditorApplication.isPlaying)
                return;
            #endif

            // Skip if application is quitting
            if (!Application.isPlaying)
                return;

            if (AssetManager.HasInstance)
                AssetManager.Instance.ReleaseAsset(this);
        }
    }
}
