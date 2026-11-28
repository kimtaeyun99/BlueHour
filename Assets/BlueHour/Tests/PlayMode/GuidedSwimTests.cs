using System.Collections;
using BlueHour.Creatures;
using BlueHour.Experience;
using BlueHour.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlueHour.Tests
{
    public sealed class GuidedSwimTests
    {
        [UnityTest]
        public IEnumerator RouteMovesAndTimerReturnsControl()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var guided = Object.FindFirstObjectByType<GuidedSwim>();
            var motor = Camera.main.GetComponentInParent<SwimMotor>();
            var start = motor.transform.position;
            guided.SetMode(ViewingMode.Route);
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(Vector3.Distance(motor.transform.position, start), Is.GreaterThan(0.2f));
            guided.SetTimer(0.2f);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(guided.Mode, Is.EqualTo(ViewingMode.Manual));
            Assert.That(motor.GetComponent<SwimInput>().Guided, Is.False);
            Assert.That(guided.TimerRemaining, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LostFollowTargetFallsBackToFixedViewing()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var guided = Object.FindFirstObjectByType<GuidedSwim>();
            var habitat = Object.FindFirstObjectByType<CreatureHabitat>();
            guided.SetMode(ViewingMode.Follow);
            Assert.That(guided.Mode, Is.EqualTo(ViewingMode.Follow));
            habitat.SetPopulationActive(false);
            yield return null;
            Assert.That(guided.Mode, Is.EqualTo(ViewingMode.Fixed));
        }
    }
}
