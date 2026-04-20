using System;
using Tables.Records;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Tables.Records
{
    [Serializable]
    public class ActRecord
    {
        public int Index = 0;

        public SceneRecord[] SceneRecords = null;
    }
}