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

                if (!ShouldPlayRecord(dialogueRecord))
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
                        dialogueSlot = CreateNarrationSlot(narrationRecord.LocalKey, narrationRecord.TypingSpeed, narrationRecord.TextRevealMode, locale);
                        break;
                    }
        
                    case EventRecord eventRecord:
                    {
                        // LocalKey 가 비어있는 EventRecord 는 화면에 출력하지 않고
                        // 사이드이펙트(CardGrant 등) 만 실행한다. 인벤토리/상태 변경은
                        // 즉시 처리되고, 다음 다이얼로그로 바로 진행한다.
                        if (string.IsNullOrEmpty(eventRecord.LocalKey))
                        {
                            DispatchEvent(eventRecord, null);
                            continue;
                        }

                        dialogueSlot = CreateNarrationSlot(eventRecord.LocalKey, eventRecord.TypingSpeed, eventRecord.TextRevealMode, locale);
                        DispatchEvent(eventRecord, dialogueSlot);
                        break;
                    }
                }

                if(dialogueSlot == null)
                    continue;

                await _dialogueCompletionSource.Task;

                // 타이핑이 끝난 후에 IsMonster TMP 캡처 + 호흡 시작.
                // 타이핑 *전* 에 호출하면 vertex/characterCount 가 0 이라 DoPulse 가 NRE.
                // BattleSceneMode 의 호흡/불꽃 효과 대상이 된다.
                if (dialogueRecord is EventRecord ev && ev.IsMonster)
                {
                    var payload = _context.GetPayload<BattleModePayload>(SceneModeType.Battle) ??
                                  new BattleModePayload();
                    
                    payload.MonsterTMP = dialogueSlot.TMP;
                    _context.SetPayload(SceneModeType.Battle, payload);
                }

                var dialogueActions = dialogueRecord.DialogueActions;
                if(dialogueActions != null && dialogueActions.Count > 0)
                    await ExecuteDialoguePostActionAsync(dialogueSlot.TMP, dialogueRecord.DialogueActions);
                
                await ShowAnswerAsync(dialogueRecord.AnswerIds);
            }
        }
    }
}
