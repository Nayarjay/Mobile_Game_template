using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    /// À poser sur le Canvas du HUD : au lancement, range tous ses enfants dans un
    /// conteneur "SafeArea" calé sur la zone sûre de l'écran (encoche, coins arrondis,
    /// barre de gestes). Rien ne change dans l'éditeur : la hiérarchie reste la vôtre.
    [DisallowMultipleComponent]
    public class SafeAreaRoot : MonoBehaviour
    {
        private RectTransform area;
        private Rect applied;
        private Vector2Int appliedScreen;

        private void Awake()
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            area = (RectTransform)go.transform;
            area.SetParent(transform, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;

            // Même rect que le canvas au départ : ancres et positions des enfants inchangées.
            var children = new List<Transform>();
            foreach (Transform c in transform) if (c != area) children.Add(c);
            foreach (var c in children) c.SetParent(area, false);

            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != applied || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y)
                Apply();
        }

        /// Les éléments créés par code APRÈS le lancement (compteur de pièces, boutons
        /// bonus, bouton DBG...) arrivent directement sur le canvas : ceux qui sont
        /// accrochés en haut ou en bas de l'écran rejoignent la zone sûre, sinon ils ne
        /// suivraient pas le reste du HUD sous l'encoche. Les fenêtres centrées et les
        /// fonds plein écran restent où ils sont (ils doivent couvrir tout l'écran).
        private void LateUpdate()
        {
            if (transform.childCount <= 1) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (!(transform.GetChild(i) is RectTransform child) || child == area) continue;
                bool pinned = Mathf.Approximately(child.anchorMin.y, child.anchorMax.y)
                              && (child.anchorMin.y <= 0.001f || child.anchorMin.y >= 0.999f);
                if (pinned) child.SetParent(area, false);
            }
        }

        private void Apply()
        {
            applied = Screen.safeArea;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            area.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
            area.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
            area.offsetMin = area.offsetMax = Vector2.zero;
        }
    }
}
