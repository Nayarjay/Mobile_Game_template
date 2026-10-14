using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCore
{
    /// Bouton qui ouvre une scène (doit être dans la liste de build). Pratique pour
    /// relier les écrans d'un prototype sans écrire de code.
    [RequireComponent(typeof(Button))]
    public class SceneButton : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        private void Awake() => GetComponent<Button>().onClick.AddListener(Load);

        private void Load()
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            GameLog.Event("scene_open", sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }
}
