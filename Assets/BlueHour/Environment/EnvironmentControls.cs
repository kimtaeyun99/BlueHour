using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueHour.Environment
{
    [RequireComponent(typeof(ReefEnvironment))]
    public sealed class EnvironmentControls : MonoBehaviour
    {
        private static readonly Season?[] Seasons = { null, Season.Summer, Season.Autumn, Season.Winter, Season.Spring };
        private static readonly Weather?[] WeatherModes = { null, Weather.Clear, Weather.Overcast, Weather.Rain };

        private ReefEnvironment environment;
        private int seasonIndex;
        private int weatherIndex;
        private bool visible;
        private float refreshAt;
        private string readout;

        private void Awake()
        {
            environment = GetComponent<ReefEnvironment>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!environment.enabled || keyboard == null || !Application.isFocused)
                return;

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                visible = !visible;
                refreshAt = 0f;
            }

            if (!visible)
                return;

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                environment.SetTimeMode((ClockMode)(((int)environment.TimeMode + 1) % 3));
                refreshAt = 0f;
            }
            if (keyboard.f3Key.wasPressedThisFrame)
            {
                environment.AdvanceFixedHour();
                refreshAt = 0f;
            }
            if (keyboard.f4Key.wasPressedThisFrame)
            {
                seasonIndex = (seasonIndex + 1) % Seasons.Length;
                environment.SetSeason(Seasons[seasonIndex]);
                refreshAt = 0f;
            }
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                weatherIndex = (weatherIndex + 1) % WeatherModes.Length;
                environment.SetWeather(WeatherModes[weatherIndex]);
                refreshAt = 0f;
            }

            if (Time.unscaledTime < refreshAt)
                return;

            refreshAt = Time.unscaledTime + 0.25f;
            var sample = environment.ViewerSnapshot;
            var zone = sample.Habitat?.EnglishName ?? "Outside region";
            var clockMode = environment.TimeMode == ClockMode.Accelerated ? "60x" : environment.TimeMode.ToString();
            var seasonMode = environment.SeasonOverride.HasValue ? "manual" : "local";
            var weatherMode = environment.WeatherOverride.HasValue ? "manual" : "simulated";
            readout = $"{zone}   |   {sample.DepthM:F1} m depth\n"
                + $"Queensland {sample.LocalTime:dd MMM HH:mm} ({clockMode})\n"
                + $"{sample.Season} ({seasonMode})  /  {sample.Weather} ({weatherMode})\n"
                + $"{sample.TemperatureC:F1} °C  |  Salinity {sample.PracticalSalinity:F2}\n"
                + $"{sample.AbsolutePressurePa / 1000f:F0} kPa absolute  |  Light {sample.LightFactor:P0}\n"
                + $"Current X {sample.CurrentMps.x:+0.00;-0.00;0.00}, Z {sample.CurrentMps.z:+0.00;-0.00;0.00} m/s\n\n"
                + "F1 Close  ·  F2 Clock  ·  F3 Fixed hour\nF4 Season  ·  F5 Weather";
        }

        private void OnGUI()
        {
            if (visible)
                GUI.Box(new Rect(20f, 20f, 368f, 181f), readout);
        }
    }
}
