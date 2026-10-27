using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableBrokenCable : DraggableCableBase {
    [SerializeField] private CableRepairMinigame cableRepairMinigame;

    private Vector2 startPosition;

    private void Awake() {
        startPosition = cableRect.anchoredPosition;
    }

    public override void OnDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;

        base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData) {
        if (!cableRepairMinigame.CanRemoveBrokenCable())
            return;

        if (!IsInsideConnectionZone()) {
            cableRepairMinigame.BrokenCableRemoved();
        } else {
            cableRect.anchoredPosition = startPosition;
        }
    }
}