using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RobaYCorre
{
    /// <summary>Crea elementos de interfaz (uGUI) por código con un estilo común.</summary>
    public static class UIFactory
    {
        static Font font;
        static Sprite rounded, star, circle;

        public static readonly Color PanelColor = new Color(0.1f, 0.09f, 0.16f, 0.86f);
        public static readonly Color ButtonColor = new Color(0.98f, 0.62f, 0.2f, 1f);
        public static readonly Color ButtonAlt = new Color(0.35f, 0.4f, 0.6f, 1f);
        public static readonly Color TextLight = new Color(1f, 0.97f, 0.9f, 1f);
        public static readonly Color Gold = new Color(1f, 0.83f, 0.25f, 1f);

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Rounded
        {
            get
            {
                if (rounded == null)
                {
                    const int s = 64; const float r = 18f;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                    var px = new Color32[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float cx = Mathf.Clamp(x + 0.5f, r, s - r);
                            float cy = Mathf.Clamp(y + 0.5f, r, s - r);
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                            float a = Mathf.Clamp01(r - d + 0.5f);
                            px[y * s + x] = new Color(1f, 1f, 1f, a);
                        }
                    tex.SetPixels32(px);
                    tex.Apply();
                    rounded = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
                }
                return rounded;
            }
        }

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

        public static Sprite Star
        {
            get
            {
                if (star == null)
                {
                    const int s = 128;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                    var pts = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float rad = (i % 2 == 0 ? 0.48f : 0.2f) * s;
                        pts[i] = new Vector2(s / 2f + Mathf.Cos(ang) * rad, s / 2f + Mathf.Sin(ang) * rad - s * 0.03f);
                    }
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            // Antialias sencillo con 4 muestras.
                            float cov = 0f;
                            for (int k = 0; k < 4; k++)
                            {
                                var p = new Vector2(x + 0.25f + 0.5f * (k % 2), y + 0.25f + 0.5f * (k / 2));
                                if (Inside(pts, p)) cov += 0.25f;
                            }
                            tex.SetPixel(x, y, new Color(1, 1, 1, cov));
                        }
                    tex.Apply();
                    star = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return star;
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
            var rt = Rect(go, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool roundedCorners = true)
        {
            var go = new GameObject(name);
            Rect(go, parent, anchor, anchor, anchor, pos, size);
            var img = go.AddComponent<Image>();
            img.color = color;
            if (roundedCorners) { img.sprite = Rounded; img.type = UnityEngine.UI.Image.Type.Sliced; }
            return img;
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
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (shadow)
            {
                var sh = go.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
                sh.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static Button Button(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color, UnityAction onClick, int fontSize = 34)
        {
            var go = new GameObject("Boton_" + text);
            Rect(go, parent, anchor, anchor, anchor, pos, size);
            var img = go.AddComponent<Image>();
            img.sprite = Rounded;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = Color.white;
            var b = go.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = color;
            cb.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            cb.selectedColor = Color.Lerp(color, Color.white, 0.15f);
            cb.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.8f);
            b.colors = cb;
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); onClick(); });
            var label = Label(go.transform, text, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size, FontStyle.Bold);
            label.name = "Texto";
            return b;
        }

        public static Slider Slider(Transform parent, string label, float value, Vector2 anchor, Vector2 pos, UnityAction<float> onChange)
        {
            var root = new GameObject("Slider_" + label);
            Rect(root, parent, anchor, anchor, anchor, pos, new Vector2(560f, 50f));
            Label(root.transform, label, 28, TextLight, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(180f, 50f));

            var sGo = new GameObject("Barra");
            Rect(sGo, root.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(360f, 28f));
            var slider = sGo.AddComponent<Slider>();

            var bg = UIFactory.Image(sGo.transform, "Fondo", new Color(1f, 1f, 1f, 0.2f), Rounded);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.raycastTarget = true;
            Stretch(bg.gameObject, sGo.transform);

            var fillArea = new GameObject("Relleno");
            var faRt = Stretch(fillArea, sGo.transform);
            faRt.offsetMin = new Vector2(6f, 4f); faRt.offsetMax = new Vector2(-6f, -4f);
            var fill = UIFactory.Image(fillArea.transform, "Fill", ButtonColor, Rounded);
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            var fillRt = Stretch(fill.gameObject, fillArea.transform);

            var handleArea = new GameObject("Asa");
            var haRt = Stretch(handleArea, sGo.transform);
            haRt.offsetMin = new Vector2(12f, 0f); haRt.offsetMax = new Vector2(-12f, 0f);
            var handle = UIFactory.Image(handleArea.transform, "Handle", Color.white, Circle);
            handle.raycastTarget = true;
            var hRt = handle.rectTransform;
            hRt.sizeDelta = new Vector2(40f, 40f);

            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(onChange);
            return slider;
        }
    }
}
