using UnityEngine;
using UnityEngine.EventSystems;

namespace RobaYCorre
{
    /// <summary>Curvas de animación con rebote (tiempo sin escalar, funcionan en pausa).</summary>
    public static class Ease
    {
        public static float OutBack(float t, float s = 2.2f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f || t >= 1f) return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }

    /// <summary>Aparece "saltando" desde tamaño cero (con retraso opcional para entradas escalonadas).</summary>
    public class UIPop : MonoBehaviour
    {
        public float delay;
        public float duration = 0.45f;
        public bool elastic;
        float t;
        Vector3 target = Vector3.one;

        void OnEnable()
        {
            t = -delay;
            transform.localScale = Vector3.zero;
        }

        public void Restart(float newDelay = 0f)
        {
            delay = newDelay;
            OnEnable();
            enabled = true;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t < 0f) { transform.localScale = Vector3.zero; return; }
            float k = t / duration;
            float s = elastic ? Ease.OutElastic(k) : Ease.OutBack(k);
            transform.localScale = target * s;
            if (k >= 1f) { transform.localScale = target; enabled = false; }
        }
    }

    /// <summary>Botón con gracia: crece y se balancea al pasar el ratón, se aplasta al pulsar.</summary>
    public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        float hover, press, wobble;
        Vector3 baseScale = Vector3.one;

        void Start() { baseScale = Vector3.one; }

        public void OnPointerEnter(PointerEventData e) { hover = 1f; wobble = 1f; }
        public void OnPointerExit(PointerEventData e) { hover = 0f; }
        public void OnPointerDown(PointerEventData e) { press = 1f; }
        public void OnPointerUp(PointerEventData e) { press = 0f; wobble = 0.6f; }

        float hoverAnim, pressAnim;

        void Update()
        {
            var pop = GetComponent<UIPop>();
            if (pop != null && pop.enabled) return;
            float dt = Time.unscaledDeltaTime;
            hoverAnim = Mathf.MoveTowards(hoverAnim, hover, dt * 6f);
            pressAnim = Mathf.MoveTowards(pressAnim, press, dt * 12f);
            wobble = Mathf.MoveTowards(wobble, 0f, dt * 2f);
            float s = 1f + 0.07f * hoverAnim - 0.08f * pressAnim;
            float sq = Mathf.Sin(Time.unscaledTime * 22f) * 0.04f * wobble;
            transform.localScale = new Vector3(baseScale.x * (s + sq), baseScale.y * (s - sq), 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 16f) * 3f * wobble);
        }
    }

    /// <summary>Balanceo continuo (títulos).</summary>
    public class UIWobble : MonoBehaviour
    {
        public float angle = 3f;
        public float speed = 2f;
        public float scale = 0.04f;
        public float phase;

        void Update()
        {
            var pop = GetComponent<UIPop>();
            if (pop != null && pop.enabled) return;
            float t = Time.unscaledTime * speed + phase;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * angle);
            float s = 1f + Mathf.Sin(t * 2f) * scale;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Gira sin parar (rayos de fondo).</summary>
    public class UISpin : MonoBehaviour
    {
        public float speed = 15f;
        void Update() { transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime); }
    }

    /// <summary>Flota arriba y abajo.</summary>
    public class UIFloat : MonoBehaviour
    {
        public float amount = 8f;
        public float speed = 2f;
        public float phase;
        RectTransform rt;
        Vector2 basePos;

        void Start()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
        }

        void Update()
        {
            rt.anchoredPosition = basePos + Vector2.up * Mathf.Sin(Time.unscaledTime * speed + phase) * amount;
        }
    }

    /// <summary>Tiembla mientras 'intensity' > 0.</summary>
    public class UIShake : MonoBehaviour
    {
        public float intensity;
        RectTransform rt;
        Vector2 basePos;
        bool init;

        void Update()
        {
            if (!init) { rt = (RectTransform)transform; basePos = rt.anchoredPosition; init = true; }
            if (intensity <= 0.001f) { rt.anchoredPosition = basePos; return; }
            rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(Time.unscaledTime * 53f), Mathf.Cos(Time.unscaledTime * 47f)) * intensity;
        }
    }
}
