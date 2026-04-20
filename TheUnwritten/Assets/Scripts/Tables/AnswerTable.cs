using UnityEngine;

using Common;
using Common.Models;
using Tables.Records;

namespace Tables
{
    [CreateAssetMenu(fileName = "AnswerTable", menuName = "Scriptable Objects/AnswerTable")]
    public class AnswerTable : ScriptableObject
    {
        public AnswerRecord[]  AnswerRecords = null;
    }
}