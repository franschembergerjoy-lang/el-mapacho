using System.Collections;
using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Mueble que se puede chocar: las sillas y lámparas se caen, las mesas tambalean y tiran lo frágil.</summary>
    public class Bumpable : MonoBehaviour
    {
        public string displayName = "una silla";
        public float noise = 18f;
        [Tooltip("Se cae al chocarla (sillas, lámparas). Si no, solo tambalea y requiere ir corriendo.")]
        public bool tipsOver = true;
        public Fragile[] fragiles;
        public Light lightToTurnOff;

        bool tipped;
        bool busy;

        public void Bump(Vector3 direction)
        {
            if (busy) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            direction.Normalize();

            if (fragiles != null)
                foreach (var f in fragiles)
                    if (f != null) f.Fall(direction);

            if (tipsOver && !tipped)
            {
                tipped = true;
                StartCoroutine(TipOver(direction));
                if (NoiseSystem.I != null) NoiseSystem.I.Burst(noise, transform.position);
                AudioManager.Play(Sfx.Bump, 1f, 0.8f);
                LevelManager.Toast($"¡Ups! Tiraste {displayName}", HUD.Warn);
            }
            else if (!tipsOver)
            {
                StartCoroutine(Wobble(direction));
                if (NoiseSystem.I != null) NoiseSystem.I.Burst(noise * 0.6f, transform.position);
                AudioManager.Play(Sfx.Bump);
            }
        }

        IEnumerator TipOver(Vector3 dir)
        {
            busy = true;
            Vector3 axis = Vector3.Cross(Vector3.up, dir);
            Vector3 pivot = transform.position + dir * 0.25f;
            float total = 0f;
            float target = 82f;
            while (total < target)
            {
                float step = Mathf.Min(target - total, Time.deltaTime * 300f);
                transform.RotateAround(pivot, axis, step);
                total += step;
                yield return null;
            }
            if (lightToTurnOff != null) lightToTurnOff.enabled = false;
            busy = false;
        }

        IEnumerator Wobble(Vector3 dir)
        {
            busy = true;
            Quaternion start = transform.rotation;
            Vector3 axis = Vector3.Cross(Vector3.up, dir);
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float a = Mathf.Sin(t * 35f) * (0.5f - t) * 8f;
                transform.rotation = Quaternion.AngleAxis(a, axis) * start;
                yield return null;
            }
            transform.rotation = start;
            busy = false;
        }
    }
}
