using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputerUI : MonoBehaviour
{

    [SerializeField] private GameObject content;
    [SerializeField] private GameObject hackerMinigame;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failedPanel;
    [SerializeField] private HackerMinigame hackerMinigameScript;
    [SerializeField] private Computer computer;

    private void OnEnable() {
        hackerMinigameScript.OnSuccess += HackerSuccess;
        hackerMinigameScript.OnFailed += HackerFailed;
    }

    private void OnDisable() {
        hackerMinigameScript.OnSuccess -= HackerSuccess;
        hackerMinigameScript.OnFailed -= HackerFailed;
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
        failedPanel.SetActive(false);
    }

    public void AccessComputer() {
        content.SetActive(false);
        hackerMinigame.SetActive(true);
        successPanel.SetActive(false);
        failedPanel.SetActive(false);
    }

    public void ShowSuccessPanel() {
        content.SetActive(false);
        hackerMinigame.SetActive(false);
        successPanel.SetActive(true);
        failedPanel.SetActive(false);
    }

    public void ShowFailedPanel() {
        content.SetActive(false);
        hackerMinigame.SetActive(false);
        failedPanel.SetActive(true);
        successPanel.SetActive(false);
    }

    public void HackerSuccess() { 
        computer.CompleteComputer();
        ShowSuccessPanel();
    }

    private void HackerFailed() { 
        ShowFailedPanel();
    }

     public void ReturnToContent() {
        failedPanel.SetActive(false);
        content.SetActive(true);

        hackerMinigameScript.ResetMinigame();
    }

    public void ExitComputer() {
        computer.ExitComputer();
    }

}
