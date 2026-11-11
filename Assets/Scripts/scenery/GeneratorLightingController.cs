
using UnityEngine;

public class GeneratorLightingController : MonoBehaviour {
    [Header("Activacion cooperativa")]
    [SerializeField] private CooperativeActivation cooperativeActivation;

    [Header("Luces principales")]
    [SerializeField] private Light[] mainLights;

    private bool? lastState;

    private void Awake() {
        // Las luces comienzan apagadas.
        SetLights(false);
    }

    private void Update() {
        bool shouldBeOn =
            cooperativeActivation != null &&
            cooperativeActivation.IsActivationCompleted();

        // Solo actualizar cuando cambia el estado.
        if (lastState.HasValue && lastState.Value == shouldBeOn)
            return;

        lastState = shouldBeOn;
        SetLights(shouldBeOn);

        Debug.Log("[LIGHTING] Luces principales " +
                  (shouldBeOn ? "ENCENDIDAS" : "APAGADAS"));
    }

    private void SetLights(bool enabled) {
        foreach (Light light in mainLights) {
            if (light != null)
                light.enabled = enabled;
        }
    }
}
