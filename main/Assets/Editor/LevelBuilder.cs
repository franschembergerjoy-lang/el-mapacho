using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RobaYCorre.EditorTools
{
    /// <summary>Datos de un nivel: mapa ASCII de la casa + función que coloca muebles, botín y vigilantes.</summary>
    public class LevelDef
    {
        public int number;
        public string sceneName;
        public string title;
        public string intro;
        public int goal;
        public bool special;
        public bool tutorial;
        public string wallMat = "ParedCrema";
        public string capMat = "RemateMarron";
        public bool mansionExit;
        public Vector2 start;
        /// <summary>
        /// '#' pared, ' ' exterior, 'X' salida, cualquier letra = suelo (el material sale de 'floors').
        /// La primera fila es el norte (lejos de la cámara). x = columna, z = (filas-1-fila).
        /// </summary>
        public string[] map;
        public Dictionary<char, Material> floors = new Dictionary<char, Material>();
        public Action<LevelCtx> populate;
    }

    public class LevelCtx
    {
        public LevelDef def;
        public int W, H;
        public Transform root, furniture, loot, actors, routes, lights, security;

        public GameObject Place(string prefab, float x, float z, float rotY = 0f, float y = 0f, Transform parent = null)
        {
            var p = ModelLibrary.Get(prefab);
            if (p == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
            go.transform.SetParent(parent != null ? parent : furniture, false);
            go.transform.position = new Vector3(x, y, z);
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            return go;
        }

        public Lootable Loot(string prefab, float x, float z, float y = 0f, bool special = false, bool alarm = false, float rotY = 0f)
        {
            var go = Place(prefab, x, z, rotY == 0f ? (x * 37f + z * 11f) % 50f - 25f : rotY, y, loot);
            if (go == null) return null;
            var l = go.GetComponent<Lootable>();
            if (special) l.isSpecial = true;
            if (alarm)
            {
                l.triggersAlarm = true;
                l.funnyLine = "¡Uy! El joyero tenía un sensor...";
            }
            return l;
        }

        public Transform Point(string name, float x, float z)
        {
            var t = new GameObject(name).transform;
            t.SetParent(routes, false);
            t.position = new Vector3(x, 0f, z);
            return t;
        }

        public Resident Resident(string prefab, float x, float z, float rotY, bool asleep, Vector2 wake, float wakeThreshold, params Vector2[] patrol)
        {
            var go = Place(prefab, x, z, rotY, 0f, actors);
            var r = go.GetComponent<Resident>();
            r.startsAsleep = asleep;
            r.wakeThreshold = wakeThreshold;
            r.wakeSpot = Point(go.name + "_Despertar", wake.x, wake.y);
            var pts = new Transform[patrol.Length];
            for (int i = 0; i < patrol.Length; i++) pts[i] = Point($"{go.name}_Ruta{i}", patrol[i].x, patrol[i].y);
            r.patrolPoints = pts;
            return r;
        }

        public Dog Dog(float x, float z, params Vector2[] patrol)
        {
            var go = Place("Perro", x, z, 0f, 0f, actors);
            var d = go.GetComponent<Dog>();
            var pts = new Transform[patrol.Length];
            for (int i = 0; i < patrol.Length; i++) pts[i] = Point($"Perro_Ruta{i}", patrol[i].x, patrol[i].y);
            d.patrolPoints = pts;
            return d;
        }

        public SecurityCamera Cam(float x, float z, float rotY, float sweep = 90f, float range = 7.5f, float speed = 0.5f)
        {
            var go = Place("CamaraSeguridad", x, z, rotY, 1.45f, security);
            var c = go.GetComponent<SecurityCamera>();
            c.sweepAngle = sweep;
            c.viewDistance = range;
            c.sweepSpeed = speed;
            return c;
        }

        public MotionSensor Sensor(float x, float z, float sx, float sz, float on = 3.5f, float off = 2f, float offset = 0f, float rotY = 0f)
        {
            var go = Place("SensorMovimiento", x, z, rotY, 0f, security);
            var s = go.GetComponent<MotionSensor>();
            s.zoneSize = new Vector2(sx, sz);
            s.onTime = on; s.offTime = off; s.timeOffset = offset;
            var t = go.transform;
            t.Find("Zona").localScale = new Vector3(sx, 0.01f, sz);
            t.Find("PosteIzq").localPosition = new Vector3(-sx / 2f, 0f, 0f);
            t.Find("PosteDer").localPosition = new Vector3(sx / 2f, 0f, 0f);
            t.Find("Laser").localScale = new Vector3(sx, 0.015f, 0.015f);
            return s;
        }

        public Light PointLight(float x, float y, float z, Color c, float intensity, float range)
        {
            var go = new GameObject("LuzAmbiente");
            go.transform.SetParent(lights, false);
            go.transform.position = new Vector3(x, y, z);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>Coloca un objeto frágil encima de un mueble; chocar el mueble corriendo lo tira.</summary>
        public void FragileOn(GameObject host, string prefab, float x, float z, float y, string hostName)
        {
            var go = Place(prefab, x, z, 0f, y);
            var f = go.GetComponent<Fragile>();
            var b = host.GetComponent<Bumpable>();
            if (b == null)
            {
                b = host.AddComponent<Bumpable>();
                b.tipsOver = false;
                b.noise = 10f;
                b.displayName = hostName;
                b.fragiles = new Fragile[0];
            }
            var list = new List<Fragile>(b.fragiles ?? new Fragile[0]) { f };
            b.fragiles = list.ToArray();
        }
    }

    public static class LevelBuilder
    {
        public const float WallHeight = 1.6f;
        public const float WallThickness = 0.35f;
        public const string SceneFolder = "Assets/Scenes/Niveles";

        public static string Build(LevelDef def)
        {
            ProjectBuilder.EnsureFolder(SceneFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int H = def.map.Length;
            int W = def.map[0].Length;
            for (int r = 0; r < H; r++)
                if (def.map[r].Length != W) throw new Exception($"Nivel {def.number}: la fila {r} mide {def.map[r].Length} y debería medir {W}");

            SetupLighting();

            var ctx = new LevelCtx { def = def, W = W, H = H };
            ctx.root = new GameObject("Casa").transform;
            ctx.furniture = Child(ctx.root, "Muebles");
            ctx.loot = new GameObject("Botin").transform;
            ctx.actors = new GameObject("Habitantes").transform;
            ctx.routes = new GameObject("Rutas").transform;
            ctx.lights = new GameObject("Luces").transform;
            ctx.security = new GameObject("Seguridad").transform;

            BuildFloors(def, ctx, W, H);
            BuildWalls(def, ctx, W, H);
            ExitZone exit = BuildExit(def, ctx, W, H, out Vector3 exitPos);
            BuildOutside(ctx, W, H, exitPos);

            def.populate?.Invoke(ctx);

            // Jugador.
            var player = ctx.Place("Mapache", def.start.x, def.start.y, 0f, 0.02f, null);
            player.transform.SetParent(null);
            player.name = "Mapache";
            var ctrl = player.GetComponent<RaccoonController>();

            // Cámara.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.16f);
            camGo.AddComponent<AudioListener>();
            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var rig = camGo.AddComponent<CameraRig>();
            rig.target = player.transform;
            var rot = Quaternion.Euler(rig.pitch, rig.yaw, 0f);
            camGo.transform.SetPositionAndRotation(player.transform.position + Vector3.up * 0.4f - rot * Vector3.forward * rig.distance, rot);

            // Gestor del nivel.
            var lmGo = new GameObject("GestorNivel");
            lmGo.AddComponent<NoiseSystem>();
            var lm = lmGo.AddComponent<LevelManager>();
            lm.levelNumber = def.number;
            lm.levelTitle = def.title;
            lm.introText = def.intro;
            lm.lootGoal = def.goal;
            lm.requiresSpecial = def.special;
            lm.showTutorial = def.tutorial;
            lm.player = ctrl;
            lm.exit = exit;
            lm.navCenter = new Vector3((W - 1) / 2f, 0f, (H - 1) / 2f);
            lm.navSize = new Vector3(W + 2f, 6f, H + 2f);

            string path = $"{SceneFolder}/{def.sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static Transform Child(Transform p, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(p, false);
            return t;
        }

        public static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.45f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.28f, 0.3f);
            RenderSettings.fog = false;
            var moon = new GameObject("Luna");
            var l = moon.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(0.8f, 0.84f, 1f);
            l.intensity = 1.25f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.7f;
            moon.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        }

        static bool IsFloor(char c) => c != '#' && c != ' ' && c != 'X';

        static char At(LevelDef def, int c, int r)
        {
            if (r < 0 || r >= def.map.Length || c < 0 || c >= def.map[0].Length) return ' ';
            return def.map[r][c];
        }

        static void BuildFloors(LevelDef def, LevelCtx ctx, int W, int H)
        {
            var quads = new Dictionary<char, List<Vector4>>(); // x0,z0,x1,z1
            void Add(char ch, float x0, float z0, float x1, float z1)
            {
                if (!quads.TryGetValue(ch, out var list)) quads[ch] = list = new List<Vector4>();
                list.Add(new Vector4(x0, z0, x1, z1));
            }
            for (int r = 0; r < H; r++)
            {
                for (int c = 0; c < W; c++)
                {
                    char ch = At(def, c, r);
                    float x = c, z = H - 1 - r;
                    if (IsFloor(ch)) { Add(ch, x - 0.5f, z - 0.5f, x + 0.5f, z + 0.5f); continue; }
                    if (ch == ' ') continue;
                    // Pared o salida: cuatro cuartos con el suelo del vecino más cercano.
                    for (int qx = -1; qx <= 1; qx += 2)
                        for (int qz = -1; qz <= 1; qz += 2)
                        {
                            char v = At(def, c, r - qz), h = At(def, c + qx, r), d = At(def, c + qx, r - qz);
                            char pick = IsFloor(v) ? v : (IsFloor(h) ? h : (IsFloor(d) ? d : ' '));
                            if (pick == ' ') continue;
                            float x0 = qx < 0 ? x - 0.5f : x, z0 = qz < 0 ? z - 0.5f : z;
                            Add(pick, x0, z0, x0 + 0.5f, z0 + 0.5f);
                        }
                }
            }
            var floorRoot = Child(ctx.root, "Suelo");
            foreach (var kv in quads)
            {
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                foreach (var q in kv.Value)
                {
                    int k = verts.Count;
                    verts.Add(new Vector3(q.x, 0f, q.y)); verts.Add(new Vector3(q.x, 0f, q.w));
                    verts.Add(new Vector3(q.z, 0f, q.w)); verts.Add(new Vector3(q.z, 0f, q.y));
                    uvs.Add(new Vector2(q.x, q.y) * 0.5f); uvs.Add(new Vector2(q.x, q.w) * 0.5f);
                    uvs.Add(new Vector2(q.z, q.w) * 0.5f); uvs.Add(new Vector2(q.z, q.y) * 0.5f);
                    tris.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
                var mesh = new Mesh { name = "Suelo_" + kv.Key };
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go = new GameObject("Suelo_" + kv.Key);
                go.transform.SetParent(floorRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = def.floors.TryGetValue(kv.Key, out var m) ? m : MatLib.Get("MaderaClara");
            }
            var col = floorRoot.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3((W - 1) / 2f, -0.25f, (H - 1) / 2f);
            col.size = new Vector3(W + 1f, 0.5f, H + 1f);
        }

        static void BuildWalls(LevelDef def, LevelCtx ctx, int W, int H)
        {
            var wallRoot = Child(ctx.root, "Paredes");
            bool[,] used = new bool[W, H];
            bool IsWall(int c, int r) => At(def, c, r) == '#';
            var wallMat = MatLib.Wall(def.wallMat);
            var skirtMat = MatLib.Get("MaderaOscura");
            var capMat = MatLib.Get(def.capMat);
            int n = 0;

            void MakeWall(float x0, float z0, float x1, float z1)
            {
                float t = WallThickness;
                var center = new Vector3((x0 + x1) / 2f, 0f, (z0 + z1) / 2f);
                var size = new Vector3(Mathf.Abs(x1 - x0) + t, WallHeight, Mathf.Abs(z1 - z0) + t);
                var go = new GameObject("Pared_" + (n++));
                go.layer = Layers.Walls;
                go.transform.SetParent(wallRoot, false);
                go.transform.position = center;
                var body = new GameObject("Muro");
                body.layer = Layers.Walls;
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = new Vector3(0f, WallHeight / 2f, 0f);
                body.AddComponent<MeshFilter>().sharedMesh = MeshLib.BoxUV(size, 1f);
                var r1 = body.AddComponent<MeshRenderer>();
                r1.sharedMaterial = wallMat;
                var cap = new GameObject("Remate");
                cap.layer = Layers.Walls;
                cap.transform.SetParent(go.transform, false);
                cap.transform.localPosition = new Vector3(0f, WallHeight + 0.04f, 0f);
                cap.transform.localScale = new Vector3(size.x + 0.04f, 0.08f, size.z + 0.04f);
                cap.AddComponent<MeshFilter>().sharedMesh = MeshLib.Cube;
                var r2 = cap.AddComponent<MeshRenderer>();
                r2.sharedMaterial = capMat;
                var skirt = new GameObject("Zocalo");
                skirt.layer = Layers.Walls;
                skirt.transform.SetParent(go.transform, false);
                skirt.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                skirt.transform.localScale = new Vector3(size.x + 0.04f, 0.16f, size.z + 0.04f);
                skirt.AddComponent<MeshFilter>().sharedMesh = MeshLib.Cube;
                var r3 = skirt.AddComponent<MeshRenderer>();
                r3.sharedMaterial = skirtMat;
                var bc = go.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, WallHeight / 2f, 0f);
                bc.size = size;
                var wf = go.AddComponent<WallFade>();
                wf.renderers = new Renderer[] { r1, r2, r3 };
                wf.normalMaterials = new[] { wallMat, capMat, skirtMat };
                wf.fadeMaterials = new[] { MatLib.Fade(wallMat), MatLib.Fade(capMat), MatLib.Fade(skirtMat) };
                wf.fadedAlpha = 0.18f;
            }

            // Tramos horizontales.
            for (int r = 0; r < H; r++)
            {
                int c = 0;
                while (c < W)
                {
                    if (!IsWall(c, r)) { c++; continue; }
                    int start = c;
                    while (c < W && IsWall(c, r)) c++;
                    int end = c - 1;
                    if (end > start)
                    {
                        float z = H - 1 - r;
                        MakeWall(start, z, end, z);
                        for (int k = start; k <= end; k++) used[k, r] = true;
                    }
                }
            }
            // Tramos verticales.
            for (int c = 0; c < W; c++)
            {
                int r = 0;
                while (r < H)
                {
                    if (!IsWall(c, r)) { r++; continue; }
                    int start = r;
                    while (r < H && IsWall(c, r)) r++;
                    int end = r - 1;
                    if (end > start)
                    {
                        MakeWall(c, H - 1 - start, c, H - 1 - end);
                        for (int k = start; k <= end; k++) used[c, k] = true;
                    }
                }
            }
            // Columnas sueltas.
            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                    if (IsWall(c, r) && !used[c, r]) MakeWall(c, H - 1 - r, c, H - 1 - r);
        }

        static ExitZone BuildExit(LevelDef def, LevelCtx ctx, int W, int H, out Vector3 pos)
        {
            pos = Vector3.zero;
            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                {
                    if (At(def, c, r) != 'X') continue;
                    float rot = 0f;
                    if (IsFloor(At(def, c, r + 1))) rot = 180f;
                    else if (IsFloor(At(def, c, r - 1))) rot = 0f;
                    else if (IsFloor(At(def, c + 1, r))) rot = 90f;
                    else if (IsFloor(At(def, c - 1, r))) rot = -90f;
                    pos = new Vector3(c, 0f, H - 1 - r);
                    var go = ctx.Place(def.mansionExit ? "PuertaMansion" : "Gatera", c, H - 1 - r, rot, 0f, ctx.root);
                    go.name = "Salida";
                    return go.GetComponentInChildren<ExitZone>();
                }
            throw new Exception($"Nivel {def.number}: el mapa no tiene salida 'X'");
        }

        static void BuildOutside(LevelCtx ctx, int W, int H, Vector3 exitPos)
        {
            var outside = new GameObject("Exterior").transform;
            var grass = new GameObject("Cesped");
            grass.transform.SetParent(outside, false);
            grass.transform.position = new Vector3((W - 1) / 2f, -0.06f, (H - 1) / 2f);
            grass.AddComponent<MeshFilter>().sharedMesh = MeshLib.BoxUV(new Vector3(W + 50f, 0.1f, H + 50f), 0.25f);
            grass.AddComponent<MeshRenderer>().sharedMaterial = MatLib.Grass();

            var rng = new System.Random(ctx.def.number * 31);
            for (float x = -1.5f; x < W + 1f; x += 2.6f)
            {
                if (Mathf.Abs(x - exitPos.x) < 2f && exitPos.z < 1f) continue;
                var b = ctx.Place("Arbusto", x + (float)rng.NextDouble() * 0.6f, -1.4f, (float)rng.NextDouble() * 360f, 0f, outside);
                b.transform.localScale = Vector3.one * (0.7f + (float)rng.NextDouble() * 0.4f);
            }
            for (float x = -2f; x < W + 2f; x += 3.5f)
                ctx.Place("Arbol", x + (float)rng.NextDouble(), H + 1.2f + (float)rng.NextDouble() * 1.5f, (float)rng.NextDouble() * 360f, 0f, outside);
            for (float z = 0f; z < H; z += 3f)
            {
                ctx.Place("Arbusto", -1.6f, z, (float)rng.NextDouble() * 360f, 0f, outside);
                ctx.Place("Arbusto", W + 0.6f, z, (float)rng.NextDouble() * 360f, 0f, outside);
            }
            if (exitPos.z < 1f)
            {
                ctx.Place("Buzon", exitPos.x + 2f, -1.4f, 0f, 0f, outside);
                for (int i = 0; i < 4; i++)
                {
                    var s = new GameObject("Baldosa");
                    s.transform.SetParent(outside, false);
                    s.transform.position = new Vector3(exitPos.x + (i % 2 == 0 ? 0.1f : -0.1f), 0.0f, -0.9f - i * 0.8f);
                    s.transform.localScale = new Vector3(0.7f, 0.04f, 0.5f);
                    s.AddComponent<MeshFilter>().sharedMesh = MeshLib.Cube;
                    s.AddComponent<MeshRenderer>().sharedMaterial = MatLib.Get("Piedra");
                }
            }
        }
    }
}
