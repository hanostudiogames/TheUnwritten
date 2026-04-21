
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using DG.Tweening;

using Common;
using Tables.Containers;
using TMPro;
using UI.Slots;
using Vector2 = UnityEngine.Vector2;

namespace UI.Main
{
    public class MainView : Common.View<MainPresenter>
    {
        [SerializeField] private RectTransform bgRectTr = null;
        [SerializeField] private CardFanSpread cardFanSpread = null;
        
        [Header("Narrative")]
        [SerializeField] private ScrollRect narrativeScrollRect = null;
        [SerializeField] private RectTransform narrativeRootRectTr = null;
        [SerializeField] private RectTransform answersRootRectTr = null;
        
        // [Header("Governance")]
        // [SerializeField] private RectTransform actionsRootRectTr = null;
        // [SerializeField] private RectTransform charactersRootRectTr = null;

        // private UIFactory _uiFactory = null;
        // private List<ICharacterSlot> _characterSlots = null;
        private List<IAnswerSlot> _answerSlots = null; 
        private List<IDialogueSlot> _dialogueSlots = null;
        
        // private Vector2 _actionsOriginalMin;
        // private Vector2 _actionsOriginalMax;
        // private Vector2 _charactersOriginalMin;
        
        public float ViewportHalfHeight
        {
            get
            {
                if (narrativeScrollRect?.viewport == null)
                    return 0;

                return narrativeScrollRect.viewport.rect.height * 0.5f - 100f;
            }
        }

        public override void Initialize(MainPresenter presenter)
        {
            base.Initialize(presenter);

            _dialogueSlots = new();
            
            // _actionsOriginalMin = actionsRootRectTr.offsetMin;
            // _actionsOriginalMax = actionsRootRectTr.offsetMax;
            // _charactersOriginalMin = charactersRootRectTr.offsetMin;
        }
        
        public override void Activate()
        {
            base.Activate();
            
            
        }

        public void InitializeAnswerSlots(AnswerSlot.IListener listener)
        {
            var answerSlots = answersRootRectTr.GetComponentsInChildren<AnswerSlot>();
            if (answerSlots == null)
                return;
            
            if(_answerSlots == null)
                _answerSlots = new ();
            
            for (int i = 0; i < answerSlots.Length; ++i)
            {
                var answerSlot = answerSlots[i];
                if(answerSlot == null)
                    continue;
                
                answerSlot.Initialize(new AnswerSlot.Param(listener));
                
                _answerSlots?.Add(answerSlot);
            }
        }
        
        public CharacterSpeechSlot CreateCharacterSpeechSlot(UIFactory uiFactory, CharacterSpeechSlot.Param param)
        {
            if (uiFactory == null)
                return null;
            
            param?.WithHeight(ViewportHalfHeight);
           
            var slot = uiFactory.Create<CharacterSpeechSlot, CharacterSpeechSlot.Param>(narrativeScrollRect?.content, param);
            if(slot != null)
                _dialogueSlots?.Add(slot);
            
            return slot;
        }
        
        public NarrationSlot CreateNarrationSlot(UIFactory uiFactory, NarrationSlot.Param param)
        {
            if (uiFactory == null)
                return null;

            param?.WithHeight(ViewportHalfHeight);
            
            var slot = uiFactory.Create<NarrationSlot, NarrationSlot.Param>(narrativeScrollRect?.content, param);
            if(slot != null)
                _dialogueSlots?.Add(slot);

            return slot;
        }

        public async UniTask ScrollToAsync(float positionY)
        {
            var contentRectTr = narrativeScrollRect.content;
            if (!contentRectTr)
                return;

            Vector2 targetPosition = new Vector2(0, positionY);
            // 현재 x값은 유지하고 y값만 targetY로 이동
            await contentRectTr.DOAnchorPos(targetPosition, 0.5f)
                .SetEase(Ease.OutCubic) // 부드러운 감속 효과
                .SetUpdate(true)
                .ToUniTask();
        }

        public void EnableScrollRect()
        {
            if (narrativeScrollRect != null)
                narrativeScrollRect.enabled = true;
        }

        public void DisableScrollRect()
        {
            if (narrativeScrollRect != null)
                narrativeScrollRect.enabled = false;
        }

        // private async UniTask ShowNarrativePanelAsync()
        // {
        //     HideAnswersAsync().Forget();
        //     await HideGovernancePanelAsync(0.3f);
        //     
        //     narrativeRootRectTr.gameObject.SetActive(true);
        // }

        // private async UniTask HideGovernancePanelAsync(float duration)
        // {
        //     if (actionsRootRectTr == null)
        //         return;
        //
        //     if (charactersRootRectTr == null)
        //         return;
        //     
        //     var offsetX = 200f;
        //
        //     var actionsMoveX = _actionsOriginalMin.x - (_actionsOriginalMax.x + actionsRootRectTr.rect.width) - offsetX;
        //     var charactersMoveX =
        //         _charactersOriginalMin.x + (charactersRootRectTr.rect.width - _charactersOriginalMin.x) + offsetX;
        //     
        //     await UniTask.WhenAll(
        //         actionsRootRectTr.DoOffsetMoveX(actionsMoveX, duration),
        //         charactersRootRectTr.DoOffsetMoveX(charactersMoveX, duration)
        //     );
        // }

        public List<TextMeshProUGUI> TMPsInDialogueSlots()
        {
            if (_dialogueSlots == null)
                return null;
            
            var tmps = new List<TextMeshProUGUI>();
            for (int i = 0; i < _dialogueSlots.Count; ++i)
            {
                var dialogueSlot = _dialogueSlots[i];
                if(dialogueSlot == null)
                    continue;
                
                tmps?.Add(dialogueSlot.TMP);
            }

            return tmps;
        }
        
        #region Answers

        private void SetAnswers(int[] answerIds)
        {
            if (answerIds == null)
                return;
            
            if (_answerSlots == null)
                return;

            for (int i = 0; i < _answerSlots.Count; ++i)
            {
                var answerSlot = _answerSlots[i];
                if(answerSlot == null)
                    continue;

                if (answerIds.Length > i)
                {
                    int answerId = answerIds[i];
                    answerSlot.SetIndex(i + 1);
                    answerSlot.SetAnswerId(answerId);
                    
                    answerSlot.Activate();
                    continue;
                }
                
                answerSlot.Deactivate();
            }
        }
        
        
        public async UniTask ShowAnswersAsync(int[] answerIds)
        {
            SetAnswers(answerIds);

            // animation 
            if (!answersRootRectTr)
                return;

            answersRootRectTr.gameObject.SetActive(true);
        }

        public async UniTask HideAnswersAsync()
        {
            if (!answersRootRectTr)
                return;

            answersRootRectTr.gameObject.SetActive(false);
        }
        #endregion

        public void ShowCards()
        {
            if (cardFanSpread == null)
                return;
            
            cardFanSpread.gameObject.SetActive(true);
        }

        public void OnDimensionChanged(bool isPortrait, UniTaskStatus answerStatus)
        {
            if(bgRectTr)
                bgRectTr.localRotation = isPortrait ? Quaternion.identity : Quaternion.Euler(0, 0, 90f);
            
            // if (mode == GameMode.Narrative)
                // HideGovernancePanelAsync(0).Forget();

            float scrollPositionY = 0;
            if (answerStatus != UniTaskStatus.Pending)
                scrollPositionY = ViewportHalfHeight;
                
            ScrollToAsync(scrollPositionY).Forget();
        }
    }
}

