using UnityEngine;

using Common;
using Common.Models;
using Tables.Records;

namespace Tables.Records
{
    [CreateAssetMenu(fileName = "TurnRecord", menuName = "Scriptable Objects/TurnRecord")]
    public class TurnRecord : ScriptableObject
    {
        public int Turn = 0;
        
        [SerializeReference, SubclassSelector]
        public DialogueRecord[] StartDialogueRecords = null;
        
        [SerializeReference, SubclassSelector]
        public DialogueRecord[] EndDialogueRecords = null;
    }
}
