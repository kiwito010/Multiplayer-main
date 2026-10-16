using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnifyingGlassPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private EngineerDiagnostic engineerDiagnostic;

    private void OnEnable() {
        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void OnDisable() {
        playerInteraction.OnInteract -= PlayerInteraction_OnInteract;
    }

    private void PlayerInteraction_OnInteract(object sender, InteractEventArgs e) {
        if (e.interactable == this) {
            Interact();
        }
    }

    public void Interact() {
        engineerDiagnostic.GiveMagnifyingGlass();
        gameObject.SetActive(false);
    }
}
