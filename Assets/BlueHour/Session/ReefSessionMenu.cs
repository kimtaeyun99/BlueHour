using System;
using System.IO;
using BlueHour.Localization;
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
        private int textSize;
        private Vector2 panelScroll;
        private bool initialMenuScheduled = true;
        private SessionStateFile<ReefSettings> settingsFile;
        private SessionStateFile<ReefExperience> experienceFile;
        private SwimMotor motor;
        private Font menuFont;
        private GUIStyle panelLabel;
        private readonly System.Collections.Generic.Dictionary<string[], string[]> translatedLabels = new();
        private static readonly string[] TextSizes = { "Normal", "Large", "Extra large" };
        private static readonly float[] TextScales = { 1f, 1.15f, 1.30f };
        private static readonly string[] Languages = { "한국어", "English" };
        private string lastSettings;
        private string lastExperience;
        private string saveWarning;
        private float nextSave;
        private int systemVSync;
        private static readonly int[] TimerMinutes = { 0, 5, 15, 30 };
        private static readonly int[] FrameLimits = { -1, 30, 60, 120 };
        private static readonly string[] TimerLabels = { "Off", "5 min", "15 min", "30 min" };
        private static readonly string[] FrameLabels = { "System", "30", "60", "120" };
        private static readonly string[] ViewingLabels = { "Manual", "Fixed", "Route", "Follow" };
        private static readonly string[] WeatherLabels = { "Auto", "Clear", "Overcast", "Rain" };
        private static readonly string[] SoundLabels = { "Water", "Ambient music", "Alternate", "Mute" };
        private static readonly string[] QualityLabels = { "Low", "High" };

        private void Awake()
        {
            menuFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 16);
            motor = viewer.GetComponentInParent<SwimMotor>();
            systemVSync = QualitySettings.vSyncCount;
        }

        private void Start()
        {
            if (Application.isBatchMode)
                return;

            var directory = Path.Combine(Application.persistentDataPath, "SaveData");
            settingsFile = new SessionStateFile<ReefSettings>(directory, "settings");
            experienceFile = new SessionStateFile<ReefExperience>(directory, "experience");
            var settings = settingsFile.Load(ValidSettings);
            var experience = experienceFile.Load(ValidExperience);
            ApplySettings(settings);

            if (experience.regionId == environment.Region.RegionId
                && motor.TryRestorePose(experience.position, experience.yaw, experience.pitch))
                guided.SetMode((ViewingMode)experience.viewingMode);

            saveWarning = settingsFile.Warning ?? experienceFile.Warning;
            nextSave = Time.unscaledTime + 4f;
        }

        private void OnApplicationQuit() => SaveProgress();

        private void OnDestroy()
        {
            if (menuFont != null)
                Destroy(menuFont);
        }

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
            if (settingsFile != null && Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 4f;
                SaveProgress();
            }
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
            if (panel != next)
                panelScroll = Vector2.zero;
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
            var matrix = GUI.matrix;
            var previousFont = GUI.skin.font;
            var scale = ReefLanguage.UiScale;
            var viewportWidth = Screen.width / scale;
            var viewportHeight = Screen.height / scale;
            GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (menuFont != null)
                GUI.skin.font = menuFont;
            panelLabel ??= new GUIStyle(GUI.skin.label)
            {
                fixedHeight = 0f, wordWrap = true, padding = new RectOffset(5, 5, 4, 5)
            };

            try
            {
                if (panel == Panel.Closed)
                {
                    if (!hideAll && showDepth && !book.IsBusy)
                        GUI.Box(new Rect(viewportWidth - 240f, 17f, 220f, 38f), CurrentLocation());
                    return;
                }

                var width = Mathf.Min(panel == Panel.Map ? 560f : 470f, viewportWidth - 26f);
                var left = (viewportWidth - width) * 0.5f;
                GUILayout.BeginArea(new Rect(left, 18f, width, viewportHeight - 36f), GUI.skin.box);
                panelScroll = GUILayout.BeginScrollView(panelScroll);
                switch (panel)
                {
                    case Panel.Menu: DrawMenu(); break;
                    case Panel.Settings: DrawSettings(); break;
                    case Panel.Map: DrawMap(viewportHeight); break;
                    case Panel.Quit: DrawQuit(); break;
                }
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            finally
            {
                GUI.skin.font = previousFont;
                GUI.matrix = matrix;
            }
        }

        private void Label(string value)
        {
            GUILayout.Label(value, panelLabel, GUILayout.MinHeight(25f));
        }

        private void DrawMenu()
        {
            Label("Blue Hour");
            Label(CurrentLocation());
            if (!string.IsNullOrEmpty(saveWarning)) Label(saveWarning);
            var mode = GUILayout.SelectionGrid((int)guided.Mode, Localized(ViewingLabels), 4);
            if ((ViewingMode)mode != guided.Mode)
                guided.SetMode((ViewingMode)mode);
            if (guided.Mode != ViewingMode.Manual)
            {
                Label($"Viewing speed · {guided.Speed:F1} m/s");
                guided.Speed = GUILayout.HorizontalSlider(guided.Speed, 0.6f, 4.0f);
                var next = GUILayout.SelectionGrid(timerIndex, Localized(TimerLabels), 4);
                if (next != timerIndex)
                {
                    timerIndex = next;
                    guided.SetTimer(TimerMinutes[next] * 60f);
                }
                if (guided.TimerRemaining > 0f)
                    Label($"Timer · {Mathf.CeilToInt(guided.TimerRemaining)} s");
                if (timerIndex != 0 && guided.TimerRemaining == 0f && GUILayout.Button(ReefLanguage.Text("Restart timer")))
                    guided.SetTimer(TimerMinutes[timerIndex] * 60f);
            }

            if (GUILayout.Button(ReefLanguage.Text("Resume")))
                ChangePanel(Panel.Closed);
            if (GUILayout.Button(ReefLanguage.Text("Map")))
                ChangePanel(Panel.Map);
            if (GUILayout.Button(ReefLanguage.Text("Settings")))
                ChangePanel(Panel.Settings);
            if (GUILayout.Button(ReefLanguage.Text("Field guide")))
            {
                ChangePanel(Panel.Closed);
                book.OpenGuide();
            }
            if (GUILayout.Button(ReefLanguage.Text("Journal")))
            {
                ChangePanel(Panel.Closed);
                book.OpenJournal();
            }
            if (GUILayout.Button(ReefLanguage.Text("Album")))
            {
                ChangePanel(Panel.Closed);
                book.OpenAlbum();
            }
            if (GUILayout.Button(ReefLanguage.Text("Exit Blue Hour")))
                ChangePanel(Panel.Quit);
        }

        private void DrawSettings()
        {
            Label(ReefLanguage.Text("Settings"));
            var language = GUILayout.SelectionGrid(ReefLanguage.IsKorean ? 0 : 1, Languages, 2);
            ReefLanguage.IsKorean = language == 0;
            Label(ReefLanguage.Text("Text size"));
            var nextTextSize = GUILayout.SelectionGrid(textSize, Localized(TextSizes), 3);
            if (nextTextSize != textSize)
                SetTextSize(nextTextSize);
            showDepth = GUILayout.Toggle(showDepth, ReefLanguage.Text("Depth and habitat HUD"));
            showFocus = GUILayout.Toggle(showFocus, ReefLanguage.Text("Species focus HUD"));
            showEnvironment = GUILayout.Toggle(showEnvironment, ReefLanguage.Text("Environment panel"));
            hideAll = GUILayout.Toggle(hideAll, ReefLanguage.Text("Hide all HUD"));
            UpdateHud();

            Label(ReefLanguage.Text("Water effects"));
            var quality = GUILayout.SelectionGrid((int)waterVisual.Quality, Localized(QualityLabels), 2);
            if (quality != (int)waterVisual.Quality)
                waterVisual.SetQuality((WaterQuality)quality);
            Label(ReefLanguage.Text("Frame limit"));
            var nextFrame = GUILayout.SelectionGrid(frameIndex, Localized(FrameLabels), 4);
            if (nextFrame != frameIndex)
            {
                frameIndex = nextFrame;
                SetFrameLimit();
            }
            Label($"Ambience · {waterAudio.AmbienceVolume:P0}");
            waterAudio.SetAmbienceVolume(GUILayout.HorizontalSlider(waterAudio.AmbienceVolume, 0f, 1f));
            Label($"Music · {waterAudio.MusicVolume:P0}");
            waterAudio.SetMusicVolume(GUILayout.HorizontalSlider(waterAudio.MusicVolume, 0f, 1f));
            waterAudio.SetMode((WaterAudioMode)GUILayout.SelectionGrid((int)waterAudio.Mode, Localized(SoundLabels), 2));

            var weather = environment.WeatherOverride.HasValue ? (int)environment.WeatherOverride.Value + 1 : 0;
            var selection = GUILayout.SelectionGrid(weather, Localized(WeatherLabels), 4);
            if (selection != weather)
                environment.SetWeather(selection == 0 ? (Weather?)null : (Weather)(selection - 1));

            if (GUILayout.Button(ReefLanguage.Text("Back")))
                OpenMenu();
        }

        private void DrawMap(float viewportHeight)
        {
            Label((ReefLanguage.IsKorean ? environment.Region.KoreanName : environment.Region.EnglishName) + " · " + ReefLanguage.Text("north up"));
            Label(CurrentLocation());
            if (!string.IsNullOrEmpty(saveWarning)) Label(saveWarning);
            var map = GUILayoutUtility.GetRect(320f, Mathf.Clamp(viewportHeight - 176f, 160f, 370f), GUILayout.ExpandWidth(true));
            var bounds = environment.Region.SwimBounds;
            var regionMin = bounds.min;
            foreach (var zone in environment.Region.Habitats)
            {
                var x1 = map.xMin + (zone.Min.x - regionMin.x) / bounds.size.x * map.width;
                var x2 = map.xMin + (zone.Max.x - regionMin.x) / bounds.size.x * map.width;
                var y1 = map.yMax - (zone.Max.y - regionMin.z) / bounds.size.z * map.height;
                var y2 = map.yMax - (zone.Min.y - regionMin.z) / bounds.size.z * map.height;
                GUI.Box(Rect.MinMaxRect(x1, y1, x2, y2), ReefLanguage.IsKorean ? zone.KoreanName : zone.EnglishName);
            }
            var position = viewer.transform.position;
            var px = map.xMin + (position.x - regionMin.x) / bounds.size.x * map.width;
            var py = map.yMax - (position.z - regionMin.z) / bounds.size.z * map.height;
            GUI.Label(new Rect(px - 5f, py - 11f, 22f, 24f), "●");
            if (GUILayout.Button(ReefLanguage.Text("Back")))
                OpenMenu();
        }

        private void DrawQuit()
        {
            Label(ReefLanguage.Text("Exit Blue Hour?"));
            if (GUILayout.Button(ReefLanguage.Text("Exit")))
                Application.Quit();
            if (GUILayout.Button(ReefLanguage.Text("Cancel")))
                OpenMenu();
        }

        private string[] Localized(string[] labels)
        {
            if (!ReefLanguage.IsKorean)
                return labels;
            if (!translatedLabels.TryGetValue(labels, out var translated))
            {
                translated = Array.ConvertAll(labels, ReefLanguage.Text);
                translatedLabels.Add(labels, translated);
            }
            return translated;
        }

        private void SetTextSize(int index)
        {
            textSize = index;
            ReefLanguage.UiScale = TextScales[textSize];
        }

        private void SetFrameLimit()
        {
            Application.targetFrameRate = FrameLimits[frameIndex];
            QualitySettings.vSyncCount = frameIndex == 0 ? systemVSync : 0;
        }

        private void ApplySettings(ReefSettings settings)
        {
            ReefLanguage.IsKorean = settings.korean;
            SetTextSize(settings.textSize);
            showDepth = settings.showDepth;
            showFocus = settings.showFocus;
            showEnvironment = settings.showEnvironment;
            hideAll = settings.hideAll;
            frameIndex = settings.frameIndex;
            SetFrameLimit();
            waterVisual.SetQuality((WaterQuality)settings.quality);
            waterAudio.SetMode((WaterAudioMode)settings.soundMode);
            waterAudio.SetAmbienceVolume(settings.ambience);
            waterAudio.SetMusicVolume(settings.music);
            guided.Speed = settings.viewingSpeed;
            environment.SetSeason(settings.season < 0 ? (Season?)null : (Season)settings.season);
            environment.SetWeather(settings.weather < 0 ? (Weather?)null : (Weather)settings.weather);
            if (settings.timeMode == (int)ClockMode.Fixed && DateTimeOffset.TryParse(settings.fixedLocalTime, out var fixedTime))
                environment.RestoreFixedTime(fixedTime);
            else
                environment.SetTimeMode((ClockMode)settings.timeMode);
            UpdateHud();
        }

        private void SaveProgress()
        {
            if (settingsFile == null || experienceFile == null)
                return;

            try
            {
                var settings = new ReefSettings
                {
                    korean = ReefLanguage.IsKorean, textSize = textSize, showDepth = showDepth, showFocus = showFocus,
                    showEnvironment = showEnvironment, hideAll = hideAll, quality = (int)waterVisual.Quality,
                    frameIndex = frameIndex, soundMode = (int)waterAudio.Mode, ambience = waterAudio.AmbienceVolume,
                    music = waterAudio.MusicVolume, viewingSpeed = guided.Speed,
                    weather = environment.WeatherOverride.HasValue ? (int)environment.WeatherOverride.Value : -1,
                    season = environment.SeasonOverride.HasValue ? (int)environment.SeasonOverride.Value : -1,
                    timeMode = (int)environment.TimeMode,
                    fixedLocalTime = environment.TimeMode == ClockMode.Fixed ? environment.ViewerSnapshot.LocalTime.ToString("O") : null
                };
                var experience = new ReefExperience
                {
                    regionId = environment.Region.RegionId, position = motor.transform.position,
                    yaw = motor.transform.eulerAngles.y, pitch = motor.ViewPitch, viewingMode = (int)guided.Mode
                };
                SaveChanged(settingsFile, settings, ValidSettings, ref lastSettings);
                SaveChanged(experienceFile, experience, ValidExperience, ref lastExperience);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                saveWarning = exception.Message;
            }
        }

        private static void SaveChanged<T>(SessionStateFile<T> file, T state, Func<T, bool> validate,
            ref string previous) where T : VersionedState, new()
        {
            var signature = JsonUtility.ToJson(state);
            if (signature == previous)
                return;
            file.Save(state, validate);
            previous = signature;
        }

        private static bool ValidSettings(ReefSettings value) =>
            value.textSize >= 0 && value.textSize < TextScales.Length && value.quality >= 0 && value.quality <= 1 && value.frameIndex >= 0 && value.frameIndex < FrameLimits.Length
            && value.soundMode >= 0 && value.soundMode <= 3 && value.ambience >= 0f && value.ambience <= 1f
            && value.music >= 0f && value.music <= 1f && value.viewingSpeed >= 0.6f && value.viewingSpeed <= 4f
            && value.season >= -1 && value.season <= 3 && value.weather >= -1 && value.weather <= 2
            && value.timeMode >= 0 && value.timeMode <= 2;

        private static bool ValidExperience(ReefExperience value) =>
            !string.IsNullOrWhiteSpace(value.regionId) && float.IsFinite(value.position.x)
            && float.IsFinite(value.position.y) && float.IsFinite(value.position.z)
            && float.IsFinite(value.yaw) && float.IsFinite(value.pitch)
            && value.viewingMode >= 0 && value.viewingMode <= 3;

        private string CurrentLocation()
        {
            var sample = environment.ViewerSnapshot;
            var habitat = ReefLanguage.IsKorean ? sample.Habitat?.KoreanName : sample.Habitat?.EnglishName;
            return $"{habitat} · {sample.DepthM:F1} m";
        }
    }
}
