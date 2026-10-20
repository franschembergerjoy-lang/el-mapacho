using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RobaYCorre.EditorTools
{
    /// <summary>
    /// Genera todo el contenido del juego (mallas, materiales, prefabs, escenas y Build Settings).
    /// Menú: Roba y corre → Reconstruir todo el juego.
    /// </summary>
    public static class ProjectBuilder
    {
        public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Roba y corre/Reconstruir todo el juego")]
        public static void BuildAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            MeshLib.Build();
            MatLib.Build();
            ModelLibrary.BuildAll();
            var scenes = new List<string> { BuildMenuScene() };
            foreach (var def in LevelLayouts.All())
            {
                try
                {
                    scenes.Add(LevelBuilder.Build(def));
                }
                catch (Exception e)
                {
                    // Un error en un nivel no impide generar los demás.
                    Debug.LogError($"Error generando el nivel {def.number}: {e}");
                }
            }
            var list = new List<EditorBuildSettingsScene>();
            foreach (var s in scenes) list.Add(new EditorBuildSettingsScene(s, true));
            EditorBuildSettings.scenes = list.ToArray();
            PlayerSettings.productName = "Roba y corre";
            PlayerSettings.companyName = "El Mapache Loco";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MenuScenePath);
            Debug.Log($"[Roba y corre] Generadas {scenes.Count} escenas.");
        }

        /// <summary>Para línea de comandos: -executeMethod RobaYCorre.EditorTools.ProjectBuilder.BuildAllBatch</summary>
        public static void BuildAllBatch()
        {
            try
            {
                BuildAll();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static string BuildMenuScene()
        {
            EnsureFolder("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LevelBuilder.SetupLighting();

            // Pequeño decorado: el mapache con su botín en un salón de noche.
            var deco = new GameObject("Decorado").transform;
            var floor = new GameObject("Suelo");
            floor.transform.SetParent(deco, false);
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(14f, 0.1f, 10f);
            floor.AddComponent<MeshFilter>().sharedMesh = MeshLib.Cube;
            floor.AddComponent<MeshRenderer>().sharedMaterial = MatLib.Floor("Madera", MatLib.FloorStyle.Wood, new Color(0.82f, 0.6f, 0.38f), new Color(0.55f, 0.37f, 0.22f));
            var wall = new GameObject("Pared");
            wall.transform.SetParent(deco, false);
            wall.transform.position = new Vector3(0f, 0.8f, 3.2f);
            wall.transform.localScale = Vector3.one;
            wall.AddComponent<MeshFilter>().sharedMesh = MeshLib.BoxUV(new Vector3(14f, 1.6f, 0.35f), 1f);
            wall.AddComponent<MeshRenderer>().sharedMaterial = MatLib.Wall("ParedNaranja");

            GameObject Put(string prefab, float x, float z, float rot, float y = 0f)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(ModelLibrary.Get(prefab));
                go.transform.SetParent(deco, false);
                go.transform.position = new Vector3(x, y, z);
                go.transform.rotation = Quaternion.Euler(0f, rot, 0f);
                return go;
            }
            Put("Sofa", 3.4f, 2.4f, 180f);
            Put("Lampara", 5.2f, 2.6f, 0f);
            Put("Alfombra", 2.4f, 0.6f, 0f);
            Put("Cuadro", 3.4f, 3.02f, 180f);
            var raccoon = Put("Mapache", 2.2f, 0.4f, 160f);
            raccoon.transform.localScale = Vector3.one * 1.6f;
            var spin = new List<Transform>
            {
                Put("PavoAsado", 3.5f, 0.5f, 0f).transform,
                Put("Joyero", 1.1f, 1.0f, 0f).transform,
                Put("Queso", 3.0f, -0.6f, 0f).transform,
                Put("Galletas", 1.4f, -0.5f, 0f).transform,
            };

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.16f);
            camGo.AddComponent<AudioListener>();
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            camGo.transform.position = new Vector3(0.6f, 4.6f, -6.2f);
            camGo.transform.rotation = Quaternion.Euler(34f, 0f, 0f);

            var menu = new GameObject("MenuPrincipal").AddComponent<MainMenu>();
            menu.displayRaccoon = raccoon.transform;
            menu.spinningLoot = spin.ToArray();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            return MenuScenePath;
        }

        [MenuItem("Roba y corre/Compilar ejecutable para Windows")]
        public static void BuildWindows()
        {
            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            string output = "Build/RobaYCorre.exe";
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-salida") output = args[i + 1];
            var report = BuildPipeline.BuildPlayer(scenes.ToArray(), output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log("[Roba y corre] Resultado de la compilación: " + report.summary.result + " (" + report.summary.totalErrors + " errores)");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("La compilación falló");
        }

        public static void BuildWindowsBatch()
        {
            try
            {
                BuildWindows();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // ------------------------------------------------------------------ capturas

        [MenuItem("Roba y corre/Capturas de pantalla de las escenas")]
        public static void Screenshots()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas"));
            Directory.CreateDirectory(dir);
            foreach (var s in EditorBuildSettings.scenes)
            {
                EditorSceneManager.OpenScene(s.path);
                var cam = Camera.main;
                if (cam == null) continue;
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 4;
                cam.targetTexture = rt;
                cam.Render();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, Path.GetFileNameWithoutExtension(s.path) + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            Debug.Log("[Roba y corre] Capturas guardadas en " + dir);
        }

        public static void ScreenshotsBatch()
        {
            try
            {
                Screenshots();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Capturas cenitales de cada nivel completo (para revisar el diseño).</summary>
        public static void OverviewBatch()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas"));
                Directory.CreateDirectory(dir);
                foreach (var s in EditorBuildSettings.scenes)
                {
                    if (s.path == MenuScenePath) continue;
                    EditorSceneManager.OpenScene(s.path);
                    var lm = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                    var cam = Camera.main;
                    if (lm == null || cam == null) continue;
                    cam.transform.position = lm.navCenter + new Vector3(0f, 32f, -16f);
                    cam.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
                    cam.fieldOfView = 50f;
                    var rt = new RenderTexture(1600, 1200, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(1600, 1200, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1600, 1200), 0, 0);
                    tex.Apply();
                    cam.targetTexture = null;
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, "Vista_" + Path.GetFileNameWithoutExtension(s.path) + ".png"), tex.EncodeToPNG());
                }
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
