using Cysharp.Threading.Tasks;
using Tables.Containers;

namespace UI.Cards
{
    public class CardController
    {
        private readonly CardFanSpread _cardFanSpread = null;
        private CardSlot.IListener _listener = null;

        public CardController(CardFanSpread cardFanSpread)
        {
            _cardFanSpread = cardFanSpread;
        }

        public void SetListener(CardSlot.IListener listener)
        {
            _listener = listener;
        }

        public void SetSelectable(bool value)
        {
            _cardFanSpread?.SetSelectable(value);
        }

        public async UniTask SetCardsAsync(int[] cardIds)
        {
            if (cardIds == null || cardIds.Length == 0)
                return;

            var cardSlots = _cardFanSpread?.CardSlots;
            if (cardSlots == null)
                return;

            _cardFanSpread?.DeactivateCardSlots();

            for (int i = 0; i < cardIds.Length; ++i)
            {
                var cardRecord = CardTableContainer.Instance.GetCardRecord(cardIds[i]);
                if (cardRecord == null)
                    continue;

                if (i >= cardSlots.Count)
                    continue;

                var cardSlot = cardSlots[i];
                if (cardSlot == null)
                    continue;

                var param = new CardSlot.Param(cardRecord)
                    .WithListener(_listener);

                cardSlot.Initialize(param);
                cardSlot.Activate();
            }

            await UniTask.CompletedTask;
        }

        public bool IsActiveCards
        {
            get
            {
                if (_cardFanSpread == null)
                    return false;
                
                return _cardFanSpread.gameObject.activeSelf;
            }
        }
        
        public void ShowCards()
        {
            if (_cardFanSpread == null)
                return;

            _cardFanSpread.gameObject.SetActive(true);
        }

        public void HideCards()
        {
            if (_cardFanSpread == null)
                return;

            _cardFanSpread.gameObject.SetActive(false);
        }
    }
}
