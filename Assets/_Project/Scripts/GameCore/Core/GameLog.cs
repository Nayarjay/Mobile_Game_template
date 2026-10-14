using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace GameCore
{
    /// Journal des événements du jeu (début/fin de niveau, achat, pub, mission...).
    /// - Chaque événement est transmis à `Forwarded` (voir UnityAnalyticsSink) ;
    /// - dans l'éditeur et en Development Build, il est aussi écrit en CSV (séparateur ;,
    ///   s'ouvre dans Excel ou Sheets) : Application.persistentDataPath/game_log.csv.
    /// Le jeu peut fournir le niveau et la map en cours via `Context`.
    public static class GameLog
    {
        /// Fichier écrit dans l'éditeur et en Development Build seulement : pas de journal
        /// qui grossit sans fin sur le téléphone des joueurs.
        public static bool Enabled = Application.isEditor || Debug.isDebugBuild;

        /// (événement, détails, numéro de niveau, map) : pour un outil de mesure en ligne.
        public static event Action<string, string, int, string> Forwarded;

        /// Renseigné par le jeu : (numéro de niveau, nom de la map) en cours.
        public static Func<(int level, string map)> Context;

        private static readonly string Session = Guid.NewGuid().ToString("N").Substring(0, 8);
        private static bool announced;

        public static string FilePath => Path.Combine(Application.persistentDataPath, "game_log.csv");

        public static void Event(string name, string details = "")
        {
            try
            {
                var (level, map) = Context != null ? Context() : (0, "");
                Forwarded?.Invoke(name, details ?? "", level, map ?? "");
                if (!Enabled) return;

                string line = string.Join(";",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    Session,
                    name,
                    level > 0 ? level.ToString() : "",
                    map ?? "",
                    (details ?? "").Replace(';', ','));

                string path = FilePath;
                bool newFile = !File.Exists(path);
                using (var w = new StreamWriter(path, true))
                {
                    if (newFile) w.WriteLine("date;session;evenement;niveau;map;details");
                    w.WriteLine(line);
                }

                if (!announced)
                {
                    announced = true;
                    Debug.Log($"GameLog : journal écrit dans {path}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GameLog : écriture impossible — {e.Message}");
            }
        }
    }
}
