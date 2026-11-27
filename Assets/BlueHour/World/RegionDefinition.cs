using System;
using UnityEngine;

namespace BlueHour.World
{
    public enum HabitatType { CoralReef, SandySeabed, RockyCliff, OpenWater, UnderwaterCave, Seagrass }

    [Serializable]
    public sealed class HabitatZone
    {
        [SerializeField] private HabitatType type;
        [SerializeField] private string id;
        [SerializeField] private string koreanName;
        [SerializeField] private string englishName;
        [SerializeField] private Vector2 min;
        [SerializeField] private Vector2 max;
        [SerializeField, Range(0f, 1f)] private float currentExposure;
        [SerializeField, Range(0f, 1f)] private float lightExposure = 1f;

        public HabitatType Type => type;
        public string Id => id;
        public string KoreanName => koreanName;
        public string EnglishName => englishName;
        public Vector2 Min => min;
        public Vector2 Max => max;
        public float CurrentExposure => currentExposure;
        public float LightExposure => lightExposure;
    }

    public sealed class RegionDefinition : ScriptableObject
    {
        [SerializeField] private string regionId;
        [SerializeField] private string koreanName;
        [SerializeField] private string englishName;
        [SerializeField] private Bounds swimBounds =
            new Bounds(new Vector3(0f, -75f, 0f), new Vector3(500f, 150f, 500f));
        [SerializeField] private int utcOffsetMinutes = 600;
        [SerializeField] private HabitatZone[] habitats = Array.Empty<HabitatZone>();

        public string RegionId => regionId;
        public string KoreanName => koreanName;
        public string EnglishName => englishName;
        public Bounds SwimBounds => swimBounds;
        public TimeSpan UtcOffset => TimeSpan.FromMinutes(utcOffsetMinutes);
        public System.Collections.Generic.IReadOnlyList<HabitatZone> Habitats => habitats;
        public float MaxDepth => swimBounds.size.y;

        public bool TryLocate(Vector3 point, out HabitatZone habitat)
        {
            habitat = null;
            if (!swimBounds.Contains(point))
                return false;

            // Shared edges belong to the zone beginning on that edge.
            var outer = swimBounds.max;
            foreach (var candidate in habitats)
            {
                if (point.x >= candidate.Min.x && point.z >= candidate.Min.y
                    && (point.x < candidate.Max.x || point.x == outer.x && point.x == candidate.Max.x)
                    && (point.z < candidate.Max.y || point.z == outer.z && point.z == candidate.Max.y))
                {
                    habitat = candidate;
                    return true;
                }
            }

            return false;
        }

        public float DepthAt(Vector3 point)
        {
            return Mathf.Clamp(swimBounds.max.y - point.y, 0f, MaxDepth);
        }
    }
}
