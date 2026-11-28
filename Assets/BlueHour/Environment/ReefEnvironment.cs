using System;
using BlueHour.World;
using UnityEngine;

namespace BlueHour.Environment
{
    public sealed class ReefEnvironment : MonoBehaviour
    {
        [SerializeField] private RegionDefinition region;
        [SerializeField] private Camera viewer;
        [SerializeField] private Light sun;
        [SerializeField] private int weatherSeed = 625;

        private EnvironmentClock clock;
        private float cloudCover;
        private float sampleTimer;

        public RegionDefinition Region => region;
        public EnvironmentSnapshot ViewerSnapshot { get; private set; }
        public ClockMode TimeMode => clock.Mode;
        public Season? SeasonOverride { get; private set; }
        public Weather? WeatherOverride { get; private set; }

        private void Awake()
        {
            if (region == null || viewer == null || sun == null)
            {
                Debug.LogError("The reef environment needs a region, camera and directional light.", this);
                enabled = false;
                return;
            }

            clock = new EnvironmentClock(region.UtcOffset);
            var localTime = clock.GetLocalTime(DateTimeOffset.UtcNow, Time.unscaledTimeAsDouble);
            cloudCover = EnvironmentModel.SimulatedCloudCover(localTime, weatherSeed);
            Refresh();
        }

        private void Update()
        {
            sampleTimer += Time.unscaledDeltaTime;
            if (sampleTimer < 0.12f)
                return;

            var localTime = clock.GetLocalTime(DateTimeOffset.UtcNow, Time.unscaledTimeAsDouble);
            var desiredCloudCover = WeatherOverride.HasValue
                ? EnvironmentModel.WeatherCloudCover(WeatherOverride.Value)
                : EnvironmentModel.SimulatedCloudCover(localTime, weatherSeed);
            cloudCover = Mathf.MoveTowards(cloudCover, desiredCloudCover, sampleTimer * 0.32f);
            sampleTimer = 0f;
            Refresh();
        }

        public void SetTimeMode(ClockMode mode)
        {
            clock.SetMode(mode, DateTimeOffset.UtcNow, Time.unscaledTimeAsDouble);
            Refresh();
        }

        public void RestoreFixedTime(DateTimeOffset time)
        {
            clock.RestoreFixedTime(time);
            Refresh();
        }

        public void AdvanceFixedHour()
        {
            clock.AdvanceFixedHours(1);
            Refresh();
        }

        public void SetSeason(Season? season)
        {
            SeasonOverride = season;
            Refresh();
        }

        public void SetWeather(Weather? weather)
        {
            WeatherOverride = weather;
            Refresh();
        }

        public EnvironmentSnapshot SampleAt(Vector3 position)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var localTime = clock.GetLocalTime(utcNow, Time.unscaledTimeAsDouble);
            var actualLocalTime = utcNow.ToOffset(region.UtcOffset);
            return EnvironmentModel.Sample(region, position, localTime,
                SeasonOverride ?? EnvironmentClock.SouthernSeason(actualLocalTime),
                WeatherOverride ?? EnvironmentModel.WeatherAt(cloudCover), cloudCover);
        }

        private void Refresh()
        {
            var snapshot = SampleAt(viewer.transform.position);
            ViewerSnapshot = snapshot;
            var litWater = new Color(0.075f, 0.32f, 0.40f);
            var deepWater = new Color(0.008f, 0.028f, 0.057f);
            var water = Color.Lerp(deepWater, litWater, Mathf.Sqrt(snapshot.LightFactor));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = water;
            RenderSettings.fogDensity = Mathf.Lerp(0.037f, 0.017f,
                Mathf.Clamp01(snapshot.LightFactor * (1.2f - snapshot.CloudCover * 0.45f)));
            RenderSettings.ambientLight = Color.Lerp(new Color(0.12f, 0.20f, 0.28f),
                new Color(0.38f, 0.58f, 0.64f), Mathf.Sqrt(snapshot.LightFactor));
            viewer.backgroundColor = water;
            var sunAngle = Mathf.Lerp(18f, 67f, EnvironmentModel.Daylight(snapshot.LocalTime));
            sun.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            sun.intensity = Mathf.Lerp(0.10f, 1.35f, Mathf.Sqrt(snapshot.LightFactor));
        }
    }
}
