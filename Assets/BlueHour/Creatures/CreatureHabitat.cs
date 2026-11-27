using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueHour.Creatures
{
    public sealed class CreatureHabitat : MonoBehaviour
    {
        private const int MaxGroupSize = 16;
        private const float Acceleration = 5f;
        private const float LookAhead = 0.55f;

        [Serializable]
        private sealed class Population
        {
            public SpeciesDefinition species;
            public Transform anchor;
            [Min(1)] public int count = 1;
        }

        private sealed class Agent
        {
            public readonly CreatureBrain Brain = new CreatureBrain();
            public CreatureView View;
            public Vector3 Velocity;
            public float Scale;
        }

        private sealed class Group
        {
            public readonly Population Definition;
            public readonly Agent[] Agents;
            public readonly Vector3[] Positions;
            public readonly Vector3[] Velocities;

            public Group(Population definition)
            {
                Definition = definition;
                Agents = new Agent[definition.count];
                Positions = new Vector3[definition.count];
                Velocities = new Vector3[definition.count];
            }
        }

        [SerializeField] private Population[] populations = Array.Empty<Population>();
        [SerializeField] private Transform observer;
        [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private Bounds swimBounds =
            new Bounds(new Vector3(0f, -12.65f, 0f), new Vector3(90f, 23.5f, 90f));
        [SerializeField] private int seed = 625;

        private Group[] groups;
        private Vector3[] homes;
        private bool populationActive;

        public int PopulationSize { get; private set; }

        private void Awake()
        {
            if (!TryValidate(out var error))
            {
                Debug.LogError(error, this);
                enabled = false;
                return;
            }

            groups = new Group[populations.Length];
            homes = new Vector3[populations.Length];
            var anchors = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var population in populations)
                anchors.Add(population.species.SpeciesId, population.anchor);

            for (var index = 0; index < groups.Length; index++)
            {
                var population = populations[index];
                var species = population.species;
                var group = new Group(population);
                groups[index] = group;
                homes[index] = species.Locomotion == Locomotion.HostAssociated
                    ? anchors[species.HostSpeciesId].position + Vector3.up * 0.38f
                    : population.anchor.position;

                for (var member = 0; member < group.Agents.Length; member++)
                {
                    var view = Instantiate(species.Prototype, transform);
                    view.name = species.KoreanName + " " + (member + 1);
                    view.Clear();
                    group.Agents[member] = new Agent { View = view };
                }
            }
        }

        private void OnEnable()
        {
            SetPopulationActive(true);
        }

        private void OnDisable()
        {
            SetPopulationActive(false);
        }

        public void SetPopulationActive(bool active)
        {
            if (groups == null || active == populationActive)
                return;

            populationActive = active;
            PopulationSize = 0;
            var random = active ? new System.Random(seed) : null;
            for (var index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                var type = group.Definition.species.Locomotion;
                for (var member = 0; member < group.Agents.Length; member++)
                {
                    var agent = group.Agents[member];
                    agent.Velocity = Vector3.zero;
                    if (!active)
                    {
                        agent.Scale = 1f;
                        agent.Brain.Reset(type, 0f);
                        agent.View.Clear();
                        continue;
                    }

                    agent.Scale = Mathf.Lerp(0.86f, 1.14f, (float)random.NextDouble());
                    var phase = (float)random.NextDouble();
                    agent.Brain.Reset(type, phase);
                    var position = group.Definition.anchor.position;
                    if (type == Locomotion.Schooling)
                    {
                        var angle = member * Mathf.PI * 2f / group.Agents.Length;
                        position += new Vector3(Mathf.Cos(angle) * 1.6f,
                            (member % 3 - 1) * 0.35f, Mathf.Sin(angle) * 1.6f);
                    }

                    agent.View.Place(position, phase * 360f, agent.Scale);
                    agent.View.gameObject.SetActive(true);
                    PopulationSize++;
                }
            }
        }

        private void Update()
        {
            if (!populationActive)
                return;

            var deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            var hasObserver = observer != null;
            var observerPosition = hasObserver ? observer.position : Vector3.zero;
            for (var index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                var species = group.Definition.species;
                if (species.Locomotion == Locomotion.Fixed)
                    continue;

                // Read one snapshot for the whole school so order never changes neighbor state.
                for (var member = 0; member < group.Agents.Length; member++)
                {
                    group.Positions[member] = group.Agents[member].View.transform.position;
                    group.Velocities[member] = group.Agents[member].Velocity;
                }

                for (var member = 0; member < group.Agents.Length; member++)
                {
                    var agent = group.Agents[member];
                    var position = group.Positions[member];
                    var steering = species.Locomotion == Locomotion.Schooling
                        ? Schooling.Steer(member, group.Positions, group.Velocities) : Vector3.zero;
                    var velocity = agent.Brain.GetVelocity(species, position, homes[index],
                        observerPosition, hasObserver, steering, deltaTime);
                    agent.Velocity = Vector3.MoveTowards(agent.Velocity, velocity, Acceleration * deltaTime);
                    var nextPosition = ConstrainMovement(position, agent, species, deltaTime);
                    agent.View.PresentMotion(nextPosition, agent.Velocity, deltaTime);
                }
            }
        }

        private Vector3 ConstrainMovement(Vector3 position, Agent agent, SpeciesDefinition species, float deltaTime)
        {
            var speed = agent.Velocity.magnitude;
            if (speed < 0.001f)
                return position;

            var direction = agent.Velocity / speed;
            var radius = species.Locomotion == Locomotion.BottomDwelling ? 0.42f : 0.13f;
            if (Physics.SphereCast(position, radius * agent.Scale, direction, out var hit,
                LookAhead + speed * deltaTime, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                var slide = Vector3.ProjectOnPlane(agent.Velocity, hit.normal);
                if (species.Locomotion == Locomotion.BottomDwelling)
                    slide.y = Mathf.Max(0.2f, slide.y);
                else if (slide.sqrMagnitude < 0.0025f)
                    slide = Vector3.Cross(hit.normal, Vector3.up) * species.Speed;

                // Stop short of the surface and take only a swept-safe slide.
                var step = slide * deltaTime;
                if (step.sqrMagnitude > 0.000001f && Physics.SphereCast(position,
                    radius * agent.Scale, step.normalized, out var slideHit, step.magnitude,
                    obstacleLayers, QueryTriggerInteraction.Ignore))
                    step = step.normalized * Mathf.Max(0f, slideHit.distance - 0.015f);

                agent.Velocity = step / Mathf.Max(deltaTime, 0.0001f);
            }

            return swimBounds.ClosestPoint(position + agent.Velocity * deltaTime);
        }

        public bool TryValidate(out string error)
        {
            if (populations == null || populations.Length == 0)
            {
                error = "Creature habitat needs at least one configured population.";
                return false;
            }

            var speciesById = new Dictionary<string, SpeciesDefinition>(StringComparer.Ordinal);
            foreach (var population in populations)
            {
                if (population == null || population.species == null || population.anchor == null
                    || population.count < 1 || population.count > MaxGroupSize
                    || (population.species.Locomotion != Locomotion.Schooling && population.count != 1))
                {
                    error = "Each population needs an anchor, species and a supported group size.";
                    return false;
                }

                var species = population.species;
                if (!species.TryValidate(out error))
                    return false;

                if (!speciesById.TryAdd(species.SpeciesId, species))
                {
                    error = "Duplicate species ID: " + species.SpeciesId;
                    return false;
                }
            }

            foreach (var species in speciesById.Values)
            {
                if (species.Locomotion == Locomotion.HostAssociated
                    && (!speciesById.TryGetValue(species.HostSpeciesId, out var host)
                        || host.Locomotion != Locomotion.Fixed))
                {
                    error = "Missing or invalid host for " + species.SpeciesId;
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
