using System;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UI.Components;
using UI.Effects;
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
        private Tween _breathingTween = null;
        private Vector3 _monsterBaseScale = Vector3.one;

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

            _dialogueSlot = payload.DialogueSlot;
            _slotId = payload.SlotId;
            _monsterTMP = payload.MonsterTMP;
            _context?.ClearPayload(Common.SceneModeType.Battle);

            if (_monsterTMP == null)
                return;

            CaptureMonsterTMP();
            Debug.Log($"[Battle] payload read — DialogueSlot={(_dialogueSlot!=null)}, SlotId={_slotId}, MonsterTMP={(_monsterTMP!=null ? _monsterTMP.name : "null")}, breathing={(_breathingTween!=null && _breathingTween.IsActive())}");

            await ShowCardAsync(_slotId > 0 ? _slotId : 1, _dialogueSlot);

            // 카드 선택에 따른 괴물 연출 분기.
            // - 불꽃(Id=1): 외부 공격, burning. 주황 bleed 깜빡임 + 떨림 — 격렬·아프게.
            // - 그림자(Id=2): 내부 잠식, melting. 깊은 보라 bleed + 글자별 랜덤 melt —
            //   잉크가 응고를 잃고 액체로 회귀하는 정적·내면적 dissolution.
            var lastCardId = _context?.CardInventory?.LastSelectedCardId ?? 0;
            Tween flameBleedTween = null;
            Tween shadowBleedTween = null;
            // Tween shadowMeltTween = null;
            Tween shadowSpreadTween = null;
            float duration = 2.5f;

            switch (lastCardId)
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
            // shadowMeltTween?.Kill();
            shadowSpreadTween?.Kill();
            // await RestoreMonsterToIdleAsync();

            var locale = LocalizationSettings.SelectedLocale;

            for (int i = 0; i < dialogueRecords.Length; ++i)
            {
                var dialogueRecord = dialogueRecords[i];
                if (dialogueRecord == null)
                    continue;

                if (!ShouldPlayRecord(dialogueRecord))
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

            flameBleedTween?.Kill();
            await RestoreMonsterToIdleAsync();

            await UniTask.CompletedTask;
        }
        
        // IsMonster=1 인 EventRecord 의 TMP 를 전투 대상으로 잡고 호흡을 시작한다.
        // 호흡은 TMP 버텍스 펄스가 아니라 전체 Transform 스케일로 처리해
        // 글자가 파르르 떨리는 느낌 없이 덩어리 전체가 천천히 부푼다.
        private void CaptureMonsterTMP()
        {
            if (_monsterTMP == null)
                return;

            // 텍스트가 비어있으면(아직 타이핑 전) vertex/characterCount 가 0 이라
            // DoPulse 의 GetState → state.pulse 가 null 인 채로 접근되어 NRE.
            // 호출 측에서 타이핑 완료 후 부르는 게 정상이지만, 방어적으로 한 번 더 확인.
            _monsterTMP.ForceMeshUpdate();
            if (_monsterTMP.textInfo == null || _monsterTMP.textInfo.characterCount == 0)
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
            // _monsterTMP.DoPulse(0f, 0.3f);
            _monsterTMP.DoShake(0f, 0.3f);

            _breathingTween = _monsterTMP.transform
                .DOScale(_monsterBaseScale * 1.06f, 1.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            Debug.Log($"[MonsterTMP] captured \"{_monsterTMP.name}\" + breathing started (chars={_monsterTMP.textInfo.characterCount})");
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
    }
}
