using System;
using System.Globalization;
using UnityEngine;

namespace GameCore
{
    /// Cadeau quotidien : un par jour, de plus en plus gros sur 7 jours consécutifs.
    /// Un jour manqué fait repartir la série au jour 1. Les pièces vont dans PlayerWallet ;
    /// pour offrir autre chose (power-ups, skins...), le jeu s'abonne à `Claimed`.
    public static class DailyReward
    {
        private const string LastKey = "daily_last_claim";
        private const string StreakKey = "daily_streak";
        private const string DateFormat = "yyyyMMdd";

        private static readonly int[] CoinsByDay = { 50, 75, 100, 125, 150, 200, 300 };

        public static int DayCount => CoinsByDay.Length;

        /// (jour de la série 1 à 7, pièces données) après chaque cadeau encaissé.
        public static event Action<int, int> Claimed;

        private static string Day(int offset) => DateTime.Now.AddDays(offset).ToString(DateFormat, CultureInfo.InvariantCulture);

        public static bool CanClaim => PlayerPrefs.GetString(LastKey, "") != Day(0);

        /// Jour de la série qui sera donné au prochain « Claim » (1 à 7).
        public static int NextDay
        {
            get
            {
                string last = PlayerPrefs.GetString(LastKey, "");
                int streak = PlayerPrefs.GetInt(StreakKey, 0);
                if (last != Day(-1) && last != Day(0)) return 1;  // série rompue (ou première fois)
                return streak >= DayCount ? 1 : streak + 1;       // après le jour 7, on repart au jour 1
            }
        }

        public static int CoinsFor(int day) => CoinsByDay[Mathf.Clamp(day, 1, DayCount) - 1];

        /// Encaisse le cadeau du jour. Renvoie (jour, pièces), ou (0, 0) s'il est déjà pris.
        public static (int day, int coins) Claim()
        {
            if (!CanClaim) return (0, 0);
            int day = NextDay;
            int coins = CoinsFor(day);

            PlayerPrefs.SetString(LastKey, Day(0));
            PlayerPrefs.SetInt(StreakKey, day);
            PlayerPrefs.Save();

            PlayerWallet.Add(coins);
            GameLog.Event("daily_claim", $"jour={day} pieces={coins}");
            Claimed?.Invoke(day, coins);
            return (day, coins);
        }

        /// Outil de test : le cadeau redevient disponible (comme si on revenait le lendemain).
        public static void DebugMakeAvailable()
        {
            PlayerPrefs.SetString(LastKey, Day(-1));
            PlayerPrefs.Save();
        }
    }
}
