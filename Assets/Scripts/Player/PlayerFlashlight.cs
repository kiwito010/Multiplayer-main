using Fusion;
using UnityEngine;

public class PlayerFlashlight : NetworkBehaviour {

    [Header("Referencias")]
    [SerializeField] private Light flashlight;

    private GameInput gameInput;

    // Estado sincronizado de la linterna.
    [Networked] private NetworkBool IsFlashlightOn { get; set; }

    private void Awake() {
        gameInput = GetComponent<GameInput>();

        if (flashlight != null) {
            flashlight.gameObject.SetActive(true);
            flashlight.enabled = false;
        }
    }

    public override void Spawned() {
        if (Object.HasStateAuthority) {
            IsFlashlightOn = false;
        }

        UpdateFlashlightVisual();
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
            RequestToggleFlashlight();
        }
    }

    private void RequestToggleFlashlight() {
        // Si somos el host y tenemos autoridad,
        // modificamos directamente el estado.
        if (Object.HasStateAuthority) {
            ToggleFlashlight();
            return;
        }

        // Si somos un cliente, pedimos al host
        // que cambie el estado.
        RPC_RequestToggleFlashlight();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestToggleFlashlight() {
        ToggleFlashlight();
    }

    private void ToggleFlashlight() {
        if (!Object.HasStateAuthority)
            return;

        IsFlashlightOn = !IsFlashlightOn;

        Debug.Log("[FLASHLIGHT] " +
            (IsFlashlightOn ? "Encendida" : "Apagada"));
    }

    public override void Render() {
        UpdateFlashlightVisual();
    }

    private void UpdateFlashlightVisual() {
        if (flashlight == null)
            return;

        flashlight.gameObject.SetActive(true);
        flashlight.enabled = IsFlashlightOn;
    }
}
