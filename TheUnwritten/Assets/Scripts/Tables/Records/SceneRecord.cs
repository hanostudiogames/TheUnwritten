using UnityEngine;
using System;

using Common;
using Tables.Records;

namespace Tables.Records
{
    [Serializable]
    public class SceneRecord
    {
        public int Index = 0;

        // public bool IsBattle = false;
        public SceneModeType SceneModeType = SceneModeType.None;

        [SerializeReference, SubclassSelector]
        public DialogueRecord[] DialogueRecords = null;
    }
}

