using BlueHour.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BlueHour.Tests
{
    public sealed class CreatureBehaviorTests
    {
        private const string SpeciesDirectory = "Assets/BlueHour/Creatures/Species/";

        [TestCase("amphiprion_melanopus", "시나몬 흰동가리")]
        [TestCase("entacmaea_quadricolor", "버블팁 말미잘")]
        [TestCase("chromis_viridis", "그린 크로미스")]
        [TestCase("taeniura_lymma", "파란점가오리")]
        [TestCase("acropora_millepora", "밀레포라 가지산호")]
        public void HasValidApprovedSpeciesDefinition(string speciesId, string koreanName)
        {
            var definition = GetSpecies(speciesId);
            Assert.That(definition.SpeciesId, Is.EqualTo(speciesId));
            Assert.That(definition.KoreanName, Is.EqualTo(koreanName));
            Assert.That(definition.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void RejectsOutOfRangeSpeedWithoutChangingTheSharedDefinition()
        {
            var copy = Object.Instantiate(GetSpecies("chromis_viridis"));
            try
            {
                var values = new SerializedObject(copy);
                values.FindProperty("speed").floatValue = -1f;
                values.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(copy.TryValidate(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void ClownfishRetreatsToHostAndResumesSwimming()
        {
            var species = GetSpecies("amphiprion_melanopus");
            var brain = new CreatureBrain();
            var host = Vector3.zero;
            var outside = Vector3.right * 2.9f;
            brain.Reset(species.Locomotion, 0.5f);

            var towardHost = brain.GetVelocity(species, outside, host, outside, true, Vector3.zero, 0.1f);
            Assert.That(brain.State, Is.EqualTo(CreatureState.Flee));
            Assert.That(towardHost.x, Is.LessThan(0f));

            brain.GetVelocity(species, host, host, host, true, Vector3.zero, 0.1f);
            Assert.That(brain.State, Is.EqualTo(CreatureState.Hide));

            brain.GetVelocity(species, host, host, Vector3.right * 1.7f, true, Vector3.zero, 0.1f);
            Assert.That(brain.State, Is.EqualTo(CreatureState.Hide));

            brain.GetVelocity(species, host, host, Vector3.right * 3f, true, Vector3.zero, 0.1f);
            Assert.That(brain.State, Is.EqualTo(CreatureState.Swim));
        }

        [Test]
        public void SchoolingSeparatesCloseNeighborsAndIgnoresDistantOnes()
        {
            var positions = new[] { Vector3.zero, Vector3.right * 0.3f, Vector3.right * 30f };
            var velocities = new[] { Vector3.zero, Vector3.forward, Vector3.back * 50f };
            var adjustment = Schooling.Steer(0, positions, velocities);

            Assert.That(adjustment.x, Is.LessThan(0f));
            Assert.That(adjustment.z, Is.GreaterThan(0f));
            Assert.That(Schooling.Steer(2, positions, velocities), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void IdenticalVariationAndInputsRepeatTheSameBehavior()
        {
            var species = GetSpecies("taeniura_lymma");
            var first = new CreatureBrain();
            var second = new CreatureBrain();
            first.Reset(species.Locomotion, 0.7f);
            second.Reset(species.Locomotion, 0.7f);
            for (var frame = 0; frame < 200; frame++)
            {
                var firstVelocity = first.GetVelocity(species, Vector3.zero, Vector3.zero,
                    Vector3.right * 5f, true, Vector3.zero, 0.05f);
                var secondVelocity = second.GetVelocity(species, Vector3.zero, Vector3.zero,
                    Vector3.right * 5f, true, Vector3.zero, 0.05f);
                Assert.That(secondVelocity, Is.EqualTo(firstVelocity));
                Assert.That(second.State, Is.EqualTo(first.State));
            }
        }

        private static SpeciesDefinition GetSpecies(string id)
        {
            var definition = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(SpeciesDirectory + id + ".asset");
            Assert.That(definition, Is.Not.Null);
            return definition;
        }
    }
}
