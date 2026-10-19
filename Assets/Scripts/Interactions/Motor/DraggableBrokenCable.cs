using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableBrokenCable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private CableRepairMinigame cableRepairMinigame;
    [SerializeField] private RectTransform cableRect;
    [SerializeField] private RectTransform removeZone;

    private Vector2 startPosition;

    private void Awake() {
        startPosition = cableRect.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;
    }

    public void OnDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;

        cableRect.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;

        if (RectTransformUtility.RectangleContainsScreenPoint(
            removeZone,
            eventData.position,
            eventData.pressEventCamera)) {
            cableRepairMinigame.BrokenCableRemoved();
        } else {
            cableRect.anchoredPosition = startPosition;
        }
    }
}