using UnityEngine;
using UnityEngine.UI;

namespace RobaYCorre
{
    /// <summary>Menú principal: título animado, Jugar → tarjetas de niveles, controles y opciones de sonido.</summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("Mapache decorativo que gira en el fondo")]
        public Transform displayRaccoon;
        public Transform[] spinningLoot;

        GameObject mainPanel, levelPanel, controlsPanel, optionsPanel;

        static readonly Color[] CardColors =
        {
            new Color(0.3f, 0.8f, 0.4f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.6f, 0.15f),
            new Color(0.7f, 0.35f, 1f), new Color(1f, 0.32f, 0.5f),
        };

        void Start()
        {
            Time.timeScale = 1f;
            var _ = AudioManager.I;
            Build();
            Show(GameProgress.OpenLevelSelect ? levelPanel : mainPanel);
            GameProgress.OpenLevelSelect = false;
            if (displayRaccoon != null)
            {
                var ctrl = displayRaccoon.GetComponent<RaccoonController>();
                if (ctrl != null) ctrl.enabled = false;
                var cc = displayRaccoon.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
            }
        }

        void Update()
        {
            if (displayRaccoon != null)
            {
                displayRaccoon.Rotate(0f, 25f * Time.deltaTime, 0f);
                var vis = displayRaccoon.GetComponentInChildren<RaccoonVisual>();
                if (vis != null) vis.SetMotion(0.25f + 0.2f * Mathf.Sin(Time.time * 0.5f), false, false);
            }
            if (spinningLoot != null)
                foreach (var l in spinningLoot)
                    if (l != null) l.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            if (GameInput.PauseDown && !mainPanel.activeSelf) Show(mainPanel);
        }

        void Show(GameObject panel)
        {
            foreach (var p in new[] { mainPanel, levelPanel, controlsPanel, optionsPanel })
                if (p != null) p.SetActive(p == panel);
            // Reinicia las animaciones de entrada.
            if (panel != null)
                foreach (var pop in panel.GetComponentsInChildren<UIPop>(true)) pop.Restart(pop.delay);
        }

        static void PopIn(Component c, float delay, bool elastic = false)
        {
            var p = c.gameObject.AddComponent<UIPop>();
            p.delay = delay;
            p.elastic = elastic;
            if (elastic) p.duration = 0.75f;
        }

        void Build()
        {
            var canvas = UIFactory.CreateCanvas("Menu_Canvas", 0);
            var root = canvas.transform;
            var C = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);

