using UnityEditor;
using UnityEngine;

namespace GameCore
{
    /// Deux réglages tout faits de la fenêtre de build Android (File → Build Profiles →
    /// Android), pour ne pas avoir à cocher les cases à la main :
    /// - TEST : fichier .apk à installer sur son téléphone, avec les erreurs affichées à
    ///   l'écran et le bouton DBG ;
    /// - STORE : fichier .aab pour Google Play, sans outils de test.
    /// Dans les deux cas : textures au format ASTC (le standard des téléphones actuels).
    /// Ne lance pas la build : il reste à cliquer sur Build (ou Build And Run).
    public static class AndroidBuildPresets
    {
        [MenuItem("Tools/Game Template/Build/Build Android — réglages de TEST (téléphone)", priority = 620)]
        private static void Test() => Apply(true);

        [MenuItem("Tools/Game Template/Build/Build Android — réglages STORE (Google Play)", priority = 621)]
        private static void Store() => Apply(false);

        private static void Apply(bool test)
        {
            EditorUserBuildSettings.development = test;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.buildAppBundle = !test;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // Taille du jeu :
            // - code C# inutilisé retiré (niveau « Low » : sûr avec DOTween, LevelPlay, UGS) ;
            // - niveaux de détail des textures jamais affichés retirés du build ;
            // - compression des données : LZ4 en test (build rapide), LZ4HC en store (plus petit,
            //   chargement aussi rapide) ;
            // - code natif : « Master » en store (plus petit et plus rapide, build plus longue).
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            PlayerSettings.mipStripping = true;
            PlayerSettings.SetIl2CppCompilerConfiguration(android, test ? Il2CppCompilerConfiguration.Release : Il2CppCompilerConfiguration.Master);
            bool compressed = SetCompression(test ? "Lz4" : "Lz4HC");

            string platform = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
                ? ""
                : "\n\nAttention : la plateforme active n'est pas Android. Dans File → Build Profiles, choisissez Android puis Switch Platform.";
            string store = test
                ? ""
                : "\n\nAvant d'envoyer sur Google Play : une icône, une signature (Player Settings → Publishing Settings → Keystore) et un numéro de version plus grand que le précédent.";
            EditorUtility.DisplayDialog("Build Android",
                (test
                    ? "Réglages de TEST appliqués : fichier .apk, Development Build coché (erreurs à l'écran, bouton DBG)."
                    : "Réglages STORE appliqués : fichier .aab, Development Build décoché.")
                + "\nTextures : ASTC. Code inutilisé retiré (Low), mipmaps inutiles retirées."
                + (compressed ? $"\nCompression : {(test ? "LZ4 (build rapide)" : "LZ4HC (plus petit)")}."
                              : "\nCompression : à choisir à la main dans le Build Profile (Compression Method : LZ4HC pour le store).")
                + platform + store
                + "\n\nSi vous utilisez un Build Profile personnalisé (et non le profil Android de base), ces cases se règlent dans ce profil.",
                "OK");
            Debug.Log($"Build Android : réglages {(test ? "TEST (.apk, Development Build)" : "STORE (.aab)")} appliqués.");
        }

        /// Méthode de compression du build Android. Unity ne la propose pas publiquement aux
        /// scripts : on passe par son réglage interne, et on prévient si elle a changé de nom.
        private static bool SetCompression(string method)
        {
            try
            {
                var compressionType = typeof(EditorUserBuildSettings).Assembly.GetType("UnityEditor.Compression");
                var setter = typeof(EditorUserBuildSettings).GetMethod("SetCompressionType",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                    null, new[] { typeof(BuildTargetGroup), compressionType }, null);
                if (compressionType == null || setter == null) return false;
                setter.Invoke(null, new[] { (object)BuildTargetGroup.Android, System.Enum.Parse(compressionType, method) });
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Build Android : compression non réglée ({e.Message}).");
                return false;
            }
        }
    }
}
