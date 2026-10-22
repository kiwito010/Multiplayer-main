using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CableRepairMinigame : MonoBehaviour, IRepairMinigame {
    private enum RepairPhase {
        CutBrokenCable,
        RemoveBrokenCable,
        PlaceNewCable,
        TwistEnds,
        Wrap,
        Complete
    }

    [SerializeField] private GameObject content;

    [SerializeField] private GameObject brokenCable;
    [SerializeField] private GameObject newCable;
    [SerializeField] private GameObject socket;
    [SerializeField] private GameObject tape;

    [SerializeField] private TextMeshProUGUI instructionText;

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameInput gameInput;

    private RepairPhase currentPhase;

    private RepairableComponent currentComponent;
    private PlayerInventory currentInventory;

    [SerializeField] private GameObject leftCutPoint;
    [SerializeField] private GameObject rightCutPoint;

    private bool leftCut;
    private bool rightCut;

    private bool brokenCableRemoved;

    private bool repairStarted;

    [SerializeField] private GameObject leftTwistPoint;
    [SerializeField] private GameObject rightTwistPoint;

    private bool leftTwisted;
    private bool rightTwisted;

    [SerializeField] private RectTransform leftWrapCenter;
    [SerializeField] private RectTransform rightWrapCenter;
    
    private bool leftWrapped;
    private bool rightWrapped;

    [SerializeField] private float requiredWrapRotation = 720f;

    private float leftWrapRotation;
    private float rightWrapRotation;

    private float previousTapeAngle;
    private bool isWrapping;

    //required objects
    [SerializeField] private GameObject requiredItemMessage;
    [SerializeField] private TextMeshProUGUI requiredItemMessageText;

    private RectTransform currentWrapCenter;
    private bool wrappingLeft;

    private string requiredCableName = "New Cable";


    private string requiredPliersName = "Alicate";
    private string requiredTapeName = "Tape";


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
        newCable.SetActive(HasRequiredCable());
        tape.SetActive(HasRequiredTape());

        leftCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable &&
            !leftCut
        );

        rightCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable &&
            !rightCut
        );

        leftTwistPoint.SetActive(
            currentPhase == RepairPhase.TwistEnds &&
            !leftTwisted
        );

        rightTwistPoint.SetActive(
            currentPhase == RepairPhase.TwistEnds &&
            !rightTwisted
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

            case RepairPhase.TwistEnds:
                instructionText.text = "Conecta las puntas del cable";
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

        if (!HasRequiredPliers()) {
            ShowRequiredItemMessage("ALICATE NECESARIO");
            return;
        }

        leftCut = true;

        UpdatePhaseUI();
        CheckCutsComplete();
    }

    public void CutRightSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (rightCut)
            return;

        if (!HasRequiredPliers()) {
            ShowRequiredItemMessage("ALICATE NECESARIO");
            return;
        }

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
                currentPhase = RepairPhase.TwistEnds;
                break;

            case RepairPhase.TwistEnds:
                currentPhase = RepairPhase.Wrap;
                break;

            case RepairPhase.Wrap:
                currentPhase = RepairPhase.Complete;
                CompleteRepair();
                return;
        }

        UpdatePhaseUI();
    }

    public void NewCablePlaced() {
        if (!brokenCableRemoved)
            return;

        if (currentPhase != RepairPhase.PlaceNewCable)
            return;

        if (!HasRequiredCable())
            return;

        bool removed =
            currentInventory.RemoveItem(requiredCableName);

        if (!removed)
            return;

        CompleteCurrentPhase();
    }

    public void TwistLeftSide() {
        if (currentPhase != RepairPhase.TwistEnds)
            return;

        if (leftTwisted)
            return;

        leftTwisted = true;

        UpdatePhaseUI();
        CheckTwistsComplete();
    }

    public void TwistRightSide() {
        if (currentPhase != RepairPhase.TwistEnds)
            return;

        if (rightTwisted)
            return;

        rightTwisted = true;

        UpdatePhaseUI();
        CheckTwistsComplete();
    }

    private void CheckTwistsComplete() {
        if (leftTwisted && rightTwisted) {
            CompleteCurrentPhase();
        }
    }

    public void StartWrapping(Vector2 tapePosition) {
        if (currentPhase != RepairPhase.Wrap)
            return;

        if (!HasRequiredTape())
            return;

        isWrapping = true;

        UpdateCurrentWrapTarget(tapePosition);

        previousTapeAngle = GetTapeAngle(
            tapePosition,
            currentWrapCenter
        );
    }


    public void UpdateWrapping(Vector2 tapePosition) {
        if (!isWrapping)
            return;

        UpdateCurrentWrapTarget(tapePosition);

        float currentTapeAngle = GetTapeAngle(
            tapePosition,
            currentWrapCenter
        );

        float angleDifference = Mathf.DeltaAngle(
            previousTapeAngle,
            currentTapeAngle
        );

        previousTapeAngle = currentTapeAngle;

        if (angleDifference < 0f) {
            float rotationAmount = Mathf.Abs(angleDifference);

            if (wrappingLeft) {
                leftWrapRotation += rotationAmount;

                if (leftWrapRotation >= requiredWrapRotation) {
                    leftWrapped = true;
                    Debug.Log("Punta izquierda encintada");
                }
            } else {
                rightWrapRotation += rotationAmount;

                if (rightWrapRotation >= requiredWrapRotation) {
                    rightWrapped = true;
                    Debug.Log("Punta derecha encintada");
                }
            }
        }

        if (leftWrapped && rightWrapped) {
            isWrapping = false;

            currentInventory.RemoveItem(requiredTapeName);

            CompleteCurrentPhase();
        }
    }
    private void UpdateCurrentWrapTarget(Vector2 tapePosition) {
        float distanceToLeft =
            Vector2.Distance(tapePosition, leftWrapCenter.position);

        float distanceToRight =
            Vector2.Distance(tapePosition, rightWrapCenter.position);

        if (!leftWrapped &&
            distanceToLeft < distanceToRight) {
            currentWrapCenter = leftWrapCenter;
            wrappingLeft = true;
        } else if (!rightWrapped) {
            currentWrapCenter = rightWrapCenter;
            wrappingLeft = false;
        }
    }

    public void StopWrapping() {
        isWrapping = false;
    }

    private float GetTapeAngle(Vector2 tapePosition, RectTransform center) {
        Vector2 direction = tapePosition - (Vector2)center.position;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void CompleteRepair() {
        currentComponent.Repair();

        Close();
    }

    private void ShowRequiredItemMessage(string message) {
        requiredItemMessageText.text = message;
        requiredItemMessage.SetActive(true);

        CancelInvoke(nameof(HideRequiredItemMessage));
        Invoke(nameof(HideRequiredItemMessage), 2f);
    }

    private void HideRequiredItemMessage() {
        requiredItemMessage.SetActive(false);
    }

    private bool HasRequiredCable()
{
    if (currentInventory == null)
    {
        Debug.Log("currentInventory es null");
        return false;
    }

    bool hasCable = currentInventory.HasItem(requiredCableName);

    Debug.Log("Buscando cable: " + requiredCableName);
    Debug.Log("Tiene cable: " + hasCable);

    return hasCable;
}

    public void CheckCableRequired() {
        if (currentPhase != RepairPhase.PlaceNewCable)
            return;

        if (HasRequiredCable())
            return;

        ShowRequiredItemMessage("CABLE NECESARIO");

        CancelInvoke(nameof(HideCableRequiredMessage));
        Invoke(nameof(HideCableRequiredMessage), 2f);
    }

    private bool HasRequiredPliers() {
        if (currentInventory == null)
            return false;

        return currentInventory.HasItem(requiredPliersName);
    }

    private void HideCableRequiredMessage() {
        ShowRequiredItemMessage("CABLE NECESARIO");
    }

    private bool HasRequiredTape() {
        if (currentInventory == null)
            return false;

        return currentInventory.HasItem(requiredTapeName);
    }

    public void CheckTapeRequired() {
        if (currentPhase != RepairPhase.Wrap)
            return;

        if (HasRequiredTape())
            return;

        ShowRequiredItemMessage("TAPE NECESARIA");
    }
}
