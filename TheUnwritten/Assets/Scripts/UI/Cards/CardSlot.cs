using UnityEngine;
using UnityEngine.EventSystems;

using Common;
using Tables.Records;
using UI.Slots;

namespace UI.Cards
{
    public class CardSlot : Slot<CardSlot.Param>,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        public class Param : ElementParam
        {
            public CardRecord CardRecord { get; private set; } = null;

            public Param(CardRecord cardRecord)
            {
                CardRecord = cardRecord;
            }
        }
        
        [SerializeField] private RectTransform rectTr = null;
    
        private CardHover _hover;

        public RectTransform Rect => rectTr;
        public bool IsSelectable { get; private set; }
        
        public override void Initialize(Param param)
        {
            base.Initialize(param);
            
            _hover = GetComponent<CardHover>();
        }

        public void SetSelectable(bool value)
        {
            IsSelectable = value;

            if (_hover != null)
                _hover.IsSelectable = value;

            if (!value && _hover != null && _hover.IsHovering)
                _hover.ForceExit();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsSelectable) return;
            _hover?.Enter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover?.Exit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsSelectable) return;

            Debug.Log($"카드 클릭: {name}");

            // TODO: 카드 사용 로직 연결
        }
    }
}
