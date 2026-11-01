using System.Collections.Generic;
using CopAtolyesi.Core;
using UnityEngine;

namespace CopAtolyesi
{
    // Muzik ve efektler Resources/Audio altindan yuklenir. Iki muzik kaynagi sarkilari kesintisiz birbirine bagliyor.
    public sealed class AudioKit : MonoBehaviour
    {
        static readonly string[] Playlist = { "music_piano_1", "music_piano_2" };
        const float WorkInterval = 0.24f;
        const float TrackFadeSeconds = 5f;
        const float StartFadeSeconds = 1.5f;

        static AudioKit instance;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly AudioSource[] decks = new AudioSource[2];
        AudioSource sfx;
        int activeDeck;
        int track;
        bool playlistOn;
        float level;
        float fadeSeconds;
        float fadeTime;
        bool fading;
        float nextWork;

        AudioSource Active => decks[activeDeck];
        AudioSource Other => decks[1 - activeDeck];

        public static void Ensure()
        {
            if (instance == null)
            {
                var go = new GameObject("AudioKit");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioKit>();
                instance.Build();
            }
            instance.StartPlaylist();
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

        // Tek bir parcayi dongude calar; bitis ekranlari icin.
        public static void PlayMusic(string name)
        {
            if (instance == null || !instance.clips.TryGetValue(name, out var clip) || instance.Active.clip == clip) return;
            instance.playlistOn = false;
            instance.Crossfade(clip, true, TrackFadeSeconds);
        }

        public static void ApplyVolume()
        {
            if (instance == null) return;
            instance.level = GameData.Config.musicVolume * GameSettings.MusicVolume;
            if (!instance.fading) instance.Active.volume = instance.level;
        }

        // Otomatik test icin calan muzik, gecis durumu ve yuklu efektler.
        public static float MusicLevel => instance != null ? instance.level : 0;
        public static string CurrentMusic => instance != null && instance.Active.isPlaying ? instance.Active.clip.name : null;
        public static bool Crossfading => instance != null && instance.fading && instance.Other.isPlaying;
        public static bool HasClip(string name) => instance != null && instance.clips.ContainsKey(name);
        public static void SeekNearEndForTest() => instance.Active.time = instance.Active.clip.length - TrackFadeSeconds + 0.1f;

        void Build()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            decks[0] = gameObject.AddComponent<AudioSource>();
            decks[1] = gameObject.AddComponent<AudioSource>();
            ApplyVolume();
            foreach (var clip in Resources.LoadAll<AudioClip>("Audio")) clips[clip.name] = clip;
        }

        void StartPlaylist()
        {
            if (playlistOn) return;
            playlistOn = true;
            track = 0;
            Crossfade(clips[Playlist[track]], false, StartFadeSeconds);
        }

        void Crossfade(AudioClip clip, bool loop, float seconds)
        {
            Other.Stop();
            activeDeck = 1 - activeDeck;
            Active.clip = clip;
            Active.loop = loop;
            Active.volume = 0;
            Active.Play();
            fadeSeconds = seconds;
            fadeTime = 0;
            fading = true;
        }

        void Update()
        {
            if (fading)
            {
                fadeTime += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(fadeTime / fadeSeconds);
                // Esit guc egrisi: gecisin ortasinda ses dusmez.
                Active.volume = level * Mathf.Sin(progress * Mathf.PI / 2);
                Other.volume = level * Mathf.Cos(progress * Mathf.PI / 2);
                if (progress >= 1)
                {
                    Other.Stop();
                    fading = false;
                }
            }
            else if (playlistOn && Active.clip.length - Active.time <= TrackFadeSeconds)
            {
                track = (track + 1) % Playlist.Length;
                Crossfade(clips[Playlist[track]], false, TrackFadeSeconds);
            }
        }
    }
}
