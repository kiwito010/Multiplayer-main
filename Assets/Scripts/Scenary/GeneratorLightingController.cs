using Fusion;
using UnityEngine;

public class GeneratorLightingController : MonoBehaviour {
    [Header("Generador")]
    [SerializeField] private RepairableComponent repairableComponent;

    [Header("Luces principales")]
    [SerializeField] private Light[] mainLights;

    [Header("Luces de emergencia")]
    [SerializeField] private Light[] emergencyLights;

    [Header("Materiales de luces")]
    [SerializeField] private Renderer[] mainLightRenderers;
    [SerializeField] private Renderer[] emergencyLightRenderers;

    [Header("Pantalla de computadora")]
    [SerializeField] private Renderer computerScreenRenderer;

    [SerializeField] private Material screenOnMaterial;
    [SerializeField] private Material screenOffMaterial;

    private bool? lastState;

    private void Awake() {
        SetLights(false);
    }

    private void Update() {
        if (repairableComponent == null)
            return;

        // Esperar a que Photon Fusion inicialice el objeto.
        NetworkObject networkObject = repairableComponent.Object;

        if (networkObject == null || !networkObject.IsValid)
            return;

        bool shouldBeOn = repairableComponent.IsRepaired();

        if (lastState.HasValue && lastState.Value == shouldBeOn)
            return;

        lastState = shouldBeOn;

        SetLights(shouldBeOn);

        Debug.Log("[LIGHTING] MainLights " + (shouldBeOn ? "ENCENDIDAS" : "APAGADAS"));
    }

    private void SetLights(bool state) {
        foreach (Light light in mainLights) {
            if (light != null)
                light.enabled = state;
        }

        foreach (Light light in emergencyLights) {
            if (light != null)
                light.enabled = !state;
        }

        SetEmission(mainLightRenderers, state);
        SetEmission(emergencyLightRenderers, !state);

        SetComputerScreen(state);
    }

    private void SetEmission(Renderer[] renderers, bool state) {
        foreach (Renderer rend in renderers) {
            if (rend == null)
                continue;

            foreach (Material material in rend.materials) {
                if (state) {
                    material.EnableKeyword("_EMISSION");
                } else {
                    material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private void SetComputerScreen(bool state) {
        if (computerScreenRenderer == null)
            return;

        Material[] materials = computerScreenRenderer.materials;

        if (state) {
            materials[3] = screenOnMaterial;
        } else {
            materials[3] = screenOffMaterial;
        }

        computerScreenRenderer.materials = materials;
    }
}
