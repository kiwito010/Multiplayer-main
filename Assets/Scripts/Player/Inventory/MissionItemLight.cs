
using UnityEngine;

public class MissionItemLight : MonoBehaviour {

    [Header("Luz de misión")]
    [SerializeField] private Light missionLight;

    [Header("Materiales del objeto")]
    [SerializeField] private Renderer itemRenderer;
    [SerializeField] private int[] materialIndexes;

    private Material[] selectedMaterials;

    private void Awake() {
        if (itemRenderer != null) {
            Material[] materials = itemRenderer.materials;

            selectedMaterials = new Material[materialIndexes.Length];

            for (int i = 0; i < materialIndexes.Length; i++) {
                int index = materialIndexes[i];

                if (index >= 0 && index < materials.Length) {
                    selectedMaterials[i] = materials[index];
                }
            }
        }

        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted) {
        // Encender o apagar la Point Light.
        if (missionLight != null)
            missionLight.enabled = highlighted;

        if (selectedMaterials == null)
            return;

        // Activar o desactivar Emission en los materiales elegidos.
        foreach (Material material in selectedMaterials) {
            if (material == null)
                continue;

            if (highlighted) {
                material.EnableKeyword("_EMISSION");
            } else {
                material.DisableKeyword("_EMISSION");
            }
        }
    }
}
