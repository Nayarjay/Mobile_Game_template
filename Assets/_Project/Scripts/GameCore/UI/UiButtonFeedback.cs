using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore
{
    /// Retour tactile d'un bouton : il s'enfonce sous le doigt puis rebondit au relâché.
    /// Ajouté tout seul sur tous les boutons du jeu (voir GameBoot) ;
    /// fonctionne aussi en pause (temps figé).
    [DisallowMultipleComponent]
    public class UiButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.9f;
        [SerializeField] private float pressDuration = 0.07f;
        [SerializeField] private float releaseDuration = 0.28f;

        private Button button;
        private Vector3 baseScale = Vector3.one;
        private bool pressed;
        private Tween tween;

        public static void Attach(Button button)
        {
            if (button != null && button.GetComponent<UiButtonFeedback>() == null)
                button.gameObject.AddComponent<UiButtonFeedback>();
        }

        private void Awake() => button = GetComponent<Button>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.IsInteractable()) return;
            if (!pressed) baseScale = transform.localScale.sqrMagnitude > 0.0001f ? transform.localScale : Vector3.one;
            pressed = true;
            GameAudio.Click();
            Play(baseScale * pressedScale, pressDuration, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!pressed) return;
            pressed = false;
            Play(baseScale, releaseDuration, Ease.OutBack);
        }

        private void Play(Vector3 target, float duration, Ease ease)
        {
            if (tween != null && tween.IsActive()) tween.Kill();
            tween = transform.DOScale(target, duration).SetEase(ease).SetUpdate(true).SetLink(gameObject);
        }

        private void OnDisable()
        {
            // Fenêtre refermée par le clic : le rebond est coupé en route. Sans retour à la
            // taille de base, le bouton resterait rétréci (et rétrécirait à chaque clic).
            bool animating = tween != null && tween.IsActive();
            if (animating) tween.Kill();
            if (pressed || animating) transform.localScale = baseScale;
            pressed = false;
        }
    }
}
