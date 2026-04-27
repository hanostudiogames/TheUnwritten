using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

using TMPro;

using Common;
using Tables.Containers;
using UI.Cards;
using UI.Effects;
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
            var param = new CharacterSpeechSlot.Param(this, localText, record.TypingSpeed, record.TextRevealMode)
                .WithCharacterName(string.Empty);
                        
            return _context?.View?.CreateCharacterSpeechSlot(_context?.UIFactory, param);
        }

        protected IDialogueSlot CreateNarrationSlot(string localKey, float typingSpeed, TextRevealMode revealMode, Locale locale)
        {
            _dialogueCompletionSource = new();
            var localText = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", localKey, locale);
            var param = new NarrationSlot.Param(this, localText, revealMode, typingSpeed);

            return _context?.View.CreateNarrationSlot(_context?.UIFactory, param);
        }

        // EventRecord 서브타입별 사이드이펙트 디스패치. 구체 타입 자체가 디스패치
        // 키이므로 enum/EventId 매핑이 필요 없다. 새 이벤트 종류를 추가하려면
        // EventRecord 서브클래스를 만들고 여기에 case 한 줄만 추가하면 된다.
        protected void DispatchEvent(EventRecord record, IDialogueSlot slot)
        {
            if (_context == null || record == null)
                return;

            switch (record)
            {
                case BattleEventRecord battle:
                    HandleBattleEvent(battle, slot);
                    break;

                case CardGrantEventRecord grant:
                    HandleCardGrantEvent(grant);
                    break;
            }
        }

        // 전투 씬으로 넘길 페이로드(슬롯/SlotId/MonsterTMP) 셋업. slot 이 없으면
        // (예: LocalKey 빈 silent 이벤트) 핸드오프할 게 없으니 무시.
        private void HandleBattleEvent(BattleEventRecord record, IDialogueSlot slot)
        {
            if (slot == null)
                return;

            // 기존 페이로드(앞선 IsMonster 캡처 등) 가 있으면 보존하고 Battle 메타만
            // 갱신. MonsterTMP 는 이 레코드 자체가 IsMonster=1 일 때만 덮어쓰고,
            // 그렇지 않으면 미리 캡처해둔 값을 유지한다.
            var payload = _context.GetPayload<BattleModePayload>(SceneModeType.Battle)
                          ?? new BattleModePayload();
            payload.Act = _act;
            payload.Scene = _scene;
            payload.SlotId = record.SlotId;
            payload.DialogueSlot = slot;
            if (record.IsMonster)
                payload.MonsterTMP = slot.TMP;

            _context.SetPayload(SceneModeType.Battle, payload);
        }

        // CardGrant — 인벤토리에 카드 추가. 화면 출력 없이도 동작 가능 (slot 무시).
        private void HandleCardGrantEvent(CardGrantEventRecord record)
        {
            var inventory = _context?.CardInventory;
            if (inventory == null)
                return;

            var cardIds = record.CardIds;
            if (cardIds == null)
                return;

            for (int i = 0; i < cardIds.Length; ++i)
                inventory.AddCard(cardIds[i]);
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
        
        protected async UniTask ShowAnswerAsync(int[] answerIds)
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
        
        protected async UniTask<CardRecord> ShowCardAsync(int slotId, IDialogueSlot activeSlot)
        {
            var cardController = _context?.CardController;
            if (cardController == null)
                return null;

            var cardSelectionHandler = _context?.CardSelectionHandler;
            if (cardSelectionHandler == null)
                return null;

            var slotRecord = SlotTableContainer.Instance?.GetSlotRecord(slotId);
            if (slotRecord == null)
                return null;

            var availableCardIds = FilterOwnedCards(slotRecord.AllowedCardIds);
            if (availableCardIds == null || availableCardIds.Length == 0)
                return null;

            await cardController.SetCardsAsync(availableCardIds);

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            cardController.ShowCards();

            if (_context?.CardSelectionHandler != null)
            {
                cardSelectionHandler.BeginSelection(activeSlot, slotRecord);
                var selectedCard = await cardSelectionHandler.AwaitCompletionAsync();
                cardController.HideCards();

                _context?.CardInventory?.SetLastSelectedCard(selectedCard?.Id ?? 0);

                return selectedCard;
            }

            cardController.HideCards();
            return null;
        }

        // AllowedCardIds 중 플레이어가 소유한 카드만 반환. 인벤토리가 없으면(테스트/무결성) 원본 그대로 반환.
        private int[] FilterOwnedCards(int[] allowedCardIds)
        {
            if (allowedCardIds == null)
                return null;

            var inventory = _context?.CardInventory;
            if (inventory == null)
                return allowedCardIds;

            var owned = new List<int>(allowedCardIds.Length);
            for (int i = 0; i < allowedCardIds.Length; ++i)
            {
                var id = allowedCardIds[i];
                if (inventory.HasCard(id))
                    owned.Add(id);
            }

            return owned.ToArray();
        }

        // RequiredCardId 가 지정된 레코드는 마지막으로 선택된 카드 Id 와 일치할 때만 재생한다.
        protected bool ShouldPlayRecord(DialogueRecord record)
        {
            if (record == null)
                return false;

            if (record.RequiredCardId == 0)
                return true;

            var lastId = _context?.CardInventory?.LastSelectedCardId ?? 0;
            return record.RequiredCardId == lastId;
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
                    scrollPositionY = 70f;
                
                return scrollPositionY;
            }
        }
    }
}
