using Cysharp.Threading.Tasks;
using UnityEngine;

using DG.Tweening;

namespace Common
{
    public static class UIExtensions
    {
        public static UniTask DoOffsetMoveX(
            this RectTransform rect,
            float deltaX,
            float duration)
        {
            return DOTween.To(
                () => rect.offsetMin.x,
                x =>
                {
                    var diff = x - rect.offsetMin.x;
                    rect.offsetMin += new Vector2(diff, 0);
                    rect.offsetMax += new Vector2(diff, 0);
                },
                deltaX,
                duration)
                .SetEase(Ease.InOutCubic)
                .ToUniTask();
        }
    }
}

