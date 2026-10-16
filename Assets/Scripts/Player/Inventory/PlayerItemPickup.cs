using UnityEngine;

public class PlayerItemPickup : MonoBehaviour {
    [SerializeField] private PlayerInteraction playerInteraction;

    private void OnEnable() {
        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void OnDisable() {
        playerInteraction.OnInteract -= PlayerInteraction_OnInteract;
    }

    private void PlayerInteraction_OnInteract(object sender, InteractEventArgs e) {
        if (e.interactable is PickableItem pickableItem) {
            pickableItem.TryPickup(e.playerInventory);
        }
    }
}