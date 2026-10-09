using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : MonoBehaviour, IInteractable {

    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private ComputerUI computerUI;
    [SerializeField] private HackerTypingText hackerTypingText;
    [SerializeField] private StaminaUI staminaUI;
    [SerializeField] private PlayerManager playerManager;

    private bool isUsing;
    private bool isCompleted;

    private bool isPoweredOn;

    private void Awake() {
        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void PlayerInteraction_OnInteract(object sender, InteractEventArgs e) {
        if (e.interactable == this) { 
            Interact();
        }
    }

    public void Interact() {

        if(!isPoweredOn)
            return;

        if (isUsing)
            return;

        isUsing = true;

        playerManager.EnterUIMode();
        staminaUI.Hide();
        computerUI.Show();

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

        playerManager.ExitUIMode();
        computerUI.Hide();

        staminaUI.EnableNormalVisibility();

        Debug.Log("Saliendo de la computadora");
    }

    public void PowerOn() {
        if (isPoweredOn)
            return;

        isPoweredOn = true;
        Debug.Log("Computer encendida");
    }

    public void PowerOff() { 
        isPoweredOn = false;

        hackerTypingText.ResetMinigame();

        ExitComputer();
        Debug.Log("Computer apagada");
    }

    public void Update() {
        if (gameInput.GetExitComputerPressed()) { 
            ExitComputer();
        }
    }
}
