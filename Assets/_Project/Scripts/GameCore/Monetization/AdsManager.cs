using System;
using System.Collections;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    /// Encapsule LevelPlay (ironSource) avec les garde-fous d'un jeu mobile sain.
    /// DEUX canaux rewarded distincts :
    /// - Revive : opt-in, une seule fois par partie (canal "revive").
    /// - Récompense (power-ups) : illimité, chaque visionnage accorde un bonus (canal "reward").
    /// Interstitiel : seulement après un Game Over définitif, grâce d'onboarding,
    /// au plus 1 partie sur N, cooldown, jamais après un revive, ni après une partie trop courte.
    /// Sans app key configurée, tout est désactivé proprement (aucun bouton cassé).
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        [Header("Interrupteur général")]
        [Tooltip("Décoché = système de pub totalement désactivé (SDK non initialisé). Idéal en développement sans clés. La simulation éditeur ci-dessous reste possible pour tester le flux.")]
        [SerializeField] private bool adsEnabled = false;

        [Header("Clés LevelPlay (Dashboard ironSource)")]
        [SerializeField] private string androidAppKey = "";
        [SerializeField] private string iosAppKey = "";
        [Tooltip("Ad Unit de la rewarded REVIVE (seconde chance en fin de partie).")]
        [SerializeField] private string reviveAdUnitId = "";
        [Tooltip("Ad Unit de la rewarded RÉCOMPENSE (power-ups, bonus divers).")]
        [SerializeField] private string rewardAdUnitId = "";
        [SerializeField] private string interstitialAdUnitId = "";
        [Tooltip("Dans l'éditeur : simule des pubs toujours prêtes et immédiatement récompensées, pour tester sans SDK.")]
        [SerializeField] private bool simulateAdsInEditor = true;

        [Header("Conditions interstitiel")]
        [SerializeField] private int firstGamesWithoutAds = 3;
        [SerializeField] private int gamesBetweenInterstitials = 3;
        [SerializeField] private float interstitialCooldown = 90f;
        [SerializeField] private float minRunDurationForAd = 20f;

        public bool ReviveUsedThisRun { get; private set; }
        /// Une pub récompensée est à l'écran (l'app passe alors en arrière-plan : ce n'est pas une vraie pause).
        public bool IsShowingAd { get; private set; }

        /// À appeler au début de chaque partie / niveau : le revive redevient disponible.
        public void BeginRun() => ReviveUsedThisRun = false;

        [Tooltip("Sur téléphone, tant que les vraies pubs sont coupées (Ads Enabled décoché) : les boutons de pub restent visibles et une fausse pub de 1,5 s donne la récompense. Pour les versions de TEST (y compris bêta Play Store). À DÉCOCHER avant la sortie publique si les vraies pubs ne sont pas branchées : sinon récompenses gratuites.")]
        [SerializeField] private bool simulateAdsWhenDisabled = true;

        /// Pubs simulées : dans l'éditeur, ou sur téléphone tant que les vraies pubs sont coupées.
        private bool EditorSim => (simulateAdsInEditor && Application.isEditor)
                                  || (simulateAdsWhenDisabled && !adsEnabled && !Application.isEditor);

        /// Revive disponible ? (non utilisé cette partie + une pub prête ou simulation)
        public bool CanOfferRevive => !ReviveUsedThisRun && (EditorSim || (adsEnabled && reviveChannel.IsReady));
        /// Pub récompense (power-up) disponible ?
        public bool CanShowRewarded => EditorSim || (adsEnabled && rewardChannel.IsReady);

        private RewardedChannel reviveChannel;
        private RewardedChannel rewardChannel;
        private LevelPlayInterstitialAd interstitial;
        private bool initialized;

        private static float lastInterstitialRealtime = -9999f;
        private static int gamesSinceInterstitial;
        private const string TotalGamesKey = "ads_total_games";

        private static int TotalGames
        {
            get => PlayerPrefs.GetInt(TotalGamesKey, 0);
            set { PlayerPrefs.SetInt(TotalGamesKey, value); PlayerPrefs.Save(); }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // un doublon (scène additive) ne remplace pas le vrai
            Instance = this;
            reviveChannel = new RewardedChannel(this, reviveAdUnitId);
            rewardChannel = new RewardedChannel(this, rewardAdUnitId);
        }

        private void Start()
        {
            if (!adsEnabled)
            {
                Debug.Log("AdsManager : système de pub désactivé (Ads Enabled décoché). SDK non initialisé.");
                return;
            }

            string appKey = Application.platform == RuntimePlatform.IPhonePlayer ? iosAppKey : androidAppKey;
            if (string.IsNullOrEmpty(appKey))
            {
                Debug.Log("AdsManager : pas d'app key LevelPlay configurée — publicités désactivées.");
                return;
            }
            // RGPD : le choix du joueur (écran de consentement du menu) part AVANT le démarrage.
            // Refus = pubs non personnalisées, pas de pubs coupées.
            LevelPlay.SetConsent(PrivacyConsent.Accepted);
            PrivacyConsent.Changed += OnConsentChanged; // le joueur peut revenir sur son choix en cours de route
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(appKey);
        }

        private void OnConsentChanged(bool accepted) => LevelPlay.SetConsent(accepted);

        private void OnDestroy()
        {
            PrivacyConsent.Changed -= OnConsentChanged;
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            reviveChannel?.Destroy();
            rewardChannel?.Destroy();
            interstitial?.DestroyAd();
        }

        private void OnInitSuccess(LevelPlayConfiguration config)
        {
            initialized = true;
            reviveChannel.Create();
            rewardChannel.Create();

            if (!string.IsNullOrEmpty(interstitialAdUnitId))
            {
                interstitial = new LevelPlayInterstitialAd(interstitialAdUnitId);
                interstitial.OnAdClosed += info => { AudioListener.pause = false; interstitial.LoadAd(); };
                interstitial.OnAdLoadFailed += error => StartCoroutine(RetryLoad(interstitial.LoadAd, 10f));
                interstitial.LoadAd();
            }
        }

        private void OnInitFailed(LevelPlayInitError error)
            => Debug.LogWarning($"AdsManager : échec d'initialisation LevelPlay — {error}");

        private IEnumerator RetryLoad(Action load, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            load();
        }

        /// Revive (fin de partie) : une seule fois par partie. onReward seulement si la
        /// pub est allée jusqu'à la récompense.
        public void ShowRevive(Action onReward, Action onFail)
        {
            if (!CanOfferRevive) { onFail?.Invoke(); return; }
            ReviveUsedThisRun = true;

            if (EditorSim) { Debug.Log("AdsManager : revive simulé."); SimulateRewarded(onReward); return; }
            reviveChannel.Show(onReward, onFail);
        }

        /// Récompense (power-ups, bonus) : illimité, canal séparé.
        public void ShowRewarded(Action onReward, Action onFail)
        {
            if (!CanShowRewarded) { onFail?.Invoke(); return; }

            if (EditorSim) { Debug.Log("AdsManager : récompense simulée."); SimulateRewarded(onReward); return; }
            rewardChannel.Show(onReward, onFail);
        }

        public void OnRunEnded(float runDurationSeconds)
        {
            TotalGames++;
            gamesSinceInterstitial++;
            TryShowInterstitial(runDurationSeconds);
        }

        private void TryShowInterstitial(float runDurationSeconds)
        {
            if (Store.NoAds) { Debug.Log("Interstitiel sauté : « sans pub » acheté."); return; }
            // Conditions de rythme (communes à la simulation et au réel)
            if (TotalGames <= firstGamesWithoutAds) { Debug.Log($"Interstitiel sauté : grâce d'onboarding ({TotalGames}/{firstGamesWithoutAds} parties)."); return; }
            if (ReviveUsedThisRun) { Debug.Log("Interstitiel sauté : revive utilisé cette partie."); return; }
            if (runDurationSeconds < minRunDurationForAd) { Debug.Log("Interstitiel sauté : partie trop courte."); return; }
            if (gamesSinceInterstitial < gamesBetweenInterstitials) { Debug.Log($"Interstitiel sauté : fréquence ({gamesSinceInterstitial}/{gamesBetweenInterstitials})."); return; }
            if (Time.realtimeSinceStartup - lastInterstitialRealtime < interstitialCooldown) { Debug.Log("Interstitiel sauté : cooldown."); return; }

            lastInterstitialRealtime = Time.realtimeSinceStartup;
            gamesSinceInterstitial = 0;

            if (EditorSim) { StartCoroutine(ShowSimulatedInterstitial()); return; }
            if (!initialized || interstitial == null || !interstitial.IsAdReady()) { Debug.Log("Interstitiel non montré : aucune pub prête."); return; }

            AudioListener.pause = true;
            interstitial.ShowAd();
        }

        /// Faux interstitiel plein écran (1,5 s) pour visualiser le rythme des pubs en
        /// développement, sans SDK.
        private IEnumerator ShowSimulatedInterstitial(Action then = null)
        {
            Debug.Log("AdsManager : PUB (simulation).");
            AudioListener.pause = true;
            // Une vraie pub met le jeu en arrière-plan : tout s'arrête. La fausse fait pareil,
            // sinon le chrono du niveau continuait de tourner derrière l'écran de pub.
            float timeBefore = Time.timeScale;
            Time.timeScale = 0f;

            var canvasGo = new GameObject("SimAdCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.96f);

            var txtGo = new GameObject("Text", typeof(Text));
            txtGo.transform.SetParent(panel.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.text = Application.isEditor ? "PUB (simulation)" : "AD\n(test version)";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 60;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            var trt = (RectTransform)txt.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;

            yield return new WaitForSecondsRealtime(1.5f);

            Destroy(canvasGo);
            AudioListener.pause = false;
            Time.timeScale = timeBefore;
            then?.Invoke();
        }

        /// Pub récompensée simulée : immédiate dans l'éditeur, fausse pub de 1,5 s sur téléphone
        /// (on voit ainsi le vrai déroulé : écran de pub, puis récompense).
        private void SimulateRewarded(Action onReward)
        {
            if (Application.isEditor) { onReward?.Invoke(); return; }
            IsShowingAd = true;
            StartCoroutine(ShowSimulatedInterstitial(() => { IsShowingAd = false; onReward?.Invoke(); }));
        }

        /// Un canal rewarded indépendant (sa propre pub, ses propres callbacks).
        private class RewardedChannel
        {
            private readonly AdsManager owner;
            private readonly string adUnitId;
            private LevelPlayRewardedAd ad;
            private Action pendingReward, pendingFail;
            private bool rewardEarned;

            public RewardedChannel(AdsManager owner, string adUnitId)
            {
                this.owner = owner;
                this.adUnitId = adUnitId;
            }

            public bool IsReady => ad != null && ad.IsAdReady();

            public void Create()
            {
                if (string.IsNullOrEmpty(adUnitId)) return;
                ad = new LevelPlayRewardedAd(adUnitId);
                ad.OnAdRewarded += (info, reward) => rewardEarned = true;
                ad.OnAdClosed += info => Resolve();
                ad.OnAdDisplayFailed += (info, error) => Resolve();
                ad.OnAdLoadFailed += error => owner.StartCoroutine(owner.RetryLoad(ad.LoadAd, 10f));
                ad.LoadAd();
            }

            public void Show(Action onReward, Action onFail)
            {
                pendingReward = onReward;
                pendingFail = onFail;
                rewardEarned = false;
                owner.IsShowingAd = true;
                AudioListener.pause = true;
                ad.ShowAd();
            }

            private void Resolve()
            {
                owner.IsShowingAd = false;
                AudioListener.pause = false;
                if (rewardEarned) pendingReward?.Invoke();
                else pendingFail?.Invoke();
                pendingReward = pendingFail = null;
                rewardEarned = false;
                ad.LoadAd();
            }

            public void Destroy() => ad?.DestroyAd();
        }
    }
}
