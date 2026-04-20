using System;
using Tables.Records;
using UnityEngine;

namespace Tables.Records
{
    [Serializable]
    public class SceneRecord
    {
        public int Index = 0;
        
        [SerializeReference, SubclassSelector]
        public DialogueRecord[] DialogueRecords = null;
    }
}

