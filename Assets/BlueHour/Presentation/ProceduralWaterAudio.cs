using System;
using BlueHour.Environment;
using UnityEngine;

namespace BlueHour.Presentation
{
    public enum WaterAudioMode { Ambience, AmbientMusic, AlternateMusic, Muted }

    public sealed class ProceduralWaterAudio : MonoBehaviour
    {
        private const int SampleRate = 24000;
        private const int LoopSeconds = 8;
        [SerializeField] private ReefEnvironment environment;
        [SerializeField] private AudioSource waterBed;
        [SerializeField] private AudioSource reefFlow;
        [SerializeField] private AudioSource ambientMusic;
        [SerializeField] private AudioSource alternateMusic;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [SerializeField] private WaterAudioMode mode;

        private AudioLowPassFilter depthFilter;
        private AudioClip bedClip;
        private AudioClip flowClip;
        private AudioClip musicClip;
        private AudioClip alternateClip;

        public WaterAudioMode Mode => mode;
        public float AmbienceVolume => ambienceVolume;
        public float MusicVolume => musicVolume;

        private void Awake()
        {
            depthFilter = waterBed.GetComponent<AudioLowPassFilter>();
            bedClip = CreateLoop("Synthesized water bed", 183, 0);
            flowClip = CreateLoop("Synthesized reef flow", 625, 0);
            musicClip = CreateLoop("Prototype slow tones", 28, 1);
            alternateClip = CreateLoop("Prototype low tones", 71, 2);
            Attach(waterBed, bedClip);
            Attach(reefFlow, flowClip);
            Attach(ambientMusic, musicClip);
            Attach(alternateMusic, alternateClip);
        }

        private static void Attach(AudioSource source, AudioClip clip)
        {
            source.loop = true;
            source.clip = clip;
            source.volume = 0f;
        }

        private void OnEnable()
        {
            waterBed.Play();
            reefFlow.Play();
            ambientMusic.Play();
            alternateMusic.Play();
        }

        private void OnDisable()
        {
            waterBed.Stop();
            reefFlow.Stop();
            ambientMusic.Stop();
            alternateMusic.Stop();
        }

        private void OnDestroy()
        {
            Destroy(bedClip);
            Destroy(flowClip);
            Destroy(musicClip);
            Destroy(alternateClip);
        }

        public void SetMode(WaterAudioMode value) => mode = value;
        public void SetAmbienceVolume(float value) => ambienceVolume = Mathf.Clamp01(value);
        public void SetMusicVolume(float value) => musicVolume = Mathf.Clamp01(value);

        private void Update()
        {
            var sample = environment.ViewerSnapshot;
            var transition = Time.unscaledDeltaTime * 0.55f;
            var bed = mode == WaterAudioMode.Muted ? 0f : ambienceVolume;
            MoveVolume(waterBed, bed * Mathf.Lerp(0.25f, 0.48f, sample.CloudCover), transition);
            MoveVolume(reefFlow, bed * Mathf.Lerp(0.18f, 0.70f, sample.CurrentMps.magnitude), transition);
            MoveVolume(ambientMusic, mode == WaterAudioMode.AmbientMusic ? musicVolume * 0.62f : 0f, transition);
            MoveVolume(alternateMusic, mode == WaterAudioMode.AlternateMusic ? musicVolume * 0.62f : 0f, transition);
            depthFilter.cutoffFrequency = Mathf.Lerp(1650f, 290f, Mathf.Clamp01(sample.DepthM / 110f));
        }

        private static void MoveVolume(AudioSource source, float target, float step)
        {
            source.volume = Mathf.MoveTowards(source.volume, target, step);
        }

        private static AudioClip CreateLoop(string label, int seed, int voice)
        {
            var values = new float[LoopSeconds * SampleRate];
            var random = new System.Random(seed);
            var phases = new double[12];
            for (var index = 0; index < phases.Length; index++)
                phases[index] = random.NextDouble() * Math.PI * 2d;

            for (var index = 0; index < values.Length; index++)
            {
                var phase = index * (Math.PI * 2d / values.Length);
                var tone = 0d;
                for (var partial = 0; partial < phases.Length; partial++)
                {
                    var waveCount = voice switch
                    {
                        1 => 261 + (partial % 3) * 87 + partial / 3 * 2,
                        2 => 196 + (partial % 3) * 65 + partial / 3,
                        _ => 42 + 9 * partial * partial
                    };
                    tone += Math.Sin(waveCount * phase + phases[partial]) * (0.055d / (1d + partial * 0.16d));
                }

                var envelope = 0.71d + 0.21d * Math.Sin(phase + phases[0]) + 0.08d * Math.Sin(3d * phase);
                values[index] = (float)(tone * envelope * (voice == 0 ? 0.46d : 0.25d));
            }

            var clip = AudioClip.Create(label, values.Length, 1, SampleRate, false);
            clip.SetData(values, 0);
            return clip;
        }
    }
}
