using System.Collections;
using System.Linq;
using BlueHour.Creatures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlueHour.Tests
{
    public sealed class CreatureHabitatTests
    {
        [UnityTest]
        public IEnumerator PresentsFiveSpeciesAndReusesResetViews()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var habitat = Object.FindFirstObjectByType<CreatureHabitat>();
            Assert.That(habitat, Is.Not.Null);
            Assert.That(habitat.TryValidate(out var error), Is.True, error);
            Assert.That(habitat.PopulationSize, Is.EqualTo(16));

            var initial = habitat.GetComponentsInChildren<CreatureView>();
            Assert.That(initial.Count(view => view.name.StartsWith("그린 크로미스")), Is.EqualTo(12));
            foreach (var name in new[] { "시나몬 흰동가리", "버블팁 말미잘", "파란점가오리", "밀레포라 가지산호" })
                Assert.That(initial.Count(view => view.name.StartsWith(name)), Is.EqualTo(1));

            habitat.SetPopulationActive(false);
            Assert.That(habitat.PopulationSize, Is.Zero);
            Assert.That(habitat.GetComponentsInChildren<CreatureView>(), Is.Empty);
            Assert.That(initial.All(view => view.transform.localPosition == Vector3.zero
                && view.transform.localScale == Vector3.one), Is.True);

            habitat.SetPopulationActive(true);
            Assert.That(habitat.GetComponentsInChildren<CreatureView>(), Is.EqualTo(initial));
            var fixedView = initial.Single(view => view.name.StartsWith("버블팁 말미잘"));
            var fish = initial.First(view => view.name.StartsWith("그린 크로미스"));
            var fixedStart = fixedView.transform.position;
            var fishStart = fish.transform.position;
            yield return new WaitForSeconds(0.4f);
            Assert.That(fixedView.transform.position, Is.EqualTo(fixedStart));
            Assert.That(Vector3.Distance(fish.transform.position, fishStart), Is.GreaterThan(0.01f));
        }
    }
}
