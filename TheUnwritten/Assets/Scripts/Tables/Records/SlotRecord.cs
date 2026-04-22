using System;
using UnityEngine;

using Common.Models;

namespace Tables.Records
{
    [Serializable]
    public class SlotRecord
    {
        public int Id = 0;
        public int[] AllowedCardIds = null;
    }
}