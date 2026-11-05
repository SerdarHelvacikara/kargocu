using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        static readonly int[][] StreetMelodies =
        {
            new[] { 0, 7, 4, -1, 7, 9, 7, -1, 5, 9, 7, -1, 4, 2, 0, -1 },
            new[] { 0, 4, 7, 9, -1, 7, 4, 2, 5, 7, 10, -1, 9, 7, 4, -1 },
            new[] { 0, 2, 7, -1, 9, 7, 4, -1, 2, 4, 9, -1, 7, 4, 2, -1 },
            new[] { 0, -1, 7, 4, 0, 9, 7, -1, 5, -1, 9, 7, 5, 4, 2, -1 }
        };
        static readonly float[] StreetTempos = { .31f, .28f, .34f, .26f };
        readonly AudioClip[] streetTracks = new AudioClip[DistrictCount];
        AudioSource musicSource;
        int musicDistrict = -1;
        bool musicActive;

        internal bool MusicRunning => musicActive && musicSource && musicSource.isPlaying;
        internal float TrafficMargin
        {
            get
            {
                if (!UsesWideVehicleBody) return controller.radius;
                Vector3 size = WideBodyHalfSize;
                return Mathf.Sqrt(size.x * size.x + size.z * size.z);
            }
        }

        void InitializeStreetMusic()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0;
        }

        void TickStreetMusic()
        {
            bool shouldPlay = Phase == GamePhase.Playing && !IsMuted && Volume > .001f;
            if (!shouldPlay)
            {
                if (musicActive) musicSource.Pause();
                musicActive = false;
                return;
            }
            if (musicDistrict != DistrictIndex)
            {
                musicDistrict = DistrictIndex;
                if (!streetTracks[musicDistrict]) streetTracks[musicDistrict] = BuildStreetTrack(musicDistrict);
                musicSource.clip = streetTracks[musicDistrict];
                musicSource.Play();
            }
            else if (!musicActive) musicSource.UnPause();
            musicActive = true;
            musicSource.volume = .18f * Volume * (PanicTimeLeft > 0 ? .37f : 1);
        }

        AudioClip BuildStreetTrack(int district)
        {
            const int rate = 16000;
            int[] melody = StreetMelodies[district];
            int samplesPerBeat = Mathf.RoundToInt(StreetTempos[district] * rate);
            var samples = new float[samplesPerBeat * melody.Length * 2];
            for (int beat = 0; beat < melody.Length * 2; beat++)
            {
                int note = melody[beat % melody.Length];
                float pitch = note < 0 ? 0 : 261.626f * Mathf.Pow(2, note / 12f) * (beat < melody.Length ? 1 : 1.12246f);
                float bass = 130.813f * (beat / 4 % 2 == 0 ? 1 : 1.33484f);
                for (int i = 0; i < samplesPerBeat; i++)
                {
                    float t = i / (float)rate;
                    float envelope = Mathf.Clamp01(t * 170) * Mathf.Exp(-t * 13) *
                        Mathf.Clamp01((samplesPerBeat - i - 1) * .012f);
                    float pluck = pitch <= 0 ? 0 : Mathf.Sin(t * pitch * 2 * Mathf.PI) +
                        .24f * Mathf.Sin(t * pitch * 4 * Mathf.PI);
                    float low = beat % 2 == 0 ? Mathf.Sin(t * bass * 2 * Mathf.PI) * .31f : 0;
                    samples[beat * samplesPerBeat + i] = (pluck * .40f + low) * envelope;
                }
            }
            AudioClip clip = AudioClip.Create("Procedural neighborhood tune " + district, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy()
        {
            foreach (AudioClip track in streetTracks) if (track) Destroy(track);
        }
    }
}
