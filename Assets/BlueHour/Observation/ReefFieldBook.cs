using BlueHour.Localization;
using System;
using System.Collections;
using System.IO;
using BlueHour.Creatures;
using BlueHour.Environment;
using BlueHour.Experience;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueHour.Observation
{
    [RequireComponent(typeof(ReefObservations))]
    public sealed class ReefFieldBook : MonoBehaviour
    {
        private enum Page { Closed, Guide, Journal, Album }
        [SerializeField] private SwimInput swimmer;
        [SerializeField] private EnvironmentControls environmentControls;

        private ReefObservations observations;
        private Page page;
        private int speciesIndex;
        private bool photoMode;
        private bool capturing;
        private float previousFov;
        private bool details;
        private bool favorites;
        private bool filterSpecies;
        private Vector2 scroll;
        private string noteId;
        private string noteText = "";
        private string linkedPhoto;
        private string selectedPhoto;
        private string pendingDelete;
        private string status;
        private Texture2D preview;
        private Font panelFont;
        public bool IsBusy => page != Page.Closed || photoMode || capturing;
        public bool SessionPanelOpen { get; set; }
        public bool FocusHudHidden { get; set; }

        public void OpenGuide() => SetPage(Page.Guide);
        public void OpenJournal() => SetPage(Page.Journal);
        public void OpenAlbum() => SetPage(Page.Album);

        private void Awake()
        {
            observations = GetComponent<ReefObservations>();
            panelFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 16);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            capturing = false;
            ExitPhoto();
            SetPage(Page.Closed);
        }

        private void OnDestroy()
        {
            if (panelFont != null)
                Destroy(panelFont);
            if (preview != null)
                Destroy(preview);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !Application.isFocused || capturing || SessionPanelOpen)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ExitPhoto();
                SetPage(Page.Closed);
                return;
            }

            if (keyboard.pKey.wasPressedThisFrame && page == Page.Closed)
            {
                if (photoMode)
                    ExitPhoto();
                else
                    EnterPhoto();
                return;
            }

            if (photoMode)
            {
                var wheel = Mouse.current == null ? 0f : Mouse.current.scroll.ReadValue().y;
                observations.Viewer.fieldOfView = Mathf.Clamp(observations.Viewer.fieldOfView - wheel * 0.025f, 24f, 75f);
                if (keyboard.enterKey.wasPressedThisFrame)
                    StartCoroutine(Capture());
                return;
            }

            if (page == Page.Closed)
            {
                if (keyboard.bKey.wasPressedThisFrame)
                {
                    SelectFocusedSpecies();
                    SetPage(Page.Guide);
                }
                else if (keyboard.jKey.wasPressedThisFrame)
                    SetPage(Page.Journal);
                else if (keyboard.lKey.wasPressedThisFrame)
                    SetPage(Page.Album);
            }
        }

        private void SelectFocusedSpecies()
        {
            if (observations.FocusSpecies == null)
                return;
            for (var index = 0; index < observations.Catalog.Count; index++)
                if (observations.Catalog[index] == observations.FocusSpecies)
                {
                    speciesIndex = index;
                    return;
                }
        }

        private void SetPage(Page target)
        {
            if (page == target)
                return;

            page = target;
            scroll = Vector2.zero;
            pendingDelete = null;
            observations.Paused = page != Page.Closed;
            swimmer.BlockForPanel(page != Page.Closed);
            if (environmentControls != null)
                environmentControls.DisplaySuppressed = page != Page.Closed || photoMode;
        }

        private void EnterPhoto()
        {
            photoMode = true;
            previousFov = observations.Viewer.fieldOfView;
            if (environmentControls != null)
                environmentControls.DisplaySuppressed = true;
        }

        private void ExitPhoto()
        {
            if (!photoMode)
                return;
            photoMode = false;
            observations.Viewer.fieldOfView = previousFov;
            if (environmentControls != null)
                environmentControls.DisplaySuppressed = page != Page.Closed;
        }

        private IEnumerator Capture()
        {
            capturing = true;
            yield return new WaitForEndOfFrame();
            Texture2D screen = null;
            try
            {
                var environment = observations.Environment;
                var state = environment.SampleAt(observations.Viewer.transform.position);
                var species = observations.SpeciesInFrame();
                screen = ScreenCapture.CaptureScreenshotAsTexture();
                var png = screen != null ? screen.EncodeToPNG() : null;
                observations.Records.SavePhoto(png, environment.Region.RegionId, state.DepthM, species);
                status = ReefLanguage.Text("Photo saved.");
            }
            catch (Exception exception) when (IsRecordError(exception))
            {
                status = exception.Message;
            }
            finally
            {
                if (screen != null)
                    Destroy(screen);
                capturing = false;
            }
        }

        private void OnGUI()
        {
            if (capturing)
                return;

            var previousFont = GUI.skin.font;
            if (panelFont != null)
                GUI.skin.font = panelFont;

            try
            {
                if (photoMode)
                    GUI.Box(new Rect(18f, Screen.height - 65f, 390f, 45f),
                        ReefLanguage.Text("Photo · wheel to zoom · Enter to capture · Esc") + (status == null ? "" : "\n" + status));
                else if (page != Page.Closed)
                    DrawPanel();
                else if (!FocusHudHidden && !SessionPanelOpen && observations.FocusSpecies != null)
                    GUI.Box(new Rect(18f, Screen.height - 60f, 280f, 38f),
                        observations.FocusSpecies.KoreanName + "  ·  B: field guide");
            }
            finally
            {
                GUI.skin.font = previousFont;
            }
        }

        private void DrawPanel()
        {
            var width = Mathf.Min(780f, Screen.width - 32f);
            var height = Mathf.Min(680f, Screen.height - 32f);
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 16f, width, height), GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(page == Page.Guide, ReefLanguage.Text("Field guide"), GUI.skin.button))
                SetPage(Page.Guide);
            if (GUILayout.Toggle(page == Page.Journal, ReefLanguage.Text("Journal"), GUI.skin.button))
                SetPage(Page.Journal);
            if (GUILayout.Toggle(page == Page.Album, ReefLanguage.Text("Album"), GUI.skin.button))
                SetPage(Page.Album);
            var close = GUILayout.Button(ReefLanguage.Text("Close"), GUILayout.Width(80f));
            GUILayout.EndHorizontal();
            if (close)
            {
                GUILayout.EndArea();
                SetPage(Page.Closed);
                return;
            }

            var displayStatus = status ?? observations.Status;
            if (!string.IsNullOrEmpty(displayStatus))
                GUILayout.Label(displayStatus);

            scroll = GUILayout.BeginScrollView(scroll);
            var species = observations.Catalog[speciesIndex];
            switch (page)
            {
                case Page.Guide: DrawGuide(species); break;
                case Page.Journal: DrawJournal(species); break;
                case Page.Album: DrawAlbum(species); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void SpeciesButtons()
        {
            GUILayout.BeginHorizontal();
            for (var index = 0; index < observations.Catalog.Count; index++)
            {
                var label = ReefLanguage.IsKorean ? observations.Catalog[index].KoreanName : observations.Catalog[index].EnglishName;
                if (GUILayout.Toggle(speciesIndex == index, label, GUI.skin.button) && speciesIndex != index)
                {
                    speciesIndex = index;
                    details = false;
                    if (page == Page.Journal)
                        ClearNote();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawGuide(SpeciesDefinition species)
        {
            SpeciesButtons();
            if (species != observations.Catalog[speciesIndex])
                return;

            GUILayout.Label($"{species.KoreanName} · {species.EnglishName}\n{species.ScientificName}");
            GUILayout.Label(ReefLanguage.Describe(species.SpeciesId, species.Sources[0].SupportedClaim));
            var found = FirstObservation(species.SpeciesId);
            GUILayout.Label(found == null ? ReefLanguage.Text("Not observed yet") : FormatObservation(found));
            details = GUILayout.Toggle(details, ReefLanguage.Text("Show evidence and behavior"));
            if (!details)
                return;
            GUILayout.Label($"Behavior: {species.Locomotion}    Response: {species.Response}");
            foreach (var source in species.Sources)
            {
                GUILayout.Label(source.Title + "\n" + source.SupportedClaim);
                GUILayout.TextField(source.Url);
            }
        }

        private void DrawJournal(SpeciesDefinition species)
        {
            SpeciesButtons();
            if (species != observations.Catalog[speciesIndex])
                return;

            var found = FirstObservation(species.SpeciesId);
            GUILayout.Label(found == null ? ReefLanguage.Text("No first observation") : FormatObservation(found));
            foreach (var note in observations.Records.Notes)
            {
                if (note.speciesId != species.SpeciesId)
                    continue;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(note.text.Length > 70 ? note.text.Substring(0, 70) + "…" : note.text))
                {
                    noteId = note.noteId;
                    noteText = note.text;
                    linkedPhoto = note.photoIds != null && note.photoIds.Length > 0 ? note.photoIds[0] : null;
                    pendingDelete = null;
                }
                if (GUILayout.Button(pendingDelete == note.noteId ? ReefLanguage.Text("Confirm") : ReefLanguage.Text("Delete"), GUILayout.Width(85f)))
                {
                    if (pendingDelete == note.noteId && TryRecord(() => observations.Records.DeleteNote(note.noteId), ReefLanguage.Text("Note deleted.")))
                    {
                        if (noteId == note.noteId)
                            ClearNote();
                        pendingDelete = null;
                    }
                    else
                        pendingDelete = note.noteId;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(noteId == null ? ReefLanguage.Text("New note") : ReefLanguage.Text("Edit note"));
            noteText = GUILayout.TextArea(noteText, 2000, GUILayout.MinHeight(95f));
            if (GUILayout.Button(linkedPhoto == null ? ReefLanguage.Text("Attach a photo") : ReefLanguage.Text("Change or remove photo")))
                linkedPhoto = NextPhotoLink(linkedPhoto);
            if (linkedPhoto != null)
            {
                var photo = FindPhoto(linkedPhoto);
                GUILayout.Label(photo == null ? ReefLanguage.Text("Photo unavailable") : "Linked photo · " + ShortDate(photo.capturedAt));
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ReefLanguage.Text("Save note")) && TryRecord(
                () => observations.Records.SaveNote(noteId, species.SpeciesId, noteText, linkedPhoto), ReefLanguage.Text("Note saved.")))
                ClearNote();
            if (GUILayout.Button(ReefLanguage.Text("Clear note")))
                ClearNote();
            GUILayout.EndHorizontal();
        }

        private void DrawAlbum(SpeciesDefinition species)
        {
            favorites = GUILayout.Toggle(favorites, ReefLanguage.Text("Favorites only"));
            filterSpecies = GUILayout.Toggle(filterSpecies, ReefLanguage.Text("Filter by species"));
            if (filterSpecies)
                SpeciesButtons();

            foreach (var photo in observations.Records.Photos)
            {
                if (favorites && !photo.favorite)
                    continue;
                if (filterSpecies && (photo.speciesIds == null || Array.IndexOf(photo.speciesIds, species.SpeciesId) < 0))
                    continue;
                if (GUILayout.Button((photo.favorite ? "★  " : "") + ShortDate(photo.capturedAt) + $"  ·  {photo.depthM:F1} m"))
                    SelectPhoto(photo);
            }

            var selected = FindPhoto(selectedPhoto);
            if (selected == null)
                return;

            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(400f), GUILayout.Height(225f));
            GUILayout.Label($"{selected.regionId} · {selected.depthM:F1} m · {ShortDate(selected.capturedAt)}");
            if (selected.speciesIds != null)
                foreach (var id in selected.speciesIds)
                    foreach (var candidate in observations.Catalog)
                        if (id == candidate.SpeciesId)
                            GUILayout.Label(ReefLanguage.IsKorean ? candidate.KoreanName : candidate.EnglishName);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(selected.favorite ? ReefLanguage.Text("Unfavorite") : ReefLanguage.Text("Favorite")))
                TryRecord(() => observations.Records.ToggleFavorite(selected), null);
            if (GUILayout.Button(ReefLanguage.Text("Export PNG")))
                TryRecord(() => status = "Exported to " + observations.Records.ExportPhoto(selected), null);
            if (GUILayout.Button(pendingDelete == selected.photoId ? ReefLanguage.Text("Confirm deletion") : ReefLanguage.Text("Delete photo")))
            {
                if (pendingDelete == selected.photoId
                    && TryRecord(() => observations.Records.DeletePhoto(selected), ReefLanguage.Text("Photo removed.")))
                    SelectPhoto(null);
                else
                    pendingDelete = selected.photoId;
            }
            GUILayout.EndHorizontal();
        }

        private void SelectPhoto(AlbumPhoto photo)
        {
            if (preview != null)
                Destroy(preview);
            preview = null;
            selectedPhoto = photo?.photoId;
            pendingDelete = null;
            if (photo == null)
                return;
            TryRecord(() =>
            {
                var loaded = new Texture2D(2, 2);
                try
                {
                    if (!loaded.LoadImage(File.ReadAllBytes(observations.Records.PhotoPath(photo)), true))
                        throw new IOException("The photo cannot be decoded.");
                    preview = loaded;
                }
                finally
                {
                    if (preview != loaded)
                        Destroy(loaded);
                }
            }, null);
        }

        private ObservationEntry FirstObservation(string id)
        {
            foreach (var entry in observations.Records.Observations)
                if (entry.speciesId == id)
                    return entry;
            return null;
        }

        private AlbumPhoto FindPhoto(string id)
        {
            if (id == null)
                return null;
            foreach (var photo in observations.Records.Photos)
                if (photo.photoId == id)
                    return photo;
            return null;
        }

        private string NextPhotoLink(string previous)
        {
            var photos = observations.Records.Photos;
            if (photos.Count == 0)
                return null;
            for (var i = 0; i < photos.Count; i++)
                if (photos[i].photoId == previous)
                    return i + 1 < photos.Count ? photos[i + 1].photoId : null;
            return photos[0].photoId;
        }

        private void ClearNote()
        {
            noteId = null;
            noteText = "";
            linkedPhoto = null;
        }

        private bool TryRecord(Action work, string success)
        {
            try
            {
                work();
                if (success != null)
                    status = success;
                return true;
            }
            catch (Exception exception) when (IsRecordError(exception))
            {
                status = exception.Message;
                return false;
            }
        }

        private static bool IsRecordError(Exception exception)
        {
            return exception is IOException or UnauthorizedAccessException or ArgumentException or UnityException;
        }

        private static string ShortDate(string timestamp)
        {
            return DateTimeOffset.TryParse(timestamp, out var value) ? value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'") : timestamp;
        }

        private static string FormatObservation(ObservationEntry entry)
        {
            return $"First seen {ShortDate(entry.firstSeenUtc)} · {entry.regionId} · {entry.depthM:F1} m · {entry.localTimeOfDay}";
        }
    }
}
