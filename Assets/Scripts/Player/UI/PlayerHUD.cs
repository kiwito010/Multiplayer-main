using UnityEngine;

public class PlayerHUD : MonoBehaviour {
    // VARIABLES:

    // Referencias:
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private ScanUI scanUI;
    [SerializeField] private CableRepairMinigame cableRepairMinigame;
    [SerializeField] private ComputerUI computerUI;

    // MÉTODOS:

    public void Initialize(
        PlayerInventory playerInventory, 
        NetworkInventoryState networkInventory, 
        PlayerDiagnostic playerDiagnostic, 
        PlayerManager playerManager, 
        GameInput gameInput
        ) {
        if (inventoryUI != null) {
            inventoryUI.Initialize(playerInventory, networkInventory);
        }

        if (playerDiagnostic != null && scanUI != null) {
            playerDiagnostic.Initialize(scanUI);
        }

        if (cableRepairMinigame != null) {
            cableRepairMinigame.Initialize(playerManager, gameInput);
        }

        if (computerUI != null) {
            computerUI.InitializeNetwork(playerManager, gameInput);
        }
    }

    public void OpenCableRepair(RepairableComponent component, PlayerInventory inventory) {
        if (cableRepairMinigame == null)
            return;

        if (cableRepairMinigame.IsOpen())
            return;

        cableRepairMinigame.Open(component, inventory);
    }

    public ComputerUI GetComputerUI() {
        return computerUI;
    }
}
