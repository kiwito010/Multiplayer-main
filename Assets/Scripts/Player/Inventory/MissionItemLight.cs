
using UnityEngine;

public class MissionItemLight : MonoBehaviour {

    [Header("Luz de misión")]
    [SerializeField] private Light missionLight;

    [Header("Materiales del objeto")]
    [SerializeField] private Renderer itemRenderer;
    [SerializeField] private int[] materialIndexes;

    private Material[] selectedMaterials;
    private Color[] originalEmissionColors;

    private void Awake() {
        if (itemRenderer != null && materialIndexes != null) {

            Material[] materials = itemRenderer.materials;

            selectedMaterials = new Material[materialIndexes.Length];
            originalEmissionColors = new Color[materialIndexes.Length];

            for (int i = 0; i < materialIndexes.Length; i++) {
                int index = materialIndexes[i];

                if (index < 0 || index >= materials.Length)
                    continue;

                Material material = materials[index];

                if (!material.HasProperty("_EmissionColor"))
                    continue;

                selectedMaterials[i] = material;

                // Guardamos el color y la intensidad originales.
                originalEmissionColors[i] =
                    material.GetColor("_EmissionColor");
            }
        }

        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted) {

        if (missionLight != null)
            missionLight.enabled = highlighted;

        if (selectedMaterials == null)
            return;

        for (int i = 0; i < selectedMaterials.Length; i++) {
            Material material = selectedMaterials[i];

            if (material == null)
                continue;

            if (highlighted) {
                material.EnableKeyword("_EMISSION");

                material.SetColor(
                    "_EmissionColor",
                    originalEmissionColors[i]
                );
            } else {
                material.SetColor(
                    "_EmissionColor",
                    Color.black
                );

                material.DisableKeyword("_EMISSION");
            }
        }
    }
}
