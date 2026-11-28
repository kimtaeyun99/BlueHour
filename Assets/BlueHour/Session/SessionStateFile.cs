using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BlueHour.Session
{
    [Serializable]
    public class VersionedState
    {
        public int schemaVersion;
        public string savedAtUtc;
    }

    [Serializable]
    public sealed class ReefSettings : VersionedState
    {
        public bool korean = true;
        public int textSize;
        public bool showDepth;
        public bool showFocus = true;
        public bool showEnvironment = true;
        public bool hideAll;
        public int quality = 1;
        public int frameIndex;
        public int soundMode;
        public float ambience = 0.65f;
        public float music = 0.35f;
        public float viewingSpeed = 2.6f;
        public int weather = -1;
        public int season = -1;
        public int timeMode;
        public string fixedLocalTime;
    }

    [Serializable]
    public sealed class ReefExperience : VersionedState
    {
        public string regionId;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public int viewingMode;
    }

    public sealed class SessionStateFile<T> where T : VersionedState, new()
    {
        private const int Current = 1;
        private readonly string path;
        private readonly string backup;
        private readonly string temporary;
        private bool preserveBackup;

        public SessionStateFile(string directory, string name)
        {
            path = Path.Combine(directory, name + ".json");
            backup = Path.Combine(directory, "Backup", name + ".bak");
            temporary = path + ".tmp";
        }

        public bool ReadOnly { get; private set; }
        public string Warning { get; private set; }

        public T Load(Func<T, bool> validate)
        {
            var recovered = FindValid(path, validate, out var valid);
            if (valid)
                return recovered;
            if (ReadOnly)
                return new T();

            preserveBackup = File.Exists(path);
            recovered = FindValid(backup, validate, out valid);
            if (!valid && !ReadOnly)
                recovered = FindValid(temporary, validate, out valid);
            if (valid)
            {
                Warning = "Recovered previous local settings.";
                return recovered;
            }

            if (preserveBackup)
            {
                ReadOnly = true;
                Warning = "Local data is unreadable. The original file was kept.";
            }
            return new T();
        }

        public void Save(T snapshot, Func<T, bool> validate)
        {
            if (ReadOnly)
                return;
            snapshot.schemaVersion = Current;
            snapshot.savedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            if (!validate(snapshot))
                throw new IOException("Refused to write an invalid local record.");

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(snapshot, true));
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            FindValid(temporary, validate, out var valid);
            if (!valid)
                throw new IOException("Could not verify the local record before saving.");
            if (File.Exists(path))
                File.Replace(temporary, path, preserveBackup ? path + ".damaged" : backup);
            else
                File.Move(temporary, path);
            preserveBackup = false;
        }

        private T FindValid(string candidate, Func<T, bool> validate, out bool valid)
        {
            valid = false;
            if (!File.Exists(candidate))
                return null;

            try
            {
                var json = File.ReadAllText(candidate);
                if (!json.TrimStart().StartsWith("{", StringComparison.Ordinal) || !json.Contains("schemaVersion"))
                    return null;
                var result = new T();
                JsonUtility.FromJsonOverwrite(json, result);
                if (result.schemaVersion > Current)
                {
                    ReadOnly = true;
                    Warning = "This local record was saved by a newer version.";
                    return null;
                }

                // Version zero uses the same fields without an explicit modern schema.
                if (result.schemaVersion == 0)
                    result.schemaVersion = Current;
                valid = result.schemaVersion == Current && validate(result);
                return valid ? result : null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }
    }
}
