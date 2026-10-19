using TMPro;
using UnityEngine;

public class CableRepairMinigame : MonoBehaviour, IRepairMinigame {
    private enum RepairPhase {
        CutBrokenCable,
        RemoveBrokenCable,
        InsertNewCable,
        Wrap,
        Complete
    }

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameInput gameInput;

    [SerializeField] private GameObject content;

    [SerializeField] private GameObject brokenCable;
    [SerializeField] private GameObject newCable;
    [SerializeField] private GameObject socket;
    [SerializeField] private GameObject tape;
    
    [SerializeField] private GameObject leftCutPoint;
    [SerializeField] private GameObject rightCutPoint;

    private bool leftCut;
    private bool rightCut;

    [SerializeField] private TextMeshProUGUI instructionText;

    private RepairPhase currentPhase;

    private RepairableComponent currentComponent;
    private PlayerInventory currentInventory;

    private bool repairStarted;

    private void Start() {
        currentPhase = RepairPhase.CutBrokenCable;
        UpdatePhaseUI();
    }

    private void Update() {
        if (gameInput.GetExitComputerPressed()) {
            Close();
        }
    }

    public void Open(RepairableComponent component, PlayerInventory inventory) {
        currentComponent = component;
        currentInventory = inventory;

        if (!repairStarted) {
            currentPhase = RepairPhase.CutBrokenCable;

            leftCut = false;
            rightCut = false;

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
        brokenCable.SetActive(
            currentPhase == RepairPhase.CutBrokenCable ||
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
            currentPhase == RepairPhase.Wrap
        );

        leftCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable && !leftCut
        );

        rightCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable && !rightCut
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
                UpdatePhaseUI();
                CompleteRepair();
                break;
        }

        UpdatePhaseUI();
    }

    public void CutLeftSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (leftCut)
            return;

        leftCut = true;

        leftCutPoint.SetActive(false);
        Debug.Log("Corte izquierdo");

        CheckCutsComplete();
    }

    public void CutRightSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (rightCut)
            return;

        rightCut = true;

        rightCutPoint.SetActive(false);
        Debug.Log("Corte derecho");

        CheckCutsComplete();
    }

    private void CheckCutsComplete() {
        if (leftCut && rightCut) {
            CompleteCurrentPhase();
        }
    }

    private void CompleteRepair() {
        currentComponent.Repair();

        Close();
    }

    public bool CanRemoveBrokenCable() {
        return currentPhase == RepairPhase.RemoveBrokenCable;
    }

    public void BrokenCableRemoved() {
        if (currentPhase != RepairPhase.RemoveBrokenCable)
            return;

        CompleteCurrentPhase();
    }
}