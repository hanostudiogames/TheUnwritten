using UnityEngine;
using System;

using Cysharp.Threading.Tasks;
using DG.Tweening;

using Common;
using TMPro;
using UI.Components;

namespace UI.Slots
{
    public interface IDialogueSlot
    {
        TextMeshProUGUI TMP { get; }
        Typer Typer { get; }
        
        void Initialize();
    }

    public abstract class DialogueSlot<TParam> : Slot<TParam>, IDialogueSlot
        where TParam : ElementParam
    {
        public class Param : ElementParam
        {
            public string Text { get; } = string.Empty;
            public float TypingSpeed { get; private set; } = 0;
            public float Height { get; private set; } = 0;

            protected Param(string text, float typingSpeed)
            {
                Text = text;
                TypingSpeed = typingSpeed;
            }

            public Param WithHeight(float height)
            {
                Height = height;

                return this;
            }
        }
        
        [SerializeField] protected Typer typer = null;
        
        protected RectTransform _rectTr = null;

        public TextMeshProUGUI TMP => typer?.TMP;
        public Typer Typer => typer;
        
        public override void Initialize(TParam param)
        {
            base.Initialize(param);
            
            if(!_rectTr)
                _rectTr = GetComponent<RectTransform>();

            InitializeTyper();
        }

        public override void Activate()
        {
            base.Activate();

            if (_param is Param param)
                SetFocused(param.Height);
        }

        private void InitializeTyper()
        {
            var typerParam = new Typer.Param(OnCompleteDialogue)
                .WithEndDelaySeconds(1f);

            if (_param is Param param)
                typerParam.WithTypingSpeed(param.TypingSpeed);
            
            typer?.Initialize(typerParam);
        }

        private void SetFocused(float height)
        {
            if (!_rectTr)
                return;
            
            _rectTr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            // _rectTr.localScale = Vector3.one * 1.1f;
        }
        
        protected UniTask RestoreScaleAsync(Action completeAction)
        {
            if (!_rectTr)
                return UniTask.CompletedTask;
            
            completeAction?.Invoke();
            // await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            // await _rectTr.DOScale(Vector3.one, 0.5f)
            //     .OnComplete(() => completeAction?.Invoke());
            
            return UniTask.CompletedTask;
        }

        protected virtual void OnCompleteDialogue()
        {
            
        }
    }
}

