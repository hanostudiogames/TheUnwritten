using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;

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

    private Vector2[] _targetPositions;
    private float[] _targetRotations;

    private CancellationTokenSource _cts;

    #region Unity

    private async void Start()
    {
        SetSelectable(true);
        
        foreach (var slot in slots)
        {
            if (!IsValid(slot)) continue;
            slot.Rect.anchoredPosition = new Vector2(0, -800f);
        }

        await UniTask.Yield();

        await InitializeCards(new List<CardSlot>(slots));
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
        _cts?.Cancel();
        _cts?.Dispose();
    }

    #endregion

    #region Public

    public async UniTask InitializeCards(List<CardSlot> initialCards)
    {
        slots.Clear();

        foreach (var slot in initialCards)
        {
            if (!IsValid(slot)) continue;

            await AddCardAnimated(slot);
            await UniTask.Delay(10);
        }
    }

    public void SetSelectable(bool value)
    {
        foreach (var slot in slots)
            slot?.SetSelectable(value);
    }

    public async UniTask AddCardAnimated(CardSlot newSlot)
    {
        if (!IsValid(newSlot)) return;

        slots.Add(newSlot);

        newSlot.Rect.anchoredPosition = new Vector2(0, -400f);
        newSlot.Rect.localScale = Vector3.one * 0.8f;

        await AnimateAll(0.15f); // 기존 카드 밀기
        await AnimateAll(duration); // 전체 정렬
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

            slot.Rect.SetSiblingIndex(i);

            if (slot.TryGetComponent<CardHover>(out var hover))
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

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        EnsureCache(count);
        CalculateTargets(count);

        Vector2[] startPos = new Vector2[count];
        float[] startRot = new float[count];

        for (int i = 0; i < count; i++)
        {
            var slot = _activeSlots[i];

            startPos[i] = slot.Rect.anchoredPosition;
            startRot[i] = slot.Rect.localEulerAngles.z;

            if (slot.TryGetComponent<CardHover>(out var hover))
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
                    Vector2.Lerp(startPos[i], _targetPositions[i], t);

                float rot = Mathf.LerpAngle(startRot[i], _targetRotations[i], t);
                slot.Rect.localRotation = Quaternion.Euler(0, 0, rot);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 최종 보정
        for (int i = 0; i < count; i++)
        {
            var slot = _activeSlots[i];

            slot.Rect.anchoredPosition = _targetPositions[i];
            slot.Rect.localRotation =
                Quaternion.Euler(0, 0, _targetRotations[i]);

            if (slot.TryGetComponent<CardHover>(out var hover))
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
        }
    }

    #endregion

    #region Helpers

    private void BuildActiveSlots()
    {
        _activeSlots.Clear();

        for (int i = 0; i < slots.Count; ++i)
        {
            var slot = slots[i];
            if (IsValid(slot))
                _activeSlots.Add(slot);
        }
    }

    private bool IsValid(CardSlot slot)
    {
        return slot != null && slot.isActiveAndEnabled;
    }
    #endregion
}