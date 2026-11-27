using System;

namespace BlueHour.Environment
{
    public enum ClockMode { LocalTime, Accelerated, Fixed }
    public enum Season { Summer, Autumn, Winter, Spring }

    public sealed class EnvironmentClock
    {
        private readonly TimeSpan offset;
        private DateTimeOffset simulationAnchor;
        private double elapsedAnchor;

        public EnvironmentClock(TimeSpan offset)
        {
            this.offset = offset;
        }

        public ClockMode Mode { get; private set; }
        public const double Acceleration = 60d;

        public DateTimeOffset GetLocalTime(DateTimeOffset utcNow, double elapsedSeconds)
        {
            return Mode switch
            {
                ClockMode.LocalTime => utcNow.ToOffset(offset),
                ClockMode.Accelerated => simulationAnchor.AddSeconds(
                    Math.Max(0d, elapsedSeconds - elapsedAnchor) * Acceleration),
                _ => simulationAnchor
            };
        }

        public void SetMode(ClockMode mode, DateTimeOffset utcNow, double elapsedSeconds)
        {
            if (Mode == mode)
                return;

            simulationAnchor = GetLocalTime(utcNow, elapsedSeconds);
            elapsedAnchor = elapsedSeconds;
            Mode = mode;
        }

        public void AdvanceFixedHours(int hours)
        {
            if (Mode == ClockMode.Fixed)
                simulationAnchor = simulationAnchor.AddHours(hours);
        }

        public static Season SouthernSeason(DateTimeOffset localTime)
        {
            return localTime.Month switch
            {
                12 or 1 or 2 => Season.Summer,
                3 or 4 or 5 => Season.Autumn,
                6 or 7 or 8 => Season.Winter,
                _ => Season.Spring
            };
        }
    }
}
