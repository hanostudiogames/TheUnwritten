using UnityEngine;

using Common;
using Common.Models;
using Tables.Records;

namespace Tables
{
    [CreateAssetMenu(fileName = "SlotTable", menuName = "Tables/SlotTable")]
    public class SlotTable : ScriptableObject
    {
        public SlotRecord[] SlotRecords = null;
    }
}