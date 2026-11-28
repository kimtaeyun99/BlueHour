using BlueHour.Environment;
using BlueHour.Experience;
using BlueHour.Observation;
using BlueHour.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueHour.Session
{
    public sealed class ReefSessionMenu : MonoBehaviour
    {
        [SerializeField] private GuidedSwim guided;
        [SerializeField] private SwimInput swimmer;
        [SerializeField] private Camera viewer;
        [SerializeField] private ReefEnvironment environment;
        [SerializeField] private EnvironmentControls environmentHud;
        [SerializeField] private ProceduralWaterAudio waterAudio;
        [SerializeField] private UnderwaterAtmosphere waterVisual;
        [SerializeField] private ReefFieldBook book;

        private enum Panel { Closed, Menu, Map, Settings, Quit }
        private Panel panel;
        private bool hideAll;
        private bool showFocus = true;
        private bool showEnvironment = true;
        private bool showDepth;
        private int timerIndex;
        private int frameIndex;
        private bool initialMenuScheduled = true;
        private static readonly int[] TimerMinutes = { 0, 5, 15, 30 };
        private static readonly int[] FrameLimits = { -1, 30, 60, 120 };
        private static readonly string[] TimerLabels = { "Off", "5 min", "15 min", "30 min" };
        private static readonly string[] FrameLabels = { "System", "30", "60", "120" };
        private static readonly string[] ViewingLabels = { "Manual", "Fixed", "Route", "Follow" };
        private static readonly string[] WeatherLabels = { "Auto", "Clear", "Overcast", "Rain" };
        private static readonly string[] SoundLabels = { "Water", "Ambient music", "Alternate", "Mute" };
        private static readonly string[] QualityLabels = { "Low", "High" };

        private void OnEnable() => guided.TimerExpired += OpenMenu;
        private void OnDisable()
        {
            guided.TimerExpired -= OpenMenu;
            ChangePanel(Panel.Closed);
        }

        private void Update()
        {
            if (initialMenuScheduled)
            {
                initialMenuScheduled = false;
                if (!Application.isBatchMode)
                    OpenMenu();
            }

            guided.Paused = panel != Panel.Closed || book.IsBusy;
            var key = Keyboard.current;
            if (key == null || !Application.isFocused || book.IsBusy)
                return;

            if (key.escapeKey.wasPressedThisFrame)
            {
                if (panel != Panel.Closed)
                    ChangePanel(Panel.Closed);
                else if (guided.Mode != ViewingMode.Manual)
                    guided.SetMode(ViewingMode.Manual);
                else
                    OpenMenu();
                return;
            }
            if (key.mKey.wasPressedThisFrame)
                ChangePanel(panel == Panel.Map ? Panel.Closed : Panel.Map);
            if (key.hKey.wasPressedThisFrame)
            {
                hideAll = !hideAll;
                UpdateHud();
            }

            if (panel == Panel.Closed && guided.Mode != ViewingMode.Manual
                && (key.wKey.wasPressedThisFrame || key.aKey.wasPressedThisFrame || key.sKey.wasPressedThisFrame
                    || key.dKey.wasPressedThisFrame || key.spaceKey.wasPressedThisFrame
                    || key.leftCtrlKey.wasPressedThisFrame || key.rightCtrlKey.wasPressedThisFrame))
                guided.SetMode(ViewingMode.Manual);
        }

        private void OpenMenu() => ChangePanel(Panel.Menu);

        private void ChangePanel(Panel next)
        {
            panel = next;
            var open = panel != Panel.Closed;
            guided.Paused = open;
            swimmer.BlockForPanel(open);
            book.SessionPanelOpen = open;
            UpdateHud();
        }

        private void UpdateHud()
        {
            environmentHud.SessionHudHidden = hideAll || !showEnvironment || panel != Panel.Closed;
            book.FocusHudHidden = hideAll || !showFocus;
        }

        private void OnGUI()
        {
            if (panel == Panel.Closed)
            {
                if (!hideAll && showDepth && !book.IsBusy)
                    GUI.Box(new Rect(Screen.width - 240f, 17f, 220f, 34f), CurrentLocation());
                return;
            }

            var width = panel == Panel.Map ? 560f : 460f;
            width = Mathf.Min(width, Screen.width - 32f);
            var left = (Screen.width - width) * 0.5f;
            GUILayout.BeginArea(new Rect(left, 22f, width, Screen.height - 44f), GUI.skin.box);
            switch (panel)
            {
                case Panel.Menu: DrawMenu(); break;
                case Panel.Settings: DrawSettings(); break;
                case Panel.Map: DrawMap(); break;
                case Panel.Quit: DrawQuit(); break;
            }
            GUILayout.EndArea();
        }

        private void DrawMenu()
        {
            GUILayout.Label("Blue Hour");
            GUILayout.Label(CurrentLocation());
            var mode = GUILayout.SelectionGrid((int)guided.Mode, ViewingLabels, 4);
            if ((ViewingMode)mode != guided.Mode)
                guided.SetMode((ViewingMode)mode);
            if (guided.Mode != ViewingMode.Manual)
            {
                GUILayout.Label($"Viewing speed · {guided.Speed:F1} m/s");
                guided.Speed = GUILayout.HorizontalSlider(guided.Speed, 0.6f, 4.0f);
                var next = GUILayout.SelectionGrid(timerIndex, TimerLabels, 4);
                if (next != timerIndex)
                {
                    timerIndex = next;
                    guided.SetTimer(TimerMinutes[next] * 60f);
                }
                if (guided.TimerRemaining > 0f)
                    GUILayout.Label($"Timer · {Mathf.CeilToInt(guided.TimerRemaining)} s");
                if (timerIndex != 0 && guided.TimerRemaining == 0f && GUILayout.Button("Restart timer"))
                    guided.SetTimer(TimerMinutes[timerIndex] * 60f);
            }

            if (GUILayout.Button("Resume"))
                ChangePanel(Panel.Closed);
            if (GUILayout.Button("Map"))
                ChangePanel(Panel.Map);
            if (GUILayout.Button("Settings"))
                ChangePanel(Panel.Settings);
            if (GUILayout.Button("Field guide"))
            {
                ChangePanel(Panel.Closed);
                book.OpenGuide();
            }
            if (GUILayout.Button("Journal"))
            {
                ChangePanel(Panel.Closed);
                book.OpenJournal();
            }
            if (GUILayout.Button("Album"))
            {
                ChangePanel(Panel.Closed);
                book.OpenAlbum();
            }
            if (GUILayout.Button("Exit Blue Hour"))
                ChangePanel(Panel.Quit);
        }

        private void DrawSettings()
        {
            GUILayout.Label("Settings");
            showDepth = GUILayout.Toggle(showDepth, "Depth and habitat HUD");
            showFocus = GUILayout.Toggle(showFocus, "Species focus HUD");
            showEnvironment = GUILayout.Toggle(showEnvironment, "Environment panel");
            hideAll = GUILayout.Toggle(hideAll, "Hide all HUD");
            UpdateHud();

            GUILayout.Label("Water effects");
            var quality = GUILayout.SelectionGrid((int)waterVisual.Quality, QualityLabels, 2);
            if (quality != (int)waterVisual.Quality)
                waterVisual.SetQuality((WaterQuality)quality);
            GUILayout.Label("Frame limit");
            var nextFrame = GUILayout.SelectionGrid(frameIndex, FrameLabels, 4);
            if (nextFrame != frameIndex)
            {
                frameIndex = nextFrame;
                Application.targetFrameRate = FrameLimits[nextFrame];
            }
            GUILayout.Label($"Ambience · {waterAudio.AmbienceVolume:P0}");
            waterAudio.SetAmbienceVolume(GUILayout.HorizontalSlider(waterAudio.AmbienceVolume, 0f, 1f));
            GUILayout.Label($"Music · {waterAudio.MusicVolume:P0}");
            waterAudio.SetMusicVolume(GUILayout.HorizontalSlider(waterAudio.MusicVolume, 0f, 1f));
            waterAudio.SetMode((WaterAudioMode)GUILayout.SelectionGrid((int)waterAudio.Mode, SoundLabels, 2));

            var weather = environment.WeatherOverride.HasValue ? (int)environment.WeatherOverride.Value + 1 : 0;
            var selection = GUILayout.SelectionGrid(weather, WeatherLabels, 4);
            if (selection != weather)
                environment.SetWeather(selection == 0 ? (Weather?)null : (Weather)(selection - 1));

            if (GUILayout.Button("Back"))
                OpenMenu();
        }

        private void DrawMap()
        {
            GUILayout.Label(environment.Region.EnglishName + " · north up");
            GUILayout.Label(CurrentLocation());
            var map = GUILayoutUtility.GetRect(320f, Mathf.Min(380f, Screen.height - 160f), GUILayout.ExpandWidth(true));
            var bounds = environment.Region.SwimBounds;
            var regionMin = bounds.min;
            foreach (var zone in environment.Region.Habitats)
            {
                var x1 = map.xMin + (zone.Min.x - regionMin.x) / bounds.size.x * map.width;
                var x2 = map.xMin + (zone.Max.x - regionMin.x) / bounds.size.x * map.width;
                var y1 = map.yMax - (zone.Max.y - regionMin.z) / bounds.size.z * map.height;
                var y2 = map.yMax - (zone.Min.y - regionMin.z) / bounds.size.z * map.height;
                GUI.Box(Rect.MinMaxRect(x1, y1, x2, y2), zone.EnglishName);
            }
            var position = viewer.transform.position;
            var px = map.xMin + (position.x - regionMin.x) / bounds.size.x * map.width;
            var py = map.yMax - (position.z - regionMin.z) / bounds.size.z * map.height;
            GUI.Label(new Rect(px - 5f, py - 11f, 22f, 24f), "●");
            if (GUILayout.Button("Back"))
                OpenMenu();
        }

        private void DrawQuit()
        {
            GUILayout.Label("Exit Blue Hour?");
            if (GUILayout.Button("Exit"))
                Application.Quit();
            if (GUILayout.Button("Cancel"))
                OpenMenu();
        }

        private string CurrentLocation()
        {
            var sample = environment.ViewerSnapshot;
            return $"{sample.Habitat?.EnglishName} · {sample.DepthM:F1} m";
        }
    }
}
