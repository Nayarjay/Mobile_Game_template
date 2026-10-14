using UnityEngine;

namespace GameCore
{
    /// Fait tourner un élément d'UI (indicateur de chargement), même quand le jeu est en pause.
    public class UiSpin : MonoBehaviour
    {
        [Tooltip("Degrés par seconde (négatif = sens horaire).")]
        [SerializeField] private float speed = -270f;

        private void Update() => transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime);
    }
}
