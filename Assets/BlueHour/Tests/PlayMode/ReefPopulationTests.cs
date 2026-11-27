using System.Collections;
using BlueHour.Creatures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlueHour.Tests
{
    public sealed class ReefPopulationTests
    {
        [UnityTest]
        public IEnumerator SpawnWaitsUntilOffscreenAndReturnsOutsideTheLargerRange()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var habitat = Object.FindFirstObjectByType<CreatureHabitat>();
            var view = Camera.main.transform;
            var rig = view.parent;
            var site = habitat.transform.Find("SandySeabed spawn site");
            Assert.That(site, Is.Not.Null);
            Assert.That(habitat.IsGroupActive(5), Is.False);

            rig.position = site.position + Vector3.right * 20f;
            view.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(2.7f);
            Assert.That(habitat.IsGroupActive(5), Is.False);

            view.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            yield return new WaitForSeconds(2.7f);
            Assert.That(habitat.IsGroupActive(5), Is.True);

            rig.position = site.position + Vector3.right * 90f;
            yield return new WaitForSeconds(2.7f);
            Assert.That(habitat.IsGroupActive(5), Is.False);
        }

        [UnityTest]
        public IEnumerator RefusesToActivateGroupsPastHardPopulationCap()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var habitat = Object.FindFirstObjectByType<CreatureHabitat>();
            Assert.That(habitat.PopulationSize, Is.EqualTo(16));
            Assert.That(habitat.TrySetGroupActive(6, true), Is.True);
            Assert.That(habitat.TrySetGroupActive(7, true), Is.True);
            Assert.That(habitat.PopulationSize, Is.EqualTo(habitat.ActiveCap));
            Assert.That(habitat.TrySetGroupActive(8, true), Is.False);
            Assert.That(habitat.IsGroupActive(8), Is.False);
            Assert.That(habitat.TrySetGroupActive(6, false), Is.True);
            Assert.That(habitat.TrySetGroupActive(7, false), Is.True);
            Assert.That(habitat.TrySetGroupActive(8, true), Is.True);
        }
    }
}
