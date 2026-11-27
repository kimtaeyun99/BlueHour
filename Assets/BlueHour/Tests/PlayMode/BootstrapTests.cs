using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlueHour.Tests
{
    public sealed class BootstrapTests
    {
        [UnityTest]
        public IEnumerator FirstBuildSceneLoadsWithAnActiveCamera()
        {
            Assert.That(SceneUtility.GetScenePathByBuildIndex(0),
                Is.EqualTo("Assets/BlueHour/Scenes/Bootstrap.unity"));

            yield return SceneManager.LoadSceneAsync(0);

            var scene = SceneManager.GetActiveScene();
            Assert.That(scene.name, Is.EqualTo("Bootstrap"));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Camera.main.gameObject.scene, Is.EqualTo(scene));
            Assert.That(Camera.main.isActiveAndEnabled, Is.True);
        }
    }
}
