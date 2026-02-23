using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 프레임당 호출을 한 곳에서만 관리하는 GameLoop.
    /// - IGameUpdate / IGameLateUpdate / IGameFixedUpdate 등록
    /// - 우선순위(priority) 지원 (큰 수가 먼저 실행)
    /// - per-frame GC 0: foreach/LINQ 미사용, 캡처 없음
    /// - 파괴된 UnityEngine.Object 자동 스킵
    /// </summary>
    public sealed class GameLoop : MonoBehaviourSingleton<GameLoop>
    {
        #region Interfaces

        public interface IGameUpdate
        {
            public bool IgnoreTimeScale { get; }
            public void OnUpdate(float deltaTime);
        }
        public interface IGameLateUpdate { public void OnLateUpdate(float dt); }
        public interface IGameFixedUpdate { public void OnFixedUpdate(float dt); }
        #endregion

        #region Channel (generic)
        private sealed class Channel<T> where T : class
        {
            // 실행 타겟, 우선순위, 인덱스 맵(빠른 제거)
            private readonly List<T> _items = new List<T>(64);
            private readonly List<int> _priorities = new List<int>(64);
            private readonly Dictionary<T, int> _index = new Dictionary<T, int>(128);

            public int Count => _items.Count;

            // 등록
            public void Register(T target, int priority = 0)
            {
                if (target == null) return;
                if (_index.ContainsKey(target)) return;

                // 우선순위 높은(값 큰) 순서로 삽입 - 내림차순
                int insert = _items.Count;
                for (int i = 0; i < _priorities.Count; i++)
                {
                    if (priority > _priorities[i])
                    {
                        insert = i;
                        break;
                    }
                }

                _items.Insert(insert, target);
                _priorities.Insert(insert, priority);

                // 인덱스 재생성(삽입 지점 이후만 업데이트)
                // (소수 등록일 때 성능 영향 미미)
                for (int i = insert; i < _items.Count; i++)
                {
                    _index[_items[i]] = i;
                }
            }

            public void Unregister(T target)
            {
                if (target == null) return;
                if (!_index.TryGetValue(target, out int idx)) return;

                _index.Remove(target);
                _items.RemoveAt(idx);
                _priorities.RemoveAt(idx);

                // 뒤쪽 인덱스만 갱신
                for (int i = idx; i < _items.Count; i++)
                {
                    _index[_items[i]] = i;
                }
            }

            /// <summary>파괴된 UnityEngine.Object를 느슨하게 청소(가끔 호출).</summary>
            public void SweepDestroyed()
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    var item = _items[i];
#if UNITY_5_3_OR_NEWER || UNITY
                    if (item is UnityEngine.Object uo && uo == null)
                    {
                        _index.Remove(item);
                        _items.RemoveAt(i);
                        _priorities.RemoveAt(i);
                        continue;
                    }
#endif
                    if (item == null)
                    {
                        _index.Remove(item);
                        _items.RemoveAt(i);
                        _priorities.RemoveAt(i);
                    }
                }

                // 인덱스 재구축 (안전성 위해)
                for (int i = 0; i < _items.Count; i++)
                    _index[_items[i]] = i;
            }

            public void TickUpdate(float deltaTime, float unscaledDeltaTime)
            {
                // 정방향: 우선순위가 높은 순으로 이미 정렬됨
                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
#if UNITY_5_3_OR_NEWER || UNITY
                    if (item is UnityEngine.Object uo && uo == null) continue;
#endif
                    if (item == null) continue;

                    // 인터페이스별 분기에서 캐스트는 호출 전에만 발생 (박싱 없음)
                    switch (item)
                    {
                        // 개별 채널이 전달하는 T에 따라 분기 (JIT가 예측 가능)
                        case IGameUpdate iu:
                            var delta = iu.IgnoreTimeScale ? unscaledDeltaTime : deltaTime;
                            iu.OnUpdate(delta);
                            break;
                        case IGameLateUpdate ilu:
                            ilu.OnLateUpdate(deltaTime);
                            break;
                        case IGameFixedUpdate ifu:
                            ifu.OnFixedUpdate(deltaTime);
                            break;
                    }
                }
            }
        }
        #endregion

        private readonly Channel<IGameUpdate> _update = new Channel<IGameUpdate>();
        private readonly Channel<IGameLateUpdate> _late = new Channel<IGameLateUpdate>();
        private readonly Channel<IGameFixedUpdate> _fixed = new Channel<IGameFixedUpdate>();

        private int _frameCount;
        private int _sweepInterval = 60; // 60프레임마다 한번 청소

        #region Unity lifecycle

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float unscaledDeltaTime = Time.unscaledDeltaTime;
            _update.TickUpdate(deltaTime, unscaledDeltaTime);
            TimerManager.UpdateTimers(deltaTime);

            if ((_frameCount++ % _sweepInterval) == 0)
            {
                _update.SweepDestroyed();
                _late.SweepDestroyed();
                _fixed.SweepDestroyed();
            }
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            float unscaledDeltaTime = Time.unscaledDeltaTime;
            _late.TickUpdate(deltaTime, unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            float unscaledDeltaTime = Time.fixedUnscaledDeltaTime;
            _fixed.TickUpdate(deltaTime, unscaledDeltaTime);
        }
        #endregion

        #region Static API
        public static void RegisterUpdate(IGameUpdate t, int priority = 0) => Instance?._update.Register(t, priority);
        public static void UnregisterUpdate(IGameUpdate t) => Instance?._update.Unregister(t);

        public static void RegisterLate(IGameLateUpdate t, int priority = 0) => Instance?._late.Register(t, priority);
        public static void UnregisterLate(IGameLateUpdate t) => Instance?._late.Unregister(t);

        public static void RegisterFixed(IGameFixedUpdate t, int priority = 0) => Instance?._fixed.Register(t, priority);
        public static void UnregisterFixed(IGameFixedUpdate t) => Instance?._fixed.Unregister(t);
        #endregion
    }
}