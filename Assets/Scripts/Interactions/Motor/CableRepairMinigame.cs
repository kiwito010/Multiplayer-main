using TMPro;
using UnityEngine;

public class CableRepairMinigame : MonoBehaviour {
    private enum RepairPhase {
        CutBrokenCable,
        RemoveBrokenCable,
        InsertNewCable,
        Wrap,
        Complete
    }

    [SerializeField] private GameObject brokenCable;
    [SerializeField] private GameObject newCable;
    [SerializeField] private GameObject socket;
    [SerializeField] private GameObject tape;

    [SerializeField] private TextMeshProUGUI instructionText;

    private RepairPhase currentPhase;

    private void Start() {
        currentPhase = RepairPhase.CutBrokenCable;
        UpdatePhaseUI();
    }

    private void UpdatePhaseUI() {
        brokenCable.SetActive(
            currentPhase == RepairPhase.Unwrap ||
            currentPhase == RepairPhase.RemoveBrokenCable
        );

        newCable.SetActive(
            currentPhase == RepairPhase.InsertNewCable ||
            currentPhase == RepairPhase.Wrap ||
            currentPhase == RepairPhase.Complete
        );

        socket.SetActive(
            currentPhase == RepairPhase.RemoveBrokenCable ||
            currentPhase == RepairPhase.InsertNewCable ||
            currentPhase == RepairPhase.Wrap ||
            currentPhase == RepairPhase.Complete
        );

        tape.SetActive(
            currentPhase == RepairPhase.Unwrap ||
            currentPhase == RepairPhase.Wrap
        );

        UpdateInstruction();
    }

    private void UpdateInstruction() {
        switch (currentPhase) {
            case RepairPhase.CutBrokenCable:
                instructionText.text = "Corta las puntas del cable roto";
                break;

            case RepairPhase.RemoveBrokenCable:
                instructionText.text = "Retira el cable roto";
                break;

            case RepairPhase.InsertNewCable:
                instructionText.text = "Coloca el cable nuevo";
                break;

            case RepairPhase.Wrap:
                instructionText.text = "Encinta la reparacion";
                break;

            case RepairPhase.Complete:
                instructionText.text = "REPARACION COMPLETA";
                break;
        }
    }

    public void CompleteCurrentPhase() {
        switch (currentPhase) {
            case RepairPhase.CutBrokenCable:
                currentPhase = RepairPhase.RemoveBrokenCable;
                break;

            case RepairPhase.RemoveBrokenCable:
                currentPhase = RepairPhase.InsertNewCable;
                break;

            case RepairPhase.InsertNewCable:
                currentPhase = RepairPhase.Wrap;
                break;

            case RepairPhase.Wrap:
                currentPhase = RepairPhase.Complete;
                break;
        }

        UpdatePhaseUI();
    }
}