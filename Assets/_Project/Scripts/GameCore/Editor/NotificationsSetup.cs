using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GameCore
{
    /// Installe le paquet Unity « Mobile Notifications », dont les rappels du jeu ont besoin
    /// (cadeau du jour, événement). Sans lui le jeu marche pareil, simplement sans rappels.
    public static class NotificationsSetup
    {
        private const string Package = "com.unity.mobile.notifications";

        [MenuItem("Tools/Game Template/Services/Installer les notifications (rappels)", priority = 604)]
        private static void Install()
        {
            if (LocalNotifier.Available)
            {
                EditorUtility.DisplayDialog("Notifications", "Le paquet Mobile Notifications est déjà installé : les rappels sont actifs dans le build Android.", "OK");
                return;
            }
            Client.Add(Package);
            Debug.Log("Installation de " + Package + " demandée : Unity recompile tout seul quand c'est fini (voir Window → Package Manager).");
            EditorUtility.DisplayDialog("Notifications", "Installation du paquet Mobile Notifications lancée.\n\nUnity va recompiler dans quelques instants. Les rappels fonctionneront ensuite dans le build Android (rien à voir dans l'éditeur).", "OK");
        }
    }
}
