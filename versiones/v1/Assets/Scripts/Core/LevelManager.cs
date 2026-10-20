using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RobaYCorre
{
    public enum LevelState { Intro, Playing, Paused, Won, Lost }

    /// <summary>Reglas de un nivel: objetivo, guardar botín, escapar, perder, pausa y estrellas.</summary>
    [DefaultExecutionOrder(-100)]
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager I { get; private set; }

        [Header("Nivel")]
        public int levelNumber = 1;
        public string levelTitle = "La casa tranquila";
        [TextArea(2, 5)] public string introText;
        [Tooltip("Monedas que hay que guardar en la salida")]
        public int lootGoal = 25;
        public bool requiresSpecial;
        public string specialItemName = "Joyero";
        [Tooltip("Muestra consejos paso a paso (nivel 1)")]
        public bool showTutorial;

        [Header("Estrellas")]
        [Tooltip("2ª estrella: botín >= objetivo x este factor")]
        public float bonusLootFactor = 1.4f;

        [Header("Navegación (se genera al empezar)")]
        public Vector3 navCenter = new Vector3(10f, 0f, 8f);
        public Vector3 navSize = new Vector3(30f, 4f, 24f);

        [Header("Referencias")]
        public RaccoonController player;
        public ExitZone exit;

        public LevelState State { get; private set; } = LevelState.Intro;
        public int Banked { get; private set; }
        public bool SpecialBanked { get; private set; }
        public float Elapsed { get; private set; }
        public bool EverSpotted { get; private set; }
        public string CaughtBy { get; private set; }
        public HUD Hud { get; private set; }

        public bool GoalMet => Banked >= lootGoal && (!requiresSpecial || SpecialBanked);
        public int TotalWithBackpack => Banked + (player != null ? player.CarriedValue : 0);

        float lastAlarm = -100f;
        NavMeshDataInstance navInstance;
        int tutorialStep;
        bool tutorialPicked, tutorialDeposited;

        public static void Toast(string msg, Color color)
        {
            if (I != null && I.Hud != null) I.Hud.Toast(msg, color);
        }

        void Awake()
        {
            I = this;
            Time.timeScale = 1f;
            if (GetComponent<NoiseSystem>() == null) gameObject.AddComponent<NoiseSystem>();
            if (player == null) player = FindAnyObjectByType<RaccoonController>();
            if (exit == null) exit = FindAnyObjectByType<ExitZone>();
            BuildNavMesh();
            var _ = AudioManager.I;
        }

        void Start()
        {
            try
            {
                Hud = HUD.Create(this);
                var noise = NoiseSystem.I;
                if (noise != null)
                {
                    noise.OnBurst += (amount, pos) => { if (amount >= noise.burstMessageThreshold) Toast("¡Hiciste ruido!", HUD.Warn); };
                    noise.OnThresholdCrossed += mark =>
                    {
                        if (mark == 60) Toast("¡Mucho ruido! Te van a buscar", HUD.Bad);
                        else if (mark == 100) Toast("¡RUIDO MÁXIMO!", HUD.Bad);
                    };
                }
                Hud.ShowIntro();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                State = LevelState.Playing;
            }
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            if (navInstance.valid) NavMesh.RemoveNavMeshData(navInstance);
        }

        void BuildNavMesh()
        {
            try
            {
                var settings = NavMesh.GetSettingsByID(0);
                settings.agentRadius = 0.28f;
                settings.agentHeight = 1.7f;
                settings.agentClimb = 0.15f;
                settings.agentSlope = 30f;
                var bounds = new Bounds(navCenter, navSize);
                var sources = new List<NavMeshBuildSource>();
                int mask = ~((1 << Layers.Player) | (1 << Layers.IgnoreRaycast));
                NavMeshBuilder.CollectSources(bounds, mask, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
                // Solo el suelo es caminable; muebles y paredes son obstáculos (nada de caminar sobre camas o mesas).
                for (int i = 0; i < sources.Count; i++)
                {
                    var s = sources[i];
                    if (s.component == null || s.component.gameObject.name != "Suelo")
                    {
                        s.area = 1;
                        sources[i] = s;
                    }
                }
                var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
                if (data != null) navInstance = NavMesh.AddNavMeshData(data);
            }
            catch (Exception e)
            {
                Debug.LogError("No se pudo generar la navegación: " + e.Message);
            }
        }

        public void BeginPlay()
        {
            if (State != LevelState.Intro) return;
            State = LevelState.Playing;
            Hud?.HideIntro();
            AudioManager.Play(Sfx.Click);
            if (showTutorial) Tutorial(0);
        }

        void Update()
        {
            if (State == LevelState.Intro && GameInput.ConfirmDown) BeginPlay();
            if (GameInput.PauseDown)
            {
                if (State == LevelState.Playing) Pause();
                else if (State == LevelState.Paused) Resume();
            }
            if (State == LevelState.Playing)
            {
                Elapsed += Time.deltaTime;
                if (showTutorial) UpdateTutorial();
            }
        }

        public void SetPrompt(string text) { Hud?.SetPrompt(text); }

        public void Pause()
        {
            if (State != LevelState.Playing) return;
            State = LevelState.Paused;
            Time.timeScale = 0f;
            Hud?.ShowPause(true);
        }

        public void Resume()
        {
            if (State != LevelState.Paused) return;
            State = LevelState.Playing;
            Time.timeScale = 1f;
            Hud?.ShowPause(false);
        }

        public void Restart() => GameProgress.LoadLevel(levelNumber);
        public void GoToMenu() => GameProgress.LoadMenu(true);
        public void NextLevel()
        {
            if (levelNumber < GameProgress.LevelCount) GameProgress.LoadLevel(levelNumber + 1);
            else GameProgress.LoadMenu(false);
        }

        /// <summary>El mapache llegó a la salida: guarda lo que lleve.</summary>
        public void OnPlayerAtExit()
        {
            if (State != LevelState.Playing || player == null) return;
            if (player.CarriedValue > 0 || player.carried.Count > 0)
            {
                bool wasMet = GoalMet;
                int v = player.DepositAll(out bool special);
                Banked += v;
                if (special) SpecialBanked = true;
                AudioManager.Play(Sfx.Deposit);
                Toast($"Guardaste {v} monedas en el escondite del botín", HUD.Good);
                if (!wasMet && GoalMet) Toast("¡Objetivo cumplido! Pulsa E para escapar o sigue robando", HUD.Good);
                tutorialDeposited = true;
            }
            else if (!GoalMet)
            {
                Toast($"Necesitas {lootGoal} monedas{(requiresSpecial && !SpecialBanked ? " y el " + specialItemName : "")} para escapar", HUD.Info);
            }
        }

        public void OnLootPicked(Lootable l)
        {
            tutorialPicked = true;
            if (l.isSpecial) Toast($"¡Tienes el {l.itemName}! Llévalo a la salida", HUD.Good);
        }

        public void OnPlayerHid() { }

        public void Escape()
        {
            if (State != LevelState.Playing || !GoalMet) return;
            State = LevelState.Won;
            int stars = 1;
            if (Banked >= Mathf.CeilToInt(lootGoal * bonusLootFactor)) stars++;
            if (!EverSpotted) stars++;
            GameProgress.SetStars(levelNumber, stars);
            GameProgress.SetBestLoot(levelNumber, Banked);
            GameProgress.Unlock(levelNumber + 1);
            AudioManager.Play(Sfx.Escape);
            Toast("¡Escapaste!", HUD.Good);
            Hud?.ShowWin(stars);
        }

        public void PlayerCaught(string by)
        {
            if (State != LevelState.Playing) return;
            State = LevelState.Lost;
            CaughtBy = by;
            player?.OnCaught();
            AudioManager.Play(Sfx.Caught);
            StartCoroutine(ShowLose());
        }

        IEnumerator ShowLose()
        {
            yield return new WaitForSeconds(1.4f);
            Hud?.ShowLose();
        }

        public void NotifySpotted(string who)
        {
            EverSpotted = true;
            AudioManager.Play(Sfx.Spotted);
            Toast($"¡Te vio {who}!", HUD.Bad);
            Hud?.Flash(new Color(1f, 0.1f, 0.1f, 0.25f));
        }

        public void TriggerAlarm(Vector3 pos, string message)
        {
            if (Time.time - lastAlarm < 1f) return;
            lastAlarm = Time.time;
            NoiseSystem.I?.ForceMax(pos);
            AudioManager.Play(Sfx.Alarm);
            Toast(message, HUD.Bad);
            Hud?.Flash(new Color(1f, 0f, 0f, 0.35f));
            foreach (var w in Perception.Watchers)
                if (w is Resident r) r.Alert(pos, true);
        }

        // ---------------- Tutorial del nivel 1 ----------------
        static readonly string[] tutorialTexts =
        {
            "Muévete con WASD o las flechas. La cocina tiene comida: acércate y pulsa E para robar.",
            "¡Bien! Correr (Shift) hace mucho ruido. Agáchate con Ctrl para moverte en silencio.",
            "Lleva el botín a la gatera (la luz amarilla) para guardarlo. Puedes hacer varios viajes.",
            "Si alguien se acerca, escóndete con E: debajo de la cama, en el armario, tras la cortina...",
            "Cuando tengas el objetivo, pulsa E en la gatera para escapar. ¡Suerte, mapache!"
        };

        void Tutorial(int step)
        {
            tutorialStep = step;
            if (step < tutorialTexts.Length) Hud?.SetTutorial(tutorialTexts[step]);
            else Hud?.SetTutorial(null);
        }

        float tutorialTimer;
        void UpdateTutorial()
        {
            tutorialTimer += Time.deltaTime;
            switch (tutorialStep)
            {
                case 0: if (tutorialPicked) { Tutorial(1); tutorialTimer = 0f; } break;
                case 1: if (tutorialTimer > 7f) { Tutorial(2); tutorialTimer = 0f; } break;
                case 2: if (tutorialDeposited) { Tutorial(3); tutorialTimer = 0f; } break;
                case 3: if (tutorialTimer > 8f || GoalMet) { Tutorial(4); tutorialTimer = 0f; } break;
                case 4: if (tutorialTimer > 10f) Tutorial(5); break;
            }
        }
    }
}
