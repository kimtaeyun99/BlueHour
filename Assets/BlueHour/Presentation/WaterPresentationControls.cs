using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueHour.Presentation
{
    public sealed class WaterPresentationControls : MonoBehaviour
    {
        [SerializeField] private ProceduralWaterAudio waterAudio;
        [SerializeField] private UnderwaterAtmosphere atmosphere;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !Application.isFocused)
                return;

            if (keyboard.f6Key.wasPressedThisFrame)
                waterAudio.SetMode((WaterAudioMode)(((int)waterAudio.Mode + 1) % 4));
            if (keyboard.f7Key.wasPressedThisFrame)
                waterAudio.SetAmbienceVolume(NextVolume(waterAudio.AmbienceVolume));
            if (keyboard.f8Key.wasPressedThisFrame)
                waterAudio.SetMusicVolume(NextVolume(waterAudio.MusicVolume));
            if (keyboard.f9Key.wasPressedThisFrame)
                atmosphere.SetQuality(atmosphere.Quality == WaterQuality.High ? WaterQuality.Low : WaterQuality.High);
        }

        private static float NextVolume(float volume) => volume >= 0.95f ? 0f : Mathf.Min(1f, volume + 0.25f);
    }
}
