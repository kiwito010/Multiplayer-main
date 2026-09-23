using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInput : MonoBehaviour
{
    private PlayerInputActions inputActions;
 
    private void Awake() {
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
    }
    public void EnableMovement() {
        inputActions.Player.Move.Enable();
        inputActions.Player.Look.Enable();
    }
    public void DisableMovement() {
        inputActions.Player.Move.Disable();
        inputActions.Player.Look.Disable();
    }

    public Vector2 GetMovementVector(){
        return inputActions.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 GetLookVector() {
        return inputActions.Player.Look.ReadValue<Vector2>();
    }

    public bool GetInteractPressed() {
        return inputActions.Player.Interact.WasPressedThisFrame();
    }

    public bool GetExitComputerPressed() { 
        return inputActions.Player.ExitComputer.WasPressedThisFrame();
    }

    public bool GetConfirmPressed() {
        return inputActions.Player.Confirm.WasPressedThisFrame();
    }

    public bool GetJumpPressed() { 
        return inputActions.Player.Jump.WasPressedThisFrame();
    }

    public bool GetCrouchPressed() {
        return inputActions.Player.Crouch.WasPressedThisFrame();
    }

    public bool GetPronePressed() {
        return inputActions.Player.Prone.WasPressedThisFrame();
    }
}
