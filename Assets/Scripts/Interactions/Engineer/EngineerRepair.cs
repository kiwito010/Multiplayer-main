using UnityEngine;

public class EngineerRepair : MonoBehaviour {
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerInventory playerInventory;

    private void OnEnable() {
        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void OnDisable() {
        playerInteraction.OnInteract -= PlayerInteraction_OnInteract;
    }

    private void PlayerInteraction_OnInteract(object sender, InteractEventArgs e) {
        if (e.interactable is RepairableComponent repairableComponent) {
            repairableComponent.OpenRepairMinigame(playerInventory);
        }
    }
}