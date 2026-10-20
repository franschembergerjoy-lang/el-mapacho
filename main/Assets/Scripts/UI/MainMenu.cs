using UnityEngine;
using UnityEngine.UI;

namespace RobaYCorre
{
    /// <summary>Menú principal: título, Jugar (selección de niveles), controles, opciones de sonido.</summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("Mapache decorativo que gira en el fondo")]
        public Transform displayRaccoon;
        public Transform[] spinningLoot;

        GameObject mainPanel, levelPanel, controlsPanel, optionsPanel;

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
        }

        void Build()
        {
            var canvas = UIFactory.CreateCanvas("Menu_Canvas", 0);
            var root = canvas.transform;
            var C = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);

            // ---- Principal ----
            mainPanel = new GameObject("Principal");
            UIFactory.Stretch(mainPanel, root);
            var m = mainPanel.transform;
            var leftCol = new Vector2(0.27f, 0.5f);
            UIFactory.Label(m, "Roba y corre", 120, UIFactory.Gold, TextAnchor.MiddleCenter, leftCol, new Vector2(0f, 300f), new Vector2(900f, 150f), FontStyle.Bold);
            UIFactory.Label(m, "Un mapache, una noche y mucho botín", 34, Color.white, TextAnchor.MiddleCenter, leftCol, new Vector2(0f, 205f), new Vector2(900f, 50f));
            UIFactory.Button(m, "Jugar", leftCol, new Vector2(0f, 70f), new Vector2(420f, 90f), UIFactory.ButtonColor, () => Show(levelPanel), 44);
            UIFactory.Button(m, "Controles", leftCol, new Vector2(0f, -40f), new Vector2(420f, 76f), UIFactory.ButtonAlt, () => Show(controlsPanel));
            UIFactory.Button(m, "Opciones", leftCol, new Vector2(0f, -130f), new Vector2(420f, 76f), UIFactory.ButtonAlt, () => Show(optionsPanel));
            UIFactory.Button(m, "Salir", leftCol, new Vector2(0f, -220f), new Vector2(420f, 76f), UIFactory.ButtonAlt, Quit);
            UIFactory.Label(m, $"Estrellas: {GameProgress.TotalStars} / {GameProgress.LevelCount * 3}", 28, UIFactory.Gold, TextAnchor.MiddleCenter, leftCol, new Vector2(0f, -320f), new Vector2(600f, 40f), FontStyle.Bold);

            // ---- Selección de niveles ----
            levelPanel = new GameObject("Niveles");
            UIFactory.Stretch(levelPanel, root);
            var lp = UIFactory.Panel(levelPanel.transform, "Panel", UIFactory.PanelColor, C, Vector2.zero, new Vector2(1180f, 860f));
            UIFactory.Label(lp.transform, "Elige una casa", 64, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -40f), new Vector2(1000f, 80f), FontStyle.Bold);
            string[] blurbs =
            {
                "Un habitante dormido. Aprende a robar y escapar.",
                "Desorden y cosas frágiles. ¡Cuidado al correr!",
                "Un perro patrulla la casa. Usa los escondites.",
                "Cámaras, sensores y dos habitantes.",
                "Habitantes, perro, vigilancia y un joyero especial."
            };
            int unlocked = GameProgress.UnlockedLevels;
            for (int i = 0; i < GameProgress.LevelCount; i++)
            {
                int lvl = i + 1;
                bool open = lvl <= unlocked;
                float y = -150f - i * 125f;
                var row = UIFactory.Panel(lp.transform, "Nivel" + lvl, new Color(1f, 1f, 1f, open ? 0.08f : 0.03f), top, new Vector2(0f, y), new Vector2(1080f, 112f));
                var rt = row.transform;
                UIFactory.Label(rt, $"{lvl}", 58, open ? UIFactory.Gold : Color.gray, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(80f, 100f), FontStyle.Bold);
                UIFactory.Label(rt, GameProgress.LevelTitles[i], 34, open ? Color.white : Color.gray, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(120f, 18f), new Vector2(560f, 44f), FontStyle.Bold);
                UIFactory.Label(rt, open ? blurbs[i] : "Bloqueado: completa el nivel anterior", 22, open ? UIFactory.TextLight : Color.gray, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(120f, -22f), new Vector2(600f, 34f));
                int stars = GameProgress.GetStars(lvl);
                for (int s = 0; s < 3; s++)
                {
                    var st = UIFactory.Image(rt, "Estrella", s < stars ? UIFactory.Gold : new Color(1f, 1f, 1f, 0.15f), UIFactory.Star);
                    st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    st.rectTransform.anchoredPosition = new Vector2(-370f + s * 50f, 0f);
                    st.rectTransform.sizeDelta = new Vector2(46f, 46f);
                }
                var b = UIFactory.Button(rt, open ? "Jugar" : "Bloqueado", new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 72f), open ? UIFactory.ButtonColor : UIFactory.ButtonAlt, open ? (UnityEngine.Events.UnityAction)(() => GameProgress.LoadLevel(lvl)) : null, 30);
                b.interactable = open;
            }
            UIFactory.Button(lp.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(-150f, 30f), new Vector2(260f, 64f), UIFactory.ButtonAlt, () => Show(mainPanel), 28);
            UIFactory.Button(lp.transform, "Borrar progreso", new Vector2(0.5f, 0f), new Vector2(170f, 30f), new Vector2(300f, 64f), new Color(0.55f, 0.25f, 0.3f), () =>
            {
                GameProgress.ResetProgress();
                GameProgress.OpenLevelSelect = true;
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameProgress.MenuScene);
            }, 26);

            // ---- Controles ----
            controlsPanel = new GameObject("Controles");
            UIFactory.Stretch(controlsPanel, root);
            var cp = UIFactory.Panel(controlsPanel.transform, "Panel", UIFactory.PanelColor, C, Vector2.zero, new Vector2(1000f, 760f));
            UIFactory.Label(cp.transform, "Controles", 60, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -40f), new Vector2(900f, 70f), FontStyle.Bold);
            UIFactory.Label(cp.transform, HUD.ControlsText, 32, Color.white, TextAnchor.UpperLeft, top, new Vector2(60f, -140f), new Vector2(720f, 260f));
            UIFactory.Label(cp.transform,
                "Roba comida y objetos, guárdalos en la salida y escapa cuando tengas el botín del nivel.\n" +
                "La barra de RUIDO de la izquierda sube al correr, chocar o recoger cosas ruidosas: con 30 alguien viene a mirar, con 60 te buscan y si te ven, te persiguen.\n" +
                "Escóndete (E) antes de que te vean. La mochila tiene 10 de espacio; con más de 6 vas lento.",
                24, UIFactory.TextLight, TextAnchor.UpperLeft, top, new Vector2(0f, -420f), new Vector2(880f, 220f));
            UIFactory.Button(cp.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(260f, 70f), UIFactory.ButtonAlt, () => Show(mainPanel));

            // ---- Opciones ----
            optionsPanel = new GameObject("Opciones");
            UIFactory.Stretch(optionsPanel, root);
            var op = UIFactory.Panel(optionsPanel.transform, "Panel", UIFactory.PanelColor, C, Vector2.zero, new Vector2(800f, 520f));
            UIFactory.Label(op.transform, "Opciones", 60, UIFactory.Gold, TextAnchor.MiddleCenter, top, new Vector2(0f, -40f), new Vector2(700f, 70f), FontStyle.Bold);
            UIFactory.Slider(op.transform, "Música", GameProgress.MusicVolume, top, new Vector2(0f, -150f), v => GameProgress.MusicVolume = v);
            UIFactory.Slider(op.transform, "Efectos", GameProgress.SfxVolume, top, new Vector2(0f, -220f), v => { GameProgress.SfxVolume = v; AudioManager.Play(Sfx.Coin, 0.6f); });
            Button mute = null;
            mute = UIFactory.Button(op.transform, GameProgress.Muted ? "Activar sonido" : "Silenciar todo", top, new Vector2(0f, -300f), new Vector2(340f, 64f), UIFactory.ButtonAlt, () =>
            {
                GameProgress.Muted = !GameProgress.Muted;
                mute.GetComponentInChildren<Text>().text = GameProgress.Muted ? "Activar sonido" : "Silenciar todo";
            }, 28);
            UIFactory.Button(op.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(260f, 70f), UIFactory.ButtonAlt, () => Show(mainPanel));
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
