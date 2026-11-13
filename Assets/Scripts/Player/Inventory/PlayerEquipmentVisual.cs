using Fusion;
using UnityEngine;

public class PlayerEquipmentVisual : MonoBehaviour {
    // VARIABLES:

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private NetworkInventoryState networkInventory;

    [Header("Puntos de equipamiento")]
    [SerializeField] private Transform firstPersonItemHolder;
    [SerializeField] private Transform thirdPersonItemHolder;

    private GameObject equippedVisual;

    private int lastSelectedSlot = -1;
    private NetworkId lastItemId;

    // MÉTODOS:

    private void Update() {
        if (networkInventory == null ||
            firstPersonItemHolder == null ||
            thirdPersonItemHolder == null)
            return;

        if (networkInventory.Object == null || !networkInventory.Object.IsValid)
            return;

        int selectedSlot;

        if (networkInventory.Object.HasInputAuthority) {
            if (playerInventory == null)
                return;

            selectedSlot = playerInventory.GetSelectedSlot();
        } else {
            selectedSlot = networkInventory.EquippedSlot;
        }

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

        GameObject visualPrefab = pickableItem.GetEquippedVisualPrefab();

        if (visualPrefab == null)
            return;

        bool isLocalPlayer = networkInventory.Object.HasInputAuthority;

        Transform targetHolder = isLocalPlayer ? firstPersonItemHolder : thirdPersonItemHolder;

        equippedVisual = Instantiate(visualPrefab, targetHolder);

        equippedVisual.SetActive(true);

        EquippedItemOffsets offsets = equippedVisual.GetComponent<EquippedItemOffsets>();

        if (offsets != null) {
            offsets.ApplyOffset(isLocalPlayer);
        } else {
            equippedVisual.transform.localPosition = Vector3.zero;
            equippedVisual.transform.localRotation = Quaternion.identity;
        }

        // Evitamos que el visual equipado interfiera con el raycast.
        foreach (Collider collider in equippedVisual.GetComponentsInChildren<Collider>(true)) {
            collider.enabled = false;
        }
    }
}
