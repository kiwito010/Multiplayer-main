using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ClimbableWall climbableWall;
    [SerializeField] private ClimbAction climbAction;

    public void Interact() { 
    }

    public ClimbableWall GetClimbableWall() { 
        return climbableWall;
    }

    public ClimbAction GetClimbAction() { 
        return climbAction;
    }
}
