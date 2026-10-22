using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobaYCorre
{
    /// <summary>
    /// Herramienta de verificación: si el juego se lanza con el argumento "-autocaptura", recorre el menú y
    /// algunos niveles, guarda capturas en la carpeta "Capturas" junto al ejecutable y se cierra.
    /// En una partida normal no hace nada.
    /// </summary>
    public class AutoCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-recorrido" && i + 1 < args.Length) int.TryParse(args[i + 1], out onlyLevel);
            }
            foreach (var a in args)
            {
                if (a == "-autocaptura" || a == "-recorrido")
                {
                    var go = new GameObject("AutoCapture");
                    DontDestroyOnLoad(go);
                    go.AddComponent<AutoCapture>();
                    return;
                }
            }
        }

        string dir;
        static int onlyLevel;

        IEnumerator Start()
        {
            dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Capturas");
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(2f);
            if (onlyLevel > 0)
            {
                yield return Tour(onlyLevel);
                Application.Quit();
                yield break;
            }
            yield return Shot("juego_menu");
            GameProgress.OpenLevelSelect = true;
            SceneManager.LoadScene(GameProgress.MenuScene);
            yield return new WaitForSeconds(2f);
            yield return Shot("juego_menu_niveles");

            GameProgress.LoadLevel(1);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("juego_n1_intro");
            LevelManager.I.BeginPlay();
            var p = LevelManager.I.player;
            var cheese = Lootable.All.Find(l => l.itemName == "Queso");
            Teleport(p, cheese.transform.position + new Vector3(-0.2f, 0f, -0.9f));
            yield return new WaitForSeconds(1.2f);
            yield return Shot("juego_n1_cerca_del_queso");
            p.TryPickup(cheese, LevelManager.I);
            var cake = Lootable.All.Find(l => l.itemName == "Pastel");
            p.TryPickup(cake, LevelManager.I);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("juego_n1_cargando_botin");
            Teleport(p, LevelManager.I.exit.transform.position);
            yield return new WaitForSeconds(1f);
            yield return Shot("juego_n1_salida");
            LevelManager.I.Escape();
            yield return new WaitForSeconds(2.5f);
            yield return Shot("juego_n1_victoria");

            GameProgress.LoadLevel(3);
            yield return new WaitForSeconds(1.2f);
            LevelManager.I.BeginPlay();
            var dog = FindAnyObjectByType<Dog>();
            Teleport(LevelManager.I.player, dog.transform.position + dog.transform.forward * 3.5f);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("juego_n3_perro");

            GameProgress.LoadLevel(5);
            yield return new WaitForSeconds(1.2f);
            LevelManager.I.BeginPlay();
            var jewel = Lootable.All.Find(l => l.isSpecial);
            Teleport(LevelManager.I.player, jewel.transform.position + new Vector3(1.5f, 0f, 0.6f));
            yield return new WaitForSeconds(1.5f);
            yield return Shot("juego_n5_galeria");
            LevelManager.I.player.EnterHiding(HidingSpot.All.Find(h => h.spotName.Contains("armario") && h.transform.position.x > 20f), LevelManager.I);
            yield return new WaitForSeconds(1f);
            yield return Shot("juego_n5_escondido");
            LevelManager.I.Pause();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("juego_n5_pausa");
            LevelManager.I.Resume();

            // Recorrido de revisión gráfica por todas las casas (los vigilantes se desactivan).
            for (int lvl = 1; lvl <= GameProgress.LevelCount; lvl++) yield return Tour(lvl);
            Application.Quit();
        }

        IEnumerator Tour(int lvl)
        {
            {
                GameProgress.LoadLevel(lvl);
                yield return new WaitForSeconds(1.2f);
                LevelManager.I.BeginPlay();
                foreach (var r in FindObjectsByType<Resident>()) r.enabled = false;
                foreach (var d in FindObjectsByType<Dog>()) d.enabled = false;
                foreach (var c in FindObjectsByType<SecurityCamera>()) c.enabled = false;
                foreach (var m in FindObjectsByType<MotionSensor>()) m.enabled = false;
                var loot = Lootable.All.FindAll(l => !l.IsCarried);
                loot.Sort((a, b) => (a.transform.position.x + a.transform.position.z * 1.7f).CompareTo(b.transform.position.x + b.transform.position.z * 1.7f));
                var pl = LevelManager.I.player;
                for (int k = 0; k < 4; k++)
                {
                    if (k > 0)
                    {
                        var l = loot[Mathf.Min(loot.Count - 1, (k * loot.Count) / 4)];
                        Teleport(pl, l.transform.position + new Vector3(0f, 0f, -0.9f));
                    }
                    yield return new WaitForSeconds(1.3f);
                    yield return Shot($"recorrido_n{lvl}_{k}");
                }
            }
        }

        static void Teleport(RaccoonController p, Vector3 pos)
        {
            var cc = p.GetComponent<CharacterController>();
            cc.enabled = false;
            p.transform.position = new Vector3(pos.x, 0.02f, pos.z);
            cc.enabled = true;
        }

        void Update()
        {
            // Durante la captura automática se ignoran clics del ratón (por si alguien toca la ventana).
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.enabled) es.enabled = false;
        }

        IEnumerator Shot(string name)
        {
            Debug.Log("[AutoCaptura] " + name);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }
}
