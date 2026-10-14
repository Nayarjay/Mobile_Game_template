using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace GameCore
{
    /// Relie le jeu au service Leaderboards de Unity Gaming Services : connexion
    /// anonyme, envoi du score de distance, lecture du classement.
    ///
    /// Prérequis côté Unity Dashboard (à faire une seule fois, hors code) :
    /// 1. Lier ce projet à un projet Unity Cloud (Project Settings > Services).
    /// 2. Créer un leaderboard sur dashboard.unity.com dont l'ID correspond
    ///    exactement à leaderboardId ci-dessous.
    /// 3. Vérifier que ce leaderboard est créé dans le même ENVIRONNEMENT que celui
    ///    visé par environmentName ci-dessous ("production" par défaut) — un
    ///    leaderboard créé en "development" est invisible pour le SDK en "production",
    ///    et inversement (erreur typique : "Leaderboard config could not be found").
    /// Tant que ce n'est pas fait, le jeu continue de fonctionner normalement :
    /// les appels échouent silencieusement (log en warning) sans planter.
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Tooltip("Doit correspondre exactement à l'ID du leaderboard créé sur le Unity Dashboard.")]
        [SerializeField] private string leaderboardId = "high_score";
        [SerializeField] private int topEntriesCount = 10;
        [Tooltip("Doit correspondre exactement à l'environnement du Dashboard où le leaderboard a été créé (visible en haut de la page du projet sur dashboard.unity.com). \"production\" par défaut.")]
        [SerializeField] private string environmentName = "production";

        public bool IsReady { get; private set; }

        private const string IdKey = "leaderboard_id", EnvKey = "leaderboard_env";
        private bool configuredByCode;

        /// Un classement a-t-il déjà été utilisé sur cet appareil ? (le menu principal,
        /// qui n'a pas de LeaderboardManager dans sa scène, s'en sert pour en créer un)
        public static bool HasSavedConfig => !string.IsNullOrEmpty(PlayerPrefs.GetString(IdKey, ""));

        /// Crée un LeaderboardManager réglé comme le dernier utilisé (menu principal).
        public static LeaderboardManager CreateFromSavedConfig(GameObject host)
        {
            if (Instance != null) return Instance;
            if (!HasSavedConfig) return null;
            var manager = host.AddComponent<LeaderboardManager>();
            manager.leaderboardId = PlayerPrefs.GetString(IdKey, manager.leaderboardId);
            manager.environmentName = PlayerPrefs.GetString(EnvKey, manager.environmentName);
            manager.configuredByCode = true;
            return manager;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // un doublon (scène additive) ne remplace pas le vrai
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private async void Start()
        {
            if (!configuredByCode)
            {
                // Mémorisé pour le menu principal (voir CreateFromSavedConfig)
                PlayerPrefs.SetString(IdKey, leaderboardId);
                PlayerPrefs.SetString(EnvKey, environmentName);
            }
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(environmentName));

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                IsReady = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Leaderboard indisponible (services Unity non configurés ?) : {e.Message}");
            }
        }

        /// Envoie un score entier (points, distance, etc. selon le prototype).
        public Task SubmitScore(int score) => SubmitDistance(score);

        /// Envoie la distance parcourue (en mètres, arrondie) comme score du joueur.
        public async Task SubmitDistance(float distanceMeters)
        {
            if (!IsReady) return;

            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, Mathf.RoundToInt(distanceMeters));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Envoi du score au leaderboard impossible : {e.Message}");
            }
        }

        /// Entrée du joueur (rang et score), ou null s'il n'a encore rien envoyé.
        public async Task<LeaderboardEntry> GetMyEntry()
        {
            if (!IsReady) return null;
            try
            {
                return await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            }
            catch (Exception)
            {
                return null; // pas encore de score : ce n'est pas une erreur
            }
        }

        /// Récupère les meilleures entrées (rang, nom, score). Liste vide si indisponible.
        public async Task<List<LeaderboardEntry>> GetTopScores()
        {
            if (!IsReady) return new List<LeaderboardEntry>();

            try
            {
                var options = new GetScoresOptions { Limit = topEntriesCount };
                var scores = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
                return scores.Results;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Lecture du leaderboard impossible : {e.Message}");
                return new List<LeaderboardEntry>();
            }
        }
    }
}
