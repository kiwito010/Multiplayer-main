using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractEventArgs : EventArgs
{

   public IInteractable interactable;

    public InteractEventArgs (IInteractable interactable) {
        this.interactable = interactable;
    }
}
