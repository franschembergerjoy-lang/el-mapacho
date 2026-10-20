using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RobaYCorre.EditorTools
{
    /// <summary>Paleta de colores del juego. Cada color genera un material URP (y su versión transparente para las paredes).</summary>
    public static class MatLib
    {
        public const string PaletteFolder = "Assets/Materials/Paleta";
        public const string FadeFolder = "Assets/Materials/Transparentes";
        public const string FloorFolder = "Assets/Materials/Suelos";
        public const string TexFolder = "Assets/Materials/Texturas";

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<Material, Material> fadeCache = new Dictionary<Material, Material>();

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        /// <summary>Nombre → (color, emisión)</summary>
        static readonly Dictionary<string, (Color col, float emit)> palette = new Dictionary<string, (Color, float)>
        {
            // Mapache
            {"MapacheGris", (C(0.5f, 0.5f, 0.56f), 0)}, {"MapacheOscuro", (C(0.16f, 0.16f, 0.19f), 0)}, {"MapacheCrema", (C(0.93f, 0.89f, 0.8f), 0)},
            {"Negro", (C(0.05f, 0.05f, 0.06f), 0)}, {"Blanco", (C(0.96f, 0.96f, 0.96f), 0)}, {"Mochila", (C(0.62f, 0.38f, 0.2f), 0)}, {"Rosa", (C(1f, 0.55f, 0.65f), 0)},
            // Personas
            {"Piel", (C(0.98f, 0.78f, 0.62f), 0)}, {"PielMorena", (C(0.72f, 0.5f, 0.36f), 0)}, {"PeloCastano", (C(0.38f, 0.22f, 0.12f), 0)},
            {"PeloGris", (C(0.78f, 0.78f, 0.8f), 0)}, {"PeloRubio", (C(0.96f, 0.8f, 0.4f), 0)}, {"PeloNegro", (C(0.12f, 0.1f, 0.1f), 0)},
            {"PijamaAzul", (C(0.3f, 0.5f, 0.92f), 0)}, {"PijamaAzulOscuro", (C(0.2f, 0.32f, 0.66f), 0)}, {"BataRosa", (C(0.95f, 0.5f, 0.68f), 0)},
            {"PijamaVerde", (C(0.38f, 0.74f, 0.5f), 0)}, {"BataMorada", (C(0.5f, 0.24f, 0.62f), 0)}, {"TrajeNegro", (C(0.12f, 0.12f, 0.15f), 0)},
            {"CamisaBlanca", (C(0.95f, 0.95f, 0.92f), 0)}, {"UniformeVerde", (C(0.2f, 0.42f, 0.32f), 0)}, {"Pantuflas", (C(0.85f, 0.35f, 0.35f), 0)},
            {"Linterna", (C(1f, 0.95f, 0.6f), 1.5f)},
            // Perro
            {"PerroMarron", (C(0.72f, 0.48f, 0.26f), 0)}, {"PerroOscuro", (C(0.42f, 0.26f, 0.14f), 0)}, {"PerroClaro", (C(0.95f, 0.85f, 0.7f), 0)}, {"Collar", (C(0.9f, 0.15f, 0.2f), 0)},
            // Madera y telas
            {"MaderaClara", (C(0.88f, 0.64f, 0.4f), 0)}, {"MaderaMedia", (C(0.66f, 0.42f, 0.24f), 0)}, {"MaderaOscura", (C(0.4f, 0.24f, 0.14f), 0)},
            {"SofaRojo", (C(0.88f, 0.32f, 0.3f), 0)}, {"SofaTurquesa", (C(0.2f, 0.62f, 0.62f), 0)}, {"SofaMostaza", (C(0.92f, 0.7f, 0.22f), 0)}, {"SofaMorado", (C(0.45f, 0.25f, 0.6f), 0)},
            {"CamaAzul", (C(0.38f, 0.55f, 0.92f), 0)}, {"CamaRosa", (C(0.98f, 0.58f, 0.72f), 0)}, {"CamaRoja", (C(0.7f, 0.12f, 0.18f), 0)},
            {"Sabana", (C(0.97f, 0.95f, 0.9f), 0)}, {"Almohada", (C(1f, 1f, 0.97f), 0)},
            {"Mantel", (C(0.9f, 0.25f, 0.25f), 0)}, {"MantelAzul", (C(0.3f, 0.5f, 0.85f), 0)}, {"CortinaRoja", (C(0.78f, 0.18f, 0.22f), 0)}, {"CortinaVerde", (C(0.3f, 0.6f, 0.4f), 0)},
            {"Alfombra1", (C(0.75f, 0.3f, 0.35f), 0)}, {"Alfombra2", (C(0.3f, 0.45f, 0.75f), 0)}, {"Alfombra3", (C(0.85f, 0.65f, 0.25f), 0)}, {"AlfombraBorde", (C(0.96f, 0.9f, 0.75f), 0)},
            // Cocina y baño
            {"MuebleBlanco", (C(0.93f, 0.93f, 0.89f), 0)}, {"Encimera", (C(0.34f, 0.38f, 0.46f), 0)}, {"Acero", (C(0.72f, 0.74f, 0.78f), 0)},
            {"Nevera", (C(0.6f, 0.88f, 0.8f), 0)}, {"Porcelana", (C(0.97f, 0.97f, 1f), 0)}, {"Agua", (C(0.45f, 0.72f, 0.95f), 0)}, {"Espejo", (C(0.7f, 0.85f, 0.95f), 0)},
            {"Olla", (C(0.85f, 0.25f, 0.2f), 0)}, {"Mimbre", (C(0.8f, 0.62f, 0.38f), 0)},
            // Plantas y otros
            {"Planta", (C(0.28f, 0.66f, 0.32f), 0)}, {"PlantaOscura", (C(0.16f, 0.46f, 0.22f), 0)}, {"Maceta", (C(0.82f, 0.42f, 0.26f), 0)}, {"Tierra", (C(0.3f, 0.2f, 0.12f), 0)},
            {"Pantalla", (C(1f, 0.92f, 0.7f), 0.6f)}, {"Bombilla", (C(1f, 0.95f, 0.75f), 2f)}, {"Carton", (C(0.8f, 0.62f, 0.4f), 0)}, {"Cinta", (C(0.9f, 0.8f, 0.55f), 0)},
            {"Ladrillo", (C(0.72f, 0.32f, 0.24f), 0)}, {"Fuego", (C(1f, 0.55f, 0.1f), 3f)}, {"Dorado", (C(1f, 0.8f, 0.25f), 0.15f)}, {"Marmol", (C(0.92f, 0.92f, 0.95f), 0)},
            {"Terciopelo", (C(0.65f, 0.08f, 0.2f), 0)}, {"PantallaTV", (C(0.3f, 0.65f, 1f), 1.4f)}, {"Cristal", (C(0.6f, 0.8f, 0.95f), 0.1f)},
            {"Juguete1", (C(0.95f, 0.3f, 0.3f), 0)}, {"Juguete2", (C(0.3f, 0.6f, 0.95f), 0)}, {"Juguete3", (C(0.95f, 0.85f, 0.2f), 0)}, {"Juguete4", (C(0.4f, 0.8f, 0.4f), 0)},
            {"Libro1", (C(0.8f, 0.25f, 0.25f), 0)}, {"Libro2", (C(0.25f, 0.45f, 0.8f), 0)}, {"Libro3", (C(0.3f, 0.65f, 0.35f), 0)}, {"Libro4", (C(0.9f, 0.7f, 0.25f), 0)}, {"Libro5", (C(0.55f, 0.3f, 0.6f), 0)},
            {"Coche", (C(0.9f, 0.22f, 0.2f), 0)}, {"Neumatico", (C(0.1f, 0.1f, 0.1f), 0)}, {"Faro", (C(1f, 1f, 0.8f), 1f)},
            {"Cesped", (C(0.24f, 0.42f, 0.3f), 0)}, {"Arbusto", (C(0.2f, 0.5f, 0.3f), 0)}, {"Tronco", (C(0.42f, 0.28f, 0.18f), 0)}, {"Piedra", (C(0.6f, 0.6f, 0.62f), 0)},
            {"Puerta", (C(0.62f, 0.36f, 0.22f), 0)}, {"Marco", (C(0.95f, 0.92f, 0.85f), 0)}, {"SalidaVerde", (C(0.2f, 1f, 0.45f), 2f)},
            {"CamaraSeg", (C(0.92f, 0.92f, 0.95f), 0)}, {"LedRojo", (C(1f, 0.1f, 0.1f), 2f)}, {"Sensor", (C(0.25f, 0.25f, 0.3f), 0)}, {"Laser", (C(1f, 0.15f, 0.2f), 2.5f)},
            {"Brillo", (C(1f, 0.85f, 0.3f), 2.5f)}, {"OjoBlanco", (C(1f, 1f, 1f), 0.3f)},
            // Comida y botín
            {"Galleta", (C(0.82f, 0.56f, 0.3f), 0)}, {"Chocolate", (C(0.32f, 0.18f, 0.1f), 0)}, {"ManzanaRoja", (C(0.92f, 0.12f, 0.12f), 0)}, {"Hoja", (C(0.3f, 0.72f, 0.2f), 0)},
            {"Queso", (C(1f, 0.82f, 0.25f), 0)}, {"QuesoAgujero", (C(0.86f, 0.62f, 0.12f), 0)}, {"PastelRosa", (C(1f, 0.6f, 0.75f), 0)}, {"Nata", (C(1f, 0.97f, 0.9f), 0)},
            {"Cereza", (C(0.85f, 0.05f, 0.15f), 0)}, {"BolsaPapas", (C(0.98f, 0.78f, 0.12f), 0)}, {"FranjaRoja", (C(0.9f, 0.2f, 0.15f), 0)}, {"Pavo", (C(0.78f, 0.44f, 0.18f), 0)},
            {"Hueso", (C(0.96f, 0.93f, 0.86f), 0)}, {"Plato", (C(0.96f, 0.96f, 0.98f), 0)}, {"Pato", (C(1f, 0.85f, 0.1f), 0)}, {"Pico", (C(1f, 0.5f, 0.1f), 0)},
            {"Reloj", (C(0.22f, 0.48f, 0.88f), 0)}, {"Telefono", (C(0.12f, 0.12f, 0.14f), 0)}, {"PantallaMovil", (C(0.35f, 0.75f, 1f), 1.6f)},
            {"CamaraFoto", (C(0.28f, 0.28f, 0.32f), 0)}, {"Lente", (C(0.12f, 0.18f, 0.3f), 0)}, {"Joyero", (C(0.56f, 0.16f, 0.62f), 0)}, {"Gema", (C(0.3f, 1f, 0.9f), 1.5f)}, {"Perla", (C(1f, 0.98f, 0.95f), 0.2f)},
            {"JarronAzul", (C(0.25f, 0.5f, 0.85f), 0)}, {"Flor1", (C(1f, 0.4f, 0.5f), 0)}, {"Flor2", (C(1f, 0.85f, 0.3f), 0)},
            // Paredes por nivel
            {"ParedCrema", (C(0.99f, 0.86f, 0.64f), 0)}, {"ParedNaranja", (C(0.96f, 0.62f, 0.36f), 0)}, {"ParedTurquesa", (C(0.5f, 0.78f, 0.74f), 0)},
            {"ParedVerde", (C(0.66f, 0.82f, 0.52f), 0)}, {"ParedGrisAzul", (C(0.76f, 0.81f, 0.9f), 0)}, {"ParedBurdeos", (C(0.66f, 0.24f, 0.3f), 0)},
            {"RemateMarron", (C(0.45f, 0.26f, 0.18f), 0)}, {"RemateOscuro", (C(0.25f, 0.28f, 0.36f), 0)}, {"RemateDorado", (C(0.85f, 0.65f, 0.25f), 0)},
        };

        static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");

        public static void Build()
        {
            ProjectBuilder.EnsureFolder(PaletteFolder);
            ProjectBuilder.EnsureFolder(FadeFolder);
            ProjectBuilder.EnsureFolder(FloorFolder);
            ProjectBuilder.EnsureFolder(TexFolder);
            ProjectBuilder.EnsureFolder("Assets/Resources/Materials");
            cache.Clear();
            fadeCache.Clear();
            foreach (var k in palette.Keys) Get(k);

            // Material translúcido para conos de visión, anillos de ruido y zonas de sensores (color por código).
            var vc = LoadOrCreate("Assets/Resources/Materials/VisionCone.mat", Unlit);
            vc.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f));
            MakeTransparent(vc, true);
            var glow = LoadOrCreate("Assets/Materials/BrilloBotin.mat", Unlit);
            glow.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f, 0.35f));
            MakeTransparent(glow, true);
            cache["_Glow"] = glow;
            cache["_Vision"] = vc;
        }

        static Material LoadOrCreate(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Get(string name)
        {
            if (cache.TryGetValue(name, out var m) && m != null) return m;
            if (!palette.TryGetValue(name, out var def))
            {
                Debug.LogWarning("Color de paleta desconocido: " + name);
                def = (Color.magenta, 0f);
            }
            m = LoadOrCreate($"{PaletteFolder}/M_{name}.mat", Lit);
            m.SetColor("_BaseColor", def.col);
            m.SetFloat("_Smoothness", 0.12f);
            m.SetFloat("_Metallic", 0f);
            if (def.emit > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", def.col * def.emit);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
            }
            cache[name] = m;
            return m;
        }

        public static Material Glow => cache.TryGetValue("_Glow", out var g) ? g : null;
        public static Material Vision => cache.TryGetValue("_Vision", out var g) ? g : null;

        /// <summary>Versión transparente (para paredes y muebles altos que se desvanecen).</summary>
        public static Material Fade(Material normal)
        {
            if (normal == null) return null;
            if (fadeCache.TryGetValue(normal, out var f) && f != null) return f;
            f = LoadOrCreate($"{FadeFolder}/{normal.name}_Transparente.mat", Lit);
            f.CopyPropertiesFromMaterial(normal);
            if (normal.HasProperty("_BaseMap")) f.SetTexture("_BaseMap", normal.GetTexture("_BaseMap"));
            MakeTransparent(f, false, true);
            fadeCache[normal] = f;
            return f;
        }

        public static void MakeTransparent(Material m, bool doubleSided, bool castShadows = false)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            if (doubleSided) m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", castShadows);
            EditorUtility.SetDirty(m);
        }

        // ---------------- Contorno de dibujo animado ----------------

        static Material outline;

        /// <summary>Material oscuro sin luz que solo pinta caras traseras (contorno "inverted hull").</summary>
        public static Material Outline
        {
            get
            {
                if (outline == null)
                {
                    outline = LoadOrCreate("Assets/Materials/Contorno.mat", Unlit);
                    outline.SetColor("_BaseColor", new Color(0.1f, 0.07f, 0.18f, 1f));
                    outline.SetFloat("_Cull", 1f);
                    outline.SetShaderPassEnabled("ShadowCaster", false);
                    EditorUtility.SetDirty(outline);
                }
                return outline;
            }
        }

        // ---------------- Suelos, paredes y césped con textura procedural ----------------

        public enum FloorStyle { Tiles, Wood, Carpet, Checker, Concrete, Wallpaper, Grass }

        public static Material Floor(string name, FloorStyle style, Color a, Color b)
        {
            string key = "Suelo_" + name;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var tex = MakeTexture(name, style, a, b);
            m = LoadOrCreate($"{FloorFolder}/M_Suelo_{name}.mat", Lit);
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", style == FloorStyle.Checker || style == FloorStyle.Tiles ? 0.4f : 0.12f);
            cache[key] = m;
            return m;
        }

        /// <summary>Material de pared con papel pintado suave del color indicado de la paleta.</summary>
        public static Material Wall(string paletteName)
        {
            string key = "Pared_" + paletteName;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var baseCol = palette.TryGetValue(paletteName, out var def) ? def.col : Color.white;
            var tex = MakeTexture("PapelPintado", FloorStyle.Wallpaper, Color.white, new Color(0.88f, 0.88f, 0.88f));
            m = LoadOrCreate($"{FloorFolder}/M_Pared_{paletteName}.mat", Lit);
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", baseCol);
            m.SetFloat("_Smoothness", 0.1f);
            cache[key] = m;
            return m;
        }

        public static Material Grass()
        {
            return Floor("Cesped", FloorStyle.Grass, new Color(0.26f, 0.48f, 0.3f), new Color(0.18f, 0.36f, 0.24f));
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        static Texture2D MakeTexture(string name, FloorStyle style, Color a, Color b)
        {
            const int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
            int seed = name.Length * 31 + (int)style;
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = Hash(x, y, seed);
                    float pn = Mathf.PerlinNoise(x * 0.03f + seed, y * 0.03f);
                    Color c = a;
                    switch (style)
                    {
                        case FloorStyle.Tiles:
                            {
                                // 4x4 baldosas con junta y bisel claro arriba/izquierda.
                                int lx = x % 64, ly = y % 64;
                                bool grout = lx < 3 || ly < 3;
                                float tint = 0.95f + 0.08f * Hash(x / 64, y / 64, seed);
                                c = grout ? b : a * tint;
                                if (!grout && (ly > 58 || lx < 7)) c = Color.Lerp(c, Color.white, 0.18f);
                                if (!grout && (ly < 8 || lx > 58)) c *= 0.93f;
                                c = Color.Lerp(c, c * (0.97f + 0.06f * pn), 0.6f);
                                break;
                            }
                        case FloorStyle.Wood:
                            {
                                int plank = y / 32;
                                int offset = (plank * 97) % 256;
                                int px2 = (x + offset) % 256;
                                bool seam = (y % 32) == 0 || px2 == 0 || (y % 32) == 31;
                                float tone = 0.86f + 0.16f * Hash(plank, (x + offset) / 256, seed);
                                float grain = 0.95f + 0.05f * Mathf.Sin(px2 * 0.12f + Mathf.Sin(y * 0.4f) * 2f + plank * 3f);
                                float knot = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(px2 % 128, y % 32), new Vector2(60f, 16f)) / 6f) * (plank % 3 == 0 ? 1f : 0f);
                                c = seam ? b : a * tone * grain * (1f - 0.25f * knot);
                                break;
                            }
                        case FloorStyle.Carpet:
                            {
                                float weave = ((x + y) % 4 < 2 ? 0.97f : 1.03f);
                                c = Color.Lerp(a, b, n * 0.3f + pn * 0.25f) * weave;
                                break;
                            }
                        case FloorStyle.Checker:
                            {
                                bool odd = ((x / 64) + (y / 64)) % 2 == 1;
                                c = odd ? b : a;
                                float vein = Mathf.Abs(Mathf.Sin((x + y * 0.6f) * 0.05f + pn * 6f));
                                if (!odd && vein < 0.04f) c *= 0.9f;
                                if (odd && vein < 0.04f) c = Color.Lerp(c, Color.white, 0.12f);
                                if ((x % 64) < 2 || (y % 64) < 2) c *= 0.85f;
                                break;
                            }
                        case FloorStyle.Concrete:
                            c = Color.Lerp(a, b, n * 0.2f + 0.35f * pn);
                            if (Hash(x / 3, y / 3, seed + 7) > 0.985f) c *= 0.8f;
                            break;
                        case FloorStyle.Wallpaper:
                            {
                                // Rayas verticales suaves con lunares pequeños.
                                bool stripe = (x / 16) % 2 == 0;
                                c = stripe ? a : b;
                                float dx = (x % 32) - 24f, dy = (y % 32) - 16f;
                                if (!stripe && dx * dx + dy * dy < 9f) c = Color.Lerp(b, Color.white, 0.6f);
                                c *= 0.98f + 0.04f * n;
                                break;
                            }
                        case FloorStyle.Grass:
                            {
                                float blade = Hash(x, y / 3, seed) > 0.7f ? 1.08f : 1f;
                                c = Color.Lerp(a, b, pn * 0.8f + n * 0.15f) * blade;
                                break;
                            }
                    }
                    c.a = 1f;
                    px[y * s + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(true);
            string path = $"{TexFolder}/T_{(style == FloorStyle.Wallpaper || style == FloorStyle.Grass ? "" : "Suelo_")}{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = 8;
            imp.mipmapEnabled = true;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
