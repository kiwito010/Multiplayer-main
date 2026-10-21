using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableTape : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler {
    [SerializeField] private CableRepairMinigame cableRepairMinigame;
    [SerializeField] private RectTransform tapeRect;
     
    public void OnBeginDrag(PointerEventData eventData) {
        cableRepairMinigame.StartWrapping(tapeRect.position);
    }

    public void OnDrag(PointerEventData eventData) {
        tapeRect.anchoredPosition += eventData.delta;

        cableRepairMinigame.UpdateWrapping(tapeRect.position);
    }

    public void OnEndDrag(PointerEventData eventData) {
        cableRepairMinigame.StopWrapping();
    }
}