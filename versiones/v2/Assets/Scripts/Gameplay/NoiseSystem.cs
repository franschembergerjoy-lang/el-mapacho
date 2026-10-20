using System;
using UnityEngine;

namespace RobaYCorre
{
    /// <summary>
    /// Barra de ruido global (0-100). Los habitantes "oyen" el nivel actual atenuado por la
    /// distancia al último punto donde se hizo ruido y por las paredes que haya en medio.
    /// </summary>
    public class NoiseSystem : MonoBehaviour
    {
        public static NoiseSystem I { get; private set; }

        [Header("Umbrales de la barra")]
        public float investigateMark = 30f;
        public float searchMark = 60f;
        public float alertMark = 100f;

        [Header("Percepción")]
        [Tooltip("Hasta esta distancia el ruido se oye completo.")]
        public float fullVolumeDistance = 3f;
        [Tooltip("Multiplicador por haber una pared entre el ruido y quien escucha.")]
        [Range(0f, 1f)] public float wallAttenuation = 0.6f;

        [Header("Ruidos fuertes")]
        [Tooltip("Ruidos instantáneos a partir de este valor muestran el aviso '¡Hiciste ruido!'.")]
        public float burstMessageThreshold = 12f;

        public float Level { get; private set; }
        public Vector3 LastNoisePosition { get; private set; }
        public float LastNoiseTime { get; private set; } = -100f;

        /// <summary>Se dispara con ruidos instantáneos: (cantidad, posición).</summary>
        public event Action<float, Vector3> OnBurst;
        /// <summary>Se dispara cuando la barra cruza hacia arriba una marca (30, 60, 100).</summary>
        public event Action<int> OnThresholdCrossed;

        void Awake()
        {
            I = this;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        public void AddContinuous(float amount, Vector3 position, bool relocate)
        {
            if (amount <= 0f) return;
            SetLevel(Level + amount);
            if (relocate)
            {
                LastNoisePosition = position;
                LastNoiseTime = Time.time;
            }
        }

        public void Burst(float amount, Vector3 position)
        {
            if (amount <= 0f) return;
            LastNoisePosition = position;
            LastNoiseTime = Time.time;
            SetLevel(Level + amount);
            NoiseRing.Spawn(position, Mathf.Lerp(1.2f, 5f, Mathf.Clamp01(amount / 50f)), amount >= 30f ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.85f, 0.3f));
            OnBurst?.Invoke(amount, position);
        }

        public void ForceMax(Vector3 position)
        {
            LastNoisePosition = position;
            LastNoiseTime = Time.time;
            SetLevel(100f);
            NoiseRing.Spawn(position, 8f, new Color(1f, 0.2f, 0.2f));
        }

        public void Decay(float perSecond)
        {
            if (perSecond <= 0f || Level <= 0f) return;
            Level = Mathf.Max(0f, Level - perSecond * Time.deltaTime);
        }

        void SetLevel(float value)
        {
            float old = Level;
            Level = Mathf.Clamp(value, 0f, 100f);
            if (old < investigateMark && Level >= investigateMark) OnThresholdCrossed?.Invoke(30);
            if (old < searchMark && Level >= searchMark) OnThresholdCrossed?.Invoke(60);
            if (old < alertMark - 0.01f && Level >= alertMark - 0.01f) OnThresholdCrossed?.Invoke(100);
        }

        /// <summary>Cuánto ruido percibe alguien en esa posición.</summary>
        public float HeardAt(Vector3 listener, float hearingRadius)
        {
            if (Level <= 0.01f) return 0f;
            Vector3 a = listener; a.y = 0f;
            Vector3 b = LastNoisePosition; b.y = 0f;
            float d = Vector3.Distance(a, b);
            if (d >= hearingRadius) return 0f;
            float att = d <= fullVolumeDistance ? 1f : 1f - (d - fullVolumeDistance) / Mathf.Max(0.01f, hearingRadius - fullVolumeDistance);
            if (Physics.Linecast(a + Vector3.up * 1.0f, b + Vector3.up * 1.0f, Layers.WallMask, QueryTriggerInteraction.Ignore))
                att *= wallAttenuation;
            return Level * Mathf.Clamp01(att);
        }
    }
}
