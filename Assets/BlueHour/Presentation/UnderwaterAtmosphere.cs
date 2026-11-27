using BlueHour.Environment;
using UnityEngine;

namespace BlueHour.Presentation
{
    public enum WaterQuality { Low, High }

    public sealed class UnderwaterAtmosphere : MonoBehaviour
    {
        private static readonly int LightFactor = Shader.PropertyToID("_LightFactor");
        private static readonly int Current = Shader.PropertyToID("_WaterCurrent");
        private static readonly int Glints = Shader.PropertyToID("_Glints");

        [SerializeField] private ReefEnvironment environment;
        [SerializeField] private Transform viewer;
        [SerializeField] private Renderer surface;
        [SerializeField] private Renderer[] seagrass;
        [SerializeField] private ParticleSystem suspendedMatter;
        [SerializeField] private Light[] shafts;
        [SerializeField] private WaterQuality quality = WaterQuality.High;

        private MaterialPropertyBlock properties;
        public WaterQuality Quality => quality;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            SetQuality(quality);
        }

        public void SetQuality(WaterQuality value)
        {
            quality = value;
            var emission = suspendedMatter.emission;
            emission.rateOverTime = value == WaterQuality.High ? 23f : 7f;
            foreach (var shaft in shafts)
                shaft.enabled = value == WaterQuality.High;
        }

        private void LateUpdate()
        {
            var sample = environment.ViewerSnapshot;
            suspendedMatter.transform.position = viewer.position;
            var force = suspendedMatter.forceOverLifetime;
            force.x = sample.CurrentMps.x * 0.19f;
            force.z = sample.CurrentMps.z * 0.19f;

            var current = sample.CurrentMps;
            properties.SetVector(Current, new Vector4(current.x, current.z, 0f, 0f));
            properties.SetFloat(LightFactor, sample.LightFactor);
            properties.SetFloat(Glints, quality == WaterQuality.High ? 1f : 0.28f);
            surface.SetPropertyBlock(properties);
            foreach (var patch in seagrass)
                patch.SetPropertyBlock(properties);

            // These local lights are the prototype's shallow moving light wells.
            var illumination = EnvironmentModel.Daylight(sample.LocalTime) * (1f - sample.CloudCover * 0.8f);
            for (var index = 0; index < shafts.Length; index++)
            {
                var drift = 1f + Mathf.Sin(Time.time * 0.74f + index * 2.1f) * 0.21f;
                shafts[index].intensity = illumination * drift * 4f;
                shafts[index].transform.localRotation = Quaternion.Euler(80f + Mathf.Sin(Time.time + index) * 3f,
                    index * 63f, 0f);
            }
        }
    }
}
