using UnityEngine;

namespace RobaYCorre
{
    /// <summary>
    /// Cámara inclinada de tres cuartos que sigue al mapache con suavidad. Las paredes y muebles altos
    /// que quedan entre la cámara y el mapache se vuelven transparentes.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        [Range(35f, 75f)] public float pitch = 55f;
        public float yaw = 0f;
        public float distance = 12f;
        public float smoothTime = 0.25f;
        [Tooltip("Adelanta el encuadre hacia donde se mueve el mapache")]
        public float lookAhead = 1.2f;
        public float fadeRadius = 0.9f;

        Vector3 focus, vel, ahead;
        readonly RaycastHit[] hits = new RaycastHit[24];

        void Start()
        {
            if (target != null) focus = target.position;
            Place();
        }

        void LateUpdate()
        {
            if (target == null) return;
            var rc = target.GetComponent<RaccoonController>();
            Vector3 wantAhead = rc != null && rc.IsMoving ? rc.MoveDirection * lookAhead : Vector3.zero;
            ahead = Vector3.Lerp(ahead, wantAhead, Time.unscaledDeltaTime * 2f);
            focus = Vector3.SmoothDamp(focus, target.position + ahead, ref vel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            Place();
            FadeOccluders();
        }

        void Place()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus + Vector3.up * 0.4f - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }

        void FadeOccluders()
        {
            Vector3 tp = target.position + Vector3.up * 0.4f;
            Vector3 from = transform.position;
            Vector3 dir = tp - from;
            float len = dir.magnitude;
            int n = Physics.SphereCastNonAlloc(from, fadeRadius, dir / len, hits, len - 0.2f, ~(1 << Layers.Player), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var wf = hits[i].collider.GetComponentInParent<WallFade>();
                if (wf != null) wf.RequestFade();
            }
            // Además, paredes justo delante (al sur) del mapache aunque no lo tapen del todo.
            n = Physics.SphereCastNonAlloc(tp + Vector3.up * 0.6f, 0.6f, Vector3.back, hits, 1.6f, Layers.WallMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var wf = hits[i].collider.GetComponentInParent<WallFade>();
                if (wf != null) wf.RequestFade();
            }
        }
    }
}
