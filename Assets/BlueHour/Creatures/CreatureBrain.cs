using UnityEngine;

namespace BlueHour.Creatures
{
    public sealed class CreatureBrain
    {
        private const float RetreatExitMultiplier = 1.35f;
        private const float HideAtDistance = 0.65f;
        private const float HostRange = 2.8f;
        private const float RestCycleSeconds = 18f;

        private float age;
        private float phase;

        public CreatureState State { get; private set; }

        public void Reset(Locomotion locomotion, float variation)
        {
            age = 0f;
            phase = Mathf.Repeat(variation, 1f);
            State = locomotion == Locomotion.Fixed ? CreatureState.Fixed : CreatureState.Swim;
        }

        public Vector3 GetVelocity(SpeciesDefinition species, Vector3 position, Vector3 home,
            Vector3 observer, bool hasObserver, Vector3 schoolSteering, float deltaTime)
        {
            age += deltaTime;
            if (species.Locomotion == Locomotion.Fixed)
            {
                State = CreatureState.Fixed;
                return Vector3.zero;
            }

            var toHome = home - position;
            var fromObserver = position - observer;
            var wasRetreating = State is CreatureState.Flee or CreatureState.Hide;
            var approachDistance = species.ApproachDistance * (wasRetreating ? RetreatExitMultiplier : 1f);
            if (hasObserver && species.Response != ApproachResponse.Ignore
                && fromObserver.sqrMagnitude < approachDistance * approachDistance)
            {
                if (species.Response == ApproachResponse.RetreatToShelter)
                {
                    if (toHome.sqrMagnitude < HideAtDistance * HideAtDistance)
                    {
                        State = CreatureState.Hide;
                        return Vector3.zero;
                    }

                    State = CreatureState.Flee;
                    return toHome.normalized * (species.Speed * 1.4f);
                }

                State = CreatureState.Flee;
                if (species.Locomotion == Locomotion.BottomDwelling)
                    fromObserver.y = 0f;
                var away = fromObserver.sqrMagnitude > 0.0001f ? fromObserver.normalized : Vector3.back;
                return away * (species.Speed * 1.4f);
            }

            var angle = phase * Mathf.PI * 2f + age * 0.28f;
            var circle = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            switch (species.Locomotion)
            {
                case Locomotion.HostAssociated:
                    State = toHome.sqrMagnitude > HostRange * HostRange
                        ? CreatureState.ReturnToHost : CreatureState.Swim;
                    return Vector3.ClampMagnitude(toHome + circle * 1.45f, 1f) * species.Speed;
                case Locomotion.Schooling:
                    State = CreatureState.School;
                    var schoolCenter = toHome + circle * 3f;
                    return Vector3.ClampMagnitude(schoolCenter * 0.35f + schoolSteering, 1f) * species.Speed;
                case Locomotion.BottomDwelling:
                    if (Mathf.Repeat(age + phase * RestCycleSeconds, RestCycleSeconds) < 4f)
                    {
                        State = CreatureState.Rest;
                        return Vector3.zero;
                    }

                    State = CreatureState.Swim;
                    return Vector3.ClampMagnitude(toHome + circle * 4f, 1f) * species.Speed;
                default:
                    return Vector3.zero;
            }
        }
    }
}
