using System.Collections;
using UnityEngine;

namespace DragonGate
{
    public sealed class CoroutineRunner : MonoBehaviourSingleton<CoroutineRunner>
    {
        public static Coroutine Run(IEnumerator coroutine)
        {
            if (Instance == null)
            {
                DGDebug.LogError("CoroutineRunner not found.");
                return null;
            }
            return Instance.StartCoroutine(coroutine);
        }

        public static void Stop(Coroutine coroutine)
        {
            if (Instance == null) return;

            Instance.StopCoroutine(coroutine);
        }
    }
}
