using System;
using UnityEngine;

using Common.Models;

namespace Tables.Records
{
    [Serializable]
    public class SlotResult
    {
        public int CardId = 0;

        // <slot_N> 위치에 채워질 결과 텍스트의 Localization 키 (Dialogue 테이블 기준).
        // 비어 있으면 카드 이름(Card 테이블의 LocalKey)으로 폴백한다.
        public string ResultLocalKey = string.Empty;
    }

    [Serializable]
    public class SlotRecord
    {
        public int Id = 0;
        public int[] AllowedCardIds = null;
        public string SelectedDialogueLocalKey = string.Empty;
        public SlotResult[] SlotResults = null;
    }
}