using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

using TMPro;

using Common;
using UI.Cards;
using UI.Slots;
using Tables.Records;

namespace UI.Main
{
    public abstract class SceneMode : NarrationSlot.IListener, CharacterSpeechSlot.IListener, AnswerSlot.IListener
    {
        protected readonly SceneModeContext _context = null;
        
        protected int _act = 0;
        protected int _scene = 0;
        protected  SceneRecord _sceneRecord = null;
      
        protected  UniTaskCompletionSource _dialogueCompletionSource = null;
        
        private DialoguePostAction _dialoguePostAction = null;
        private UniTaskCompletionSource _answerCompletionSource = null;
        // private DialoguePostAction _dialoguePostAction = null;
        
        protected SceneMode(SceneModeContext context)
        {
            _context = context;
        }

        public async UniTask PlayAsync(int act, int scene, SceneRecord sceneRecord, DialoguePostAction dialoguePostAction)
        {
            _act = act;
            _scene = scene;
            _sceneRecord = sceneRecord;
            _dialoguePostAction = dialoguePostAction;

            _context?.View?.InitializeAnswerSlots(this);

            await OnPlayAsync();
        }

        protected abstract UniTask OnPlayAsync();
        
        protected IDialogueSlot CreateCharacterSpeechSlot(CharacterSpeechRecord record, Locale locale)
        {
            if (record == null)
                return null;
            
            _dialogueCompletionSource = new();
                        
            // var characterNameLocalText = LocalizationSettings.StringDatabase.GetLocalizedString("Character", "messenger", locale);
            var localText = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", record.LocalKey, locale);
            var param = new CharacterSpeechSlot.Param(this, localText, record.TypingSpeed)
                .WithCharacterName(string.Empty);
                        
            return _context?.View?.CreateCharacterSpeechSlot(_context?.UIFactory, param);
        }

        protected IDialogueSlot CreateNarrationSlot(string localKey, float typingSpeed, Locale locale)
        {
            _dialogueCompletionSource = new();
            var localText = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", localKey, locale);
            var param = new NarrationSlot.Param(this, localText, typingSpeed);
            
            return _context?.View.CreateNarrationSlot(_context?.UIFactory, param);
        }
        
        protected async UniTask ExecuteDialoguePostActionAsync(TextMeshProUGUI tmp, List<DialogueAction> dialogueActions)
        {
            var view = _context?.View;
            if (view == null)
                return;
            
            if (_dialoguePostAction == null)
                return;
            
            var tmps = new List<TextMeshProUGUI>();
            tmps.Add(tmp);
            tmps.AddRange(view.TMPsInDialogueSlots());
            
            var param = new DialoguePostAction.Param(tmps, _act, _scene)
                .WithDialogueActions(dialogueActions);

            await _dialoguePostAction.SetParam(param)
                .ExecuteAsync();
        }
        
        protected async UniTask ActiveAnswerAsync(int[] answerIds)
        {
            var view = _context?.View;
            if (view == null)
                return;
            
            if (answerIds == null || answerIds.Length <= 0)
                return;

            _answerCompletionSource = new UniTaskCompletionSource();

            await view.ShowAnswersAsync(answerIds);
                    
            await view.ScrollToAsync(0);
            view.EnableScrollRect();
                    
            await _answerCompletionSource.Task;   
                    
            view.DisableScrollRect();
            await view.ScrollToAsync(view.ViewportHalfHeight);
        }

        public virtual void End()
        {
            _dialogueCompletionSource?.TrySetResult();
        }
        
        #region AnswerSlot.IListener

        void AnswerSlot.IListener.OnSelectedAnswer(int id)
        {
            _context?.View?.HideAnswersAsync();
            _answerCompletionSource?.TrySetResult();
        }
        #endregion
        
        public float ScrollPositionY
        {
            get
            {
                float scrollPositionY = 0;
                if (_context == null)
                    return scrollPositionY;
                
                var view = _context?.View;
                var cardController = _context?.CardController;
                
                var answerStatus = UniTaskStatus.Canceled;
                if (_answerCompletionSource?.Task != null)
                    answerStatus = _answerCompletionSource.Task.Status;
                
                if (view != null && answerStatus != UniTaskStatus.Pending)
                    scrollPositionY = _context.View.ViewportHalfHeight;
            
                if(cardController != null &&
                   cardController.IsActiveCards)
                    scrollPositionY = 100f;
                
                return scrollPositionY;
            }
        }
    }
}
