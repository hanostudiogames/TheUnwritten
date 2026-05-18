using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace UI.Cards
{
    [ExecuteAlways]
    public class CardFanSpread : MonoBehaviour
    {
        [Header("슬롯")]
        [SerializeField] private List<CardSlot> slots = new();

        [Header("부채 설정")]
        [SerializeField] private float radius = 200f;
        [SerializeField] private float angleSpacing = 10f;
        [SerializeField] private Vector2 centerOffset = new(0, -200f);

        [Header("애니메이션")]
        [SerializeField] private float duration = 0.25f;

        [Header("선택 제출 연출")]
        [SerializeField] private float submitPopDuration = 0.18f;
        [SerializeField] private float submitFlyDuration = 0.34f;
        [SerializeField] private float submitPopLift = 95f;
        [SerializeField] private float submitFlyLift = 280f;
        [SerializeField] private float submitPopScale = 1.24f;
        [SerializeField] private float submitEndScale = 0.72f;

        private readonly List<CardSlot> _activeSlots = new();
        private readonly List<CardHover> _activeHovers = new();
        private readonly List<CardSlot> _initialSlots = new();

        private Vector2[] _targetPositions;
        private Vector2[] _startPositions;
        private Vector3[] _startScales;
        private float[] _targetRotations;
        private float[] _startRotations;

        private CancellationTokenSource _cts;
        private bool _selectable = false;
        private bool _isShowingAnimation = false;
        private CanvasGroup _canvasGroup = null;

        public List<CardSlot> CardSlots => slots;
        
        private async void Start()
        {
            if (!Application.isPlaying)
                return;

            EnsureCanvasGroup();
            SetSelectable(_selectable);
            
            foreach (var slot in slots)
            {
                if (!IsValid(slot)) 
                    continue;
                
                slot.Rect.anchoredPosition = new Vector2(0, -800f);
            }

            await UniTask.Yield();

            _initialSlots.Clear();
            _initialSlots.AddRange(slots);
            await InitializeCards(_initialSlots);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                SpreadImmediate();
        }
        
        private async void OnEnable()
        {
            if (!Application.isPlaying)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                SpreadImmediate();
            }
        }

        private void OnDisable()
        {
            CancelAnimation();
        }

        private void OnDestroy()
        {
            CancelAnimation();
        }

        private void Awake()
        {
            EnsureCanvasGroup();
        }
        
        private async UniTask InitializeCards(List<CardSlot> initialCards)
        {
            CancelAnimation();
            slots.Clear();

            foreach (var slot in initialCards)
            {
                if (!IsValid(slot)) 
                    continue;

                await AddCardAnimated(slot);
                await UniTask.DelayFrame(1);
            }
        }

        public void SetSelectable(bool value)
        {
            _selectable = value;
            ApplySelectableState(value && !_isShowingAnimation);
        }

        private void ApplySelectableState(bool value)
        {
            EnsureCanvasGroup();

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = value;
                _canvasGroup.blocksRaycasts = value;
            }

            foreach (var slot in slots)
            {
                slot?.SetSelectable(value);
            }
        }

        public async UniTask PlayShowAnimationAsync()
        {
            if (!Application.isPlaying)
                return;

            _isShowingAnimation = true;
            SetSelectable(false);

            try
            {
                BuildActiveSlots();

                if (_activeSlots.Count == 0)
                    return;

                var slotsToShow = new List<CardSlot>(_activeSlots);
                for (int i = 0; i < slotsToShow.Count; i++)
                {
                    var slot = slotsToShow[i];
                    SetSlotAlpha(slot, 1f);
                    slot.Rect.anchoredPosition = new Vector2(0, -400f);
                    slot.Rect.localRotation = Quaternion.identity;
                    slot.Rect.localScale = Vector3.one * 0.8f;
                }

                await AnimateSequentially(slotsToShow, duration);
            }
            finally
            {
                _isShowingAnimation = false;
                ApplySelectableState(_selectable);
            }
        }

        private async UniTask AddCardAnimated(CardSlot newSlot)
        {
            if (!IsValid(newSlot)) 
                return;

            slots.Add(newSlot);
            newSlot.SetSelectable(_selectable);

            newSlot.Rect.anchoredPosition = new Vector2(0, -400f);
            newSlot.Rect.localScale = Vector3.one * 0.8f;

            await AnimateAll(duration);
        }

        public async UniTask PlaySubmitAnimationAsync(CardSlot selectedSlot)
        {
            if (!Application.isPlaying || !IsValid(selectedSlot))
                return;

            CancelAnimation();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            BuildActiveSlots();
            if (_activeSlots.Count == 0 || !_activeSlots.Contains(selectedSlot))
                return;

            ApplySelectableState(false);

            int count = _activeSlots.Count;
            var startPositions = new Vector2[count];
            var startScales = new Vector3[count];
            var startRotations = new float[count];
            var startAlphas = new float[count];
            var canvasGroups = new CanvasGroup[count];
            int selectedIndex = _activeSlots.IndexOf(selectedSlot);

            for (int i = 0; i < count; i++)
            {
                var slot = _activeSlots[i];
                var hover = GetHover(slot);
                hover?.ForceExit();

                startPositions[i] = slot.Rect.anchoredPosition;
                startScales[i] = slot.Rect.localScale;
                startRotations[i] = slot.Rect.localEulerAngles.z;
                canvasGroups[i] = GetOrCreateSlotCanvasGroup(slot);
                startAlphas[i] = canvasGroups[i] != null ? canvasGroups[i].alpha : 1f;
            }

            selectedSlot.Rect.SetAsLastSibling();

            bool cancelled = await AnimateSubmitPopAsync(
                selectedIndex,
                startPositions,
                startScales,
                startRotations,
                startAlphas,
                canvasGroups,
                token);

            if (cancelled)
                return;

            bool flyCancelled = await AnimateSubmitFlyAsync(
                selectedIndex,
                startPositions,
                startScales,
                startRotations,
                startAlphas,
                canvasGroups,
                token);

            if (!flyCancelled)
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        private void SpreadImmediate()
        {
            BuildActiveSlots();
            if (_activeSlots.Count == 0) 
                return;

            EnsureCache(_activeSlots.Count);
            CalculateTargets(_activeSlots.Count);

            for (int i = 0; i < _activeSlots.Count; i++)
            {
                var slot = _activeSlots[i];

                slot.Rect.anchoredPosition = _targetPositions[i];
                slot.Rect.localRotation = Quaternion.Euler(0, 0, _targetRotations[i]);
                slot.Rect.localScale = Vector3.one;

                slot.Rect.SetSiblingIndex(i);

                var hover = _activeHovers[i];
                if (hover != null)
                {
                    hover.ForceExit();
                    hover.SetOrigin(_targetPositions[i],
                        Quaternion.Euler(0, 0, _targetRotations[i]));
                }
            }
        }

        #region Core Animation

        private async UniTask AnimateAll(float animDuration)
        {
            BuildActiveSlots();
            int count = _activeSlots.Count;
            if (count == 0) 
                return;

            await AnimateSlots(_activeSlots, count, animDuration);
        }

        private async UniTask AnimateSequentially(List<CardSlot> animationSlots, float animDuration)
        {
            if (animationSlots == null || animationSlots.Count == 0)
                return;

            for (int count = 1; count <= animationSlots.Count; count++)
            {
                if (!isActiveAndEnabled)
                    return;

                if (!await AnimateSlots(animationSlots, count, animDuration))
                    return;

                await UniTask.DelayFrame(1);
            }
        }

        private async UniTask<bool> AnimateSlots(List<CardSlot> animationSlots, int count, float animDuration)
        {
            if (animationSlots == null || count == 0)
                return false;

            if (animDuration <= 0f)
            {
                ApplyImmediate(animationSlots, count);
                return true;
            }

            CancelAnimation();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            EnsureCache(count);
            CalculateTargets(count);

            for (int i = 0; i < count; i++)
            {
                var slot = animationSlots[i];

                _startPositions[i] = slot.Rect.anchoredPosition;
                _startRotations[i] = slot.Rect.localEulerAngles.z;
                _startScales[i] = slot.Rect.localScale;

                var hover = GetHover(slot);
                if (hover != null)
                    hover.ForceExit();
            }

            float time = 0f;

            while (time < animDuration)
            {
                if (token.IsCancellationRequested) return false;

                time += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - (time / animDuration), 2f);

                for (int i = 0; i < count; i++)
                {
                    var slot = animationSlots[i];

                    slot.Rect.anchoredPosition =
                        Vector2.Lerp(_startPositions[i], _targetPositions[i], t);

                    float rot = Mathf.LerpAngle(_startRotations[i], _targetRotations[i], t);
                    slot.Rect.localRotation = Quaternion.Euler(0, 0, rot);
                    slot.Rect.localScale = Vector3.Lerp(_startScales[i], Vector3.one, t);
                }

                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow())
                    return false;
            }

            // 최종 보정
            for (int i = 0; i < count; i++)
            {
                var slot = animationSlots[i];

                slot.Rect.anchoredPosition = _targetPositions[i];
                slot.Rect.localRotation =
                    Quaternion.Euler(0, 0, _targetRotations[i]);
                slot.Rect.localScale = Vector3.one;

                slot.Rect.SetSiblingIndex(i);

                var hover = GetHover(slot);
                if (hover != null)
                {
                    hover.SetOrigin(_targetPositions[i],
                        Quaternion.Euler(0, 0, _targetRotations[i]));
                }
            }

            return true;
        }

        #endregion

        #region Submit Animation

        private async UniTask<bool> AnimateSubmitPopAsync(
            int selectedIndex,
            Vector2[] startPositions,
            Vector3[] startScales,
            float[] startRotations,
            float[] startAlphas,
            CanvasGroup[] canvasGroups,
            CancellationToken token)
        {
            float animDuration = Mathf.Max(0.001f, submitPopDuration);
            float time = 0f;

            while (time < animDuration)
            {
                if (token.IsCancellationRequested)
                    return true;

                time += Time.deltaTime;
                float t = EaseOutBack(Mathf.Clamp01(time / animDuration));

                ApplySubmitPose(
                    selectedIndex,
                    startPositions,
                    startScales,
                    startRotations,
                    startAlphas,
                    canvasGroups,
                    t,
                    false);

                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow())
                    return true;
            }

            ApplySubmitPose(
                selectedIndex,
                startPositions,
                startScales,
                startRotations,
                startAlphas,
                canvasGroups,
                1f,
                false);

            return false;
        }

        private async UniTask<bool> AnimateSubmitFlyAsync(
            int selectedIndex,
            Vector2[] startPositions,
            Vector3[] startScales,
            float[] startRotations,
            float[] startAlphas,
            CanvasGroup[] canvasGroups,
            CancellationToken token)
        {
            float animDuration = Mathf.Max(0.001f, submitFlyDuration);
            float time = 0f;

            while (time < animDuration)
            {
                if (token.IsCancellationRequested)
                    return true;

                time += Time.deltaTime;
                float t = EaseOutCubic(Mathf.Clamp01(time / animDuration));

                ApplySubmitPose(
                    selectedIndex,
                    startPositions,
                    startScales,
                    startRotations,
                    startAlphas,
                    canvasGroups,
                    t,
                    true);

                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow())
                    return true;
            }

            ApplySubmitPose(
                selectedIndex,
                startPositions,
                startScales,
                startRotations,
                startAlphas,
                canvasGroups,
                1f,
                true);

            return false;
        }

        private void ApplySubmitPose(
            int selectedIndex,
            Vector2[] startPositions,
            Vector3[] startScales,
            float[] startRotations,
            float[] startAlphas,
            CanvasGroup[] canvasGroups,
            float t,
            bool flyOut)
        {
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                var slot = _activeSlots[i];
                if (!IsValid(slot))
                    continue;

                bool selected = i == selectedIndex;
                if (selected)
                {
                    Vector2 popPosition = new(0f, startPositions[i].y + submitPopLift);
                    Vector2 flyPosition = new(0f, startPositions[i].y + submitFlyLift);
                    Vector3 popScale = Vector3.one * submitPopScale;
                    Vector3 flyScale = Vector3.one * submitEndScale;
                    float rotation = flyOut ? 0f : Mathf.LerpAngle(startRotations[i], 0f, t);

                    slot.Rect.anchoredPosition = flyOut
                        ? Vector2.Lerp(popPosition, flyPosition, t)
                        : Vector2.Lerp(startPositions[i], popPosition, t);
                    slot.Rect.localScale = flyOut
                        ? Vector3.Lerp(popScale, flyScale, t)
                        : Vector3.Lerp(startScales[i], popScale, t);
                    slot.Rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

                    float alpha = flyOut ? Mathf.Lerp(1f, 0f, t) : Mathf.Lerp(startAlphas[i], 1f, t);
                    SetCanvasGroupAlpha(canvasGroups[i], alpha);
                }
                else
                {
                    Vector2 popPosition = startPositions[i] + Vector2.down * 42f;
                    Vector2 flyPosition = startPositions[i] + Vector2.down * 85f;
                    Vector3 popScale = Vector3.one * 0.92f;
                    Vector3 flyScale = Vector3.one * 0.82f;
                    float popRotation = Mathf.LerpAngle(startRotations[i], 0f, 0.65f);
                    float rotation = flyOut
                        ? Mathf.LerpAngle(popRotation, 0f, t)
                        : Mathf.LerpAngle(startRotations[i], popRotation, t);

                    slot.Rect.anchoredPosition = flyOut
                        ? Vector2.Lerp(popPosition, flyPosition, t)
                        : Vector2.Lerp(startPositions[i], popPosition, t);
                    slot.Rect.localScale = flyOut
                        ? Vector3.Lerp(popScale, flyScale, t)
                        : Vector3.Lerp(startScales[i], popScale, t);
                    slot.Rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
                    SetCanvasGroupAlpha(
                        canvasGroups[i],
                        flyOut ? Mathf.Lerp(0.38f, 0f, t) : Mathf.Lerp(startAlphas[i], 0.38f, t));
                }
            }
        }

        #endregion

        #region Layout

        private void CalculateTargets(int count)
        {
            if (count == 1)
            {
                _targetPositions[0] = new Vector2(centerOffset.x, centerOffset.y + radius);
                _targetRotations[0] = 0f;
                return;
            }

            float totalSpread = angleSpacing * (count - 1);
            float startAngle = totalSpread * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle - (angleSpacing * i);
                float rad = angle * Mathf.Deg2Rad;

                _targetPositions[i] = new Vector2(
                    centerOffset.x + Mathf.Sin(rad) * radius,
                    centerOffset.y + Mathf.Cos(rad) * radius
                );

                _targetRotations[i] = -angle;
            }
        }

        private void EnsureCache(int count)
        {
            if (_targetPositions == null || _targetPositions.Length != count)
            {
                _targetPositions = new Vector2[count];
                _targetRotations = new float[count];
                _startPositions = new Vector2[count];
                _startRotations = new float[count];
                _startScales = new Vector3[count];
            }
        }

        private void ApplyImmediate(List<CardSlot> animationSlots, int count)
        {
            EnsureCache(count);
            CalculateTargets(count);

            for (int i = 0; i < count; i++)
            {
                var slot = animationSlots[i];
                slot.Rect.anchoredPosition = _targetPositions[i];
                slot.Rect.localRotation = Quaternion.Euler(0, 0, _targetRotations[i]);
                slot.Rect.localScale = Vector3.one;
                slot.Rect.SetSiblingIndex(i);

                var hover = GetHover(slot);
                if (hover != null)
                {
                    hover.ForceExit();
                    hover.SetOrigin(_targetPositions[i],
                        Quaternion.Euler(0, 0, _targetRotations[i]));
                }
            }
        }

        #endregion

        #region Helpers

        private void BuildActiveSlots()
        {
            _activeSlots.Clear();
            _activeHovers.Clear();

            for (int i = 0; i < slots.Count; ++i)
            {
                var slot = slots[i];
                if (!IsValid(slot))
                    continue;

                _activeSlots.Add(slot);
                _activeHovers.Add(slot.TryGetComponent<CardHover>(out var hover) ? hover : null);
            }
        }

        private void CancelAnimation()
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup != null)
                return;

            if (!TryGetComponent(out _canvasGroup))
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private CanvasGroup GetOrCreateSlotCanvasGroup(CardSlot slot)
        {
            if (slot == null)
                return null;

            if (!slot.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = slot.gameObject.AddComponent<CanvasGroup>();

            return canvasGroup;
        }

        private void SetSlotAlpha(CardSlot slot, float alpha)
        {
            SetCanvasGroupAlpha(GetOrCreateSlotCanvasGroup(slot), alpha);
        }

        private void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        }

        private float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private CardHover GetHover(CardSlot slot)
        {
            if (slot == null)
                return null;

            return slot.TryGetComponent<CardHover>(out var hover) ? hover : null;
        }

        public void DeactivateCardSlots()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; ++i)
            {
                SetSlotAlpha(slots[i], 1f);
                slots[i]?.Deactivate();
            }
        }

        private bool IsValid(CardSlot slot)
        {
            return slot != null && slot.isActiveAndEnabled && slot.Rect != null;
        }
        #endregion
    }
}
