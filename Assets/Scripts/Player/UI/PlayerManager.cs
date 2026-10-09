using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private GameInput gameInput;

    private void Awake() {
        gameInput = GetComponent<GameInput>();
    }

    public void EnterUIMode() {
        gameInput.DisableMovement();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitUIMode() { 
        gameInput.EnableMovement();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
