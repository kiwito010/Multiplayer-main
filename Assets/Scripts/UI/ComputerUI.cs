using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputerUI : MonoBehaviour
{

    [SerializeField] private GameObject content;
    [SerializeField] private GameObject hackerMinigame;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private HackerTypingText hackerTypingText;
    [SerializeField] private Computer computer;

    private void OnEnable() {
        hackerTypingText.OnSuccess += HackerSuccess;
    }

    private void OnDisable() {
        hackerTypingText.OnSuccess -= HackerSuccess;
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
    }

    public void AccessComputer() {
        content.SetActive(false);
        hackerMinigame.SetActive(true);
        successPanel.SetActive(false);

        hackerTypingText.StartMinigame();
    }

    public void ShowSuccessPanel() {
        content.SetActive(false);
        hackerMinigame.SetActive(false);
        successPanel.SetActive(true);
    }

    public void HackerSuccess() { 
        computer.CompleteComputer();
        ShowSuccessPanel();
    }

     public void ReturnToContent() {
        content.SetActive(true);

        hackerTypingText.ResetMinigame();
    }

    public void ExitComputer() {
        computer.ExitComputer();
    }

}
