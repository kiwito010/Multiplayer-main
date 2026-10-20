using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableBrokenCable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private CableRepairMinigame cableRepairMinigame;
    [SerializeField] private RectTransform cableRect;
    [SerializeField] private RectTransform connectionZone;

    private Vector2 startPosition;

    private void Awake() {
        startPosition = cableRect.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        Debug.Log("BEGIN DRAG");

        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;
    }

    public void OnDrag(PointerEventData eventData) {
        Debug.Log("DRAG");

        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;

        cableRect.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;

        Vector2 cableScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                null,
                cableRect.position
            );

        bool stillInConnectionZone =
            RectTransformUtility.RectangleContainsScreenPoint(
                connectionZone,
                cableScreenPosition,
                null
            );

        if (!stillInConnectionZone) {
            cableRepairMinigame.BrokenCableRemoved();
        } else {
            cableRect.anchoredPosition = startPosition;
        }
    }
}