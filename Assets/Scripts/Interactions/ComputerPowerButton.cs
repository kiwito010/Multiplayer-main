using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputerPowerButton : MonoBehaviour, IInteractable
{
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Computer computer;

    private void OnEnable() {
        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void OnDisable() { 
        playerInteraction.OnInteract -= PlayerInteraction_OnInteract;
    }

    private void PlayerInteraction_OnInteract(object sender, InteractEventArgs e) {
        if (e.interactable == this) {
            computer.PowerOn();
        }
    }

    public void Interact() {

    }
}
