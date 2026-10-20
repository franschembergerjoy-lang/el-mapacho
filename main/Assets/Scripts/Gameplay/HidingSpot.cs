using System.Collections.Generic;
using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Escondite: debajo de la cama, dentro del armario, detrás de la cortina...</summary>
    public class HidingSpot : MonoBehaviour
    {
        public static readonly List<HidingSpot> All = new List<HidingSpot>();

        [Tooltip("Se muestra como 'Esconderse {spotName}'")]
        public string spotName = "debajo de la cama";
        public Transform hidePoint;
        [Tooltip("Puntos por donde se puede entrar y salir")]
        public Transform[] exitPoints;
        [Tooltip("Ojitos que asoman mientras el mapache está escondido")]
        public GameObject peekEyes;
        [Tooltip("Parte que se sacude al entrar o salir")]
        public Transform wobblePart;

        public bool Occupied { get; private set; }
        float wobble;
        Vector3 wobbleBase = Vector3.one;

        void Awake()
        {
            if (wobblePart != null) wobbleBase = wobblePart.localScale;
            if (peekEyes != null) peekEyes.SetActive(false);
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public string Prompt => $"[E] Esconderse {spotName}";

        /// <summary>Distancia desde un punto hasta la entrada más cercana.</summary>
        public float DistanceTo(Vector3 from)
        {
            float best = float.MaxValue;
            if (exitPoints == null || exitPoints.Length == 0) return Perception.FlatDistance(from, transform.position);
            foreach (var e in exitPoints)
                if (e != null) best = Mathf.Min(best, Perception.FlatDistance(from, e.position));
            return best;
        }

        public Vector3 FindFreeExit(Vector3 preferred)
        {
            Vector3 best = transform.position;
            float bestScore = float.MaxValue;
            bool found = false;
            if (exitPoints != null)
            {
                foreach (var e in exitPoints)
                {
                    if (e == null) continue;
                    Vector3 p = e.position; p.y = 0f;
                    bool blocked = Physics.CheckSphere(p + Vector3.up * 0.4f, 0.26f, Layers.ObstacleMask, QueryTriggerInteraction.Ignore);
                    float score = Perception.FlatDistance(p, preferred) + (blocked ? 1000f : 0f);
                    if (score < bestScore) { bestScore = score; best = p; found = true; }
                }
            }
            if (!found) best = transform.position + transform.forward * 1f;
            best.y = 0.02f;
            return best;
        }

        public void Enter()
        {
            Occupied = true;
            wobble = 0.35f;
            if (peekEyes != null) peekEyes.SetActive(true);
        }

        public void Exit()
        {
            Occupied = false;
            wobble = 0.35f;
            if (peekEyes != null) peekEyes.SetActive(false);
        }

        void Update()
        {
            if (wobblePart == null) return;
            if (wobble > 0f)
            {
                wobble -= Time.deltaTime;
                float k = Mathf.Sin(wobble * 40f) * wobble * 0.25f;
                wobblePart.localScale = new Vector3(wobbleBase.x * (1f + k), wobbleBase.y * (1f - k), wobbleBase.z * (1f + k));
            }
            else
            {
                wobblePart.localScale = wobbleBase;
            }
            if (peekEyes != null && Occupied)
            {
                // Parpadeo de los ojos del mapache escondido.
                bool blink = Mathf.Repeat(Time.time, 3.2f) < 0.12f;
                peekEyes.transform.localScale = new Vector3(1f, blink ? 0.1f : 1f, 1f);
            }
        }
    }
}
