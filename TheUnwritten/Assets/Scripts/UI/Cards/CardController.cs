using System.Linq;
using Cysharp.Threading.Tasks;
using Tables;
using Tables.Containers;
using UnityEngine;

namespace UI.Cards
{
    public class CardController
    {
        private readonly CardFanSpread _cardFanSpread = null;
        
        public CardController(CardFanSpread cardFanSpread)
        {
            _cardFanSpread = cardFanSpread;
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
                if(cardRecord == null)
                    continue;
                
                if(i >= cardSlots.Count)
                    continue;
                
                var cardSlot = cardSlots[i];
                if(cardSlot == null)
                    continue;

                cardSlot.Initialize(new CardSlot.Param(cardRecord));
                cardSlot.Activate();
            }

            await UniTask.CompletedTask;
            // await _cardFanSpread.InitializeCards(cardIds.ToList());
        }
        
        public void ShowCards()
        {
            if (_cardFanSpread == null)
                return;
            
            _cardFanSpread.gameObject.SetActive(true);
        }
    }
}

