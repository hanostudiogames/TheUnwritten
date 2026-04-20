using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class CardHover : MonoBehaviour
{
    [SerializeField] private RectTransform rectTr = null;

    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float hoverHeight = 80f;
    [SerializeField] private float duration = 0.15f;

    public bool IsSelectable { get; set; }
    public bool IsHovering => _isHovering;

    private CancellationTokenSource _cts;
    private bool _isHovering;

    private Vector2 _originPos;
    private Quaternion _originRot;
    private bool _hasOrigin;

    private void Awake()
    {
        // 🔥 안전장치 (혹시 안 넣었을 때)
        if (rectTr == null)
            rectTr = transform as RectTransform;
    }

    public void Enter()
    {
        if (!IsSelectable || !_hasOrigin) return;

        _isHovering = true;
        Play(true).Forget();
    }

    public void Exit()
    {
        if (!_isHovering || !_hasOrigin) return;

        _isHovering = false;
        Play(false).Forget();
    }

    public void ForceExit()
    {
        _isHovering = false;
        SnapToOrigin();
    }

    public void SetOrigin(Vector2 pos, Quaternion rot)
    {
        _originPos = pos;
        _originRot = rot;
        _hasOrigin = true;

        if (!_isHovering)
        {
            SnapToOrigin();
        }
    }

    public void SnapToOrigin()
    {
        rectTr.anchoredPosition = _originPos;
        rectTr.localRotation = _originRot;
        rectTr.localScale = Vector3.one;
    }

    private async UniTask Play(bool enter)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Vector2 startPos = rectTr.anchoredPosition;
        Vector3 startScale = rectTr.localScale;
        Quaternion startRot = rectTr.localRotation;

        Vector2 targetPos = enter
            ? _originPos + Vector2.up * hoverHeight
            : _originPos;

        Vector3 targetScale = enter ? Vector3.one * hoverScale : Vector3.one;
        Quaternion targetRot = enter ? Quaternion.identity : _originRot;

        if (enter)
        {
            rectTr.SetAsLastSibling();
        }

        float time = 0f;

        while (time < duration)
        {
            if (token.IsCancellationRequested) return;

            time += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - (time / duration), 2f);

            rectTr.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rectTr.localScale = Vector3.Lerp(startScale, targetScale, t);
            rectTr.localRotation = Quaternion.Lerp(startRot, targetRot, t);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        rectTr.anchoredPosition = targetPos;
        rectTr.localScale = targetScale;
        rectTr.localRotation = targetRot;
    }
}