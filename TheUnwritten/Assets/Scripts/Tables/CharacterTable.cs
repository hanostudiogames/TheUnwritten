using System;
using UnityEngine;

using Common;
using Common.Models;
using Tables.Records;

namespace Tables
{
    [CreateAssetMenu(fileName = "CharacterTable", menuName = "Scriptable Objects/CharacterTable")]
    public class CharacterTable : ScriptableObject
    {
        public CharacterRecord[] CharacterRecords = null; 
    }
}