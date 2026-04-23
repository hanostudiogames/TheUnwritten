using System;
using UnityEngine;

using Common.Models;

namespace Tables.Records
{
    [Serializable]
    public class CardRecord
    {
        public int Id = 0;
        public string Key = string.Empty;
        public string LocalKey = string.Empty;

        // public DecipherRecord[] DecipherRecords = null;
    }
}