using UnityEngine;

public class PlayerHUD : MonoBehaviour {
    // VARIABLES:

    // Referencias:
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private ScanUI scanUI;

    // MÉTODOS:

    public void Initialize(PlayerInventory playerInventory, NetworkInventoryState networkInventory, PlayerDiagnostic playerDiagnostic) {
        if (inventoryUI != null) {
            inventoryUI.Initialize(playerInventory, networkInventory);
        }

        if (playerDiagnostic != null && scanUI != null) {
            playerDiagnostic.Initialize(scanUI);
        }
    }

}
