using UnityEngine;

namespace RobaYCorre
{
    /// <summary>
    /// Sensor de movimiento: zona roja en el suelo que se enciende y apaga. Si está encendida y la cruzas
    /// caminando o corriendo, salta la alarma. Agachado y despacio no te detecta.
    /// </summary>
    public class MotionSensor : MonoBehaviour
    {
        public Vector2 zoneSize = new Vector2(3f, 2f);
        public float onTime = 3.5f;
        public float offTime = 2f;
        public float timeOffset;
        [Tooltip("Velocidad mínima que detecta (agachado vas a 1.3)")]
        public float speedThreshold = 1.6f;
        public float detectTime = 0.25f;
        public float alarmCooldown = 6f;
        public Renderer zone;
        public Renderer led;

        float detect, cooldown;
        MaterialPropertyBlock mpb;

        public bool IsOn
        {
            get
            {
                float cycle = onTime + offTime;
                return Mathf.Repeat(Time.time + timeOffset, cycle) < onTime;
            }
        }

        bool PlayerInside(RaccoonController p)
        {
            Vector3 local = transform.InverseTransformPoint(p.transform.position);
            return Mathf.Abs(local.x) <= zoneSize.x * 0.5f && Mathf.Abs(local.z) <= zoneSize.y * 0.5f;
        }

        void Update()
        {
            var lm = LevelManager.I;
            if (lm == null || lm.player == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            bool on = IsOn;
            float dt = Time.deltaTime;
            if (cooldown > 0f) cooldown -= dt;

            if (lm.State == LevelState.Playing && on && cooldown <= 0f)
            {
                var p = lm.player;
                if (!p.IsHidden && PlayerInside(p) && p.CurrentSpeed > speedThreshold)
                {
                    detect += dt;
                    if (detect >= detectTime)
                    {
                        detect = 0f;
                        cooldown = alarmCooldown;
                        lm.NotifySpotted("un sensor");
                        lm.TriggerAlarm(p.transform.position, "¡Un sensor de movimiento te detectó!");
                    }
                }
                else detect = 0f;
            }

            if (zone != null)
            {
                Color c = cooldown > 0f ? new Color(1f, 0.1f, 0.1f, 0.45f) : (on ? new Color(1f, 0.2f, 0.25f, 0.28f + 0.06f * Mathf.Sin(Time.time * 8f)) : new Color(0.3f, 1f, 0.5f, 0.12f));
                mpb.SetColor("_BaseColor", c);
                zone.SetPropertyBlock(mpb);
            }
            if (led != null)
            {
                var lp = new MaterialPropertyBlock();
                lp.SetColor("_BaseColor", on ? Color.red : Color.green);
                lp.SetColor("_EmissionColor", (on ? Color.red : Color.green) * 2f);
                led.SetPropertyBlock(lp);
            }
        }
    }
}
