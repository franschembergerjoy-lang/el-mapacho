using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobaYCorre
{
    public static class Layers
    {
        public const int IgnoreRaycast = 2;
        public const int Player = 8;
        public const int Walls = 9;

        /// <summary>Todo lo que bloquea la vista (paredes y muebles), excepto el jugador y los triggers.</summary>
        public static int ObstacleMask => ~((1 << Player) | (1 << IgnoreRaycast));
        public static int WallMask => 1 << Walls;
    }

    /// <summary>Progreso, opciones y carga de escenas (guardado en PlayerPrefs).</summary>
    public static class GameProgress
    {
        public const string MenuScene = "MainMenu";

        public static readonly string[] LevelScenes =
        {
            "Nivel1_CasaTranquila",
            "Nivel2_CasaDesordenada",
            "Nivel3_CasaConPerro",
            "Nivel4_CasaInteligente",
            "Nivel5_MansionColeccionista",
        };

        public static readonly string[] LevelTitles =
        {
            "La casa tranquila",
            "La casa desordenada",
            "La casa con perro",
            "La casa inteligente",
            "La mansión del coleccionista",
        };

        public static int LevelCount => LevelScenes.Length;

        /// <summary>Si es true, el menú principal abre directamente la selección de niveles.</summary>
        public static bool OpenLevelSelect;

        public static int UnlockedLevels => Mathf.Clamp(PlayerPrefs.GetInt("RyC_Unlocked", 1), 1, LevelCount);

        public static void Unlock(int level)
        {
            level = Mathf.Clamp(level, 1, LevelCount);
            if (level > UnlockedLevels)
            {
                PlayerPrefs.SetInt("RyC_Unlocked", level);
                PlayerPrefs.Save();
            }
        }

        public static int GetStars(int level) => PlayerPrefs.GetInt("RyC_Stars_" + level, 0);

        public static void SetStars(int level, int stars)
        {
            if (stars > GetStars(level))
            {
                PlayerPrefs.SetInt("RyC_Stars_" + level, stars);
                PlayerPrefs.Save();
            }
        }

        public static int GetBestLoot(int level) => PlayerPrefs.GetInt("RyC_Loot_" + level, 0);

        public static void SetBestLoot(int level, int loot)
        {
            if (loot > GetBestLoot(level))
            {
                PlayerPrefs.SetInt("RyC_Loot_" + level, loot);
                PlayerPrefs.Save();
            }
        }

        public static int TotalStars
        {
            get
            {
                int s = 0;
                for (int i = 1; i <= LevelCount; i++) s += GetStars(i);
                return s;
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat("RyC_Music", 0.5f);
            set { PlayerPrefs.SetFloat("RyC_Music", Mathf.Clamp01(value)); AudioManager.RefreshVolumes(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat("RyC_Sfx", 0.8f);
            set { PlayerPrefs.SetFloat("RyC_Sfx", Mathf.Clamp01(value)); AudioManager.RefreshVolumes(); }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt("RyC_Muted", 0) == 1;
            set { PlayerPrefs.SetInt("RyC_Muted", value ? 1 : 0); AudioManager.RefreshVolumes(); }
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey("RyC_Unlocked");
            for (int i = 1; i <= LevelCount; i++)
            {
                PlayerPrefs.DeleteKey("RyC_Stars_" + i);
                PlayerPrefs.DeleteKey("RyC_Loot_" + i);
            }
            PlayerPrefs.Save();
        }

        public static void LoadLevel(int level)
        {
            Time.timeScale = 1f;
            level = Mathf.Clamp(level, 1, LevelCount);
            SceneManager.LoadScene(LevelScenes[level - 1]);
        }

        public static void LoadMenu(bool openLevelSelect)
        {
            Time.timeScale = 1f;
            OpenLevelSelect = openLevelSelect;
            SceneManager.LoadScene(MenuScene);
        }
    }
}
