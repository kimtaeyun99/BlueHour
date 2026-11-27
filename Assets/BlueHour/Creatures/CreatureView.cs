using UnityEngine;

namespace BlueHour.Creatures
{
    public sealed class CreatureView : MonoBehaviour
    {
        public void Place(Vector3 position, float yaw, float scale)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            transform.localScale = Vector3.one * scale;
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
        }

        public void Clear()
        {
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }
}
