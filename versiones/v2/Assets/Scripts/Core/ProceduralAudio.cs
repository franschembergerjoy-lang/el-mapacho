using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Genera efectos y una música ambiental sencilla sin archivos de audio.</summary>
    public static class ProceduralAudio
    {
        const int Rate = 22050;
        static readonly System.Random rng = new System.Random(1234);

        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        static float Env(float t, float attack, float decay) => t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);
        static float Sq(float phase) => Mathf.Sin(phase) >= 0 ? 1f : -1f;
        static float Saw(float phase) => (phase / (2f * Mathf.PI)) % 1f * 2f - 1f;
        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        delegate float Gen(float t, float dur);

        static AudioClip Make(string name, float dur, Gen g)
        {
            int n = Mathf.Max(1, (int)(dur * Rate));
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float fade = Mathf.Clamp01((dur - t) / 0.02f);
                d[i] = Mathf.Clamp(g(t, dur) * fade, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        public static AudioClip Build(Sfx s)
        {
            const float TwoPi = Mathf.PI * 2f;
            switch (s)
            {
                case Sfx.Pickup:
                    return Make("Pickup", 0.18f, (t, d) => Mathf.Sin(TwoPi * (520f * t + 1600f * t * t)) * Env(t, 0.005f, 0.08f) * 0.5f);
                case Sfx.Coin:
                    return Make("Coin", 0.3f, (t, d) =>
                    {
                        float f = t < 0.08f ? 988f : 1319f;
                        float tt = t < 0.08f ? t : t - 0.08f;
                        return (Mathf.Sin(TwoPi * f * t) * 0.7f + Sq(TwoPi * f * t) * 0.15f) * Env(tt, 0.003f, 0.09f) * 0.45f;
                    });
                case Sfx.Bump:
                    return Make("Bump", 0.3f, (t, d) => Mathf.Sin(TwoPi * (90f - 60f * t) * t) * Mathf.Exp(-t * 14f) * 0.8f + Noise() * Mathf.Exp(-t * 40f) * 0.3f);
                case Sfx.Crash:
                    return Make("Crash", 0.7f, (t, d) => Noise() * Mathf.Exp(-t * 6f) * 0.5f + (Mathf.Sin(TwoPi * 3100f * t) + Mathf.Sin(TwoPi * 4270f * t)) * Mathf.Exp(-t * 9f) * 0.12f);
                case Sfx.Spotted:
                    return Make("Spotted", 0.4f, (t, d) =>
                    {
                        float f = t < 0.12f ? 660f : 990f;
                        return (Sq(TwoPi * f * t) * 0.25f + Mathf.Sin(TwoPi * f * t) * 0.3f) * Env(t < 0.12f ? t : t - 0.12f, 0.005f, 0.12f);
                    });
                case Sfx.Hide:
                    {
                        float lp = 0f;
                        return Make("Hide", 0.35f, (t, d) => { lp += (Noise() - lp) * 0.08f; return lp * Mathf.Sin(Mathf.PI * t / d) * 1.6f; });
                    }
                case Sfx.Unhide:
                    {
                        float lp = 0f;
                        return Make("Unhide", 0.25f, (t, d) => { lp += (Noise() - lp) * 0.15f; return lp * Mathf.Sin(Mathf.PI * t / d) * 1.2f; });
                    }
                case Sfx.Escape:
                    return Make("Escape", 0.75f, (t, d) =>
                    {
                        int[] notes = { 72, 76, 79, 84 };
                        int idx = Mathf.Min(3, (int)(t / 0.12f));
                        float tt = t - idx * 0.12f;
                        float f = Midi(notes[idx]);
                        return (Mathf.Sin(TwoPi * f * t) * 0.6f + Sq(TwoPi * f * t) * 0.1f) * Env(tt, 0.004f, idx == 3 ? 0.3f : 0.1f) * 0.5f;
                    });
                case Sfx.Caught:
                    return Make("Caught", 1.1f, (t, d) =>
                    {
                        int step = Mathf.Min(3, (int)(t / 0.25f));
                        float f = Midi(64 - step * 2) * (1f + 0.02f * Mathf.Sin(TwoPi * 6f * t));
                        float tt = t - step * 0.25f;
                        return Saw(TwoPi * f * t) * Env(tt, 0.01f, step == 3 ? 0.4f : 0.15f) * 0.3f;
                    });
                case Sfx.Bark:
                    return Make("Bark", 0.5f, (t, d) =>
                    {
                        float tt = t < 0.22f ? t : t - 0.22f;
                        if (tt > 0.15f) return 0f;
                        float f = 420f - 900f * tt;
                        return (Saw(TwoPi * f * tt) * 0.5f + Noise() * 0.3f) * Env(tt, 0.01f, 0.05f) * 0.7f;
                    });
                case Sfx.Alarm:
                    return Make("Alarm", 1.4f, (t, d) =>
                    {
                        float f = ((int)(t / 0.2f)) % 2 == 0 ? 920f : 680f;
                        return Sq(TwoPi * f * t) * 0.22f;
                    });
                case Sfx.Drop:
                    return Make("Drop", 0.2f, (t, d) => Mathf.Sin(TwoPi * (320f * t - 500f * t * t)) * Env(t, 0.003f, 0.06f) * 0.6f);
                case Sfx.Click:
                    return Make("Click", 0.05f, (t, d) => Mathf.Sin(TwoPi * 1500f * t) * Env(t, 0.001f, 0.015f) * 0.4f);
                case Sfx.Crunch:
                    return Make("Crunch", 0.45f, (t, d) => (((int)(t * 40f)) % 3 == 0 ? Noise() : Noise() * 0.2f) * Mathf.Exp(-t * 4f) * 0.6f);
                case Sfx.Deposit:
                    return Make("Deposit", 0.6f, (t, d) =>
                    {
                        float v = 0f;
                        for (int i = 0; i < 4; i++)
                        {
                            float tt = t - i * 0.1f;
                            if (tt < 0) continue;
                            v += Mathf.Sin(TwoPi * (1200f + i * 180f) * tt) * Env(tt, 0.002f, 0.07f);
                        }
                        return v * 0.3f;
                    });
                case Sfx.Full:
                    return Make("Full", 0.3f, (t, d) => Sq(TwoPi * 140f * t) * Env(t, 0.005f, 0.12f) * 0.25f);
                case Sfx.Question:
                    return Make("Question", 0.35f, (t, d) => Mathf.Sin(TwoPi * (260f * t + 260f * t * t)) * Env(t, 0.02f, 0.15f) * 0.5f);
                case Sfx.Wake:
                    return Make("Wake", 0.5f, (t, d) => Mathf.Sin(TwoPi * (180f * t + 200f * t * t)) * Env(t, 0.05f, 0.2f) * 0.5f + Noise() * Env(t, 0.05f, 0.1f) * 0.05f);
            }
            return Make(s.ToString(), 0.05f, (t, d) => 0f);
        }

        /// <summary>Bucle de música "de puntillas": bajo pizzicato y melodía misteriosa.</summary>
        public static AudioClip Music()
        {
            const float TwoPi = Mathf.PI * 2f;
            const float bpm = 100f;
            float beat = 60f / bpm;
            int bars = 8;
            float dur = bars * 4 * beat;
            int n = (int)(dur * Rate);
            var data = new float[n];

            // Bajo: una nota por pulso (La menor, misterioso y juguetón).
            int[] bass = { 45, 48, 52, 48, 45, 48, 52, 53, 41, 45, 48, 45, 40, 44, 47, 44,
                           45, 48, 52, 48, 45, 48, 52, 53, 41, 45, 48, 45, 40, 44, 47, 40 };
            for (int i = 0; i < bass.Length; i++)
                AddNote(data, i * beat, beat * 0.9f, Midi(bass[i]), 0.22f, 0.18f, false);

            // Melodía en corcheas con silencios (-1).
            int[] mel = { -1, 69, -1, 72, 71, -1, 69, -1,  -1, 76, -1, 75, 76, -1, -1, -1,
                          -1, 65, -1, 69, 72, -1, 71, -1,  68, -1, 71, -1, 68, -1, -1, -1,
                          -1, 69, -1, 72, 71, -1, 69, -1,  -1, 76, -1, 75, 76, -1, 79, -1,
                          77, -1, 76, -1, 74, -1, 72, -1,  71, -1, 68, -1, 69, -1, -1, -1 };
            for (int i = 0; i < mel.Length; i++)
                if (mel[i] > 0) AddNote(data, i * beat * 0.5f, beat * 0.45f, Midi(mel[i]), 0.08f, 0.12f, true);

            // Platillo suave en contratiempo.
            var r = new System.Random(7);
            for (int i = 0; i < bars * 8; i++)
            {
                if (i % 2 == 0) continue;
                int start = (int)(i * beat * 0.5f * Rate);
                int len = (int)(0.05f * Rate);
                for (int k = 0; k < len && start + k < n; k++)
                    data[start + k] += (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-k / (float)Rate * 60f) * 0.025f;
            }

            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create("MusicaAmbiente", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;

            void AddNote(float[] buf, float start, float length, float freq, float vol, float decay, bool bright)
            {
                int s0 = (int)(start * Rate);
                int len = (int)(length * Rate);
                for (int k = 0; k < len && s0 + k < buf.Length; k++)
                {
                    float t = k / (float)Rate;
                    float env = Env(t, 0.005f, decay) * Mathf.Clamp01((length - t) / 0.03f);
                    float v = Mathf.Sin(TwoPi * freq * t);
                    if (bright) v = v * 0.7f + Mathf.Sin(TwoPi * freq * 2f * t) * 0.2f + Mathf.Sin(TwoPi * freq * 3f * t) * 0.1f;
                    buf[s0 + k] += v * env * vol;
                }
            }
        }
    }
}
