using UnityEngine;

public class InteractionMissionGate : MonoBehaviour {
    public enum Requirement {
        CableRepaired,
        FuseInstalled,
        Mission2Started
    }

    [Header("Requisito de interacción")]
    [SerializeField] private Requirement requirement;

    [Header("Mensaje mientras está bloqueado")]
    [SerializeField]
    private string blockedMessage =
        "No disponible todavía";

    private Mission1TaskController missionController;

    public bool TryGetBlockedMessage(out string message) {
        message = null;

        if (missionController == null) {
            missionController =
                FindFirstObjectByType<Mission1TaskController>();
        }

        // Si todavía no existe el controlador,
        // no mostramos un bloqueo inventado.
        if (missionController == null)
            return false;

        int stage = missionController.CurrentStage;

        bool blocked = requirement switch {
            Requirement.CableRepaired => stage < 2,
            Requirement.FuseInstalled => stage < 3,
            Requirement.Mission2Started => stage < 4,
            _ => false
        };

        if (!blocked)
            return false;

        message = blockedMessage;
        return true;
    }
}
