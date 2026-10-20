using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Parpadeo suave de una luz (fuego de la chimenea).</summary>
    [RequireComponent(typeof(Light))]
    public class Flicker : MonoBehaviour
    {
        public float amount = 0.35f;
        public float speed = 6f;
        Light l;
        float baseIntensity;

        void Awake()
        {
            l = GetComponent<Light>();
            baseIntensity = l.intensity;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(Time.time * speed, transform.position.x);
            l.intensity = baseIntensity * (1f - amount * 0.5f + amount * n);
        }
    }
}
