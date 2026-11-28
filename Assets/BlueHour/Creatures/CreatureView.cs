using UnityEngine;

namespace BlueHour.Creatures
{
    public sealed class CreatureView : MonoBehaviour
    {
        [SerializeField] private Transform[] animatedParts = System.Array.Empty<Transform>();
        private Quaternion[] restPose;
        private float phase;

        public SpeciesDefinition Species { get; private set; }
        public void BindSpecies(SpeciesDefinition definition) => Species = definition;

        private void Awake()
        {
            restPose = new Quaternion[animatedParts.Length];
            for (var index = 0; index < animatedParts.Length; index++)
                restPose[index] = animatedParts[index].localRotation;
        }
        public void Place(Vector3 position, float yaw, float scale)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            transform.localScale = Vector3.one * scale;
            phase = yaw * Mathf.Deg2Rad;
        }

        public void PresentMotion(Vector3 position, Vector3 velocity, float deltaTime)
        {
            var facing = new Vector3(velocity.x, 0f, velocity.z);
            if (facing.sqrMagnitude > 0.0025f)
            {
                var rotation = Quaternion.LookRotation(facing, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, 150f * deltaTime);
            }

            transform.position = position;
            var motion = Mathf.Clamp01(velocity.magnitude);
            phase = Mathf.Repeat(phase + deltaTime * Mathf.Lerp(3f, 16f, motion), Mathf.PI * 2f);
            for (var index = 0; index < animatedParts.Length; index++)
            {
                var swing = Mathf.Sin(phase + index * 1.15f) * Mathf.Lerp(2f, 24f, motion);
                animatedParts[index].localRotation = restPose[index] * Quaternion.Euler(0f, swing, 0f);
            }
        }

        public void Clear()
        {
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            phase = 0f;
            for (var index = 0; index < animatedParts.Length; index++)
                animatedParts[index].localRotation = restPose[index];
            gameObject.SetActive(false);
        }
    }
}
