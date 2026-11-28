using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BlueHour.Creatures;
using BlueHour.Environment;
using UnityEngine;

namespace BlueHour.Observation
{
    public sealed class ReefObservations : MonoBehaviour
    {
        [SerializeField] private Camera viewer;
        [SerializeField] private CreatureHabitat habitat;
        [SerializeField] private ReefEnvironment environment;
        [SerializeField] private SpeciesDefinition[] catalog;
        [SerializeField, Min(0.1f)] private float maxDistance = 10f;
        [SerializeField, Range(1f, 90f)] private float viewingAngle = 9f;
        [SerializeField, Min(0.1f)] private float requiredTime = 1.4f;

        private readonly List<CreatureView> views = new List<CreatureView>(32);
        private readonly HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal);
        private readonly ObservationDwell dwell = new ObservationDwell();
        private float timeUntilSample;
        private float minAlignment;

        public ReefRecords Records { get; private set; }
        public IReadOnlyList<SpeciesDefinition> Catalog => catalog;
        public SpeciesDefinition FocusSpecies { get; private set; }
        public string Status { get; private set; }
        public bool Paused { get; set; }
        public Camera Viewer => viewer;
        public ReefEnvironment Environment => environment;

        private void Awake()
        {
            minAlignment = Mathf.Cos(viewingAngle * Mathf.Deg2Rad);
            Records = new ReefRecords(Path.Combine(Application.persistentDataPath, "SaveData"));
            foreach (var entry in Records.Observations)
                observed.Add(entry.speciesId);
            Status = Records.LoadWarning;
        }

        private void Update()
        {
            if (Paused)
            {
                ResetTracking();
                return;
            }

            timeUntilSample += Time.unscaledDeltaTime;
            if (timeUntilSample < 0.15f)
                return;

            var elapsed = timeUntilSample;
            timeUntilSample = 0f;
            var candidate = FindFocused();
            FocusSpecies = candidate == null ? null : candidate.Species;
            if (candidate == null)
            {
                dwell.Reset();
                return;
            }

            var id = FocusSpecies.SpeciesId;
            if (!dwell.Step(candidate, Mathf.Min(0.2f, elapsed), requiredTime) || observed.Contains(id))
                return;

            var sample = environment.SampleAt(viewer.transform.position);
            var hour = sample.LocalTime.Hour;
            try
            {
                if (Records.RecordFirst(new ObservationEntry
                {
                    speciesId = id,
                    firstSeenUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    regionId = environment.Region.RegionId,
                    depthM = sample.DepthM,
                    localTimeOfDay = hour < 6 || hour >= 18 ? "night" : "day"
                }))
                    Status = "New observation: " + FocusSpecies.EnglishName;

                observed.Add(id);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Status = exception.Message;
                dwell.Reset();
            }
        }

        public string[] SpeciesInFrame()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            habitat.GetComponentsInChildren(false, views);
            foreach (var view in views)
            {
                var center = ViewCenter(view);
                var point = viewer.WorldToViewportPoint(center);
                if (point.z > viewer.nearClipPlane && point.z < 20f && point.x >= 0f && point.x <= 1f
                    && point.y >= 0f && point.y <= 1f && Visible(center) && view.Species != null)
                    ids.Add(view.Species.SpeciesId);
            }

            var result = new string[ids.Count];
            ids.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private CreatureView FindFocused()
        {
            CreatureView best = null;
            var bestAlignment = minAlignment;
            var origin = viewer.transform.position;
            var cameraDirection = viewer.transform.forward;
            habitat.GetComponentsInChildren(false, views);
            foreach (var view in views)
            {
                if (view.Species == null)
                    continue;
                var center = ViewCenter(view);
                var offset = center - origin;
                if (offset.sqrMagnitude > maxDistance * maxDistance || offset.sqrMagnitude < 0.01f)
                    continue;
                var alignment = Vector3.Dot(offset.normalized, cameraDirection);
                if (alignment <= bestAlignment || !Visible(center))
                    continue;
                best = view;
                bestAlignment = alignment;
            }
            return best;
        }

        private static Vector3 ViewCenter(CreatureView view)
        {
            return view.Species != null && view.Species.Locomotion == Locomotion.Fixed
                ? view.transform.position + Vector3.up * 0.20f
                : view.transform.position;
        }

        private bool Visible(Vector3 position)
        {
            var origin = viewer.transform.position;
            var offset = position - origin;
            var distance = offset.magnitude;
            return !Physics.Raycast(origin, offset / Mathf.Max(0.001f, distance), Mathf.Max(0f, distance - 0.03f),
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void ResetTracking()
        {
            FocusSpecies = null;
            timeUntilSample = 0f;
            dwell.Reset();
        }
    }

    public sealed class ObservationDwell
    {
        private object target;
        private float seconds;

        public bool Step(object candidate, float step, float threshold)
        {
            if (!ReferenceEquals(candidate, target))
            {
                target = candidate;
                seconds = 0f;
            }
            seconds += step;
            return target != null && seconds >= threshold;
        }

        public void Reset()
        {
            target = null;
            seconds = 0f;
        }
    }
}
