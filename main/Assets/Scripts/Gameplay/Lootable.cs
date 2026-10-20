using System.Collections.Generic;
using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Objeto robable. Todos los valores se ajustan desde el Inspector.</summary>
    public class Lootable : MonoBehaviour
    {
        public static readonly List<Lootable> All = new List<Lootable>();

        public string itemName = "Galletas";
        [Tooltip("Monedas que vale")] public int value = 5;
        [Tooltip("Espacio que ocupa en la mochila (capacidad 10)")] [Range(1, 6)] public int weight = 1;
        [Tooltip("Ruido base al recogerlo (se multiplica en el mapache)")] public float pickupNoise = 2f;
        [Tooltip("Objeto especial obligatorio en algunos niveles")] public bool isSpecial;
        [Tooltip("Al recogerlo suena una alarma que alerta a todos")] public bool triggersAlarm;
        [TextArea] public string funnyLine;

        [Header("Visual")]
        public Transform sparkle;
        public GameObject glow;

        public bool IsCarried { get; private set; }
        Vector3 baseScale;
        bool highlighted;
        float pulse;
        Vector3 sparkleBase;

        void Awake()
        {
            baseScale = transform.localScale;
            if (sparkle != null) sparkleBase = sparkle.localPosition;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public string NoiseDescription
        {
            get
            {
                float n = pickupNoise;
                if (triggersAlarm) return "¡ALARMA!";
                if (n >= 12f) return "muy ruidoso";
                if (n >= 7f) return "ruidoso";
                if (n >= 4f) return "algo de ruido";
                return "silencioso";
            }
        }

        public string Prompt => $"[E] Robar {itemName}   ·   {value} monedas   ·   espacio {weight}   ·   {NoiseDescription}";

        public void SetHighlight(bool on)
        {
            if (highlighted == on) return;
            highlighted = on;
            if (!on) transform.localScale = IsCarried ? transform.localScale : baseScale;
        }

        void Update()
        {
            if (IsCarried) return;
            if (highlighted)
            {
                pulse += Time.deltaTime * 7f;
                transform.localScale = baseScale * (1f + 0.1f * Mathf.Sin(pulse));
            }
            if (sparkle != null)
            {
                sparkle.localPosition = sparkleBase + Vector3.up * (Mathf.Sin(Time.time * 2.5f + transform.position.x) * 0.06f);
                sparkle.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            }
        }

        public void OnPicked(Transform carrier)
        {
            IsCarried = true;
            highlighted = false;
            transform.localScale = baseScale;
            if (sparkle != null) sparkle.gameObject.SetActive(false);
            if (glow != null) glow.SetActive(false);
            transform.SetParent(carrier, false);
            gameObject.SetActive(false);
        }

        /// <summary>Lo muestra en una posición del mapache (cabeza o en brazos) con una escala dada.</summary>
        public void ShowCarried(Transform slot, float scale)
        {
            transform.SetParent(slot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = baseScale * scale;
            gameObject.SetActive(true);
        }

        public void HideCarried(Transform storage)
        {
            transform.SetParent(storage, false);
            gameObject.SetActive(false);
        }

        public void OnDropped(Vector3 floorPosition, Quaternion rotation)
        {
            IsCarried = false;
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(floorPosition, rotation);
            transform.localScale = baseScale;
            if (sparkle != null) sparkle.gameObject.SetActive(true);
            if (glow != null) glow.SetActive(true);
            gameObject.SetActive(true);
        }
    }
}
