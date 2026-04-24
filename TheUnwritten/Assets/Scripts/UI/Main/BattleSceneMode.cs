using Cysharp.Threading.Tasks;
using TMPro;
using UI.Components;
using UI.Slots;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace UI.Main
{
    public class BattleSceneMode : SceneMode
    {
        private int _slotId = 0;
        private TextMeshProUGUI _monsterTMP = null;
        private IDialogueSlot _dialogueSlot = null;

        public BattleSceneMode(SceneModeContext sceneModeContext) : base(sceneModeContext)
        {
            // _cardInput = sceneModeContext?.BattleCardInput;
        }
        
       
        
        protected override async UniTask OnPlayAsync()
        {
            var view = _context?.View;
            if (view == null)
                return;

            var cardInput = _context?.BattleCardInput;
            
            var dialogueRecords = _sceneRecord.DialogueRecords;
            if (dialogueRecords == null)
                return;
            
            await view.ScrollToAsync(100f);

            // var slotId = dialogueRecord.SlotId > 0 ? dialogueRecord.SlotId : _slotId;
            // if (slotId > 0 && _cardInput != null)
            if(cardInput != null)
                await cardInput.RequestCardAsync(1, _dialogueSlot);
            
            var payload = _context?.GetPayload<BattleModePayload>(Common.SceneModeType.Battle);
            if (payload == null)
                return;

            // if (payload.Act != _act || payload.Scene != _scene)
            // {
            //     _context?.ClearPayload(Common.SceneModeType.Battle);
            //     return;
            // }

            _dialogueSlot = payload.DialogueSlot;
            _slotId = payload.SlotId;
            _monsterTMP = payload.MonsterTMP;
            _context?.ClearPayload(Common.SceneModeType.Battle);
            
            
            var locale = LocalizationSettings.SelectedLocale;

            for (int i = 0; i < dialogueRecords.Length; ++i)
            {
                var dialogueRecord = dialogueRecords[i];
                if (dialogueRecord == null)
                    continue;

                var localText = LocalizationSettings.StringDatabase
                    .GetLocalizedString("Dialogue", dialogueRecord.LocalKey, locale);

                var dialogueTyper = _dialogueSlot?.Typer;
                if (dialogueTyper != null)
                {
                    var typerParam = new Typer.Param(null)
                        .WithTypingSpeed(dialogueRecord.TypingSpeed)
                        .WithEndDelaySeconds(dialogueRecord.EndDelaySeconds);

                    dialogueTyper.Initialize(typerParam);
                    await dialogueTyper.TypeTextAsync(localText);
                }
                
              
            }

            await UniTask.CompletedTask;
        }
    }
}
