using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RobaYCorre
{
    /// <summary>
    /// Interfaz durante el juego: botín, objetivo, mochila, barra de ruido, avisos animados y pantallas
    /// (inicio, pausa, victoria, derrota). Estilo colorido con rebotes.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static readonly Color Good = new Color(0.45f, 1f, 0.45f);
        public static readonly Color Warn = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Bad = new Color(1f, 0.38f, 0.35f);
        public static readonly Color Info = new Color(0.75f, 0.88f, 1f);

        /// <summary>Lista de controles: (tecla, acción).</summary>
        public static readonly (string key, string action)[] Controls =
        {
            ("WASD", "moverse (o flechas)"),
            ("Shift", "correr (¡hace ruido!)"),
            ("Ctrl", "agacharse (silencioso)"),
            ("E", "robar · escapar"),
            ("Q", "esconderse / salir"),
            ("R", "soltar el último objeto"),
            ("Esc", "pausa"),
        };

        public const string ControlsText =
            "<b>WASD / Flechas</b>  moverse\n" +
            "<b>Shift</b>  correr (¡hace ruido!)\n" +
            "<b>Ctrl</b> o <b>C</b>  agacharse (silencioso)\n" +
            "<b>E</b>  robar · escapar\n" +
            "<b>Q</b>  esconderse / salir del escondite\n" +
            "<b>R</b>  soltar el último objeto\n" +
            "<b>Esc</b>  pausa";

        LevelManager lm;
        Canvas canvas;
        RectTransform canvasRt, iconRootRt, toastRoot, popRoot;

        Text bankedText, carriedText, backpackText, specialText, timeText, noiseStatus, promptText, tutorialText, hiddenText;
        Image[] backpackSegs;
        RectTransform noiseFill;
        Image noiseFillImg, flashImg, hiddenOverlay;
        UIShake noiseShake;
        UIPop promptPop;
        GameObject promptPanel, tutorialPanel, introPanel, pausePanel, losePanel, winPanel;
        Color flashColor;
        float flashT, lastPopTime = -10f;
        string lastPopText;
        readonly List<(GameObject go, float life)> toasts = new List<(GameObject, float)>();
        readonly List<(GameObject go, float life)> pops = new List<(GameObject, float)>();
        readonly List<Text> iconPool = new List<Text>();
        Text exitIcon;

        public static HUD Create(LevelManager lm)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUD>();
            hud.lm = lm;
            hud.Build();
            return hud;
        }

        static Vector2 TL => new Vector2(0f, 1f);
        static Vector2 TR => new Vector2(1f, 1f);
        static Vector2 TC => new Vector2(0.5f, 1f);
        static Vector2 BC => new Vector2(0.5f, 0f);
        static Vector2 BL => new Vector2(0f, 0f);
        static Vector2 C => new Vector2(0.5f, 0.5f);

        void Build()
        {
            canvas = UIFactory.CreateCanvas("HUD_Canvas", 10);
            canvas.transform.SetParent(transform, false);
            canvasRt = canvas.GetComponent<RectTransform>();
            var root = canvas.transform;

            var iconRoot = new GameObject("Iconos");
            iconRootRt = UIFactory.Stretch(iconRoot, root);

            flashImg = UIFactory.Image(root, "Destello", new Color(1, 0, 0, 0));
            UIFactory.Stretch(flashImg.gameObject, root);
            hiddenOverlay = UIFactory.Image(root, "Escondido", new Color(0.03f, 0.02f, 0.12f, 0.4f));
            UIFactory.Stretch(hiddenOverlay.gameObject, root);
            hiddenOverlay.gameObject.SetActive(false);

            // --- Insignia del nivel ---
            var badge = UIFactory.Panel(root, "Insignia", UIFactory.ButtonPink, TL, new Vector2(28f, -22f), new Vector2(470f, 58f));
            var badgeRoot = badge.transform.parent;
            badgeRoot.localRotation = Quaternion.Euler(0f, 0f, 2f);
            UIFactory.Label(badge.transform, $"NIVEL {lm.levelNumber} · {lm.levelTitle}", 24, Color.white, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(460f, 50f));
            badgeRoot.gameObject.AddComponent<UIPop>().delay = 0.05f;

            // --- Botín ---
            var loot = UIFactory.Panel(root, "Botin", UIFactory.PanelColor, TL, new Vector2(28f, -96f), new Vector2(470f, 150f));
            var lt = loot.transform;
            var coin = UIFactory.Image(lt, "Moneda", Color.white, UIFactory.Coin);
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            coin.rectTransform.anchoredPosition = new Vector2(70f, 6f);
            coin.rectTransform.sizeDelta = new Vector2(96f, 96f);
            coin.gameObject.AddComponent<UIWobble>().angle = 8f;
            UIFactory.Label(lt, "BOTÍN GUARDADO", 20, UIFactory.Gold, TextAnchor.UpperLeft, TL, new Vector2(135f, -14f), new Vector2(320f, 28f));
            bankedText = UIFactory.Label(lt, "", 50, Color.white, TextAnchor.UpperLeft, TL, new Vector2(135f, -38f), new Vector2(330f, 60f), FontStyle.Bold);
            carriedText = UIFactory.Label(lt, "", 20, UIFactory.TextLight, TextAnchor.UpperLeft, TL, new Vector2(135f, -104f), new Vector2(330f, 30f));
            loot.transform.parent.gameObject.AddComponent<UIPop>().delay = 0.12f;

            // --- Mochila (10 segmentos) ---
            var pack = UIFactory.Panel(root, "Mochila", UIFactory.PanelColor, TL, new Vector2(28f, -264f), new Vector2(470f, 104f));
            var pt = pack.transform;
            backpackText = UIFactory.Label(pt, "", 22, Color.white, TextAnchor.UpperLeft, TL, new Vector2(20f, -10f), new Vector2(440f, 30f));
            backpackSegs = new Image[10];
            for (int i = 0; i < 10; i++)
            {
                var seg = UIFactory.Image(pt, "Hueco" + i, new Color(1f, 1f, 1f, 0.18f), UIFactory.Chunky);
                seg.type = Image.Type.Sliced;
                var r = seg.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
                r.pivot = new Vector2(0f, 0f);
                r.anchoredPosition = new Vector2(20f + i * 43f + (i >= 6 ? 8f : 0f), 14f);
                r.sizeDelta = new Vector2(38f, 36f);
                backpackSegs[i] = seg;
            }
            pack.transform.parent.gameObject.AddComponent<UIPop>().delay = 0.18f;

            if (lm.requiresSpecial)
            {
                var sp = UIFactory.Panel(root, "Especial", new Color(0.55f, 0.18f, 0.7f, 0.95f), TL, new Vector2(28f, -386f), new Vector2(470f, 52f));
                specialText = UIFactory.Label(sp.transform, "", 22, UIFactory.Gold, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(450f, 44f));
                sp.transform.parent.gameObject.AddComponent<UIPop>().delay = 0.24f;
            }

            // --- Tiempo ---
            var tp = UIFactory.Panel(root, "Tiempo", UIFactory.ButtonAlt, TR, new Vector2(-28f, -22f), new Vector2(220f, 92f));
            timeText = UIFactory.Label(tp.transform, "0:00", 40, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(210f, 52f), FontStyle.Bold);
            UIFactory.Label(tp.transform, "Esc = pausa", 18, UIFactory.TextLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(210f, 26f));
            tp.transform.parent.gameObject.AddComponent<UIPop>().delay = 0.1f;

            BuildNoiseBar(root);

            // --- Indicación de interacción ---
            var pp = UIFactory.Panel(root, "Indicacion", UIFactory.PanelDark, BC, new Vector2(0f, 30f), new Vector2(1180f, 66f));
            promptPanel = pp.transform.parent.gameObject;
            promptText = UIFactory.Label(pp.transform, "", 26, Color.white, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(1160f, 60f));
            promptPop = promptPanel.AddComponent<UIPop>();
            promptPop.duration = 0.25f;
            promptPanel.SetActive(false);

            hiddenText = UIFactory.Label(root, "", 34, Info, TextAnchor.MiddleCenter, C, new Vector2(0f, -170f), new Vector2(900f, 60f));

            // --- Consejos (nivel 1) ---
            var tut = UIFactory.Panel(root, "Tutorial", UIFactory.ButtonAlt, TR, new Vector2(-28f, -150f), new Vector2(520f, 128f));
            tutorialPanel = tut.transform.parent.gameObject;
            var tag = UIFactory.Panel(tut.transform, "Etiqueta", UIFactory.ButtonColor, TL, new Vector2(16f, 22f), new Vector2(150f, 40f));
            tag.transform.parent.localRotation = Quaternion.Euler(0f, 0f, 4f);
            UIFactory.Label(tag.transform, "CONSEJO", 20, UIFactory.Ink, TextAnchor.MiddleCenter, C, new Vector2(0f, 1f), new Vector2(150f, 36f), FontStyle.Normal, false);
            tutorialText = UIFactory.Label(tut.transform, "", 21, Color.white, TextAnchor.MiddleLeft, C, new Vector2(0f, -8f), new Vector2(484f, 100f));
            tutorialPanel.SetActive(false);

            // --- Avisos y carteles grandes ---
            var tr = new GameObject("Avisos");
            toastRoot = UIFactory.Rect(tr, root, TC, TC, TC, new Vector2(0f, -110f), new Vector2(760f, 300f));
            var vl = tr.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.spacing = 10f;
            vl.childControlHeight = false; vl.childControlWidth = false;
            vl.childForceExpandHeight = false; vl.childForceExpandWidth = false;
            var pr = new GameObject("Carteles");
            popRoot = UIFactory.Rect(pr, root, C, C, C, new Vector2(0f, 230f), new Vector2(10f, 10f));

            BuildIntro(root);
            BuildPause(root);
        }

        void BuildNoiseBar(Transform root)
        {
            var anchor = new Vector2(0f, 0.5f);
            var frame = UIFactory.Panel(root, "Ruido", UIFactory.PanelColor, anchor, new Vector2(34f, -130f), new Vector2(112f, 430f));
            var frameRoot = frame.transform.parent;
            noiseShake = frameRoot.gameObject.AddComponent<UIShake>();
            frameRoot.gameObject.AddComponent<UIPop>().delay = 0.3f;
            var f = frame.transform;
            var track = UIFactory.Image(f, "Pista", UIFactory.Ink, UIFactory.Solid);
            track.type = Image.Type.Sliced;
            var trt = track.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(-14f, 18f);
            trt.sizeDelta = new Vector2(44f, 350f);
            noiseFillImg = UIFactory.Image(track.transform, "Relleno", Good, UIFactory.Chunky);
            noiseFillImg.type = Image.Type.Sliced;
            noiseFill = noiseFillImg.rectTransform;
            noiseFill.anchorMin = Vector2.zero; noiseFill.anchorMax = new Vector2(1f, 0f);
            noiseFill.offsetMin = new Vector2(5f, 5f); noiseFill.offsetMax = new Vector2(-5f, 0f);
            foreach (int mark in new[] { 30, 60, 100 })
            {
                var m = UIFactory.Image(track.transform, "Marca" + mark, Color.white);
                var mr = m.rectTransform;
                mr.anchorMin = new Vector2(0f, mark / 100f); mr.anchorMax = new Vector2(1f, mark / 100f);
                mr.sizeDelta = new Vector2(12f, 4f); mr.anchoredPosition = Vector2.zero;
                var lbl = UIFactory.Label(track.transform, mark.ToString(), 18, Color.white, TextAnchor.MiddleLeft, new Vector2(1f, mark / 100f), Vector2.zero, new Vector2(46f, 22f));
                lbl.rectTransform.pivot = new Vector2(0f, 0.5f);
                lbl.rectTransform.anchoredPosition = new Vector2(7f, 0f);
            }
            UIFactory.Label(f, "RUIDO", 20, UIFactory.Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(112f, 28f));
            noiseStatus = UIFactory.Label(root, "", 24, Color.white, TextAnchor.MiddleLeft, anchor, new Vector2(160f, -130f), new Vector2(300f, 36f));
            noiseStatus.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        // ------------------------------------------------------------------ ventanas

        GameObject Modal(Transform root, string name, Vector2 size, Color color, out Transform panel)
        {
            var dim = UIFactory.Image(root, name, new Color(0.05f, 0.02f, 0.15f, 0.6f));
            dim.raycastTarget = true;
            UIFactory.Stretch(dim.gameObject, root);
            var body = UIFactory.Panel(dim.transform, "Panel", color, C, Vector2.zero, size);
            var pop = body.transform.parent.gameObject.AddComponent<UIPop>();
            pop.elastic = true;
            pop.duration = 0.5f;
            panel = body.transform;
            return dim.gameObject;
        }

        Text Title(Transform p, string text, int size, Color color, float y, bool wobble = true)
        {
            var t = UIFactory.Label(p, text, size, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(980f, size * 1.4f), FontStyle.Bold);
            if (wobble) t.gameObject.AddComponent<UIWobble>().angle = 2.5f;
            return t;
        }

        void ControlsGrid(Transform p, Vector2 anchor, Vector2 pos)
        {
            var g = UIFactory.Group(p, "Controles", anchor, pos, new Vector2(840f, 250f));
            for (int i = 0; i < Controls.Length; i++)
            {
                int col = i < 4 ? 0 : 1;
                int row = i < 4 ? i : i - 4;
                float x = col * 430f;
                float y = -row * 64f;
                var (key, action) = Controls[i];
                float w = key.Length > 2 ? 104f : 64f;
                var kc = UIFactory.KeyCap(g, key, TL, new Vector2(x, y), w);
                var pop = kc.gameObject.AddComponent<UIPop>();
                pop.delay = 0.35f + i * 0.06f;
                UIFactory.Label(g, action, 24, Color.white, TextAnchor.MiddleLeft, TL, new Vector2(x + w + 14f, y - 4f), new Vector2(300f, 52f));
            }
        }

        void BuildIntro(Transform root)
        {
            introPanel = Modal(root, "Intro", new Vector2(1040f, 830f), UIFactory.PanelColor, out var p);
            var top = new Vector2(0.5f, 1f);
            var lvl = UIFactory.Panel(p, "Nivel", UIFactory.ButtonPink, top, new Vector2(0f, 30f), new Vector2(220f, 56f));
            lvl.transform.parent.localRotation = Quaternion.Euler(0f, 0f, -3f);
            UIFactory.Label(lvl.transform, $"NIVEL {lm.levelNumber}", 28, Color.white, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(210f, 50f));
            Title(p, lm.levelTitle.ToUpper(), 62, UIFactory.Gold, -50f);
            UIFactory.Label(p, lm.introText, 24, UIFactory.TextLight, TextAnchor.UpperCenter, top, new Vector2(0f, -150f), new Vector2(940f, 110f));

            var goal = UIFactory.Panel(p, "Objetivo", UIFactory.ButtonAlt, top, new Vector2(0f, -268f), new Vector2(820f, 84f));
            var coin = UIFactory.Image(goal.transform, "Moneda", Color.white, UIFactory.Coin);
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            coin.rectTransform.anchoredPosition = new Vector2(52f, 0f);
            coin.rectTransform.sizeDelta = new Vector2(64f, 64f);
            coin.gameObject.AddComponent<UIWobble>().angle = 10f;
            string goalText = $"Guarda <color=#FFE14D>{lm.lootGoal} monedas</color> en la salida";
            if (lm.requiresSpecial) goalText += $"\n¡y roba el <color=#FFE14D>{lm.specialItemName}</color>!";
            UIFactory.Label(goal.transform, goalText, lm.requiresSpecial ? 25 : 30, Color.white, TextAnchor.MiddleCenter, C, new Vector2(30f, 2f), new Vector2(720f, 80f));

            ControlsGrid(p, top, new Vector2(0f, -380f));

            var b = UIFactory.Button(p, "¡A ROBAR!", new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(400f, 92f), UIFactory.ButtonGreen, () => lm.BeginPlay(), 44);
            b.gameObject.AddComponent<UIPop>().delay = 0.8f;
            UIFactory.Label(p, "(o pulsa Enter)", 18, UIFactory.TextLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(400f, 26f));
        }

        void BuildPause(Transform root)
        {
            pausePanel = Modal(root, "Pausa", new Vector2(760f, 820f), UIFactory.PanelColor, out var p);
            var top = new Vector2(0.5f, 1f);
            Title(p, "PAUSA", 80, UIFactory.Gold, -30f);
            AddButton(p, "Continuar", top, new Vector2(0f, -160f), new Vector2(460f, 84f), UIFactory.ButtonGreen, () => lm.Resume(), 0.15f);
            AddButton(p, "Reiniciar nivel", top, new Vector2(0f, -262f), new Vector2(460f, 84f), UIFactory.ButtonColor, () => lm.Restart(), 0.22f);
            AddButton(p, "Menú principal", top, new Vector2(0f, -364f), new Vector2(460f, 84f), UIFactory.ButtonAlt, () => lm.GoToMenu(), 0.29f);
            UIFactory.Slider(p, "Música", GameProgress.MusicVolume, top, new Vector2(0f, -480f), v => GameProgress.MusicVolume = v);
            UIFactory.Slider(p, "Efectos", GameProgress.SfxVolume, top, new Vector2(0f, -548f), v => GameProgress.SfxVolume = v);
            Button mute = null;
            mute = UIFactory.Button(p, GameProgress.Muted ? "Activar sonido" : "Silenciar todo", top, new Vector2(0f, -630f), new Vector2(340f, 64f), UIFactory.ButtonPink, () =>
            {
                GameProgress.Muted = !GameProgress.Muted;
                mute.GetComponentInChildren<Text>().text = GameProgress.Muted ? "Activar sonido" : "Silenciar todo";
            }, 28);
            UIFactory.Label(p, "E robar/escapar · Q esconderse · R soltar · Shift correr · Ctrl agacharse", 18, UIFactory.TextLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(720f, 50f));
            pausePanel.SetActive(false);
        }

        Button AddButton(Transform p, string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick, float delay, int fontSize = 36)
        {
            var b = UIFactory.Button(p, text, anchor, pos, size, color, onClick, fontSize);
            b.gameObject.AddComponent<UIPop>().delay = delay;
            return b;
        }

        public void ShowIntro() { if (introPanel != null) introPanel.SetActive(true); }
        public void HideIntro() { if (introPanel != null) introPanel.SetActive(false); }

        public void ShowPause(bool on)
        {
            if (pausePanel == null) return;
            pausePanel.SetActive(on);
        }

        public void ShowLose()
        {
            if (losePanel != null) Destroy(losePanel);
            losePanel = Modal(canvas.transform, "Derrota", new Vector2(980f, 640f), new Color(0.75f, 0.18f, 0.3f, 0.97f), out var p);
            var top = new Vector2(0.5f, 1f);
            var t = Title(p, "¡TE ATRAPARON!", 76, Color.white, -40f, false);
            t.gameObject.AddComponent<UIShake>().intensity = 4f;
            UIFactory.Label(p, $"Te atrapó {lm.CaughtBy}.", 32, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -160f), new Vector2(900f, 50f));
            string[] tips =
            {
                "Consejo: agáchate (Ctrl) cerca de los habitantes; te ven desde menos distancia.",
                "Consejo: si la barra de ruido pasa de 60, quédate quieto o escóndete (Q) hasta que baje.",
                "Consejo: escóndete antes de que te vean; si ya te están mirando, ¡no podrás!",
                "Consejo: suelta objetos pesados (R) para correr más rápido si te persiguen.",
                "Consejo: guarda el botín en la salida varias veces; no hace falta llevarlo todo junto."
            };
            var tipBox = UIFactory.Panel(p, "Consejo", new Color(0.35f, 0.08f, 0.18f, 0.9f), top, new Vector2(0f, -230f), new Vector2(860f, 120f));
            UIFactory.Label(tipBox.transform, tips[Random.Range(0, tips.Length)], 24, Color.white, TextAnchor.MiddleCenter, C, Vector2.zero, new Vector2(820f, 110f));
            AddButton(p, "Reintentar", new Vector2(0.5f, 0f), new Vector2(-190f, 56f), new Vector2(340f, 92f), UIFactory.ButtonGreen, () => lm.Restart(), 0.4f, 40);
            AddButton(p, "Menú", new Vector2(0.5f, 0f), new Vector2(190f, 56f), new Vector2(300f, 92f), UIFactory.ButtonAlt, () => lm.GoToMenu(), 0.5f, 40);
            promptPanel.SetActive(false);
            tutorialPanel.SetActive(false);
        }

        public void ShowWin(int stars)
        {
            if (winPanel != null) Destroy(winPanel);
            bool last = lm.levelNumber >= GameProgress.LevelCount;
            var dim = UIFactory.Image(canvas.transform, "Victoria", new Color(0.05f, 0.02f, 0.15f, 0.6f));
            dim.raycastTarget = true;
            UIFactory.Stretch(dim.gameObject, canvas.transform);
            winPanel = dim.gameObject;
            var rays = UIFactory.Image(dim.transform, "Rayos", new Color(1f, 0.85f, 0.3f, 0.35f), UIFactory.Burst);
            rays.rectTransform.sizeDelta = new Vector2(1900f, 1900f);
            rays.gameObject.AddComponent<UISpin>().speed = 12f;
            var body = UIFactory.Panel(dim.transform, "Panel", UIFactory.PanelColor, C, Vector2.zero, new Vector2(1000f, last ? 900f : 820f));
            var pop = body.transform.parent.gameObject.AddComponent<UIPop>();
            pop.elastic = true; pop.duration = 0.55f;
            var p = body.transform;
            var top = new Vector2(0.5f, 1f);
            float y = -30f;
            if (last)
            {
                Title(p, "¡VICTORIA TOTAL!", 76, UIFactory.Gold, y);
                y -= 108f;
                UIFactory.Label(p, "Robaste la mansión del coleccionista. ¡Eres el mapache más famoso del barrio!", 26, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(900f, 70f));
                y -= 70f;
            }
            else
            {
                Title(p, "¡ESCAPASTE!", 84, Good, y);
                y -= 112f;
                UIFactory.Label(p, lm.levelTitle, 30, UIFactory.TextLight, TextAnchor.MiddleCenter, top, new Vector2(0f, y), new Vector2(900f, 44f));
                y -= 40f;
            }
            y -= 110f;
            for (int i = 0; i < 3; i++)
            {
                bool got = i < stars;
                var s = UIFactory.Image(p, "Estrella" + i, got ? UIFactory.Gold : new Color(0.35f, 0.3f, 0.55f, 1f), UIFactory.Star);
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = top;
                s.rectTransform.anchoredPosition = new Vector2((i - 1) * 170f, y + (i == 1 ? 20f : 0f));
                s.rectTransform.sizeDelta = i == 1 ? new Vector2(170f, 170f) : new Vector2(140f, 140f);
                s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 10f);
                var sp = s.gameObject.AddComponent<UIPop>();
                sp.elastic = true; sp.duration = 0.8f; sp.delay = 0.6f + i * 0.35f;
                if (got) StartCoroutine(StarSound(0.65f + i * 0.35f, i));
            }
            y -= 120f;
            int t = Mathf.FloorToInt(lm.Elapsed);
            var chips = UIFactory.Group(p, "Datos", top, new Vector2(0f, y), new Vector2(900f, 70f));
            var c1 = UIFactory.Panel(chips, "Botin", UIFactory.ButtonColor, new Vector2(0.5f, 0.5f), new Vector2(-220f, 0f), new Vector2(400f, 64f));
            UIFactory.Label(c1.transform, $"{lm.Banked} monedas (meta {lm.lootGoal})", 26, UIFactory.Ink, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(390f, 60f), FontStyle.Normal, false);
            var c2 = UIFactory.Panel(chips, "Tiempo", UIFactory.ButtonAlt, new Vector2(0.5f, 0.5f), new Vector2(220f, 0f), new Vector2(300f, 64f));
            UIFactory.Label(c2.transform, $"Tiempo {t / 60}:{t % 60:00}", 26, Color.white, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(290f, 60f));
            y -= 70f;
            int bonus = Mathf.CeilToInt(lm.lootGoal * lm.bonusLootFactor);
            string crit =
                "<color=#7CFF7C>✓</color> Escapar con el objetivo\n" +
                (lm.Banked >= bonus ? "<color=#7CFF7C>✓</color>" : "<color=#FF7070>✗</color>") + $" Botín de {bonus} o más\n" +
                (!lm.EverSpotted ? "<color=#7CFF7C>✓</color>" : "<color=#FF7070>✗</color>") + " Que nadie te descubra";
            crit = crit.Replace("✓", "SÍ").Replace("✗", "NO");
            UIFactory.Label(p, crit, 24, Color.white, TextAnchor.UpperCenter, top, new Vector2(0f, y), new Vector2(900f, 110f));
            if (last)
                UIFactory.Label(p, $"Estrellas totales: {GameProgress.TotalStars} / {GameProgress.LevelCount * 3}", 30, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, y - 112f), new Vector2(900f, 44f), FontStyle.Bold);

            var bottom = new Vector2(0.5f, 0f);
            if (!last)
            {
                AddButton(p, "¡Siguiente!", bottom, new Vector2(-250f, 46f), new Vector2(360f, 90f), UIFactory.ButtonGreen, () => lm.NextLevel(), 1.6f, 40);
                AddButton(p, "Reintentar", bottom, new Vector2(110f, 46f), new Vector2(260f, 90f), UIFactory.ButtonColor, () => lm.Restart(), 1.7f, 32);
                AddButton(p, "Menú", bottom, new Vector2(350f, 46f), new Vector2(180f, 90f), UIFactory.ButtonAlt, () => lm.GoToMenu(), 1.8f, 32);
            }
            else
            {
                AddButton(p, "Menú principal", bottom, new Vector2(-190f, 46f), new Vector2(360f, 90f), UIFactory.ButtonGreen, () => GameProgress.LoadMenu(false), 1.6f, 36);
                AddButton(p, "Jugar otra vez", bottom, new Vector2(190f, 46f), new Vector2(340f, 90f), UIFactory.ButtonAlt, () => lm.Restart(), 1.7f, 34);
            }
            promptPanel.SetActive(false);
            tutorialPanel.SetActive(false);
        }

        System.Collections.IEnumerator StarSound(float delay, int i)
        {
            yield return new WaitForSecondsRealtime(delay);
            AudioManager.Play(Sfx.Coin, 1f, 1f + i * 0.15f);
        }

        // ------------------------------------------------------------------ avisos

        public void SetPrompt(string text)
        {
            if (promptPanel == null) return;
            bool on = !string.IsNullOrEmpty(text);
            if (promptPanel.activeSelf != on) promptPanel.SetActive(on);
            if (!on) return;
            string styled = text.Replace("[E]", "<color=#FFE14D>[E]</color>").Replace("[Q]", "<color=#7FE3FF>[Q]</color>").Replace("[R]", "<color=#FF9EC8>[R]</color>");
            if (promptText.text != styled)
            {
                promptText.text = styled;
                promptPop.duration = 0.25f;
                promptPop.Restart();
            }
        }

        public void SetTutorial(string text)
        {
            if (tutorialPanel == null) return;
            bool on = !string.IsNullOrEmpty(text);
            tutorialPanel.SetActive(on);
            if (on)
            {
                tutorialText.text = text;
                var pop = tutorialPanel.GetComponent<UIPop>();
                if (pop == null) pop = tutorialPanel.AddComponent<UIPop>();
                pop.Restart();
            }
        }

        public void Toast(string msg, Color color)
        {
            if (toastRoot == null) return;
            foreach (var t in toasts)
                if (t.go != null && t.go.GetComponentInChildren<Text>().text == msg) return;
            Color bg = Color.Lerp(color, new Color(0.15f, 0.08f, 0.35f), 0.55f);
            bg.a = 0.95f;
            var body = UIFactory.Panel(toastRoot, "Aviso", bg, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(760f, 46f));
            var root = body.transform.parent.gameObject;
            UIFactory.Label(body.transform, msg, 22, Color.white, TextAnchor.MiddleCenter, C, new Vector2(0f, 1f), new Vector2(740f, 44f));
            var pop = root.AddComponent<UIPop>();
            pop.duration = 0.35f;
            toasts.Add((root, 2.6f));
            while (toasts.Count > 4)
            {
                Destroy(toasts[0].go);
                toasts.RemoveAt(0);
            }
        }

        /// <summary>Cartel grande y animado en el centro ("¡Hiciste ruido!", "¡Te vio!", "¡Escapaste!").</summary>
        public void Pop(string text, Color color)
        {
            if (popRoot == null) return;
            if (text == lastPopText && Time.unscaledTime - lastPopTime < 1.5f) return;
            lastPopText = text;
            lastPopTime = Time.unscaledTime;
            foreach (var p in pops) if (p.go != null) Destroy(p.go);
            pops.Clear();
            var t = UIFactory.Label(popRoot, text, 92, color, TextAnchor.MiddleCenter, C, Vector2.zero, new Vector2(1400f, 140f), FontStyle.Bold);
            t.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-6f, 6f));
            var pop = t.gameObject.AddComponent<UIPop>();
            pop.elastic = true;
            pop.duration = 0.6f;
            pops.Add((t.gameObject, 1.3f));
        }

        public void Flash(Color c)
        {
            flashColor = c;
            flashT = 1f;
        }

        // ------------------------------------------------------------------ actualización

        void Update()
        {
            if (lm == null) return;
            float dt = Time.unscaledDeltaTime;

            bankedText.text = $"{lm.Banked}<size=32> / {lm.lootGoal}</size>" + (lm.GoalMet ? " <color=#7CFF7C><size=30>¡OK!</size></color>" : "");
            var p = lm.player;
            if (p != null)
            {
                carriedText.text = p.carried.Count > 0 ? $"En la mochila: <color=#FFE14D>+{p.CarriedValue}</color> ({p.carried.Count} obj.)" : "Mochila vacía: ¡a robar!";
                backpackText.text = $"MOCHILA {p.CarriedWeight}/{p.capacity}" + (p.IsHeavy ? "   <color=#FFB347>¡vas lento!</color>" : "");
                for (int i = 0; i < backpackSegs.Length; i++)
                {
                    bool full = i < p.CarriedWeight;
                    Color c = !full ? new Color(1f, 1f, 1f, 0.15f) : (p.CarriedWeight >= p.capacity ? Bad : (i >= p.normalSpeedCapacity ? new Color(1f, 0.6f, 0.2f) : Good));
                    backpackSegs[i].color = c;
                }
                hiddenOverlay.gameObject.SetActive(p.IsHidden);
                hiddenText.text = p.IsHidden && lm.State == LevelState.Playing ? "Escondido... nadie puede verte" : "";
            }
            if (specialText != null)
                specialText.text = lm.SpecialBanked ? $"{lm.specialItemName}: ¡guardado!" :
                    (p != null && p.CarryingSpecial ? $"{lm.specialItemName}: ¡llévalo a la salida!" : $"Objeto especial: {lm.specialItemName} (falta)");
            int t = Mathf.FloorToInt(lm.Elapsed);
            timeText.text = $"{t / 60}:{t % 60:00}";

            var noise = NoiseSystem.I;
            float level = noise != null ? noise.Level : 0f;
            float k = Mathf.Clamp01(level / 100f);
            noiseFill.anchorMax = new Vector2(1f, Mathf.Max(0.03f, k));
            Color nc = level < 30f ? Good : (level < 60f ? Warn : (level < 99f ? new Color(1f, 0.55f, 0.2f) : Bad));
            noiseFillImg.color = nc;
            noiseStatus.color = nc;
            noiseStatus.text = level < 5f ? "Silencio" : (level < 30f ? "Poco ruido" : (level < 60f ? "¡Te pueden oír!" : (level < 99f ? "¡Te buscan!" : "¡ALERTA!")));
            noiseStatus.rectTransform.anchoredPosition = new Vector2(160f, -130f - 157f + 340f * k);
            noiseShake.intensity = level >= 60f ? (level >= 99f ? 6f : 3f) : 0f;

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
                if (life < 0.35f)
                {
                    var cg = go.GetComponent<CanvasGroup>();
                    if (cg == null) cg = go.AddComponent<CanvasGroup>();
                    cg.alpha = life / 0.35f;
                }
            }
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var (go, life) = pops[i];
                life -= dt;
                if (go == null || life <= 0f)
                {
                    if (go != null) Destroy(go);
                    pops.RemoveAt(i);
                    continue;
                }
                pops[i] = (go, life);
                if (life < 0.35f)
                {
                    var cg = go.GetComponent<CanvasGroup>();
                    if (cg == null) cg = go.AddComponent<CanvasGroup>();
                    cg.alpha = life / 0.35f;
                    go.transform.localScale = Vector3.one * (1f + (0.35f - life) * 0.8f);
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
                        iconPool.Add(UIFactory.Label(iconRootRt, "", 52, Color.white, TextAnchor.MiddleCenter, C, Vector2.zero, new Vector2(220f, 80f), FontStyle.Bold));
                    var icon = iconPool[used++];
                    icon.gameObject.SetActive(true);
                    icon.text = txt;
                    icon.color = w.IconColor;
                    bool alarm = txt.Contains("!");
                    icon.fontSize = txt == "Zzz" ? 34 : (alarm ? 60 : 52);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var local);
                    var rt = icon.rectTransform;
                    float bob = txt == "Zzz" ? Mathf.Sin(Time.time * 2f) * 8f : Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 10f;
                    rt.anchoredPosition = local + new Vector2(0f, bob);
                    float s = alarm ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.15f : 1f;
                    rt.localScale = new Vector3(s, s, 1f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 4f + used) * 8f);
                }
            }
            for (int i = used; i < iconPool.Count; i++) iconPool[i].gameObject.SetActive(false);

            // Cartel sobre la salida.
            if (lm.exit != null && cam != null)
            {
                if (exitIcon == null) exitIcon = UIFactory.Label(iconRootRt, "SALIDA", 30, Good, TextAnchor.MiddleCenter, C, Vector2.zero, new Vector2(300f, 50f));
                Vector3 sp = cam.WorldToScreenPoint(lm.exit.transform.position + Vector3.up * 1.9f);
                bool show = sp.z > 0f && lm.State == LevelState.Playing;
                exitIcon.gameObject.SetActive(show);
                if (show)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var local);
                    exitIcon.rectTransform.anchoredPosition = local + Vector2.up * Mathf.Sin(Time.time * 3f) * 6f;
                    exitIcon.color = lm.GoalMet ? Good : Warn;
                    exitIcon.text = lm.GoalMet ? "¡SALIDA! (E)" : "SALIDA";
                }
            }
        }
    }
}
