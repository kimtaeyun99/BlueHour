using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BlueHour.Core;
using UnityEngine;

namespace BlueHour.Observation
{
    [Serializable]
    public sealed class ObservationEntry
    {
        public string speciesId;
        public string firstSeenUtc;
        public string regionId;
        public float depthM;
        public string localTimeOfDay;
    }

    [Serializable]
    public sealed class JournalNote
    {
        public string noteId;
        public string speciesId;
        public string text;
        public string createdAt;
        public string updatedAt;
        public string[] photoIds;
    }

    [Serializable]
    public sealed class AlbumPhoto
    {
        public string photoId;
        public string path;
        public string capturedAt;
        public string regionId;
        public float depthM;
        public string[] speciesIds;
        public bool favorite;
    }

    public sealed class ReefRecords
    {
        private const int Schema = 1;

        [Serializable]
        private sealed class DataFile<T> where T : class
        {
            public int schemaVersion;
            public string savedAtUtc;
            public List<T> items;
        }

        private sealed class FutureSchemaException : IOException
        {
            public FutureSchemaException() : base("This record was saved by a newer version of Blue Hour.") { }
        }

        private readonly string root;
        private readonly HashSet<string> readOnlyFiles = new HashSet<string>(StringComparer.Ordinal);
        private List<ObservationEntry> observations;
        private List<JournalNote> notes;
        private List<AlbumPhoto> photos;

        public ReefRecords(string path)
        {
            root = Path.GetFullPath(path);
            observations = Read<ObservationEntry>("encyclopedia");
            notes = Read<JournalNote>("journal");
            photos = Read<AlbumPhoto>("album");
        }

        public IReadOnlyList<ObservationEntry> Observations => observations;
        public IReadOnlyList<JournalNote> Notes => notes;
        public IReadOnlyList<AlbumPhoto> Photos => photos;
        public string LoadWarning { get; private set; }

        public bool RecordFirst(ObservationEntry item)
        {
            if (item == null || !DataId.IsValid(item.speciesId)
                || observations.Exists(value => value.speciesId == item.speciesId))
                return false;

            var updated = new List<ObservationEntry>(observations) { item };
            Write("encyclopedia", updated);
            observations = updated;
            return true;
        }

        public void SaveNote(string noteId, string speciesId, string text, string photoId)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 2000 || !DataId.IsValid(speciesId))
                throw new ArgumentException("A species and a note of 1–2000 characters are required.");
            if (!string.IsNullOrEmpty(photoId) && !photos.Exists(photo => photo.photoId == photoId))
                throw new ArgumentException("The chosen photo is no longer in the album.");

            var updated = new List<JournalNote>(notes);
            var index = updated.FindIndex(note => note.noteId == noteId);
            var now = Timestamp();
            var entry = new JournalNote
            {
                noteId = index < 0 ? Guid.NewGuid().ToString("N") : noteId,
                speciesId = speciesId,
                text = text.Trim(),
                createdAt = index < 0 ? now : updated[index].createdAt,
                updatedAt = now,
                photoIds = string.IsNullOrEmpty(photoId) ? Array.Empty<string>() : new[] { photoId }
            };

            if (index < 0)
                updated.Add(entry);
            else
                updated[index] = entry;

