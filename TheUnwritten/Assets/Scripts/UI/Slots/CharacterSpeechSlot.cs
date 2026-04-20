using UnityEngine.Localization.Settings;
using UnityEngine;

using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

using Common;
using UI.Components;

namespace UI.Slots
{
    public class CharacterSpeechSlot : DialogueSlot<CharacterSpeechSlot.Param>
    {
        public new  class Param : DialogueSlot<CharacterSpeechSlot.Param>.Param
        {
            public IListener Listener { get; } = null;
            public string CharacterNameText { get; private set; } = string.Empty;

            public Param(IListener listener, string text, float typingSpeed) : base(text, typingSpeed)
            {
                Listener = listener;
            }

            public Param WithCharacterName(string text)
            {
                CharacterNameText = text;
                return this;
            }
        }

        public interface IListener
        {
            void End();
        }

        [SerializeField] private Typer characterNameTyper = null;

        public override void Initialize(Param param)
        {
            base.Initialize(param);
            
            var typerParam = new Typer.Param(OnCompleteTyping)
                .WithEndDelaySeconds(1f);
            
            characterNameTyper?.Initialize(typerParam);
        }

        public override void Activate()
        {
            base.Activate();

            var characterNameText = $"  {_param?.CharacterNameText}";
            characterNameTyper?.TypeTextAsync(characterNameText);
        }

        protected override void OnCompleteDialogue()
        {
            base.OnCompleteDialogue();

            RestoreScaleAsync(() => { _param?.Listener?.End(); }).Forget();
        }

        private void OnCompleteTyping()
        {
            typer?.TypeTextAsync(_param?.Text);
        }
    }
}

