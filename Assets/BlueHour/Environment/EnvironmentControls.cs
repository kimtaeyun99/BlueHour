using BlueHour.Localization;
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
        private Font font;
        public bool DisplaySuppressed { get; set; }
        public bool SessionHudHidden { get; set; }

        private void Awake()
        {
            environment = GetComponent<ReefEnvironment>();
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 14);
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
            var zone = (ReefLanguage.IsKorean ? sample.Habitat?.KoreanName : sample.Habitat?.EnglishName) ?? ReefLanguage.Text("Outside region");
            var clockMode = environment.TimeMode == ClockMode.Accelerated ? "60x" : ReefLanguage.Text(environment.TimeMode.ToString());
            var seasonMode = environment.SeasonOverride.HasValue ? "manual" : "local";
            var weatherMode = environment.WeatherOverride.HasValue ? "manual" : "simulated";
            readout = $"{zone}   |   {sample.DepthM:F1} m {ReefLanguage.Text("depth")}\n"
                + $"{ReefLanguage.Text("Queensland")} {sample.LocalTime:dd MMM HH:mm} ({clockMode})\n"
                + $"{ReefLanguage.Text(sample.Season.ToString())} ({ReefLanguage.Text(seasonMode)})  /  {ReefLanguage.Text(sample.Weather.ToString())} ({ReefLanguage.Text(weatherMode)})\n"
                + $"{sample.TemperatureC:F1} °C  |  {ReefLanguage.Text("Salinity")} {sample.PracticalSalinity:F2}\n"
                + $"{sample.AbsolutePressurePa / 1000f:F0} kPa {ReefLanguage.Text("absolute")}  |  {ReefLanguage.Text("Light")} {sample.LightFactor:P0}\n"
                + $"{ReefLanguage.Text("Current")} X {sample.CurrentMps.x:+0.00;-0.00;0.00}, Z {sample.CurrentMps.z:+0.00;-0.00;0.00} m/s\n\n"
                + ReefLanguage.Text("F1 Close  ·  F2 Clock  ·  F3 Fixed hour") + "\n" + ReefLanguage.Text("F4 Season  ·  F5 Weather");
        }

        private void OnGUI()
        {
            if (!visible || DisplaySuppressed || SessionHudHidden)
                return;
            var previous = GUI.skin.font;
            var matrix = GUI.matrix;
            GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(ReefLanguage.UiScale, ReefLanguage.UiScale, 1f));
            if (font != null)
                GUI.skin.font = font;
            GUI.Box(new Rect(20f, 20f, 400f, 190f), readout);
            GUI.skin.font = previous;
            GUI.matrix = matrix;
        }

        private void OnDestroy()
        {
            if (font != null)
                Destroy(font);
        }
    }
}
