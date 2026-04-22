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

        private readonly List<CardSlot> _activeSlots = new();
        private readonly List<CardHover> _activeHovers = new();
        private readonly List<CardSlot> _initialSlots = new();

        private Vector2[] _targetPositions;
        private Vector2[] _startPositions;
        private Vector3[] _startScales;
        private float[] _targetRotations;
        private float[] _startRotations;

        private CancellationTokenSource _cts;
        private bool _selectable = true;

        public List<CardSlot> CardSlots => slots;

        #region Unity

        private async void Start()
        {
            if (!Application.isPlaying)
                return;

            SetSelectable(_selectable);
            
            foreach (var slot in slots)
            {
                if (!IsValid(slot)) continue;
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

        #endregion

        #region Public

        public async UniTask InitializeCards(List<CardSlot> initialCards)
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

            foreach (var slot in slots)
                slot?.SetSelectable(value);
        }

        public async UniTask AddCardAnimated(CardSlot newSlot)
        {
            if (!IsValid(newSlot)) return;

            slots.Add(newSlot);
            newSlot.SetSelectable(_selectable);

            newSlot.Rect.anchoredPosition = new Vector2(0, -400f);
            newSlot.Rect.localScale = Vector3.one * 0.8f;

            await AnimateAll(duration);
        }

        public void SpreadImmediate()
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

        #endregion

        #region Core Animation

        private async UniTask AnimateAll(float animDuration)
        {
            BuildActiveSlots();
            int count = _activeSlots.Count;
            if (count == 0) 
                return;

            if (animDuration <= 0f)
            {
                SpreadImmediate();
                return;
            }

            CancelAnimation();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            EnsureCache(count);
            CalculateTargets(count);

            for (int i = 0; i < count; i++)
            {
                var slot = _activeSlots[i];

                _startPositions[i] = slot.Rect.anchoredPosition;
                _startRotations[i] = slot.Rect.localEulerAngles.z;
                _startScales[i] = slot.Rect.localScale;

                var hover = _activeHovers[i];
                if (hover != null)
                    hover.ForceExit();
            }

            float time = 0f;

            while (time < animDuration)
            {
                if (token.IsCancellationRequested) return;

                time += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - (time / animDuration), 2f);

                for (int i = 0; i < count; i++)
                {
                    var slot = _activeSlots[i];

                    slot.Rect.anchoredPosition =
                        Vector2.Lerp(_startPositions[i], _targetPositions[i], t);

                    float rot = Mathf.LerpAngle(_startRotations[i], _targetRotations[i], t);
                    slot.Rect.localRotation = Quaternion.Euler(0, 0, rot);
                    slot.Rect.localScale = Vector3.Lerp(_startScales[i], Vector3.one, t);
                }

                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow())
                    return;
            }

            // 최종 보정
            for (int i = 0; i < count; i++)
            {
                var slot = _activeSlots[i];

                slot.Rect.anchoredPosition = _targetPositions[i];
                slot.Rect.localRotation =
                    Quaternion.Euler(0, 0, _targetRotations[i]);
                slot.Rect.localScale = Vector3.one;

                var hover = _activeHovers[i];
                if (hover != null)
                {
                    hover.SetOrigin(_targetPositions[i],
                        Quaternion.Euler(0, 0, _targetRotations[i]));
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

        public void DeactivateCardSlots()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; ++i)
            {
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
