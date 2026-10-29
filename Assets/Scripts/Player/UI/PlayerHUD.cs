using UnityEngine;

public class PlayerHUD : MonoBehaviour {
    // VARIABLES:

    // Referencias:
    [SerializeField] private InventoryUI inventoryUI;

    // MÉTODOS:

    public void Initialize(PlayerInventory playerInventory, NetworkInventoryState networkInventory) {
        if (inventoryUI != null) {
            inventoryUI.Initialize(playerInventory, networkInventory);
        }
    }
}
