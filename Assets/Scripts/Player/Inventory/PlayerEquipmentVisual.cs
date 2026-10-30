
using Fusion;
using UnityEngine;

public class PlayerEquipmentVisual : MonoBehaviour {
    // VARIABLES:

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private NetworkInventoryState networkInventory;
    [SerializeField] private Transform handPoint;

    private GameObject equippedVisual;

    private int lastSelectedSlot = -1;
    private NetworkId lastItemId;

    // MÉTODOS:

    private void Update() {
        if (playerInventory == null || networkInventory == null || handPoint == null)
            return;

        if (networkInventory.Object == null || !networkInventory.Object.IsValid)
            return;

        if (!networkInventory.Object.HasInputAuthority)
            return;

        int selectedSlot = playerInventory.GetSelectedSlot();

        NetworkId itemId = selectedSlot >= 0 ? networkInventory.GetItemIdInSlot(selectedSlot) : default(NetworkId);

        if (selectedSlot == lastSelectedSlot && itemId == lastItemId) {
            if (itemId == default(NetworkId) || equippedVisual != null)
                return;
        }

        lastSelectedSlot = selectedSlot;
        lastItemId = itemId;

        UpdateEquippedVisual(itemId);
    }

    private void UpdateEquippedVisual(NetworkId itemId) {
        if (equippedVisual != null) {
            Destroy(equippedVisual);
            equippedVisual = null;
        }

        if (itemId == default(NetworkId))
            return;

        if (!networkInventory.Runner.TryFindObject(itemId, out NetworkObject itemObject))
            return;

        NetworkPickableItem pickableItem = itemObject.GetComponent<NetworkPickableItem>();

        if (pickableItem == null)
            return;

        GameObject visualPrefab = pickableItem.GetItemVisual();

        if (visualPrefab == null)
            return;

        equippedVisual = Instantiate(visualPrefab, handPoint);

        equippedVisual.SetActive(true);

        equippedVisual.transform.localPosition = Vector3.zero;
        equippedVisual.transform.localRotation = Quaternion.identity;

        // Evitamos que el visual equipado interfiera con el raycast.
        foreach (Collider collider in equippedVisual.GetComponentsInChildren<Collider>(true)) {
            collider.enabled = false;
        }
    }
}
