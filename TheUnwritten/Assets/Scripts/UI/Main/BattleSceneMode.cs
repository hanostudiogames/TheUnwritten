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

        public BattleSceneMode(SceneModeContext sceneModeContext) : base(sceneModeContext)
        {
            // _cardInput = sceneModeContext?.BattleCardInput;
        }
        
       
        
        protected override async UniTask OnPlayAsync()
        {
            var view = _context?.View;
            if (view == null)
                return;

            // var cardInput = _context?.BattleCardInput;
            
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
            // 호흡 트윈은 NormalSceneMode 에서 IsMonster 캡처 시점(몬스터 등장)부터
            // 이미 돌고 있다. 여기서는 참조만 받아 종료 시 정리한다.
            // _breathingTween = payload.BreathingTween;
            _context?.ClearPayload(Common.SceneModeType.Battle);

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
            Tween shadowMeltTween = null;

            if (lastCardId == 1 && _monsterTMP != null)
            {
                // 불꽃 — Bleed 가 글자마다 다른 강도로 yoyo 노이즈 (DoBleedFlame),
                // Shake 는 sin(time+i) 라 글자별 위상차로 부분 떨림.
                var flameColor = new Color(1f, 0.42f, 0.08f, 1f);
                flameBleedTween = _monsterTMP.DoBleedFlame(0.85f, 1.4f, flameColor);
                _monsterTMP.DoShake(3f, 1.8f);
            }
            else if (lastCardId == 2 && _monsterTMP != null)
            {
                // 그림자 — 색이 검정보다 더 깊은 보라로 잠기고, 글자들이 랜덤 타이밍으로
                // 천천히 늘어져 흘러내린다. delayStep 0.04 로 글자 사이 약간씩 시차를
                // 두면 "녹는 파장" 이 글자열을 따라 번져가는 느낌이 살아난다.
                var shadowColor = new Color(0.1f, 0.05f, 0.2f, 1f);
                shadowBleedTween = _monsterTMP.DoBleed(0.85f, 2.5f, shadowColor);
                shadowMeltTween = _monsterTMP.DORandomMelt(0.5f, 2.5f, 0.04f);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(3));

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

            // 전투 종료 — 몬스터 죽음. 호흡/불꽃/그림자 정지 후 원상태로 부드럽게 복귀.
            _breathingTween?.Kill();
            flameBleedTween?.Kill();
            shadowBleedTween?.Kill();
            shadowMeltTween?.Kill();
            if (_monsterTMP != null)
            {
                _monsterTMP.DoPulse(0f, 0.6f);
                if (lastCardId == 1)
                    _monsterTMP.DoBleed(0f, 0.8f);
                else if (lastCardId == 2)
                {
                    _monsterTMP.DoBleed(0f, 0.8f);
                    _monsterTMP.DoMelt(0f, 0.8f);
                }
            }

            await UniTask.CompletedTask;
        }
        
        // IsMonster=1 인 EventRecord 의 TMP 를 BattleModePayload.MonsterTMP 로 기록하고
        // 즉시 호흡 펄스를 시작한다. 호흡은 *몬스터 등장 → 죽음* 까지 끊김없이
        // 살아있어야 하므로 Battle 씬 진입 시점이 아니라 등장 시점(여기) 에서 켠다.
        // 카드 선택 비동기 대기, 씬 전환 모두 가로질러 DOTween 이 트윈을 유지한다.
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
                    _monsterTMP.DoPulse(0f, 0.4f);
            }

            _monsterTMP.DoPulse(0.2f, 3f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            Debug.Log($"[MonsterTMP] captured \"{_monsterTMP.name}\" + breathing started (chars={_monsterTMP.textInfo.characterCount})");
        }
    }
}
