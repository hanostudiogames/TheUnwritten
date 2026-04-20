using UnityEngine;
using UnityEngine.EventSystems;

public class CardSlot : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private CardHover _hover;

    public bool IsSelectable { get; private set; }

    void Awake()
    {
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