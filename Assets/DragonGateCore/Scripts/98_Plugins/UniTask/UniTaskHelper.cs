using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine;

namespace DragonGate
{
    public partial class UniTaskHelper
    {
        // 게임 전역 토큰
        private static CancellationTokenSource globalTokenSource = new CancellationTokenSource();
        // 씬 전용 토큰
        private static CancellationTokenSource _sceneTokenSource = null;
        // 씬 전환에 사용하는 토큰
        private static CancellationTokenSource _transitionTokenSource = null;

        public static void CancelGlobalToken()
        {
            DGDebug.Log("Cancel Global Token!", Color.magenta);
            Cancel(globalTokenSource);
            globalTokenSource = null;
        }

        /// <summary>
        /// 전역 토큰과 결합된 씬 토큰
        /// </summary>
        /// <returns></returns>
        public static CancellationTokenSource CreateSceneToken()
        {
            if (_sceneTokenSource == null || _sceneTokenSource.IsCancellationRequested)
            {
                _sceneTokenSource = CancellationTokenSource.CreateLinkedTokenSource(globalTokenSource.Token);
            }
            return _sceneTokenSource;
        }

        public static void CancelSceneToken()
        {
            if (_sceneTokenSource != null)
            {
                DGDebug.Log("Cancel Scene Token!", Color.magenta);
                Cancel(_sceneTokenSource);
                _sceneTokenSource = null;
            }
        }

        public static CancellationTokenSource CreateTransitionToken()
        {
            if (_transitionTokenSource == null || _transitionTokenSource.IsCancellationRequested)
            {
                _transitionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(globalTokenSource.Token);
            }

            return _transitionTokenSource;
        }

        /// <summary>
        /// 씬 토큰과 결합된 오브젝트 단위의 토큰
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static CancellationTokenSource CreateObjectToken(MonoBehaviour behaviour)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(behaviour.destroyCancellationToken, _sceneTokenSource.Token);
        }

        public static CancellationTokenSource GetSceneLinkedToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(_sceneTokenSource.Token);
        }

        public static CancellationTokenSource GetGlobalLinkedToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(globalTokenSource.Token);
        }

        public static void Cancel(CancellationTokenSource tokenSource)
        {
            // DGLog.Log("Cancel", Color.magenta);
            tokenSource?.Cancel();
            tokenSource?.Dispose();
        }

        public static async UniTask Yield(ICancelable obj, PlayerLoopTiming loopTiming = PlayerLoopTiming.Update)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.Yield(cancellationToken: tokenSource.Token, timing: loopTiming);
        }

        public static async UniTask WaitForSeconds(ICancelable obj, float delay)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.WaitForSeconds(delay, cancellationToken: tokenSource.Token);
        }

        public static async UniTask WaitForSecondsScene(float delay)
        {
            await UniTask.WaitForSeconds(delay, cancellationToken: _sceneTokenSource.Token);
        }

        public static async UniTask WaitUntil(ICancelable obj, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.WaitUntil(predicate, timing, tokenSource.Token, cancelImmediately);
        }

        public static async UniTask NextFrame(ICancelable obj, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.NextFrame(timing, cancellationToken: tokenSource.Token);
        }

        /// <summary>
        /// 여러 UniTask를 모두 기다림 (취소 토큰 적용)
        /// </summary>
        public static async UniTask WhenAll(ICancelable obj, params UniTask[] tasks)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.WhenAll(tasks).AttachExternalCancellation(tokenSource.Token);
        }

        /// <summary>
        /// 여러 UniTask를 모두 기다리고 결과 배열 반환
        /// </summary>
        public static async UniTask<T[]> WhenAll<T>(ICancelable obj, params UniTask<T>[] tasks)
        {
            var tokenSource = obj.GetTokenSource();
            return await UniTask.WhenAll(tasks).AttachExternalCancellation(tokenSource.Token);
        }

        public static async UniTask<T[]> WhenAll<T>(ICancelable obj, IEnumerable<UniTask<T>> tasks)
        {
            var tokenSource = obj.GetTokenSource();
            return await UniTask.WhenAll(tasks).AttachExternalCancellation(tokenSource.Token);
        }

#if DOTWEEN && UNITASK_DOTWEEN_SUPPORT
        public static async UniTask WaitTween(ICancelable obj, Tween tween)
        {
            var tokenSource = obj.GetTokenSource();
            await tween.AwaitForComplete(cancellationToken: tokenSource.Token);
        }
#endif
    }
}