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
        public float TypingSpeed = 0.1f;
        public float EndDelaySeconds = 1f;
        public TextRevealMode TextRevealMode = TextRevealMode.SmoothLeftToRight;
        
        public int[] AnswerIds = null;

        public int SlotId = 0;

        // 0 이면 항상 재생. >0 이면 마지막으로 선택된 카드 Id 와 일치할 때만 재생 (분기 모놀로그용).
        public int RequiredCardId = 0;

        public List<DialogueAction> DialogueActions = null;
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
    
    // 사이드이펙트(전투 진입, 카드 지급 등) 를 일으키는 다이얼로그.
    // 구체 타입(서브클래스) 자체가 디스패치 키 — EventId(int) 라우팅을 대체.
    [Serializable]
    public abstract class EventRecord : DialogueRecord
    {
        // 모든 이벤트 레코드 공통: 이 텍스트가 "잉크 괴물" 의 텍스트인지 표시.
        // BattleSceneMode 의 호흡/불꽃 효과 대상 TMP 를 결정한다.
        public bool IsMonster = false;
    }

    // 전투 씬으로 핸드오프할 페이로드(슬롯/SlotId/MonsterTMP) 를 셋업하는 이벤트.
    [Serializable]
    public class BattleEventRecord : EventRecord
    {
    }

    // 카드 인벤토리에 카드를 추가하는 이벤트.
    [Serializable]
    public class CardGrantEventRecord : EventRecord
    {
        public int[] CardIds = null;
    }
}
