
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using DG.Tweening;

using Common;
using Tables.Containers;

using TMPro;
using UI.Cards;
using UI.Slots;
using Vector2 = UnityEngine.Vector2;

namespace UI.Main
{
    public class MainView : Common.View<MainPresenter>
    {
        private const float DialogueSlotShiftDuration = 0.45f;
        private const float DialogueSlotFadeDuration = 0.18f;
        private static readonly Color32 ActSceneTextColor = new(0xF0, 0xC8, 0x6A, 0xFF);
        private static readonly Color32 ActSceneOutlineColor = new(0x2A, 0x16, 0x08, 0xFF);
        private const float ActSceneOutlineWidth = 0.2f;

        [SerializeField] private RectTransform bgRectTr = null;
        [SerializeField] private CardFanSpread cardFanSpread = null;
        [SerializeField] private Image libraryImg = null;
        [SerializeField] private TextMeshProUGUI actSceneTMP = null;
        
        [Header("Narrative")]
        [SerializeField] private ScrollRect narrativeScrollRect = null;
        [SerializeField] private RectTransform narrativeRootRectTr = null;
        [SerializeField] private RectTransform answersRootRectTr = null;
        
        private List<IAnswerSlot> _answerSlots = null; 
        private List<IDialogueSlot> _dialogueSlots = null;

        public CardFanSpread CardFanSpread => cardFanSpread;
        
        public float ViewportHalfHeight
        {
            get
            {
                if (narrativeScrollRect?.viewport == null)
                    return 0;

                return narrativeScrollRect.viewport.rect.height * 0.5f - 120f;
            }
        }

        public override void Initialize(MainPresenter presenter)
        {
            base.Initialize(presenter);

            _dialogueSlots = new();
            
            var answerSlots = answersRootRectTr.GetComponentsInChildren<AnswerSlot>();
            if (answerSlots != null)
            {
                if(_answerSlots == null)
                    _answerSlots = new ();
                
                _answerSlots?.AddRange(answerSlots);
            }

            ApplyActSceneTextStyle();
        }
        
        public override void Activate()
        {
            base.Activate();
            
            
        }

        public void InitializeAnswerSlots(AnswerSlot.IListener listener)
        {
            if (_answerSlots == null)
                return;

            for (int i = 0; i < _answerSlots.Count; ++i)
            {
                var answerSlot = _answerSlots[i];
                if(answerSlot == null)
                    continue;
                
                answerSlot.Initialize(new AnswerSlot.Param(listener));
            }
        }

        public void UpdateActScene(int act, int scene)
        {
            if (actSceneTMP == null)
                return;

            ApplyActSceneTextStyle();
            actSceneTMP.text = $"{act}-{scene}";
        }

        private void ApplyActSceneTextStyle()
        {
            if (actSceneTMP == null)
                return;

            actSceneTMP.color = ActSceneTextColor;
            actSceneTMP.faceColor = ActSceneTextColor;
            actSceneTMP.fontStyle |= FontStyles.Bold;
            actSceneTMP.fontWeight = FontWeight.Bold;
            actSceneTMP.outlineColor = ActSceneOutlineColor;
            actSceneTMP.outlineWidth = ActSceneOutlineWidth;
            actSceneTMP.UpdateMeshPadding();
        }
        
        public CharacterSpeechSlot CreateCharacterSpeechSlot(UIFactory uiFactory, CharacterSpeechSlot.Param param)
        {
            if (uiFactory == null)
                return null;
            
            param?.WithHeight(ViewportHalfHeight);

            var transition = CaptureDialogueContentTransition();
            var slot = uiFactory.Create<CharacterSpeechSlot, CharacterSpeechSlot.Param>(narrativeScrollRect?.content, param);
            if(slot != null)
            {
                _dialogueSlots?.Add(slot);
                PrepareDialogueSlotFadeIn(slot);
                PlayDialogueSlotAddedAnimationAsync(transition, slot).Forget();
            }
            
            return slot;
        }
        
