using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableBrokenCable : DraggableCableBase {
    [SerializeField] private CableRepairMinigame cableRepairMinigame;
    [SerializeField] private Vector2 removedPosition;

    private Vector2 startPosition;

    private void Awake() {
        startPosition = cableRect.anchoredPosition;
    }

    public override void OnDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanDragBrokenCable())
            return;

        base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanDragBrokenCable())
            return;

        if (cableRepairMinigame.CanRemoveBrokenCable()) {
            if (!IsInsideConnectionZone()) {
                cableRepairMinigame.BrokenCableRemoved();
            } else {
                cableRect.anchoredPosition = startPosition;
            }
        }
    }

    public void RestorePosition(bool removed) {
        cableRect.anchoredPosition =
            removed ? removedPosition : startPosition;
    }
}
