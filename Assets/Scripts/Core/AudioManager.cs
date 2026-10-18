using System.Collections.Generic;
using UnityEngine;

namespace RobaYCorre
{
    public enum Sfx
    {
        Pickup, Coin, Bump, Crash, Spotted, Hide, Unhide, Escape, Caught,
        Bark, Alarm, Drop, Click, Crunch, Deposit, Full, Question, Wake
    }

    /// <summary>
    /// Música y efectos. Si existe un clip en Resources/Audio con el nombre del efecto
    /// (por ejemplo "Audio/Pickup" o "Audio/Music"), se usa ese archivo; si no, se genera
    /// un sonido sencillo por código para que el juego nunca se quede mudo.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager instance;

        public static AudioManager I
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }
                return instance;
            }
        }

        AudioSource music;
        readonly List<AudioSource> sources = new List<AudioSource>();
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        int nextSource;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
            {
                var file = Resources.Load<AudioClip>("Audio/" + s);
                clips[s] = file != null ? file : ProceduralAudio.Build(s);
            }

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            var musicFile = Resources.Load<AudioClip>("Audio/Music");
            music.clip = musicFile != null ? musicFile : ProceduralAudio.Music();
            music.Play();

            for (int i = 0; i < 10; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                sources.Add(src);
            }
            ApplyVolumes();
        }

        public static void RefreshVolumes()
        {
            if (instance != null) instance.ApplyVolumes();
        }

        void ApplyVolumes()
        {
            bool muted = GameProgress.Muted;
            if (music != null) music.volume = muted ? 0f : GameProgress.MusicVolume * 0.55f;
            float sfx = muted ? 0f : GameProgress.SfxVolume;
            foreach (var s in sources) s.volume = sfx;
        }

        public static void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
        {
            var m = I;
            if (!m.clips.TryGetValue(sfx, out var clip) || clip == null) return;
            // Evita que el mismo sonido se acumule muchas veces en el mismo instante.
            if (m.lastPlayed.TryGetValue(sfx, out var t) && Time.unscaledTime - t < 0.05f) return;
            m.lastPlayed[sfx] = Time.unscaledTime;
            var src = m.sources[m.nextSource];
            m.nextSource = (m.nextSource + 1) % m.sources.Count;
            src.pitch = pitch * Random.Range(0.96f, 1.04f);
            src.PlayOneShot(clip, volume);
        }
    }
}
