using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

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

    private Vector2[] _targetPositions;
    private float[] _targetRotations;

    private CancellationTokenSource _cts;

    private async void Start()
    {
        SetSelectable(true);
        
        foreach (var slot in slots)
        {
            if (slot == null) continue;

            var rt = slot.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -800f);
        }
        
        await UniTask.Yield(); // 슬롯 준비 기다림

        // 👉 초기 카드 리스트 (외부에서 받거나, slots 복사)
        var initialCards = new List<CardSlot>(slots);
        await InitializeCards(initialCards);
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            SpreadImmediate();
    }

    void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
    
    public async UniTask InitializeCards(List<CardSlot> initialCards)
    {
        slots.Clear();

        foreach (var slot in initialCards)
        {
            await AddCardAnimated(slot);

            // 👉 한 장씩 템포 주기 (연출)
            await UniTask.Delay(10);
        }
    }

    public void SetSelectable(bool value)
    {
        foreach (var slot in slots)
        {
            slot?.SetSelectable(value);
        }
    }

    // 🔥 카드 추가 (핵심 함수)
    public async UniTask AddCardAnimated(CardSlot newSlot)
    {
        slots.Add(newSlot);

        RectTransform newRt = newSlot.GetComponent<RectTransform>();

        // 아래에서 등장
        newRt.anchoredPosition = new Vector2(0, -400f);
        newRt.localScale = Vector3.one * 0.8f;

        await ShiftExistingCards();   // 기존 카드 밀기
        await SpreadAnimated();       // 전체 정렬
    }

    public void SpreadImmediate()
    {
        int count = slots.Count;
        if (count == 0) return;

        EnsureCache(count);
        CalculateTargets(count);

        for (int i = 0; i < count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();

            Vector2 pos = _targetPositions[i];
            Quaternion rot = Quaternion.Euler(0, 0, _targetRotations[i]);

            rt.SetSiblingIndex(i);

            if (slot.TryGetComponent<CardHover>(out var hover))
            {
                hover.ForceExit();
                hover.SetOrigin(pos, rot);
            }
        }
    }

    // 🔥 기존 카드 밀기
    private async UniTask ShiftExistingCards()
    {
        int count = slots.Count;
        if (count <= 1) return;

        EnsureCache(count);
        CalculateTargets(count);

        float shiftDuration = 0.15f;

        Vector2[] startPos = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();
            startPos[i] = rt.anchoredPosition;

            if (slot.TryGetComponent<CardHover>(out var hover))
            {
                hover.ForceExit();
            }
        }

        float time = 0f;

        while (time < shiftDuration)
        {
            time += Time.deltaTime;
            float t = time / shiftDuration;

            for (int i = 0; i < count - 1; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.Lerp(startPos[i], _targetPositions[i], t);
            }

            await UniTask.Yield();
        }
    }

    public async UniTask SpreadAnimated()
    {
        int count = slots.Count;
        if (count == 0) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        EnsureCache(count);
        CalculateTargets(count);

        Vector2[] startPos = new Vector2[count];
        float[] startRot = new float[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = slots[i].GetComponent<RectTransform>();
            startPos[i] = rt.anchoredPosition;
            startRot[i] = rt.localEulerAngles.z;
        }

        float time = 0f;

        while (time < duration)
        {
            if (token.IsCancellationRequested) return;

            time += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - (time / duration), 2f);

            for (int i = 0; i < count; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                RectTransform rt = slot.GetComponent<RectTransform>();

                rt.anchoredPosition = Vector2.Lerp(startPos[i], _targetPositions[i], t);

                float rot = Mathf.LerpAngle(startRot[i], _targetRotations[i], t);
                rt.localRotation = Quaternion.Euler(0, 0, rot);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 🔥 최종 정렬 (ForceExit 없음)
        for (int i = 0; i < count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();

            rt.anchoredPosition = _targetPositions[i];
            rt.localRotation = Quaternion.Euler(0, 0, _targetRotations[i]);

            if (slot.TryGetComponent<CardHover>(out var hover))
            {
                hover.SetOrigin(_targetPositions[i], Quaternion.Euler(0, 0, _targetRotations[i]));
            }
        }
    }

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
}