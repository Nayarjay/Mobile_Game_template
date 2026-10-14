using UnityEngine;

namespace GameCore
{
    /// Sons et musique du jeu, sans rien à poser dans les scènes : les sources audio se
    /// créent toutes seules au premier son et survivent aux changements de scène (les
    /// clics de menu ne sont pas coupés). Respecte GameSettings (musique / bruitages).
    public static class GameAudio
    {
        /// Volume général des bruitages et de la musique (0 à 1).
        public static float SfxVolume = 1f;
        public static float MusicVolume = 0.6f;

        /// Son joué par chaque bouton (UiButtonFeedback). Null = pas de son.
        public static AudioClip ClickClip;

        private static AudioSource plain;   // sons à hauteur normale
        private static AudioSource pitched; // sons dont la hauteur varie
        private static AudioSource musicSource;

        private static AudioSource Make(string name)
        {
            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true; // les boutons du menu pause s'entendent
            return source;
        }

        public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || !Application.isPlaying || !GameSettings.SfxOn) return;

            if (Mathf.Approximately(pitch, 1f))
            {
                if (plain == null) plain = Make("GameAudio");
                plain.PlayOneShot(clip, volume * SfxVolume);
            }
            else
            {
                if (pitched == null) pitched = Make("GameAudio (pitch)");
                pitched.pitch = pitch;
                pitched.PlayOneShot(clip, volume * SfxVolume);
            }
        }

        public static void Click() => Play(ClickClip, 0.7f);

        /// Lance la musique en boucle. Rappeler avec la même musique ne la relance pas.
        public static void PlayMusic(AudioClip clip)
        {
            if (clip == null || !Application.isPlaying) return;
            if (musicSource == null) { musicSource = Make("GameMusic"); musicSource.loop = true; }
            musicSource.volume = MusicVolume;
            musicSource.mute = !GameSettings.MusicOn;
            if (musicSource.clip != clip) { musicSource.clip = clip; musicSource.Play(); }
            else if (!musicSource.isPlaying) musicSource.Play();
        }

        public static void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        /// Réglages du joueur modifiés : la musique est coupée ou remise tout de suite.
        public static void ApplySettings()
        {
            if (musicSource != null) musicSource.mute = !GameSettings.MusicOn;
        }
    }
}
