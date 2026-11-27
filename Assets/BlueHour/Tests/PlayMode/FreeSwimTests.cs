using System.Collections;
using BlueHour.Experience;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlueHour.Tests
{
    public sealed class FreeSwimTests
    {
        private SwimMotor motor;

        [UnitySetUp]
        public IEnumerator LoadSwimScene()
        {
            yield return SceneManager.LoadSceneAsync(0);
            Assert.That(Camera.main, Is.Not.Null);
            motor = Camera.main.GetComponentInParent<SwimMotor>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(motor.GetComponent<SwimInput>().isActiveAndEnabled, Is.True);
        }

        [Test]
        public void SwimsInSixDirectionsAndChangesView()
        {
            AssertDisplacement(Vector2.up, 0f, Vector3.forward);
            AssertDisplacement(Vector2.down, 0f, Vector3.back);
            AssertDisplacement(Vector2.left, 0f, Vector3.left);
            AssertDisplacement(Vector2.right, 0f, Vector3.right);
            AssertDisplacement(Vector2.zero, 1f, Vector3.up);
            AssertDisplacement(Vector2.zero, -1f, Vector3.down);

            var originalView = Camera.main.transform.forward;
            motor.Swim(Vector2.zero, 0f, new Vector2(100f, 80f), 0f);
            Assert.That(Vector3.Angle(originalView, Camera.main.transform.forward), Is.GreaterThan(10f));
            Assert.That(Vector3.Dot(motor.transform.up, Vector3.up), Is.GreaterThan(0.99f));
        }

        [Test]
        public void SeabedCollisionAndBoundariesConstrainMovement()
        {
            motor.transform.position = new Vector3(0f, -22f, -19f);
            Physics.SyncTransforms();
            motor.Swim(Vector2.zero, -1f, Vector2.zero, 2f);
            Assert.That(motor.transform.position.y, Is.InRange(-24.75f, -24.35f));

            motor.transform.position = new Vector3(49f, -5f, -19f);
            Physics.SyncTransforms();
            motor.Swim(Vector2.right, 0f, Vector2.zero, 0.5f);
            Assert.That(motor.transform.position.x, Is.InRange(49.35f, 49.45f));
            motor.Swim(Vector2.right, 1f, Vector2.zero, 30f);
            Assert.That(motor.transform.position.x, Is.LessThanOrEqualTo(50.001f));
            Assert.That(motor.transform.position.y, Is.InRange(-0.31f, -0.299f));

            var prior = motor.transform.position;
            motor.Swim(Vector2.left, -1f, Vector2.zero, 0.25f);
            Assert.That(motor.transform.position.x, Is.LessThan(prior.x - 0.6f));
            Assert.That(motor.transform.position.y, Is.LessThan(prior.y - 0.6f));
        }

        private void AssertDisplacement(Vector2 planar, float vertical, Vector3 direction)
        {
            var start = motor.transform.position;
            motor.Swim(planar, vertical, Vector2.zero, 0.1f);
            Assert.That(Vector3.Distance(motor.transform.position, start + direction * 0.4f),
                Is.LessThan(0.01f));
        }
    }
}
