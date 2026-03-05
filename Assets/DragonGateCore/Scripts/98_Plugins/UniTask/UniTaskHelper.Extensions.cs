using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DragonGate
{
    public static class UniTaskHelperExtensions
    {
        public static async UniTask PlayAndWait(this Animator animator, ICancelable cancelable, string stateName, int layer = 0, float normalizedTime = 0f)
        {
            var hashName = Animator.StringToHash(stateName);
            animator.Play(hashName, layer, normalizedTime);
            // await UniTaskHelper.NextFrame(cancelable);
            // 원하는 상태에 들어왔는지 체크해본다
            await UniTaskHelper.WaitUntil(cancelable, () =>
            {
                var info = animator.GetCurrentAnimatorStateInfo(layer);
                return info.shortNameHash == hashName && !animator.IsInTransition(layer);
            });
            // 재생이 종료되었는지 대기한다.
            await UniTaskHelper.WaitUntil(cancelable, () =>
            {
                var info = animator.GetCurrentAnimatorStateInfo(layer);
                // 상태가 바뀌었으면 대기 종료
                if (info.shortNameHash != hashName)
                    return true;
                return info.normalizedTime >= 1f;
            });
        }
    }
}
