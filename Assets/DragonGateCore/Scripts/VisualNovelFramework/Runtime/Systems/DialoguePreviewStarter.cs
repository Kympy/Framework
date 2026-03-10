#if UNITY_EDITOR

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DragonGate
{
    public class DialoguePreviewStarter : GameStarter
    {
        protected override void CreateGlobalSingleton()
        {
            base.CreateGlobalSingleton();
            CameraShaker.CreateInstance();
        }

        protected override async UniTask PreLoad()
        {
            await base.PreLoad();

            var settings = Resources.Load<DialoguePreviewSettings>("DialogueGraphSettings");
            if (settings == null)
            {
                DGDebug.LogError("[DialoguePreviewStarter] DialogueGraphSettings not found in Resources.");
                return;
            }

            if (settings.DialogueRunnerPrefab == null || !settings.DialogueRunnerPrefab.RuntimeKeyIsValid())
            {
                DGDebug.LogWarning("[DialoguePreviewStarter] DialogueRunnerPrefab is not assigned in DialoguePreviewSettings.");
                new GameObject("DialogueRunner").AddComponent<DialogueRunner>();
                return;
            }

            await settings.DialogueRunnerPrefab.InstantiateAsync().ToUniTask();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            // DialogueRunner.Instance.StartDi
        }
    }
}
#endif