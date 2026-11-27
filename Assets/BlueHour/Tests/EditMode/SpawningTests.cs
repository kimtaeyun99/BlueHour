using BlueHour.Creatures;
using BlueHour.Environment;
using BlueHour.World;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace BlueHour.Tests
{
    public sealed class SpawningTests
    {
        [Test]
        public void WeightSelectionSkipsUnavailableEntriesAndIsRepeatable()
        {
            var weights = new[] { 0f, 1f, 3f, 0f };
            Assert.That(SpawnChoices.WeightedIndex(weights, 0f), Is.EqualTo(1));
            Assert.That(SpawnChoices.WeightedIndex(weights, 0.25f), Is.EqualTo(2));
            Assert.That(SpawnChoices.WeightedIndex(weights, 1f), Is.EqualTo(2));
            Assert.That(SpawnChoices.WeightedIndex(new float[4], 0.3f), Is.EqualTo(-1));
            var first = new System.Random(625);
            var second = new System.Random(625);
            for (var i = 0; i < 20; i++)
                Assert.That(SpawnChoices.WeightedIndex(weights, (float)first.NextDouble()),
                    Is.EqualTo(SpawnChoices.WeightedIndex(weights, (float)second.NextDouble())));
        }

        [Test]
        public void DefaultProfileExcludesOtherHabitatsAndOutsideTheWaterVolume()
        {
            var region = AssetDatabase.LoadAssetAtPath<RegionDefinition>(
                "Assets/BlueHour/World/GreatBarrierReef/GreatBarrierReef.asset");
            var summerNoon = new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.FromHours(10));
            var profile = new SpawnProfile();
            Assert.That(profile.IsValid, Is.True);
            var reef = EnvironmentModel.Sample(region, new Vector3(0f, -15f, -9f),
                summerNoon, Season.Summer, Weather.Clear, 0.2f);
            var sand = EnvironmentModel.Sample(region, new Vector3(-120f, -15f, -9f),
                summerNoon, Season.Summer, Weather.Clear, 0.2f);
            var tooDeep = EnvironmentModel.Sample(region, new Vector3(0f, -151f, -9f),
                summerNoon, Season.Summer, Weather.Clear, 0.2f);
            Assert.That(profile.Matches(reef), Is.True);
            Assert.That(profile.Matches(sand), Is.False);
            Assert.That(profile.Matches(tooDeep), Is.False);
        }
    }
}
