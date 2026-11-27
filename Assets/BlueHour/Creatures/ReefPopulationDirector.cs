using System;
using System.Collections.Generic;
using BlueHour.Environment;
using UnityEngine;

namespace BlueHour.Creatures
{
    [RequireComponent(typeof(CreatureHabitat))]
    public sealed class ReefPopulationDirector : MonoBehaviour
    {
        [Serializable]
        private sealed class SpawnSite
        {
            public int groupIndex;
            public Transform anchor;
            public SpawnProfile profile;
            public bool isSchoolEvent;
        }

        [SerializeField] private ReefEnvironment environment;
        [SerializeField] private Camera observer;
        [SerializeField] private SpawnSite[] sites = Array.Empty<SpawnSite>();
        [SerializeField, Min(0.1f)] private float interval = 2.5f;
        [SerializeField, Min(1f)] private float activationDistance = 50f;
        [SerializeField, Min(1f)] private float returnDistance = 75f;
        [SerializeField, Min(1f)] private float minimumSpawnDistance = 6f;
        [SerializeField, Min(1f)] private float eventDuration = 38f;
        [SerializeField, Min(1f)] private float eventCooldown = 75f;
        [SerializeField, Range(0f, 1f)] private float eventChance = 0.18f;
        [SerializeField] private int seed = 173;

        private readonly Plane[] frustum = new Plane[6];
        private CreatureHabitat habitat;
        private System.Random random;
        private float[] choices;
        private float nextTick;
        private float nextEvent;
        private float eventExpiresAt;
        private int activeEvent = -1;

        public bool HasActiveEvent => activeEvent >= 0;

        private void Start()
        {
            habitat = GetComponent<CreatureHabitat>();
            if (!TryValidate(out var error))
            {
                Debug.LogError(error, this);
                enabled = false;
                return;
            }

            random = new System.Random(seed);
            choices = new float[sites.Length];
            nextTick = Time.time + interval;
            nextEvent = nextTick;
        }

        private void Update()
        {
            var now = Time.time;
            if (now < nextTick)
                return;

            nextTick = now + interval;
            Tick(now);
        }

        private void Tick(float now)
        {
            GeometryUtility.CalculateFrustumPlanes(observer, frustum);
            var viewPosition = observer.transform.position;

            for (var index = 0; index < sites.Length; index++)
            {
                var site = sites[index];
                choices[index] = 0f;
                var distance = (site.anchor.position - viewPosition).sqrMagnitude;
                var suitable = site.profile.Matches(environment.SampleAt(site.anchor.position));
                if (habitat.IsGroupActive(site.groupIndex))
                {
                    var expired = site.isSchoolEvent && now >= eventExpiresAt;
                    if ((!suitable || expired || distance > returnDistance * returnDistance)
                        && !habitat.IsGroupVisible(site.groupIndex, frustum)
                        && habitat.TrySetGroupActive(site.groupIndex, false) && index == activeEvent)
                    {
                        activeEvent = -1;
                        nextEvent = now + eventCooldown;
                    }
                    continue;
                }

                if (!suitable || distance > activationDistance * activationDistance
                    || distance < minimumSpawnDistance * minimumSpawnDistance
                    || habitat.IsGroupVisible(site.groupIndex, frustum) || !habitat.CanPlaceGroup(site.groupIndex))
                    continue;

                if (site.isSchoolEvent)
                {
                    if (activeEvent < 0 && now >= nextEvent && random.NextDouble() < eventChance
                        && habitat.TrySetGroupActive(site.groupIndex, true))
                    {
                        activeEvent = index;
                        eventExpiresAt = now + eventDuration;
                    }
                    continue;
                }

                choices[index] = site.profile.Weight;
            }

            var chosen = SpawnChoices.WeightedIndex(choices, (float)random.NextDouble());
            if (chosen >= 0)
                habitat.TrySetGroupActive(sites[chosen].groupIndex, true);
        }

        private bool TryValidate(out string error)
        {
            if (!habitat.enabled || environment == null || observer == null || sites == null || sites.Length == 0
                || minimumSpawnDistance >= activationDistance || activationDistance >= returnDistance)
            {
                error = "Reef spawning requires a habitat, environment, observer and valid distance bands.";
                return false;
            }

            var groups = new HashSet<int>();
            foreach (var site in sites)
            {
                if (site == null || site.anchor == null || !habitat.HasGroupAnchor(site.groupIndex, site.anchor)
                    || site.profile == null || !site.profile.IsValid || !groups.Add(site.groupIndex))
                {
                    error = "Reef spawning has an invalid or duplicate spawn site.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
