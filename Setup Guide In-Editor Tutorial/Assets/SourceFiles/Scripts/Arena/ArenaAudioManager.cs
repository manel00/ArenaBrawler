using UnityEngine;

namespace ArenaEnhanced
{
    public static class ArenaAudioManager
    {
        private static AudioSource _source;

        private static AudioSource GetSource()
        {
            if (_source == null)
            {
                var go = new GameObject("ArenaAudioManager");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.volume = 0.5f;
            }
            return _source;
        }

        public static void PlayFireball() => PlayTone(400f, 0.1f);
        public static void PlayHit() => PlayTone(200f, 0.08f);
        public static void PlayDeath() => PlayTone(100f, 0.3f);
        public static void PlayShield() => PlayTone(600f, 0.15f);
        public static void PlayDash() => PlayTone(500f, 0.08f);
        public static void PlayMelee() => PlayTone(250f, 0.1f);
        public static void PlayPickup() => PlayTone(800f, 0.1f);

        private static void PlayTone(float freq, float duration)
        {
            var src = GetSource();
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Max(0f, 1f - (float)i / samples);
            }
            clip.SetData(data, 0);
            src.PlayOneShot(clip);
        }
    }
}
