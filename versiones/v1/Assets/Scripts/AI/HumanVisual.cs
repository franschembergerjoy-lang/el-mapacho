using UnityEngine;

namespace RobaYCorre
{
    public enum HumanPose { Sleep, SitUp, Idle, Walk, Investigate, Search, Chase, Cheer }

    /// <summary>Poses y animación procedural de los habitantes.</summary>
    public class HumanVisual : MonoBehaviour
    {
        public Transform model;
        public Transform body;
        public Transform head;
        public Transform armL, armR, legL, legR;
        public GameObject nightcap;
        public Light flashlight;
        [Tooltip("Posición local del modelo cuando duerme (acostado en la cama)")]
        public Vector3 sleepOffset = new Vector3(0f, 0.6f, 0.85f);

        [HideInInspector] public HumanPose pose = HumanPose.Idle;
        [HideInInspector] public float speed;

        float phase;
        Vector3 bodyBase;
        bool init;

        void Init()
        {
            if (init) return;
            init = true;
            if (body != null) bodyBase = body.localPosition;
        }

        void LateUpdate()
        {
            Init();
            float dt = Time.deltaTime;
            float t = Time.time;
            phase += dt * Mathf.Lerp(4f, 11f, Mathf.Clamp01(speed / 4f)) * (speed > 0.05f ? 1f : 0f);

            Quaternion modelRot = Quaternion.identity;
            Vector3 modelPos = Vector3.zero;
            float armLx = 0f, armRx = 0f, armLz = 0f, armRz = 0f, legLx = 0f, legRx = 0f;
            float headYaw = 0f, headPitch = 0f;
            float swing = Mathf.Sin(phase);

            switch (pose)
            {
                case HumanPose.Sleep:
                    modelRot = Quaternion.Euler(-90f, 0f, 0f);
                    modelPos = sleepOffset;
                    armLz = 10f; armRz = -10f;
                    break;
                case HumanPose.SitUp:
                    modelRot = Quaternion.Euler(-30f, 0f, 0f);
                    modelPos = sleepOffset + new Vector3(0f, 0.05f, -0.3f);
                    armLx = -40f; armRx = -40f;
                    headYaw = Mathf.Sin(t * 4f) * 30f;
                    break;
                case HumanPose.Idle:
                    armLz = 6f; armRz = -6f;
                    headYaw = Mathf.Sin(t * 0.8f) * 20f;
                    break;
                case HumanPose.Walk:
                    legLx = swing * 28f; legRx = -swing * 28f;
                    armLx = -swing * 22f; armRx = swing * 22f;
                    break;
                case HumanPose.Investigate:
                    legLx = swing * 26f; legRx = -swing * 26f;
                    armLx = -swing * 15f;
                    armRx = -80f;
                    headYaw = Mathf.Sin(t * 2.2f) * 25f;
                    headPitch = 8f;
                    break;
                case HumanPose.Search:
                    legLx = swing * 30f; legRx = -swing * 30f;
                    armRx = -75f;
                    armLz = 25f;
                    headYaw = Mathf.Sin(t * 3f) * 45f;
                    break;
                case HumanPose.Chase:
                    legLx = swing * 45f; legRx = -swing * 45f;
                    armLx = -110f + swing * 20f; armRx = -110f - swing * 20f;
                    modelRot = Quaternion.Euler(10f, 0f, 0f);
                    break;
                case HumanPose.Cheer:
                    armLx = -170f + Mathf.Sin(t * 12f) * 15f; armRx = -170f - Mathf.Sin(t * 12f) * 15f;
                    modelPos = Vector3.up * Mathf.Abs(Mathf.Sin(t * 8f)) * 0.15f;
                    break;
            }

            if (model != null)
            {
                model.localRotation = Quaternion.Slerp(model.localRotation, modelRot, dt * 8f);
                model.localPosition = Vector3.Lerp(model.localPosition, modelPos, dt * 8f);
            }
            if (body != null)
            {
                float breathe = pose == HumanPose.Sleep ? Mathf.Sin(t * 1.6f) * 0.03f : Mathf.Abs(swing) * 0.02f;
                body.localPosition = bodyBase + Vector3.up * breathe;
            }
            SetRot(armL, armLx, armLz);
            SetRot(armR, armRx, armRz);
            SetRot(legL, legLx, 0f);
            SetRot(legR, legRx, 0f);
            if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(headPitch, headYaw, 0f), dt * 6f);
            if (nightcap != null) nightcap.SetActive(pose == HumanPose.Sleep || pose == HumanPose.SitUp || nightcapAlways);
            if (flashlight != null) flashlight.enabled = pose == HumanPose.Investigate || pose == HumanPose.Search || pose == HumanPose.Chase || pose == HumanPose.Walk || pose == HumanPose.Idle;
        }

        [Tooltip("Si es true el gorro de dormir se queda puesto despierto")]
        public bool nightcapAlways = true;

        void SetRot(Transform tr, float x, float z)
        {
            if (tr == null) return;
            tr.localRotation = Quaternion.Slerp(tr.localRotation, Quaternion.Euler(x, 0f, z), Time.deltaTime * 12f);
        }
    }
}
