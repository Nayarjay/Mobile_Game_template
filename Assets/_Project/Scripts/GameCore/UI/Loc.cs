using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    /// LANGUES. Le texte anglais sert de clé : `Loc.T("PLAY")` rend « JOUER » en français,
    /// et le texte anglais tel quel s'il n'y a pas de traduction (rien ne casse si une
    /// phrase est oubliée). Au premier lancement la langue du téléphone est prise si elle
    /// est proposée, sinon l'anglais ; le joueur peut ensuite la changer depuis le menu
    /// (LanguageUI). Pour ajouter une langue : une valeur dans `Language`, son nom dans
    /// `NativeNames`, son code dans `Codes`, et une colonne de plus dans `Rows`.
    public static class Loc
    {
        public enum Language { English, French, Spanish, Portuguese, German }

        /// Nom de chaque langue, écrit dans sa propre langue (même ordre que l'enum).
        public static readonly string[] NativeNames = { "English", "Français", "Español", "Português", "Deutsch" };
        /// Code court : nom du drapeau (Resources/UI/Flags/<code>) et valeur sauvegardée.
        public static readonly string[] Codes = { "en", "fr", "es", "pt", "de" };

        private const string Key = "settings_language";
        private static bool loaded;
        private static Language current;
        private static Dictionary<string, string> table;

        public static event Action Changed;

        public static Language Current
        {
            get
            {
                if (!loaded) Load();
                return current;
            }
        }

        public static string Code => Codes[(int)Current];

        public static void Set(Language language)
        {
            loaded = true;
            current = language;
            table = null;
            PlayerPrefs.SetString(Key, Codes[(int)language]);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static void Load()
        {
            loaded = true;
            table = null;
            int saved = Array.IndexOf(Codes, PlayerPrefs.GetString(Key, ""));
            current = saved >= 0 ? (Language)saved : Detect();
        }

        private static Language Detect()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.French: return Language.French;
                case SystemLanguage.Spanish: return Language.Spanish;
                case SystemLanguage.Portuguese: return Language.Portuguese;
                case SystemLanguage.German: return Language.German;
                default: return Language.English;
            }
        }

        /// Traduit un texte anglais dans la langue du joueur.
        public static string T(string english)
        {
            if (string.IsNullOrEmpty(english)) return english;
            int column = (int)Current; // 0 = anglais : rien à chercher
            if (column == 0) return english;
            if (table == null)
            {
                table = new Dictionary<string, string>(Rows.Length);
                foreach (var row in Rows)
                    if (column < row.Length && !string.IsNullOrEmpty(row[column])) table[row[0]] = row[column];
            }
            return table.TryGetValue(english, out string translated) ? translated : english;
        }

        /// Traduit puis remplit les {0}, {1}...
        public static string F(string english, params object[] args) => string.Format(T(english), args);

        /// « 2ND », « 2E », « 2.º »... pour les classements.
        public static string Ordinal(int n)
        {
            switch (Current)
            {
                case Language.French: return n == 1 ? "1RE" : n + "E";
                case Language.Spanish: return n + ".º";
                case Language.Portuguese: return n + "º";
                case Language.German: return n + ".";
                default: return n == 1 ? "1ST" : n == 2 ? "2ND" : n == 3 ? "3RD" : n + "TH";
            }
        }

        // Anglais (la clé), français, espagnol, portugais (Brésil), allemand.
        // {0} dans les textes de consentement = nom du jeu (Loc.F(texte, Application.productName)).
        private static readonly string[][] Rows =
        {
            // --- Général ---
            new[] { "PLAY", "JOUER", "JUGAR", "JOGAR", "SPIELEN" },
            new[] { "Level {0}", "Niveau {0}", "Nivel {0}", "Nível {0}", "Level {0}" },
            new[] { "Score", "Score", "Puntos", "Pontos", "Punkte" },
            new[] { "Best", "Record", "Récord", "Recorde", "Rekord" },
            new[] { "VICTORY!", "VICTOIRE !", "¡VICTORIA!", "VITÓRIA!", "SIEG!" },
            new[] { "GAME OVER", "PARTIE TERMINÉE", "FIN DEL JUEGO", "FIM DE JOGO", "SPIEL VORBEI" },
            new[] { "Continue?", "Continuer ?", "¿Continuar?", "Continuar?", "Weiter?" },
            new[] { "PAUSED", "PAUSE", "PAUSA", "PAUSADO", "PAUSE" },
            new[] { "RESUME", "REPRENDRE", "REANUDAR", "CONTINUAR", "FORTSETZEN" },
            new[] { "RETRY", "REJOUER", "REINTENTAR", "TENTAR DE NOVO", "NOCHMAL" },
            new[] { "NEXT", "SUIVANT", "SIGUIENTE", "PRÓXIMO", "WEITER" },
            new[] { "HOME", "MENU", "INICIO", "INÍCIO", "MENÜ" },
            new[] { "SETTINGS", "RÉGLAGES", "AJUSTES", "AJUSTES", "EINSTELLUNGEN" },
            new[] { "Music", "Musique", "Música", "Música", "Musik" },
            new[] { "Sound", "Sons", "Sonido", "Som", "Sound" },
            new[] { "Vibration", "Vibrations", "Vibración", "Vibração", "Vibration" },
            new[] { "Language", "Langue", "Idioma", "Idioma", "Sprache" },
            new[] { "LOADING", "CHARGEMENT", "CARGANDO", "CARREGANDO", "LÄDT" },

            // --- Récompenses / boutique ---
            new[] { "+{0} coins", "+{0} pièces", "+{0} monedas", "+{0} moedas", "+{0} Münzen" },
            new[] { "DAILY GIFT", "CADEAU DU JOUR", "REGALO DIARIO", "PRESENTE DIÁRIO", "TAGESGESCHENK" },
            new[] { "Day {0}", "Jour {0}", "Día {0}", "Dia {0}", "Tag {0}" },
            new[] { "CLAIM", "RÉCUPÉRER", "RECLAMAR", "PEGAR", "ABHOLEN" },
            new[] { "WATCH AD", "VOIR UNE PUB", "VER ANUNCIO", "VER ANÚNCIO", "WERBUNG ANSEHEN" },
            new[] { "SHOP", "BOUTIQUE", "TIENDA", "LOJA", "SHOP" },
            new[] { "NO ADS", "SANS PUB", "SIN ANUNCIOS", "SEM ANÚNCIOS", "KEINE WERBUNG" },
            new[] { "RESTORE PURCHASES", "RESTAURER LES ACHATS", "RESTAURAR COMPRAS", "RESTAURAR COMPRAS", "KÄUFE WIEDERHERSTELLEN" },
            new[] { "Not enough coins", "Pas assez de pièces", "No tienes suficientes monedas", "Moedas insuficientes", "Nicht genug Münzen" },

            // --- Avis ---
            new[] { "Enjoying the game?", "Le jeu te plaît ?", "¿Te gusta el juego?", "Está gostando do jogo?", "Gefällt dir das Spiel?" },
            new[] { "RATE US", "NOTE-NOUS", "VALÓRANOS", "AVALIE-NOS", "BEWERTE UNS" },
            new[] { "LATER", "PLUS TARD", "MÁS TARDE", "DEPOIS", "SPÄTER" },

            // --- Confidentialité ---
            new[] { "YOUR PRIVACY", "TA VIE PRIVÉE", "TU PRIVACIDAD", "SUA PRIVACIDADE", "DEINE PRIVATSPHÄRE" },
            new[] { "ACCEPT", "ACCEPTER", "ACEPTAR", "ACEITAR", "AKZEPTIEREN" },
            new[] { "NO THANKS", "NON MERCI", "NO, GRACIAS", "NÃO, OBRIGADO", "NEIN DANKE" },
            new[] { "Privacy policy", "Politique de confidentialité", "Política de privacidad", "Política de privacidade", "Datenschutzerklärung" },
            new[] { "Credits", "Crédits", "Créditos", "Créditos", "Credits" },
            // Texte du consentement : version sans pub (mesure d'audience seule), puis version avec pubs
            new[]
            {
                "With your permission, {0} sends anonymous play statistics (levels played, device model, country) through Unity Analytics, to help us fix bugs and balance the game.\n\nThere is no advertising in this version. Your progress and your settings stay on your device.\n\nYou can change your choice at any time from the settings.",
                "Avec ton accord, {0} envoie des statistiques de jeu anonymes (niveaux joués, modèle de l'appareil, pays) via Unity Analytics, pour nous aider à corriger les bugs et à équilibrer le jeu.\n\nCette version ne contient aucune publicité. Ta progression et tes réglages restent sur ton appareil.\n\nTu peux modifier ton choix à tout moment depuis les réglages.",
                "Con tu permiso, {0} envía estadísticas de juego anónimas (niveles jugados, modelo del dispositivo, país) a través de Unity Analytics, para ayudarnos a corregir errores y equilibrar el juego.\n\nEsta versión no contiene publicidad. Tu progreso y tus ajustes se quedan en tu dispositivo.\n\nPuedes cambiar tu elección en cualquier momento desde los ajustes.",
                "Com a sua permissão, {0} envia estatísticas de jogo anônimas (níveis jogados, modelo do aparelho, país) pelo Unity Analytics, para nos ajudar a corrigir erros e equilibrar o jogo.\n\nEsta versão não tem publicidade. Seu progresso e seus ajustes ficam no seu aparelho.\n\nVocê pode mudar sua escolha a qualquer momento nos ajustes.",
                "Mit deiner Erlaubnis sendet {0} anonyme Spielstatistiken (gespielte Level, Gerätemodell, Land) über Unity Analytics, damit wir Fehler beheben und das Spiel ausbalancieren können.\n\nDiese Version enthält keine Werbung. Dein Fortschritt und deine Einstellungen bleiben auf deinem Gerät.\n\nDu kannst deine Wahl jederzeit in den Einstellungen ändern.",
            },
            new[]
            {
                "{0} is free thanks to ads.\n\nWith your permission, we and our partners use data from your device (such as an advertising ID) to show ads that match your interests, and anonymous play statistics to improve the game.\n\nIf you refuse, ads are still shown but they are not personalised. Your progress and your settings stay on your device.\n\nYou can change your choice at any time from the settings.",
                "{0} est gratuit grâce à la publicité.\n\nAvec ton accord, nous et nos partenaires utilisons des données de ton appareil (comme un identifiant publicitaire) pour afficher des publicités adaptées à tes centres d'intérêt, et des statistiques de jeu anonymes pour améliorer le jeu.\n\nSi tu refuses, des publicités restent affichées mais elles ne sont pas personnalisées. Ta progression et tes réglages restent sur ton appareil.\n\nTu peux modifier ton choix à tout moment depuis les réglages.",
                "{0} es gratis gracias a los anuncios.\n\nCon tu permiso, nosotros y nuestros socios usamos datos de tu dispositivo (como un identificador de publicidad) para mostrarte anuncios acordes con tus intereses, y estadísticas de juego anónimas para mejorar el juego.\n\nSi lo rechazas, se siguen mostrando anuncios pero no son personalizados. Tu progreso y tus ajustes se quedan en tu dispositivo.\n\nPuedes cambiar tu elección en cualquier momento desde los ajustes.",
                "{0} é gratuito graças aos anúncios.\n\nCom a sua permissão, nós e nossos parceiros usamos dados do seu aparelho (como um identificador de publicidade) para mostrar anúncios de acordo com seus interesses, e estatísticas de jogo anônimas para melhorar o jogo.\n\nSe você recusar, os anúncios continuam aparecendo, mas não são personalizados. Seu progresso e seus ajustes ficam no seu aparelho.\n\nVocê pode mudar sua escolha a qualquer momento nos ajustes.",
                "{0} ist dank Werbung kostenlos.\n\nMit deiner Erlaubnis nutzen wir und unsere Partner Daten deines Geräts (z. B. eine Werbe-ID), um Werbung passend zu deinen Interessen zu zeigen, sowie anonyme Spielstatistiken, um das Spiel zu verbessern.\n\nWenn du ablehnst, wird weiterhin Werbung gezeigt, aber sie ist nicht personalisiert. Dein Fortschritt und deine Einstellungen bleiben auf deinem Gerät.\n\nDu kannst deine Wahl jederzeit in den Einstellungen ändern.",
            },
        };
    }
}
