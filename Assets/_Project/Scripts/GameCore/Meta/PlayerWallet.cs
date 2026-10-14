using System;
using UnityEngine;

namespace GameCore
{
    /// Pièces du joueur (sauvegardées). Gagnées en jeu, en cadeau du jour ou en boutique,
    /// dépensées dans les améliorations / skins.
    public static class PlayerWallet
    {
        private const string Key = "wallet_coins";

        /// (nouveau total) à chaque gain ou dépense.
        public static event Action<int> Changed;

        public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Set(Coins + amount);
            GameLog.Event("coins_gain", amount.ToString());
        }

        public static bool TrySpend(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Set(Coins - amount);
            GameLog.Event("coins_spend", amount.ToString());
            return true;
        }

        private static void Set(int value)
        {
            PlayerPrefs.SetInt(Key, Mathf.Max(0, value));
            PlayerPrefs.Save();
            Changed?.Invoke(Coins);
        }

        /// Outil de test.
        public static void ResetAll() => Set(0);
    }
}
