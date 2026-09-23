using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : MonoBehaviour, IInteractable {

    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private ComputerUI computerUI;

    private bool isUsing;
    private bool isCompleted;

    private void Awake() {
        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void PlayerInteraction_OnInteract(object sender, InteractEventArgs e) {
        if (e.interactable == this) { 
            Interact();
        }
    }

    private void EnterComputerMode() { 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ExitComputerMode() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Interact() {
        if (isUsing)
            return;

        isUsing = true;

        gameInput.DisableMovement();

        computerUI.Show();

        EnterComputerMode();

        Debug.Log("Entrando a la computadora");
    }

    public void CompleteComputer() {
        isCompleted = true;
    }

    public bool IsCompleted() { 
        return isCompleted;
    }

    public void ExitComputer() { 
        if(!isUsing) 
            return;

        isUsing = false;

        gameInput.EnableMovement();

        computerUI.Hide();

        ExitComputerMode();

        Debug.Log("Saliendo de la computadora");
    }

    public void Update() {
        if (gameInput.GetExitComputerPressed()) { 
            ExitComputer();
        }
    }
}
