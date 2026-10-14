using System;
using UnityEngine;

namespace GameCore
{
    /// Boutique : « sans pub » et packs de pièces. Le jeu ne parle qu'à cette classe ;
    /// le vrai paiement passe par un connecteur (IStoreBackend) à brancher sur Unity IAP.
    /// Tant qu'aucun connecteur n'est branché, la boutique est MASQUÉE dans le jeu (pas
    /// de bouton qui ne mène à rien). Dans l'éditeur, un connecteur de simulation valide
    /// tous les achats pour tester les écrans.
    public static class Store
    {
        public class Product
        {
            public string id;          // identifiant à créer à l'identique dans Google Play / App Store
            public string title;
            public string fallbackPrice; // affiché si la boutique n'a pas encore renvoyé le prix local
            public int coins;
            public bool removesAds;
        }

        public static readonly Product[] Products =
        {
            new Product { id = "no_ads", title = "NO ADS", fallbackPrice = "$2.99", removesAds = true },
            new Product { id = "coins_small", title = "1,000 COINS", fallbackPrice = "$0.99", coins = 1000 },
            new Product { id = "coins_medium", title = "3,500 COINS", fallbackPrice = "$2.99", coins = 3500 },
            new Product { id = "coins_large", title = "10,000 COINS", fallbackPrice = "$5.99", coins = 10000 },
        };

        private const string NoAdsKey = "store_no_ads";

        /// Connecteur de paiement (null = boutique masquée).
        public static IStoreBackend Backend { get; set; }
        public static bool Available => Backend != null && Backend.Ready;

        /// Achat ou restauration : les écrans se rafraîchissent.
        public static event Action Changed;

        /// « Sans pub » acheté : plus d'interstitiels (les pubs récompensées, choisies par
        /// le joueur, restent proposées).
        public static bool NoAds
        {
            get => PlayerPrefs.GetInt(NoAdsKey, 0) == 1;
            private set { PlayerPrefs.SetInt(NoAdsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static string PriceOf(Product product)
        {
            string price = Available ? Backend.PriceOf(product.id) : null;
            return string.IsNullOrEmpty(price) ? product.fallbackPrice : price;
        }

        /// Un produit « sans pub » déjà acheté ne se rachète pas.
        public static bool Owned(Product product) => product.removesAds && NoAds;

        public static void Buy(Product product, Action<bool> done)
        {
            if (!Available || product == null || Owned(product)) { done?.Invoke(false); return; }
            Backend.Purchase(product.id, ok =>
            {
                if (ok) Grant(product);
                done?.Invoke(ok);
            });
        }

        /// Restaure les achats non consommables (« sans pub ») — obligatoire sur iOS.
        public static void Restore(Action<bool> done)
        {
            if (!Available) { done?.Invoke(false); return; }
            Backend.Restore(id =>
            {
                foreach (var p in Products)
                    if (p.id == id && p.removesAds) { NoAds = true; Changed?.Invoke(); }
            }, done);
        }

        /// Crédite un achat validé. Public : le connecteur l'appelle aussi pour un achat
        /// resté en attente (paiement validé après la fermeture du jeu).
        public static void Grant(Product product)
        {
            if (product.coins > 0) PlayerWallet.Add(product.coins);
            if (product.removesAds) NoAds = true;
            GameLog.Event("purchase", product.id);
            Changed?.Invoke();
        }

        public static Product Find(string id)
        {
            foreach (var p in Products) if (p.id == id) return p;
            return null;
        }

#if UNITY_EDITOR
        /// Éditeur : achats simulés (toujours acceptés), pour tester la boutique sans magasin.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallEditorSimulation()
        {
            if (Backend is EditorSimulation && !SimulateInEditor) Backend = null;
            else if (Backend == null && SimulateInEditor) Backend = new EditorSimulation();
        }

        private const string SimulateKey = "gamecore_shop_simulation";
        private const string SimulateMenu = "Tools/Game Template/Boutique/Boutique simulée dans l'éditeur";

        /// Case à cocher du menu Tools : décochée, la boutique est masquée dans l'éditeur
        /// comme elle l'est dans un build sans connecteur de paiement.
        private static bool SimulateInEditor
        {
            get => UnityEditor.EditorPrefs.GetBool(SimulateKey, true);
            set => UnityEditor.EditorPrefs.SetBool(SimulateKey, value);
        }

        [UnityEditor.MenuItem(SimulateMenu, priority = 502)]
        private static void ToggleSimulation()
        {
            SimulateInEditor = !SimulateInEditor;
            if (Application.isPlaying) Debug.Log("Boutique simulée : pris en compte au prochain Play.");
        }

        [UnityEditor.MenuItem(SimulateMenu, true)]
        private static bool ToggleSimulationValidate()
        {
            UnityEditor.Menu.SetChecked(SimulateMenu, SimulateInEditor);
            return true;
        }

        private class EditorSimulation : IStoreBackend
        {
            public bool Ready => true;
            public string PriceOf(string id) => null;
            public void Purchase(string id, Action<bool> done)
            {
                Debug.Log($"Store : achat simulé (éditeur) — {id}");
                done?.Invoke(true);
            }
            public void Restore(Action<string> owned, Action<bool> done) => done?.Invoke(true);
        }
#endif
    }

    /// Connecteur de paiement à brancher (Unity IAP) : Store.Backend = new MonConnecteur().
    public interface IStoreBackend
    {
        /// La boutique a répondu et les produits sont achetables.
        bool Ready { get; }
        /// Prix localisé (« 2,99 € »), ou null s'il n'est pas encore connu.
        string PriceOf(string productId);
        void Purchase(string productId, Action<bool> done);
        /// Appelle `owned` pour chaque produit non consommable déjà acheté, puis `done`.
        void Restore(Action<string> owned, Action<bool> done);
    }
}
