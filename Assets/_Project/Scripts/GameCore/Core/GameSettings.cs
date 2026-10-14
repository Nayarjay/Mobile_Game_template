using UnityEngine;

namespace GameCore
{
    /// Réglages du joueur (sauvegardés) : musique et bruitages. Les vibrations sont dans
    /// Haptics.Enabled, la langue dans Loc.
    public static class GameSettings
    {
        private const string MusicKey = "settings_music_on";
        private const string SfxKey = "settings_sfx_on";

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set => Save(MusicKey, value);
        }

        /// Bruitages (boutons, victoire...).
        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set => Save(SfxKey, value);
        }

        private static void Save(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            GameAudio.ApplySettings();
        }
    }
}
