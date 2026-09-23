using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HackerMinigame : MonoBehaviour {
    [SerializeField] private string[] texts;
    private string currentText;
    [SerializeField] private TMP_Text textDisplay;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private ComputerUI computerUI;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameInput gameInput;

    private int completedTexts;
    private const int maxTexts = 3;

    private int attempts;
    private const int maxAttemtps = 3;

    private List<int> usedTextIndexes = new List<int>();

    public event System.Action OnSuccess;
    public event System.Action OnFailed;

    private void Start() {
        LoadRandomText();

        errorText.gameObject.SetActive(false);

        codeInput.onValueChanged.AddListener((inputText) => {
            codeInput.text = inputText.ToUpper();
        });

        string code = GetCode();

        Debug.Log(code);

        UpdateAttemptsText();
        UpdateProgressText();

        codeInput.onValueChanged.AddListener(OnCodeInputChanged);

    }

    private void Update() {
        if (gameInput.GetConfirmPressed()) { 
            ConfirmCode();
        }
    }

    private void LoadRandomText() {
        int randomIndex;

        do {
            randomIndex = Random.Range(0, texts.Length);
        } while (usedTextIndexes.Contains(randomIndex));

        usedTextIndexes.Add(randomIndex);

        currentText = texts[randomIndex];
        textDisplay.text = currentText;
    }

    private string GetCode() {
        string code = "";

        foreach (char character in currentText) {
            if (char.IsUpper(character) || char.IsDigit(character)) {
                code += character;
            }
        }
        return code;
    }

    private string GetPlayerCode() {
        return codeInput.text;
    }

    public void ConfirmCode() {
        string correctCode = GetCode();
        string playerCode = GetPlayerCode();

        if (playerCode == correctCode) {
            completedTexts++;

            UpdateProgressText();

            codeInput.text = "";
            errorText.gameObject.SetActive(false);

            if (completedTexts >= maxTexts) {
                OnSuccess?.Invoke();
            } else { 
                LoadRandomText();
            }

        } else {
            attempts++;

            UpdateAttemptsText();

            if (attempts >= maxAttemtps) {

                errorText.gameObject.SetActive(false);

                OnFailed?.Invoke();
            } else {
                codeInput.text = "";
                errorText.gameObject.SetActive(true);
            }
        }
    }

    public void ResetMinigame() {
        attempts = 0;
        completedTexts = 0;
        
        usedTextIndexes.Clear();

        codeInput.text = "";

        errorText.gameObject.SetActive(false);

        UpdateAttemptsText();
        UpdateProgressText();
        LoadRandomText();
    }

    private void UpdateAttemptsText() {
        attemptsText.text = "Intentos: " + attempts + "/" + maxAttemtps;
    }

    private void UpdateProgressText() { 
        progressText.text = "Progreso " + completedTexts + "/" + maxTexts;
    }

    private void OnCodeInputChanged(string value) {
        errorText.gameObject.SetActive(false);
    }
}
