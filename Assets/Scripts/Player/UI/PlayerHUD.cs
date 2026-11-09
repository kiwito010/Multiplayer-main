using UnityEngine;

public class PlayerHUD : MonoBehaviour {
    // VARIABLES:

    // Referencias:
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private ScanUI scanUI;
    [SerializeField] private CableRepairMinigame cableRepairMinigame;
    [SerializeField] private ComputerUI computerUI;
    [SerializeField] private InteractionPromptUI interactionPromptUI;

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

        if (interactionPromptUI != null && networkInventory != null) {

            PlayerNetworkInteraction interaction = networkInventory.GetComponent<PlayerNetworkInteraction>();

            interactionPromptUI.Initialize(interaction);
        }
    }

    public void OpenCableRepair(
     RepairableComponent component,
     PlayerInventory inventory) {
        Debug.Log("[REPAIR DEBUG] Solicitud de apertura");

        if (cableRepairMinigame == null) {
            Debug.LogWarning("[REPAIR DEBUG] Minijuego no asignado");
            return;
        }

        if (cableRepairMinigame.IsOpen()) {
            Debug.LogWarning("[REPAIR DEBUG] Minijuego sigue abierto");
            return;
        }

        Debug.Log("[REPAIR DEBUG] Abriendo minijuego");

        cableRepairMinigame.Open(component, inventory);
    }

    public ComputerUI GetComputerUI() {
        return computerUI;
    }
}
