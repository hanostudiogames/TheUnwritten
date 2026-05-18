using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UI.Components
{
    public class TopSlideAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRectTransform = null;
        [SerializeField] private bool startHidden = true;
        [SerializeField] private float hiddenOffset = 160f;
        [SerializeField] private float showDuration = 0.35f;
        [SerializeField] private float hideDuration = 0.22f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Vector2 _shownAnchoredPosition;
        private bool _initialized;
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        private void Awake()
        {
            Initialize();
            SetVisibleInstant(!startHidden);
        }

        private void OnDisable()
        {
            panelRectTransform?.DOKill(false);
        }

        public void Show()
        {
            ShowAsync().Forget();
        }

        public void Hide()
        {
            HideAsync().Forget();
        }

        public UniTask ShowAsync(bool instant = false)
        {
            return SetVisibleAsync(true, instant);
        }

        public UniTask HideAsync(bool instant = false)
        {
            return SetVisibleAsync(false, instant);
        }

        public void ShowInstant()
        {
            SetVisibleInstant(true);
        }

        public void HideInstant()
        {
            SetVisibleInstant(false);
        }

        [ContextMenu("Top/Show")]
        private void ShowFromContextMenu()
        {
            Show();
        }

        [ContextMenu("Top/Hide")]
        private void HideFromContextMenu()
        {
            Hide();
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            if (panelRectTransform == null)
                panelRectTransform = transform as RectTransform;

            if (panelRectTransform == null)
                return;

            _shownAnchoredPosition = panelRectTransform.anchoredPosition;
            _isVisible = true;
            _initialized = true;
        }

        private async UniTask SetVisibleAsync(bool visible, bool instant)
        {
            Initialize();
            if (panelRectTransform == null)
                return;

            var targetPosition = visible ? _shownAnchoredPosition : GetHiddenAnchoredPosition();
            panelRectTransform.DOKill(false);
            _isVisible = visible;

            var duration = visible ? showDuration : hideDuration;
            if (instant || duration <= 0f || !Application.isPlaying)
            {
                panelRectTransform.anchoredPosition = targetPosition;
                return;
            }

            await panelRectTransform
                .DOAnchorPos(targetPosition, duration)
                .SetEase(visible ? showEase : hideEase)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void SetVisibleInstant(bool visible)
        {
            Initialize();
            if (panelRectTransform == null)
                return;

            panelRectTransform.DOKill(false);
            panelRectTransform.anchoredPosition = visible ? _shownAnchoredPosition : GetHiddenAnchoredPosition();
            _isVisible = visible;
        }

        private Vector2 GetHiddenAnchoredPosition()
        {
            if (panelRectTransform == null)
                return _shownAnchoredPosition;

            var hiddenDistance = GetHiddenDistance();
            return _shownAnchoredPosition + Vector2.up * (hiddenDistance + hiddenOffset);
        }

        private float GetHiddenDistance()
        {
            var rect = panelRectTransform.rect;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                panelRectTransform,
                panelRectTransform);

            return Mathf.Max(
                rect.height,
                -rect.yMin,
                -bounds.min.y,
                panelRectTransform.sizeDelta.y,
                1f);
        }
    }
}
