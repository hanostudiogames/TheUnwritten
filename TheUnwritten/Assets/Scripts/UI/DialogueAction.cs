
using System;
using Common;

namespace UI
{
    [Serializable]
    public class DialogueAction
    {
        public DialogueActionType DialogueActionType = DialogueActionType.None;
        public int TmpCount = 0;
        public float Duration = 0;
        public float TargetValue = 0;
        
        public float StartDelay = 0;
        public float EndDelay = 0;
    }
}
