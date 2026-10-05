using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class HackerTypingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI CodeText;

    [TextArea]
    [SerializeField] private string fullText;

    [SerializeField] private float typingSpeed = 0.04f;

    private bool waitingForKey;
    private Key expectedKey;

    private void Start() {
        StartCoroutine(TypeText());
    }

    private void Update() {
        if (!waitingForKey)
            return;

        if (Keyboard.current[expectedKey].wasPressedThisFrame) { 
            waitingForKey = false;

            Debug.Log("Tecla correcta");
        }
    }

    private IEnumerator TypeText() {
        CodeText.text = "";

        for (int i = 0; i < fullText.Length; i++) { 
            char currentCharacter = fullText[i];

            if (TryGetKeyFromChat(currentCharacter, out Key key)) {

                expectedKey = key;
                waitingForKey = true;

                CodeText.text += "<color=yellow>" + currentCharacter + "</color>";

                Debug.Log("Esperando tecla " + expectedKey);

                yield return new WaitUntil(() => !waitingForKey);
            } else {
                CodeText.text += currentCharacter;
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private bool TryGetKeyFromChat(char character, out Key key) {
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
}
