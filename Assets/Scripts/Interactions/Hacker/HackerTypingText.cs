using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class HackerTypingText : MonoBehaviour {
    //VARIABLES:

    //Referencia a otros scripts

    [SerializeField] private Computer computer;

        //CodeText
            //(el texto, la speed, la tecla q hay q presionar, el intervalo de caracteres)

    [SerializeField] private TextMeshProUGUI CodeText;

    [TextArea]
    [SerializeField] private string fullText;

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

    public event System.Action OnSuccess;
    public event System.Action OnFailed;
    private void Start() {
        StartCoroutine(TypeText());
    }

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

    private IEnumerator TypeText() {
        CodeText.text = "";
        charactersSinceLastQTE = 0;
        currentFails = 0;

        for (int i = 0; i < fullText.Length; i++) { 
            char currentCharacter = fullText[i];

            bool canBeQTEKey = TryGetKeyFromChar(currentCharacter, out Key key);

            if (charactersSinceLastQTE >= charactersBetweenQTE && canBeQTEKey) {

                expectedKey = key;
                waitingForKey = true;

                timeCircle.gameObject.SetActive(false);

                keyTimer = keyTime;
                timeCircle.gameObject.SetActive(true);
                timeCircle.localScale = Vector3.one;

                textBeforeQTE = CodeText.text;
                currentQTECharacter = currentCharacter;

                CodeText.text += "<color=yellow>" + currentCharacter + "</color>";

                CodeText.ForceMeshUpdate();

                int characterIndex = CodeText.textInfo.characterCount - 1;

                TMP_CharacterInfo characterInfo = CodeText.textInfo.characterInfo[characterIndex];

                Vector3 characterCenter = (characterInfo.bottomLeft + characterInfo.topRight) / 2f;

                qteContainer.localPosition = characterCenter;

                qteContainer.gameObject.SetActive(true);

                Debug.Log("Esperando tecla " + expectedKey);

                charactersSinceLastQTE = 0;

                yield return new WaitUntil(() => !waitingForKey);

            } else {
                CodeText.text += currentCharacter;
                charactersSinceLastQTE++;
            }

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

            default:
                key = Key.None;
                return false;
        }
    }

    private void FailQTE() {

        CodeText.text = textBeforeQTE + "<color=red>" + currentQTECharacter + "</color>";
        
        waitingForKey = false;

        timeCircle.gameObject.SetActive(false);

        currentFails++;

        Debug.Log("Fallo " + currentFails + "/" + maxFails);

        if (currentFails >= maxFails) {
            Debug.Log("Demasiados fallos");

            computer.PowerOff();
        }
    }

    public void ResetMinigame() { 
        StopAllCoroutines();

        waitingForKey = false;

        currentFails = 0;
        charactersSinceLastQTE = 0;

        keyTimer = 0f;

        CodeText.text = "";

        qteContainer.gameObject.SetActive(false);
    }
}
