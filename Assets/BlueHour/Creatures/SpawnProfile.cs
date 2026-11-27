using System;
using BlueHour.Environment;
using BlueHour.World;
using UnityEngine;

namespace BlueHour.Creatures
{
    [Flags]
    public enum SpawnSeasons { Summer = 1, Autumn = 2, Winter = 4, Spring = 8, Any = 15 }

    [Serializable]
    public sealed class SpawnProfile
    {
        [SerializeField] private HabitatType habitat;
        [SerializeField] private Vector2 depthRangeM = new Vector2(0f, 150f);
        [SerializeField] private Vector2 localHours = new Vector2(0f, 24f);
        [SerializeField] private SpawnSeasons seasons = SpawnSeasons.Any;
        [SerializeField, Range(0f, 1f)] private float maxCloudCover = 1f;
        [SerializeField, Min(0.01f)] private float weight = 1f;

        public float Weight => weight;

        public bool Matches(in EnvironmentSnapshot sample)
        {
            var hour = sample.LocalTime.TimeOfDay.TotalHours;
            var inHours = localHours.x <= localHours.y
                ? hour >= localHours.x && hour < localHours.y
                : hour >= localHours.x || hour < localHours.y;
            var season = (SpawnSeasons)(1 << (int)sample.Season);
            return sample.Habitat != null && sample.Habitat.Type == habitat
                && sample.DepthM >= depthRangeM.x && sample.DepthM <= depthRangeM.y
                && inHours && (seasons & season) != 0 && sample.CloudCover <= maxCloudCover;
        }

        public bool IsValid => depthRangeM.x >= 0f && depthRangeM.y <= 150f && depthRangeM.x <= depthRangeM.y
            && localHours.x >= 0f && localHours.x < 24f && localHours.y >= 0f && localHours.y <= 24f
            && localHours.x != localHours.y && (seasons & SpawnSeasons.Any) != 0
            && maxCloudCover >= 0f && maxCloudCover <= 1f && float.IsFinite(weight) && weight > 0f;
    }

    public static class SpawnChoices
    {
        public static int WeightedIndex(float[] weights, float unitRoll)
        {
            var total = 0f;
            var last = -1;
            for (var index = 0; index < weights.Length; index++)
            {
                if (weights[index] <= 0f)
                    continue;
                total += weights[index];
                last = index;
            }
            if (total <= 0f)
                return -1;

            var roll = Mathf.Clamp01(unitRoll) * total;
            for (var index = 0; index < weights.Length; index++)
            {
                if (weights[index] <= 0f)
                    continue;
                roll -= weights[index];
                if (roll < 0f)
                    return index;
            }
            return last;
        }
    }
}
