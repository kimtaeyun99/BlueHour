using UnityEngine;

namespace BlueHour.Creatures
{
    public static class Schooling
    {
        private const float NeighborRadius = 3f;
        private const float SeparationRadius = 0.8f;

        public static Vector3 Steer(int member, Vector3[] positions, Vector3[] velocities)
        {
            var separation = Vector3.zero;
            var alignment = Vector3.zero;
            var center = Vector3.zero;
            var count = 0;
            var origin = positions[member];

            for (var neighbor = 0; neighbor < positions.Length; neighbor++)
            {
                if (neighbor == member)
                    continue;

                var offset = positions[neighbor] - origin;
                var distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > NeighborRadius * NeighborRadius)
                    continue;

                if (distanceSquared < SeparationRadius * SeparationRadius)
                {
                    var distance = Mathf.Sqrt(distanceSquared);
                    var away = distance > 0.001f
                        ? -offset / distance : (member < neighbor ? Vector3.left : Vector3.right);
                    separation += away * (1f - distance / SeparationRadius);
                }

                center += offset;
                alignment += velocities[neighbor];
                count++;
            }

            if (count == 0)
                return Vector3.zero;

            return separation * 2.8f + center * (0.20f / count) + alignment * (0.55f / count);
        }
    }
}
