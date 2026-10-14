using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    /// Adapte l'échelle d'un canvas à la forme de l'écran. L'interface est dessinée pour
    /// du 9:16 (1080×1920) :
    /// - écran plus ÉTROIT (téléphones longs, 9:19,5 ou 9:21) : calée sur la largeur, la
    ///   hauteur en plus laisse de l'air ;
    /// - écran plus LARGE (tablettes 3:4, pliables ouverts) : calée sur la hauteur, sinon
    ///   les panneaux hauts dépasseraient de l'écran.
    /// Ajouté tout seul sur chaque canvas au chargement d'une scène (GameBoot).
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasAspectFit : MonoBehaviour
    {
        private CanvasScaler scaler;
        private Vector2Int applied;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != applied.x || Screen.height != applied.y) Apply(); // rotation, pliable, fenêtre redimensionnée
        }

        private void Apply()
        {
            applied = new Vector2Int(Screen.width, Screen.height);
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || Screen.height <= 0) return;

            Vector2 reference = scaler.referenceResolution;
            float screenAspect = (float)Screen.width / Screen.height;
            float referenceAspect = reference.x / Mathf.Max(1f, reference.y);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = screenAspect > referenceAspect ? 1f : 0f;
        }
    }
}
