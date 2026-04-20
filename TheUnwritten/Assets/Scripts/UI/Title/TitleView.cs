using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

using UI.Components;
using UI.Effects;
using UI.Extensions;
using UI.Utilities;

namespace UI.Title
{
    public class TitleView : Common.View<TitlePresenter>
    {
        private readonly string Title = "THE UNWRITTEN";
        
        [SerializeField] private RectTransform bgRectTr = null;
        [SerializeField] private Typer titleTextTyper = null;
        [SerializeField] private Button startBtn = null;
        
        public override void Initialize(TitlePresenter presenter)
        {
            base.Initialize(presenter);
            
            startBtn?.onClick?.RemoveAllListeners();
            startBtn?.onClick?.AddListener(() => { _presenter?.OpenMainView(); });
            
            ActiveStartBtn(false);
            
            titleTextTyper?.Initialize(new Typer.Param(OnCompleteTitleText)
                .WithStartDelaySeconds(1f)
                .WithEndDelaySeconds(1f)
                .WithTypingSpeed(0.3f));
        }

        public override void Activate()
        {
            base.Activate();

            var title = Title.Replace(Title, "!HE ABWRI?TEN");
            var text = UI.Utilities.TextSpriteUtility.ConvertToSpriteText(title);

            titleTextTyper?.TypeTextAsync(text);
        }

        private void ActiveStartBtn(bool isActive)
        {
            if (startBtn != null)
                startBtn.transform.parent.gameObject.SetActive(isActive);

        }

        private void OnCompleteTitleText()
        {
            PlaySpriteSequenceAsync().Forget();
        }

        private async UniTask PlaySpriteSequenceAsync()
        {
            var tmp = titleTextTyper?.TMP;
            if (tmp == null)
                return;

            // tmp.DORandomShake(10f, 5f, 0.2f);
            
            for (int i = 0; i < Title.Length; i++)
            {
                if (Title[i] == ' ')
                    continue;
                
                await tmp.AnimateCharacterAsync(i, shakeStrength: 15f, frequency: 60f, duration: 0.5f,
                    onComplete: () =>
                    {
                        tmp.ReplaceSpriteAt(0 , $"{Title[i]}");
                    });
            }

            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            ActiveStartBtn(true);
            
            // tmp.DOShear(0.01f, 5f);
            // tmp.DOSequentialFold(0.5f, 2f, 0.5f);
            // tmp.DORandomCollapse( 1f, 0.1f);
            // tmp.DOFold(0.2f, 1f);
            // tmp.DORandomShake(10f, 3f, 0.1f);
            // tmp.DORandomCollapse(1f, 0.1f)?.SetDelay(2.5f);

        }
        
        public void OnDimensionChanged(bool isPortrait)
        {
            if(bgRectTr)
                bgRectTr.localRotation = isPortrait ? Quaternion.identity : Quaternion.Euler(0, 0, 90f);

            if (titleTextTyper != null &&
                titleTextTyper.TMP != null)
                titleTextTyper.TMP.fontSize = isPortrait ? 70f : 90f;
        }
    }
}

