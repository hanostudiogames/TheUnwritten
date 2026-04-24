using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

using Common;
using Data;
using Tables;
using Tables.Containers;
using Tables.Records;
using UI.Cards;
using UI.Slots;

namespace UI.Main
{
    public interface IMainPresenter
    {
        
    }
    
    public class MainPresenter : Presenter<MainView, MainModel>, IMainPresenter,
        // CharacterSpeechSlot.IListener,
        // NarrationSlot.IListener,
        // AnswerSlot.IListener,
        ISceneListener,
        IBattleCardInput
    {
        private readonly UIFactory _uiFactory = null;
        private readonly IGameManager _gameManager = null;
        private readonly CardController _cardController = null;
        private readonly ICardSelectionHandler _cardSelectionHandler = null;
        // private readonly IBattleController _battleController = null;
        
        // private UniTaskCompletionSource _dialogueCompletionSource = null;
        // private UniTaskCompletionSource _answerCompletionSource = null;
        private DialoguePostAction _dialoguePostAction = null;

        private readonly SceneModeContext _sceneModeContext = null;
        private Dictionary<SceneModeType, SceneMode> _sceneModes = null;
        private SceneMode _sceneMode = null;
        
        public MainPresenter(MainView view, MainModel model, 
            IGameManager gameManager, 
            UIManager uiManager, 
            SlotInteractionHandler slotInteractionHandler) : base (view, model)
        {
            _view = view;

            _gameManager = gameManager;
            _uiFactory = new UIFactory(uiManager);
            _cardController = new(view.CardFanSpread);
            _cardSelectionHandler = slotInteractionHandler;
            // _battleController = battleController;
            _sceneModeContext = new SceneModeContext(_view, _cardSelectionHandler, _cardController, _uiFactory, this);

            _sceneModes = new();
            
            _cardController?.SetListener(slotInteractionHandler);
            uiManager?.RegisterDimensionHandler<MainPresenter>(OnDimensionChanged);
        }

        public override void Activate()
        {
            base.Activate();
            
            _view.FadeLibraryAsync(0, 0).Forget();
        }

        private async UniTask PlayAsync(int act, int scene)
        {
            if (_view == null)
                return;
            
            var sceneRecord = ActTableContainer.Instance?.GetSceneRecord(act, scene);
            if (sceneRecord == null)
                return;

            var sceneMode = CreateSceneMode(sceneRecord.SceneModeType);
            if (sceneMode == null)
                return;

            if (_dialoguePostAction == null)
                _dialoguePostAction = new();
            
            _sceneMode = sceneMode;

            await sceneMode.PlayAsync(act, scene, sceneRecord, _dialoguePostAction);
            
            // var dialogues = sceneRecord.DialogueRecords;
            // if (dialogues == null)
            //     return;
            //
      
            //
            // _view.DisableScrollRect();
            // await _view.ScrollToAsync(_view.ViewportHalfHeight);
            //
            // var locale = LocalizationSettings.SelectedLocale;
            //                     
            // for (int i = 0; i < dialogues.Length; ++i)
            // {
            //     var dialogueRecord = dialogues[i];
            //     if(dialogueRecord == null) 
            //         continue;
            //
            //     IDialogueSlot dialogueSlot = null;
            //     int eventId = 0;
            //     
            //     switch (dialogueRecord)
            //     {
            //         case CharacterSpeechRecord characterSpeechRecord:
            //         {
            //             dialogueSlot = CreateCharacterSpeechSlot(characterSpeechRecord, locale);
            //             break;
            //         }
            //
            //         case NarrationRecord narrationRecord:
            //         { 
            //             dialogueSlot = CreateNarrationSlot(narrationRecord.LocalKey, narrationRecord.TypingSpeed, locale);
            //             break;
            //         }
            //
            //         case EventRecord eventRecord:
            //         {
            //             dialogueSlot = CreateNarrationSlot(eventRecord.LocalKey, eventRecord.TypingSpeed, locale);
            //             eventId = eventRecord.EventId;
            //             
            //             if (eventRecord.IsMonster)
            //                 _battleController?.SetTargetTMP(dialogueSlot?.TMP);
            //             
            //             break;
            //         }
            //     }
            //
            //     if(dialogueSlot == null)
            //         continue;
            //
            //     await _dialogueCompletionSource.Task;
            //
            //     await ExecuteDialoguePostActionAsync(dialogueSlot.TMP, act, scene, dialogueRecord.DialogueActions);
            //     await ActiveAnswerAsync(dialogueRecord.AnswerIds);
            //     // await TriggerEventAsync(eventId, dialogueRecord.SlotId, dialogueSlot);
            // }
            //
            // await _view.ScrollToAsync(0);
            // // await UniTask.Delay(TimeSpan.FromSeconds(5f));
            //
            // _view.EnableScrollRect();
        }

        private SceneMode CreateSceneMode(SceneModeType sceneModeType)
        {
            if (_sceneModes == null)
                return null;

            SceneMode sceneMode = null;
            if (_sceneModes.TryGetValue(sceneModeType, out sceneMode))
                return sceneMode;

            switch (sceneModeType)
            {
                case SceneModeType.Normal:
                {
                    sceneMode = new NormalSceneMode(_sceneModeContext);
                    break;
                }
                
                case SceneModeType.Battle:
                {
                    sceneMode = new BattleSceneMode(_sceneModeContext);
                    break;
                }
            }

            if (sceneMode == null)
                return null;
   
            _sceneModes[sceneModeType] = sceneMode;
            
            return sceneMode;
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
        //     return _view.CreateCharacterSpeechSlot(_uiFactory, param);
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

        // private async UniTask ExecuteDialoguePostActionAsync(TextMeshProUGUI tmp, int act, int scene, 
        //     List<DialogueAction> dialogueActions)
        // {
        //     if (_dialoguePostAction == null)
        //         return;
        //     
        //     var tmps = new List<TextMeshProUGUI>();
        //     tmps.Add(tmp);
        //     tmps.AddRange(_view.TMPsInDialogueSlots());
        //     
        //     var param = new DialoguePostAction.Param(tmps, act, scene)
        //         .WithDialogueActions(dialogueActions);
        //
        //     await _dialoguePostAction.SetParam(param)
        //         .ExecuteAsync();
        // }
        //
        // private async UniTask ActiveAnswerAsync(int[] answerIds)
        // {
        //     if (answerIds == null || answerIds.Length <= 0)
        //         return;
        //
        //     _answerCompletionSource = new UniTaskCompletionSource();
        //
        //     await _view.ShowAnswersAsync(answerIds);
        //             
        //     await _view.ScrollToAsync(0);
        //     _view.EnableScrollRect();
        //             
        //     await _answerCompletionSource.Task;   
        //             
        //     _view.DisableScrollRect();
        //     await _view.ScrollToAsync(_view.ViewportHalfHeight);
        // }

        // private async UniTask TriggerEventAsync(int eventId, int slotId, IDialogueSlot dialogueSlot)
        // {
        //     if (eventId <= 0)
        //         return;
        //
        //     if (eventId == 1)
        //     {
        //         if (_battleController != null)
        //         {
        //             await _view.ScrollToAsync(100f);
        //             
        //             _battleController.SetDialogueTMP(dialogueSlot?.TMP);
        //             await _battleController.PlayBattleAsync(eventId, slotId, dialogueSlot);
        //         }
        //     }
        // }

        public UniTask<CardRecord> RequestCardAsync(int slotId, IDialogueSlot activeSlot)
        {
            return ActiveCardAsync(slotId, activeSlot);
        }

        private async UniTask<CardRecord> ActiveCardAsync(int slotId, IDialogueSlot activeSlot)
        {
            var slotRecord = SlotTableContainer.Instance?.GetSlotRecord(slotId);
            if (slotRecord == null)
                return null;

            await _cardController.SetCardsAsync(slotRecord.AllowedCardIds);
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            _cardController.ShowCards();

            if (_cardSelectionHandler != null)
            {
                _cardSelectionHandler.BeginSelection(activeSlot);
                var selectedCard = await _cardSelectionHandler.AwaitCompletionAsync();
                _cardController.HideCards();
                return selectedCard;
            }

            _cardController.HideCards();
            return null;
        }
        
        // #region CharacterSpeechSlot.Listener
        // void CharacterSpeechSlot.IListener.End()
        // {
        //     _dialogueCompletionSource?.TrySetResult();
        // }
        // #endregion
        //
        // #region NarrationSlot.IListener
        //
        // void NarrationSlot.IListener.End()
        // {
        //     _dialogueCompletionSource?.TrySetResult();
        // }
        // #endregion
        
        // #region AnswerSlot.IListener
        //
        // void AnswerSlot.IListener.OnSelectedAnswer(int id)
        // {
        //     _view?.HideAnswersAsync();
        //     _answerCompletionSource?.TrySetResult();
        // }
        // #endregion
        
        #region ISceneListener

        async UniTask ISceneListener.OnStartSceneAsync(int act, int scene)
        {
            if (act == 1 && scene < 3)
                await _view.FadeLibraryAsync(0.4f, 3f);
            
            await PlayAsync(act, scene);
        }

        async UniTask ISceneListener.OnEndSceneAsync(int act, int scene)
        {
            if (act == 1 && scene == 1)
                await _view.FadeLibraryAsync(0, 3f);
        }
        #endregion

        
        
        protected override void OnDimensionChanged(bool isPortrait)
        {
            base.OnDimensionChanged(isPortrait);

            if (_view == null)
                return;

            float scrollPositionY = 0;
            if (_sceneMode != null)
                scrollPositionY = _sceneMode.ScrollPositionY;
            
            _view?.OnDimensionChanged(isPortrait, scrollPositionY);
        }
    }
}
