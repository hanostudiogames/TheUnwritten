using UnityEngine;

using Common;
using Common.Models;
using Tables.Records;
using Unity.VectorGraphics;

namespace Tables
{
    [CreateAssetMenu(fileName = "ActTable", menuName = "Scriptable Objects/ActTable")]
    public class ActTable : ScriptableObject
    {
        public ActRecord[] ActRecords = null;
    }
}