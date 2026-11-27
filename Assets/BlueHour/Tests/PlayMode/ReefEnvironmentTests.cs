using System.Collections;
using BlueHour.Environment;
using BlueHour.Experience;
using BlueHour.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlueHour.Tests
{
    public sealed class ReefEnvironmentTests
    {
        [UnityTest]
        public IEnumerator MainSceneSupportsRegionsDepthAndEnvironmentModes()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var environment = Object.FindFirstObjectByType<ReefEnvironment>();
            var motor = Camera.main.GetComponentInParent<SwimMotor>();

            Assert.That(environment.Region.RegionId, Is.EqualTo("gbr"));
            Assert.That(environment.ViewerSnapshot.Habitat.Type, Is.EqualTo(HabitatType.CoralReef));
            Assert.That(environment.TimeMode, Is.EqualTo(ClockMode.LocalTime));

            environment.SetTimeMode(ClockMode.Accelerated);
            environment.SetTimeMode(ClockMode.Fixed);
            var fixedTime = environment.ViewerSnapshot.LocalTime;
            environment.AdvanceFixedHour();
            Assert.That(environment.ViewerSnapshot.LocalTime, Is.EqualTo(fixedTime.AddHours(1)));
            environment.SetSeason(Season.Winter);
            environment.SetWeather(Weather.Rain);
            Assert.That(environment.ViewerSnapshot.Season, Is.EqualTo(Season.Winter));
            Assert.That(environment.ViewerSnapshot.Weather, Is.EqualTo(Weather.Rain));
            environment.SetWeather(null);
            environment.SetSeason(null);
            Assert.That(environment.WeatherOverride, Is.Null);
            Assert.That(environment.SeasonOverride, Is.Null);

            motor.transform.position = new Vector3(0f, -147f, 199f);
            Physics.SyncTransforms();
            motor.Swim(Vector2.zero, -1f, Vector2.zero, 5f);
            var basin = environment.SampleAt(motor.transform.position);
            Assert.That(basin.Habitat.Type, Is.EqualTo(HabitatType.OpenWater));
            Assert.That(basin.DepthM, Is.EqualTo(150f).Within(0.02f));

            motor.transform.position = new Vector3(152f, -80f, 110f);
            Physics.SyncTransforms();
            motor.Swim(Vector2.up, 0f, Vector2.zero, 13f);
            Assert.That(motor.transform.position.z, Is.GreaterThan(160f));
            Assert.That(environment.SampleAt(motor.transform.position).Habitat.Type,
                Is.EqualTo(HabitatType.UnderwaterCave));
        }
    }
}
