using UnityEngine;

namespace GameCore
{
    /// Série de victoires : chaque niveau gagné d'affilée augmente la récompense du
    /// suivant ; une défaite remet la série à zéro.
    public static class WinStreak
    {
        private const string Key = "win_streak";
        /// Bonus par victoire d'affilée (à partir de la 2e), et son plafond.
        public const float BonusPerWin = 0.1f;
        public const int MaxBonusWins = 5;

        /// Niveaux gagnés d'affilée.
        public static int Count => Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));

        /// Multiplicateur de récompense pour une série donnée : 1 à la première victoire,
        /// puis +10 % par victoire, jusqu'à +50 %.
        public static float Multiplier(int streak) => 1f + Mathf.Clamp(streak - 1, 0, MaxBonusWins) * BonusPerWin;

        /// Niveau gagné : la série monte. Renvoie la nouvelle série.
        public static int RegisterWin()
        {
            int streak = Count + 1;
            PlayerPrefs.SetInt(Key, streak);
            PlayerPrefs.Save();
            return streak;
        }

        /// Défaite : la série repart de zéro.
        public static void Reset()
        {
            if (Count == 0) return;
            PlayerPrefs.SetInt(Key, 0);
            PlayerPrefs.Save();
        }
    }
}
