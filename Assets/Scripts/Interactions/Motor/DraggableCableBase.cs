using UnityEngine;
using UnityEngine.EventSystems;

public abstract class DraggableCableBase :
    MonoBehaviour,
    IDragHandler,
    IEndDragHandler {
    [SerializeField] protected RectTransform cableRect;
    [SerializeField] protected RectTransform connectionZone;

    public void OnDrag(PointerEventData eventData) {
        cableRect.anchoredPosition += eventData.delta;
    }

    public abstract void OnEndDrag(PointerEventData eventData);

    protected bool IsInsideConnectionZone() {
        Vector2 cableScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                null,
                cableRect.position
            );

        return RectTransformUtility.RectangleContainsScreenPoint(
            connectionZone,
            cableScreenPosition,
            null
        );
    }
}