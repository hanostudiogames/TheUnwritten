using UnityEngine.Localization.Settings;
using UnityEngine;

using Cysharp.Threading.Tasks;
using DG.Tweening;

using Common;

namespace UI.Slots
{
    public class NarrationSlot : DialogueSlot<NarrationSlot.Param>
    {
        public new class Param : DialogueSlot<NarrationSlot.Param>.Param
        {
            public IListener Listener { get; } = null;
            
            public Param(IListener listener, string text, TextRevealMode revealMode, float typingSpeed) : base(text, typingSpeed, revealMode)
            {
                Listener = listener;
            }
        }

        public interface IListener
        {
            void End();
        }
        
        // [SerializeField] private Typer typer = null; 

        public override void Initialize(Param param)
        {
            base.Initialize(param);
        }

        public override void Activate()
        {
            base.Activate();
            
            // typer?.SetTypingSpeed(0.1f);
            typer?.TypeTextAsync(_param?.Text);
        }
        
        protected override void OnCompleteDialogue()
        {
            base.OnCompleteDialogue();

            RestoreScaleAsync(() => { _param?.Listener?.End(); }).Forget();
        }
    }
}