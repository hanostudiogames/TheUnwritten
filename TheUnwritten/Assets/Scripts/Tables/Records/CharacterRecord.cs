using System;
using UnityEngine;

using Common;
using Common.Models;

namespace Tables.Records
{
    [Serializable]
    public class CharacterRecord
    {
        public CharacterEntry CharacterEntry = null;
        
        public string NameLocalKey = string.Empty;
        // public CharacterRole Role = CharacterRole.None;
    }
}
