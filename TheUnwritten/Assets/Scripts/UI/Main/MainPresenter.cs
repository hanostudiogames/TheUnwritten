using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

using Cysharp.Threading.Tasks;
using DG.Tweening;

using Common;
using Data;
using Tables.Containers;
using Tables.Records;
using TMPro;
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
        
        private UniTaskCompletionSource _dialogueCompletionSource = null;
        private UniTaskCompletionSource _answerCompletionSource = null;
        private DialoguePostAction _dialoguePostAction = null;
        
        public MainPresenter(MainView view, MainModel model, IGameManager gameManager, UIManager uiManager) : base (view, model)
        {
            _view = view;

            _gameManager = gameManager;
            _uiFactory = new UIFactory(uiManager);
            
            // gameManager?.RegisterModeHandler<MainPresenter>(OnModeChanged);
            uiManager?.RegisterDimensionHandler<MainPresenter>(OnDimensionChanged);
            
            view?.InitializeAnswerSlots(this);
        }

        public override void Activate()
        {
            base.Activate();
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
                var dialogue = dialogues[i];
                if(dialogue == null) 
                    continue;

                IDialogueSlot dialogueSlot = null;
                switch (dialogue)
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
                
                await _dialogueCompletionSource.Task;

                await ExecuteDialoguePostActionAsync(dialogueSlot.TMP, act, scene, dialogue.PostActionType);
                await ActiveAnswerAsync(dialogue.AnswerIds);
                
                // var answerIds = dialogue.AnswerIds;
                // if (answerIds != null &&
                //     answerIds.Length > 0)
                // {
                //     Debug.Log(answerIds.Length);
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
            }
            
            await _view.ScrollToAsync(0);
            await UniTask.Delay(TimeSpan.FromSeconds(5f));
            
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

        private async UniTask ExecuteDialoguePostActionAsync(TextMeshProUGUI tmp, int act, int scene, DialoguePostActionType postActionType)
        {
            if (_dialoguePostAction == null)
                return;

            var tmps = new List<TextMeshProUGUI>();
            
            if (postActionType == DialoguePostActionType.DoShearAllTMP ||
                postActionType == DialoguePostActionType.DoFoldAllTMP)
                tmps.AddRange(_view.TMPsInDialogueSlots());
            else
                tmps.Add(tmp);
            
            var param = new DialoguePostAction.Param(tmps, act, scene)
                .WithDialoguePostActionType(postActionType);

            await _dialoguePostAction.SetParam(param)
                .ExecuteAsync();
        }

        private async UniTask ActiveAnswerAsync(int[] answerIds)
        {
            // var answerIds = dialogue.AnswerIds;
            if (answerIds == null || answerIds.Length <= 0)
                return;
            
            Debug.Log(answerIds.Length);
                    
            _answerCompletionSource = new UniTaskCompletionSource();

            await _view.ShowAnswersAsync(answerIds);
                    
            await _view.ScrollToAsync(0);
            _view.EnableScrollRect();
                    
            await _answerCompletionSource.Task;   
                    
            _view.DisableScrollRect();
            await _view.ScrollToAsync(_view.ViewportHalfHeight);
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
            await PlayDialogueAsync(act, scene);
        }

        UniTask ISceneListener.OnEndSceneAsync()
        {
            return UniTask.CompletedTask;
        }
        #endregion
        
        protected override void OnDimensionChanged(bool isPortrait)
        {
            base.OnDimensionChanged(isPortrait);
            
            var answerStatus = UniTaskStatus.Canceled;
            if (_answerCompletionSource?.Task != null)
                answerStatus = _answerCompletionSource.Task.Status;
            
            _view?.OnDimensionChanged(isPortrait, answerStatus);
        }
    }
}

