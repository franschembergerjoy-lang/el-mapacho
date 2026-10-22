using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RobaYCorre
{
    /// <summary>
    /// Crea la interfaz (uGUI) por código con un estilo "de fiesta": botones gorditos con borde oscuro,
    /// degradado y sombra, letras gruesas con contorno y animaciones con rebote (ver UIJuice).
    /// </summary>
    public static class UIFactory
    {
        static Font font, titleFont;
        static Sprite rounded, chunky, solid, star, circle, burst, coin;

        // Paleta
        public static readonly Color Ink = new Color(0.09f, 0.06f, 0.22f, 1f);          // contornos
        public static readonly Color PanelColor = new Color(0.27f, 0.16f, 0.62f, 0.95f);  // morado
        public static readonly Color PanelDark = new Color(0.16f, 0.09f, 0.4f, 0.92f);
        public static readonly Color ButtonColor = new Color(1f, 0.8f, 0.12f, 1f);       // amarillo
        public static readonly Color ButtonAlt = new Color(0.2f, 0.58f, 1f, 1f);          // azul
        public static readonly Color ButtonGreen = new Color(0.32f, 0.86f, 0.32f, 1f);
        public static readonly Color ButtonPink = new Color(1f, 0.36f, 0.62f, 1f);
        public static readonly Color ButtonRed = new Color(1f, 0.32f, 0.3f, 1f);
        public static readonly Color TextLight = new Color(1f, 0.98f, 0.92f, 1f);
        public static readonly Color Gold = new Color(1f, 0.85f, 0.15f, 1f);

        /// <summary>Letra redonda y gruesa del sistema (Arial Rounded / Segoe UI Black), con alternativa integrada.</summary>
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    try { font = Font.CreateDynamicFontFromOSFont(new[] { "Arial Rounded MT Bold", "Segoe UI Black", "Arial Black", "Arial" }, 32); }
                    catch { font = null; }
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static Font TitleFont
        {
            get
            {
                if (titleFont == null)
                {
                    try { titleFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI Black", "Arial Black", "Arial Rounded MT Bold", "Arial" }, 48); }
                    catch { titleFont = null; }
                    if (titleFont == null) titleFont = Font;
                }
                return titleFont;
            }
        }

        // ---------------------------------------------------------------- sprites procedurales

        static Sprite MakeRounded(int size, float radius, bool gradient, bool gloss)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    float v = 1f;
                    if (gradient)
                    {
                        float t = y / (float)(size - 1);           // 0 abajo, 1 arriba
                        v = Mathf.Lerp(0.78f, 1f, t);
                        if (gloss && t > 0.62f && t < 0.9f && x > radius * 0.6f && x < size - radius * 0.6f) v = Mathf.Min(1.18f, v + 0.12f);
                        if (t < 0.12f) v *= 0.9f;
                    }
                    px[y * size + x] = new Color(Mathf.Min(1f, v), Mathf.Min(1f, v), Mathf.Min(1f, v), a);
                }
            tex.SetPixels(px);
            tex.Apply();
            float b = radius + 2f;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        /// <summary>Rectángulo redondeado liso (para fondos y bordes).</summary>
        public static Sprite Rounded => rounded != null ? rounded : (rounded = MakeRounded(64, 18f, false, false));
        /// <summary>Rectángulo redondeado con degradado y brillo (botones y tarjetas).</summary>
        public static Sprite Chunky => chunky != null ? chunky : (chunky = MakeRounded(96, 30f, true, true));
        public static Sprite Solid => solid != null ? solid : (solid = MakeRounded(96, 34f, false, false));

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    const int s = 64;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f));
                            tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(s / 2f - d)));
                        }
                    tex.Apply();
                    circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return circle;
            }
        }

        /// <summary>Moneda dorada con borde oscuro.</summary>
        public static Sprite Coin
        {
            get
            {
                if (coin == null)
                {
                    const int s = 96;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                    var c = new Vector2(s / 2f, s / 2f);
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                            float a = Mathf.Clamp01(s / 2f - d);
                            Color col;
                            if (d > s / 2f - 7f) col = Ink;
                            else if (d > s / 2f - 15f) col = new Color(1f, 0.68f, 0.05f);
                            else col = Color.Lerp(new Color(1f, 0.95f, 0.45f), new Color(1f, 0.78f, 0.1f), (s - y) / (float)s);
                            if (d < 14f && d > 7f) col = new Color(1f, 0.68f, 0.05f);
                            col.a = a;
                            tex.SetPixel(x, y, col);
                        }
                    tex.Apply();
                    coin = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return coin;
            }
        }

        public static Sprite Star
        {
            get
            {
                if (star == null)
                {
                    const int s = 128;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                    var outer = StarPoly(s, 0.48f, 0.22f);
                    var inner = StarPoly(s, 0.39f, 0.175f);
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float covO = 0f, covI = 0f;
                            for (int k = 0; k < 4; k++)
                            {
                                var p = new Vector2(x + 0.25f + 0.5f * (k % 2), y + 0.25f + 0.5f * (k / 2));
                                if (Inside(outer, p)) covO += 0.25f;
                                if (Inside(inner, p)) covI += 0.25f;
                            }
                            // Borde oscuro, interior blanco (se tiñe con el color de la imagen).
                            float g = Mathf.Lerp(0.82f, 1f, y / (float)s);
                            Color col = Color.Lerp(new Color(0.25f, 0.2f, 0.4f), new Color(g, g, g), covI);
                            col.a = covO;
                            tex.SetPixel(x, y, col);
                        }
                    tex.Apply();
                    star = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return star;
            }
        }

        static Vector2[] StarPoly(int s, float rOut, float rIn)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0 ? rOut : rIn) * s;
                pts[i] = new Vector2(s / 2f + Mathf.Cos(ang) * rad, s / 2f + Mathf.Sin(ang) * rad - s * 0.03f);
            }
            return pts;
        }

        static Sprite sideFade;

        /// <summary>Degradado horizontal suave (opaco a la izquierda, transparente a la derecha).</summary>
        public static Sprite SideFade
        {
            get
            {
                if (sideFade == null)
                {
                    const int w = 256, h = 4;
                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                    for (int x = 0; x < w; x++)
                    {
                        float t = x / (float)(w - 1);
                        float a = 1f - Mathf.SmoothStep(0.35f, 1f, t);
                        for (int y = 0; y < h; y++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                    tex.Apply();
                    sideFade = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }
                return sideFade;
            }
        }

        /// <summary>Rayos de sol (para fondos que giran).</summary>
        public static Sprite Burst
        {
            get
            {
                if (burst == null)
                {
                    const int s = 512;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float dx = x - s / 2f, dy = y - s / 2f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy) / (s / 2f);
                            float ang = Mathf.Atan2(dy, dx);
                            // Borde suavizado de cada rayo (sin escalones).
                            float ray = Mathf.Clamp01(Mathf.Sin(ang * 12f) * Mathf.Max(8f, d * 60f) + 0.5f);
                            float a = ray * Mathf.Clamp01(1f - d) * Mathf.Clamp01(d * 6f);
                            tex.SetPixel(x, y, new Color(1, 1, 1, a));
                        }
                    tex.Apply();
                    burst = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return burst;
            }
        }

        static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }

        // ---------------------------------------------------------------- estructura

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(GameObject go, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(GameObject go, Transform parent)
        {
            return Rect(go, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        /// <summary>Contenedor vacío (pivote y ancla en el mismo punto).</summary>
        public static RectTransform Group(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            return Rect(go, parent, anchor, anchor, anchor, pos, size);
        }

        /// <summary>
        /// Tarjeta "gordita": sombra + borde oscuro + cuerpo con degradado. Devuelve la imagen del cuerpo
        /// (los hijos se añaden ahí). El objeto raíz es body.transform.parent.
        /// </summary>
        public static Image Panel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool chunkyStyle = true)
        {
            var root = new GameObject(name);
            var rt = Rect(root, parent, anchor, anchor, anchor, pos, size);
            if (!chunkyStyle)
            {
                var flat = root.AddComponent<Image>();
                flat.sprite = Rounded;
                flat.type = UnityEngine.UI.Image.Type.Sliced;
                flat.color = color;
                return flat;
            }
            var shadow = Image(rt, "Sombra", new Color(0.05f, 0.02f, 0.15f, 0.22f * color.a), Solid);
            shadow.type = UnityEngine.UI.Image.Type.Sliced;
            Fill(shadow.rectTransform, new Vector2(0f, -7f), new Vector2(0f, -1f));
            var border = Image(rt, "Borde", Ink * new Color(1, 1, 1, Mathf.Max(0.6f, color.a)), Solid);
            border.type = UnityEngine.UI.Image.Type.Sliced;
            Fill(border.rectTransform, new Vector2(-4f, -4f), new Vector2(4f, 4f));
            var body = Image(rt, "Cuerpo", color, Chunky);
            body.type = UnityEngine.UI.Image.Type.Sliced;
            body.raycastTarget = true;
            Fill(body.rectTransform, Vector2.zero, Vector2.zero);
            return body;
        }

        static void Fill(RectTransform rt, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>().SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 boxSize, FontStyle style = FontStyle.Normal, bool shadow = true)
        {
            var go = new GameObject("Texto");
            Rect(go, parent, anchor, anchor, anchor, pos, boxSize);
            var t = go.AddComponent<Text>();
            t.font = style == FontStyle.Bold && size >= 40 ? TitleFont : Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (shadow)
            {
                // Contorno fino y nítido, sin sombra desplazada.
                float w = Mathf.Clamp(size / 22f, 1f, 2.6f);
                var ol = go.AddComponent<Outline>();
                ol.effectColor = new Color(Ink.r, Ink.g, Ink.b, 0.85f);
                ol.effectDistance = new Vector2(w, w);
                ol.useGraphicAlpha = true;
            }
            return t;
        }

        /// <summary>Texto con contorno para colocar sobre el mundo (iconos de "!" y "?").</summary>
        public static Text BigLabel(Transform parent, string text, int size, Color color)
        {
            var t = Label(parent, text, size, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, size * 1.6f), FontStyle.Bold);
            return t;
        }

        public static Button Button(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color, UnityAction onClick, int fontSize = 34)
        {
            var body = Panel(parent, "Boton_" + text, color, anchor, pos, size);
            var root = body.transform.parent.gameObject;
            body.color = Color.white;
            var b = root.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = color;
            cb.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            cb.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            cb.selectedColor = color;
            cb.disabledColor = new Color(0.55f, 0.55f, 0.6f, 1f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.targetGraphic = body;
            if (onClick != null) b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); onClick(); });
            var label = Label(root.transform, text, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), size, FontStyle.Bold);
            label.font = TitleFont;
            label.name = "Texto";
            root.AddComponent<UIButtonJuice>();
            return b;
        }

        public static Slider Slider(Transform parent, string label, float value, Vector2 anchor, Vector2 pos, UnityAction<float> onChange)
        {
            var root = new GameObject("Slider_" + label);
            Rect(root, parent, anchor, anchor, anchor, pos, new Vector2(560f, 56f));
            Label(root.transform, label, 28, TextLight, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(180f, 50f));

            var sGo = new GameObject("Barra");
            Rect(sGo, root.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(360f, 30f));
            var slider = sGo.AddComponent<Slider>();

            var bg = Image(sGo.transform, "Fondo", Ink, Solid);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.raycastTarget = true;
            Fill(bg.rectTransform, new Vector2(-4f, -4f), new Vector2(4f, 4f));

            var fillArea = new GameObject("Relleno");
            var faRt = Stretch(fillArea, sGo.transform);
            faRt.offsetMin = new Vector2(2f, 2f); faRt.offsetMax = new Vector2(-2f, -2f);
            var fill = Image(fillArea.transform, "Fill", ButtonColor, Chunky);
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            var fillRt = Stretch(fill.gameObject, fillArea.transform);

            var handleArea = new GameObject("Asa");
            var haRt = Stretch(handleArea, sGo.transform);
            haRt.offsetMin = new Vector2(14f, 0f); haRt.offsetMax = new Vector2(-14f, 0f);
            var handleBorder = Image(handleArea.transform, "HandleBorde", Ink, Circle);
            var handle = Image(handleBorder.transform, "Handle", Color.white, Circle);
            handle.rectTransform.anchorMin = Vector2.zero; handle.rectTransform.anchorMax = Vector2.one;
            handle.rectTransform.offsetMin = new Vector2(5f, 5f); handle.rectTransform.offsetMax = new Vector2(-5f, -5f);
            handleBorder.raycastTarget = true;
            handleBorder.rectTransform.sizeDelta = new Vector2(48f, 48f);

            slider.fillRect = fillRt;
            slider.handleRect = handleBorder.rectTransform;
            slider.targetGraphic = handleBorder;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        /// <summary>Tecla dibujada (para mostrar controles).</summary>
        public static RectTransform KeyCap(Transform parent, string key, Vector2 anchor, Vector2 pos, float width = 64f)
        {
            var body = Panel(parent, "Tecla_" + key, new Color(0.95f, 0.95f, 1f), anchor, pos, new Vector2(width, 58f));
            var t = Label(body.transform, key, key.Length > 3 ? 20 : 26, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(width, 50f), FontStyle.Normal, false);
            t.font = TitleFont;
            return (RectTransform)body.transform.parent;
        }
    }
}
