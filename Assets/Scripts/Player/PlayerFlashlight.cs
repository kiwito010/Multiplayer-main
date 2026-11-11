
using Fusion;
using UnityEngine;

public class PlayerFlashlight : NetworkBehaviour {
    [Header("Referencias")]
    [SerializeField] private Light flashlight;
    private GameInput gameInput;

    private bool isOn = false;

    private void Awake() {
        gameInput = GetComponent<GameInput>();

        if (flashlight != null) {
            flashlight.gameObject.SetActive(true);
            flashlight.enabled = false;
        }
    }

    private void Update() {
        if (Object == null ||
            !Object.IsValid ||
            !Object.HasInputAuthority)
            return;

        if (flashlight == null || gameInput == null)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (gameInput.WasFlashlightPressed()) {
            isOn = !isOn;

            flashlight.gameObject.SetActive(true);
            flashlight.enabled = isOn;

            Debug.Log("[FLASHLIGHT] " + (isOn ? "Encendida" : "Apagada"));
        }
    }
}
