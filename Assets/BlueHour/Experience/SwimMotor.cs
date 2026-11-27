using UnityEngine;

namespace BlueHour.Experience
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SwimMotor : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField, Min(0f)] private float speed = 4f;
        [SerializeField, Min(0f)] private float degreesPerPixel = 0.12f;
        [SerializeField, Min(0.01f)] private float boundaryFadeDistance = 5f;
        [SerializeField, Min(0.01f)] private float verticalBoundaryFadeDistance = 5f;
        [SerializeField] private Bounds swimBounds =
            new Bounds(new Vector3(0f, -25f, 0f), new Vector3(100f, 50f, 100f));

        private CharacterController controller;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            pitch = Mathf.DeltaAngle(0f, view.localEulerAngles.x);
        }

        public void Swim(Vector2 planarInput, float verticalInput, Vector2 lookDelta, float deltaTime)
        {
            transform.Rotate(0f, lookDelta.x * degreesPerPixel, 0f);
            pitch = Mathf.Clamp(pitch - lookDelta.y * degreesPerPixel, -85f, 85f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // Looking up or down leaves explicit ascent and descent independent.
            var localDirection = Vector3.ClampMagnitude(
                new Vector3(planarInput.x, verticalInput, planarInput.y), 1f);
            var displacement = transform.TransformDirection(localDirection) * (speed * deltaTime);
            var position = transform.position;
            var min = swimBounds.min;
            var max = swimBounds.max;

            displacement.x = LimitAxis(position.x, displacement.x, min.x, max.x, boundaryFadeDistance);
            displacement.y = LimitAxis(position.y, displacement.y, min.y, max.y, verticalBoundaryFadeDistance);
            displacement.z = LimitAxis(position.z, displacement.z, min.z, max.z, boundaryFadeDistance);
            controller.Move(displacement);
        }

        private static float LimitAxis(float position, float displacement, float min, float max, float fadeDistance)
        {
            var distance = Mathf.Max(0f, displacement < 0f ? position - min : max - position);
            var speedScale = Mathf.Clamp01(distance / fadeDistance);
            return Mathf.Sign(displacement) * Mathf.Min(Mathf.Abs(displacement) * speedScale, distance);
        }
    }
}
