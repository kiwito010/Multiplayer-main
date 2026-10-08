using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class HackerTypingText : MonoBehaviour {
    //VARIABLES:

    //Referencia a otros scripts

    [SerializeField] private Computer computer;

    //Variable Phase
    private int currentPhase;

    //CodeTexts
    [SerializeField] private TextMeshProUGUI CodeText;

    [TextArea]
    [SerializeField] private string phase1Text;

    [TextArea]
    [SerializeField] private string phase2Text;

    [TextArea]
    [SerializeField] private string phase3Text;

        //(la speed, la tecla q hay q presionar, el intervalo de caracteres)

    [SerializeField] private float typingSpeed = 0.04f;

    private bool waitingForKey;
    private Key expectedKey;

    [SerializeField] private int charactersBetweenQTE = 20;

    private int charactersSinceLastQTE;

        //circulo de tiempo

    [SerializeField] private RectTransform timeCircle;

    [SerializeField] private float keyTime = 1.5f;

    [SerializeField] private RectTransform qteContainer;

    private float keyTimer;

        //fails
    [SerializeField] private int maxFails = 3;

    private int currentFails;

    private string textBeforeQTE;
    private char currentQTECharacter;

    //EVENTOS:
    public event System.Action OnSuccess;
    public event System.Action OnFailure;

    //progress and errors
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI errorsText;

    private int typedCharacters;
    private int totalCharacters;

    private void Update() {
        if (!waitingForKey)
            return;

        keyTimer -= Time.deltaTime;

        float timerNormalized = keyTimer / keyTime;

        timeCircle.localScale = Vector3.one * timerNormalized;

        if (keyTimer <= 0f) {
            FailQTE();
            return;
        }

        if (Keyboard.current[expectedKey].wasPressedThisFrame) {
            CodeText.text = textBeforeQTE + "<color=green>" + currentQTECharacter + "</color>";
            
            waitingForKey = false;

            qteContainer.gameObject.SetActive(false);

            Debug.Log("Tecla correcta");
        } else if (Keyboard.current.anyKey.wasPressedThisFrame) {
            FailQTE();
        }
    }

    private IEnumerator TypeText(string textToType) {
        CodeText.text = "";

        charactersSinceLastQTE = 0;

        for (int i = 0; i < textToType.Length; i++) { 
            char currentCharacter = textToType[i];

            bool canBeQTEKey = TryGetKeyFromChar(currentCharacter, out Key key);

            if (charactersSinceLastQTE >= charactersBetweenQTE && canBeQTEKey) {

                expectedKey = key;
                waitingForKey = true;

                keyTimer = keyTime;

                textBeforeQTE = CodeText.text;
                currentQTECharacter = currentCharacter;

                CodeText.text += "<color=yellow>" + currentCharacter + "</color>";

                CodeText.ForceMeshUpdate();

                int characterIndex = CodeText.textInfo.characterCount - 1;

                TMP_CharacterInfo characterInfo = CodeText.textInfo.characterInfo[characterIndex];

                Vector3 characterCenter = (characterInfo.bottomLeft + characterInfo.topRight) / 2f;

                qteContainer.localPosition = characterCenter;

                timeCircle.localScale = Vector3.one;

                qteContainer.gameObject.SetActive(true);

                Debug.Log("Esperando tecla " + expectedKey);

                charactersSinceLastQTE = 0;

                yield return new WaitUntil(() => !waitingForKey);

            } else {
                CodeText.text += currentCharacter;
                charactersSinceLastQTE++;
            }

            typedCharacters++;
            UpdateProgress();

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private bool TryGetKeyFromChar(char character, out Key key) {
        switch (char.ToUpper(character)) {
            case 'W':
                key = Key.W;
                return true;

            case 'A':
                key = Key.A;
                return true;

            case 'S':
                key = Key.S;
                return true;

            case 'D':
                key = Key.D;
                return true;

            case 'Q':
                key = Key.Q;
                return true;

            case 'E':
                key = Key.E;
                return true;

            case 'Z':
                key = Key.Z;
                return true;

            case 'X':
                key = Key.X;
                return true;

            case 'C':
                key = Key.C;
                return true;

            case 'V':
                key = Key.V;
                return true;

            case 'F':
                key = Key.F;
                return true;

            case 'R':
                key = Key.R;
                return true;

            default:
                key = Key.None;
                return false;
        }
    }

    private void FailQTE() {

        CodeText.text = textBeforeQTE + "<color=red>" + currentQTECharacter + "</color>";
        
        waitingForKey = false;

        qteContainer.gameObject.SetActive(false);

        currentFails++;
        UpdateErrorsText();

        Debug.Log("Fallo " + currentFails + "/" + maxFails);

        if (currentFails >= maxFails) {
            Debug.Log("Demasiados fallos");

            OnFailure?.Invoke();
        }
    }

    public void ResetMinigame() { 
        StopAllCoroutines();

        waitingForKey = false;

        currentFails = 0;
        UpdateErrorsText();

        charactersSinceLastQTE = 0;

        typedCharacters = 0;
        UpdateProgress();

        keyTimer = 0f;

        CodeText.text = "";

        qteContainer.gameObject.SetActive(false);
    }

    public void StartMinigame() {
        totalCharacters =
            phase1Text.Length +
            phase2Text.Length +
            phase3Text.Length;

        ResetMinigame();

        StartCoroutine(RunMinigame());
    }

    private IEnumerator RunMinigame() { 
        currentPhase = 1;
        SetDifficulty(currentPhase);
        yield return StartCoroutine(TypeText(phase1Text));

        currentPhase = 2;
        SetDifficulty(currentPhase);

        yield return StartCoroutine(TypeText(phase2Text));

        currentPhase = 3;
        SetDifficulty(currentPhase);
        yield return StartCoroutine(TypeText(phase3Text));

        OnSuccess?.Invoke();
    }

    private void SetDifficulty(int phase) {
        if (phase == 1) {
            typingSpeed = 0.05f;
            charactersBetweenQTE = 20;
            keyTime = 1.8f;
        } else if (phase == 2) {
            typingSpeed = 0.04f;
            charactersBetweenQTE = 15;
            keyTime = 1.4f;
        } else if (phase == 3) {
            typingSpeed = 0.03f;
            charactersBetweenQTE = 10;
            keyTime = 1f;
        }
    }

    private void UpdateProgress() { 
        float progress = (float) typedCharacters / totalCharacters;

        int percentage = Mathf.RoundToInt(progress * 100f);

        progressText.text = "PROGRESS " + percentage + "%";
    }

    private void UpdateErrorsText() {
        errorsText.text = "ERRORS " + currentFails + "/" + maxFails;
    }
}
