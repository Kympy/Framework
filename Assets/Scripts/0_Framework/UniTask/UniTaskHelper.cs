using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public class UniTaskHelper
    {
        // 게임 전역 토큰
        private static CancellationTokenSource globalTokenSource = new CancellationTokenSource();

        // 씬 전용 토큰
        private static CancellationTokenSource _sceneTokenSource = null;

        // 씬 전환에 사용하는 토큰
        private static CancellationTokenSource _transitionTokenSource = null;

        public static void CancelGlobalToken()
        {
            DGLog.Log("Cancel Global Token!", Color.magenta);
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
                _sceneTokenSource = CancellationTokenSource.CreateLinkedTokenSource(globalTokenSource.Token, new CancellationTokenSource().Token);
            }
            return _sceneTokenSource;
        }

        public static void CancelSceneToken()
        {
            if (_sceneTokenSource != null)
            {
                DGLog.Log("Cancel Scene Token!", Color.magenta);
                Cancel(_sceneTokenSource);
                _sceneTokenSource = null;
            }
        }

        public static CancellationTokenSource CreateTransitionToken()
        {
            if (_transitionTokenSource == null || _transitionTokenSource.IsCancellationRequested)
            {
                _transitionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(globalTokenSource.Token, new CancellationTokenSource().Token);
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

        public static CancellationTokenSource CreateNormalSceneLinkedToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource().Token, _sceneTokenSource.Token);
        }

        public static CancellationTokenSource CreateNormalGlobalLinkedToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource().Token, globalTokenSource.Token);
        }

        public static void Cancel(CancellationTokenSource tokenSource)
        {
            tokenSource?.Cancel();
            tokenSource?.Dispose();
        }

        public static async UniTask Yield(ICancelable obj)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.Yield(cancellationToken: tokenSource.Token);
        }

        public static async UniTask WaitForSeconds(ICancelable obj, float delay)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.WaitForSeconds(delay, cancellationToken: tokenSource.Token);
        }

        public static async UniTask WaitUntil(ICancelable obj, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
        {
            var tokenSource = obj.GetTokenSource();
            await UniTask.WaitUntil(predicate, timing, tokenSource.Token, cancelImmediately);
        }
    }
}