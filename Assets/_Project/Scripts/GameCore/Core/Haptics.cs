using UnityEngine;

namespace GameCore
{
    /// Vibrations courtes. Android : vibreur du téléphone. iOS : retour haptique natif
    /// (Assets/Plugins/iOS/GameHaptics.mm). Ailleurs : ne fait rien.
    public static class Haptics
    {
        private const string Key = "settings_haptics_on";
        private static float last = -99f;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Key, 1) == 1;
            set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// Petit retour (ramassage, clic important).
        public static void Light() => Pulse(12, 0);
        /// Retour moyen (power-up, palier).
        public static void Medium() => Pulse(28, 1);
        /// Retour fort (danger, victoire).
        public static void Heavy() => Pulse(60, 2);

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void GameHapticImpact(int strength);
#endif

        private static void Pulse(long milliseconds, int strength)
        {
            if (!Enabled || Time.unscaledTime - last < 0.06f) return; // pas de bourdonnement continu
            last = Time.unscaledTime;
#if UNITY_IOS && !UNITY_EDITOR
            try { GameHapticImpact(strength); }
            catch (System.Exception) { }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!searched)
                {
                    searched = true;
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (vibrator != null) vibrator.Call("vibrate", milliseconds);
            }
            catch (System.Exception) { vibrator = null; }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;
        private static bool searched;

        // Jamais appelée : sa présence fait ajouter la permission VIBRATE au build Android.
        private static void KeepVibratePermission() => Handheld.Vibrate();
#endif
    }
}
