using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RobaYCorre
{
    /// <summary>Interfaz durante el juego: botín, objetivo, mochila, barra de ruido, avisos y pantallas.</summary>
    public class HUD : MonoBehaviour
    {
        public static readonly Color Good = new Color(0.45f, 1f, 0.5f);
        public static readonly Color Warn = new Color(1f, 0.82f, 0.3f);
        public static readonly Color Bad = new Color(1f, 0.4f, 0.35f);
        public static readonly Color Info = new Color(0.85f, 0.9f, 1f);

        public const string ControlsText =
            "<b>WASD / Flechas</b>  moverse\n" +
            "<b>Shift</b>  correr (¡hace ruido!)\n" +
            "<b>Ctrl</b> o <b>C</b>  agacharse (silencioso)\n" +
            "<b>E</b>  robar · esconderse · escapar\n" +
            "<b>Q</b>  soltar el último objeto\n" +
            "<b>Esc</b>  pausa";

        LevelManager lm;
        Canvas canvas;
        RectTransform canvasRt;

        Text levelText, bankedText, carriedText, backpackText, specialText, timeText, noiseStatus, promptText, tutorialText, hiddenText;
        RectTransform backpackFill, noiseFill;
        Image backpackFillImg, noiseFillImg, flashImg, hiddenOverlay;
        GameObject promptPanel, tutorialPanel, introPanel, pausePanel, losePanel, winPanel;
        RectTransform toastRoot;
        Color flashColor;
        float flashT;
        readonly List<(GameObject go, float life)> toasts = new List<(GameObject, float)>();
        readonly List<Text> iconPool = new List<Text>();

        public static HUD Create(LevelManager lm)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUD>();
            hud.lm = lm;
            hud.Build();
            return hud;
        }

        void Build()
        {
            canvas = UIFactory.CreateCanvas("HUD_Canvas", 10);
            canvas.transform.SetParent(transform, false);
            canvasRt = canvas.GetComponent<RectTransform>();
            var root = canvas.transform;
            Vector2 TL = new Vector2(0f, 1f), TR = new Vector2(1f, 1f), BC = new Vector2(0.5f, 0f), TC = new Vector2(0.5f, 1f), C = new Vector2(0.5f, 0.5f), BL = Vector2.zero;

            // Iconos sobre los personajes (debajo del resto).
            var iconRoot = new GameObject("Iconos");
            UIFactory.Stretch(iconRoot, root);

            // Destello rojo al ser visto.
            flashImg = UIFactory.Image(root, "Destello", new Color(1, 0, 0, 0));
            UIFactory.Stretch(flashImg.gameObject, root);

            hiddenOverlay = UIFactory.Image(root, "Escondido", new Color(0.02f, 0.02f, 0.08f, 0.35f));
            UIFactory.Stretch(hiddenOverlay.gameObject, root);
            hiddenOverlay.gameObject.SetActive(false);

            // Panel de información (arriba a la izquierda).
            var info = UIFactory.Panel(root, "Info", UIFactory.PanelColor, TL, new Vector2(24f, -24f), new Vector2(520f, lm.requiresSpecial ? 250f : 214f));
            info.rectTransform.pivot = TL;
            info.rectTransform.anchoredPosition = new Vector2(24f, -24f);
            var it = info.transform;
            levelText = UIFactory.Label(it, $"NIVEL {lm.levelNumber} · {lm.levelTitle}", 24, UIFactory.Gold, TextAnchor.UpperLeft, TL, new Vector2(20f, -14f), new Vector2(500f, 34f), FontStyle.Bold);
            bankedText = UIFactory.Label(it, "", 32, Color.white, TextAnchor.UpperLeft, TL, new Vector2(20f, -50f), new Vector2(500f, 40f), FontStyle.Bold);
            carriedText = UIFactory.Label(it, "", 22, UIFactory.TextLight, TextAnchor.UpperLeft, TL, new Vector2(20f, -98f), new Vector2(500f, 30f));
            backpackText = UIFactory.Label(it, "", 22, UIFactory.TextLight, TextAnchor.UpperLeft, TL, new Vector2(20f, -132f), new Vector2(500f, 30f));
            var bbg = UIFactory.Panel(it, "MochilaFondo", new Color(1, 1, 1, 0.15f), TL, new Vector2(20f, -176f), new Vector2(480f, 22f));
            bbg.rectTransform.pivot = TL; bbg.rectTransform.anchoredPosition = new Vector2(20f, -176f);
            backpackFillImg = UIFactory.Image(bbg.transform, "MochilaRelleno", Good, UIFactory.Rounded);
            backpackFillImg.type = Image.Type.Sliced;
            backpackFill = backpackFillImg.rectTransform;
            backpackFill.anchorMin = Vector2.zero; backpackFill.anchorMax = new Vector2(0f, 1f);
            backpackFill.pivot = new Vector2(0f, 0.5f); backpackFill.offsetMin = Vector2.zero; backpackFill.offsetMax = Vector2.zero;
            // Marca de "lento" en 6/10.
            var slowMark = UIFactory.Image(bbg.transform, "MarcaLento", new Color(1f, 1f, 1f, 0.7f));
            var smr = slowMark.rectTransform; smr.anchorMin = new Vector2(0.6f, 0f); smr.anchorMax = new Vector2(0.6f, 1f); smr.sizeDelta = new Vector2(3f, 6f); smr.anchoredPosition = Vector2.zero;
            if (lm.requiresSpecial)
                specialText = UIFactory.Label(it, "", 22, Warn, TextAnchor.UpperLeft, TL, new Vector2(20f, -208f), new Vector2(500f, 30f), FontStyle.Bold);

            // Tiempo y ayuda (arriba a la derecha).
            var tp = UIFactory.Panel(root, "Tiempo", UIFactory.PanelColor, TR, new Vector2(-24f, -24f), new Vector2(260f, 84f));
            tp.rectTransform.pivot = TR; tp.rectTransform.anchoredPosition = new Vector2(-24f, -24f);
            timeText = UIFactory.Label(tp.transform, "0:00", 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(250f, 40f), FontStyle.Bold);
            UIFactory.Label(tp.transform, "Esc: pausa", 18, UIFactory.TextLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(250f, 26f));

            // Barra de ruido vertical a la izquierda.
            BuildNoiseBar(root);

            // Indicación de interacción (abajo al centro).
            var pp = UIFactory.Panel(root, "Indicacion", new Color(0.05f, 0.05f, 0.1f, 0.8f), BC, new Vector2(0f, 40f), new Vector2(1000f, 62f));
            pp.rectTransform.pivot = new Vector2(0.5f, 0f);
            promptPanel = pp.gameObject;
            promptText = UIFactory.Label(pp.transform, "", 26, Color.white, TextAnchor.MiddleCenter, C, Vector2.zero, new Vector2(980f, 60f), FontStyle.Bold);
            promptPanel.SetActive(false);

            hiddenText = UIFactory.Label(root, "", 30, Info, TextAnchor.MiddleCenter, C, new Vector2(0f, -170f), new Vector2(900f, 50f), FontStyle.Bold);

            // Tutorial (abajo a la izquierda).
            var tut = UIFactory.Panel(root, "Tutorial", new Color(0.15f, 0.25f, 0.45f, 0.9f), BL, new Vector2(110f, 130f), new Vector2(560f, 120f));
            tut.rectTransform.pivot = BL; tut.rectTransform.anchoredPosition = new Vector2(110f, 120f);
            tutorialPanel = tut.gameObject;
            UIFactory.Label(tut.transform, "CONSEJO", 18, UIFactory.Gold, TextAnchor.UpperLeft, TL, new Vector2(20f, -12f), new Vector2(270f, 24f), FontStyle.Bold);
            tutorialText = UIFactory.Label(tut.transform, "", 22, Color.white, TextAnchor.UpperLeft, C, new Vector2(0f, -10f), new Vector2(520f, 80f));
            tutorialPanel.SetActive(false);

            // Avisos (arriba al centro).
            var tr = new GameObject("Avisos");
            toastRoot = UIFactory.Rect(tr, root, TC, TC, TC, new Vector2(0f, -110f), new Vector2(900f, 300f));
            var vl = tr.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.spacing = 6f;
            vl.childControlHeight = false; vl.childControlWidth = false;
            vl.childForceExpandHeight = false; vl.childForceExpandWidth = false;

            iconRootRt = iconRoot.GetComponent<RectTransform>();
            BuildIntro(root);
            BuildPause(root);
        }

        RectTransform iconRootRt;

        void BuildNoiseBar(Transform root)
        {
            var anchor = new Vector2(0f, 0.5f);
            var frame = UIFactory.Panel(root, "Ruido", UIFactory.PanelColor, anchor, new Vector2(40f, -10f), new Vector2(110f, 470f));
            var f = frame.transform;
            var bg = UIFactory.Panel(f, "Fondo", new Color(1f, 1f, 1f, 0.12f), new Vector2(0.5f, 0.5f), new Vector2(-16f, 12f), new Vector2(40f, 380f));
            noiseFillImg = UIFactory.Image(bg.transform, "Relleno", Good, UIFactory.Rounded);
            noiseFillImg.type = Image.Type.Sliced;
            noiseFill = noiseFillImg.rectTransform;
            noiseFill.anchorMin = Vector2.zero; noiseFill.anchorMax = new Vector2(1f, 0f);
            noiseFill.offsetMin = new Vector2(3f, 3f); noiseFill.offsetMax = new Vector2(-3f, 0f);
            foreach (int mark in new[] { 30, 60, 100 })
            {
                var m = UIFactory.Image(bg.transform, "Marca" + mark, new Color(1f, 1f, 1f, 0.85f));
                var mr = m.rectTransform;
                mr.anchorMin = new Vector2(0f, mark / 100f); mr.anchorMax = new Vector2(1f, mark / 100f);
                mr.sizeDelta = new Vector2(10f, 3f); mr.anchoredPosition = Vector2.zero;
                var lbl = UIFactory.Label(bg.transform, mark.ToString(), 16, Color.white, TextAnchor.MiddleLeft, new Vector2(1f, mark / 100f), new Vector2(26f, 0f), new Vector2(40f, 20f), FontStyle.Bold);
                lbl.rectTransform.anchorMin = lbl.rectTransform.anchorMax = new Vector2(1f, mark / 100f); lbl.rectTransform.pivot = new Vector2(0f, 0.5f); lbl.rectTransform.anchoredPosition = new Vector2(6f, 0f);
            }
            UIFactory.Label(f, "RUIDO", 18, UIFactory.Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(110f, 24f), FontStyle.Bold);
            noiseStatus = UIFactory.Label(root, "", 20, Color.white, TextAnchor.MiddleLeft, anchor, new Vector2(230f, -230f), new Vector2(260f, 30f), FontStyle.Bold);
        }

        GameObject ModalPanel(Transform root, string name, Vector2 size)
        {
            var dim = UIFactory.Image(root, name, new Color(0f, 0f, 0f, 0.55f));
            dim.raycastTarget = true;
            UIFactory.Stretch(dim.gameObject, root);
            var p = UIFactory.Panel(dim.transform, "Panel", new Color(0.12f, 0.1f, 0.2f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            p.raycastTarget = true;
            return dim.gameObject;
        }

        void BuildIntro(Transform root)
        {
            introPanel = ModalPanel(root, "Intro", new Vector2(1000f, 760f));
            var p = introPanel.transform.Find("Panel");
            var top = new Vector2(0.5f, 1f);
            UIFactory.Label(p, $"Nivel {lm.levelNumber}", 30, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -50f), new Vector2(900f, 40f), FontStyle.Bold);
            UIFactory.Label(p, lm.levelTitle, 60, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -110f), new Vector2(900f, 70f), FontStyle.Bold);
            UIFactory.Label(p, lm.introText, 26, UIFactory.TextLight, TextAnchor.UpperCenter, top, new Vector2(0f, -190f), new Vector2(860f, 120f));
            string goal = $"<b>Objetivo:</b> guarda <color=#FFD54A><b>{lm.lootGoal} monedas</b></color> en la salida";
            if (lm.requiresSpecial) goal += $"\ny roba el <color=#FFD54A><b>{lm.specialItemName}</b></color> de la colección";
            UIFactory.Label(p, goal, 28, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -310f), new Vector2(900f, 80f));
            var cbox = UIFactory.Panel(p, "Controles", new Color(1f, 1f, 1f, 0.07f), top, new Vector2(0f, -405f), new Vector2(620f, 200f));
            UIFactory.Label(cbox.transform, ControlsText, 24, UIFactory.TextLight, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(520f, 200f));
            UIFactory.Button(p, "¡A robar!", new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(360f, 80f), UIFactory.ButtonColor, () => lm.BeginPlay(), 36);
            UIFactory.Label(p, "(o pulsa Enter)", 18, UIFactory.TextLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(400f, 24f));
        }

        void BuildPause(Transform root)
        {
            pausePanel = ModalPanel(root, "Pausa", new Vector2(720f, 760f));
            var p = pausePanel.transform.Find("Panel");
            var top = new Vector2(0.5f, 1f);
            UIFactory.Label(p, "PAUSA", 60, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -70f), new Vector2(600f, 70f), FontStyle.Bold);
            UIFactory.Button(p, "Continuar", top, new Vector2(0f, -170f), new Vector2(420f, 72f), UIFactory.ButtonColor, () => lm.Resume());
            UIFactory.Button(p, "Reiniciar nivel", top, new Vector2(0f, -255f), new Vector2(420f, 72f), UIFactory.ButtonAlt, () => lm.Restart());
            UIFactory.Button(p, "Menú principal", top, new Vector2(0f, -340f), new Vector2(420f, 72f), UIFactory.ButtonAlt, () => lm.GoToMenu());
            UIFactory.Slider(p, "Música", GameProgress.MusicVolume, top, new Vector2(0f, -440f), v => GameProgress.MusicVolume = v);
            UIFactory.Slider(p, "Efectos", GameProgress.SfxVolume, top, new Vector2(0f, -500f), v => GameProgress.SfxVolume = v);
            Button mute = null;
            mute = UIFactory.Button(p, GameProgress.Muted ? "Activar sonido" : "Silenciar todo", top, new Vector2(0f, -580f), new Vector2(320f, 60f), UIFactory.ButtonAlt, () =>
            {
                GameProgress.Muted = !GameProgress.Muted;
                mute.GetComponentInChildren<Text>().text = GameProgress.Muted ? "Activar sonido" : "Silenciar todo";
            }, 28);
            UIFactory.Label(p, ControlsText.Replace("\n", "   ·   "), 16, UIFactory.TextLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(680f, 60f));
            pausePanel.SetActive(false);
        }

        public void ShowIntro() { if (introPanel != null) introPanel.SetActive(true); }
        public void HideIntro() { if (introPanel != null) introPanel.SetActive(false); }
        public void ShowPause(bool on) { if (pausePanel != null) pausePanel.SetActive(on); }

        public void ShowLose()
        {
            if (losePanel != null) Destroy(losePanel);
            losePanel = ModalPanel(canvas.transform, "Derrota", new Vector2(900f, 560f));
            var p = losePanel.transform.Find("Panel");
            var top = new Vector2(0.5f, 1f);
            UIFactory.Label(p, "¡TE ATRAPARON!", 64, Bad, TextAnchor.MiddleCenter, top, new Vector2(0f, -80f), new Vector2(860f, 80f), FontStyle.Bold);
            UIFactory.Label(p, $"Te atrapó {lm.CaughtBy}.", 30, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -170f), new Vector2(860f, 40f));
            string[] tips =
            {
                "Consejo: agáchate (Ctrl) cerca de los habitantes; te ven desde menos distancia.",
                "Consejo: si la barra de ruido pasa de 60, quédate quieto o escóndete hasta que baje.",
                "Consejo: escóndete antes de que te vean; si ya te están mirando, ¡no podrás!",
                "Consejo: suelta objetos pesados (Q) para correr más rápido si te persiguen.",
                "Consejo: guarda el botín en la salida varias veces; no hace falta llevarlo todo junto."
            };
            UIFactory.Label(p, tips[Random.Range(0, tips.Length)], 24, UIFactory.TextLight, TextAnchor.MiddleCenter, top, new Vector2(0f, -250f), new Vector2(800f, 80f));
            UIFactory.Button(p, "Reintentar", new Vector2(0.5f, 0f), new Vector2(-170f, 80f), new Vector2(300f, 80f), UIFactory.ButtonColor, () => lm.Restart());
            UIFactory.Button(p, "Menú", new Vector2(0.5f, 0f), new Vector2(170f, 80f), new Vector2(300f, 80f), UIFactory.ButtonAlt, () => lm.GoToMenu());
            promptPanel.SetActive(false);
        }

        public void ShowWin(int stars)
        {
            if (winPanel != null) Destroy(winPanel);
            bool last = lm.levelNumber >= GameProgress.LevelCount;
            winPanel = ModalPanel(canvas.transform, "Victoria", new Vector2(980f, last ? 860f : 760f));
            var p = winPanel.transform.Find("Panel");
            var top = new Vector2(0.5f, 1f);
            float y = -70f;
            if (last)
            {
                UIFactory.Label(p, "¡VICTORIA TOTAL!", 66, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(940f, 80f), FontStyle.Bold);
                y -= 80f;
                UIFactory.Label(p, "Robaste la mansión del coleccionista. ¡Eres el mapache más famoso del barrio!", 26, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(880f, 60f));
                y -= 70f;
            }
            else
            {
                UIFactory.Label(p, "¡ESCAPASTE!", 70, Good, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(900f, 80f), FontStyle.Bold);
                y -= 90f;
            }
            UIFactory.Label(p, lm.levelTitle, 30, UIFactory.TextLight, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(900f, 40f));
            y -= 110f;
            for (int i = 0; i < 3; i++)
            {
                var s = UIFactory.Image(p, "Estrella" + i, i < stars ? UIFactory.Gold : new Color(1f, 1f, 1f, 0.15f), UIFactory.Star);
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = top;
                s.rectTransform.anchoredPosition = new Vector2((i - 1) * 150f, y + (i == 1 ? 15f : 0f));
                s.rectTransform.sizeDelta = new Vector2(130f, 130f);
            }
            y -= 110f;
            int t = Mathf.FloorToInt(lm.Elapsed);
            string summary = $"Botín: <b><color=#FFD54A>{lm.Banked} monedas</color></b>   (objetivo {lm.lootGoal})\nTiempo: <b>{t / 60}:{t % 60:00}</b>";
            UIFactory.Label(p, summary, 30, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(900f, 90f));
            y -= 100f;
            string crit =
                $"★ Escapar con el objetivo  ✔\n" +
                $"★ Botín de {Mathf.CeilToInt(lm.lootGoal * lm.bonusLootFactor)} o más  {(lm.Banked >= Mathf.CeilToInt(lm.lootGoal * lm.bonusLootFactor) ? "✔" : "✘")}\n" +
                $"★ Que nadie te descubra  {(!lm.EverSpotted ? "✔" : "✘")}";
            crit = crit.Replace("★", "•").Replace("✔", "(sí)").Replace("✘", "(no)");
            UIFactory.Label(p, crit, 24, UIFactory.TextLight, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(900f, 100f));
            if (last)
                UIFactory.Label(p, $"Estrellas totales: {GameProgress.TotalStars} / {GameProgress.LevelCount * 3}", 28, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, y - 110f), new Vector2(900f, 40f), FontStyle.Bold);

            var bottom = new Vector2(0.5f, 0f);
            if (!last)
            {
                UIFactory.Button(p, "Siguiente nivel", bottom, new Vector2(-240f, 70f), new Vector2(320f, 80f), UIFactory.ButtonColor, () => lm.NextLevel());
                UIFactory.Button(p, "Reintentar", bottom, new Vector2(100f, 70f), new Vector2(240f, 80f), UIFactory.ButtonAlt, () => lm.Restart());
                UIFactory.Button(p, "Menú", bottom, new Vector2(340f, 70f), new Vector2(200f, 80f), UIFactory.ButtonAlt, () => lm.GoToMenu());
            }
            else
            {
                UIFactory.Button(p, "Menú principal", bottom, new Vector2(-170f, 70f), new Vector2(320f, 80f), UIFactory.ButtonColor, () => GameProgress.LoadMenu(false));
                UIFactory.Button(p, "Jugar otra vez", bottom, new Vector2(170f, 70f), new Vector2(300f, 80f), UIFactory.ButtonAlt, () => lm.Restart());
            }
            promptPanel.SetActive(false);
            tutorialPanel.SetActive(false);
        }

        public void SetPrompt(string text)
        {
            if (promptPanel == null) return;
            bool on = !string.IsNullOrEmpty(text);
            if (promptPanel.activeSelf != on) promptPanel.SetActive(on);
            if (on && promptText.text != text) promptText.text = text;
        }

        public void SetTutorial(string text)
        {
            if (tutorialPanel == null) return;
            tutorialPanel.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) tutorialText.text = text;
        }

        public void Toast(string msg, Color color)
        {
            if (toastRoot == null) return;
            // Evita repetir el mismo aviso varias veces seguidas.
            foreach (var t in toasts)
                if (t.go != null && t.go.GetComponentInChildren<Text>().text == msg) return;
            var bg = UIFactory.Panel(toastRoot, "Aviso", new Color(0.05f, 0.04f, 0.1f, 0.78f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 42f));
            UIFactory.Label(bg.transform, msg, 22, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 40f), FontStyle.Bold);
            toasts.Add((bg.gameObject, 2.4f));
            while (toasts.Count > 4)
            {
                Destroy(toasts[0].go);
                toasts.RemoveAt(0);
            }
        }

        public void Flash(Color c)
        {
            flashColor = c;
            flashT = 1f;
        }

        void Update()
        {
            if (lm == null) return;
            float dt = Time.unscaledDeltaTime;

            bankedText.text = $"Botín guardado: <color=#FFD54A>{lm.Banked}</color> / {lm.lootGoal}" + (lm.GoalMet ? "  <color=#7CFF8A>✓</color>".Replace("✓", "¡OK!") : "");
            var p = lm.player;
            if (p != null)
            {
                carriedText.text = p.carried.Count > 0 ? $"En la mochila: {p.CarriedValue} monedas ({p.carried.Count} objeto{(p.carried.Count == 1 ? "" : "s")})" : "En la mochila: nada todavía";
                backpackText.text = $"Espacio de la mochila: {p.CarriedWeight} / {p.capacity}" + (p.IsHeavy ? "   <color=#FFB347>(vas más lento)</color>" : "");
                float k = p.CarriedWeight / (float)Mathf.Max(1, p.capacity);
                backpackFill.anchorMax = new Vector2(k, 1f);
                backpackFillImg.color = p.CarriedWeight >= p.capacity ? Bad : (p.IsHeavy ? Warn : Good);
                hiddenOverlay.gameObject.SetActive(p.IsHidden);
                hiddenText.text = p.IsHidden && lm.State == LevelState.Playing ? "Escondido... nadie puede verte" : "";
            }
            if (specialText != null)
                specialText.text = lm.SpecialBanked ? $"Objeto especial: {lm.specialItemName} — ¡guardado!" :
                    (p != null && p.CarryingSpecial ? $"Objeto especial: {lm.specialItemName} — ¡llévalo a la salida!" : $"Objeto especial: {lm.specialItemName} — falta");
            int t = Mathf.FloorToInt(lm.Elapsed);
            timeText.text = $"{t / 60}:{t % 60:00}";

            var noise = NoiseSystem.I;
            float level = noise != null ? noise.Level : 0f;
            noiseFill.anchorMax = new Vector2(1f, Mathf.Max(0.02f, level / 100f));
            Color nc = level < 30f ? Good : (level < 60f ? Warn : (level < 99f ? new Color(1f, 0.55f, 0.2f) : Bad));
            noiseFillImg.color = nc;
            noiseStatus.color = nc;
            noiseStatus.text = level < 5f ? "Silencio" : (level < 30f ? "Poco ruido" : (level < 60f ? "Te pueden oír" : (level < 99f ? "¡Te buscan!" : "¡ALERTA!")));
            var nrt = noiseStatus.rectTransform;
            nrt.anchoredPosition = new Vector2(160f, -178f + 380f * Mathf.Clamp01(level / 100f));

            // Avisos que desaparecen.
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var (go, life) = toasts[i];
                life -= dt;
                if (go == null || life <= 0f)
                {
                    if (go != null) Destroy(go);
                    toasts.RemoveAt(i);
                    continue;
                }
                toasts[i] = (go, life);
                if (life < 0.4f)
                {
                    var cg = go.GetComponent<CanvasGroup>();
                    if (cg == null) cg = go.AddComponent<CanvasGroup>();
                    cg.alpha = life / 0.4f;
                }
            }

            if (flashT > 0f)
            {
                flashT -= dt * 2f;
                var c = flashColor; c.a *= Mathf.Clamp01(flashT);
                flashImg.color = c;
            }
            else if (flashImg.color.a > 0f) flashImg.color = new Color(0, 0, 0, 0);

            UpdateIcons();
        }

        void UpdateIcons()
        {
            var cam = Camera.main;
            int used = 0;
            if (cam != null && lm.State != LevelState.Intro)
            {
                foreach (var w in Perception.Watchers)
                {
                    if (w == null) continue;
                    string txt = w.IconText;
                    if (string.IsNullOrEmpty(txt)) continue;
                    Vector3 sp = cam.WorldToScreenPoint(w.IconPosition);
                    if (sp.z < 0f) continue;
                    if (used >= iconPool.Count)
                    {
                        var l = UIFactory.Label(iconRootRt, "", 44, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Vector2(160f, 60f), FontStyle.Bold);
                        var ol = l.gameObject.AddComponent<Outline>();
                        ol.effectColor = new Color(0f, 0f, 0f, 0.8f);
                        ol.effectDistance = new Vector2(2f, -2f);
                        iconPool.Add(l);
                    }
                    var icon = iconPool[used++];
                    icon.gameObject.SetActive(true);
                    icon.text = txt;
                    icon.color = w.IconColor;
                    icon.fontSize = txt == "Zzz" ? 30 : 46;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var local);
                    var rt = icon.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    float bob = txt == "Zzz" ? Mathf.Sin(Time.time * 2f) * 6f : 0f;
                    rt.anchoredPosition = local + new Vector2(0f, bob);
                }
            }
            for (int i = used; i < iconPool.Count; i++) iconPool[i].gameObject.SetActive(false);
        }
    }
}
