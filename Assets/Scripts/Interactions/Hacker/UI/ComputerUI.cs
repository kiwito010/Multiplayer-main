using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputerUI : MonoBehaviour
{
    [SerializeField] private HackerTypingText hackerTypingText;
    [SerializeField] private Computer computer;
    private NetworkComputer networkComputer;
    private PlayerManager networkPlayerManager;
    private GameInput networkGameInput;

    [SerializeField] private GameObject content;
    [SerializeField] private GameObject hackerMinigame;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failurePanel;

    private void OnEnable() {
        hackerTypingText.OnSuccess += HackerSuccess;
        hackerTypingText.OnFailure += HackerFailure;
    }

    private void OnDisable() {
        hackerTypingText.OnSuccess -= HackerSuccess;
        hackerTypingText.OnFailure -= HackerFailure;
    }

    public void InitializeNetwork(PlayerManager playerManager, GameInput gameInput) {
        networkPlayerManager = playerManager;
        networkGameInput = gameInput;
    }

    public void SetComputer(Computer newComputer) {
        computer = newComputer;
    }

    private void Update() {
        if (networkComputer == null || networkGameInput == null)
            return;

        if (networkGameInput.GetExitPressed()) {
            ExitNetworkComputer();
        }
    }

    public void Show() { 
        gameObject.SetActive(true);

        if (computer.IsCompleted()) {
            ShowSuccessPanel();
        } else {
            ShowContent();
        }
    }
    public void Hide() {
        gameObject.SetActive(false);
    }

    public void ShowContent() {

        content.SetActive(true);
        hackerMinigame.SetActive(false);
        successPanel.SetActive(false);
        failurePanel.SetActive(false);
    }

    public void AccessComputer() {
        content.SetActive(false);
        hackerMinigame.SetActive(true);
        successPanel.SetActive(false);
        failurePanel.SetActive(false);    
        hackerTypingText.StartMinigame();
    }

    public void ShowSuccessPanel() {
        content.SetActive(false);
        hackerMinigame.SetActive(false);
        successPanel.SetActive(true);
        failurePanel.SetActive(false);
    }

    public void ShowFailurePanel() {
        content.SetActive(false);
        hackerMinigame.SetActive(false);
        successPanel.SetActive(false);
        failurePanel.SetActive(true);
    }

    public void ShowNetwork(NetworkComputer computerToUse) {
        networkComputer = computerToUse;

        if (networkPlayerManager != null)
            networkPlayerManager.EnterUIMode();

        gameObject.SetActive(true);
        ShowContent();
    }

    private void HackerSuccess() { 
        computer.CompleteComputer();
        ShowSuccessPanel();
    }

    private void HackerFailure() { 
        ShowFailurePanel();

        Invoke(nameof(ShutdownComputer), 2f);
    }

    private void ShutdownComputer() {
        computer.PowerOff();
    }

    public void ReturnToContent() {
        content.SetActive(true);

        hackerTypingText.ResetMinigame();
    }

    public void ExitComputer() {
        if (networkComputer != null) {
            ExitNetworkComputer();
            return;
        }

        if (computer != null)
            computer.ExitComputer();
    }

    private void ExitNetworkComputer() {
        if (networkComputer == null)
            return;

        networkComputer.RequestExit();
        networkComputer = null;

        if (networkPlayerManager != null)
            networkPlayerManager.ExitUIMode();

        Hide();
    }
}
