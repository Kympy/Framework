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

            var settings = Resources.Load<DialoguePreviewSettings>("DialoguePreviewSettings");
            if (settings == null)
            {
                DGDebug.LogWarning("[DialoguePreviewStarter] DialoguePreviewSettings not found in Resources. Creating DialogueRunner without prefab.");
                new GameObject("DialogueRunner").AddComponent<DialogueRunner>();
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