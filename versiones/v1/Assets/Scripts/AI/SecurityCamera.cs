using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Cámara de seguridad que gira de lado a lado. Si te ve un instante, salta la alarma.</summary>
    public class SecurityCamera : MonoBehaviour, IWatcher
    {
        public Transform head;
        public Renderer led;
        [Tooltip("Ángulo total del barrido")] public float sweepAngle = 90f;
        [Tooltip("Velocidad del barrido (ciclos por minuto aprox.)")] public float sweepSpeed = 0.5f;
        public float viewDistance = 7.5f;
        public float viewAngle = 45f;
        public float detectTime = 0.6f;
        public float alarmCooldown = 6f;

        float baseYaw, t, suspicion, cooldown;
        VisionCone cone;
        MaterialPropertyBlock mpb;
        bool seesNow, couldSee;

        public bool IsWatchingPlayer => seesNow;
        public bool CouldSeePlayer => couldSee;
        public Vector3 IconPosition => transform.position + Vector3.up * 0.5f;
        public string IconText => cooldown > 0f ? "¡!" : (suspicion > 0.05f ? "?" : "");
        public Color IconColor => cooldown > 0f ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.85f, 0.2f);

        void OnEnable() { if (!Perception.Watchers.Contains(this)) Perception.Watchers.Add(this); }
        void OnDisable() { Perception.Watchers.Remove(this); }

        void Start()
        {
            baseYaw = transform.eulerAngles.y;
            cone = VisionCone.Create(name);
            mpb = new MaterialPropertyBlock();
            t = Random.value * 10f;
        }

        Vector3 Forward => Quaternion.Euler(0f, baseYaw + Mathf.Sin(t * sweepSpeed * Mathf.PI * 2f / 6f) * sweepAngle * 0.5f, 0f) * Vector3.forward;

        void Update()
        {
            var lm = LevelManager.I;
            if (lm == null || lm.player == null) return;
            if (lm.State != LevelState.Playing) { seesNow = couldSee = false; return; }
            float dt = Time.deltaTime;
            if (suspicion <= 0.01f) t += dt;   // se detiene a mirar si sospecha
            Vector3 fwd = Forward;
            if (head != null) head.rotation = Quaternion.LookRotation(fwd) * Quaternion.Euler(30f, 0f, 0f);

            var p = lm.player;
            Vector3 floorOrigin = new Vector3(transform.position.x, 0f, transform.position.z);
            couldSee = Perception.CanSee(transform.position, fwd, viewDistance, viewAngle, p.TargetPoint);
            seesNow = couldSee && !p.IsHidden;
            if (cooldown > 0f) cooldown -= dt;

            if (seesNow && cooldown <= 0f)
            {
                suspicion += dt / detectTime;
                if (suspicion >= 1f)
                {
                    suspicion = 0f;
                    cooldown = alarmCooldown;
                    lm.NotifySpotted("una cámara");
                    lm.TriggerAlarm(p.transform.position, "¡Una cámara te grabó! ¡Alarma!");
                }
            }
            else suspicion = Mathf.Max(0f, suspicion - dt);

            Color c = cooldown > 0f ? new Color(1f, 0.15f, 0.15f, 0.35f) : (suspicion > 0.05f ? new Color(1f, 0.7f, 0.1f, 0.32f) : new Color(0.4f, 0.8f, 1f, 0.22f));
            cone?.Show(floorOrigin, fwd, viewDistance, viewAngle, c);
            if (led != null)
            {
                bool blink = Mathf.Repeat(Time.time, 1f) < 0.5f;
                mpb.SetColor("_BaseColor", cooldown > 0f || blink ? Color.red : new Color(0.3f, 0f, 0f));
                mpb.SetColor("_EmissionColor", (cooldown > 0f || blink ? Color.red : Color.black) * 2f);
                led.SetPropertyBlock(mpb);
            }
        }

        void OnDestroy()
        {
            if (cone != null) Destroy(cone.gameObject);
        }
    }
}
