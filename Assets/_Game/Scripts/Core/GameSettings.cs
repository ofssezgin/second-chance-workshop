using UnityEngine;

namespace CopAtolyesi.Core
{
    // Oyuncu ayarlari cihazda saklanir, oyun yeniden acilinca korunur.
    public static class GameSettings
    {
        public static readonly string[] Languages = { "tr", "en" };

        const string LanguageKey = "language";
        const string MusicKey = "musicVolume";
        const string SfxKey = "sfxVolume";
        const float DefaultVolume = 0.5f;

        public static string Language
        {
            get => PlayerPrefs.GetString(LanguageKey, Languages[0]);
            set
            {
                PlayerPrefs.SetString(LanguageKey, value);
                PlayerPrefs.Save();
                GameData.SetLanguage(value);
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, DefaultVolume);
            set => SaveVolume(MusicKey, value);
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, DefaultVolume);
            set => SaveVolume(SfxKey, value);
        }

        static void SaveVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