            // Fondo: degradado lateral y rayos girando detrás del título.
            var shade = UIFactory.Image(root, "Degradado", new Color(0.22f, 0.08f, 0.5f, 0.75f), UIFactory.SideFade);
            var srt = shade.rectTransform;
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(0.62f, 1f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var rays = UIFactory.Image(root, "Rayos", new Color(1f, 0.85f, 0.3f, 0.22f), UIFactory.Burst);
            rays.rectTransform.anchorMin = rays.rectTransform.anchorMax = new Vector2(0.27f, 0.72f);
            rays.rectTransform.sizeDelta = new Vector2(1500f, 1500f);
            rays.gameObject.AddComponent<UISpin>().speed = 10f;

            // ---- Principal ----
            mainPanel = new GameObject("Principal");
            UIFactory.Stretch(mainPanel, root);
            var m = mainPanel.transform;
            var leftCol = new Vector2(0.27f, 0.5f);
            var t1 = UIFactory.Label(m, "ROBA", 150, UIFactory.Gold, TextAnchor.MiddleCenter, leftCol, new Vector2(-40f, 360f), new Vector2(900f, 170f), FontStyle.Bold);
            t1.font = UIFactory.TitleFont;
            t1.gameObject.AddComponent<UIWobble>().angle = 4f;
            PopIn(t1, 0.05f, true);
            var t2 = UIFactory.Label(m, "Y CORRE", 120, Color.white, TextAnchor.MiddleCenter, leftCol, new Vector2(40f, 225f), new Vector2(900f, 150f), FontStyle.Bold);
            t2.font = UIFactory.TitleFont;
            var w2 = t2.gameObject.AddComponent<UIWobble>();
            w2.angle = 4f; w2.phase = 1.5f;
            PopIn(t2, 0.2f, true);
            var sub = UIFactory.Panel(m, "Subtitulo", UIFactory.ButtonPink, leftCol, new Vector2(0f, 120f), new Vector2(620f, 60f));
            sub.transform.parent.localRotation = Quaternion.Euler(0f, 0f, -2f);
            UIFactory.Label(sub.transform, "Un mapache, una noche y mucho botín", 26, Color.white, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(600f, 56f));
            PopIn(sub.transform.parent, 0.35f);

            var play = UIFactory.Button(m, "¡JUGAR!", leftCol, new Vector2(0f, -10f), new Vector2(470f, 120f), UIFactory.ButtonColor, () => Show(levelPanel), 64);
            play.GetComponentInChildren<Text>().color = Color.white;
            PopIn(play, 0.45f, true);
            PopIn(UIFactory.Button(m, "Controles", leftCol, new Vector2(-122f, -150f), new Vector2(230f, 80f), UIFactory.ButtonAlt, () => Show(controlsPanel), 30), 0.55f);
            PopIn(UIFactory.Button(m, "Opciones", leftCol, new Vector2(122f, -150f), new Vector2(230f, 80f), UIFactory.ButtonAlt, () => Show(optionsPanel), 30), 0.6f);
            PopIn(UIFactory.Button(m, "Salir", leftCol, new Vector2(0f, -250f), new Vector2(230f, 70f), UIFactory.ButtonRed, Quit, 28), 0.65f);

            var starsChip = UIFactory.Panel(m, "Estrellas", UIFactory.PanelColor, leftCol, new Vector2(0f, -350f), new Vector2(320f, 64f));
            var st = UIFactory.Image(starsChip.transform, "Estrella", UIFactory.Gold, UIFactory.Star);
            st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            st.rectTransform.anchoredPosition = new Vector2(46f, 2f);
            st.rectTransform.sizeDelta = new Vector2(58f, 58f);
            st.gameObject.AddComponent<UIWobble>().angle = 12f;
            UIFactory.Label(starsChip.transform, $"{GameProgress.TotalStars} / {GameProgress.LevelCount * 3}", 32, Color.white, TextAnchor.MiddleCenter, C, new Vector2(30f, 2f), new Vector2(220f, 56f));
            PopIn(starsChip.transform.parent, 0.75f);

            // ---- Selección de niveles: tarjetas ----
            levelPanel = new GameObject("Niveles");
            UIFactory.Stretch(levelPanel, root);
            var lp = levelPanel.transform;
            var dim = UIFactory.Image(lp, "Oscuro", new Color(0.08f, 0.03f, 0.2f, 0.55f));
            UIFactory.Stretch(dim.gameObject, lp);
            var lt = UIFactory.Label(lp, "ELIGE UNA CASA", 84, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -40f), new Vector2(1200f, 110f), FontStyle.Bold);
            lt.gameObject.AddComponent<UIWobble>().angle = 2f;
            PopIn(lt, 0f, true);
            string[] blurbs =
            {
                "Un habitante dormido. Aprende a robar y escapar.",
                "Desorden y cosas frágiles. ¡Cuidado al correr!",
                "Un perro patrulla la casa. Usa los escondites.",
                "Cámaras, sensores y dos habitantes.",
                "Habitantes, perro, vigilancia y un joyero especial.",
            };
            int unlocked = GameProgress.UnlockedLevels;
            for (int i = 0; i < GameProgress.LevelCount; i++)
            {
                int lvl = i + 1;
                bool open = lvl <= unlocked;
                float x = (i - 2) * 350f;
                Color col = open ? CardColors[i] : new Color(0.45f, 0.45f, 0.55f);
                var card = UIFactory.Panel(lp, "Nivel" + lvl, col, C, new Vector2(x, 20f), new Vector2(320f, 520f));
                var cardRoot = card.transform.parent;
                cardRoot.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 2f : -2f));
                var ct = card.transform;
                var num = UIFactory.Label(ct, open ? lvl.ToString() : "?", 140, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -10f), new Vector2(300f, 170f), FontStyle.Bold);
                num.font = UIFactory.TitleFont;
                var nm = UIFactory.Panel(ct, "Titulo", UIFactory.PanelDark, top, new Vector2(0f, -190f), new Vector2(290f, 92f));
                UIFactory.Label(nm.transform, GameProgress.LevelTitles[i], 26, Color.white, TextAnchor.MiddleCenter, C, new Vector2(0f, 2f), new Vector2(280f, 88f));
                UIFactory.Label(ct, open ? blurbs[i] : "Completa el nivel anterior para desbloquearlo", 21, Color.white, TextAnchor.UpperCenter, top, new Vector2(0f, -296f), new Vector2(280f, 90f));
                int stars = GameProgress.GetStars(lvl);
                for (int s = 0; s < 3; s++)
                {
                    var sImg = UIFactory.Image(ct, "Estrella", s < stars ? UIFactory.Gold : new Color(0.25f, 0.2f, 0.4f, 0.9f), UIFactory.Star);
                    sImg.rectTransform.anchorMin = sImg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    sImg.rectTransform.anchoredPosition = new Vector2((s - 1) * 62f, 140f + (s == 1 ? 8f : 0f));
                    sImg.rectTransform.sizeDelta = new Vector2(58f, 58f);
                }
                var b = UIFactory.Button(ct, open ? "JUGAR" : "BLOQUEADO", new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(250f, 78f), open ? UIFactory.ButtonColor : UIFactory.ButtonAlt,
                    open ? (UnityEngine.Events.UnityAction)(() => GameProgress.LoadLevel(lvl)) : null, open ? 34 : 24);
                b.interactable = open;
                if (open) cardRoot.gameObject.AddComponent<UIFloat>().phase = i;
                PopIn(cardRoot, 0.15f + i * 0.1f, true);
            }
            PopIn(UIFactory.Button(lp, "Volver", new Vector2(0.5f, 0f), new Vector2(-180f, 50f), new Vector2(280f, 78f), UIFactory.ButtonAlt, () => Show(mainPanel), 32), 0.7f);
            PopIn(UIFactory.Button(lp, "Borrar progreso", new Vector2(0.5f, 0f), new Vector2(180f, 50f), new Vector2(320f, 78f), UIFactory.ButtonRed, () =>
            {
                GameProgress.ResetProgress();
                GameProgress.OpenLevelSelect = true;
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameProgress.MenuScene);
            }, 28), 0.75f);

            // ---- Controles ----
            controlsPanel = new GameObject("Controles");
            UIFactory.Stretch(controlsPanel, root);
            var cp = UIFactory.Panel(controlsPanel.transform, "Panel", UIFactory.PanelColor, C, Vector2.zero, new Vector2(1060f, 860f));
            PopIn(cp.transform.parent, 0f, true);
            var ctitle = UIFactory.Label(cp.transform, "CONTROLES", 76, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -24f), new Vector2(900f, 100f), FontStyle.Bold);
            ctitle.gameObject.AddComponent<UIWobble>().angle = 2f;
            var grid = UIFactory.Group(cp.transform, "Teclas", top, new Vector2(0f, -150f), new Vector2(860f, 280f));
            for (int i = 0; i < HUD.Controls.Length; i++)
            {
                int col = i < 4 ? 0 : 1;
                int row = i < 4 ? i : i - 4;
                var (key, action) = HUD.Controls[i];
                float w = key.Length > 2 ? 104f : 64f;
                var kc = UIFactory.KeyCap(grid, key, new Vector2(0f, 1f), new Vector2(col * 440f, -row * 66f), w);
                PopIn(kc, 0.2f + i * 0.06f);
                UIFactory.Label(grid, action, 26, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(col * 440f + w + 14f, -row * 66f - 4f), new Vector2(310f, 52f));
            }
            var rules = UIFactory.Panel(cp.transform, "Reglas", UIFactory.PanelDark, top, new Vector2(0f, -450f), new Vector2(960f, 250f));
            UIFactory.Label(rules.transform,
                "Roba comida y objetos, guárdalos en la salida y escapa cuando tengas el botín del nivel.\n" +
                "La barra de RUIDO sube al correr, chocar o recoger cosas ruidosas: con 30 alguien viene a mirar, con 60 te buscan y si te ven, te persiguen.\n" +
                "Escóndete con Q antes de que te vean. La mochila tiene 10 de espacio; con más de 6 vas lento.",
                23, Color.white, TextAnchor.MiddleCenter, C, Vector2.zero, new Vector2(920f, 240f));
            PopIn(UIFactory.Button(cp.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(280f, 80f), UIFactory.ButtonColor, () => Show(mainPanel)), 0.6f);

            // ---- Opciones ----
            optionsPanel = new GameObject("Opciones");
            UIFactory.Stretch(optionsPanel, root);
            var op = UIFactory.Panel(optionsPanel.transform, "Panel", UIFactory.PanelColor, C, Vector2.zero, new Vector2(820f, 560f));
            PopIn(op.transform.parent, 0f, true);
            var otitle = UIFactory.Label(op.transform, "OPCIONES", 76, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -24f), new Vector2(700f, 100f), FontStyle.Bold);
            otitle.gameObject.AddComponent<UIWobble>().angle = 2f;
            UIFactory.Slider(op.transform, "Música", GameProgress.MusicVolume, top, new Vector2(0f, -170f), v => GameProgress.MusicVolume = v);
            UIFactory.Slider(op.transform, "Efectos", GameProgress.SfxVolume, top, new Vector2(0f, -245f), v => { GameProgress.SfxVolume = v; AudioManager.Play(Sfx.Coin, 0.6f); });
            Button mute = null;
            mute = UIFactory.Button(op.transform, GameProgress.Muted ? "Activar sonido" : "Silenciar todo", top, new Vector2(0f, -330f), new Vector2(360f, 72f), UIFactory.ButtonPink, () =>
            {
                GameProgress.Muted = !GameProgress.Muted;
                mute.GetComponentInChildren<Text>().text = GameProgress.Muted ? "Activar sonido" : "Silenciar todo";
            }, 30);
            PopIn(UIFactory.Button(op.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(280f, 80f), UIFactory.ButtonColor, () => Show(mainPanel)), 0.3f);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
