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

        public void GuideTowards(Vector3 destination, Vector3 lookAt, float metresPerSecond, float deltaTime)
        {
            var position = transform.position;
            var target = swimBounds.ClosestPoint(destination);
            var displacement = Vector3.ClampMagnitude(target - position, metresPerSecond * deltaTime);
            controller.Move(displacement);

            var direction = lookAt - view.position;
            if (direction.sqrMagnitude < 0.01f)
                return;

            var horizontal = new Vector3(direction.x, 0f, direction.z);
            if (horizontal.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(horizontal, Vector3.up), deltaTime * 40f);
            var targetPitch = Mathf.Clamp(-Mathf.Atan2(direction.y, horizontal.magnitude) * Mathf.Rad2Deg, -75f, 75f);
            pitch = Mathf.MoveTowards(pitch, targetPitch, deltaTime * 32f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public float ViewPitch => pitch;

        public bool TryRestorePose(Vector3 point, float yaw, float savedPitch)
        {
            if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z)
                || !float.IsFinite(yaw) || !float.IsFinite(savedPitch) || !swimBounds.Contains(point))
                return false;

            var half = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
            controller.enabled = false;
            var center = point + controller.center;
            var occupied = Physics.CheckCapsule(center - Vector3.up * half, center + Vector3.up * half,
                Mathf.Max(0.01f, controller.radius - 0.06f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (!occupied)
            {
                transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
                pitch = Mathf.Clamp(savedPitch, -85f, 85f);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            controller.enabled = true;
            return !occupied;
        }

        private static float LimitAxis(float position, float displacement, float min, float max, float fadeDistance)
        {
            var distance = Mathf.Max(0f, displacement < 0f ? position - min : max - position);
            var speedScale = Mathf.Clamp01(distance / fadeDistance);
            return Mathf.Sign(displacement) * Mathf.Min(Mathf.Abs(displacement) * speedScale, distance);
        }
    }
}
