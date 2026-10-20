using TMPro;
using UnityEngine;

public class CableRepairMinigame : MonoBehaviour, IRepairMinigame {
    private enum RepairPhase {
        CutBrokenCable,
        RemoveBrokenCable,
        PlaceNewCable,
        Wrap,
        Complete
    }

    [SerializeField] private GameObject content;

    [SerializeField] private GameObject brokenCable;
    [SerializeField] private GameObject newCable;
    [SerializeField] private GameObject socket;
    [SerializeField] private GameObject tape;

    [SerializeField] private GameObject leftCutPoint;
    [SerializeField] private GameObject rightCutPoint;

    [SerializeField] private TextMeshProUGUI instructionText;

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameInput gameInput;

    private RepairPhase currentPhase;

    private RepairableComponent currentComponent;
    private PlayerInventory currentInventory;

    private bool leftCut;
    private bool rightCut;

    private bool brokenCableRemoved;

    private bool repairStarted;

    private void Update() {
        if (content.activeSelf && gameInput.GetExitPressed()) {
            Close();
        }
    }

    public void Open(
        RepairableComponent component,
        PlayerInventory inventory) {
        currentComponent = component;
        currentInventory = inventory;

        if (!repairStarted) {
            currentPhase = RepairPhase.CutBrokenCable;

            leftCut = false;
            rightCut = false;

            brokenCableRemoved = false;

            repairStarted = true;
        }

        content.SetActive(true);

        playerManager.EnterUIMode();

        UpdatePhaseUI();
    }

    public void Close() {
        playerManager.ExitUIMode();

        content.SetActive(false);
    }

    private void UpdatePhaseUI() {
        brokenCable.SetActive(true);
        newCable.SetActive(true);
        socket.SetActive(true);
        tape.SetActive(true);

        leftCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable &&
            !leftCut
        );

        rightCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable &&
            !rightCut
        );

        UpdateInstruction();
    }

    private void UpdateInstruction() {
        switch (currentPhase) {
            case RepairPhase.CutBrokenCable:
                instructionText.text =
                    "Corta las puntas del cable roto";
                break;

            case RepairPhase.RemoveBrokenCable:
                instructionText.text =
                    "Retira el cable roto";
                break;

            case RepairPhase.PlaceNewCable:
                instructionText.text =
                    "Coloca el cable nuevo";
                break;

            case RepairPhase.Wrap:
                instructionText.text =
                    "Encinta la reparacion";
                break;

            case RepairPhase.Complete:
                instructionText.text =
                    "REPARACION COMPLETA";
                break;
        }
    }

    public void CutLeftSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (leftCut)
            return;

        leftCut = true;

        UpdatePhaseUI();
        CheckCutsComplete();
    }

    public void CutRightSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (rightCut)
            return;

        rightCut = true;

        UpdatePhaseUI();
        CheckCutsComplete();
    }

    private void CheckCutsComplete() {
        if (leftCut && rightCut) {
            CompleteCurrentPhase();
        }
    }

    public bool CanRemoveBrokenCable() {
        return currentPhase == RepairPhase.RemoveBrokenCable;
    }

    public void BrokenCableRemoved() {
        if (currentPhase != RepairPhase.RemoveBrokenCable)
            return;

        brokenCableRemoved = true;

        CompleteCurrentPhase();
    }

    public bool CanPlaceNewCable() {
        return brokenCableRemoved;
    }

    public void CompleteCurrentPhase() {
        switch (currentPhase) {
            case RepairPhase.CutBrokenCable:
                currentPhase = RepairPhase.RemoveBrokenCable;
                break;

            case RepairPhase.RemoveBrokenCable:
                currentPhase = RepairPhase.PlaceNewCable;
                break;

            case RepairPhase.PlaceNewCable:
                currentPhase = RepairPhase.Wrap;
                break;

            case RepairPhase.Wrap:
                currentPhase = RepairPhase.Complete;
                CompleteRepair();
                return;
        }

        UpdatePhaseUI();
    }

    private void CompleteRepair() {
        currentComponent.Repair();

        Close();
    }
}