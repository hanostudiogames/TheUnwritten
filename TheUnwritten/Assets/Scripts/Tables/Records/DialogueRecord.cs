using System;
using System.Collections.Generic;
using UnityEngine;

using Common;
using Common.Models;
using UI;

namespace Tables.Records
{
    [Serializable]
    public abstract class DialogueRecord
    {
        public string LocalKey = string.Empty;
        public float TypingSpeed = 0.05f;
        public float EndDelaySeconds = 1f;

        public int[] AnswerIds = null;

        public int SlotId = 0;
        
        public List<DialogueAction> DialogueActions = null;

        public bool IsMonster = false;
        public int EventId = 0;
    }

    [Serializable]
    public class NarrationRecord : DialogueRecord
    {
        
    }

    [Serializable]
    public class CharacterSpeechRecord : DialogueRecord
    {
        public string CharacterLocalKey = string.Empty;
    }
}
