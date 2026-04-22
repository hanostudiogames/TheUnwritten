using System;
using Tables.Records;
using UnityEngine;

namespace Tables.Records
{
    [Serializable]
    public class SceneRecord
    {
        public int Index = 0;

        // 전투 씬 여부. true 인 경우 MainPresenter 대신 BattleManager 가 씬을 처리한다.
        public bool IsBattle = false;

        [SerializeReference, SubclassSelector]
        public DialogueRecord[] DialogueRecords = null;
    }
}

