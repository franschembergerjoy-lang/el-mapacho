using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Salida (gatera o puerta). Al llegar se guarda el botín; con el objetivo cumplido se puede escapar.</summary>
    public class ExitZone : MonoBehaviour
    {
        public float radius = 1.3f;
        public string exitName = "la gatera";
        public Renderer marker;
        public Light exitLight;

        MaterialPropertyBlock mpb;

        public bool PlayerInside { get; private set; }

        void Update()
        {
            var lm = LevelManager.I;
            if (lm == null || lm.player == null) return;
            var p = lm.player;
            bool inside = !p.IsHidden && Perception.FlatDistance(p.transform.position, transform.position) <= radius;
            if (inside && lm.State == LevelState.Playing)
            {
                if (!PlayerInside || p.CarriedValue > 0) lm.OnPlayerAtExit();
            }
            PlayerInside = inside;
            p.AtExit = inside ? this : (p.AtExit == this ? null : p.AtExit);

            if (marker != null)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                Color c = lm.GoalMet ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.85f, 0.25f);
                c.a = 0.35f + 0.2f * Mathf.Sin(Time.time * 3f);
                mpb.SetColor("_BaseColor", c);
                marker.SetPropertyBlock(mpb);
                float s = 1f + 0.05f * Mathf.Sin(Time.time * 3f);
                marker.transform.localScale = new Vector3(radius * 2f * s, marker.transform.localScale.y, radius * 2f * s);
            }
            if (exitLight != null) exitLight.color = lm.GoalMet ? new Color(0.3f, 1f, 0.45f) : new Color(1f, 0.8f, 0.3f);
        }
    }
}