        public NarrationSlot CreateNarrationSlot(UIFactory uiFactory, NarrationSlot.Param param)
        {
            if (uiFactory == null)
                return null;

            param?.WithHeight(ViewportHalfHeight);

            var transition = CaptureDialogueContentTransition();
            var slot = uiFactory.Create<NarrationSlot, NarrationSlot.Param>(narrativeScrollRect?.content, param);
            if(slot != null)
            {
                _dialogueSlots?.Add(slot);
                PrepareDialogueSlotFadeIn(slot);
                PlayDialogueSlotAddedAnimationAsync(transition, slot).Forget();
            }

            return slot;
        }

        private DialogueContentTransition CaptureDialogueContentTransition()
        {
            var contentRectTr = narrativeScrollRect?.content;
            if (!contentRectTr)
                return default;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTr);
            Canvas.ForceUpdateCanvases();

            return new DialogueContentTransition(
                contentRectTr,
                contentRectTr.rect.height);
        }

        private async UniTaskVoid PlayDialogueSlotAddedAnimationAsync(
            DialogueContentTransition transition,
            IDialogueSlot addedSlot)
        {
            var contentRectTr = narrativeScrollRect?.content;
            if (!contentRectTr)
                return;

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTr);
            Canvas.ForceUpdateCanvases();

            if (transition.Content == contentRectTr)
            {
                float heightDelta = Mathf.Max(0f, contentRectTr.rect.height - transition.Height);
                if (heightDelta > 0.01f)
                {
                    var targetPosition = contentRectTr.anchoredPosition;
                    var startPosition = targetPosition - Vector2.up * heightDelta;

                    contentRectTr.DOKill(false);
                    contentRectTr.anchoredPosition = startPosition;
                    contentRectTr.DOAnchorPos(targetPosition, DialogueSlotShiftDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true);
                }
            }

            PlayDialogueSlotFadeIn(addedSlot);
        }

        private void PlayDialogueSlotFadeIn(IDialogueSlot dialogueSlot)
        {
            var canvasGroup = GetOrCreateDialogueSlotCanvasGroup(dialogueSlot);
            if (canvasGroup == null)
                return;

            canvasGroup.DOKill(false);
            canvasGroup.DOFade(1f, DialogueSlotFadeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void PrepareDialogueSlotFadeIn(IDialogueSlot dialogueSlot)
        {
            var canvasGroup = GetOrCreateDialogueSlotCanvasGroup(dialogueSlot);
            if (canvasGroup == null)
                return;

            canvasGroup.DOKill(false);
            canvasGroup.alpha = 0f;
        }

        private CanvasGroup GetOrCreateDialogueSlotCanvasGroup(IDialogueSlot dialogueSlot)
        {
            var rectTr = GetDialogueSlotRect(dialogueSlot);
            if (!rectTr)
                return null;

            if (!rectTr.TryGetComponent<CanvasGroup>(out var canvasGroup))
                canvasGroup = rectTr.gameObject.AddComponent<CanvasGroup>();

            return canvasGroup;
        }

        private RectTransform GetDialogueSlotRect(IDialogueSlot dialogueSlot)
        {
            var component = dialogueSlot as Component;
            if (component == null)
                return null;

            return component.transform as RectTransform;
        }

        private readonly struct DialogueContentTransition
        {
            public readonly RectTransform Content;
            public readonly float Height;

            public DialogueContentTransition(RectTransform content, float height)
            {
                Content = content;
                Height = height;
            }
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

        private void SetAnswers(int[] answerIds, bool hasDecipher)
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
                    answerSlot.SetAnswerId(answerId, hasDecipher);
                    
                    answerSlot.Activate();
                    continue;
                }
                
                answerSlot.Deactivate();
            }
        }
        
        
        public async UniTask ShowAnswersAsync(int[] answerIds, bool hasDecipher)
        {
            SetAnswers(answerIds, hasDecipher);

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

        public async UniTask FadeLibraryAsync(float value, float duration)
        {
            if (libraryImg == null)
                return;
            
            await libraryImg.DOFade(value, duration);
        }

        public void OnDimensionChanged(bool isPortrait, float scrollPositionY)
        {
            if(bgRectTr)
                bgRectTr.localRotation = isPortrait ? Quaternion.identity : Quaternion.Euler(0, 0, 90f);
            
            ScrollToAsync(scrollPositionY).Forget();
        }
    }
}
