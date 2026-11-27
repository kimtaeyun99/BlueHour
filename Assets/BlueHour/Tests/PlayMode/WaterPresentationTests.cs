using System.Collections;
using System.Linq;
using BlueHour.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlueHour.Tests
{
    public sealed class WaterPresentationTests
    {
        [UnityTest]
        public IEnumerator SceneProvidesVisualQualityAndSynthesizedSpatialAudio()
        {
            yield return SceneManager.LoadSceneAsync(0);

            var effects = Object.FindFirstObjectByType<UnderwaterAtmosphere>();
            var sound = Object.FindFirstObjectByType<ProceduralWaterAudio>();
            Assert.That(effects, Is.Not.Null);
            Assert.That(sound, Is.Not.Null);

            var water = effects.transform.Find("Water surface").GetComponent<Renderer>();
            Assert.That(water.sharedMaterial.shader.name, Is.EqualTo("BlueHour/Prototype Water Surface"));
            var particleSystem = effects.GetComponentInChildren<ParticleSystem>();
            var lights = effects.GetComponentsInChildren<Light>();
            Assert.That(lights.Length, Is.EqualTo(3));
            Assert.That(lights.All(light => light.enabled), Is.True);

            effects.SetQuality(WaterQuality.Low);
            Assert.That(lights.All(light => !light.enabled), Is.True);
            Assert.That(particleSystem.emission.rateOverTime.constant, Is.EqualTo(7f));
            effects.SetQuality(WaterQuality.High);
            Assert.That(particleSystem.emission.rateOverTime.constant, Is.EqualTo(23f));

            var sources = sound.GetComponentsInChildren<AudioSource>();
            Assert.That(sources.Length, Is.EqualTo(4));
            Assert.That(sources.Count(source => source.spatialBlend == 1f), Is.EqualTo(1));
            Assert.That(sources.All(source => source.clip != null && source.clip.samples > 0 && source.loop), Is.True);

            sound.SetAmbienceVolume(2f);
            sound.SetMusicVolume(-1f);
            Assert.That(sound.AmbienceVolume, Is.EqualTo(1f));
            Assert.That(sound.MusicVolume, Is.Zero);
            sound.SetMode(WaterAudioMode.Muted);
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(sources.All(source => source.volume == 0f), Is.True);
        }
    }
}