            Write("journal", updated);
            notes = updated;
        }

        public void DeleteNote(string noteId)
        {
            var updated = new List<JournalNote>(notes);
            if (updated.RemoveAll(note => note.noteId == noteId) == 0)
                return;
            Write("journal", updated);
            notes = updated;
        }

        public AlbumPhoto SavePhoto(byte[] png, string region, float depth, string[] speciesIds)
        {
            if (png == null || png.Length < 8 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71)
                throw new ArgumentException("The capture did not produce PNG data.");

            var id = Guid.NewGuid().ToString("N");
            var record = new AlbumPhoto
            {
                photoId = id, path = "Photos/" + id + ".png", capturedAt = Timestamp(),
                regionId = region, depthM = depth, speciesIds = speciesIds ?? Array.Empty<string>()
            };

            var path = PhotoPath(record);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            WriteBytes(path + ".tmp", png);
            File.Move(path + ".tmp", path);
            try
            {
                var updated = new List<AlbumPhoto>(photos) { record };
                Write("album", updated);
                photos = updated;
                return record;
            }
            catch
            {
                File.Delete(path);
                throw;
            }
        }

        public void ToggleFavorite(AlbumPhoto photo)
        {
            var index = photos.FindIndex(item => item.photoId == photo.photoId);
            if (index < 0)
                return;

            var updated = new List<AlbumPhoto>(photos);
            var copy = new AlbumPhoto
            {
                photoId = photo.photoId, path = photo.path, capturedAt = photo.capturedAt, regionId = photo.regionId,
                depthM = photo.depthM, speciesIds = photo.speciesIds, favorite = !photo.favorite
            };
            updated[index] = copy;
            Write("album", updated);
            photos = updated;
        }

        public void DeletePhoto(AlbumPhoto photo)
        {
            var updated = new List<AlbumPhoto>(photos);
            if (updated.RemoveAll(item => item.photoId == photo.photoId) == 0)
                return;
            var path = PhotoPath(photo);
            if (notes.Exists(note => note.photoIds != null && Array.IndexOf(note.photoIds, photo.photoId) >= 0))
                throw new IOException("Remove the photo link from the journal note before deleting the photo.");

            Write("album", updated);
            photos = updated;
            if (File.Exists(path))
            {
                // Retain files while the album backup can still reference them.
                var archive = Path.Combine(root, "Removed Photos");
                Directory.CreateDirectory(archive);
                File.Copy(path, Path.Combine(archive, photo.photoId + ".png"), true);
                File.Delete(path);
            }
        }

        public string ExportPhoto(AlbumPhoto photo)
        {
            var export = Path.Combine(root, "Exports");
            Directory.CreateDirectory(export);
            var destination = Path.Combine(export, "BlueHour-" + photo.photoId + ".png");
            File.Copy(PhotoPath(photo), destination, true);
            return destination;
        }

        public string PhotoPath(AlbumPhoto photo)
        {
            if (!Guid.TryParseExact(photo.photoId, "N", out _) || photo.path != "Photos/" + photo.photoId + ".png")
                throw new IOException("The photo has an invalid local path.");
            var primary = Path.Combine(root, "Photos", photo.photoId + ".png");
            var archived = Path.Combine(root, "Removed Photos", photo.photoId + ".png");
            return !File.Exists(primary) && File.Exists(archived) ? archived : primary;
        }

        private List<T> Read<T>(string key) where T : class
        {
            var primary = Path.Combine(root, key + ".json");
            try
            {
                if (File.Exists(primary))
                    return Parse<T>(primary);
            }
            catch (FutureSchemaException exception)
            {
                readOnlyFiles.Add(key);
                LoadWarning = exception.Message;
                return new List<T>();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                LoadWarning = "Could not open " + key + ". Tried the previous backup.";
            }

            try
            {
                var backup = Path.Combine(root, "Backup", key + ".bak");
                if (File.Exists(backup))
                    return Parse<T>(backup);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                LoadWarning = "Could not recover " + key + ". The original records were preserved.";
                readOnlyFiles.Add(key);
            }
            return new List<T>();
        }

        private static List<T> Parse<T>(string path) where T : class
        {
            var file = JsonUtility.FromJson<DataFile<T>>(File.ReadAllText(path));
            if (file != null && file.schemaVersion > Schema)
                throw new FutureSchemaException();
            if (file == null || file.schemaVersion != Schema || file.items == null || file.items.Exists(item => item == null))
                throw new IOException("Invalid record file.");
            return file.items;
        }

        private void Write<T>(string key, List<T> records) where T : class
        {
            if (readOnlyFiles.Contains(key))
                throw new IOException("This record file is read-only after a load error.");

            Directory.CreateDirectory(root);
            var backup = Path.Combine(root, "Backup");
            Directory.CreateDirectory(backup);
            var path = Path.Combine(root, key + ".json");
            var temporary = path + ".tmp";
            var file = new DataFile<T> { schemaVersion = Schema, savedAtUtc = Timestamp(), items = records };
            var json = JsonUtility.ToJson(file, true);
            WriteBytes(temporary, System.Text.Encoding.UTF8.GetBytes(json));
            Parse<T>(temporary);
            if (File.Exists(path))
                File.Replace(temporary, path, Path.Combine(backup, key + ".bak"));
            else
                File.Move(temporary, path);
        }

        private static void WriteBytes(string path, byte[] bytes)
        {
            using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            output.Write(bytes, 0, bytes.Length);
            output.Flush(true);
        }

        private static string Timestamp()
        {
            return DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
