using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

using Common;
using Tables.Records;
using UI.Slots;

namespace UI.Main
{
    public class NormalSceneMode : SceneMode
    {
        public NormalSceneMode(SceneModeContext context) : base(context)
        {
            
        }
        
        protected override async UniTask OnPlayAsync()
        {
            var view = _context?.View;
            if (view == null)
                return;
            
            var dialogues = _sceneRecord.DialogueRecords;
            if (dialogues == null)
                return;
        
            view.DisableScrollRect();
            await view.ScrollToAsync(view.ViewportHalfHeight);
        
            var locale = LocalizationSettings.SelectedLocale;
                                
            for (int i = 0; i < dialogues.Length; ++i)
            {
                var dialogueRecord = dialogues[i];
                if(dialogueRecord == null) 
                    continue;
        
                IDialogueSlot dialogueSlot = null;
                // int eventId = 0;
                
                switch (dialogueRecord)
                {
                    case CharacterSpeechRecord characterSpeechRecord:
                    {
                        dialogueSlot = CreateCharacterSpeechSlot(characterSpeechRecord, locale);
                        break;
                    }
        
                    case NarrationRecord narrationRecord:
                    { 
                        dialogueSlot = CreateNarrationSlot(narrationRecord.LocalKey, narrationRecord.TypingSpeed, locale);
                        break;
                    }
        
                    case EventRecord eventRecord:
                    {
                        dialogueSlot = CreateNarrationSlot(eventRecord.LocalKey, eventRecord.TypingSpeed, locale);
                        DispatchEvent(eventRecord, dialogueSlot);
                        break;
                    }
                }
        
                if(dialogueSlot == null)
                    continue;
        
                await _dialogueCompletionSource.Task;

                var dialogueActions = dialogueRecord.DialogueActions;
                if(dialogueActions != null && dialogueActions.Count > 0)
                    await ExecuteDialoguePostActionAsync(dialogueSlot.TMP, dialogueRecord.DialogueActions);
                
                await ShowAnswerAsync(dialogueRecord.AnswerIds);
                // await TriggerEventAsync(eventId, dialogueRecord.SlotId, dialogueSlot);
            }
            
            // await view.ScrollToAsync(0);
            // // await UniTask.Delay(TimeSpan.FromSeconds(5f));
            //
            // view.EnableScrollRect();
        }
        
        // private IDialogueSlot CreateCharacterSpeechSlot(CharacterSpeechRecord record, Locale locale)
        // {
        //     if (record == null)
        //         return null;
        //     
        //     _dialogueCompletionSource = new();
        //                 
        //     // var characterNameLocalText = LocalizationSettings.StringDatabase.GetLocalizedString("Character", "messenger", locale);
        //     var localText = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", record.LocalKey, locale);
        //     var param = new CharacterSpeechSlot.Param(this, localText, record.TypingSpeed)
        //         .WithCharacterName(string.Empty);
        //                 
        //     return _context?.View?.CreateCharacterSpeechSlot(_context?.UIFactory, param);
        // }
        //
        // private IDialogueSlot CreateNarrationSlot(string localKey, float typingSpeed, Locale locale)
        // {
        //     _dialogueCompletionSource = new();
        //     var localText = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", localKey, locale);
        //     var param = new NarrationSlot.Param(this, localText, typingSpeed);
        //     
        //     return _view.CreateNarrationSlot(_uiFactory, param);
        // }
    }
}
