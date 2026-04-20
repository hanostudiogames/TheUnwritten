using UnityEngine;

using Common;
using Common.Models;
using Tables.Records;

namespace Tables
{
    [CreateAssetMenu(fileName = "GameTable", menuName = "Scriptable Objects/GameTable")]
    public class GameTable : ScriptableObject
    {
        public TurnRecord[] TurnRecords = null;
    }
}
    