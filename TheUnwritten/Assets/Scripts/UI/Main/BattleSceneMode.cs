using UnityEngine;
using UnityEngine.Localization.Settings;
using System;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;

using Tables.Containers;
using TMPro;
using UI.Components;
using UI.Effects;
using UI.Slots;

namespace UI.Main
{
    public class BattleSceneMode : SceneMode
    {
        private static readonly Color FlameCorrosionColor = new Color(1f, 0.36f, 0.06f, 1f);
        private static readonly Color ShadowCorrosionColor = new Color(0.08f, 0.03f, 0.14f, 1f);
        private static readonly Color SpeechCorrosionColor = new Color(0.06f, 0.02f, 0.08f, 1f);

        private TextMeshProUGUI _monsterTMP = null;
        // private IDialogueSlot _dialogueSlot = null;
        private Tween _breathingTween = null;
        private Vector3 _monsterBaseScale = Vector3.one;
        private int _monsterHitCount = 0;
        private int _monsterEstimatedMaxHits = 1;
        private int _monsterSpeechCorrosionRunId = 0;
        private bool _monsterNearDeathStarted = false;

        public BattleSceneMode(SceneModeContext sceneModeContext) : base(sceneModeContext)
        {
            // _cardInput = sceneModeContext?.BattleCardInput;
        }
        
