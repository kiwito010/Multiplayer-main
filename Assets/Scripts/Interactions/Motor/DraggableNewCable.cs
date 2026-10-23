using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableNewCable : DraggableCableBase {
    [SerializeField] private CableRepairMinigame cableRepairMinigame;

    public override void OnEndDrag(PointerEventData eventData) {
        if (!IsInsideConnectionZone())
            return;

        if (!cableRepairMinigame.CanPlaceNewCable())
            return;

        cableRect.position = connectionZone.position;

        cableRepairMinigame.NewCablePlaced();
    }
}