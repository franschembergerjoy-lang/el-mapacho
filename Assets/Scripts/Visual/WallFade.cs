using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Hace transparente una pared o mueble alto mientras tapa al mapache.</summary>
    public class WallFade : MonoBehaviour
    {
        public Renderer[] renderers;
        public Material[] normalMaterials;
        public Material[] fadeMaterials;
        [Range(0f, 1f)] public float fadedAlpha = 0.2f;

        float alpha = 1f;
        float lastRequest = -10f;
        bool usingFade;
        MaterialPropertyBlock mpb;

        void Awake()
        {
            enabled = false;
        }

        public void RequestFade()
        {
            lastRequest = Time.unscaledTime;
            enabled = true;
        }

        void Update()
        {
            if (renderers == null || fadeMaterials == null) { enabled = false; return; }
            bool want = Time.unscaledTime - lastRequest < 0.12f;
            alpha = Mathf.MoveTowards(alpha, want ? fadedAlpha : 1f, Time.unscaledDeltaTime * 4f);

            if (!want && alpha >= 0.999f)
            {
                if (usingFade)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] == null) continue;
                        renderers[i].sharedMaterial = normalMaterials[i];
                        renderers[i].SetPropertyBlock(null);
                    }
                    usingFade = false;
                }
                enabled = false;
                return;
            }

            if (mpb == null) mpb = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || i >= fadeMaterials.Length || fadeMaterials[i] == null) continue;
                if (!usingFade) renderers[i].sharedMaterial = fadeMaterials[i];
                Color c = fadeMaterials[i].GetColor("_BaseColor");
                c.a = alpha;
                mpb.SetColor("_BaseColor", c);
                renderers[i].SetPropertyBlock(mpb);
            }
            usingFade = true;
        }
    }
}
