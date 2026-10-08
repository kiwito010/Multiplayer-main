using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputerUI : MonoBehaviour
{

    [SerializeField] private GameObject content;
    [SerializeField] private GameObject hackerMinigame;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failurePanel;
    [SerializeField] private HackerTypingText hackerTypingText;
    [SerializeField] private Computer computer;

    private void OnEnable() {
        hackerTypingText.OnSuccess += HackerSuccess;
        hackerTypingText.OnFailure += HackerFailure;
    }

    private void OnDisable() {
        hackerTypingText.OnSuccess -= HackerSuccess;
        hackerTypingText.OnFailure -= HackerFailure;

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
        computer.ExitComputer();
    }

}
