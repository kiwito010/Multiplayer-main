using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplorerAbilities : MonoBehaviour
{
    private PlayerInteraction playerInteraction;
    private GameInput gameInput;
    private PlayerMovement playerMovement;
    private bool isProne;

    private void Awake() {
        playerInteraction = GetComponent<PlayerInteraction>();
        gameInput = GetComponent<GameInput>();
        playerMovement = GetComponent<PlayerMovement>();

        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void PlayerInteraction_OnInteract(object sender, InteractEventArgs e) {
        if (e.interactable is ClimbInteractable climbInteractable) { 
            ClimbableWall climbableWall = climbInteractable.GetClimbableWall();

            if (climbInteractable.GetClimbAction() == ClimbAction.Climb) { 
                Climb(climbableWall);
            } 
            else if (climbInteractable.GetClimbAction() == ClimbAction.Drop) {
                 Drop(climbableWall);
            }
        }
    }

    private void Climb(ClimbableWall climbableWall) { 
        transform.position = climbableWall.GetTopDestination();
    }

    private void Drop(ClimbableWall climbableWall) {
        transform.position = climbableWall.GetBottomDestination();
    }

    private void Update() {
        if (gameInput.GetPronePressed()) {
            if (playerMovement.IsProne()) {
                playerMovement.ExitProne();
            } else { 
                playerMovement.EnterProne();
            }
        }
    }
}
