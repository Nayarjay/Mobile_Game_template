// Envoi des événements du jeu (GameLog) à Unity Analytics.
//
// Compilé seulement quand le package com.unity.services.analytics est installé
// (symbole GAMECORE_ANALYTICS posé par GameCore.asmdef). Pour que les événements
// apparaissent sur dashboard.unity.com > Analytics > Event Manager, créer un événement
// personnalisé « game_event » avec les paramètres
// eventName (string), levelNumber (integer), mapName (string), details (string).
// Rien n'est collecté sans le « oui » du joueur (PrivacyConsent).
#if GAMECORE_ANALYTICS
using System;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

namespace GameCore
{
    public static class UnityAnalyticsSink
    {
        private const string EventName = "game_event";
        private static bool ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GameLog.Forwarded += Send;
            PrivacyConsent.Changed += OnConsentChanged;
            if (PrivacyConsent.Accepted) Begin(); // RGPD : rien n'est collecté sans un « oui »
        }

        private static void OnConsentChanged(bool accepted)
        {
            if (accepted) { if (!ready) Begin(); return; }
            bool wasCollecting = ready;
            ready = false;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized) return;
                AnalyticsService.Instance.StopDataCollection();
                // Retrait du consentement (RGPD, droit à l'effacement) : ce qui avait été envoyé est supprimé
                if (wasCollecting) AnalyticsService.Instance.RequestDataDeletion();
            }
            catch (Exception e) { Debug.LogWarning($"Analytics : arrêt impossible — {e.Message}"); }
        }

        private static async void Begin()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    await UnityServices.InitializeAsync();
                AnalyticsService.Instance.StartDataCollection();
                ready = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Analytics indisponible : {e.Message}");
            }
        }

        private static void Send(string name, string details, int levelNumber, string map)
        {
            if (!ready) return;
            try
            {
                var e = new CustomEvent(EventName)
                {
                    { "eventName", name },
                    { "levelNumber", levelNumber },
                    { "mapName", map ?? "" },
                    { "details", details ?? "" },
                };
                AnalyticsService.Instance.RecordEvent(e);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Analytics : événement non envoyé — {ex.Message}");
            }
        }
    }
}
#endif
