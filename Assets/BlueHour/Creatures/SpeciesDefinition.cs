using System;
using BlueHour.Core;
using UnityEngine;

namespace BlueHour.Creatures
{
    public enum Locomotion { Fixed, HostAssociated, Schooling, BottomDwelling }
    public enum ApproachResponse { Ignore, RetreatToShelter, Flee }
    public enum CreatureState { Fixed, Swim, School, ReturnToHost, Hide, Rest, Flee }

    [Serializable]
    public sealed class SpeciesSource
    {
        [SerializeField] private string title;
        [SerializeField] private string url;
        [SerializeField] private string supportedClaim;

        public string Title => title;
        public string Url => url;
        public string SupportedClaim => supportedClaim;
    }

    public sealed class SpeciesDefinition : ScriptableObject
    {
        [SerializeField] private string speciesId;
        [SerializeField] private string koreanName;
        [SerializeField] private string englishName;
        [SerializeField] private string scientificName;
        [SerializeField] private Locomotion locomotion;
        [SerializeField] private ApproachResponse approachResponse;
        [SerializeField] private string hostSpeciesId;
        [SerializeField, Min(0f)] private float speed;
        [SerializeField, Min(0f)] private float approachDistance;
        [SerializeField] private CreatureView prototype;
        [SerializeField] private SpeciesSource[] sources = Array.Empty<SpeciesSource>();

        public string SpeciesId => speciesId;
        public string KoreanName => koreanName;
        public string EnglishName => englishName;
        public string ScientificName => scientificName;
        public Locomotion Locomotion => locomotion;
        public ApproachResponse Response => approachResponse;
        public string HostSpeciesId => hostSpeciesId;
        public float Speed => speed;
        public float ApproachDistance => approachDistance;
        public CreatureView Prototype => prototype;
        public System.Collections.Generic.IReadOnlyList<SpeciesSource> Sources => sources;

        public bool TryValidate(out string error)
        {
            if (!DataId.IsValid(speciesId) || string.IsNullOrWhiteSpace(koreanName)
                || string.IsNullOrWhiteSpace(englishName) || string.IsNullOrWhiteSpace(scientificName))
            {
                error = "A stable ID and Korean, English and scientific names are required.";
                return false;
            }

            if (prototype == null || sources == null || sources.Length == 0)
            {
                error = "A display prototype and at least one evidence record are required.";
                return false;
            }

            foreach (var source in sources)
            {
                if (source == null || string.IsNullOrWhiteSpace(source.Title)
                    || string.IsNullOrWhiteSpace(source.SupportedClaim)
                    || !Uri.TryCreate(source.Url, UriKind.Absolute, out var uri)
                    || uri.Scheme != Uri.UriSchemeHttps)
                {
                    error = "Each source needs an HTTPS URL, title and supported claim.";
                    return false;
                }
            }

            if (!Enum.IsDefined(typeof(Locomotion), locomotion)
                || !Enum.IsDefined(typeof(ApproachResponse), approachResponse)
                || !float.IsFinite(speed) || !float.IsFinite(approachDistance)
                || (locomotion == Locomotion.Fixed ? speed != 0f : speed <= 0f)
                || (approachResponse == ApproachResponse.Ignore ? approachDistance != 0f : approachDistance <= 0f)
                || (locomotion == Locomotion.HostAssociated && !DataId.IsValid(hostSpeciesId))
                || (locomotion != Locomotion.HostAssociated && !string.IsNullOrEmpty(hostSpeciesId)))
            {
                error = "Locomotion, host reference, speed or approach distance is invalid.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
