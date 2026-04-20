using System;
using UnityEngine;

using Common;
using Common.Models;

namespace Tables.Records
{
    [Serializable]
    public abstract class DialogueRecord
    {
        public string LocalKey = string.Empty;
        public float TypingSpeed = 0.05f;
        public float EndDelaySeconds = 1f;

        public int[] AnswerIds = null;
        
        public DialoguePostActionType PostActionType = DialoguePostActionType.None;
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
