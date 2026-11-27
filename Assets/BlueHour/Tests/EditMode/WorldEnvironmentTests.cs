using System;
using BlueHour.Environment;
using BlueHour.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BlueHour.Tests
{
    public sealed class WorldEnvironmentTests
    {
        private const string RegionPath = "Assets/BlueHour/World/GreatBarrierReef/GreatBarrierReef.asset";
        private static readonly DateTimeOffset Utc = new DateTimeOffset(2026, 11, 30, 16, 59, 0, TimeSpan.Zero);

        [Test]
        public void ClockModesUseQueenslandOffsetAndPreserveSimulatedTime()
        {
            var clock = new EnvironmentClock(TimeSpan.FromHours(10));
            var local = clock.GetLocalTime(Utc, 10d);
            Assert.That(local.Offset, Is.EqualTo(TimeSpan.FromHours(10)));
            Assert.That(local.Month, Is.EqualTo(12));
            Assert.That(EnvironmentClock.SouthernSeason(local), Is.EqualTo(Season.Summer));

            clock.SetMode(ClockMode.Accelerated, Utc, 10d);
            Assert.That(clock.GetLocalTime(Utc, 11d), Is.EqualTo(local.AddMinutes(1)));
            clock.SetMode(ClockMode.Fixed, Utc, 12d);
            Assert.That(clock.GetLocalTime(Utc.AddDays(1), 50d), Is.EqualTo(local.AddMinutes(2)));
            clock.AdvanceFixedHours(2);
            Assert.That(clock.GetLocalTime(Utc, 200d), Is.EqualTo(local.AddMinutes(2).AddHours(2)));
            clock.SetMode(ClockMode.LocalTime, Utc.AddDays(1), 210d);
            Assert.That(clock.GetLocalTime(Utc.AddDays(1), 211d), Is.EqualTo(local.AddDays(1)));
        }

        [TestCase(-175f, 0f, HabitatType.SandySeabed)]
        [TestCase(0f, 0f, HabitatType.CoralReef)]
        [TestCase(-175f, 160f, HabitatType.RockyCliff)]
        [TestCase(0f, 160f, HabitatType.OpenWater)]
        [TestCase(175f, 160f, HabitatType.UnderwaterCave)]
        [TestCase(175f, 0f, HabitatType.Seagrass)]
        [TestCase(85f, 45f, HabitatType.UnderwaterCave)]
        [TestCase(250f, 250f, HabitatType.UnderwaterCave)]
        public void RegionIdentifiesHabitatIncludingSharedEdges(float x, float z, HabitatType expected)
        {
            var region = GetRegion();
            Assert.That(region.SwimBounds.size, Is.EqualTo(new Vector3(500f, 150f, 500f)));
            Assert.That(region.Habitats.Count, Is.EqualTo(6));
            Assert.That(region.TryLocate(new Vector3(x, -50f, z), out var habitat), Is.True);
            Assert.That(habitat.Type, Is.EqualTo(expected));
            Assert.That(region.TryLocate(new Vector3(x, -151f, z), out _), Is.False);
            Assert.That(region.TryLocate(new Vector3(250.01f, -50f, z), out _), Is.False);
        }

        [Test]
        public void DepthChangesPressureTemperatureLightAndCurrents()
        {
            var region = GetRegion();
            var noon = Utc.ToOffset(TimeSpan.FromHours(10)).AddHours(9);
            var surface = EnvironmentModel.Sample(region, new Vector3(0f, 0f, 160f), noon,
                Season.Summer, Weather.Clear, 0.12f);
            var deep = EnvironmentModel.Sample(region, new Vector3(0f, -150f, 160f), noon,
                Season.Summer, Weather.Clear, 0.12f);
            var rainy = EnvironmentModel.Sample(region, new Vector3(0f, 0f, 160f), noon,
                Season.Winter, Weather.Rain, 0.94f);
            Assert.That(surface.DepthM, Is.Zero);
            Assert.That(surface.AbsolutePressurePa, Is.EqualTo(101325f));
            Assert.That(deep.DepthM, Is.EqualTo(150f));
            Assert.That(deep.AbsolutePressurePa, Is.EqualTo(1609097.5f).Within(400f));
            Assert.That(deep.TemperatureC, Is.LessThan(surface.TemperatureC));
            Assert.That(deep.PracticalSalinity, Is.GreaterThan(surface.PracticalSalinity));
            Assert.That(deep.LightFactor, Is.LessThan(surface.LightFactor));
            Assert.That(deep.CurrentMps.magnitude, Is.LessThan(surface.CurrentMps.magnitude));
            Assert.That(rainy.LightFactor, Is.LessThan(surface.LightFactor));
            Assert.That(rainy.TemperatureC, Is.LessThan(surface.TemperatureC));
        }

        [Test]
        public void SeededWeatherIsRepeatableAndTransitionsContinuously()
        {
            var boundary = new DateTimeOffset(2026, 11, 30, 18, 0, 0, TimeSpan.Zero);
            var before = EnvironmentModel.SimulatedCloudCover(boundary.AddSeconds(-1), 625);
            var after = EnvironmentModel.SimulatedCloudCover(boundary.AddSeconds(1), 625);
            Assert.That(after, Is.EqualTo(before).Within(0.0001f));
            Assert.That(EnvironmentModel.SimulatedCloudCover(boundary.AddSeconds(1), 625), Is.EqualTo(after));
            Assert.That(EnvironmentModel.WeatherAt(0.12f), Is.EqualTo(Weather.Clear));
            Assert.That(EnvironmentModel.WeatherAt(0.64f), Is.EqualTo(Weather.Overcast));
            Assert.That(EnvironmentModel.WeatherAt(0.94f), Is.EqualTo(Weather.Rain));
        }

        private static RegionDefinition GetRegion()
        {
            var region = AssetDatabase.LoadAssetAtPath<RegionDefinition>(RegionPath);
            Assert.That(region, Is.Not.Null);
            return region;
        }
    }
}
