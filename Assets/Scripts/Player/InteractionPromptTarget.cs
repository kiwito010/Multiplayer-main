using UnityEngine;

public class InteractionPromptTarget : MonoBehaviour {
    [Header("Mensaje de interacción")]
    [SerializeField] private string interactionText = "Interactuar";

    public string GetInteractionText() {
        return interactionText;
    }
}
