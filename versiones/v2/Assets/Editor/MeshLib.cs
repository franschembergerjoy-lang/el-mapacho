using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobaYCorre.EditorTools
{
    /// <summary>Mallas low poly con sombreado plano, guardadas en Assets/Models/Meshes.</summary>
    public static class MeshLib
    {
        public const string Folder = "Assets/Models/Meshes";
        public static Mesh Cube, Sphere, Cylinder, Cone, Wedge, Shade, Pot, Octa;

        public static void Build()
        {
            ProjectBuilder.EnsureFolder(Folder);
            Cube = Save("LP_Cubo", MakeCube());
            Sphere = Save("LP_Esfera", MakeIcoSphere(1));
            Cylinder = Save("LP_Cilindro", MakeFrustum(10, 0.5f, 0.5f));
            Cone = Save("LP_Cono", MakeFrustum(8, 0.5f, 0f));
            Shade = Save("LP_Pantalla", MakeFrustum(8, 0.5f, 0.32f));
            Pot = Save("LP_Maceta", MakeFrustum(8, 0.36f, 0.5f));
            Wedge = Save("LP_Cuna", MakeWedge());
            Octa = Save("LP_Octaedro", MakeOcta());
        }


        /// <summary>Caja centrada con UV en metros (las texturas se repiten sin estirarse). No se guarda como asset.</summary>
        public static Mesh BoxUV(Vector3 size, float uvScale)
        {
            Vector3 h = size * 0.5f;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int k = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(ua * uvScale); uv.Add(ub * uvScale); uv.Add(uc * uvScale); uv.Add(ud * uvScale);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f) t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                else t.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            }
            float X = h.x, Y = h.y, Z = h.z;
            Quad(new Vector3(-X, -Y, Z), new Vector3(-X, Y, Z), new Vector3(X, Y, Z), new Vector3(X, -Y, Z), Vector3.forward, new Vector2(-X, -Y), new Vector2(-X, Y), new Vector2(X, Y), new Vector2(X, -Y));
            Quad(new Vector3(-X, -Y, -Z), new Vector3(-X, Y, -Z), new Vector3(X, Y, -Z), new Vector3(X, -Y, -Z), Vector3.back, new Vector2(-X, -Y), new Vector2(-X, Y), new Vector2(X, Y), new Vector2(X, -Y));
            Quad(new Vector3(X, -Y, -Z), new Vector3(X, Y, -Z), new Vector3(X, Y, Z), new Vector3(X, -Y, Z), Vector3.right, new Vector2(-Z, -Y), new Vector2(-Z, Y), new Vector2(Z, Y), new Vector2(Z, -Y));
            Quad(new Vector3(-X, -Y, -Z), new Vector3(-X, Y, -Z), new Vector3(-X, Y, Z), new Vector3(-X, -Y, Z), Vector3.left, new Vector2(-Z, -Y), new Vector2(-Z, Y), new Vector2(Z, Y), new Vector2(Z, -Y));
            Quad(new Vector3(-X, Y, -Z), new Vector3(-X, Y, Z), new Vector3(X, Y, Z), new Vector3(X, Y, -Z), Vector3.up, new Vector2(-X, -Z), new Vector2(-X, Z), new Vector2(X, Z), new Vector2(X, -Z));
            Quad(new Vector3(-X, -Y, -Z), new Vector3(-X, -Y, Z), new Vector3(X, -Y, Z), new Vector3(X, -Y, -Z), Vector3.down, new Vector2(-X, -Z), new Vector2(-X, Z), new Vector2(X, Z), new Vector2(X, -Z));
            var m = new Mesh { name = "CajaUV" };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh Save(string name, Mesh m)
        {
            string path = $"{Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            m.name = name;
            if (existing != null)
            {
                existing.Clear();
                existing.vertices = m.vertices;
                existing.triangles = m.triangles;
                existing.normals = m.normals;
                existing.uv = m.uv;
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Convierte triángulos en malla sin vértices compartidos y orienta las caras hacia fuera.</summary>
        static Mesh Flat(List<Vector3> pts, List<int> tris)
        {
            Vector3 center = Vector3.zero;
            foreach (var p in pts) center += p;
            center /= Mathf.Max(1, pts.Count);
            var v = new List<Vector3>();
            var t = new List<int>();
            var uv = new List<Vector2>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = pts[tris[i]], b = pts[tris[i + 1]], c = pts[tris[i + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3f;
                if (Vector3.Dot(n, centroid - center) < 0f) { var tmp = b; b = c; c = tmp; }
                int k = v.Count;
                v.Add(a); v.Add(b); v.Add(c);
                uv.Add(new Vector2(a.x + a.z, a.y)); uv.Add(new Vector2(b.x + b.z, b.y)); uv.Add(new Vector2(c.x + c.z, c.y));
                t.Add(k); t.Add(k + 1); t.Add(k + 2);
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.SetUVs(0, uv);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh MakeCube()
        {
            var p = new List<Vector3>();
            for (int i = 0; i < 8; i++)
                p.Add(new Vector3((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f));
            int[][] faces =
            {
                new[] {0, 1, 3, 2}, new[] {4, 5, 7, 6}, // -z, +z
                new[] {0, 1, 5, 4}, new[] {2, 3, 7, 6}, // -y, +y
                new[] {0, 2, 6, 4}, new[] {1, 3, 7, 5}, // -x, +x
            };
            var t = new List<int>();
            foreach (var f in faces) { t.Add(f[0]); t.Add(f[1]); t.Add(f[2]); t.Add(f[0]); t.Add(f[2]); t.Add(f[3]); }
            return Flat(p, t);
        }

        static Mesh MakeOcta()
        {
            var p = new List<Vector3> { Vector3.right * 0.5f, Vector3.left * 0.5f, Vector3.up * 0.5f, Vector3.down * 0.5f, Vector3.forward * 0.5f, Vector3.back * 0.5f };
            var t = new List<int> { 0, 2, 4, 4, 2, 1, 1, 2, 5, 5, 2, 0, 0, 3, 4, 4, 3, 1, 1, 3, 5, 5, 3, 0 };
            return Flat(p, t);
        }

        static Mesh MakeIcoSphere(int subdiv)
        {
            float g = (1f + Mathf.Sqrt(5f)) / 2f;
            var p = new List<Vector3>
            {
                new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0),
                new Vector3(0, -1, g), new Vector3(0, 1, g), new Vector3(0, -1, -g), new Vector3(0, 1, -g),
                new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1),
            };
            var t = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            for (int s = 0; s < subdiv; s++)
            {
                var cache = new Dictionary<long, int>();
                var nt = new List<int>();
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (cache.TryGetValue(key, out int idx)) return idx;
                    p.Add((p[a] + p[b]) * 0.5f);
                    cache[key] = p.Count - 1;
                    return p.Count - 1;
                }
                for (int i = 0; i < t.Count; i += 3)
                {
                    int a = t[i], b = t[i + 1], c = t[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                t = nt;
            }
            for (int i = 0; i < p.Count; i++) p[i] = p[i].normalized * 0.5f;
            return Flat(p, t);
        }

        static Mesh MakeFrustum(int sides, float rBottom, float rTop)
        {
            var p = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                p.Add(new Vector3(Mathf.Cos(a) * rBottom, -0.5f, Mathf.Sin(a) * rBottom));
            }
            bool apex = rTop <= 0.0001f;
            if (apex) p.Add(new Vector3(0f, 0.5f, 0f));
            else
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    p.Add(new Vector3(Mathf.Cos(a) * rTop, 0.5f, Mathf.Sin(a) * rTop));
                }
            p.Add(new Vector3(0f, -0.5f, 0f));
            int bottomCenter = p.Count - 1;
            int topCenter = -1;
            if (!apex) { p.Add(new Vector3(0f, 0.5f, 0f)); topCenter = p.Count - 1; }

            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                if (apex)
                {
                    t.Add(i); t.Add(n); t.Add(sides);
                }
                else
                {
                    t.Add(i); t.Add(n); t.Add(sides + n);
                    t.Add(i); t.Add(sides + n); t.Add(sides + i);
                    t.Add(topCenter); t.Add(sides + i); t.Add(sides + n);
                }
                t.Add(bottomCenter); t.Add(n); t.Add(i);
            }
            return Flat(p, t);
        }

        static Mesh MakeWedge()
        {
            // Prisma triangular (cuña de queso), base de 1x1, altura 1.
            var b = new[] { new Vector3(-0.5f, 0, -0.45f), new Vector3(0.5f, 0, -0.45f), new Vector3(0f, 0, 0.55f) };
            var p = new List<Vector3>();
            foreach (var v in b) p.Add(v + Vector3.down * 0.5f);
            foreach (var v in b) p.Add(v + Vector3.up * 0.5f);
            var t = new List<int> { 0, 1, 2, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
            return Flat(p, t);
        }
    }
}
