using System;
using System.Collections.Generic;

namespace UI.Cards
{
    // 플레이어가 획득한 카드 ID 집합. 씬 런타임 중 CardGrant 이벤트로 확장된다.
    public class CardInventory
    {
        public event Action<int> CardGranted;

        private readonly HashSet<int> _ownedIds = new();

        public IReadOnlyCollection<int> OwnedIds => _ownedIds;

        // 가장 최근 슬롯 인터랙션에서 선택된 카드 Id. 분기 모놀로그(DialogueRecord.RequiredCardId) 판정에 사용.
        // 0 = 미선택/스킵.
        public int LastSelectedCardId { get; private set; }

        public void SetLastSelectedCard(int id)
        {
            LastSelectedCardId = id;
        }

        public bool HasCard(int id)
        {
            return _ownedIds.Contains(id);
        }

        public bool AddCard(int id)
        {
            if (id <= 0)
                return false;

            if (!_ownedIds.Add(id))
                return false;

            CardGranted?.Invoke(id);
            return true;
        }

        public void Clear()
        {
            _ownedIds.Clear();
            LastSelectedCardId = 0;
        }
    }
}
