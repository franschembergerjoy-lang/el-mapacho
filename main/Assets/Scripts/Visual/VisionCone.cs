using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Abanico translúcido en el suelo que muestra lo que ve un vigilante. Se recorta contra paredes y muebles.</summary>
    public class VisionCone : MonoBehaviour
    {
        public int segments = 22;
        public float floorHeight = 0.03f;
        public float rayHeight = 0.5f;

        Mesh mesh;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Vector3[] verts;
        int[] tris;
        static Material sharedMat;

        public static Material SharedMaterial
        {
            get
            {
                if (sharedMat == null) sharedMat = Resources.Load<Material>("Materials/VisionCone");
                return sharedMat;
            }
        }

        public static VisionCone Create(string ownerName)
        {
            var go = new GameObject(ownerName + "_Vision");
            go.layer = Layers.IgnoreRaycast;
            var vc = go.AddComponent<VisionCone>();
            vc.Init();
            return vc;
        }

        void Init()
        {
            mesh = new Mesh { name = "VisionConeMesh" };
            mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SharedMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mpb = new MaterialPropertyBlock();
            verts = new Vector3[segments + 2];
            tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
        }

        public void Hide()
        {
            if (mr != null) mr.enabled = false;
        }

        public void Show(Vector3 origin, Vector3 forward, float range, float angle, Color color)
        {
            if (mesh == null) Init();
            mr.enabled = true;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 o = new Vector3(origin.x, floorHeight, origin.z);
            verts[0] = o;
            Vector3 rayOrigin = new Vector3(origin.x, rayHeight, origin.z);
            for (int i = 0; i <= segments; i++)
            {
                float a = -angle * 0.5f + angle * i / segments;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * forward;
                float dist = range;
                if (Physics.Raycast(rayOrigin, dir, out var hit, range, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
                    dist = hit.distance;
                verts[i + 1] = o + dir * dist;
            }
            mesh.Clear();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mpb.SetColor("_BaseColor", color);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>Anillo que se expande en el suelo cuando se hace ruido.</summary>
    public class NoiseRing : MonoBehaviour
    {
        float life, maxLife = 0.8f, radius;
        Color color;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        static Mesh ringMesh;

        public static void Spawn(Vector3 position, float radius, Color color)
        {
            if (VisionCone.SharedMaterial == null) return;
            var go = new GameObject("NoiseRing");
            go.layer = Layers.IgnoreRaycast;
            go.transform.position = new Vector3(position.x, 0.05f, position.z);
            var r = go.AddComponent<NoiseRing>();
            r.radius = radius;
            r.color = color;
            go.AddComponent<MeshFilter>().sharedMesh = RingMesh();
            r.mr = go.AddComponent<MeshRenderer>();
            r.mr.sharedMaterial = VisionCone.SharedMaterial;
            r.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.mpb = new MaterialPropertyBlock();
            r.Update();
        }

        static Mesh RingMesh()
        {
            if (ringMesh != null) return ringMesh;
            const int seg = 40;
            var v = new Vector3[seg * 2];
            var t = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * 0.88f;
                v[i * 2 + 1] = d;
                int n = (i + 1) % seg;
                t[i * 6] = i * 2; t[i * 6 + 1] = n * 2; t[i * 6 + 2] = i * 2 + 1;
                t[i * 6 + 3] = i * 2 + 1; t[i * 6 + 4] = n * 2; t[i * 6 + 5] = n * 2 + 1;
            }
            ringMesh = new Mesh { name = "NoiseRing", vertices = v, triangles = t };
            ringMesh.RecalculateBounds();
            return ringMesh;
        }

        void Update()
        {
            life += Time.deltaTime;
            float k = life / maxLife;
            if (k >= 1f) { Destroy(gameObject); return; }
            float s = Mathf.Lerp(0.2f, radius, 1f - (1f - k) * (1f - k));
            transform.localScale = new Vector3(s, 1f, s);
            var c = color; c.a = 0.7f * (1f - k);
            mpb.SetColor("_BaseColor", c);
            mr.SetPropertyBlock(mpb);
        }
    }
}
