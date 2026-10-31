using System.Collections.Generic;
using CopAtolyesi.Core;
using UnityEngine;

namespace CopAtolyesi
{
    // Muzik ve efektler Resources/Audio altindan yuklenir; ses seviyesi AudioListener.volume ile ayarlanir.
    public sealed class AudioKit : MonoBehaviour
    {
        const string MainMusic = "music_main";
        const float WorkInterval = 0.24f;

        static AudioKit instance;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource sfx;
        AudioSource music;
        float nextWork;

        public static void Ensure()
        {
            if (instance == null)
            {
                var go = new GameObject("AudioKit");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioKit>();
                instance.Build();
            }
            PlayMusic(MainMusic);
        }

        public static void Play(string name)
        {
            if (instance == null || !instance.clips.TryGetValue(name, out var clip)) return;
            instance.sfx.PlayOneShot(clip, GameData.Config.sfxVolume * GameSettings.SfxVolume);
        }

        // Kaydirici surekli oynarken ses ust uste binmesin diye aralikli calar.
        public static void PlayWork(string name)
        {
            if (instance == null || Time.unscaledTime < instance.nextWork) return;
            instance.nextWork = Time.unscaledTime + WorkInterval;
            Play(name);
        }

        public static void PlayMusic(string name)
        {
            if (instance == null || !instance.clips.TryGetValue(name, out var clip) || instance.music.clip == clip) return;
            instance.music.clip = clip;
            instance.music.Play();
        }

        public static void ApplyVolume()
        {
            if (instance != null) instance.music.volume = GameData.Config.musicVolume * GameSettings.MusicVolume;
        }

        // Otomatik test icin calan muzik ve yuklu efektler.
        public static float MusicLevel => instance != null ? instance.music.volume : 0;
        public static string CurrentMusic => instance != null && instance.music.isPlaying ? instance.music.clip.name : null;
        public static bool HasClip(string name) => instance != null && instance.clips.ContainsKey(name);

        void Build()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            ApplyVolume();
            foreach (var clip in Resources.LoadAll<AudioClip>("Audio")) clips[clip.name] = clip;
        }
    }
}
