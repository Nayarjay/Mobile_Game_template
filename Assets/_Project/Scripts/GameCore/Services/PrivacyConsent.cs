using System;
using UnityEngine;

namespace GameCore
{
    /// Consentement du joueur (RGPD) : mesure d'audience, et publicités personnalisées quand
    /// le jeu en affiche. Demandé une fois au premier lancement (PrivacyConsentUI), modifiable
    /// ensuite depuis l'écran des crédits. Tant que le joueur n'a pas répondu, rien n'est collecté.
    /// - Analytics (UnityAnalyticsSink) : ne démarre qu'avec un « oui » ; un « non » l'arrête
    ///   et demande l'effacement de ce qui avait été envoyé.
    /// - Pubs (AdsManager) : le choix est transmis à LevelPlay avant son démarrage, et à
    ///   chaque changement ; avec un « non », les pubs restent possibles mais non personnalisées.
    /// Ne demandent PAS ce consentement (rien ne quitte le téléphone) : la sauvegarde, la
    /// langue, les réglages du son, les rappels (notifications locales, autorisées à part
    /// par Android).
    public static class PrivacyConsent
    {
        private const string Key = "privacy_consent"; // -1 : pas encore demandé, 0 : refusé, 1 : accepté
        private const string VersionKey = "privacy_consent_version";

        /// Version du texte de consentement. À AUGMENTER dès que ce que le jeu collecte change
        /// (nouvel outil de mesure, arrivée des pubs...) : la question est alors reposée à
        /// tous les joueurs, et rien n'est collecté tant qu'ils n'ont pas répondu de nouveau.
        /// 1 : premier texte. 2 : texte qui décrit ce que fait vraiment la version (mesure
        /// d'audience seule tant qu'il n'y a pas de pubs) et précise ce qui reste sur l'appareil.
        public const int Version = 2;

        /// Le jeu affiche-t-il des publicités dans cette version ? Choisit le texte montré au
        /// joueur. À passer à true EN MÊME TEMPS que « Ads Enabled » sur l'AdsManager (et
        /// augmenter Version) — à vérifier avant chaque sortie.
        public const bool AdsLive = false;

        /// Adresse de la politique de confidentialité (page web publique). Vide = lien masqué.
        /// À remplir avant la sortie (le texte est dans Docs/privacy-policy.md).
        public const string PolicyUrl = "";

        /// (accepté) à chaque nouvelle réponse du joueur.
        public static event Action<bool> Changed;

        /// Le joueur a répondu, et à la version actuelle du texte.
        public static bool Answered => PlayerPrefs.GetInt(Key, -1) >= 0 && PlayerPrefs.GetInt(VersionKey, 1) >= Version;
        public static bool Accepted => Answered && PlayerPrefs.GetInt(Key, -1) == 1;

        public static void Set(bool accepted)
        {
            PlayerPrefs.SetInt(Key, accepted ? 1 : 0);
            PlayerPrefs.SetInt(VersionKey, Version);
            PlayerPrefs.Save();
            Changed?.Invoke(accepted);
        }
    }
}
