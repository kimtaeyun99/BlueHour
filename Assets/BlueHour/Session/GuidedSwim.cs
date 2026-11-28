using System.Collections.Generic;
using BlueHour.Creatures;
using BlueHour.Experience;
using UnityEngine;

namespace BlueHour.Session
{
    public enum ViewingMode { Manual, Fixed, Route, Follow }

    public sealed class GuidedSwim : MonoBehaviour
    {
        [SerializeField] private SwimInput input;
        [SerializeField] private SwimMotor motor;
        [SerializeField] private CreatureHabitat habitat;
        [SerializeField] private ReefPopulationDirector events;
        [SerializeField] private Vector3[] waypoints =
        {
            new Vector3(0f, -9f, -9f), new Vector3(12f, -8f, 9f), new Vector3(15f, -12f, 44f),
            new Vector3(3f, -17f, 79f), new Vector3(-8f, -18f, 59f), new Vector3(-4f, -9f, 0f)
        };

        private readonly List<CreatureView> candidates = new List<CreatureView>(32);
        private CreatureView target;
        private int routeIndex;
        private float remaining;
        private float eventCooldown;
        private float detourTime;
        private Vector3 eventLook;

        public ViewingMode Mode { get; private set; }
        public float Speed { get; set; } = 2.6f;
        public float TimerRemaining => remaining;
        public bool Paused { get; set; }
        public event System.Action TimerExpired;

        public void SetMode(ViewingMode mode)
        {
            if (mode == ViewingMode.Route && waypoints.Length < 2)
                mode = ViewingMode.Fixed;

            target = mode == ViewingMode.Follow ? FindTarget() : null;
            if (mode == ViewingMode.Follow && target == null)
                mode = ViewingMode.Fixed;

            Mode = mode;
            input.Guided = mode != ViewingMode.Manual;
            detourTime = 0f;
            if (mode == ViewingMode.Route)
                routeIndex = NearestWaypoint();
        }

        public void SetTimer(float seconds) => remaining = Mathf.Max(0f, seconds);

        private void OnDisable()
        {
            if (input != null)
                input.Guided = false;
        }

        private void Update()
        {
            if (Paused || Mode == ViewingMode.Manual)
                return;
            var elapsed = Time.unscaledDeltaTime;
            if (remaining > 0f)
            {
                remaining = Mathf.Max(0f, remaining - elapsed);
                if (remaining == 0f)
                {
                    SetMode(ViewingMode.Manual);
                    TimerExpired?.Invoke();
                    return;
                }
            }

            var delta = Mathf.Min(elapsed, 0.05f);
            if (Mode == ViewingMode.Route)
                AdvanceRoute(delta);
            else if (Mode == ViewingMode.Follow)
                AdvanceFollow(delta);
        }

        private void AdvanceRoute(float delta)
        {
            var position = motor.transform.position;
            var waypoint = waypoints[routeIndex];
            if ((position - waypoint).sqrMagnitude < 2.25f)
            {
                routeIndex = (routeIndex + 1) % waypoints.Length;
                waypoint = waypoints[routeIndex];
            }

            eventCooldown = Mathf.Max(0f, eventCooldown - delta);
            if (eventCooldown == 0f && events != null && events.TryGetEventPosition(out var center)
                && (center - position).sqrMagnitude < 24f * 24f)
            {
                detourTime = 8f;
                eventCooldown = 48f;
                eventLook = center;
            }

            if (detourTime > 0f)
            {
                if (events.TryGetEventPosition(out var currentEvent))
                {
                    detourTime -= delta;
                    var look = (currentEvent + eventLook) * 0.5f;
                    motor.GuideTowards(look + Vector3.back * 6f + Vector3.up * 2f, look, Speed, delta);
                    return;
                }
                detourTime = 0f;
            }

            motor.GuideTowards(waypoint, waypoint, Speed, delta);
        }

        private void AdvanceFollow(float delta)
        {
            if (target == null || !target.isActiveAndEnabled
                || (target.transform.position - motor.transform.position).sqrMagnitude > 28f * 28f)
            {
                SetMode(ViewingMode.Fixed);
                return;
            }

            var look = target.transform.position;
            var away = motor.transform.position - look;
            var offset = new Vector3(away.x, 0f, away.z).normalized * 3.3f + Vector3.up * 0.6f;
            motor.GuideTowards(look + offset, look, Speed, delta);
        }

        private int NearestWaypoint()
        {
            var result = 0;
            var best = float.MaxValue;
            for (var index = 0; index < waypoints.Length; index++)
            {
                var distance = (motor.transform.position - waypoints[index]).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    result = index;
                }
            }
            return result;
        }

        private CreatureView FindTarget()
        {
            habitat.GetComponentsInChildren(false, candidates);
            CreatureView best = null;
            var nearest = 18f * 18f;
            foreach (var candidate in candidates)
            {
                if (candidate.Species == null || candidate.Species.Locomotion == Locomotion.Fixed)
                    continue;

                var distance = (motor.transform.position - candidate.transform.position).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
