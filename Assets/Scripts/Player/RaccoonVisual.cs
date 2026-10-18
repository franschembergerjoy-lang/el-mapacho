using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Animación procedural del mapache: caminar, correr, agacharse, recoger, chocar, esconderse y ser atrapado.</summary>
    public class RaccoonVisual : MonoBehaviour
    {
        public Transform model;
        public Transform body;
        public Transform head;
        public Transform[] legs;          // delantera izq, delantera der, trasera izq, trasera der
        public Transform[] tailSegments;
        public Transform earL, earR;
        public Transform backpack;
        public GameObject dizzyStars;

        float speed01;
        bool crouch, run, hidden, caught, holdingBig;
        float phase, crouchBlend, pickupT, bumpT, caughtT;
        Vector3 bodyBase, headBase, modelBase, backpackBase = Vector3.one;
        Quaternion headRot;
        bool initialized;

        void Init()
        {
            if (initialized) return;
            initialized = true;
            if (body != null) bodyBase = body.localPosition;
            if (head != null) { headBase = head.localPosition; headRot = head.localRotation; }
            if (model != null) modelBase = model.localPosition;
            if (backpack != null) backpackBase = backpack.localScale;
            if (dizzyStars != null) dizzyStars.SetActive(false);
        }

        void Awake() { Init(); SetBackpackLoad(0f, false); }

        public void SetMotion(float s01, bool isCrouching, bool isRunning)
        {
            speed01 = s01;
            crouch = isCrouching;
            run = isRunning;
        }

        public void PlayPickup() { pickupT = 0.35f; }
        public void PlayBump() { bumpT = 0.4f; }

        public void SetHidden(bool h)
        {
            hidden = h;
            if (model != null) model.gameObject.SetActive(!h);
        }

        public void PlayCaught()
        {
            caught = true;
            caughtT = 0f;
            if (model != null) model.gameObject.SetActive(true);
            if (dizzyStars != null) dizzyStars.SetActive(true);
        }

        public void SetBackpackLoad(float load01, bool bigItem)
        {
            Init();
            holdingBig = bigItem;
            if (backpack == null) return;
            backpack.gameObject.SetActive(load01 > 0.001f);
            backpack.localScale = backpackBase * (0.75f + load01 * 0.9f);
        }

        void LateUpdate()
        {
            Init();
            if (hidden && !caught) return;
            float dt = Time.deltaTime;

            if (caught)
            {
                caughtT += dt;
                if (model != null)
                {
                    model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(0f, caughtT * 90f, 180f), dt * 6f);
                    model.localPosition = Vector3.Lerp(model.localPosition, modelBase + Vector3.up * 0.55f, dt * 6f);
                }
                if (legs != null)
                    for (int i = 0; i < legs.Length; i++)
                        if (legs[i] != null) legs[i].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 25f + i) * 40f, 0f, 0f);
                if (dizzyStars != null) dizzyStars.transform.Rotate(0f, 240f * dt, 0f, Space.Self);
                return;
            }

            float moving = speed01 > 0.02f ? 1f : 0f;
            phase += dt * Mathf.Lerp(7f, 17f, speed01) * moving;
            crouchBlend = Mathf.MoveTowards(crouchBlend, crouch ? 1f : 0f, dt * 8f);

            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.05f * Mathf.Clamp01(speed01 * 2f);
            if (body != null)
            {
                body.localPosition = bodyBase + Vector3.up * (bob - 0.1f * crouchBlend);
                body.localScale = new Vector3(1f + 0.08f * crouchBlend, 1f - 0.2f * crouchBlend, 1f + 0.05f * crouchBlend);
            }

            if (legs != null)
            {
                float amp = 42f * Mathf.Clamp01(speed01 * 2.5f);
                for (int i = 0; i < legs.Length; i++)
                {
                    if (legs[i] == null) continue;
                    float off = (i == 0 || i == 3) ? 0f : Mathf.PI;
                    legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + off) * amp, 0f, 0f);
                }
            }

            if (tailSegments != null)
            {
                for (int i = 0; i < tailSegments.Length; i++)
                {
                    if (tailSegments[i] == null) continue;
                    float sway = Mathf.Sin(Time.time * (run ? 9f : 3f) + i * 0.7f) * (10f + 14f * speed01);
                    float lift = run ? -6f : (crouch ? 8f : 2f);
                    tailSegments[i].localRotation = Quaternion.Euler(lift, sway, 0f);
                }
            }

            if (head != null)
            {
                float dip = 0f;
                if (pickupT > 0f)
                {
                    pickupT -= dt;
                    dip = Mathf.Sin((1f - pickupT / 0.35f) * Mathf.PI) * 40f;
                }
                float look = holdingBig ? -12f : 0f;
                head.localRotation = headRot * Quaternion.Euler(dip + look + crouchBlend * 8f, Mathf.Sin(Time.time * 1.3f) * 6f * (1f - speed01), 0f);
                head.localPosition = headBase + Vector3.down * 0.06f * crouchBlend + Vector3.up * bob * 0.5f;
            }

            float earFlat = crouchBlend * 35f;
            if (earL != null) earL.localRotation = Quaternion.Euler(-earFlat, 0f, 15f + earFlat * 0.5f);
            if (earR != null) earR.localRotation = Quaternion.Euler(-earFlat, 0f, -15f - earFlat * 0.5f);

            if (model != null)
            {
                float wob = 0f;
                if (bumpT > 0f)
                {
                    bumpT -= dt;
                    wob = Mathf.Sin(bumpT * 40f) * bumpT * 40f;
                }
                model.localRotation = Quaternion.Euler(-wob, 0f, Mathf.Sin(phase * 0.5f) * 3f * speed01);
                model.localPosition = modelBase;
            }
        }
    }
}
