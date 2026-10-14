using System;
#if GAMECORE_NOTIFICATIONS && UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace GameCore
{
    /// NOTIFICATIONS LOCALES (rappels programmés sur le téléphone, sans serveur). Petite
    /// façade au-dessus du paquet Unity « Mobile Notifications » : tant que le paquet n'est
    /// pas installé (Tools → Game Template → Services → Installer les notifications), tout
    /// ceci ne fait rien et le jeu fonctionne pareil. Dans sa propre assembly pour que le
    /// reste du jeu compile avec ou sans le paquet.
    public static class LocalNotifier
    {
        private const string ChannelId = "reminders";

        /// Le paquet est-il installé (et la plateforme gérée) ?
        public static bool Available
        {
            get
            {
#if GAMECORE_NOTIFICATIONS && UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }

#if GAMECORE_NOTIFICATIONS && UNITY_ANDROID
        private static bool channelReady;

        private static void EnsureChannel()
        {
            if (channelReady) return;
            channelReady = true;
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id = ChannelId,
                Name = "Reminders",
                Importance = Importance.Default,
                Description = "Game reminders",
            });
        }
#endif

        /// Affiche la demande d'autorisation du système (Android 13 et plus ; sans effet avant).
        public static void RequestPermission()
        {
#if GAMECORE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed) new PermissionRequest();
#endif
        }

        /// Programme (ou remplace) la notification `id` pour l'heure donnée.
        public static void Schedule(int id, string title, string text, DateTime when)
        {
#if GAMECORE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            EnsureChannel();
            var notification = new AndroidNotification { Title = title, Text = text, FireTime = when };
            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, id);
#endif
        }

        /// Annule tout ce qui est programmé et efface les notifications déjà affichées.
        public static void CancelAll()
        {
#if GAMECORE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllNotifications();
#endif
        }
    }
}
