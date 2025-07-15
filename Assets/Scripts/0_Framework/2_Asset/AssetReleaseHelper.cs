using System;
using UnityEngine;

namespace Framework
{
    public class AssetReleaseHelper : MonoBehaviour
    {
        private void OnDestroy()
        {
            AssetManager.Instance.ReleaseAsset(this);
        }
    }
}
