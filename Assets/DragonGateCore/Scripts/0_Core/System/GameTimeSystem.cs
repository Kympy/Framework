using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class GameTimeSystem : Singleton<GameTimeSystem>, GameLoop.IGameUpdate
    {
        public GameTime CurrentTime => new GameTime(_totalTick);
        public bool IgnoreTimeScale { get; } = false;
        public bool IsTicking { get; private set; } = false;

        private long _totalTick;
        private float _elapsedSeconds;
        // 현실 시간 N초마다 1틱 증가
        private float _secondsPerTick = 1f;

        private readonly List<GameTimeEventHandle> _scheduledEvents = new List<GameTimeEventHandle>();

        private event Action<GameTime> OnTick;

        public void SetTotalTicks(long startTotalTick)
        {
            if (IsTicking)
            {
                DGDebug.LogError("Can't set start time when time is ticking.");
                return;
            }
            _totalTick = startTotalTick;
        }

        public void StartGameTime()
        {
            if (IsTicking) return;
            GameLoop.RegisterUpdate(this);
            IsTicking = true;
        }

        public void StopGameTime()
        {
            if (IsTicking == false) return;
            GameLoop.UnregisterUpdate(this);
            IsTicking = false;
        }

        public void OnUpdate(float deltaTime)
        {
            _elapsedSeconds += deltaTime;
            while (_elapsedSeconds >= _secondsPerTick)
            {
                _elapsedSeconds -= _secondsPerTick;
                _totalTick++;
                OnTick?.Invoke(CurrentTime);
                CheckAndFireEvents();
            }
        }

        /// <summary>현실 1초 = 게임 1초 모드. 1틱 = 1게임초 (TicksPerGameMinute = 60).</summary>
        public void SetRealTimeMode()
        {
            if (IsTicking)
            {
                DGDebug.Log("Can't switch mode when time is ticking.", Color.softRed);
                return;
            }
            GameTime.TicksPerGameMinute = 60;
            _secondsPerTick = 1f;
        }

        /// <summary>현실 N초 = 게임 1분 모드. 1틱 = 1게임분 (TicksPerGameMinute = 1).</summary>
        public void SetRealSecondsPerGameMinuteMode(int seconds)
        {
            if (IsTicking)
            {
                DGDebug.Log("Can't switch mode when time is ticking.", Color.softRed);
                return;
            }
            GameTime.TicksPerGameMinute = 1;
            _secondsPerTick = seconds;
        }

        /// <summary>특정 시각의 GameTime을 생성합니다.</summary>
        public GameTime CreateTime(int year = 0, int month = 0, int day = 0, int hour = 0, int minute = 0)
        {
            long totalMinutes = (long)year  * GameTime.MinutesPerYear
                              + (long)month * GameTime.MinutesPerMonth
                              + (long)day   * GameTime.MinutesPerDay
                              + (long)hour  * GameTime.MinutesPerHour
                              + minute;
            return new GameTime(totalMinutes * GameTime.TicksPerGameMinute);
        }

        /// <summary>특정 게임 시각에 발동할 이벤트를 등록합니다. 반환된 핸들로 제거할 수 있습니다.</summary>
        public GameTimeEventHandle AddEvent(GameTime targetTime, Action<long> callback)
        {
            var handle = new GameTimeEventHandle(targetTime.TotalTick, callback);
            InsertSorted(handle);
            return handle;
        }

        /// <summary>매 N게임시간마다 반복 발동할 이벤트를 등록합니다. 반환된 핸들로 제거할 수 있습니다.</summary>
        public GameTimeRepeatingEventHandle AddRepeatingEvent(Action<long> callback, int hours = 0, int minutes = 0, int seconds = 0)
        {
            long intervalTick = ToIntervalTick(hours, minutes, seconds);
            if (intervalTick <= 0)
            {
                DGDebug.Log("AddRepeatingEvent: interval must be greater than 0.", Color.softRed);
                return null;
            }
            var handle = new GameTimeRepeatingEventHandle(_totalTick + intervalTick, intervalTick, callback);
            InsertSorted(handle);
            return handle;
        }

        /// <summary>매일 특정 게임 시각(N시 M분)에 반복 발동할 이벤트를 등록합니다. 반환된 핸들로 제거할 수 있습니다.</summary>
        public GameTimeRepeatingEventHandle AddDailyEvent(Action<long> callback, int hour, int minute = 0)
        {
            long dayIntervalTick = ToIntervalTick(hours: GameTime.HoursPerDay);
            long targetTickOfDay  = ToIntervalTick(hours: hour, minutes: minute);

            var todayTicks = _totalTick % dayIntervalTick;
            long todayStartTick = _totalTick - todayTicks;
            long firstFireTick  = todayStartTick + targetTickOfDay;

            if (firstFireTick <= _totalTick)
                firstFireTick += dayIntervalTick;

            var handle = new GameTimeRepeatingEventHandle(firstFireTick, dayIntervalTick, callback);
            InsertSorted(handle);
            return handle;
        }

        /// <summary>등록된 이벤트를 제거합니다. 일회성·반복 이벤트 모두 사용 가능합니다.</summary>
        public void RemoveEvent(GameTimeEventHandle handle)
        {
            _scheduledEvents.Remove(handle);
        }

        private long ToIntervalTick(int hours = 0, int minutes = 0, int seconds = 0)
        {
            long totalMinutes = (long)hours * GameTime.MinutesPerHour + minutes;
            return totalMinutes * GameTime.TicksPerGameMinute + (long)seconds * (GameTime.TicksPerGameMinute / 60);
        }

        private void InsertSorted(GameTimeEventHandle handle)
        {
            int insertIndex = _scheduledEvents.Count;
            for (int i = 0; i < _scheduledEvents.Count; i++)
            {
                if (_scheduledEvents[i].TargetTick > handle.TargetTick)
                {
                    insertIndex = i;
                    break;
                }
            }
            _scheduledEvents.Insert(insertIndex, handle);
        }

        private void CheckAndFireEvents()
        {
            while (_scheduledEvents.Count > 0 && _scheduledEvents[0].TargetTick <= _totalTick)
            {
                var handle = _scheduledEvents[0];
                _scheduledEvents.RemoveAt(0);
                handle.Callback?.Invoke(_totalTick);

                if (handle is GameTimeRepeatingEventHandle repeating)
                {
                    repeating.TargetTick += repeating.IntervalTick;
                    InsertSorted(repeating);
                }
            }
        }

        public void AddTickEvent(Action<GameTime> callback, bool executeInit = false)
        {
            OnTick -= callback;
            OnTick += callback;

            if (executeInit)
            {
                callback?.Invoke(CurrentTime);
            }
        }

        public void RemoveTickEvent(Action<GameTime> callback)
        {
            OnTick -= callback;
        }

        public bool IsAm()
        {
            return CurrentTime.Hour < 12;
        }
    }
}