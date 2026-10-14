using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCore
{
    /// Point d'entrée du jeu, posé dans la scène Boot (première scène du build) avec les
    /// services (AdsManager, LeaderboardManager...). Il :
    /// - garde ce GameObject et ses services d'une scène à l'autre ;
    /// - règle la fréquence d'images et la mise en veille de l'écran ;
    /// - ajoute à chaque scène chargée CanvasAspectFit sur les canvas et UiButtonFeedback
    ///   sur les boutons (rien à poser à la main) ;
    /// - ouvre ensuite la première vraie scène (le menu).
    [DisallowMultipleComponent]
    public class GameBoot : MonoBehaviour
    {
        public static GameBoot Instance { get; private set; }

        [Tooltip("Scène ouverte après le démarrage (doit être dans la liste de build).")]
        [SerializeField] private string firstScene = "MainMenu";
        [SerializeField] private int targetFrameRate = 60;
        [Tooltip("Son des boutons (UiButtonFeedback). Vide = pas de son.")]
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            GameAudio.ClickClip = clickClip;
            GameAudio.SfxVolume = sfxVolume;
            GameAudio.MusicVolume = musicVolume;

            SceneManager.sceneLoaded += OnSceneLoaded;
            GameLog.Event("app_start", Application.version);
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(firstScene) && SceneManager.GetActiveScene().name != firstScene)
                SceneManager.LoadScene(firstScene);
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
                    if (scaler.GetComponent<CanvasAspectFit>() == null) scaler.gameObject.AddComponent<CanvasAspectFit>();
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                    UiButtonFeedback.Attach(button);
            }
        }
    }
}
