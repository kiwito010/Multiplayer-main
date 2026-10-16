using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractEventArgs : EventArgs
{

    public IInteractable interactable;
    public PlayerInventory playerInventory;

    public InteractEventArgs (IInteractable interactable, PlayerInventory playerInventory) {
        this.interactable = interactable;
        this.playerInventory = playerInventory;
    }
}
