using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

using Cysharp.Threading.Tasks;

using Common;
using Tables.Containers;
using Tables.Records;
using TMPro;
using UI.Cards;
using UI.Slots;

namespace UI.Main
{
    /// <summary>
    /// 전투 씬 전용 오케스트레이터.
    /// MainPresenter 와 형제 계층으로 동작하며, SceneRecord.IsBattle 이 true 인
    /// 씬에서만 Dialogue/Card 진행을 담당한다. 일반 씬은 MainPresenter 가 처리한다.
    ///
    /// v1 범위:
    /// - 전투 씬 판별 후 DialogueRecord 들을 순차 재생 (기존 DialogueSlot/Typer 재사용)
    /// - 몬스터 등장 연출은 DialoguePostAction (InkMonsterAppear 등) 재사용
    /// - 카드 선택은 CardFanSpread 재사용
    /// - 실시간 서술 개입(빈칸 [ ]) 은 후속 단계에서 추가 예정
    /// </summary>
    public class BattleManager :
        CharacterSpeechSlot.IListener,
        NarrationSlot.IListener,
        ISceneListener
    {
        private readonly MainView _view = null;
        private readonly MainModel _model = null;
        private readonly UIFactory _uiFactory = null;
        private readonly IGameManager _gameManager = null;
        private readonly CardController _cardController = null;

        private UniTaskCompletionSource _dialogueCompletionSource = null;
        private UniTaskCompletionSource _cardCompletionSource = null;
        private DialoguePostAction _dialoguePostAction = null;

        public BattleManager(MainView view, MainModel model, IGameManager gameManager, UIFactory uiFactory,
            CardController cardController)
        {
            _view = view;
            _model = model;
            _gameManager = gameManager;
            _uiFactory = uiFactory;
            _cardController = cardController;
        }

        #region ISceneListener

        async UniTask ISceneListener.OnStartSceneAsync(int act, int scene)
        {
            await RunBattleSceneIfApplicableAsync(act, scene);
        }

        UniTask ISceneListener.OnEndSceneAsync(int act, int scene)
        {
            return UniTask.CompletedTask;
        }

        #endregion

        private async UniTask RunBattleSceneIfApplicableAsync(int act, int scene)
        {
            var sceneRecord = ActTableContainer.Instance?.GetSceneRecord(act, scene);
            if (sceneRecord == null)
                return;

            // 전투 씬이 아니면 본 매니저는 아무것도 하지 않는다 (MainPresenter 담당).
            if (!sceneRecord.IsBattle)
                return;

            if (_view == null)
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
                if (dialogue == null)
                    continue;

                IDialogueSlot dialogueSlot = null;
                switch (dialogue)
                {
                    case CharacterSpeechRecord characterSpeech:
                        dialogueSlot = CreateCharacterSpeechSlot(characterSpeech, locale);
                        break;

                    case NarrationRecord narration:
                        dialogueSlot = CreateNarrationSlot(narration, locale);
                        break;
                }

                if (dialogueSlot == null)
                    continue;

                await _dialogueCompletionSource.Task;

                await ExecuteDialoguePostActionAsync(dialogueSlot.TMP, act, scene, dialogue.DialogueActions);
                await ActiveCardAsync(dialogue.SlotId);
            }

            await _view.ScrollToAsync(0);
            _view.EnableScrollRect();
        }

        private IDialogueSlot CreateCharacterSpeechSlot(CharacterSpeechRecord record, Locale locale)
        {
            if (record == null)
                return null;

            _dialogueCompletionSource = new();

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

        private async UniTask ActiveCardAsync(int slotId)
        {
            if (_cardController == null)
                return;

            var slotRecord = SlotTableContainer.Instance?.GetSlotRecord(slotId);
            if (slotRecord == null)
                return;

            await _cardController.SetCardsAsync(slotRecord.AllowedCardIds);

            _cardCompletionSource = new UniTaskCompletionSource();

            await _view.ScrollToAsync(0);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            _cardController.ShowCards();

            // v1 에서는 카드 선택 후속 로직이 아직 없으므로 바로 완료.
            // TODO: CardSlot 클릭 리스너를 통해 선택된 카드 ID 로 전투 결과 분기.
            _cardCompletionSource.TrySetResult();

            await _cardCompletionSource.Task;
        }

        #region CharacterSpeechSlot.IListener

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
    }
}
