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
        CharacterSpeechSlot.IListener,
        NarrationSlot.IListener,
        AnswerSlot.IListener,
        ISceneListener
    {
        private readonly UIFactory _uiFactory = null;
        private readonly IGameManager _gameManager = null;
        private readonly CardController _cardController = null;
        private readonly ICardSelectionHandler _cardSelectionHandler = null;
        private readonly IBattleController _battleController = null;
        
        private UniTaskCompletionSource _dialogueCompletionSource = null;
        private UniTaskCompletionSource _answerCompletionSource = null;
        private DialoguePostAction _dialoguePostAction = null;
        
        public MainPresenter(MainView view, MainModel model, IGameManager gameManager, UIManager uiManager, 
            SlotInteractionHandler slotInteractionHandler,
            IBattleController battleController) : base (view, model)
        {
            _view = view;

            _gameManager = gameManager;
            _uiFactory = new UIFactory(uiManager);
            _cardController = new(view.CardFanSpread);
            _cardSelectionHandler = slotInteractionHandler;
            _battleController = battleController;
            
            _cardController?.SetListener(slotInteractionHandler);
            uiManager?.RegisterDimensionHandler<MainPresenter>(OnDimensionChanged);
            
            view.InitializeAnswerSlots(this);
        }

        public override void Activate()
        {
            base.Activate();
            
            _view.FadeLibraryAsync(0, 0).Forget();
        }

        private async UniTask PlayDialogueAsync(int act, int scene)
        {
            if (_view == null)
                return;

            var sceneRecord = ActTableContainer.Instance?.GetSceneRecord(act, scene);
            if (sceneRecord == null)
                return;

            var dialogues = sceneRecord.DialogueRecords;
            if (dialogues == null)
                return;

            if (_dialoguePostAction == null)
                _dialoguePostAction = new();

            _view.DisableScrollRect();
            await _view.ScrollToAsync(_view.ViewportHalfHeight);

            var locale = LocalizationSettings.SelectedLocale;
            
            for (int i = 0; i < dialogues.Length; ++i)
            {
                var dialogueRecord = dialogues[i];
                if(dialogueRecord == null) 
                    continue;

                IDialogueSlot dialogueSlot = null;
                switch (dialogueRecord)
                {
                    case CharacterSpeechRecord characterSpeech:
                    {
                        dialogueSlot = CreateCharacterSpeechSlot(characterSpeech, locale);
                        break;
                    }

                    case NarrationRecord narration:
                    { 
                        dialogueSlot = CreateNarrationSlot(narration, locale);
                        break;
                    }
                }

                if(dialogueSlot == null)
                    continue;

                if (dialogueRecord.IsMonster)
                    _battleController?.SetTargetTMP(dialogueSlot.TMP);
                
                await _dialogueCompletionSource.Task;

                await ExecuteDialoguePostActionAsync(dialogueSlot.TMP, act, scene, dialogueRecord.DialogueActions);
                await ActiveAnswerAsync(dialogueRecord.AnswerIds);
                await TriggerEventAsync(dialogueRecord.EventId, dialogueRecord.SlotId, dialogueSlot);
            }
            
            await _view.ScrollToAsync(0);
            // await UniTask.Delay(TimeSpan.FromSeconds(5f));
            
            _view.EnableScrollRect();
        }

        private IDialogueSlot CreateCharacterSpeechSlot(CharacterSpeechRecord record, Locale locale)
        {
            if (record == null)
                return null;
            
            _dialogueCompletionSource = new();
                        
            // var characterNameLocalText = LocalizationSettings.StringDatabase.GetLocalizedString("Character", "messenger", locale);
            var localText = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", record.LocalKey, locale);
            var param = new CharacterSpeechSlot.Param(this, localText, record.TypingSpeed)
                .WithCharacterName(string.Empty);
                        
            return _view.CreateCharacterSpeechSlot(_uiFactory, param);
        }

        private IDialogueSlot CreateNarrationSlot(NarrationRecord record, Locale locale)
        {
            if (record == null)
                return null;
            
            _dialogueCompletionSource = new();
            var localText = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", record.LocalKey, locale);
            var param = new NarrationSlot.Param(this, localText, record.TypingSpeed);
            
            return _view.CreateNarrationSlot(_uiFactory, param);
        }

        private async UniTask ExecuteDialoguePostActionAsync(TextMeshProUGUI tmp, int act, int scene, 
            List<DialogueAction> dialogueActions)
        {
            if (_dialoguePostAction == null)
                return;
            
            var tmps = new List<TextMeshProUGUI>();
            tmps.Add(tmp);
            tmps.AddRange(_view.TMPsInDialogueSlots());
            
            var param = new DialoguePostAction.Param(tmps, act, scene)
                .WithDialogueActions(dialogueActions);

            await _dialoguePostAction.SetParam(param)
                .ExecuteAsync();
        }

        private async UniTask ActiveAnswerAsync(int[] answerIds)
        {
            if (answerIds == null || answerIds.Length <= 0)
                return;

            _answerCompletionSource = new UniTaskCompletionSource();

            await _view.ShowAnswersAsync(answerIds);
                    
            await _view.ScrollToAsync(0);
            _view.EnableScrollRect();
                    
            await _answerCompletionSource.Task;   
                    
            _view.DisableScrollRect();
            await _view.ScrollToAsync(_view.ViewportHalfHeight);
        }

        private async UniTask TriggerEventAsync(int eventId, int slotId, IDialogueSlot dialogueSlot)
        {
            if (eventId <= 0)
                return;

            if (eventId == 1)
            {
                if (_battleController != null)
                {
                    await _view.ScrollToAsync(100f);
                    
                    ActiveCardAsync(slotId, dialogueSlot).Forget();
                    
                    _battleController.SetDialogueTMP(dialogueSlot?.TMP);
                    await _battleController.PlayBattleAsync();
                }
            }
        }
        private async UniTask ActiveCardAsync(int slotId, IDialogueSlot activeSlot)
        {
            var slotRecord = SlotTableContainer.Instance?.GetSlotRecord(slotId);
            if (slotRecord == null)
                return;

            await _cardController.SetCardsAsync(slotRecord.AllowedCardIds);
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            _cardController.ShowCards();

            if (_cardSelectionHandler != null)
            {
                _cardSelectionHandler.BeginSelection(activeSlot);
                // await _cardSelectionHandler.AwaitCompletionAsync();
            }
        }
        
        #region CharacterSpeechSlot.Listener
        void CharacterSpeechSlot.IListener.End()
        {
            _dialogueCompletionSource?.TrySetResult();
        }
        #endregion

        #region NarrationSlot.IListener

        void NarrationSlot.IListener.End()
        {
            _dialogueCompletionSource?.TrySetResult();
        }
        #endregion
        
        #region AnswerSlot.IListener

        void AnswerSlot.IListener.OnSelectedAnswer(int id)
        {
            _view?.HideAnswersAsync();
            _answerCompletionSource?.TrySetResult();
        }
        #endregion
        
        #region ISceneListener

        async UniTask ISceneListener.OnStartSceneAsync(int act, int scene)
        {
            if (act == 1 && scene < 3)
                await _view.FadeLibraryAsync(0.4f, 3f);
            
            await PlayDialogueAsync(act, scene);
        }

        async UniTask ISceneListener.OnEndSceneAsync(int act, int scene)
        {
            if (act == 1 && scene == 1)
                await _view.FadeLibraryAsync(0, 3f);
        }
        #endregion

        private float ScrollPositionY
        {
            get
            {
                var answerStatus = UniTaskStatus.Canceled;
                if (_answerCompletionSource?.Task != null)
                    answerStatus = _answerCompletionSource.Task.Status;
            
                float scrollPositionY = 0;
                if (answerStatus != UniTaskStatus.Pending)
                    scrollPositionY = _view.ViewportHalfHeight;
            
                if(_cardController != null &&
                   _cardController.IsActiveCards)
                    scrollPositionY = 100f;
                
                return scrollPositionY;
            }
        }
        
        protected override void OnDimensionChanged(bool isPortrait)
        {
            base.OnDimensionChanged(isPortrait);

            if (_view == null)
                return;
            
            _view?.OnDimensionChanged(isPortrait, ScrollPositionY);
        }
    }
}

