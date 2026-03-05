using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    // 게임 루프에 부착하여 사용. 기본적으로 유니티 업데이트 사이클 이후에 호출.
    public static class TimerManager
    {
        private class Timer
        {
            public float RemainingTime;
            public float Interval;
            public float IntervalElapsedTime;
            public bool RandomInterval;
            public float MinInterval;
            public float MaxInterval;
            public bool Repeat;
            public bool IsEveryFrame;
            public bool IsUnscaled;
            public System.Action Callback;
            public UnityEngine.Behaviour Owner;

            public void DecreaseTime(float deltaTime)
            {
                RemainingTime -= deltaTime;
            }

            // true를 반환하면 타이머 즉시 제거
            public bool CheckInvoke(float deltaTime)
            {
                if (Owner != null && !Owner.isActiveAndEnabled)
                    return true;

                if (IsEveryFrame)
                {
                    Callback?.Invoke();
                    return false;
                }

                IntervalElapsedTime += deltaTime;
                if (IntervalElapsedTime >= Interval)
                {
                    IntervalElapsedTime -= Interval;
                    Callback?.Invoke();
                    // 랜덤 간격 타이머 일 경우 간격 재설정
                    if (RandomInterval)
                    {
                        var newInterval = UnityEngine.Random.Range(MinInterval, MaxInterval);
                        Interval = newInterval;
                    }
                }
                return false;
            }
        }

        private static uint _maxTimerId = 1; // 0은 미초기화(invalid) 예약
        private static Stack<uint> _idPool = new();
        private static Stack<Timer> _timerPool = new();
        private static Dictionary<uint, Timer> _activeTimers = new();
        private static Dictionary<uint, Timer> _timersToAdd = new();
        private static HashSet<uint> _timersToRemove = new();

        // 일정 간격으로 반복되는 타이머
        public static TimerHandle SetRepeatTimer(float interval, System.Action callback, UnityEngine.Behaviour owner = null)
        {
            var id = GetId();
            var timer = GetTimer();
            timer.Callback = callback;
            timer.Owner = owner;
            timer.Repeat = true;
            timer.RemainingTime = -1;
            timer.Interval = interval;
            timer.RandomInterval = false;
            timer.MinInterval = 0;
            timer.MaxInterval = 0;
            return SetTimerInternal(id, timer);
        }

        // 매 프레임 실행되는 타이머
        public static TimerHandle SetEveryFrameTimer(System.Action callback, UnityEngine.Behaviour owner = null)
        {
            var id = GetId();
            var timer = GetTimer();
            timer.Callback = callback;
            timer.Owner = owner;
            timer.Repeat = true;
            timer.IsEveryFrame = true;
            timer.RemainingTime = -1;
            timer.Interval = 0;
            timer.RandomInterval = false;
            timer.MinInterval = 0;
            timer.MaxInterval = 0;
            return SetTimerInternal(id, timer);
        }

        // 랜덤 간격으로 반복되는 타이머
        public static TimerHandle SetRandomIntervalTimer(float minInterval, float maxInterval, System.Action callback, UnityEngine.Behaviour owner = null)
        {
            var id = GetId();
            var timer = GetTimer();
            timer.Callback = callback;
            timer.Owner = owner;
            timer.Repeat = true;
            timer.RemainingTime = -1;
            timer.Interval = Random.Range(minInterval, maxInterval);
            timer.RandomInterval = true;
            timer.MinInterval = minInterval;
            timer.MaxInterval = maxInterval;
            return SetTimerInternal(id, timer);
        }

        // delay 후 1회 실행되는 타이머
        public static TimerHandle SetDelayTimer(float delay, System.Action callback, UnityEngine.Behaviour owner = null, bool ignoreTimeScale = false)
        {
            var id = GetId();
            var timer = GetTimer();
            timer.Callback = callback;
            timer.Owner = owner;
            timer.Repeat = false;
            timer.RemainingTime = delay;
            timer.Interval = delay;
            timer.RandomInterval = false;
            timer.MinInterval = 0;
            timer.MaxInterval = 0;
            timer.IsUnscaled = ignoreTimeScale;
            return SetTimerInternal(id, timer);
        }

        // 지속 시간만큼 일정 간격마다 실행되는 타이머
        public static TimerHandle SetTimer(float duration, float interval, System.Action callback, UnityEngine.Behaviour owner = null)
        {
            var id = GetId();
            var timer = GetTimer();
            timer.Callback = callback;
            timer.Owner = owner;
            timer.Repeat = false;
            timer.RemainingTime = duration;
            timer.Interval = interval;
            timer.RandomInterval = false;
            timer.MinInterval = 0;
            timer.MaxInterval = 0;
            return SetTimerInternal(id, timer);
        }
        
        private static TimerHandle SetTimerInternal(uint id, Timer timer)
        {
            var handle = new TimerHandle()
            {
                Id = id,
            };
            // UpdateTimers 순회 중에 타이머가 추가될 수 있으므로 대기 큐에 추가
            _timersToAdd.Add(id, timer);
            return handle;
        }

        public static void Clear(TimerHandle handle)
        {
            Clear(handle.Id);
        }

        private static void Clear(uint timerId)
        {
            if (timerId == 0) return;
            // 아직 추가되지 않은 타이머라면 추가 대기 큐에서 제거
            if (_timersToAdd.TryGetValue(timerId, out var pendingTimer))
            {
                _timersToAdd.Remove(timerId);
                ReturnId(timerId);
                ReturnTimer(pendingTimer);
                return;
            }

            // 활성 타이머라면 제거 예약
            if (_activeTimers.ContainsKey(timerId))
            {
                _timersToRemove.Add(timerId);
            }
        }

        public static void UpdateTimers(float deltaTime, float unscaledDeltaTime)
        {
            // 대기 중인 타이머를 activeTimers에 추가
            foreach (var timerPair in _timersToAdd)
            {
                _activeTimers.Add(timerPair.Key, timerPair.Value);
            }
            _timersToAdd.Clear();

            // activeTimers 순회 (콜백에서 타이머 추가/제거 가능)
            foreach (var timerPair in _activeTimers)
            {
                if (_timersToRemove.Contains(timerPair.Key))
                {
                    continue;
                }
                var timer = timerPair.Value;
                float dt = timer.IsUnscaled ? unscaledDeltaTime : deltaTime;
                bool ownerDead = timer.CheckInvoke(dt);
                if (ownerDead)
                {
                    _timersToRemove.Add(timerPair.Key);
                }
                else if (timer.Repeat == false)
                {
                    timer.DecreaseTime(dt);
                    if (timer.RemainingTime <= 0f)
                    {
                        _timersToRemove.Add(timerPair.Key);
                    }
                }
            }

            // 제거 예정 타이머 처리
            foreach (var timerId in _timersToRemove)
            {
                var timer = _activeTimers[timerId];
                _activeTimers.Remove(timerId);
                ReturnId(timerId);
                ReturnTimer(timer);
            }
            _timersToRemove.Clear();
        }

        private static uint GetId()
        {
            if (_idPool.TryPop(out var id) == false)
            {
                return _maxTimerId++;
            }
            return id;
        }
        
        private static Timer GetTimer()
        {
            if (_timerPool.TryPop(out var timer) == false) 
                return new Timer();
            return timer;
        }
        
        private static void ReturnId(uint id)
        {
            _idPool.Push(id);
        }

        private static void ReturnTimer(Timer timer)
        {
            timer.Callback = null;
            timer.Owner = null;
            timer.IsEveryFrame = false;
            timer.IsUnscaled = false;
            timer.IntervalElapsedTime = 0;
            _timerPool.Push(timer);
        }
    }
}