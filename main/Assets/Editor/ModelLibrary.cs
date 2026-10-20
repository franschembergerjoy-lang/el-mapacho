using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RobaYCorre.EditorTools
{
    /// <summary>
    /// Construye todos los modelos low poly del juego combinando mallas sencillas y los guarda como prefabs
    /// en Assets/Prefabs. Convención: el "frente" de cada mueble mira hacia +Z y el pivote está en el suelo.
    /// </summary>
    public static class ModelLibrary
    {
        public const string Root = "Assets/Prefabs";
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        public static GameObject Get(string name)
        {
            if (prefabs.TryGetValue(name, out var p) && p != null) return p;
            Debug.LogError("Prefab desconocido: " + name);
            return null;
        }

        // ------------------------------------------------------------------ helpers

        static Transform R(string name) => new GameObject(name).transform;

        static Transform Pv(Transform parent, string name, float x, float y, float z, float rx = 0, float ry = 0, float rz = 0)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = new Vector3(x, y, z);
            t.localRotation = Quaternion.Euler(rx, ry, rz);
            return t;
        }

        static GameObject P(Transform parent, Mesh mesh, Material mat, float x, float y, float z, float sx, float sy, float sz, float rx, float ry, float rz)
        {
            var go = new GameObject(mesh != null ? mesh.name.Replace("LP_", "") : "Parte");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(rx, ry, rz);
            go.transform.localScale = new Vector3(sx, sy, sz);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        static GameObject B(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Cube, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);
        static GameObject S(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Sphere, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);
        static GameObject Cy(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Cylinder, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);
        static GameObject Co(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Cone, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);
        static GameObject Sh(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Shade, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);
        static GameObject Pt(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Pot, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);
        static GameObject We(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Wedge, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);
        static GameObject Oc(Transform p, string m, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) => P(p, MeshLib.Octa, MatLib.Get(m), x, y, z, sx, sy, sz, rx, ry, rz);

        static BoxCollider Col(Transform root, float cx, float cy, float cz, float sx, float sy, float sz)
        {
            var c = root.gameObject.AddComponent<BoxCollider>();
            c.center = new Vector3(cx, cy, cz);
            c.size = new Vector3(sx, sy, sz);
            return c;
        }

        static Light AddLight(Transform parent, LightType type, Color color, float intensity, float range, float x, float y, float z, float spotAngle = 60f)
        {
            var t = Pv(parent, "Luz", x, y, z);
            var l = t.gameObject.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            if (type == LightType.Spot) l.spotAngle = spotAngle;
            return l;
        }

        static GameObject Save(Transform root, string folder)
        {
            string dir = $"{Root}/{folder}";
            ProjectBuilder.EnsureFolder(dir);
            string path = $"{dir}/{root.name}.prefab";
            string key = root.name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            Object.DestroyImmediate(root.gameObject);
            prefabs[key] = prefab;
            return prefab;
        }

        static void AddFade(Transform root)
        {
            var wf = root.gameObject.AddComponent<WallFade>();
            var rs = root.GetComponentsInChildren<MeshRenderer>();
            wf.renderers = rs;
            wf.normalMaterials = new Material[rs.Length];
            wf.fadeMaterials = new Material[rs.Length];
            for (int i = 0; i < rs.Length; i++)
            {
                wf.normalMaterials[i] = rs[i].sharedMaterial;
                wf.fadeMaterials[i] = MatLib.Fade(rs[i].sharedMaterial);
            }
        }

        static GameObject Eyes(Transform parent, float x, float y, float z, float ry = 0f)
        {
            var e = Pv(parent, "OjosMapache", x, y, z, 0, ry, 0);
            S(e, "MapacheOscuro", 0, 0, 0, 0.28f, 0.1f, 0.05f);
            foreach (float s in new[] { -1f, 1f })
            {
                S(e, "OjoBlanco", s * 0.065f, 0, 0.02f, 0.075f, 0.075f, 0.04f);
                S(e, "Negro", s * 0.065f, 0, 0.04f, 0.035f, 0.04f, 0.02f);
            }
            return e.gameObject;
        }

        static HidingSpot Hide(Transform root, string spotName, Vector3 hideLocal, Vector3[] exits, GameObject eyes, Transform wobble)
        {
            var h = root.gameObject.AddComponent<HidingSpot>();
            h.spotName = spotName;
            h.hidePoint = Pv(root, "PuntoEscondite", hideLocal.x, hideLocal.y, hideLocal.z);
            h.exitPoints = new Transform[exits.Length];
            for (int i = 0; i < exits.Length; i++) h.exitPoints[i] = Pv(root, "Salida" + i, exits[i].x, 0f, exits[i].z);
            h.peekEyes = eyes;
            h.wobblePart = wobble;
            return h;
        }

        // ------------------------------------------------------------------ build

        public static void BuildAll()
        {
            prefabs.Clear();
            BuildRaccoon();
            BuildHuman("Habitante_Pijama", "PijamaAzul", "PijamaAzulOscuro", "Piel", "PeloCastano", 0, false, false, "PijamaAzul", false, "el habitante");
            BuildHuman("Habitante_Bata", "BataRosa", "BataRosa", "PielMorena", "PeloNegro", 1, false, false, "BataRosa", false, "la vecina");
            BuildHuman("Habitante_Insomne", "PijamaVerde", "PijamaVerde", "Piel", "PeloRubio", 0, false, true, null, false, "el insomne");
            BuildHuman("Coleccionista", "BataMorada", "BataMorada", "Piel", "PeloGris", 2, true, true, "BataMorada", false, "el coleccionista");
            BuildHuman("Mayordomo", "TrajeNegro", "TrajeNegro", "Piel", "PeloGris", 0, true, false, null, true, "el mayordomo");
            BuildHuman("Ama_de_llaves", "UniformeVerde", "TrajeNegro", "PielMorena", "PeloCastano", 1, false, false, null, false, "la ama de llaves");
            BuildDog();

            BuildBed("Cama", "CamaAzul", "MaderaMedia", 1.6f, 2.2f);
            BuildBed("CamaInfantil", "CamaRosa", "MaderaClara", 1.2f, 1.9f);
            BuildBed("CamaReal", "CamaRoja", "MaderaOscura", 2.0f, 2.3f);
            BuildNightstand();
            BuildWardrobe();
            BuildSofa("Sofa", "SofaRojo", 2.0f);
            BuildSofa("SofaTurquesa", "SofaTurquesa", 2.0f);
            BuildSofa("SofaMorado", "SofaMorado", 2.2f);
            BuildArmchair();
            BuildTable("MesaCocina", "Mantel");
            BuildTable("MesaComedor", "MantelAzul");
            BuildChair();
            BuildCounter("Encimera", false);
            BuildCounter("Fregadero", true);
            BuildStove();
            BuildFridge();
            BuildBookshelf();
            BuildDresser();
            BuildFloorLamp();
            BuildPlant("Planta", 1f);
            BuildPlant("PlantaPequena", 0.65f);
            BuildTV();
            BuildCoffeeTable();
            BuildRug("Alfombra", "Alfombra1");
            BuildRug("AlfombraAzul", "Alfombra2");
            BuildRug("AlfombraDorada", "Alfombra3");
            BuildBathtub();
            BuildToilet();
            BuildSink();
            BuildWasher();
            BuildBasket();
            BuildCurtain("Cortina", "CortinaRoja");
            BuildCurtain("CortinaVerde", "CortinaVerde");
            BuildDesk();
            BuildPedestal();
            BuildPiano();
            BuildFireplace();
            BuildPainting("Cuadro", "Agua", "Planta");
            BuildPainting("CuadroRetrato", "Alfombra3", "BataMorada");
            BuildBoxes();
            BuildClothes();
            BuildToys();
            BuildToyChest();
            BuildVase();
            BuildPlates();
            BuildDogBed();
            BuildTrash();
            BuildCar();
            BuildStatue();
            BuildBush();
            BuildTree();
            BuildMailbox();
            BuildExit("Gatera", false);
            BuildExit("PuertaMansion", true);
            BuildSecurityCamera();
            BuildMotionSensor();

            BuildLoot();
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ personajes

        static void BuildRaccoon()
        {
            var root = R("Mapache");
            root.gameObject.layer = Layers.Player;
            var cc = root.gameObject.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.33f, 0f);
            cc.radius = 0.26f;
            cc.height = 0.66f;
            cc.stepOffset = 0.15f;
            cc.skinWidth = 0.03f;

            var model = Pv(root, "Modelo", 0, 0, 0);
            var body = Pv(model, "Cuerpo", 0, 0.34f, 0);
            S(body, "MapacheGris", 0, 0, 0, 0.5f, 0.42f, 0.72f);
            S(body, "MapacheCrema", 0, -0.07f, 0.06f, 0.38f, 0.3f, 0.56f);

            var pack = Pv(body, "Mochila", 0, 0.2f, -0.1f);
            B(pack, "Mochila", 0, 0, 0, 0.32f, 0.22f, 0.3f);
            B(pack, "MaderaOscura", 0, 0.115f, 0, 0.34f, 0.03f, 0.32f);
            B(pack, "Dorado", 0, 0.02f, -0.155f, 0.07f, 0.07f, 0.02f);
            foreach (float s in new[] { -1f, 1f }) B(pack, "MaderaOscura", s * 0.12f, -0.08f, 0.16f, 0.04f, 0.2f, 0.04f);

            var head = Pv(body, "Cabeza", 0, 0.13f, 0.36f);
            S(head, "MapacheGris", 0, 0.05f, 0.06f, 0.42f, 0.36f, 0.38f);
            S(head, "MapacheCrema", 0, -0.03f, 0.2f, 0.34f, 0.2f, 0.22f);
            S(head, "Blanco", 0, 0.16f, 0.17f, 0.3f, 0.06f, 0.16f);
            S(head, "MapacheOscuro", 0, 0.08f, 0.19f, 0.45f, 0.13f, 0.24f);
            foreach (float s in new[] { -1f, 1f })
            {
                S(head, "OjoBlanco", s * 0.09f, 0.09f, 0.285f, 0.085f, 0.085f, 0.06f);
                S(head, "Negro", s * 0.09f, 0.09f, 0.315f, 0.045f, 0.05f, 0.03f);
            }
            S(head, "MapacheCrema", 0, -0.01f, 0.3f, 0.15f, 0.11f, 0.16f);
            S(head, "Negro", 0, 0.02f, 0.385f, 0.065f, 0.05f, 0.05f);
            var earL = Pv(head, "OrejaIzq", -0.13f, 0.2f, 0.02f, 0, 0, 15);
            Co(earL, "MapacheGris", 0, 0.06f, 0, 0.13f, 0.14f, 0.07f);
            Co(earL, "MapacheOscuro", 0, 0.055f, 0.025f, 0.08f, 0.09f, 0.03f);
            var earR = Pv(head, "OrejaDer", 0.13f, 0.2f, 0.02f, 0, 0, -15);
            Co(earR, "MapacheGris", 0, 0.06f, 0, 0.13f, 0.14f, 0.07f);
            Co(earR, "MapacheOscuro", 0, 0.055f, 0.025f, 0.08f, 0.09f, 0.03f);
            var headSlot = Pv(head, "RanuraCabeza", 0, 0.23f, 0.05f);
            var holdSlot = Pv(body, "RanuraBrazos", 0, 0.62f, 0.12f);

            var legs = new Transform[4];
            float[,] lp = { { -0.14f, 0.22f }, { 0.14f, 0.22f }, { -0.15f, -0.2f }, { 0.15f, -0.2f } };
            for (int i = 0; i < 4; i++)
            {
                var leg = Pv(model, "Pata" + i, lp[i, 0], 0.2f, lp[i, 1]);
                Cy(leg, "MapacheOscuro", 0, -0.09f, 0, 0.1f, 0.18f, 0.1f);
                S(leg, "Negro", 0, -0.18f, 0.025f, 0.12f, 0.06f, 0.14f);
                legs[i] = leg;
            }

            var tail = new List<Transform>();
            Transform prev = Pv(model, "Cola", 0, 0.36f, -0.34f);
            tail.Add(prev);
            for (int i = 0; i < 6; i++)
            {
                float size = 0.21f - i * 0.016f;
                S(prev, i % 2 == 0 ? "MapacheGris" : "MapacheOscuro", 0, 0, -0.06f, size, size, 0.15f);
                if (i < 5)
                {
                    prev = Pv(prev, "ColaSeg" + (i + 1), 0, 0.01f, -0.11f);
                    tail.Add(prev);
                }
            }

            var dizzy = Pv(model, "Mareo", 0, 0.85f, 0.3f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                Oc(dizzy, "Brillo", Mathf.Cos(a) * 0.22f, 0, Mathf.Sin(a) * 0.22f, 0.1f, 0.1f, 0.1f);
            }

            var vis = root.gameObject.AddComponent<RaccoonVisual>();
            vis.model = model; vis.body = body; vis.head = head; vis.legs = legs; vis.tailSegments = tail.ToArray();
            vis.earL = earL; vis.earR = earR; vis.backpack = pack; vis.dizzyStars = dizzy.gameObject;
            var ctrl = root.gameObject.AddComponent<RaccoonController>();
            ctrl.visual = vis; ctrl.headSlot = headSlot; ctrl.holdSlot = holdSlot;
            Save(root, "Personajes");
        }

        static void BuildHuman(string name, string shirt, string pants, string skin, string hair, int hairStyle, bool mustache, bool glasses, string capColor, bool bowtie, string displayName)
        {
            var root = R(name);
            var agent = root.gameObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f; agent.height = 1.75f; agent.speed = 1.5f; agent.enabled = false;
            var model = Pv(root, "Modelo", 0, 0, 0);

            var legL = Pv(model, "PiernaIzq", -0.12f, 0.8f, 0);
            var legR = Pv(model, "PiernaDer", 0.12f, 0.8f, 0);
            foreach (var leg in new[] { legL, legR })
            {
                Cy(leg, pants, 0, -0.38f, 0, 0.19f, 0.76f, 0.19f);
                B(leg, "Pantuflas", 0, -0.77f, 0.06f, 0.18f, 0.09f, 0.3f);
            }
            var body = Pv(model, "Torso", 0, 0.8f, 0);
            S(body, shirt, 0, 0.34f, 0, 0.58f, 0.8f, 0.42f);
            S(body, pants, 0, 0.04f, 0, 0.5f, 0.24f, 0.38f);
            S(body, "Blanco", 0, 0.48f, 0.205f, 0.05f, 0.05f, 0.03f);
            S(body, "Blanco", 0, 0.34f, 0.215f, 0.05f, 0.05f, 0.03f);
            if (bowtie)
            {
                B(body, "CamisaBlanca", 0, 0.58f, 0.17f, 0.18f, 0.14f, 0.06f);
                B(body, "Collar", 0, 0.66f, 0.2f, 0.14f, 0.06f, 0.04f);
            }

            var head = Pv(body, "Cabeza", 0, 0.72f, 0);
            S(head, skin, 0, 0.17f, 0, 0.38f, 0.42f, 0.38f);
            S(head, skin, 0, 0.15f, 0.2f, 0.09f, 0.1f, 0.1f);
            foreach (float s in new[] { -1f, 1f })
            {
                S(head, "Negro", s * 0.08f, 0.22f, 0.17f, 0.05f, 0.065f, 0.03f);
                S(head, skin, s * 0.19f, 0.17f, 0, 0.07f, 0.1f, 0.06f);
            }
            switch (hairStyle)
            {
                case 0: S(head, hair, 0, 0.3f, -0.03f, 0.41f, 0.24f, 0.41f); break;
                case 1:
                    S(head, hair, 0, 0.3f, -0.03f, 0.41f, 0.24f, 0.41f);
                    S(head, hair, 0, 0.42f, -0.1f, 0.18f, 0.16f, 0.18f);
                    break;
                default:
                    foreach (float s in new[] { -1f, 1f }) S(head, hair, s * 0.17f, 0.22f, -0.05f, 0.1f, 0.15f, 0.22f);
                    break;
            }
            if (mustache) S(head, hair, 0, 0.09f, 0.19f, 0.22f, 0.05f, 0.06f);
            if (glasses)
            {
                B(head, "Negro", 0, 0.22f, 0.2f, 0.3f, 0.025f, 0.02f);
                foreach (float s in new[] { -1f, 1f }) Cy(head, "Cristal", s * 0.08f, 0.22f, 0.195f, 0.1f, 0.015f, 0.1f, 90, 0, 0);
            }
            GameObject nightcap = null;
            if (capColor != null)
            {
                var nc = Pv(head, "GorroDormir", 0, 0.33f, -0.02f, -20, 0, 0);
                Co(nc, capColor, 0, 0.13f, 0, 0.38f, 0.36f, 0.38f);
                S(nc, "Blanco", 0, 0.31f, 0, 0.1f, 0.1f, 0.1f);
                B(nc, "Blanco", 0, -0.02f, 0, 0.4f, 0.05f, 0.4f);
                nightcap = nc.gameObject;
            }

            var armL = Pv(body, "BrazoIzq", -0.32f, 0.6f, 0);
            var armR = Pv(body, "BrazoDer", 0.32f, 0.6f, 0);
            foreach (var arm in new[] { armL, armR })
            {
                Cy(arm, shirt, 0, -0.26f, 0, 0.14f, 0.52f, 0.14f);
                S(arm, skin, 0, -0.56f, 0, 0.14f, 0.14f, 0.14f);
            }
            var fl = Pv(armR, "Linterna", 0, -0.6f, 0.04f);
            Cy(fl, "Negro", 0, -0.06f, 0, 0.07f, 0.18f, 0.07f);
            Cy(fl, "Linterna", 0, -0.16f, 0, 0.085f, 0.02f, 0.085f);
            var lt = Pv(fl, "Haz", 0, -0.18f, 0, 90, 0, 0);
            var light = lt.gameObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.93f, 0.7f);
            light.intensity = 4f;
            light.range = 8f;
            light.spotAngle = 55f;
            light.shadows = LightShadows.None;

            var vis = root.gameObject.AddComponent<HumanVisual>();
            vis.model = model; vis.body = body; vis.head = head; vis.armL = armL; vis.armR = armR; vis.legL = legL; vis.legR = legR;
            vis.nightcap = nightcap; vis.flashlight = light;
            var res = root.gameObject.AddComponent<Resident>();
            res.visual = vis;
            res.displayName = displayName;
            Save(root, "Personajes");
        }

        static void BuildDog()
        {
            var root = R("Perro");
            var agent = root.gameObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.25f; agent.height = 0.7f; agent.enabled = false;
            var model = Pv(root, "Modelo", 0, 0, 0);
            S(model, "PerroMarron", 0, 0.42f, 0, 0.38f, 0.34f, 0.74f);
            S(model, "PerroClaro", 0, 0.36f, 0.06f, 0.3f, 0.24f, 0.56f);
            S(model, "PerroOscuro", 0.13f, 0.5f, -0.12f, 0.16f, 0.12f, 0.22f);
            var head = Pv(model, "Cabeza", 0, 0.6f, 0.36f);
            S(head, "PerroMarron", 0, 0.04f, 0.04f, 0.32f, 0.3f, 0.32f);
            S(head, "PerroClaro", 0, -0.03f, 0.2f, 0.18f, 0.14f, 0.22f);
            S(head, "Negro", 0, 0.02f, 0.31f, 0.08f, 0.06f, 0.06f);
            S(head, "Rosa", 0, -0.1f, 0.24f, 0.07f, 0.02f, 0.1f);
            foreach (float s in new[] { -1f, 1f })
            {
                S(head, "OjoBlanco", s * 0.08f, 0.1f, 0.17f, 0.07f, 0.07f, 0.05f);
                S(head, "Negro", s * 0.08f, 0.1f, 0.195f, 0.04f, 0.045f, 0.02f);
                S(head, "PerroOscuro", s * 0.16f, 0.02f, 0f, 0.09f, 0.24f, 0.15f, 0, 0, s * 15f);
            }
            Cy(model, "Collar", 0, 0.54f, 0.27f, 0.28f, 0.05f, 0.28f, 70, 0, 0);
            S(model, "Dorado", 0, 0.47f, 0.37f, 0.06f, 0.06f, 0.03f);
            var legs = new Transform[4];
            float[,] lp = { { -0.12f, 0.24f }, { 0.12f, 0.24f }, { -0.12f, -0.22f }, { 0.12f, -0.22f } };
            for (int i = 0; i < 4; i++)
            {
                var leg = Pv(model, "Pata" + i, lp[i, 0], 0.28f, lp[i, 1]);
                Cy(leg, "PerroMarron", 0, -0.13f, 0, 0.1f, 0.26f, 0.1f);
                S(leg, "PerroClaro", 0, -0.26f, 0.02f, 0.11f, 0.06f, 0.13f);
                legs[i] = leg;
            }
            var tail = Pv(model, "Cola", 0, 0.52f, -0.36f, 35, 0, 0);
            Cy(tail, "PerroMarron", 0, 0, -0.12f, 0.06f, 0.26f, 0.06f, 90, 0, 0);
            var dog = root.gameObject.AddComponent<Dog>();
            dog.model = model; dog.head = head; dog.tail = tail; dog.legs = legs;
            Save(root, "Personajes");
        }

        // ------------------------------------------------------------------ muebles

        static void BuildBed(string name, string blanket, string wood, float w, float l)
        {
            var root = R(name);
            float hl = l / 2f;
            B(root, wood, 0, 0.22f, 0, w, 0.18f, l);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    B(root, wood, sx * (w / 2f - 0.07f), 0.07f, sz * (hl - 0.07f), 0.1f, 0.14f, 0.1f);
            B(root, "Sabana", 0, 0.4f, 0.03f, w - 0.08f, 0.2f, l - 0.12f);
            var blanketGo = B(root, blanket, 0, 0.5f, 0.3f, w - 0.02f, 0.1f, l * 0.6f);
            B(root, blanket, 0, 0.36f, hl - 0.02f, w - 0.02f, 0.3f, 0.04f);
            B(root, "Almohada", 0, 0.56f, -hl + 0.38f, w * 0.6f, 0.12f, 0.36f);
            B(root, wood, 0, 0.6f, -hl + 0.04f, w + 0.1f, 1.0f, 0.1f);
            B(root, wood, 0, 1.12f, -hl + 0.04f, w + 0.16f, 0.06f, 0.14f);
            Col(root, 0, 0.35f, 0, w, 0.7f, l);
            Col(root, 0, 0.6f, -hl + 0.04f, w + 0.1f, 1.2f, 0.12f);
            var eyes = Eyes(root, 0, 0.12f, hl + 0.03f);
            Hide(root, "debajo de la cama", new Vector3(0, 0.05f, 0.2f),
                new[] { new Vector3(w / 2f + 0.4f, 0, 0.3f), new Vector3(-w / 2f - 0.4f, 0, 0.3f), new Vector3(0, 0, hl + 0.45f) }, eyes, blanketGo.transform);
            Save(root, "Muebles");
        }

        static void BuildNightstand()
        {
            var root = R("MesitaNoche");
            B(root, "MaderaClara", 0, 0.27f, 0, 0.5f, 0.54f, 0.45f);
            B(root, "MaderaMedia", 0, 0.33f, 0.226f, 0.4f, 0.16f, 0.02f);
            S(root, "Dorado", 0, 0.33f, 0.24f, 0.04f, 0.04f, 0.03f);
            var lamp = Pv(root, "Lamparita", -0.15f, 0.54f, -0.1f);
            Cy(lamp, "Dorado", 0, 0.12f, 0, 0.03f, 0.24f, 0.03f);
            Sh(lamp, "Pantalla", 0, 0.28f, 0, 0.18f, 0.14f, 0.18f);
            AddLight(lamp, LightType.Point, new Color(1f, 0.82f, 0.55f), 0.7f, 2.6f, 0, 0.25f, 0.1f);
            Col(root, 0, 0.27f, 0, 0.5f, 0.54f, 0.45f);
            Save(root, "Muebles");
        }

        static void BuildWardrobe()
        {
            var root = R("Armario");
            var body = Pv(root, "Cuerpo", 0, 0, 0);
            B(body, "MaderaMedia", 0, 1.0f, 0, 1.2f, 2.0f, 0.6f);
            foreach (float s in new[] { -1f, 1f })
            {
                B(body, "MaderaClara", s * 0.3f, 1.02f, 0.305f, 0.57f, 1.86f, 0.03f);
                S(body, "Dorado", s * 0.06f, 1.05f, 0.33f, 0.05f, 0.05f, 0.04f);
            }
            B(body, "MaderaOscura", 0, 2.03f, 0, 1.3f, 0.08f, 0.66f);
            B(body, "MaderaOscura", 0, 0.03f, 0, 1.25f, 0.06f, 0.64f);
            Col(root, 0, 1.0f, 0, 1.2f, 2.0f, 0.6f);
            var eyes = Eyes(root, 0, 0.5f, 0.33f);
            Hide(root, "dentro del armario", new Vector3(0, 0.05f, 0), new[] { new Vector3(0, 0, 0.8f), new Vector3(0.5f, 0, 0.8f), new Vector3(-0.5f, 0, 0.8f) }, eyes, body);
            AddFade(root);
            Save(root, "Muebles");
        }

        static void BuildSofa(string name, string color, float w)
        {
            var root = R(name);
            var body = Pv(root, "Cuerpo", 0, 0, 0);
            B(body, color, 0, 0.25f, 0.05f, w, 0.3f, 0.85f);
            foreach (float s in new[] { -1f, 1f })
            {
                B(body, color, s * (w / 4f - 0.02f), 0.45f, 0.1f, w / 2f - 0.12f, 0.14f, 0.64f);
                B(body, color, s * (w / 2f - 0.08f), 0.47f, 0.05f, 0.18f, 0.44f, 0.86f);
                B(body, "MaderaOscura", s * (w / 2f - 0.12f), 0.05f, 0.4f, 0.08f, 0.1f, 0.08f);
                B(body, "MaderaOscura", s * (w / 2f - 0.12f), 0.05f, -0.3f, 0.08f, 0.1f, 0.08f);
            }
            B(body, color, 0, 0.62f, -0.31f, w, 0.62f, 0.24f);
            B(body, "Almohada", w / 2f - 0.45f, 0.62f, -0.12f, 0.3f, 0.28f, 0.1f, -10, 0, 8);
            Col(root, 0, 0.45f, 0.02f, w, 0.9f, 0.9f);
            var eyes = Eyes(root, 0, 0.09f, 0.5f);
            Hide(root, "debajo del sofá", new Vector3(0, 0.05f, 0),
                new[] { new Vector3(0, 0, 0.95f), new Vector3(0, 0, -0.95f), new Vector3(w / 2f + 0.35f, 0, 0), new Vector3(-w / 2f - 0.35f, 0, 0) }, eyes, body);
            Save(root, "Muebles");
        }

        static void BuildArmchair()
        {
            var root = R("Sillon");
            B(root, "SofaMostaza", 0, 0.25f, 0.03f, 0.9f, 0.3f, 0.85f);
            B(root, "SofaMostaza", 0, 0.45f, 0.08f, 0.6f, 0.14f, 0.62f);
            B(root, "SofaMostaza", 0, 0.65f, -0.32f, 0.9f, 0.7f, 0.2f);
            foreach (float s in new[] { -1f, 1f }) B(root, "SofaMostaza", s * 0.38f, 0.48f, 0.03f, 0.16f, 0.46f, 0.85f);
            Col(root, 0, 0.45f, 0, 0.9f, 0.9f, 0.88f);
            Save(root, "Muebles");
        }

        static void BuildTable(string name, string cloth)
        {
            var root = R(name);
            B(root, "MaderaClara", 0, 0.72f, 0, 1.6f, 0.06f, 1.0f);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    B(root, "MaderaMedia", sx * 0.7f, 0.36f, sz * 0.4f, 0.08f, 0.72f, 0.08f);
            var c = Pv(root, "Mantel", 0, 0, 0);
            B(c, cloth, 0, 0.755f, 0, 1.66f, 0.02f, 1.06f);
            foreach (float s in new[] { -1f, 1f })
            {
                B(c, cloth, 0, 0.52f, s * 0.53f, 1.66f, 0.46f, 0.02f);
                B(c, cloth, s * 0.83f, 0.52f, 0, 0.02f, 0.46f, 1.06f);
            }
            B(c, "Blanco", 0, 0.29f, 0.54f, 1.66f, 0.03f, 0.02f);
            Col(root, 0, 0.39f, 0, 1.62f, 0.78f, 1.02f);
            var eyes = Eyes(root, 0, 0.16f, 0.56f);
            Hide(root, "debajo de la mesa", new Vector3(0, 0.05f, 0),
                new[] { new Vector3(0, 0, 0.95f), new Vector3(0, 0, -0.95f), new Vector3(1.2f, 0, 0), new Vector3(-1.2f, 0, 0) }, eyes, c);
            Save(root, "Muebles");
        }

        static void BuildChair()
        {
            var root = R("Silla");
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Cy(root, "MaderaMedia", sx * 0.18f, 0.22f, sz * 0.18f, 0.05f, 0.44f, 0.05f);
            B(root, "MaderaClara", 0, 0.46f, 0, 0.45f, 0.05f, 0.45f);
            B(root, "Mantel", 0, 0.5f, 0.02f, 0.38f, 0.04f, 0.36f);
            B(root, "MaderaMedia", 0, 0.72f, -0.2f, 0.45f, 0.45f, 0.05f);
            Col(root, 0, 0.45f, 0, 0.45f, 0.9f, 0.45f);
            var bump = root.gameObject.AddComponent<Bumpable>();
            bump.displayName = "una silla"; bump.noise = 18f; bump.tipsOver = true;
            Save(root, "Muebles");
        }

        static void BuildCounter(string name, bool sink)
        {
            var root = R(name);
            B(root, "MuebleBlanco", 0, 0.43f, 0, 1.0f, 0.86f, 0.6f);
            B(root, "Encimera", 0, 0.885f, 0.02f, 1.02f, 0.05f, 0.64f);
            foreach (float s in new[] { -1f, 1f })
            {
                B(root, "MuebleBlanco", s * 0.25f, 0.42f, 0.305f, 0.46f, 0.72f, 0.02f);
                B(root, "Acero", s * 0.06f, 0.6f, 0.32f, 0.03f, 0.16f, 0.03f);
            }
            if (sink)
            {
                B(root, "Acero", 0, 0.9f, 0.04f, 0.55f, 0.02f, 0.38f);
                B(root, "Agua", 0, 0.905f, 0.04f, 0.45f, 0.01f, 0.28f);
                Cy(root, "Acero", 0, 1.02f, -0.2f, 0.04f, 0.24f, 0.04f);
                B(root, "Acero", 0, 1.13f, -0.1f, 0.04f, 0.04f, 0.22f);
            }
            Col(root, 0, 0.45f, 0, 1.0f, 0.9f, 0.6f);
            Save(root, "Muebles");
        }

        static void BuildStove()
        {
            var root = R("Fogones");
            B(root, "Acero", 0, 0.43f, 0, 0.8f, 0.86f, 0.6f);
            B(root, "Negro", 0, 0.88f, 0.02f, 0.8f, 0.04f, 0.62f);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Cy(root, "MapacheOscuro", sx * 0.18f, 0.905f, sz * 0.14f, 0.2f, 0.01f, 0.2f);
            B(root, "Negro", 0, 0.42f, 0.305f, 0.6f, 0.4f, 0.02f);
            B(root, "Acero", 0, 0.68f, 0.32f, 0.5f, 0.03f, 0.03f);
            Cy(root, "Olla", 0.18f, 0.99f, -0.14f, 0.26f, 0.16f, 0.26f);
            Col(root, 0, 0.45f, 0, 0.8f, 0.9f, 0.6f);
            Save(root, "Muebles");
        }

        static void BuildFridge()
        {
            var root = R("Nevera");
            B(root, "Nevera", 0, 0.95f, 0, 0.8f, 1.9f, 0.7f);
            B(root, "Encimera", 0, 1.25f, 0.355f, 0.78f, 0.02f, 0.01f);
            B(root, "Acero", 0.3f, 1.6f, 0.37f, 0.04f, 0.4f, 0.04f);
            B(root, "Acero", 0.3f, 0.75f, 0.37f, 0.04f, 0.5f, 0.04f);
            B(root, "Juguete1", -0.2f, 1.55f, 0.36f, 0.08f, 0.08f, 0.02f);
            B(root, "Juguete3", -0.05f, 1.45f, 0.36f, 0.12f, 0.09f, 0.02f);
            B(root, "Juguete2", -0.22f, 0.9f, 0.36f, 0.14f, 0.18f, 0.02f);
            Col(root, 0, 0.95f, 0, 0.8f, 1.9f, 0.7f);
            AddFade(root);
            Save(root, "Muebles");
        }

        static void BuildBookshelf()
        {
            var root = R("Estanteria");
            B(root, "MaderaOscura", 0, 0.95f, -0.18f, 1.2f, 1.9f, 0.04f);
            foreach (float s in new[] { -1f, 1f }) B(root, "MaderaOscura", s * 0.58f, 0.95f, 0, 0.04f, 1.9f, 0.4f);
            float[] shelves = { 0.03f, 0.5f, 0.95f, 1.4f, 1.88f };
            foreach (float y in shelves) B(root, "MaderaMedia", 0, y, 0, 1.16f, 0.04f, 0.4f);
            var rng = new System.Random(5);
            string[] books = { "Libro1", "Libro2", "Libro3", "Libro4", "Libro5" };
            for (int sIdx = 0; sIdx < 4; sIdx++)
            {
                float y0 = shelves[sIdx] + 0.02f;
                float x = -0.54f;
                while (x < 0.52f)
                {
                    float bw = 0.05f + (float)rng.NextDouble() * 0.05f;
                    float bh = 0.25f + (float)rng.NextDouble() * 0.12f;
                    bool gap = sIdx == 2 && x > -0.25f && x < 0.25f; // hueco central para dejar objetos
                    if (!gap) B(root, books[rng.Next(books.Length)], x + bw / 2f, y0 + bh / 2f, 0.02f, bw, bh, 0.28f, 0, 0, rng.NextDouble() < 0.1 ? 8f : 0f);
                    x += bw + 0.01f;
                }
            }
            Col(root, 0, 0.95f, 0, 1.2f, 1.9f, 0.4f);
            AddFade(root);
            Save(root, "Muebles");
        }

        static void BuildDresser()
        {
            var root = R("Comoda");
            B(root, "MaderaClara", 0, 0.4f, 0, 1.0f, 0.78f, 0.45f);
            B(root, "MaderaMedia", 0, 0.8f, 0, 1.04f, 0.04f, 0.48f);
            for (int i = 0; i < 3; i++)
            {
                B(root, "MaderaMedia", 0, 0.16f + i * 0.24f, 0.226f, 0.9f, 0.2f, 0.02f);
                S(root, "Dorado", 0, 0.16f + i * 0.24f, 0.24f, 0.05f, 0.05f, 0.03f);
            }
            Col(root, 0, 0.41f, 0, 1.0f, 0.82f, 0.45f);
            Save(root, "Muebles");
        }

        static void BuildFloorLamp()
        {
            var root = R("Lampara");
            Cy(root, "MapacheOscuro", 0, 0.03f, 0, 0.36f, 0.06f, 0.36f);
            Cy(root, "Acero", 0, 0.8f, 0, 0.05f, 1.5f, 0.05f);
            Sh(root, "Pantalla", 0, 1.6f, 0, 0.5f, 0.36f, 0.5f);
            var l = AddLight(root, LightType.Point, new Color(1f, 0.8f, 0.52f), 1.3f, 5.5f, 0, 1.45f, 0);
            Col(root, 0, 0.8f, 0, 0.36f, 1.6f, 0.36f);
            var bump = root.gameObject.AddComponent<Bumpable>();
            bump.displayName = "una lámpara"; bump.noise = 22f; bump.tipsOver = true; bump.lightToTurnOff = l;
            Save(root, "Muebles");
        }

        static void BuildPlant(string name, float s)
        {
            var root = R(name);
            var p = Pv(root, "Planta", 0, 0, 0);
            p.localScale = Vector3.one * s;
            Pt(p, "Maceta", 0, 0.2f, 0, 0.45f, 0.4f, 0.45f);
            Cy(p, "Tierra", 0, 0.39f, 0, 0.4f, 0.03f, 0.4f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                var leaf = Pv(p, "Hoja" + i, 0, 0.4f, 0, 0, a, 0);
                Co(leaf, i % 2 == 0 ? "Planta" : "PlantaOscura", 0, 0.3f, 0.12f, 0.18f, 0.6f, 0.1f, 25, 0, 0);
            }
            S(p, "Planta", 0, 0.75f, 0, 0.3f, 0.35f, 0.3f);
            Col(root, 0, 0.4f * s, 0, 0.45f * s, 0.8f * s, 0.45f * s);
            Save(root, "Muebles");
        }

        static void BuildTV()
        {
            var root = R("MuebleTV");
            B(root, "MaderaOscura", 0, 0.25f, 0, 1.4f, 0.5f, 0.45f);
            B(root, "MaderaMedia", 0, 0.25f, 0.226f, 1.3f, 0.36f, 0.02f);
            B(root, "Negro", 0, 0.54f, -0.05f, 0.3f, 0.08f, 0.2f);
            B(root, "Negro", 0, 0.92f, -0.05f, 1.15f, 0.68f, 0.06f);
            B(root, "PantallaTV", 0, 0.92f, -0.015f, 1.04f, 0.58f, 0.01f);
            AddLight(root, LightType.Point, new Color(0.45f, 0.7f, 1f), 0.9f, 3.2f, 0, 0.9f, 0.7f);
            Col(root, 0, 0.6f, 0, 1.4f, 1.2f, 0.45f);
            Save(root, "Muebles");
        }

        static void BuildCoffeeTable()
        {
            var root = R("MesaCentro");
            B(root, "MaderaClara", 0, 0.38f, 0, 1.0f, 0.05f, 0.6f);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    B(root, "MaderaMedia", sx * 0.42f, 0.18f, sz * 0.22f, 0.06f, 0.36f, 0.06f);
            B(root, "Libro2", -0.3f, 0.415f, 0.1f, 0.24f, 0.02f, 0.18f, 0, 15, 0);
            Col(root, 0, 0.2f, 0, 1.0f, 0.42f, 0.6f);
            Save(root, "Muebles");
        }

        static void BuildRug(string name, string color)
        {
            var root = R(name);
            B(root, "AlfombraBorde", 0, 0.008f, 0, 2.5f, 0.016f, 1.7f);
            B(root, color, 0, 0.012f, 0, 2.3f, 0.016f, 1.5f);
            B(root, "AlfombraBorde", 0, 0.014f, 0, 1.4f, 0.016f, 0.7f);
            B(root, color, 0, 0.016f, 0, 1.2f, 0.016f, 0.5f);
            Save(root, "Decoracion");
        }

        static void BuildBathtub()
        {
            var root = R("Banera");
            B(root, "Porcelana", 0, 0.28f, 0, 1.6f, 0.5f, 0.8f);
            B(root, "Porcelana", 0, 0.54f, 0, 1.66f, 0.06f, 0.86f);
            B(root, "Agua", 0, 0.46f, 0, 1.4f, 0.04f, 0.6f);
            S(root, "Nata", 0.3f, 0.5f, 0.1f, 0.3f, 0.1f, 0.25f);
            S(root, "Nata", 0.45f, 0.5f, -0.1f, 0.2f, 0.08f, 0.2f);
            Cy(root, "Acero", -0.72f, 0.7f, 0, 0.05f, 0.3f, 0.05f);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    S(root, "Dorado", sx * 0.7f, 0.04f, sz * 0.3f, 0.1f, 0.08f, 0.1f);
            Col(root, 0, 0.3f, 0, 1.6f, 0.6f, 0.8f);
            Save(root, "Muebles");
        }

        static void BuildToilet()
        {
            var root = R("Inodoro");
            Cy(root, "Porcelana", 0, 0.2f, 0.05f, 0.38f, 0.4f, 0.48f);
            Cy(root, "Porcelana", 0, 0.42f, 0.07f, 0.42f, 0.05f, 0.52f);
            B(root, "Porcelana", 0, 0.62f, -0.22f, 0.46f, 0.42f, 0.18f);
            B(root, "Acero", 0.15f, 0.8f, -0.2f, 0.08f, 0.03f, 0.04f);
            Col(root, 0, 0.4f, -0.02f, 0.46f, 0.8f, 0.65f);
            Save(root, "Muebles");
        }

        static void BuildSink()
        {
            var root = R("Lavabo");
            Cy(root, "Porcelana", 0, 0.38f, -0.05f, 0.18f, 0.76f, 0.18f);
            B(root, "Porcelana", 0, 0.8f, 0, 0.56f, 0.12f, 0.45f);
            B(root, "Agua", 0, 0.865f, 0.02f, 0.42f, 0.01f, 0.3f);
            Cy(root, "Acero", 0, 0.92f, -0.17f, 0.04f, 0.14f, 0.04f);
            B(root, "Dorado", 0, 1.3f, -0.21f, 0.5f, 0.56f, 0.03f);
            B(root, "Espejo", 0, 1.3f, -0.19f, 0.42f, 0.48f, 0.01f);
            Col(root, 0, 0.45f, 0, 0.56f, 0.9f, 0.45f);
            Save(root, "Muebles");
        }

        static void BuildWasher()
        {
            var root = R("Lavadora");
            B(root, "MuebleBlanco", 0, 0.43f, 0, 0.66f, 0.86f, 0.64f);
            Cy(root, "Acero", 0, 0.42f, 0.32f, 0.46f, 0.03f, 0.46f, 90, 0, 0);
            Cy(root, "Agua", 0, 0.42f, 0.335f, 0.36f, 0.02f, 0.36f, 90, 0, 0);
            B(root, "Encimera", 0, 0.8f, 0.3f, 0.6f, 0.08f, 0.04f);
            S(root, "LedRojo", 0.22f, 0.8f, 0.33f, 0.04f, 0.04f, 0.02f);
            Col(root, 0, 0.43f, 0, 0.66f, 0.86f, 0.64f);
            Save(root, "Muebles");
        }

        static void BuildBasket()
        {
            var root = R("CestoRopa");
            Pt(root, "Mimbre", 0, 0.27f, 0, 0.62f, 0.54f, 0.62f);
            var heap = Pv(root, "Ropa", 0, 0.52f, 0);
            S(heap, "PijamaAzul", -0.08f, 0, 0, 0.42f, 0.2f, 0.4f);
            S(heap, "BataRosa", 0.1f, 0.04f, 0.05f, 0.32f, 0.18f, 0.3f);
            S(heap, "Blanco", 0.02f, 0.08f, -0.1f, 0.26f, 0.12f, 0.22f);
            B(heap, "Juguete3", 0.2f, 0.02f, 0.22f, 0.08f, 0.04f, 0.22f, 0, 30, -40);
            Col(root, 0, 0.3f, 0, 0.62f, 0.62f, 0.62f);
            var eyes = Eyes(root, 0, 0.62f, 0.22f);
            Hide(root, "dentro del cesto de ropa", new Vector3(0, 0.05f, 0),
                new[] { new Vector3(0, 0, 0.75f), new Vector3(0.75f, 0, 0), new Vector3(-0.75f, 0, 0), new Vector3(0, 0, -0.75f) }, eyes, heap);
            Save(root, "Muebles");
        }

        static void BuildCurtain(string name, string color)
        {
            var root = R(name);
            Cy(root, "Acero", 0, 1.5f, 0.1f, 0.04f, 1.9f, 0.04f, 0, 0, 90);
            B(root, "Espejo", 0, 0.95f, 0.01f, 1.2f, 0.9f, 0.02f);
            var cloth = Pv(root, "Tela", 0, 0, 0);
            for (int i = 0; i < 6; i++)
            {
                float x = -0.7f + i * 0.28f;
                B(cloth, color, x, 0.77f, 0.12f + (i % 2 == 0 ? 0.03f : -0.02f), 0.29f, 1.45f, 0.05f);
            }
            B(cloth, "Dorado", -0.72f, 0.8f, 0.17f, 0.06f, 0.06f, 0.06f);
            Col(root, 0, 0.8f, 0.12f, 1.7f, 1.6f, 0.12f);
            var eyes = Eyes(root, 0.55f, 0.32f, 0.17f);
            Hide(root, "detrás de la cortina", new Vector3(0, 0.05f, 0.05f),
                new[] { new Vector3(0, 0, 0.75f), new Vector3(1.1f, 0, 0.45f), new Vector3(-1.1f, 0, 0.45f) }, eyes, cloth);
            Save(root, "Muebles");
        }

        static void BuildDesk()
        {
            var root = R("Escritorio");
            B(root, "MaderaMedia", 0, 0.73f, 0, 1.3f, 0.05f, 0.6f);
            B(root, "MaderaClara", 0.46f, 0.36f, 0, 0.36f, 0.7f, 0.58f);
            for (int i = 0; i < 3; i++) B(root, "MaderaMedia", 0.46f, 0.15f + i * 0.21f, 0.292f, 0.3f, 0.17f, 0.02f);
            B(root, "MaderaClara", -0.6f, 0.36f, 0, 0.06f, 0.7f, 0.58f);
            B(root, "MaderaClara", 0, 0.45f, -0.27f, 1.2f, 0.5f, 0.03f);
            B(root, "Negro", -0.05f, 1.0f, -0.2f, 0.5f, 0.32f, 0.04f);
            B(root, "PantallaTV", -0.05f, 1.0f, -0.177f, 0.44f, 0.26f, 0.01f);
            B(root, "Negro", -0.05f, 0.8f, -0.2f, 0.06f, 0.12f, 0.04f);
            Col(root, 0, 0.38f, 0, 1.3f, 0.76f, 0.6f);
            var eyes = Eyes(root, -0.2f, 0.15f, 0.32f);
            Hide(root, "debajo del escritorio", new Vector3(-0.2f, 0.05f, 0), new[] { new Vector3(-0.2f, 0, 0.85f), new Vector3(-1.0f, 0, 0.3f) }, eyes, null);
            Save(root, "Muebles");
        }

        static void BuildPedestal()
        {
            var root = R("Pedestal");
            B(root, "Marmol", 0, 0.05f, 0, 0.58f, 0.1f, 0.58f);
            B(root, "Marmol", 0, 0.52f, 0, 0.42f, 0.86f, 0.42f);
            B(root, "Dorado", 0, 0.98f, 0, 0.56f, 0.05f, 0.56f);
            B(root, "Terciopelo", 0, 1.02f, 0, 0.38f, 0.04f, 0.38f);
            Col(root, 0, 0.5f, 0, 0.58f, 1.0f, 0.58f);
            Save(root, "Muebles");
        }

        static void BuildPiano()
        {
            var root = R("Piano");
            B(root, "Negro", 0, 0.66f, -0.12f, 1.6f, 1.24f, 0.46f);
            B(root, "Negro", 0, 0.7f, 0.18f, 1.5f, 0.06f, 0.3f);
            B(root, "Blanco", 0, 0.74f, 0.2f, 1.42f, 0.03f, 0.24f);
            for (int i = 0; i < 10; i++) B(root, "Negro", -0.62f + i * 0.14f, 0.765f, 0.15f, 0.05f, 0.03f, 0.13f);
            foreach (float s in new[] { -1f, 1f }) B(root, "Negro", s * 0.72f, 0.35f, 0.25f, 0.08f, 0.7f, 0.08f);
            Cy(root, "Dorado", -0.5f, 1.38f, -0.12f, 0.06f, 0.24f, 0.06f);
            Cy(root, "Nata", -0.5f, 1.55f, -0.12f, 0.04f, 0.12f, 0.04f);
            Col(root, 0, 0.64f, 0, 1.6f, 1.28f, 0.7f);
            Save(root, "Muebles");
        }

        static void BuildFireplace()
        {
            var root = R("Chimenea");
            B(root, "Ladrillo", 0, 0.62f, 0, 1.6f, 1.24f, 0.5f);
            B(root, "Negro", 0, 0.38f, 0.2f, 0.8f, 0.62f, 0.14f);
            B(root, "MaderaOscura", 0, 1.27f, 0.04f, 1.8f, 0.08f, 0.6f);
            Co(root, "Fuego", -0.1f, 0.25f, 0.18f, 0.25f, 0.35f, 0.2f);
            Co(root, "Fuego", 0.12f, 0.22f, 0.2f, 0.2f, 0.28f, 0.18f);
            B(root, "Tronco", 0, 0.1f, 0.2f, 0.6f, 0.08f, 0.1f, 0, 10, 0);
            var l = AddLight(root, LightType.Point, new Color(1f, 0.55f, 0.2f), 1.6f, 5f, 0, 0.5f, 0.6f);
            l.gameObject.AddComponent<Flicker>();
            Col(root, 0, 0.65f, 0, 1.8f, 1.3f, 0.6f);
            Save(root, "Muebles");
        }

        static void BuildPainting(string name, string bg, string fg)
        {
            var root = R(name);
            B(root, "Dorado", 0, 1.0f, 0.02f, 1.0f, 0.7f, 0.04f);
            B(root, bg, 0, 1.0f, 0.045f, 0.86f, 0.56f, 0.01f);
            S(root, fg, -0.12f, 0.9f, 0.05f, 0.5f, 0.25f, 0.01f);
            S(root, "Juguete3", 0.25f, 1.12f, 0.052f, 0.14f, 0.14f, 0.01f);
            Save(root, "Decoracion");
        }

        static void BuildBoxes()
        {
            var root = R("Cajas");
            B(root, "Carton", 0, 0.25f, 0, 0.6f, 0.5f, 0.5f);
            B(root, "Cinta", 0, 0.5f, 0, 0.62f, 0.01f, 0.1f);
            B(root, "Carton", 0.05f, 0.7f, 0.02f, 0.45f, 0.4f, 0.4f, 0, 15, 0);
            B(root, "Carton", -0.48f, 0.2f, 0.08f, 0.38f, 0.4f, 0.38f, 0, -10, 0);
            B(root, "Cinta", -0.48f, 0.405f, 0.08f, 0.39f, 0.01f, 0.08f, 0, -10, 0);
            Col(root, -0.15f, 0.45f, 0.03f, 1.0f, 0.9f, 0.56f);
            Save(root, "Decoracion");
        }

        static void BuildClothes()
        {
            var root = R("RopaSuelo");
            S(root, "PijamaAzul", 0, 0.04f, 0, 0.6f, 0.08f, 0.4f, 0, 20, 0);
            S(root, "Juguete1", 0.25f, 0.06f, 0.15f, 0.35f, 0.1f, 0.3f);
            B(root, "Blanco", -0.3f, 0.04f, 0.2f, 0.12f, 0.06f, 0.28f, 0, 40, 0);
            B(root, "Juguete2", 0.4f, 0.04f, -0.2f, 0.12f, 0.06f, 0.28f, 0, -30, 0);
            Save(root, "Decoracion");
        }

        static void BuildToys()
        {
            var root = R("JuguetesSuelo");
            B(root, "Juguete1", 0, 0.08f, 0, 0.16f, 0.16f, 0.16f, 0, 20, 0);
            B(root, "Juguete2", 0.2f, 0.08f, 0.1f, 0.16f, 0.16f, 0.16f, 0, -15, 0);
            B(root, "Juguete3", 0.1f, 0.24f, 0.05f, 0.16f, 0.16f, 0.16f, 0, 40, 0);
            S(root, "Juguete4", -0.3f, 0.12f, 0.2f, 0.24f, 0.24f, 0.24f);
            B(root, "Juguete2", -0.1f, 0.05f, -0.3f, 0.3f, 0.1f, 0.14f, 0, 60, 0);
            Save(root, "Decoracion");
        }

        static void BuildToyChest()
        {
            var root = R("BaulJuguetes");
            B(root, "Juguete2", 0, 0.25f, 0, 0.9f, 0.5f, 0.5f);
            var lid = Pv(root, "Tapa", 0, 0.5f, -0.25f, -12, 0, 0);
            B(lid, "Juguete3", 0, 0.03f, 0.25f, 0.94f, 0.06f, 0.54f);
            Oc(root, "Juguete3", -0.25f, 0.3f, 0.255f, 0.12f, 0.12f, 0.02f);
            Oc(root, "Juguete1", 0.2f, 0.25f, 0.255f, 0.1f, 0.1f, 0.02f);
            Col(root, 0, 0.28f, 0, 0.9f, 0.56f, 0.5f);
            var eyes = Eyes(root, 0, 0.53f, 0.24f);
            Hide(root, "dentro del baúl de juguetes", new Vector3(0, 0.05f, 0), new[] { new Vector3(0, 0, 0.75f), new Vector3(0.8f, 0, 0), new Vector3(-0.8f, 0, 0) }, eyes, lid);
            Save(root, "Muebles");
        }

        static void BuildVase()
        {
            var root = R("Jarron");
            S(root, "JarronAzul", 0, 0.14f, 0, 0.22f, 0.28f, 0.22f);
            Cy(root, "JarronAzul", 0, 0.3f, 0, 0.1f, 0.12f, 0.1f);
            Cy(root, "Planta", 0, 0.42f, 0, 0.02f, 0.2f, 0.02f);
            S(root, "Flor1", 0, 0.53f, 0, 0.1f, 0.08f, 0.1f);
            S(root, "Flor2", 0.06f, 0.48f, 0.03f, 0.08f, 0.06f, 0.08f);
            var f = root.gameObject.AddComponent<Fragile>();
            f.displayName = "un jarrón"; f.noise = 28f; f.shardMesh = MeshLib.Cube; f.shardMaterial = MatLib.Get("JarronAzul");
            Save(root, "Fragiles");
        }

        static void BuildPlates()
        {
            var root = R("Platos");
            for (int i = 0; i < 5; i++) Cy(root, "Plato", 0.005f * (i % 2), 0.015f + i * 0.03f, 0, 0.3f, 0.025f, 0.3f);
            Cy(root, "Reloj", 0, 0.165f, 0, 0.2f, 0.005f, 0.2f);
            var f = root.gameObject.AddComponent<Fragile>();
            f.displayName = "una pila de platos"; f.noise = 30f; f.shardMesh = MeshLib.Cube; f.shardMaterial = MatLib.Get("Plato");
            Save(root, "Fragiles");
        }

        static void BuildDogBed()
        {
            var root = R("CamaPerro");
            Cy(root, "Collar", 0, 0.08f, 0, 0.95f, 0.16f, 0.75f);
            Cy(root, "PerroClaro", 0, 0.12f, 0, 0.75f, 0.1f, 0.55f);
            Cy(root, "Hueso", 0.25f, 0.2f, 0.05f, 0.05f, 0.2f, 0.05f, 0, 0, 90);
            Save(root, "Decoracion");
        }

        static void BuildTrash()
        {
            var root = R("Papelera");
            Cy(root, "Acero", 0, 0.3f, 0, 0.38f, 0.6f, 0.38f);
            Cy(root, "Encimera", 0, 0.62f, 0, 0.4f, 0.04f, 0.4f);
            S(root, "ManzanaRoja", 0.05f, 0.66f, 0.05f, 0.08f, 0.04f, 0.08f);
            Col(root, 0, 0.32f, 0, 0.38f, 0.64f, 0.38f);
            Save(root, "Decoracion");
        }

        static void BuildCar()
        {
            var root = R("Coche");
            B(root, "Coche", 0, 0.55f, 0, 1.7f, 0.55f, 3.6f);
            B(root, "Coche", 0, 1.02f, -0.2f, 1.5f, 0.45f, 1.8f);
            B(root, "Cristal", 0, 1.03f, -0.2f, 1.52f, 0.34f, 1.6f);
            B(root, "Cristal", 0, 1.03f, 0.71f, 1.3f, 0.32f, 0.02f, -25, 0, 0);
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f }) Cy(root, "Neumatico", sx * 0.82f, 0.34f, sz * 1.15f, 0.68f, 0.24f, 0.68f, 0, 0, 90);
                B(root, "Faro", sx * 0.58f, 0.6f, 1.8f, 0.3f, 0.14f, 0.03f);
                B(root, "LedRojo", sx * 0.6f, 0.62f, -1.8f, 0.26f, 0.12f, 0.03f);
            }
            Col(root, 0, 0.7f, 0, 1.75f, 1.4f, 3.65f);
            AddFade(root);
            Save(root, "Muebles");
        }

        static void BuildStatue()
        {
            var root = R("Estatua");
            B(root, "Marmol", 0, 0.45f, 0, 0.5f, 0.9f, 0.5f);
            B(root, "Dorado", 0, 0.92f, 0, 0.56f, 0.05f, 0.56f);
            S(root, "Marmol", 0, 1.15f, 0, 0.3f, 0.4f, 0.3f);
            S(root, "Marmol", 0, 1.42f, 0, 0.26f, 0.3f, 0.26f);
            S(root, "Marmol", 0, 1.42f, 0.13f, 0.06f, 0.08f, 0.08f);
            Col(root, 0, 0.7f, 0, 0.56f, 1.4f, 0.56f);
            Save(root, "Decoracion");
        }

        static void BuildBush()
        {
            var root = R("Arbusto");
            S(root, "Arbusto", 0, 0.3f, 0, 0.9f, 0.6f, 0.8f);
            S(root, "PlantaOscura", 0.3f, 0.25f, 0.15f, 0.55f, 0.45f, 0.5f);
            S(root, "Planta", -0.25f, 0.35f, -0.1f, 0.5f, 0.5f, 0.5f);
            S(root, "Flor1", 0.1f, 0.6f, 0.25f, 0.08f, 0.08f, 0.08f);
            Save(root, "Exterior");
        }

        static void BuildTree()
        {
            var root = R("Arbol");
            Cy(root, "Tronco", 0, 0.8f, 0, 0.3f, 1.6f, 0.3f);
            Co(root, "PlantaOscura", 0, 2.1f, 0, 1.8f, 1.6f, 1.8f);
            Co(root, "Arbusto", 0, 2.9f, 0, 1.4f, 1.3f, 1.4f);
            Co(root, "Planta", 0, 3.6f, 0, 0.9f, 1.0f, 0.9f);
            Save(root, "Exterior");
        }

        static void BuildMailbox()
        {
            var root = R("Buzon");
            Cy(root, "MaderaMedia", 0, 0.5f, 0, 0.08f, 1.0f, 0.08f);
            B(root, "Juguete2", 0, 1.08f, 0, 0.28f, 0.24f, 0.45f);
            B(root, "Collar", 0.16f, 1.2f, -0.1f, 0.02f, 0.18f, 0.06f);
            Save(root, "Exterior");
        }

        static void BuildExit(string name, bool mansion)
        {
            var root = R(name);
            // Puerta visible por ambos lados (la cámara suele verla desde fuera).
            B(root, "MaderaOscura", 0, 0.8f, 0, 1.75f, 1.6f, 0.22f);
            B(root, "Marco", 0, 1.62f, 0, 1.79f, 0.08f, 0.26f);
            foreach (float side in new[] { -1f, 1f })
            {
                float z = side * 0.13f;
                if (!mansion)
                {
                    B(root, "Marco", 0, 0.8f, z, 1.15f, 1.55f, 0.03f);
                    B(root, "Puerta", 0, 0.78f, z + side * 0.01f, 1.0f, 1.45f, 0.03f);
                    B(root, "MaderaOscura", 0, 0.26f, z + side * 0.025f, 0.52f, 0.44f, 0.02f);
                    B(root, "Negro", 0, 0.26f, z + side * 0.03f, 0.44f, 0.36f, 0.01f);
                    B(root, "MaderaClara", 0, 0.27f, z + side * 0.05f, 0.4f, 0.32f, 0.02f, side * 20f, 0, 0);
                    S(root, "Dorado", 0.38f, 0.85f, z + side * 0.03f, 0.07f, 0.07f, 0.05f);
                }
                else
                {
                    foreach (float s in new[] { -1f, 1f })
                    {
                        B(root, "Puerta", s * 0.38f, 0.75f, z + side * 0.01f, 0.72f, 1.5f, 0.03f);
                        B(root, "Dorado", s * 0.38f, 0.75f, z + side * 0.025f, 0.5f, 1.2f, 0.01f);
                        B(root, "MaderaOscura", s * 0.38f, 0.75f, z + side * 0.03f, 0.42f, 1.1f, 0.01f);
                        S(root, "Dorado", s * 0.08f, 0.8f, z + side * 0.04f, 0.08f, 0.08f, 0.06f);
                    }
                }
                B(root, "SalidaVerde", 0, 1.45f, z + side * 0.03f, 0.7f, 0.14f, 0.02f);
            }
            Col(root, 0, 0.8f, -0.05f, 1.75f, 1.6f, 0.36f);
            // Bloqueo para no salir de la casa.
            Col(root, 0, 0.8f, -0.6f, 2.2f, 1.6f, 0.4f);
            var zone = Pv(root, "ZonaSalida", 0, 0, 0.8f);
            var ez = zone.gameObject.AddComponent<ExitZone>();
            ez.exitName = mansion ? "la puerta principal" : "la gatera";
            var marker = P(zone, MeshLib.Cylinder, MatLib.Vision, 0, 0.02f, 0, 2.6f, 0.01f, 2.6f, 0, 0, 0);
            marker.name = "Marcador";
            var mr = marker.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ez.marker = mr;
            ez.exitLight = AddLight(zone, LightType.Point, new Color(1f, 0.8f, 0.3f), 1.6f, 3.5f, 0, 1.2f, 0.2f);
            Save(root, "Casa");
        }

        static void BuildSecurityCamera()
        {
            var root = R("CamaraSeguridad");
            B(root, "Sensor", 0, 0, 0.04f, 0.16f, 0.16f, 0.08f);
            Cy(root, "Sensor", 0, 0, 0.12f, 0.04f, 0.14f, 0.04f, 90, 0, 0);
            var head = Pv(root, "Cabezal", 0, -0.03f, 0.2f);
            B(head, "CamaraSeg", 0, 0, 0.06f, 0.2f, 0.17f, 0.34f);
            B(head, "CamaraSeg", 0, 0.1f, 0.1f, 0.24f, 0.02f, 0.42f);
            Cy(head, "Negro", 0, 0, 0.24f, 0.12f, 0.04f, 0.12f, 90, 0, 0);
            Cy(head, "Lente", 0, 0, 0.26f, 0.08f, 0.02f, 0.08f, 90, 0, 0);
            var led = S(head, "LedRojo", 0.07f, 0.06f, 0.23f, 0.04f, 0.04f, 0.04f);
            var cam = root.gameObject.AddComponent<SecurityCamera>();
            cam.head = head;
            cam.led = led.GetComponent<MeshRenderer>();
            Save(root, "Seguridad");
        }

        static void BuildMotionSensor()
        {
            var root = R("SensorMovimiento");
            var zone = P(root, MeshLib.Cube, MatLib.Vision, 0, 0.025f, 0, 3f, 0.01f, 2f, 0, 0, 0);
            zone.name = "Zona";
            zone.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObject led = null;
            foreach (float s in new[] { -1f, 1f })
            {
                var post = Pv(root, s < 0 ? "PosteIzq" : "PosteDer", s * 1.5f, 0, 0);
                Cy(post, "Sensor", 0, 0.2f, 0, 0.1f, 0.4f, 0.1f);
                var l = S(post, "LedRojo", 0, 0.43f, 0, 0.08f, 0.08f, 0.08f);
                if (led == null) led = l;
            }
            var laser = B(root, "Laser", 0, 0.3f, 0, 3f, 0.015f, 0.015f);
            laser.name = "Laser";
            laser.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var ms = root.gameObject.AddComponent<MotionSensor>();
            ms.zone = zone.GetComponent<MeshRenderer>();
            ms.led = led.GetComponent<MeshRenderer>();
            Save(root, "Seguridad");
        }

        // ------------------------------------------------------------------ botín

        static Transform LootRoot(string name, int value, int weight, float noise, float height, string funny)
        {
            var root = R(name);
            var l = root.gameObject.AddComponent<Lootable>();
            l.itemName = name switch
            {
                "PapasFritas" => "Bolsa de papas fritas",
                "PavoAsado" => "Pavo asado",
                "Telefono" => "Teléfono",
                "Camara" => "Cámara de fotos",
                _ => name
            };
            l.value = value; l.weight = weight; l.pickupNoise = noise; l.funnyLine = funny;
            var glow = P(root, MeshLib.Cylinder, MatLib.Glow, 0, 0.012f, 0, 0.55f + weight * 0.07f, 0.005f, 0.55f + weight * 0.07f, 0, 0, 0);
            glow.name = "Brillo";
            glow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.glow = glow;
            var sp = Pv(root, "Destello", 0, height + 0.3f, 0);
            Oc(sp, "Brillo", 0, 0, 0, 0.1f, 0.14f, 0.1f);
            l.sparkle = sp;
            return root;
        }

        static void BuildLoot()
        {
            var r = LootRoot("Galletas", 5, 1, 2f, 0.16f, "Ñam... el mapache se come una galleta por el camino");
            for (int i = 0; i < 3; i++)
            {
                Cy(r, "Galleta", 0.01f * i, 0.03f + i * 0.05f, -0.01f * i, 0.24f, 0.045f, 0.24f);
                S(r, "Chocolate", 0.05f, 0.055f + i * 0.05f, 0.04f, 0.04f, 0.02f, 0.04f);
                S(r, "Chocolate", -0.06f, 0.055f + i * 0.05f, -0.02f, 0.035f, 0.02f, 0.035f);
            }
            Save(r, "Botin");

            r = LootRoot("Manzana", 8, 1, 2f, 0.24f, "Una manzana brillante. ¡Crunch!");
            S(r, "ManzanaRoja", 0, 0.11f, 0, 0.22f, 0.2f, 0.22f);
            Cy(r, "Tronco", 0, 0.23f, 0, 0.02f, 0.06f, 0.02f);
            S(r, "Hoja", 0.045f, 0.245f, 0, 0.09f, 0.02f, 0.05f, 0, 0, 20);
            Save(r, "Botin");

            r = LootRoot("Queso", 10, 2, 4f, 0.16f, "El mapache se pone el queso de sombrero. Muy elegante.");
            We(r, "Queso", 0, 0.08f, 0, 0.36f, 0.16f, 0.32f);
            S(r, "QuesoAgujero", 0.06f, 0.161f, -0.05f, 0.06f, 0.01f, 0.06f);
            S(r, "QuesoAgujero", -0.05f, 0.161f, 0.02f, 0.05f, 0.01f, 0.05f);
            S(r, "QuesoAgujero", 0.05f, 0.08f, -0.145f, 0.05f, 0.05f, 0.01f);
            Save(r, "Botin");

            r = LootRoot("Pastel", 20, 3, 6f, 0.3f, "¡Un pastel entero! Lo lleva por encima de la cabeza como un trofeo");
            Cy(r, "Plato", 0, 0.01f, 0, 0.48f, 0.02f, 0.48f);
            Cy(r, "PastelRosa", 0, 0.11f, 0, 0.38f, 0.18f, 0.38f);
            Cy(r, "Nata", 0, 0.215f, 0, 0.4f, 0.04f, 0.4f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                S(r, "Nata", Mathf.Cos(a) * 0.15f, 0.245f, Mathf.Sin(a) * 0.15f, 0.06f, 0.05f, 0.06f);
            }
            S(r, "Cereza", 0, 0.27f, 0, 0.07f, 0.07f, 0.07f);
            Save(r, "Botin");

            r = LootRoot("PapasFritas", 12, 1, 10f, 0.34f, "¡CRUNCH CRUNCH! Las papas fritas hacen muchísimo ruido");
            B(r, "BolsaPapas", 0, 0.16f, 0, 0.24f, 0.3f, 0.09f);
            B(r, "BolsaPapas", 0, 0.32f, 0, 0.25f, 0.03f, 0.05f);
            B(r, "FranjaRoja", 0, 0.17f, 0.046f, 0.2f, 0.1f, 0.005f);
            S(r, "Queso", 0, 0.18f, 0.05f, 0.08f, 0.05f, 0.01f);
            Save(r, "Botin");

            r = LootRoot("PavoAsado", 35, 4, 12f, 0.3f, "El pavo es más grande que el mapache. Corre como puede con él a cuestas");
            Cy(r, "Plato", 0, 0.015f, 0, 0.62f, 0.03f, 0.62f);
            S(r, "Pavo", 0, 0.15f, 0, 0.44f, 0.26f, 0.34f);
            foreach (float s in new[] { -1f, 1f })
            {
                Cy(r, "Pavo", s * 0.16f, 0.2f, 0.12f, 0.09f, 0.2f, 0.09f, -55, 0, s * 20f);
                S(r, "Hueso", s * 0.2f, 0.28f, 0.2f, 0.07f, 0.07f, 0.07f);
            }
            S(r, "Hoja", 0.18f, 0.04f, -0.2f, 0.12f, 0.03f, 0.08f);
            Save(r, "Botin");

            r = LootRoot("Juguete", 8, 1, 3f, 0.24f, "Un patito de goma. ¡Cuac! (muy bajito)");
            S(r, "Pato", 0, 0.08f, 0, 0.2f, 0.15f, 0.25f);
            S(r, "Pato", 0, 0.18f, 0.07f, 0.13f, 0.13f, 0.13f);
            Co(r, "Pico", 0, 0.17f, 0.15f, 0.06f, 0.08f, 0.04f, 90, 0, 0);
            foreach (float s in new[] { -1f, 1f }) S(r, "Negro", s * 0.04f, 0.21f, 0.12f, 0.025f, 0.025f, 0.02f);
            Save(r, "Botin");

            r = LootRoot("Reloj", 15, 1, 5f, 0.28f, "Tic, tac... el mapache se pone el despertador de sombrero");
            Cy(r, "Reloj", 0, 0.13f, 0, 0.22f, 0.08f, 0.22f, 90, 0, 0);
            Cy(r, "Blanco", 0, 0.13f, 0.042f, 0.17f, 0.01f, 0.17f, 90, 0, 0);
            B(r, "Negro", 0, 0.16f, 0.05f, 0.012f, 0.06f, 0.005f);
            B(r, "Negro", 0.02f, 0.13f, 0.05f, 0.04f, 0.012f, 0.005f);
            foreach (float s in new[] { -1f, 1f })
            {
                S(r, "Dorado", s * 0.07f, 0.24f, 0, 0.08f, 0.06f, 0.08f);
                Cy(r, "Negro", s * 0.06f, 0.02f, 0, 0.03f, 0.04f, 0.03f);
            }
            Save(r, "Botin");

            r = LootRoot("Telefono", 25, 1, 8f, 0.06f, "Un teléfono carísimo... ¡con 47 fotos de gatos!");
            B(r, "Telefono", 0, 0.015f, 0, 0.17f, 0.03f, 0.32f);
            B(r, "PantallaMovil", 0, 0.031f, 0, 0.15f, 0.004f, 0.27f);
            Save(r, "Botin");

            r = LootRoot("Camara", 30, 2, 8f, 0.2f, "Clic! El mapache se hace un selfie sin querer");
            B(r, "CamaraFoto", 0, 0.09f, 0, 0.26f, 0.16f, 0.13f);
            Cy(r, "Lente", 0, 0.09f, 0.09f, 0.11f, 0.09f, 0.11f, 90, 0, 0);
            Cy(r, "Cristal", 0, 0.09f, 0.135f, 0.08f, 0.01f, 0.08f, 90, 0, 0);
            B(r, "CamaraFoto", -0.07f, 0.185f, 0, 0.07f, 0.04f, 0.06f);
            S(r, "LedRojo", 0.08f, 0.175f, 0.02f, 0.035f, 0.02f, 0.035f);
            Save(r, "Botin");

            r = LootRoot("Joyero", 40, 3, 18f, 0.26f, "¡Brilla muchísimo! Pesa, hace ruido... pero vale una fortuna");
            B(r, "Joyero", 0, 0.08f, 0, 0.32f, 0.16f, 0.24f);
            B(r, "Dorado", 0, 0.08f, 0.121f, 0.1f, 0.06f, 0.01f);
            B(r, "Dorado", 0, 0.2f, -0.08f, 0.34f, 0.04f, 0.26f, -35, 0, 0);
            Oc(r, "Gema", -0.05f, 0.18f, 0.02f, 0.07f, 0.07f, 0.07f);
            Oc(r, "Gema", 0.06f, 0.18f, -0.02f, 0.06f, 0.06f, 0.06f);
            for (int i = 0; i < 5; i++) S(r, "Perla", -0.12f + i * 0.06f, 0.17f, 0.08f, 0.035f, 0.035f, 0.035f);
            Save(r, "Botin");
        }
    }
}
