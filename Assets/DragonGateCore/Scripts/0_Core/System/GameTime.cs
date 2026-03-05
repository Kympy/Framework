namespace DragonGate
{
    public readonly struct GameTime
    {
        /// <summary>게임 1분을 구성하는 틱 수. GameTimeSystem이 초기화 시 설정합니다.</summary>
        public static int TicksPerGameMinute { get; internal set; } = 1;

        private readonly long _totalTick;

        public long TotalTick => _totalTick;

        public long TotalMinutes => _totalTick / TicksPerGameMinute;
        public long TotalHours   => TotalMinutes / MinutesPerHour;
        public long TotalDays    => TotalMinutes / MinutesPerDay;

        public int Year   => (int)(TotalMinutes / MinutesPerYear);
        public int Month  => (int)((TotalMinutes % MinutesPerYear) / MinutesPerMonth);
        public int Day    => (int)((TotalMinutes % MinutesPerMonth) / MinutesPerDay);
        public int Hour   => (int)((TotalMinutes % MinutesPerDay) / MinutesPerHour);
        public int Minute => (int)(TotalMinutes % MinutesPerHour);
        /// <summary>TicksPerGameMinute > 1인 모드에서만 유효. 그 외에서는 항상 0.</summary>
        public int Second => (int)(_totalTick % TicksPerGameMinute) * (60 / TicksPerGameMinute);

        public const int MinutesPerHour  = 60;
        public const int HoursPerDay     = 24;
        public const int DaysPerMonth    = 30;
        public const int MonthsPerYear   = 12;

        public const int MinutesPerDay   = HoursPerDay * MinutesPerHour;
        public const int MinutesPerMonth = DaysPerMonth * MinutesPerDay;
        public const int MinutesPerYear  = MonthsPerYear * MinutesPerMonth;

        public GameTime(long totalTick)
        {
            _totalTick = totalTick;
        }

        public GameTime(int year, int month, int day, int hour, int minute, int second = 0)
        {
            long totalMinutes = (long)year  * MinutesPerYear
                              + (long)month * MinutesPerMonth
                              + (long)day   * MinutesPerDay
                              + (long)hour  * MinutesPerHour
                              + minute;
            _totalTick = totalMinutes * TicksPerGameMinute + second * (TicksPerGameMinute / 60);
        }

        public float GetNormalizedDayTime()
        {
            return (Minute + Hour * 60) / (float)MinutesPerDay;
        }
    }
}