using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HackerQTE : MonoBehaviour
{
    [SerializeField] private Computer computer;

    [SerializeField] private float qteInterval = 10f;
    
    [SerializeField] private int startingSequenceLength = 2;
    [SerializeField] private int maxSequenceLength = 5;

    private float qteTimer;

    private Key[] qteKeys = 
        { 
        Key.W, 
        Key.A, 
        Key.S, 
        Key.D, 
        Key.Q, 
        Key.E, 
        Key.Space 
    };

    private Key[] currentSequence;

    private int currentKeyIndex;
    private int currentSequenceLength;
    
    private bool qteActive;

    [SerializeField] private float keyTime = 5f;
    [SerializeField] private float minKeyTime = 2f;
    [SerializeField] private float timeReduction = 0.5f;

    private float keyTimer;

    private void Awake() {
        currentSequenceLength = startingSequenceLength;
    }

    private void Update() {

        if (!qteActive) {
            qteTimer += Time.deltaTime;

            if (qteTimer >= qteInterval) {
                StartQTE();
                qteTimer = 0f;
            }
        } else {
            keyTimer -= Time.deltaTime;

            if (keyTimer <= 0f) { 
                FailQTE();
                return;
            }

            Key expectedKey = currentSequence[currentKeyIndex];

            if (Keyboard.current[expectedKey].wasPressedThisFrame) {
                currentKeyIndex++;

                if (currentKeyIndex >= currentSequence.Length) {
                    CompleteQTE();
                } else {
                    keyTimer = keyTime;

                    Debug.Log("Tecla actual: " + currentSequence[currentKeyIndex]);
                }
            } else if (Keyboard.current.anyKey.wasPressedThisFrame){
                FailQTE();
            }
        }
    }

    private void StartQTE() {
        currentSequence = new Key[currentSequenceLength];

        for (int i = 0; i < currentSequence.Length; i++) { 
            int randomIndex = Random.Range(0, qteKeys.Length);
            currentSequence[i] = qteKeys[randomIndex];
            }

        currentKeyIndex = 0;
        qteActive = true;

        keyTimer = keyTime;

        Debug.Log("QTE iniciado");
        Debug.Log("Tecla actual: " + currentSequence[currentKeyIndex]);

    }

    private void CompleteQTE() { 
        qteActive = false;

        Debug.Log("QTE completado");

        if (currentSequenceLength < maxSequenceLength) {
            currentSequenceLength++;
        }
    }

    private void FailQTE() { 
        qteActive = false;

        Debug.Log("QTE fallado");

        computer.PowerOff();
    }
}