        protected override async UniTask OnPlayAsync()
        {
            var view = _context?.View;
            if (view == null)
                return;
            
            var dialogueRecords = _sceneRecord.DialogueRecords;
            if (dialogueRecords == null)
                return;
            
            await view.ScrollToAsync(70f);

            // payload 를 먼저 읽어 이전 씬에서 만든 다이얼로그 슬롯/Typer 를 확보해야
            // ShowCardAsync 가 <slot_N> 자리를 정상적으로 채울 수 있다.
            var payload = _context?.GetPayload<BattleModePayload>(Common.SceneModeType.Battle);
            if (payload == null)
                return;
            
            var slotId = payload.SlotId;
            var dialogueSlot = payload.DialogueSlot;
            _monsterTMP = payload.MonsterTMP;
            _context?.ClearPayload(Common.SceneModeType.Battle);

            if (_monsterTMP == null)
                return;

            _monsterHitCount = 0;
            _monsterNearDeathStarted = false;
            _monsterEstimatedMaxHits = Mathf.Max(1, (slotId > 0 ? 1 : 0) + CountCardPrompts(dialogueRecords));

            CaptureMonsterTMP();
            Debug.Log($"[Battle] payload read — DialogueSlot={(dialogueSlot!=null)}, SlotId={slotId}, MonsterTMP={(_monsterTMP!=null ? _monsterTMP.name : "null")}, breathing={(_breathingTween!=null && _breathingTween.IsActive())}");

            Tween flameBleedTween = null;
            
            var cardRecord = await ShowCardAsync(slotId > 0 ? slotId : 1, dialogueSlot);
            if (cardRecord != null)
            {
                PlayMonsterHitReaction(cardRecord.Id);

                // 카드 선택에 따른 괴물 연출 분기.
                // - 불꽃(Id=1): 외부 공격, burning. 주황 bleed 깜빡임 + 떨림 — 격렬·아프게.
                // - 그림자(Id=2): 내부 잠식, melting. 깊은 보라 bleed + 글자별 랜덤 melt —
                //   잉크가 응고를 잃고 액체로 회귀하는 정적·내면적 dissolution.
                // var lastCardId = _context?.CardInventory?.LastSelectedCardId ?? 0;
                
                Tween shadowBleedTween = null;
                Tween shadowSpreadTween = null;
                float duration = 2.5f;

                switch (cardRecord.Id)
                {
                    case 1:
                    {
                        // 불꽃 — Bleed 가 글자마다 다른 강도로 yoyo 노이즈 (DoBleedFlame),
                        // Shake 는 sin(time+i) 라 글자별 위상차로 부분 떨림.
                        var flameColor = new Color(1f, 0.42f, 0.08f, 1f);
                        flameBleedTween = _monsterTMP.DoBleedFlame(0.85f, duration, flameColor);
                        _monsterTMP.DoShake(3f, duration);

                        break;
                    }

                    case 2:
                    {
                        // 그림자 — 색이 검정보다 더 깊은 보라로 잠기고, 글자들이 랜덤 타이밍으로
                        // 천천히 늘어져 흘러내린다. delayStep 0.04 로 글자 사이 약간씩 시차를
                        // 두면 "녹는 파장" 이 글자열을 따라 번져가는 느낌이 살아난다.
                        var shadowColor = new Color(0.1f, 0.05f, 0.2f, 1f);
                        var spreadColor = new Color(0.1f, 0.05f, 0.2f, 0.5f);
                        shadowBleedTween = _monsterTMP.DoBleed(0.85f, duration, shadowColor);
                        // shadowMeltTween = _monsterTMP.DORandomMelt(0.5f, 2.5f, 0.04f);
                        shadowSpreadTween = _monsterTMP.DoShadowSpread(1f, duration, spreadColor);

                        break;
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(duration));
                // 
                shadowBleedTween?.Kill();
                shadowSpreadTween?.Kill();
                
                var slotRecord = SlotTableContainer.Instance?.GetSlotRecord(slotId);
                if (slotRecord != null)
                {
                    var localKey = string.Format(slotRecord.SelectedDialogueLocalKey, cardRecord.Key);
                    await TextTypingAsync(dialogueSlot?.Typer, localKey);
                }
            }
            
            for (int i = 0; i < dialogueRecords.Length; ++i)
            {
                var dialogueRecord = dialogueRecords[i];
                if (dialogueRecord == null)
                    continue;

                if (dialogueRecord.LocalKey == "20")
                {
                    flameBleedTween?.Kill();
                    await RestoreMonsterToIdleAsync();
                }

                // if (dialogueRecord.LocalKey == "24")
                //     StartMonsterNearDeathCollapse();

                // if (!ShouldPlayRecord(dialogueRecord))
                //     continue;

                // var localText = LocalizationSettings.StringDatabase
                //     .GetLocalizedString("Dialogue", dialogueRecord.LocalKey, locale);

                await TextTypingAsync(dialogueSlot?.Typer, dialogueRecord.LocalKey, dialogueRecord.TextRevealMode,
                    dialogueRecord.TypingSpeed, dialogueRecord.EndDelaySeconds);

                var dialogueActions = dialogueRecord.DialogueActions;
                if (dialogueActions != null && dialogueActions.Count > 0)
                {
                    bool monsterAbsorbedText = HasDialogueAction(dialogueActions, DialogueActionType.SuckIntoMonster);

                    await ExecuteDialoguePostActionAsync(dialogueSlot?.TMP, dialogueActions, _monsterTMP);

                    if (monsterAbsorbedText)
                        AdoptMonsterCurrentScaleAsBreathingBase();
                }
                
                cardRecord = await ShowCardAsync(dialogueRecord.SlotId, dialogueSlot);
                if (cardRecord != null)
                {
                    PlayMonsterHitReaction(cardRecord.Id);

                    var slotRecord = SlotTableContainer.Instance?.GetSlotRecord(dialogueRecord.SlotId);
                    if (slotRecord != null)
                    {
                        var localKey = string.Format(slotRecord.SelectedDialogueLocalKey, cardRecord.Key);
                        await TextTypingAsync(dialogueSlot?.Typer, localKey);
                    }
                }
            }
            
            await UniTask.CompletedTask;
        }

        private async UniTask TextTypingAsync(Typer typer, string localKey, TextRevealMode revealMode = TextRevealMode.SmoothLeftToRight, float typingSpeed = 0.1f, float endDelaySeconds = 1f)
        {
            var localText = LocalizationSettings.StringDatabase
                .GetLocalizedString("Dialogue", localKey, LocalizationSettings.SelectedLocale);

            // var dialogueTyper = dialogueSlot?.Typer;
            if (typer != null)
            {
                int corrosionRunId = 0;
                var typerParam = new Typer.Param(null)
                    .WithRevealMode(revealMode)
                    .WithTypingSpeed(typingSpeed)
                    .WithEndDelaySeconds(endDelaySeconds);

                typer.Initialize(typerParam);

                if (ShouldCorrodeMonsterWhileTyping(localText))
                    corrosionRunId = BeginMonsterSpeechCorrosion();

                try
                {
                    await typer.TypeTextAsync(localText);
                }
                finally
                {
                    EndMonsterSpeechCorrosion(corrosionRunId);
                }
            }
        }

        private async UniTask AfterSelectedCardAsync(int cardId)
        {
            var cardRecord = CardTableContainer.Instance?.GetCardRecord(cardId);
            if (cardRecord == null)
                return;
            
            
        }
        
        // IsMonster=1 인 EventRecord 의 TMP 를 전투 대상으로 잡고 호흡을 시작한다.
        // 호흡은 TMP 버텍스 펄스가 아니라 전체 Transform 스케일로 처리해
        // 글자가 파르르 떨리는 느낌 없이 덩어리 전체가 천천히 부푼다.
        private void CaptureMonsterTMP()
        {
            if (_monsterTMP == null)
                return;

            // 등장 연출이 끝난 TMP 에 ForceMeshUpdate 를 다시 호출하면, 변형된 버텍스가
            // 한 프레임 원래 위치로 재생성되어 몬스터 텍스트가 튀어 보일 수 있다.
            var textInfo = _monsterTMP.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
            {
                _monsterTMP.ForceMeshUpdate();
                textInfo = _monsterTMP.textInfo;
            }

            if (textInfo == null || textInfo.characterCount == 0)
            {
                Debug.LogWarning($"[MonsterTMP] tmp \"{_monsterTMP.name}\" has no characters yet — skipping breathing start");
                return;
            }

            if (_breathingTween != null && _breathingTween.IsActive())
            {
                _breathingTween.Kill();
                if (_monsterTMP != null)
                    _monsterTMP.transform.localScale = _monsterBaseScale;
            }

            _monsterBaseScale = _monsterTMP.transform.localScale;

            _breathingTween = _monsterTMP.transform
                .DOScale(_monsterBaseScale * 1.06f, 1.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            Debug.Log($"[MonsterTMP] captured \"{_monsterTMP.name}\" + breathing started (chars={textInfo.characterCount})");
        }

        private async UniTask RestoreMonsterToIdleAsync()
        {
            if (_monsterTMP == null)
                return;

            float duration = 0.45f;

            _breathingTween?.Kill();
            
            _monsterTMP.DoBleed(0f, 0.45f);
            _monsterTMP.DoShadowSpread(0f, duration);
            // _monsterTMP.DoMelt(0f, duration);
            _monsterTMP.DoPulse(0f, duration);
            _monsterTMP.DoShake(0f, 0.25f);

            _monsterTMP.transform
                .DOScale(_monsterBaseScale, duration)
                .SetEase(Ease.InOutSine);

            await UniTask.Delay(TimeSpan.FromSeconds(duration));

            StartMonsterBreathing();
        }

        private void StartMonsterBreathing()
        {
            if (_monsterTMP == null)
                return;

            _breathingTween?.Kill();
            _monsterTMP.transform.localScale = _monsterBaseScale;
            _breathingTween = _monsterTMP.transform
                .DOScale(_monsterBaseScale * 1.06f, 1.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void AdoptMonsterCurrentScaleAsBreathingBase()
        {
            if (_monsterTMP == null || _monsterNearDeathStarted)
                return;

            _breathingTween?.Kill();
            _monsterBaseScale = _monsterTMP.transform.localScale;
            _breathingTween = _monsterTMP.transform
                .DOScale(_monsterBaseScale * 1.06f, 1.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private bool HasDialogueAction(System.Collections.Generic.List<DialogueAction> dialogueActions, DialogueActionType actionType)
        {
            if (dialogueActions == null)
                return false;

            for (int i = 0; i < dialogueActions.Count; ++i)
            {
                var action = dialogueActions[i];
                if (action != null && action.DialogueActionType == actionType)
                    return true;
            }

            return false;
        }

        private int CountCardPrompts(Tables.Records.DialogueRecord[] dialogueRecords)
        {
            if (dialogueRecords == null)
                return 0;

            int count = 0;
            for (int i = 0; i < dialogueRecords.Length; i++)
            {
                if (dialogueRecords[i] != null && dialogueRecords[i].SlotId > 0)
                    count++;
            }

            return count;
        }

        private void PlayMonsterHitReaction(int cardId)
        {
            if (_monsterTMP == null || _monsterNearDeathStarted)
                return;

            _monsterHitCount++;

            float pressure = MonsterDamagePressure();
            int minDropCount = 1 + Mathf.FloorToInt(pressure * 1.4f);
            int maxDropCount = Mathf.Max(minDropCount, 2 + Mathf.CeilToInt(pressure * 3f));
            var corrosionColor = cardId == 1 ? FlameCorrosionColor : ShadowCorrosionColor;

            _monsterTMP.DoCorrodedLetterDrop(
                minDropCount,
                maxDropCount,
                Mathf.Lerp(0.82f, 1.12f, pressure),
                pressure,
                0.035f,
                corrosionColor);

            if (pressure >= 0.55f)
            {
                int extraDropCount = 1 + Mathf.FloorToInt(pressure * 2f);
                _monsterTMP.DoLetterDrop(1, extraDropCount, 1.15f, pressure, 0.06f, corrosionColor)
                    ?.SetDelay(0.32f);
            }
        }

        private float MonsterDamagePressure()
        {
            if (_monsterEstimatedMaxHits <= 0)
                return 0f;

            return Mathf.Clamp01((float)_monsterHitCount / _monsterEstimatedMaxHits);
        }

        private bool ShouldCorrodeMonsterWhileTyping(string localText)
        {
            if (_monsterTMP == null || _monsterNearDeathStarted)
                return false;

            if (string.IsNullOrEmpty(localText))
                return false;

            return localText.IndexOf("#6A1B9A", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   localText.IndexOf("#54157B", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   localText.IndexOf("<sprite name=\"W\"", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int BeginMonsterSpeechCorrosion()
        {
            int runId = ++_monsterSpeechCorrosionRunId;
            RunMonsterSpeechCorrosionAsync(runId).Forget();
            return runId;
        }

        private void EndMonsterSpeechCorrosion(int runId)
        {
            if (runId > 0 && _monsterSpeechCorrosionRunId == runId)
                _monsterSpeechCorrosionRunId++;
        }

        private async UniTaskVoid RunMonsterSpeechCorrosionAsync(int runId)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0.12f, 0.28f)));

            while (_monsterTMP != null && _monsterSpeechCorrosionRunId == runId)
            {
                float pressure = MonsterDamagePressure();
                int maxDropCount = pressure >= 0.6f ? 2 : 1;

                _monsterTMP.DoCorrodedLetterDrop(
                    1,
                    maxDropCount,
                    Mathf.Lerp(0.9f, 0.62f, pressure),
                    pressure,
                    0.025f,
                    SpeechCorrosionColor);

                float interval = Mathf.Lerp(1.05f, 0.32f, pressure);
                await UniTask.Delay(TimeSpan.FromSeconds(interval));
            }
        }

        private void StartMonsterNearDeathCollapse()
        {
            if (_monsterTMP == null || _monsterNearDeathStarted)
                return;

            _monsterNearDeathStarted = true;
            _monsterSpeechCorrosionRunId++;
            _breathingTween?.Kill();

            _monsterTMP.DoShake(6f, 0.35f);
            _monsterTMP.DoBleed(1f, 0.7f, ShadowCorrosionColor);
            _monsterTMP.DoShadowSpread(1.15f, 1.4f, new Color(0.07f, 0.02f, 0.12f, 0.65f));
            _monsterTMP.DoMonsterNearDeathCollapse(2.1f);

            _monsterTMP.transform
                .DOScale(_monsterBaseScale * 0.92f, 0.42f)
                .SetEase(Ease.InOutSine)
                .SetLoops(2, LoopType.Yoyo);
        }
    }
}
