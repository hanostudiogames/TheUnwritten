using Tables.Records;
using UnityEngine;

namespace Tables
{
    [CreateAssetMenu(fileName = "CardTable", menuName = "Tables/CardTable")]
    public class CardTable : ScriptableObject
    {
        public CardRecord[] CardRecords = null;
    }
}