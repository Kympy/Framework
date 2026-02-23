#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace DragonGate
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI (Custom)/SoftImage(Preview Only)")]
    public class SoftImage : Image
#if UNITY_EDITOR
        , IPreprocessBuildWithReport
#endif
    {
        [SerializeField] private AssetReferenceSprite _spriteReference;

//     public new Sprite Sprite
//     {
//         get => base.sprite;
//         set
//         {
// #if UNITY_EDITOR
//             if (!Application.isPlaying)
//             {
//                 PNLog.LogError("SoftImage.Sprite cannot be set in editor mode. Use _spriteReference instead.");
//             }
// #endif
//             return;
//         }
//     }

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                PreviewInEditor();
            }
            else
            {
                base.sprite = null;
            }
#endif
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying)
            {
                PreviewInEditor();
            }
        }

        // protected override void OnDisable()
        // {
        //     base.OnDisable();
        //     if (!Application.isPlaying)
        //     {
        //         base.sprite = null;
        //     }
        // }

        // protected override void OnDestroy()
        // {
        //     base.OnDestroy();
        //     if (!Application.isPlaying)
        //     {
        //         base.sprite = null;
        //     }
        // }

        private void PreviewInEditor()
        {
            var previewSprite = _spriteReference?.editorAsset;
            if (previewSprite == null)
            {
                base.sprite = null;
            }
            else
            {
                base.sprite = previewSprite as Sprite;
            }
        }

        public int callbackOrder { get; } = 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (this != null)
            {
                base.sprite = null;
            }
        }
#endif
    }
}