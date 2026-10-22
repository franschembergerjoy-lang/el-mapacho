using System.Collections;
using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Objeto frágil (jarrón, platos): cae y se rompe con mucho ruido si pasas corriendo o chocas su mueble.</summary>
    public class Fragile : MonoBehaviour
    {
        public string displayName = "un jarrón";
        public float noise = 28f;
        [Tooltip("Si el mapache pasa corriendo a esta distancia, lo tira")]
        public float runTriggerRadius = 0.85f;
        public Mesh shardMesh;
        public Material shardMaterial;

        bool fallen;

        void Update()
        {
            if (fallen) return;
            var lm = LevelManager.I;
            if (lm == null || lm.State != LevelState.Playing || lm.player == null) return;
            var p = lm.player;
            if (p.IsRunning && Perception.FlatDistance(p.transform.position, transform.position) < runTriggerRadius)
                Fall(p.MoveDirection);
        }

        public void Fall(Vector3 dir)
        {
            if (fallen) return;
            fallen = true;
            StartCoroutine(FallRoutine(dir));
        }

        IEnumerator FallRoutine(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            transform.SetParent(null, true);
            Vector3 start = transform.position;
            Vector3 end = new Vector3(start.x, 0.02f, start.z) + dir * 0.6f;
            float t = 0f;
            const float dur = 0.4f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                Vector3 p = Vector3.Lerp(start, end, k);
                p.y = Mathf.Lerp(start.y, end.y, k * k) + Mathf.Sin(k * Mathf.PI) * 0.25f;
                transform.position = p;
                transform.Rotate(dir.z * 500f * Time.deltaTime, 0f, -dir.x * 500f * Time.deltaTime, Space.World);
                yield return null;
            }

            if (NoiseSystem.I != null) NoiseSystem.I.Burst(noise, end);
            AudioManager.Play(Sfx.Crash);
            LevelManager.Toast($"¡Crash! Se rompió {displayName}", HUD.Bad);

            // Pedacitos.
            if (shardMesh != null && shardMaterial != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    var s = new GameObject("Pedazo");
                    s.AddComponent<MeshFilter>().sharedMesh = shardMesh;
                    s.AddComponent<MeshRenderer>().sharedMaterial = shardMaterial;
                    float a = i * 60f + Random.Range(-20f, 20f);
                    Vector3 off = Quaternion.Euler(0f, a, 0f) * Vector3.forward * Random.Range(0.15f, 0.45f);
                    s.transform.position = end + off + Vector3.up * 0.03f;
                    s.transform.rotation = Random.rotation;
                    s.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);
                }
            }
            gameObject.SetActive(false);
        }
    }
}
