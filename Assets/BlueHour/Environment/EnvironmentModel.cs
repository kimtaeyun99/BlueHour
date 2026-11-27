using System;
using BlueHour.World;
using UnityEngine;

namespace BlueHour.Environment
{
    public enum Weather { Clear, Overcast, Rain }

    public readonly struct EnvironmentSnapshot
    {
        public EnvironmentSnapshot(DateTimeOffset localTime, Season season, Weather weather, HabitatZone habitat,
            float depth, float temperature, float salinity, float pressure, float light, Vector3 current, float cloudCover)
        {
            LocalTime = localTime;
            Season = season;
            Weather = weather;
            Habitat = habitat;
            DepthM = depth;
            TemperatureC = temperature;
            PracticalSalinity = salinity;
            AbsolutePressurePa = pressure;
            LightFactor = light;
            CurrentMps = current;
            CloudCover = cloudCover;
        }

        public DateTimeOffset LocalTime { get; }
        public Season Season { get; }
        public Weather Weather { get; }
        public HabitatZone Habitat { get; }
        public float DepthM { get; }
        public float TemperatureC { get; }
        public float PracticalSalinity { get; }
        public float AbsolutePressurePa { get; }
        public float LightFactor { get; }
        public Vector3 CurrentMps { get; }
        public float CloudCover { get; }
    }

    public static class EnvironmentModel
    {
        private const float SeaLevelPressurePa = 101325f;
        private const float DensityKgPerCubicMetre = 1025f;
        private const float GravityMpsSquared = 9.80665f;

        public static float WeatherCloudCover(Weather weather)
        {
            return weather switch
            {
                Weather.Clear => 0.12f,
                Weather.Overcast => 0.64f,
                _ => 0.94f
            };
        }

        public static float SimulatedCloudCover(DateTimeOffset localTime, int seed)
        {
            const int secondsPerPeriod = 6 * 3600;
            var period = localTime.ToUnixTimeMilliseconds() / (secondsPerPeriod * 1000d);
            var whole = (long)Math.Floor(period);
            var fraction = (float)(period - whole);
            return Mathf.SmoothStep(WeatherValue(whole, seed), WeatherValue(whole + 1, seed), fraction);
        }

        private static float WeatherValue(long period, int seed)
        {
            unchecked
            {
                var value = (uint)period ^ (uint)(period >> 32) ^ (uint)seed;
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                return (value & 0xffffffu) / 16777215f;
            }
        }

        public static Weather WeatherAt(float cloudCover)
        {
            return cloudCover >= 0.77f ? Weather.Rain : cloudCover >= 0.43f ? Weather.Overcast : Weather.Clear;
        }

        public static float Daylight(DateTimeOffset localTime)
        {
            var angle = ((float)localTime.TimeOfDay.TotalHours - 6f) * Mathf.PI / 12f;
            return Mathf.Clamp01(Mathf.Sin(angle));
        }

        public static EnvironmentSnapshot Sample(RegionDefinition region, Vector3 position, DateTimeOffset localTime,
            Season season, Weather weather, float cloudCover)
        {
            region.TryLocate(position, out var habitat);
            var depth = region.DepthAt(position);
            var surfaceTemperature = season switch
            {
                Season.Summer => 28.5f,
                Season.Autumn => 26.8f,
                Season.Winter => 23.2f,
                _ => 25.4f
            };

            var temperature = Mathf.Lerp(surfaceTemperature, 18.5f, 1f - Mathf.Exp(-depth / 95f));
            var salinity = 34.7f + 0.65f * (1f - Mathf.Exp(-depth / 80f))
                - cloudCover * 0.20f * Mathf.Exp(-depth / 10f);
            var pressure = SeaLevelPressurePa + DensityKgPerCubicMetre * GravityMpsSquared * depth;
            var light = Mathf.Lerp(0.025f, 1f, Daylight(localTime)) * (1f - 0.62f * cloudCover)
                * Mathf.Exp(-0.023f * depth) * (habitat?.LightExposure ?? 1f);
            var tidalPhase = (float)(localTime.ToUnixTimeSeconds() % 44712) * (2f * Mathf.PI / 44712f);
            var exposure = habitat?.CurrentExposure ?? 0.5f;
            var current = new Vector3(Mathf.Sin(tidalPhase + position.z * 0.006f), 0f,
                0.56f * Mathf.Cos(tidalPhase + position.x * 0.007f));
            current *= exposure * (0.25f + 0.43f * Mathf.Exp(-depth / 48f));

            return new EnvironmentSnapshot(localTime, season, weather, habitat, depth,
                temperature, salinity, pressure, light, current, cloudCover);
        }
    }
}
