using UnityEngine.Localization.Settings;

using Cysharp.Threading.Tasks;

using Tables.Records;
using UI.Cards;
using UI.Slots;

namespace UI.Main
{
    /// <summary>
    /// 기획서 ⑤ 실시간 서술 개입(Narrative Intervention) 전담.
    /// 활성 다이얼로그의 Typer 에 &lt;slot_N&gt; 이 존재할 때, 카드 선택 시
    /// 카드 이름을 첫 번째 빈 슬롯에 타이핑해 채운다.
    ///
    /// 적용 씬 (v7.0 기획서 기준):
    /// - 1-3 첫 조우 (튜토리얼)
    /// - 2-7 서술 포식자 (비움이 정답 — 별도 처리 필요, TODO)
    /// - 3-5 최종전 1R / 3R / 5R
    /// </summary>
    public interface ICardSelectionHandler
    {
        void BeginSelection(IDialogueSlot activeSlot, SlotRecord slotRecord);
        UniTask<CardRecord> AwaitCompletionAsync();
    }

    public class SlotInteractionHandler : ICardSelectionHandler, CardSlot.IListener
    {
        private IDialogueSlot _activeSlot = null;
        private SlotRecord _slotRecord = null;
        private UniTaskCompletionSource<CardRecord> _completionSource = null;
        private bool _filling = false;

        public void BeginSelection(IDialogueSlot activeSlot, SlotRecord slotRecord)
        {
            _activeSlot = activeSlot;
            _slotRecord = slotRecord;
            _completionSource = new UniTaskCompletionSource<CardRecord>();
            _filling = false;
        }

        public UniTask<CardRecord> AwaitCompletionAsync()
        {
            return _completionSource?.Task ?? UniTask.FromResult<CardRecord>(null);
        }

        void CardSlot.IListener.OnCardSelected(CardRecord cardRecord)
        {
            if (_filling || cardRecord == null)
                return;

            HandleCardSelectedAsync(cardRecord).Forget();
        }

        private async UniTaskVoid HandleCardSelectedAsync(CardRecord cardRecord)
        {
            _filling = true;

            var typer = _activeSlot?.Typer;
            var slotName = typer?.FirstEmptySlot();

            if (!string.IsNullOrEmpty(slotName))
            {
                var text = ResolveSlotResultText(cardRecord);
                await typer.TypeIntoSlotAsync(slotName, text);
            }

            _completionSource?.TrySetResult(cardRecord);
        }

        // SlotRecord.SlotResults 에 카드 매핑이 있으면 그 ResultLocalKey 를 Dialogue 테이블에서 해석,
        // 없으면 카드 이름(Card 테이블)으로 폴백한다.
        private string ResolveSlotResultText(CardRecord cardRecord)
        {
            var locale = LocalizationSettings.SelectedLocale;

            var results = _slotRecord?.SlotResults;
            if (results != null)
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    var result = results[i];
                    if (result == null || result.CardId != cardRecord.Id)
                        continue;

                    if (!string.IsNullOrEmpty(result.ResultLocalKey))
                    {
                        return LocalizationSettings.StringDatabase
                            .GetLocalizedString("Dialogue", result.ResultLocalKey, locale);
                    }

                    break;
                }
            }

            return LocalizationSettings.StringDatabase
                .GetLocalizedString("Card", cardRecord.LocalKey, locale);
        }
    }
}
