using UnityEngine;

using Common;
using Common.Models;
using Tables.Records;

namespace Tables
{
    [CreateAssetMenu(fileName = "GameTable", menuName = "Tables/GameTable")]
    public class GameTable : ScriptableObject
    {
        public TurnRecord[] TurnRecords = null;
    }
}
    